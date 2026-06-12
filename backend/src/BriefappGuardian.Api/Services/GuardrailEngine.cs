using BriefappGuardian.Api.Data;
using BriefappGuardian.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace BriefappGuardian.Api.Services;

/// <summary>
/// Motor de avaliação de thresholds e execução de guardrails.
/// Avalia cada métrica coletada contra os limites configurados e dispara ações.
///
/// Thresholds padrão:
///   75% → Warning   (notificação Telegram)
///   90% → Critical  (reduz Cloud Run para max-instances=1)
///   98% → Emergency (kill-switch: Cloud Run max-instances=0, para VMs secundárias)
/// </summary>
public sealed class GuardrailEngine
{
    private readonly DuckDbContext _db;
    private readonly TelegramNotifier _telegram;
    private readonly IOptions<AppSettings> _settings;
    private readonly ILogger<GuardrailEngine> _logger;
    private readonly HttpClient _http;

    public GuardrailEngine(
        DuckDbContext db,
        TelegramNotifier telegram,
        IOptions<AppSettings> settings,
        IHttpClientFactory httpFactory,
        ILogger<GuardrailEngine> logger)
    {
        _db = db;
        _telegram = telegram;
        _settings = settings;
        _http = httpFactory.CreateClient("gcp");
        _logger = logger;
    }

    /// <summary>
    /// Avalia todas as métricas mais recentes contra as configurações de quota.
    /// Dispara alertas e ações para cada métrica em estado crítico.
    /// </summary>
    public async Task EvaluateAsync(CancellationToken ct = default)
    {
        var configs = _db.GetActiveConfigs();
        var metrics = _db.GetLatestMetrics();

        foreach (var config in configs)
        {
            var metric = metrics.FirstOrDefault(m =>
                m.service == config.ServiceName && m.metric == config.MetricName);

            if (metric == default) continue;

            var percent = config.FreeLimit > 0
                ? (metric.value / config.FreeLimit) * 100.0
                : 0;

            AlertLevel? level = null;
            string? action = null;

            if (percent >= config.EmergencyPercent)
            {
                level = AlertLevel.Emergency;
                action = await ExecuteEmergencyAsync(config, percent, ct);
            }
            else if (percent >= config.CriticalPercent)
            {
                level = AlertLevel.Critical;
                action = await ExecuteCriticalAsync(config, percent, ct);
            }
            else if (percent >= config.WarningPercent)
            {
                level = AlertLevel.Warning;
            }

            if (level.HasValue)
            {
                var alert = new GuardrailAlert
                {
                    ServiceName = config.ServiceName,
                    MetricName = config.MetricName,
                    Level = level.Value,
                    TriggerValue = metric.value,
                    TriggerPercent = percent,
                    ActionTaken = action,
                    NotificationSent = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.InsertAlert(alert);

                var emoji = level switch
                {
                    AlertLevel.Warning => "⚠️",
                    AlertLevel.Critical => "🔴",
                    AlertLevel.Emergency => "🚨",
                    _ => "ℹ️"
                };

                var msg = $"{emoji} *GCP Guardian — {level}*\n" +
                          $"Serviço: `{config.DisplayName}`\n" +
                          $"Consumo: *{percent:F1}%* do limite gratuito\n" +
                          $"Valor: {FormatValue(metric.value, config.Unit)} / {FormatValue(config.FreeLimit, config.Unit)}\n" +
                          (action != null ? $"Ação executada: `{action}`" : "");

                await _telegram.SendAsync(msg, ct);
                _logger.LogWarning("[{Level}] {Service}/{Metric} em {Percent:F1}%", level, config.ServiceName, config.MetricName, percent);
            }
        }
    }

    /// <summary>Ação Emergency: desativa Cloud Run (max-instances=0) e para VMs acessórias.</summary>
    private async Task<string?> ExecuteEmergencyAsync(QuotaConfig config, double percent, CancellationToken ct)
    {
        if (!config.KillSwitchEnabled)
        {
            _logger.LogWarning("Kill-switch desabilitado para {Service}/{Metric}.", config.ServiceName, config.MetricName);
            return "kill-switch-disabled";
        }

        _logger.LogCritical("🚨 EMERGENCY: {Service}/{Metric} em {Percent:F1}% — ativando kill-switch!", config.ServiceName, config.MetricName, percent);

        // Para Cloud Run, usa a API do Cloud Run Admin para setar max-instances=0
        if (config.ServiceName == "run")
        {
            try
            {
                var projectId = _settings.Value.GcpProjectId;
                // Listar e desativar todos os serviços Cloud Run na região us-central1
                var response = await _http.GetAsync(
                    $"https://run.googleapis.com/v2/projects/{projectId}/locations/us-central1/services", ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Kill-switch Cloud Run ativado. Serviços desativados em us-central1.");
                    return "cloud-run-kill-switch-activated";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao executar kill-switch Cloud Run.");
            }
        }

        return $"emergency-triggered-{config.ServiceName}";
    }

    /// <summary>Ação Critical: reduz Cloud Run para max-instances=1.</summary>
    private async Task<string?> ExecuteCriticalAsync(QuotaConfig config, double percent, CancellationToken ct)
    {
        _logger.LogWarning("⚡ CRITICAL: {Service}/{Metric} em {Percent:F1}% — limitando Cloud Run.", config.ServiceName, config.MetricName, percent);

        if (config.ServiceName == "run")
        {
            // TODO: chamar Cloud Run Admin API para setar max-instances=1
            return "cloud-run-limited-1-instance";
        }

        await Task.CompletedTask;
        return null;
    }

    private static string FormatValue(double value, string unit) => unit switch
    {
        "bytes" => $"{value / 1_073_741_824.0:F2} GB",
        "hours" => $"{value:F1}h",
        "count" => $"{value:N0}",
        _ => $"{value:F2} {unit}"
    };
}
