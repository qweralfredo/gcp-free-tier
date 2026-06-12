using BriefappGuardian.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace BriefappGuardian.Api.Services;

/// <summary>
/// Coleta métricas de quota do GCP via Cloud Monitoring API (REST).
/// Usa as credenciais Application Default Credentials (ADC) montadas via volume Docker.
/// Para minimizar chamadas, agrega por 15 minutos e grava apenas um ponto por período.
/// </summary>
public sealed class GcpMetricsCollector
{
    private readonly HttpClient _http;
    private readonly IOptions<AppSettings> _settings;
    private readonly ILogger<GcpMetricsCollector> _logger;

    // Base URL da Cloud Monitoring v3 REST API
    private const string MonitoringBase = "https://monitoring.googleapis.com/v3";

    public GcpMetricsCollector(
        IHttpClientFactory httpFactory,
        IOptions<AppSettings> settings,
        ILogger<GcpMetricsCollector> logger)
    {
        _http = httpFactory.CreateClient("gcp");
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Coleta todas as métricas monitoradas e retorna lista de GcpMetric.
    /// Em caso de falha na API GCP, retorna lista vazia e loga o erro.
    /// </summary>
    public async Task<IReadOnlyList<GcpMetric>> CollectAsync(CancellationToken ct = default)
    {
        var projectId = _settings.Value.GcpProjectId;
        if (string.IsNullOrWhiteSpace(projectId))
        {
            _logger.LogWarning("GCP_PROJECT_ID não configurado — coleta ignorada.");
            return [];
        }

        var results = new List<GcpMetric>();
        var now = DateTime.UtcNow;
        var period = now.ToString("yyyy-MM");
        var today = now.ToString("yyyy-MM-dd");

        try
        {
            // Cloud Storage Egress (diário)
            var egressBytes = await QueryTimeSeriesAsync(
                projectId,
                "storage.googleapis.com/network/sent_bytes_count",
                now.Date, now, ct);
            if (egressBytes.HasValue)
                results.Add(new GcpMetric
                {
                    ServiceName = "storage",
                    MetricName = "egress_bytes",
                    Value = egressBytes.Value,
                    Unit = "bytes",
                    FreeLimit = 1_073_741_824,
                    Period = today,
                    CollectedAt = now
                });

            // Cloud Storage total armazenado (mensal)
            var storageBytes = await QueryTimeSeriesAsync(
                projectId,
                "storage.googleapis.com/storage/total_bytes",
                now.AddDays(-1), now, ct);
            if (storageBytes.HasValue)
                results.Add(new GcpMetric
                {
                    ServiceName = "storage",
                    MetricName = "storage_bytes",
                    Value = storageBytes.Value,
                    Unit = "bytes",
                    FreeLimit = 5_368_709_120,
                    Period = period,
                    CollectedAt = now
                });

            // Cloud Run requisições (mensal)
            var runRequests = await QueryTimeSeriesAsync(
                projectId,
                "run.googleapis.com/request_count",
                new DateTime(now.Year, now.Month, 1), now, ct);
            if (runRequests.HasValue)
                results.Add(new GcpMetric
                {
                    ServiceName = "run",
                    MetricName = "request_count",
                    Value = runRequests.Value,
                    Unit = "count",
                    FreeLimit = 2_000_000,
                    Period = period,
                    CollectedAt = now
                });

            // Compute Engine horas da VM e2-micro (mensal)
            var computeHours = await QueryTimeSeriesAsync(
                projectId,
                "compute.googleapis.com/instance/uptime",
                new DateTime(now.Year, now.Month, 1), now, ct);
            if (computeHours.HasValue)
                results.Add(new GcpMetric
                {
                    ServiceName = "compute",
                    MetricName = "instance_hours",
                    Value = computeHours.Value / 3600.0,   // segundos → horas
                    Unit = "hours",
                    FreeLimit = 730,
                    Period = period,
                    CollectedAt = now
                });

            _logger.LogInformation("Coleta GCP concluída: {Count} métricas coletadas.", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao coletar métricas GCP. Próxima tentativa em {Min} minutos.",
                _settings.Value.CollectionIntervalMinutes);
        }

        return results;
    }

    /// <summary>
    /// Consulta a série temporal de uma métrica GCP e retorna a soma do período.
    /// Retorna null se a série não tiver dados ou se a API retornar erro.
    /// </summary>
    private async Task<double?> QueryTimeSeriesAsync(
        string projectId,
        string metricType,
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct)
    {
        // Formatar timestamps no formato RFC3339
        var start = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var end = endTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var url = $"{MonitoringBase}/projects/{projectId}/timeSeries" +
                  $"?filter=metric.type%3D%22{Uri.EscapeDataString(metricType)}%22" +
                  $"&interval.startTime={Uri.EscapeDataString(start)}" +
                  $"&interval.endTime={Uri.EscapeDataString(end)}" +
                  $"&aggregation.alignmentPeriod=86400s" +
                  $"&aggregation.perSeriesAligner=ALIGN_SUM" +
                  $"&aggregation.crossSeriesReducer=REDUCE_SUM";

        try
        {
            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Cloud Monitoring API retornou {Status} para {Metric}: {Body}",
                    response.StatusCode, metricType, body[..Math.Min(body.Length, 200)]);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<MonitoringResponse>(ct);
            if (json?.TimeSeries == null || json.TimeSeries.Count == 0)
            {
                _logger.LogDebug("Sem dados para métrica {Metric} no período {Start} → {End}",
                    metricType, start, end);
                return null;
            }

            // Soma todos os pontos de todas as séries (ex: múltiplos buckets)
            double total = 0;
            foreach (var series in json.TimeSeries)
            {
                foreach (var point in series.Points ?? [])
                {
                    total += point.Value?.DoubleValue ?? point.Value?.Int64Value ?? 0;
                }
            }
            return total;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout ao consultar {Metric} — API GCP lenta.", metricType);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar {Metric}.", metricType);
            return null;
        }
    }

    // DTOs internos para desserialização da resposta da Cloud Monitoring API
    private sealed record MonitoringResponse(List<TimeSeries>? TimeSeries);
    private sealed record TimeSeries(List<DataPoint>? Points);
    private sealed record DataPoint(TimeInterval? Interval, TypedValue? Value);
    private sealed record TimeInterval(string? StartTime, string? EndTime);
    private sealed record TypedValue(double? DoubleValue, long? Int64Value);
}
