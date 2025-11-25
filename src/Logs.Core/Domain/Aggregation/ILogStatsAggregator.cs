using Logs.Core.Domain.Models.Stats;

namespace Logs.Core.Domain.Aggregation;

/// <summary>
/// Интерфейс для агрегации статистики из записей лог-файлов
/// </summary>
public interface ILogStatsAggregator
{
    /// <summary>
    /// Агрегирует статистику из списка записей лог-файлов
    /// </summary>
    LogStats Aggregate(IEnumerable<LogEntry> entries, IEnumerable<string> files);
}
