using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace BriefappGuardian.Api.Services;

/// <summary>
/// Envia notificações via Telegram Bot API.
/// Suporta mensagens em MarkdownV2. Falhas são silenciosas (log only) para não bloquear o fluxo.
/// </summary>
public sealed class TelegramNotifier
{
    private readonly HttpClient _http;
    private readonly IOptions<AppSettings> _settings;
    private readonly ILogger<TelegramNotifier> _logger;

    private const string TelegramBase = "https://api.telegram.org";

    public TelegramNotifier(
        IHttpClientFactory httpFactory,
        IOptions<AppSettings> settings,
        ILogger<TelegramNotifier> logger)
    {
        _http = httpFactory.CreateClient("telegram");
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Envia uma mensagem para o chat/grupo configurado.
    /// Não lança exceção — falha silenciosa com log.
    /// </summary>
    public async Task SendAsync(string message, CancellationToken ct = default)
    {
        var token = _settings.Value.TelegramBotToken;
        var chatId = _settings.Value.TelegramChatId;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
        {
            _logger.LogDebug("Telegram não configurado — notificação ignorada.");
            return;
        }

        try
        {
            var payload = new
            {
                chat_id = chatId,
                text = message,
                parse_mode = "Markdown",
                disable_web_page_preview = true
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{TelegramBase}/bot{token}/sendMessage";
            var response = await _http.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Telegram retornou {Status}: {Body}", response.StatusCode, body[..Math.Min(body.Length, 200)]);
            }
            else
            {
                _logger.LogInformation("Notificação Telegram enviada com sucesso.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar notificação Telegram.");
        }
    }
}
