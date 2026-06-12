using BriefappGuardian.Api.Data;
using BriefappGuardian.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BriefappGuardian.Api.Workers;

/// <summary>
/// Worker background que executa a coleta de métricas GCP em intervalos regulares.
/// Intervalo padrão: 15 minutos (configurável via AppSettings.CollectionIntervalMinutes).
/// Após cada coleta, aciona o GuardrailEngine para avaliação de thresholds.
/// </summary>
public sealed class MetricsCollectorWorker : BackgroundService
{
    private readonly GcpMetricsCollector _collector;
    private readonly GuardrailEngine _guardrails;
    private readonly DuckDbContext _db;
    private readonly IOptions<AppSettings> _settings;
    private readonly ILogger<MetricsCollectorWorker> _logger;

    public MetricsCollectorWorker(
        GcpMetricsCollector collector,
        GuardrailEngine guardrails,
        DuckDbContext db,
        IOptions<AppSettings> settings,
        ILogger<MetricsCollectorWorker> logger)
    {
        _collector = collector;
        _guardrails = guardrails;
        _db = db;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricsCollectorWorker iniciado. Intervalo: {Min} minutos.",
            _settings.Value.CollectionIntervalMinutes);

        // Executa imediatamente na primeira vez, depois em intervalos
        while (!stoppingToken.IsCancellationRequested)
        {
            await CollectAndEvaluateAsync(stoppingToken);

            var interval = TimeSpan.FromMinutes(_settings.Value.CollectionIntervalMinutes);
            _logger.LogDebug("Próxima coleta em {Time}.", DateTime.UtcNow.Add(interval).ToString("HH:mm:ss"));
            await Task.Delay(interval, stoppingToken);
        }

        _logger.LogInformation("MetricsCollectorWorker encerrado.");
    }

    private async Task CollectAndEvaluateAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[{Time}] Iniciando coleta de métricas GCP...", DateTime.UtcNow.ToString("HH:mm:ss"));

            var metrics = await _collector.CollectAsync(ct);
            foreach (var metric in metrics)
                _db.UpsertMetric(metric);

            _logger.LogInformation("{Count} métricas gravadas no DuckDB.", metrics.Count);

            // Avaliar guardrails após cada coleta
            await _guardrails.EvaluateAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Encerramento normal — não logar como erro
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no ciclo de coleta. Próxima tentativa no próximo intervalo.");
        }
    }
}
