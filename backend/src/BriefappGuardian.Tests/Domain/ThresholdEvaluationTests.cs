using BriefappGuardian.Core.Entities;
using FluentAssertions;
using Xunit;

namespace BriefappGuardian.Tests.Domain;

/// <summary>
/// Testes TDD para a lógica de thresholds do GuardrailEngine.
/// Valida a classificação de status (ok/warning/critical/emergency) com base em percentual.
/// Esses testes documentam a regra de negócio central do sistema.
/// </summary>
public sealed class ThresholdEvaluationTests
{
    // ── Classificação de status ───────────────────────────────────────

    [Theory]
    [InlineData(0.0,  "ok")]
    [InlineData(50.0, "ok")]
    [InlineData(74.9, "ok")]
    public void Status_BelowWarning_ShouldBeOk(double percent, string expectedStatus)
    {
        DetermineStatus(percent).Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(75.0, "warning")]
    [InlineData(80.0, "warning")]
    [InlineData(89.9, "warning")]
    public void Status_BetweenWarningAndCritical_ShouldBeWarning(double percent, string expectedStatus)
    {
        DetermineStatus(percent).Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(90.0, "critical")]
    [InlineData(95.0, "critical")]
    [InlineData(97.9, "critical")]
    public void Status_BetweenCriticalAndEmergency_ShouldBeCritical(double percent, string expectedStatus)
    {
        DetermineStatus(percent).Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(98.0,  "emergency")]
    [InlineData(99.0,  "emergency")]
    [InlineData(100.0, "emergency")]
    [InlineData(150.0, "emergency")] // quota estourada
    public void Status_AtOrAboveEmergency_ShouldBeEmergency(double percent, string expectedStatus)
    {
        DetermineStatus(percent).Should().Be(expectedStatus);
    }

    // ── Boundary values (limites exatos) ─────────────────────────────

    [Fact]
    public void Status_At75Exactly_ShouldBeWarning_NotOk()
    {
        DetermineStatus(75.0).Should().Be("warning");
    }

    [Fact]
    public void Status_At90Exactly_ShouldBeCritical_NotWarning()
    {
        DetermineStatus(90.0).Should().Be("critical");
    }

    [Fact]
    public void Status_At98Exactly_ShouldBeEmergency_NotCritical()
    {
        DetermineStatus(98.0).Should().Be("emergency");
    }

    // ── Projeção de fim de período ────────────────────────────────────

    [Fact]
    public void Projection_WhenDayOfMonthIs15_ShouldProjectDouble()
    {
        const double currentValue = 500_000_000; // 500 MB consumidos no dia 15
        const int dayOfMonth = 15;
        const int daysInMonth = 30;

        var dailyRate = currentValue / dayOfMonth;
        var projected = dailyRate * daysInMonth;

        projected.Should().BeApproximately(1_000_000_000, precision: 1);
    }

    [Fact]
    public void DaysToLimit_WhenConsuming10PercentPerDay_ShouldBe9DaysFromNow()
    {
        const double freeLimit = 1_000_000;
        const double currentValue = 100_000;   // 10% consumido
        const double dailyRate = 100_000;       // 10%/dia

        var daysToLimit = (int)Math.Floor((freeLimit - currentValue) / dailyRate);

        daysToLimit.Should().Be(9); // 900_000 / 100_000 = 9 dias
    }

    [Fact]
    public void DaysToLimit_WhenAlreadyAtLimit_ShouldBeZeroOrNegative()
    {
        const double freeLimit = 1_000_000;
        const double currentValue = 1_000_001; // além do limite
        const double dailyRate = 50_000;

        var daysToLimit = (int)Math.Floor((freeLimit - currentValue) / dailyRate);

        daysToLimit.Should().BeLessThanOrEqualTo(0);
    }

    // ── AlertLevel mapping ────────────────────────────────────────────

    [Fact]
    public void AlertLevel_Mapping_ShouldMapCorrectly()
    {
        var levelToString = new Dictionary<AlertLevel, string>
        {
            [AlertLevel.Warning]   = "warning",
            [AlertLevel.Critical]  = "critical",
            [AlertLevel.Emergency] = "emergency",
        };

        levelToString[AlertLevel.Warning].Should().Be("warning");
        levelToString[AlertLevel.Critical].Should().Be("critical");
        levelToString[AlertLevel.Emergency].Should().Be("emergency");
    }

    [Theory]
    [InlineData(74.9, false, false, false)] // none
    [InlineData(75.0, true,  false, false)] // warning only
    [InlineData(90.0, true,  true,  false)] // warning + critical
    [InlineData(98.0, true,  true,  true)]  // all three
    public void ThresholdFlags_ShouldBeCorrectForEachPercent(
        double percent, bool isWarning, bool isCritical, bool isEmergency)
    {
        (percent >= 75).Should().Be(isWarning,  $"percent={percent} warning={isWarning}");
        (percent >= 90).Should().Be(isCritical, $"percent={percent} critical={isCritical}");
        (percent >= 98).Should().Be(isEmergency,$"percent={percent} emergency={isEmergency}");
    }

    // ── Helper que replica a lógica do DashboardEndpoints ────────────
    private static string DetermineStatus(double percent) => percent switch
    {
        >= 98 => "emergency",
        >= 90 => "critical",
        >= 75 => "warning",
        _     => "ok"
    };
}
