using BriefappGuardian.Core.Entities;
using FluentAssertions;
using Xunit;


namespace BriefappGuardian.Tests.Domain;

/// <summary>
/// Testes TDD para a entidade GcpMetric.
/// Red → Green → Refactor
/// </summary>
public sealed class GcpMetricTests
{
    // ── UsagePercent ─────────────────────────────────────────────────

    [Fact]
    public void UsagePercent_WhenValueIsZero_ShouldReturnZero()
    {
        var metric = MakeMetric(value: 0, freeLimit: 1_073_741_824);
        metric.UsagePercent.Should().Be(0);
    }

    [Fact]
    public void UsagePercent_WhenValueIsHalfOfLimit_ShouldReturn50()
    {
        var metric = MakeMetric(value: 536_870_912, freeLimit: 1_073_741_824);
        metric.UsagePercent.Should().BeApproximately(50.0, precision: 0.01);
    }

    [Fact]
    public void UsagePercent_WhenValueExceedsLimit_ShouldExceed100()
    {
        var metric = MakeMetric(value: 2_000_000, freeLimit: 1_000_000);
        metric.UsagePercent.Should().BeApproximately(200.0, precision: 0.01);
    }

    [Fact]
    public void UsagePercent_WhenFreeLimitIsZero_ShouldReturnZero_NotDivideByZero()
    {
        var metric = MakeMetric(value: 999, freeLimit: 0);
        metric.UsagePercent.Should().Be(0);
    }

    [Theory]
    [InlineData(750_000, 1_000_000, 75.0)]   // exatamente 75% — threshold Warning
    [InlineData(900_000, 1_000_000, 90.0)]   // exatamente 90% — threshold Critical
    [InlineData(980_000, 1_000_000, 98.0)]   // exatamente 98% — threshold Emergency
    public void UsagePercent_ShouldMatchExpectedThresholdValues(
        double value, double freeLimit, double expected)
    {
        var metric = MakeMetric(value, freeLimit);
        metric.UsagePercent.Should().BeApproximately(expected, precision: 0.001);
    }

    // ── Imutabilidade e inicialização ──────────────────────────────

    [Fact]
    public void GcpMetric_CollectedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var metric = MakeMetric(0, 1000);
        var after = DateTime.UtcNow.AddSeconds(1);

        metric.CollectedAt.Should().BeOnOrAfter(before);
        metric.CollectedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void GcpMetric_ShouldPreserveAllProperties()
    {
        var metric = new GcpMetric
        {
            ServiceName = "storage",
            MetricName = "egress_bytes",
            Value = 536_870_912,
            Unit = "bytes",
            FreeLimit = 1_073_741_824,
            Period = "2025-06"
        };

        metric.ServiceName.Should().Be("storage");
        metric.MetricName.Should().Be("egress_bytes");
        metric.Value.Should().Be(536_870_912);
        metric.Unit.Should().Be("bytes");
        metric.FreeLimit.Should().Be(1_073_741_824);
        metric.Period.Should().Be("2025-06");
    }

    // ── Helper ─────────────────────────────────────────────────────
    private static GcpMetric MakeMetric(double value, double freeLimit) => new()
    {
        ServiceName = "test-service",
        MetricName = "test-metric",
        Value = value,
        Unit = "bytes",
        FreeLimit = freeLimit,
        Period = "2025-06"
    };
}
