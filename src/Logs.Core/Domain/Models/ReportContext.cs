namespace Logs.Core.Domain.Models;

/// <summary>
/// Контекст для форматирования отчета с информацией о временном диапазоне
/// </summary>
public sealed class ReportContext
{
    /// <summary>
    /// Начальная дата фильтрации
    /// </summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>
    /// Конечная дата фильтрации
    /// </summary>
    public DateTimeOffset? To { get; init; }
}
