using Logs.Core.Domain.Models;
using Logs.Core.Domain.Models.Stats;

namespace Logs.Core.Application.Abstractions.Reporting;

/// <summary>
/// Интерфейс для форматирования статистики в различные форматы вывода
/// </summary>
public interface IReportFormatter
{
    /// <summary>
    /// Имя формата
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Форматирует статистику в строку согласно формату
    /// </summary>
    string Format(LogStats stats, ReportContext context);
}
