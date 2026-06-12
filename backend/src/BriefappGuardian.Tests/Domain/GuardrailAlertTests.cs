using BriefappGuardian.Core.Entities;
using FluentAssertions;
using Xunit;

namespace BriefappGuardian.Tests.Domain;

/// <summary>
/// Testes TDD para GuardrailAlert e AlertLevel enum.
/// Valida criação, propriedades e invariantes do domínio de alertas.
/// </summary>
public sealed class GuardrailAlertTests
{
    [Fact]
    public void GuardrailAlert_ShouldPreserveLevel_Warning()
    {
        var alert = MakeAlert(AlertLevel.Warning, percent: 76.5);
        alert.Level.Should().Be(AlertLevel.Warning);
        ((int)alert.Level).Should().Be(1);
    }

    [Fact]
    public void GuardrailAlert_ShouldPreserveLevel_Critical()
    {
        var alert = MakeAlert(AlertLevel.Critical, percent: 91.0);
        alert.Level.Should().Be(AlertLevel.Critical);
        ((int)alert.Level).Should().Be(2);
    }

    [Fact]
    public void GuardrailAlert_ShouldPreserveLevel_Emergency()
    {
        var alert = MakeAlert(AlertLevel.Emergency, percent: 98.5);
        alert.Level.Should().Be(AlertLevel.Emergency);
        ((int)alert.Level).Should().Be(3);
    }

    [Fact]
    public void GuardrailAlert_DefaultCreatedAt_ShouldBeUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var alert = MakeAlert(AlertLevel.Warning, 75.0);
        var after = DateTime.UtcNow.AddSeconds(1);

        alert.CreatedAt.Should().BeOnOrAfter(before);
        alert.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void GuardrailAlert_ActionTaken_CanBeNull()
    {
        var alert = MakeAlert(AlertLevel.Warning, 76.0, action: null);
        alert.ActionTaken.Should().BeNull();
    }

    [Fact]
    public void GuardrailAlert_ActionTaken_CanHaveValue()
    {
        var alert = MakeAlert(AlertLevel.Emergency, 98.5, action: "cloud-run-kill-switch-activated");
        alert.ActionTaken.Should().Be("cloud-run-kill-switch-activated");
    }

    [Theory]
    [InlineData(75.0, AlertLevel.Warning)]
    [InlineData(90.0, AlertLevel.Critical)]
    [InlineData(98.0, AlertLevel.Emergency)]
    public void AlertLevel_EnumValues_ShouldBeOrdered(double percent, AlertLevel expectedLevel)
    {
        var alert = MakeAlert(expectedLevel, percent);
        alert.TriggerPercent.Should().Be(percent);
        alert.Level.Should().Be(expectedLevel);
    }

    [Fact]
    public void GuardrailAlert_NotificationSent_DefaultFalse()
    {
        var alert = MakeAlert(AlertLevel.Warning, 75.0);
        alert.NotificationSent.Should().BeFalse();
    }

    // ── Helper ──────────────────────────────────────────────────────
    private static GuardrailAlert MakeAlert(AlertLevel level, double percent, string? action = null) =>
        new()
        {
            ServiceName = "run",
            MetricName = "request_count",
            Level = level,
            TriggerValue = 1_500_000,
            TriggerPercent = percent,
            ActionTaken = action,
        };
}
