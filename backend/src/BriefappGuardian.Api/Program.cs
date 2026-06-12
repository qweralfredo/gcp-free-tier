using BriefappGuardian.Api;
using BriefappGuardian.Api.Data;
using BriefappGuardian.Api.Endpoints;
using BriefappGuardian.Api.Services;
using BriefappGuardian.Api.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

// ─── Serilog: logging estruturado com rotação diária ──────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception}")
    .WriteTo.File(
        path: "/var/log/briefapp/api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Iniciando BriefappGuardian API...");

    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog como provider de logging ─────────────────────────────────────
    builder.Host.UseSerilog();

    // ─── Configurações tipadas ─────────────────────────────────────────────────
    builder.Services.Configure<AppSettings>(
        builder.Configuration.GetSection(AppSettings.Section));

    // Sobrescreve com variáveis de ambiente (convenção: BriefappGuardian__DuckDbPath)
    builder.Services.Configure<AppSettings>(opts =>
    {
        var env = builder.Configuration;
        opts.DuckDbPath = env["DUCKDB_PATH"] ?? opts.DuckDbPath;
        opts.GcpProjectId = env["GCP_PROJECT_ID"] ?? opts.GcpProjectId;
        opts.GcsBucketName = env["GCS_BUCKET_NAME"] ?? opts.GcsBucketName;
        opts.TelegramBotToken = env["TELEGRAM_BOT_TOKEN"] ?? opts.TelegramBotToken;
        opts.TelegramChatId = env["TELEGRAM_CHAT_ID"] ?? opts.TelegramChatId;
    });

    // ─── HttpClients ───────────────────────────────────────────────────────────
    builder.Services.AddHttpClient("gcp", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        // O token OAuth é gerenciado pela Application Default Credentials
        // Injetado via Google.Auth em uma versão futura; por agora usa ADC no ambiente
    });

    builder.Services.AddHttpClient("telegram", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    // ─── Serviços de domínio ───────────────────────────────────────────────────
    builder.Services.AddSingleton<DuckDbContext>();     // singleton = single-writer DuckDB
    builder.Services.AddSingleton<GcpMetricsCollector>();
    builder.Services.AddSingleton<TelegramNotifier>();
    builder.Services.AddSingleton<GuardrailEngine>();

    // ─── Background worker de coleta ───────────────────────────────────────────
    builder.Services.AddHostedService<MetricsCollectorWorker>();

    // ─── ASP.NET ───────────────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opts =>
    {
        opts.SwaggerDoc("v1", new() { Title = "BriefappGuardian API", Version = "v1" });
    });

    // CORS — permite chamadas do container PHP na mesma rede Docker
    builder.Services.AddCors(opts =>
    {
        opts.AddPolicy("briefapp-net", policy =>
            policy.WithOrigins("http://localhost", "http://briefapp-php", "http://briefapp-nginx")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    var app = builder.Build();

    // ─── Middleware pipeline ───────────────────────────────────────────────────
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondido {StatusCode} em {Elapsed:0.0}ms";
    });

    app.UseCors("briefapp-net");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // ─── Endpoints ─────────────────────────────────────────────────────────────
    app.MapDashboardEndpoints();

    Log.Information("BriefappGuardian API pronta. Escutando em {Urls}", app.Urls);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha fatal ao inicializar BriefappGuardian API.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
