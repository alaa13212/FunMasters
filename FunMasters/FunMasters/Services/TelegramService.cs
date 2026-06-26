using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FunMasters.Data;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class TelegramService(HttpClient httpClient, IConfiguration configuration)
{
    private readonly string _botToken = configuration["Telegram:BotToken"] ?? "";
    private readonly string _chatId = configuration["Telegram:ChatId"] ?? "";

    // Snake_case property names + no HTML-encoder escaping, so <b>…</b> reaches Telegram verbatim
    // and parse_mode is honored on both endpoints.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = null
    };

    public async Task SendMessageAsync(string text)
    {
        if (string.IsNullOrEmpty(_botToken) || string.IsNullOrEmpty(_chatId))
            return;

        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var body = JsonSerializer.Serialize(new { chat_id = _chatId, text, parse_mode = "HTML" }, JsonOptions);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        await httpClient.PostAsync(url, content);
    }

    public async Task SendPhotoAsync(string caption, Stream photoStream, string fileName, string contentType)
    {
        if (string.IsNullOrEmpty(_botToken) || string.IsNullOrEmpty(_chatId))
            return;

        var url = $"https://api.telegram.org/bot{_botToken}/sendPhoto";

        using var content = new MultipartFormDataContent
        {
            { new StringContent(_chatId), "chat_id" },
            { new StringContent("HTML"), "parse_mode" }
        };
        if (!string.IsNullOrWhiteSpace(caption))
            content.Add(new StringContent(caption), "caption");

        var streamContent = new StreamContent(photoStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "photo", fileName);

        await httpClient.PostAsync(url, content);
    }
}