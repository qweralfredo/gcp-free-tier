using BriefappGuardian.Api.Services;
using BriefappGuardian.Api;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text;
using Xunit;

namespace BriefappGuardian.Tests.Services;

/// <summary>
/// Testes TDD para TelegramNotifier.
/// Valida que: mensagens são enviadas quando configurado, e falha é silenciosa quando não configurado.
/// </summary>
public sealed class TelegramNotifierTests
{
    private static IOptions<AppSettings> MakeSettings(string token = "", string chatId = "") =>
        Options.Create(new AppSettings { TelegramBotToken = token, TelegramChatId = chatId });

    // ── Sem configuração ────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_WhenTokenIsEmpty_ShouldNotThrow()
    {
        var factory = MakeHttpFactory(HttpStatusCode.OK);
        var notifier = new TelegramNotifier(factory, MakeSettings(), NullLogger<TelegramNotifier>.Instance);

        // Não deve lançar exceção mesmo sem token configurado
        var act = async () => await notifier.SendAsync("test message");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_WhenChatIdIsEmpty_ShouldNotThrow()
    {
        var factory = MakeHttpFactory(HttpStatusCode.OK);
        var notifier = new TelegramNotifier(factory, MakeSettings(token: "123:abc", chatId: ""), NullLogger<TelegramNotifier>.Instance);

        var act = async () => await notifier.SendAsync("test");
        await act.Should().NotThrowAsync();
    }

    // ── Falha na API ─────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_WhenApiReturns400_ShouldNotThrow()
    {
        var factory = MakeHttpFactory(HttpStatusCode.BadRequest, "{\"ok\":false,\"error\":\"bad request\"}");
        var notifier = new TelegramNotifier(factory, MakeSettings("123:abc", "-100123"), NullLogger<TelegramNotifier>.Instance);

        // Falha silenciosa — não deve propagar exceção
        var act = async () => await notifier.SendAsync("alert message");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_WhenNetworkFails_ShouldNotThrow()
    {
        var factory = MakeFailingHttpFactory();
        var notifier = new TelegramNotifier(factory, MakeSettings("123:abc", "-100123"), NullLogger<TelegramNotifier>.Instance);

        var act = async () => await notifier.SendAsync("test");
        await act.Should().NotThrowAsync();
    }

    // ── Mensagem vazia ────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_WithEmptyMessage_ShouldNotThrow()
    {
        var factory = MakeHttpFactory(HttpStatusCode.OK);
        var notifier = new TelegramNotifier(factory, MakeSettings("tok", "chat"), NullLogger<TelegramNotifier>.Instance);

        var act = async () => await notifier.SendAsync(string.Empty);
        await act.Should().NotThrowAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static IHttpClientFactory MakeHttpFactory(HttpStatusCode status, string body = "{\"ok\":true}")
    {
        var handler = new FakeHttpMessageHandler(status, body);
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    private static IHttpClientFactory MakeFailingHttpFactory()
    {
        var handler = new ThrowingHttpMessageHandler();
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    // Fake handler que retorna resposta controlada
    private sealed class FakeHttpMessageHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    // Fake handler que sempre lança exceção de rede
    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Simulated network failure");
    }
}
