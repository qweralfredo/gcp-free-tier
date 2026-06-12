namespace BriefappGuardian.Api;

/// <summary>
/// Configurações da aplicação lidas via appsettings.json / variáveis de ambiente.
/// </summary>
public sealed class AppSettings
{
    public const string Section = "BriefappGuardian";

    /// <summary>Caminho local do arquivo DuckDB (ex: /app/data/briefapp_cache.db)</summary>
    public string DuckDbPath { get; set; } = "/app/data/briefapp_cache.db";

    /// <summary>ID do projeto GCP a ser monitorado</summary>
    public string GcpProjectId { get; set; } = string.Empty;

    /// <summary>Nome do bucket GCS para sync de Parquet</summary>
    public string GcsBucketName { get; set; } = "briefapp-guardian-metrics";

    /// <summary>Token do bot Telegram para alertas</summary>
    public string TelegramBotToken { get; set; } = string.Empty;

    /// <summary>Chat ID do Telegram para alertas</summary>
    public string TelegramChatId { get; set; } = string.Empty;

    /// <summary>Intervalo de coleta de métricas em minutos (default: 15)</summary>
    public int CollectionIntervalMinutes { get; set; } = 15;

    /// <summary>Habilitar sync com GCS (requer Service Account configurado)</summary>
    public bool GcsSyncEnabled { get; set; } = true;
}
