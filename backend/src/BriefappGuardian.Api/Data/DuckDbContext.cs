using BriefappGuardian.Core.Entities;
using DuckDB.NET.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BriefappGuardian.Api.Data;

/// <summary>
/// Gerencia o banco DuckDB local (briefapp_cache.db).
/// Inicializa o schema, insere métricas e consulta dados para o dashboard.
/// Todas as operações são synchronous-safe para o modelo single-writer do DuckDB.
/// </summary>
public sealed class DuckDbContext : IDisposable
{
    private readonly DuckDBConnection _connection;
    private readonly ILogger<DuckDbContext> _logger;

    public DuckDbContext(IOptions<AppSettings> options, ILogger<DuckDbContext> logger)
    {
        _logger = logger;
        var dbPath = options.Value.DuckDbPath;
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connection = new DuckDBConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        _logger.LogInformation("Inicializando schema DuckDB...");
        ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS gcp_metrics (
                id          BIGINT PRIMARY KEY,
                service_name VARCHAR NOT NULL,
                metric_name  VARCHAR NOT NULL,
                value        DOUBLE NOT NULL,
                unit         VARCHAR NOT NULL,
                free_limit   DOUBLE NOT NULL,
                period       VARCHAR NOT NULL,
                collected_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE SEQUENCE IF NOT EXISTS seq_metrics START 1;

            CREATE TABLE IF NOT EXISTS guardrail_alerts (
                id                BIGINT PRIMARY KEY,
                service_name      VARCHAR NOT NULL,
                metric_name       VARCHAR NOT NULL,
                level             INTEGER NOT NULL,
                trigger_value     DOUBLE NOT NULL,
                trigger_percent   DOUBLE NOT NULL,
                action_taken      VARCHAR,
                notification_sent BOOLEAN NOT NULL DEFAULT FALSE,
                notes             VARCHAR,
                created_at        TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE SEQUENCE IF NOT EXISTS seq_alerts START 1;

            CREATE TABLE IF NOT EXISTS quota_config (
                id                BIGINT PRIMARY KEY,
                service_name      VARCHAR NOT NULL,
                metric_name       VARCHAR NOT NULL,
                display_name      VARCHAR NOT NULL,
                free_limit        DOUBLE NOT NULL,
                unit              VARCHAR NOT NULL,
                warning_percent   DOUBLE NOT NULL DEFAULT 75,
                critical_percent  DOUBLE NOT NULL DEFAULT 90,
                emergency_percent DOUBLE NOT NULL DEFAULT 98,
                kill_switch_enabled BOOLEAN NOT NULL DEFAULT TRUE,
                period_type       VARCHAR NOT NULL DEFAULT 'monthly',
                is_active         BOOLEAN NOT NULL DEFAULT TRUE,
                updated_at        TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE SEQUENCE IF NOT EXISTS seq_config START 1;
        ");

        SeedDefaultQuotaConfig();
    }

    private void SeedDefaultQuotaConfig()
    {
        var count = ExecuteScalar<long>("SELECT COUNT(*) FROM quota_config");
        if (count > 0) return;

        _logger.LogInformation("Inserindo configurações padrão de quota GCP Free Tier...");

        // Limites GCP Always Free (us-central1, us-west1, us-east1)
        var defaults = new[]
        {
            ("compute", "instance_hours", "Compute Engine e2-micro", 730.0, "hours", "monthly"),
            ("storage", "egress_bytes", "Cloud Storage Egress", 1_073_741_824.0, "bytes", "daily"),
            ("storage", "storage_bytes", "Cloud Storage Total", 5_368_709_120.0, "bytes", "monthly"),
            ("storage", "class_a_ops", "Cloud Storage Operações Classe A (Escrita)", 20_000.0, "count", "daily"),
            ("storage", "class_b_ops", "Cloud Storage Operações Classe B (Leitura)", 50_000.0, "count", "daily"),
            ("run", "request_count", "Cloud Run Requisições", 2_000_000.0, "count", "monthly"),
            ("run", "cpu_seconds", "Cloud Run CPU", 180_000.0, "cpu-seconds", "monthly"),
            ("run", "memory_gib_seconds", "Cloud Run Memória", 360_000.0, "gib-seconds", "monthly"),
            ("functions", "invocations", "Cloud Functions Invocações", 2_000_000.0, "count", "monthly"),
            ("pubsub", "message_bytes", "Pub/Sub Mensagens", 10_737_418_240.0, "bytes", "monthly"),
            ("monitoring", "api_calls", "Cloud Monitoring API Calls", 1_000_000_000.0, "count", "monthly"),
        };

        foreach (var (svc, metric, display, limit, unit, period) in defaults)
        {
            ExecuteNonQuery(@$"
                INSERT INTO quota_config 
                    (id, service_name, metric_name, display_name, free_limit, unit, period_type)
                VALUES
                    (nextval('seq_config'), '{svc}', '{metric}', '{display}', {limit}, '{unit}', '{period}')
            ");
        }
    }

    public void UpsertMetric(GcpMetric metric)
    {
        // DuckDB não tem UPSERT nativo — delete + insert para o mesmo período/serviço/métrica
        ExecuteNonQuery(@$"
            DELETE FROM gcp_metrics
            WHERE service_name = '{metric.ServiceName}'
              AND metric_name  = '{metric.MetricName}'
              AND period       = '{metric.Period}';

            INSERT INTO gcp_metrics
                (id, service_name, metric_name, value, unit, free_limit, period, collected_at)
            VALUES
                (nextval('seq_metrics'),
                 '{metric.ServiceName}',
                 '{metric.MetricName}',
                 {metric.Value},
                 '{metric.Unit}',
                 {metric.FreeLimit},
                 '{metric.Period}',
                 '{metric.CollectedAt:yyyy-MM-dd HH:mm:ss}');
        ");
    }

    public void InsertAlert(GuardrailAlert alert)
    {
        ExecuteNonQuery(@$"
            INSERT INTO guardrail_alerts
                (id, service_name, metric_name, level, trigger_value, trigger_percent,
                 action_taken, notification_sent, notes, created_at)
            VALUES
                (nextval('seq_alerts'),
                 '{alert.ServiceName}',
                 '{alert.MetricName}',
                 {(int)alert.Level},
                 {alert.TriggerValue},
                 {alert.TriggerPercent},
                 {(alert.ActionTaken != null ? $"'{alert.ActionTaken}'" : "NULL")},
                 {alert.NotificationSent.ToString().ToLower()},
                 {(alert.Notes != null ? $"'{alert.Notes}'" : "NULL")},
                 '{alert.CreatedAt:yyyy-MM-dd HH:mm:ss}');
        ");
    }

    public IReadOnlyList<(string service, string metric, double value, double freeLimit, string unit, string period, DateTime collectedAt)>
        GetLatestMetrics()
    {
        var results = new List<(string, string, double, double, string, string, DateTime)>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT DISTINCT ON (service_name, metric_name)
                service_name, metric_name, value, free_limit, unit, period, collected_at
            FROM gcp_metrics
            ORDER BY service_name, metric_name, collected_at DESC;
        ";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add((
                reader.GetString(0),
                reader.GetString(1),
                reader.GetDouble(2),
                reader.GetDouble(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetDateTime(6)
            ));
        }
        return results;
    }

    public IReadOnlyList<(long id, string service, string metric, int level, double percent, string? action, DateTime createdAt)>
        GetRecentAlerts(int limit = 20)
    {
        var results = new List<(long, string, string, int, double, string?, DateTime)>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $@"
            SELECT id, service_name, metric_name, level, trigger_percent, action_taken, created_at
            FROM guardrail_alerts
            ORDER BY created_at DESC
            LIMIT {limit};
        ";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add((
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetDouble(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetDateTime(6)
            ));
        }
        return results;
    }

    public IReadOnlyList<QuotaConfig> GetActiveConfigs()
    {
        var results = new List<QuotaConfig>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT id, service_name, metric_name, display_name, free_limit, unit,
                   warning_percent, critical_percent, emergency_percent,
                   kill_switch_enabled, period_type, is_active, updated_at
            FROM quota_config
            WHERE is_active = TRUE;
        ";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new QuotaConfig
            {
                Id = reader.GetInt64(0),
                ServiceName = reader.GetString(1),
                MetricName = reader.GetString(2),
                DisplayName = reader.GetString(3),
                FreeLimit = reader.GetDouble(4),
                Unit = reader.GetString(5),
                WarningPercent = reader.GetDouble(6),
                CriticalPercent = reader.GetDouble(7),
                EmergencyPercent = reader.GetDouble(8),
                KillSwitchEnabled = reader.GetBoolean(9),
                PeriodType = reader.GetString(10),
                IsActive = reader.GetBoolean(11),
                UpdatedAt = reader.GetDateTime(12)
            });
        }
        return results;
    }

    private void ExecuteNonQuery(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private T ExecuteScalar<T>(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        return (T)cmd.ExecuteScalar()!;
    }

    public void Dispose() => _connection.Dispose();
}
