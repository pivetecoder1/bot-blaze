using System;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

public static class ApiConsumer
{
    #region Models

    public class Data
    {
        public string id { get; set; } = "";
        public DateTime created_at { get; set; }
        public int color { get; set; }
        public int roll { get; set; }
        public string server_seed { get; set; } = "";
    }

    public class Estrategias
    {
        public int color;
        public int colorProteger;
        public int comparacaoUm;
        public int comparacaoDois;
        public int comparacaoTres;
        public int comparacaoQuatro;
    }

    #endregion

    #region Fields

    private static TelegramBotClient? botClient;
    private static readonly HttpClient httpClient = new HttpClient();
    
    private const string ApiUrl = "https://blaze.bet.br/api/singleplayer-originals/originals/roulette_games/recent/1";
    private const string ChatId = "5254675478";
    private const string BotToken = "8628893389:AAHkUuGs4Kgq9kd5KDgegvLtQf6W4yt1SV0";
    
    private static readonly Dictionary<string, string> Headers = new()
    {
        { "accept", "application/json, text/plain, */*" },
        { "accept-language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7,es;q=0.6" },
        { "authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6MTE0MTE1MTMsImlzUmVmcmVzaFRva2VuIjpmYWxzZSwiYmxvY2tzIjpbXSwidXVpZCI6IjFlMjA0OTU1LTZjNWUtNGZmZC04MDI5LWZkZTI0ODEwMDBiMiIsImlhdCI6MTc3NTE3NDU1NSwiZXhwIjoxNzc1MjE3NzU1fQ.dijjGTJZbRF17zSfWuZouLFNJ5RtbvAhUhrEpYmwwGU" },
        { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36" },
        { "x-client-version", "ed795cb38" },
        { "x-session-id", "Wld46D5Ybl" },
        { "referer", "https://blaze.bet.br/pt/games/double" }
    };

    public static int wins = 0;
    public static int losses = 0;
    public static int whites = 0;
    public static int totalEntradas = 0;
    public static int tentativasAtuais = 0;
    public static bool emJogo = false;

    private static string ultimosResultados = "";

    #endregion

    #region Main

    static async Task Main(string[] args)
    {
        foreach (var header in Headers)
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        botClient = new TelegramBotClient(BotToken);
        Console.WriteLine("=========================================");
        Console.WriteLine("        BOT BLAZE INICIADO!              ");
        Console.WriteLine("=========================================");
        Console.WriteLine($"Chat ID: {ChatId}");
        Console.WriteLine("=========================================");
        
        await Start();
    }

    #endregion

    #region Estratégias

    public static List<Estrategias> GetListaDeEstrategias()
    {
        return new List<Estrategias>
        {
            new Estrategias() { color = 1, colorProteger = 0, comparacaoUm = 1, comparacaoDois = 2, comparacaoTres = 1, comparacaoQuatro = 2 },
            new Estrategias() { color = 2, colorProteger = 0, comparacaoUm = 2, comparacaoDois = 1, comparacaoTres = 2, comparacaoQuatro = 1 },
            new Estrategias() { color = 2, colorProteger = 0, comparacaoUm = 2, comparacaoDois = 1, comparacaoTres = 2, comparacaoQuatro = 2 },
            new Estrategias() { color = 1, colorProteger = 0, comparacaoUm = 1, comparacaoDois = 2, comparacaoTres = 1, comparacaoQuatro = 1 },
            new Estrategias() { color = 1, colorProteger = 0, comparacaoUm = 1, comparacaoDois = 1, comparacaoTres = 2, comparacaoQuatro = 2 },
            new Estrategias() { color = 2, colorProteger = 0, comparacaoUm = 2, comparacaoDois = 2, comparacaoTres = 1, comparacaoQuatro = 1 },
            new Estrategias() { color = 2, colorProteger = 0, comparacaoUm = 1, comparacaoDois = 2, comparacaoTres = 2, comparacaoQuatro = 1 },
            new Estrategias() { color = 2, colorProteger = 0, comparacaoUm = 1, comparacaoDois = 1, comparacaoTres = 1, comparacaoQuatro = 2 }
        };
    }

    #endregion

    #region API

    public static async Task<List<Data>> RetornaUltimosResultados()
    {
        try
        {
            var response = await httpClient.GetAsync(ApiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<Data>();
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Data>>(responseBody) ?? new List<Data>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na API: {ex.Message}");
            return new List<Data>();
        }
    }

    #endregion

    #region Lógica Principal

    public static async Task Start()
    {
        await CheckEstrategias(GetListaDeEstrategias());
    }

    public static async Task CheckEstrategias(List<Estrategias> ListEstrategias)
    {
        while (true)
        {
            await Task.Delay(1500);
            var dataList = await RetornaUltimosResultados();

            if (dataList.Count < 4) continue;

            // Converte cores para nomes com emojis
            string cor1 = dataList[3].color == 0 ? "⚪ BRANCO" : (dataList[3].color == 1 ? "🔴 VERMELHO" : "⚫ PRETO");
            string cor2 = dataList[2].color == 0 ? "⚪ BRANCO" : (dataList[2].color == 1 ? "🔴 VERMELHO" : "⚫ PRETO");
            string cor3 = dataList[1].color == 0 ? "⚪ BRANCO" : (dataList[1].color == 1 ? "🔴 VERMELHO" : "⚫ PRETO");
            string cor4 = dataList[0].color == 0 ? "⚪ BRANCO" : (dataList[0].color == 1 ? "🔴 VERMELHO" : "⚫ PRETO");
            
            string cores = $"🎨 Últimas cores:\n{cor1} | {cor2} | {cor3} | {cor4}";
            
            if (cores != ultimosResultados)
            {
                ultimosResultados = cores;
                Console.Clear();
                Console.WriteLine("=========================================");
                Console.WriteLine("        BOT BLAZE INICIADO!              ");
                Console.WriteLine("=========================================");
                Console.WriteLine(cores);
                Console.WriteLine("-----------------------------------------");
                Console.WriteLine($"✅ Wins: {wins} | ❌ Loss: {losses} | ⚪ Branco: {whites}");
                Console.WriteLine($"📊 Total: {totalEntradas} | 🎯 Win%: {GetWinPercentage():F1}%");
                Console.WriteLine("=========================================");
                Console.WriteLine("🔍 Procurando padrões...");
            }

            foreach (var estrategia in ListEstrategias)
            {
                if (estrategia.comparacaoUm == dataList[0].color &&
                    estrategia.comparacaoDois == dataList[1].color &&
                    estrategia.comparacaoTres == dataList[2].color &&
                    estrategia.comparacaoQuatro == dataList[3].color)
                {
                    string nomeCor = estrategia.color == 0 ? "BRANCO" : (estrategia.color == 1 ? "VERMELHO" : "PRETO");
                    string emojiCor = estrategia.color == 0 ? "⚪" : (estrategia.color == 1 ? "🔴" : "⚫");
                    string protecao = "⚪️";
                    
                    await EnviarMensagemEntrada(nomeCor, emojiCor, protecao);
                    await AguardarResultado(dataList[0].id, estrategia.color, estrategia.colorProteger);
                    
                    await Task.Delay(5000);
                }
            }
        }
    }

    #endregion

    #region Aguardar Resultado

    public static async Task AguardarResultado(string idRodada, int corSelecionada, int corProtecao)
    {
        emJogo = true;
        tentativasAtuais = 1;
        
        while (tentativasAtuais <= 3)
        {
            await Task.Delay(3000);
            var dataList = await RetornaUltimosResultados();
            
            if (dataList.Count > 0 && dataList[0].id != idRodada)
            {
                int resultado = dataList[0].color;
                
                if (resultado == corSelecionada)
                {
                    wins++;
                    totalEntradas++;
                    await EnviarMensagemWin(corSelecionada, tentativasAtuais);
                    emJogo = false;
                    return;
                }
                else if (resultado == corProtecao)
                {
                    whites++;
                    totalEntradas++;
                    await EnviarMensagemWhite();
                    emJogo = false;
                    return;
                }
                else
                {
                    if (tentativasAtuais < 3)
                    {
                        tentativasAtuais++;
                        await EnviarMensagemGale(tentativasAtuais, corSelecionada, corProtecao);
                        idRodada = dataList[0].id;
                    }
                    else
                    {
                        losses++;
                        totalEntradas++;
                        await EnviarMensagemLoss();
                        emJogo = false;
                        return;
                    }
                }
            }
        }
    }

    #endregion

    #region Mensagens do Telegram

    public static async Task EnviarMensagemEntrada(string cor, string emoji, string protecao)
    {
        var dataList = await RetornaUltimosResultados();
        int ultimoNumero = dataList.Count > 0 ? dataList[0].roll : 12;
        string numeroFormatado = ultimoNumero.ToString();
        
        string mensagem = $"💰💵 <b>ENTRADA CONFIRMADA</b> 💵💰\n\n";
        mensagem += $"✅ <b>APOSTAR</b> {emoji} + {protecao}\n\n";
        mensagem += $"📌 <b>APÓS NÚMERO:</b> {numeroFormatado}\n\n";
        mensagem += $"\n👩‍💻 <a href='http://bit.ly/blazelinkEMG'><b>ABRIR JOGO</b></a>\n";
        mensagem += $"\n➡️ <a href='http://bit.ly/3kSqs24'>CLIQUE AQUI</a> E ABRA SUA CONTA!";
        
        await botClient!.SendTextMessageAsync(ChatId, mensagem, ParseMode.Html, disableWebPagePreview: true);
        Console.WriteLine($"📤 Entrada enviada: {cor} (Após número: {numeroFormatado})");
    }

    public static async Task EnviarMensagemWin(int cor, int tentativa)
    {
        string corNome = cor == 0 ? "BRANCO" : (cor == 1 ? "VERMELHO" : "PRETO");
        string emoji = cor == 0 ? "⚪" : (cor == 1 ? "🔴" : "⚫");
        
        string mensagem = $"✅✅✅ <b>WINN</b> ✅✅✅\n\n";
        mensagem += $"🎯 🎯 <b>ACERTOU A COR!</b>\n\n";
        mensagem += $"💵💰 14x <b>faça seu gerenciamento</b>\n\n";
        mensagem += $"📊 <b>ESTATÍSTICAS:</b>\n";
        mensagem += $"✅ Win: {wins}\n";
        mensagem += $"❌ Loss: {losses}\n";
        mensagem += $"⚪️ Branco: {whites}\n";
        mensagem += $"📊 Total: {totalEntradas}\n";
        mensagem += $"🎯 Win %: {GetWinPercentage():F1}%";
        
        await botClient!.SendTextMessageAsync(ChatId, mensagem, ParseMode.Html);
        Console.WriteLine($"✅ WIN! {corNome} (GALE {tentativa})");
    }

    public static async Task EnviarMensagemWhite()
    {
        string mensagem = $"✅⚪️ <b>WINN NA PROTEÇÃO</b> ⚪️✅\n\n";
        mensagem += $"🎯 🎯 <b>ACERTOU O BRANCO!</b>\n\n";
        mensagem += $"💵💰 14x <b>faça seu gerenciamento</b>\n\n";
        mensagem += $"📊 <b>ESTATÍSTICAS:</b>\n";
        mensagem += $"✅ Win: {wins}\n";
        mensagem += $"❌ Loss: {losses}\n";
        mensagem += $"⚪️ Branco: {whites}\n";
        mensagem += $"📊 Total: {totalEntradas}\n";
        mensagem += $"🎯 Win %: {GetWinPercentage():F1}%";
        
        await botClient!.SendTextMessageAsync(ChatId, mensagem, ParseMode.Html);
        Console.WriteLine($"✅ WIN na proteção (Branco)");
    }

    public static async Task EnviarMensagemGale(int gale, int cor, int protecao)
    {
        string corNome = cor == 0 ? "BRANCO" : (cor == 1 ? "VERMELHO" : "PRETO");
        string emoji = cor == 0 ? "⚪" : (cor == 1 ? "🔴" : "⚫");
        string protecaoNome = protecao == 0 ? "BRANCO" : (protecao == 1 ? "VERMELHO" : "PRETO");
        string emojiProtecao = protecao == 0 ? "⚪" : (protecao == 1 ? "🔴" : "⚫");
        
        string mensagem = $"⚠️ <b>GALE {gale}/3</b> ⚠️\n\n";
        mensagem += $"🎯 <b>APOSTAR</b> {emoji} {corNome}\n";
        mensagem += $"🛡️ <b>PROTEÇÃO</b> {emojiProtecao} {protecaoNome}\n\n";
        mensagem += $"💪 <b>Continue! Próxima rodada!</b>";
        
        await botClient!.SendTextMessageAsync(ChatId, mensagem, ParseMode.Html);
        Console.WriteLine($"⚠️ GALE {gale}/3");
    }

    public static async Task EnviarMensagemLoss()
    {
        string mensagem = $"❌❌❌ <b>LOSS</b> ❌❌❌\n\n";
        mensagem += $"⏰ <b>3 tentativas esgotadas sem acertar.</b>\n\n";
        mensagem += $"⚠️ <b>CUIDADO COM O GERENCIAMENTO!</b>\n\n";
        mensagem += $"📊 <b>ESTATÍSTICAS:</b>\n";
        mensagem += $"✅ Win: {wins}\n";
        mensagem += $"❌ Loss: {losses}\n";
        mensagem += $"⚪️ Branco: {whites}\n";
        mensagem += $"📊 Total: {totalEntradas}\n";
        mensagem += $"🎯 Win %: {GetWinPercentage():F1}%";
        
        await botClient!.SendTextMessageAsync(ChatId, mensagem, ParseMode.Html);
        Console.WriteLine($"❌ LOSS TOTAL!");
    }

    public static float GetWinPercentage()
    {
        if (totalEntradas == 0) return 0;
        return (float)(wins + whites) / totalEntradas * 100;
    }

    #endregion
}