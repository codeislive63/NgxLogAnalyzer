using FluentAssertions;
using Logs.Core.Domain.Aggregation;
using Logs.Core.Domain.Models.Stats;

namespace Logs.Tests.Domain.Aggregation;

/// <summary>
/// Тесты для агрегатора статистики
/// </summary>
public class LogStatsAggregatorTests
{
    private readonly LogStatsAggregator _aggregator = new();

    /// <summary>
    /// Проверяет корректный расчет всех метрик: количество запросов, размеры ответов, топ ресурсов, коды ответов, распределение по датам и уникальные протоколы
    /// </summary>
    [Fact]
    public void Aggregate_ShouldCalculateMetrics()
    {
        var entries = new List<LogEntry>
        {
            new(DateTimeOffset.Parse("2024-03-01T10:00:00Z"), "/a", "HTTP/1.1", 200, 100),
            new(DateTimeOffset.Parse("2024-03-01T11:00:00Z"), "/a", "HTTP/1.1", 200, 300),
            new(DateTimeOffset.Parse("2024-03-02T09:00:00Z"), "/b", "grpc", 404, 500)
        };

        var stats = _aggregator.Aggregate(entries, ["file.log"]);

        stats.TotalRequestsCount.Should().Be(3);
        stats.ResponseSizeInBytes.average.Should().BeApproximately(300, 0.01);
        stats.ResponseSizeInBytes.max.Should().Be(500);
        stats.ResponseSizeInBytes.p95.Should().BeApproximately(480, 0.01);

        stats.Resources.Should().ContainEquivalentOf(("/a", 2));
        stats.Resources.Should().ContainEquivalentOf(("/b", 1));

        stats.ResponseCodes.Should().ContainEquivalentOf((200, 2));
        stats.ResponseCodes.Should().ContainEquivalentOf((404, 1));

        stats.RequestsPerDate.Should().HaveCount(2);
        stats.UniqueProtocols.Should().Contain(["HTTP/1.1", "grpc"]);
    }

    /// <summary>
    /// Проверяет корректную обработку пустого списка записей
    /// </summary>
    [Fact]
    public void Aggregate_ShouldHandleEmptyInput()
    {
        var stats = _aggregator.Aggregate([], []);

        stats.TotalRequestsCount.Should().Be(0);
        stats.ResponseSizeInBytes.average.Should().Be(0);
        stats.ResponseSizeInBytes.max.Should().Be(0);
        stats.ResponseSizeInBytes.p95.Should().Be(0);
        stats.Resources.Should().BeEmpty();
        stats.ResponseCodes.Should().BeEmpty();
        stats.RequestsPerDate.Should().BeEmpty();
        stats.UniqueProtocols.Should().BeEmpty();
    }
}
