using Logs.Core.Domain.Models.Stats;
using System.Globalization;

namespace Logs.Core.Domain.Aggregation;

/// <summary>
/// Агрегатор статистики из записей лог-файлов
/// </summary>
public sealed class LogStatsAggregator : ILogStatsAggregator
{
    /// <inheritdoc />
    public LogStats Aggregate(IEnumerable<LogEntry> entries, IEnumerable<string> files)
    {
        var entryList = entries as IReadOnlyList<LogEntry> ?? [.. entries];
        var fileList = files as IReadOnlyList<string> ?? [.. files];

        var total = entryList.Count;

        var sizes = entryList
            .Select(e => (double)e.ResponseSizeBytes)
            .OrderBy(x => x)
            .ToArray();

        double avg = total == 0 ? 0 : Math.Round(sizes.Average(), 2, MidpointRounding.AwayFromZero);
        double max = sizes.Length == 0 ? 0 : sizes[^1];
        double p95 = sizes.Length == 0 ? 0 : Percentile(sizes, 0.95);

        var resources = entryList
            .GroupBy(e => e.Resource)
            .Select(g => (resource: g.Key, count: g.Count()))
            .OrderByDescending(t => t.count)
            .ThenBy(t => t.resource, StringComparer.Ordinal)
            .Take(10)
            .Select(t => (t.resource, t.count))
            .ToList();

        var codes = entryList
            .GroupBy(e => e.StatusCode)
            .Select(g => (code: g.Key, count: g.Count()))
            .OrderByDescending(t => t.count)
            .ThenBy(t => t.code)
            .ToList();

        var perDate = entryList
            .GroupBy(e => DateOnly.FromDateTime(e.TimestampUtc.Date))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var count = g.Count();
                var pct = total == 0 ? 0 : Math.Round(count * 100.0 / total, 2, MidpointRounding.AwayFromZero);
                var date = g.Key;
                var weekday = CultureInfo.GetCultureInfo("en-US").DateTimeFormat.GetDayName(date.ToDateTime(TimeOnly.MinValue).DayOfWeek);
                return (date, weekday, count, pct);
            })
            .ToList();

        var protocols = entryList
            .GroupBy(e => e.Protocol)
            .Select(g => (protocol: g.Key, count: g.Count()))
            .OrderByDescending(p => p.protocol.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(p => p.count)
            .ThenBy(p => p.protocol, StringComparer.Ordinal)
            .Select(p => p.protocol)
            .ToList();

        return new LogStats(fileList, total, (avg, max, p95), resources, codes, perDate, protocols);
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }

        var position = percentile * (sorted.Length - 1);
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);

        if (lowerIndex == upperIndex)
        {
            return sorted[lowerIndex];
        }

        var fraction = position - lowerIndex;
        return sorted[lowerIndex] + fraction * (sorted[upperIndex] - sorted[lowerIndex]);
    }
}
