namespace FunMasters.Services;

public class TelegramService(HttpClient httpClient, IConfiguration configuration)
{
    private readonly string _botToken = configuration["Telegram:BotToken"] ?? "";
    private readonly string _chatId = configuration["Telegram:ChatId"] ?? "";

    public async Task SendMessageAsync(string text)
    {
        if (string.IsNullOrEmpty(_botToken) || string.IsNullOrEmpty(_chatId))
            return;

        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        await httpClient.PostAsJsonAsync(url, new { chat_id = _chatId, text, parse_mode = "HTML" });
    }
}
