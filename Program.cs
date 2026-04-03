using System;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using System.IO;

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
        { "authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6MTE0MTE1MTMsImlzUmVmcmVzaFRva2VuIjpmYWxzZSwiYmxvY2tzIjpbXSwidXVpZCI6IjY2ZWQ1MDUwLTYyYTYtNGM1Ny05N2Y0LTc0NmY5MDhlYjE1OSIsImlhdCI6MTc3NTE4MjczNywiZXhwIjoxNzc1MjI1OTM3fQ.eTOssgzy5JhcB-bNeoJrn6bKs3jA5pweIzwYOyOvE-c" },
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

    #endregion

    #region Main

    static async Task Main(string[] args)
    {
        // Força os logs aparecerem no Railway
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        
        foreach (var header in Headers)
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        botClient = new TelegramBotClient(BotToken);
        
        Console.WriteLine("=========================================");
        Console.WriteLine("        BOT BLAZE INICIADO!              ");
        Console.WriteLine("=========================================");
        Console.WriteLine($"Chat ID: {ChatId}");
        Console.WriteLine("API: Blaze.bet.br (Autenticada)");
        Console.WriteLine("=========================================");
        Console.WriteLine("Bot iniciado com sucesso!");
        Console.WriteLine("Aguardando padrões da Blaze...");
        Console.WriteLine("");
        
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

    private static string CorNome(int cor)
    {
        return cor switch
        {
            0 => "⚪ BRANCO",
            1 => "🔴 VERMELHO",
            2 => "⚫ PRETO",
            _ => "❓ DESCONHECIDO"
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
                Console.WriteLine($"⚠️ API retornou: {response.StatusCode}");
                return new List<Data>();
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Data>>(responseBody) ?? new List<Data>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro na API: {ex.Message}");
            return new List<Data>();
        }
    }

    #endregion

    #region Lógica Principal

    public static async Task Start()
    {
        Console.WriteLine("Iniciando monitoramento...");
        await CheckEstrategias(GetListaDeEstrategias());
    }

    public static async Task CheckEstrategias(List<Estrategias> ListEstrategias)
    {
        int contador = 0;
        Console.WriteLine("Procurando padrões na Blaze...");
        
        while (true)
        {
            await Task.Delay(1500);
            var dataList = await RetornaUltimosResultados();

            if (dataList.Count < 4) 
            {
                Console.WriteLine("⚠️ Aguardando dados da API...");
                continue;
            }

            // Converte cores para nomes com emojis
            string cor1 = CorNome(dataList[3].color);
            string cor2 = CorNome(dataList[2].color);
            string cor3 = CorNome(dataList[1].color);
            string cor4 = CorNome(dataList[0].color);
            
            // Mostra a cada 10 segundos
            contador++;
            if (contador >= 6)
            {
                contador = 0;
                Console.WriteLine("");
                Console.WriteLine($"📊 [{DateTime.Now:HH:mm:ss}] ÚLTIMAS 4 CORES:");
                Console.WriteLine($"   ← {cor1}");
                Console.WriteLine($"   ← {cor2}");
                Console.WriteLine($"   ← {cor3}");
                Console.WriteLine($"   ← {cor4} (ÚLTIMA)");
                Console.WriteLine($"📈 Estatísticas: ✅Wins:{wins} | ❌Loss:{losses} | ⚪Branco:{whites} | 📊Total:{totalEntradas} | 🎯Taxa:{GetWinPercentage():F1}%");
                Console.WriteLine("🔍 Procurando padrões...");
                Console.WriteLine("-----------------------------------------");
            }

            foreach (var estrategia in ListEstrategias)
            {
                if (estrategia.comparacaoUm == dataList[0].color &&
                    estrategia.comparacaoDois == dataList[1].color &&
                    estrategia.comparacaoTres == dataList[2].color &&
                    estrategia.comparacaoQuatro == dataList[3].color)
                {
                    Console.WriteLine("");
                    Console.WriteLine("🎯🎯🎯 PADRÃO ENCONTRADO! 🎯🎯🎯");
                    Console.WriteLine($"📊 Sequência: {cor4} | {cor3} | {cor2} | {cor1}");
                    
                    string nomeCor = estrategia.color == 0 ? "BRANCO" : (estrategia.color == 1 ? "VERMELHO" : "PRETO");
                    string emojiCor = estrategia.color == 0 ? "⚪" : (estrategia.color == 1 ? "🔴" : "⚫");
                    string protecao = "⚪️";
                    
                    Console.WriteLine($"🎯 ENTRAR NA COR: {emojiCor} {nomeCor}");
                    Console.WriteLine("📤 Enviando mensagem para o Telegram...");
                    
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
                string resultadoNome = CorNome(resultado);
                
                Console.WriteLine($"📊 Resultado da rodada: {resultadoNome}");
                
                if (resultado == corSelecionada)
                {
                    wins++;
                    totalEntradas++;
                    Console.WriteLine($"✅ WIN! (GALE {tentativasAtuais})");
                    await EnviarMensagemWin(corSelecionada, tentativasAtuais);
                    emJogo = false;
                    return;
                }
                else if (resultado == corProtecao)
                {
                    whites++;
                    totalEntradas++;
                    Console.WriteLine($"✅ WIN NA PROTEÇÃO! (Branco)");
                    await EnviarMensagemWhite();
                    emJogo = false;
                    return;
                }
                else
                {
                    if (tentativasAtuais < 3)
                    {
                        tentativasAtuais++;
                        Console.WriteLine($"❌ LOSS! Indo para GALE {tentativasAtuais}/3");
                        await EnviarMensagemGale(tentativasAtuais, corSelecionada, corProtecao);
                        idRodada = dataList[0].id;
                    }
                    else
                    {
                        losses++;
                        totalEntradas++;
                        Console.WriteLine($"❌❌❌ LOSS TOTAL! 3 tentativas esgotadas.");
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
        Console.WriteLine($"✅ Mensagem de ENTRADA enviada para o Telegram!");
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
        Console.WriteLine($"✅ Mensagem de WIN enviada!");
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
        Console.WriteLine($"✅ Mensagem de WIN NA PROTEÇÃO enviada!");
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
        Console.WriteLine($"⚠️ Mensagem de GALE {gale}/3 enviada!");
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
        Console.WriteLine($"❌ Mensagem de LOSS enviada!");
    }

    public static float GetWinPercentage()
    {
        if (totalEntradas == 0) return 0;
        return (float)(wins + whites) / totalEntradas * 100;
    }

    #endregion
}