using Logs.Core.Application.Abstractions.Reporting;
using Logs.Core.Domain.Models;
using Logs.Core.Domain.Models.Stats;
using System.Text;

namespace Logs.Formatters.Adoc;

/// <summary>
/// Форматтер для вывода статистики в формате AsciiDoc
/// </summary>
public sealed class AdocReportFormatter : IReportFormatter
{
    /// <inheritdoc />
    public string Name => "adoc";

    /// <summary>
    /// Форматирует статистику в AsciiDoc
    /// </summary>
    public string Format(LogStats stats, ReportContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("== Общая информация");
        sb.AppendLine();
        sb.AppendLine("|===");
        sb.AppendLine("| Метрика | Значение");
        sb.AppendLine($"| Файл(-ы) | {string.Join(", ", stats.Files)}");
        sb.AppendLine($"| Количество запросов | {stats.TotalRequestsCount}");
        sb.AppendLine($"| Средний размер ответа | {stats.ResponseSizeInBytes.average}b");
        sb.AppendLine($"| 95p размера ответа | {stats.ResponseSizeInBytes.p95}b");
        sb.AppendLine("|===");
        sb.AppendLine();
        sb.AppendLine("== Коды ответа");
        sb.AppendLine("|===");
        sb.AppendLine("| Код | Количество");

        foreach (var (code, totalResponsesCount) in stats.ResponseCodes)
        {
            sb.AppendLine($"| {code} | {totalResponsesCount}");
        }

        sb.AppendLine("|===");
        return sb.ToString();
    }
}
