using Logs.Core.Application.Abstractions.Reporting;
using Logs.Core.Domain.Models;
using Logs.Core.Domain.Models.Stats;
using System.Globalization;

namespace Logs.Formatters.Markdown;

/// <summary>
/// Форматтер для вывода статистики в формате Markdown
/// </summary>
public sealed class MarkdownReportFormatter : IReportFormatter
{
    /// <inheritdoc />
    public string Name => "markdown";

    /// <summary>
    /// Форматирует статистику в Markdown с таблицами и заголовками
    /// </summary>
    public string Format(LogStats stats, ReportContext context)
    {
        var fromStr = context.From?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "-";
        var toStr = context.To?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "-";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("#### Общая информация");
        sb.AppendLine();
        sb.AppendLine("|        Метрика        |     Значение |");
        sb.AppendLine("|:---------------------:|-------------:|");
        sb.AppendLine($"|       Файл(-ы)        | `{string.Join(", ", stats.Files)}` |");
        sb.AppendLine($"|    Начальная дата     |   {fromStr} |");
        sb.AppendLine($"|     Конечная дата     |   {toStr} |");
        sb.AppendLine($"|  Количество запросов  |         {stats.TotalRequestsCount} |");
        sb.AppendLine($"| Средний размер ответа |         {stats.ResponseSizeInBytes.average}b |");
        sb.AppendLine($"|  95p размера ответа   |         {stats.ResponseSizeInBytes.p95}b |");
        sb.AppendLine();

        sb.AppendLine("#### Запрашиваемые ресурсы");
        sb.AppendLine();
        sb.AppendLine("|     Ресурс      | Количество |");
        sb.AppendLine("|:---------------:|-----------:|");

        foreach (var (resource, totalRequestsCount) in stats.Resources)
        {
            sb.AppendLine($"|  `{resource}`  |      {totalRequestsCount} |");
        }

        sb.AppendLine();
        sb.AppendLine("#### Коды ответа");
        sb.AppendLine();
        sb.AppendLine("| Код |          Имя          | Количество |");
        sb.AppendLine("|:---:|:---------------------:|-----------:|");

        foreach (var (code, totalResponsesCount) in stats.ResponseCodes)
        {
            sb.AppendLine($"| {code} |          -           |       {totalResponsesCount} |");
        }

        return sb.ToString();
    }
}
