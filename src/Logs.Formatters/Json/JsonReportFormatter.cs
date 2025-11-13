using Logs.Core.Application.Abstractions.Reporting;
using Logs.Core.Domain.Models;
using Logs.Core.Domain.Models.Stats;
using System.Globalization;
using System.Text.Json;

namespace Logs.Formatters.Json;

/// <summary>
/// Форматтер для вывода статистики в формате JSON
/// </summary>
public sealed class JsonReportFormatter : IReportFormatter
{
    private static readonly JsonSerializerOptions WriteIndented = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc />
    public string Name => "json";

    /// <summary>
    /// Форматирует статистику в JSON
    /// </summary>
    public string Format(LogStats stats, ReportContext context)
    {
        var obj = new
        {
            files = stats.Files,
            totalRequestsCount = stats.TotalRequestsCount,
            responseSizeInBytes = new
            {
                stats.ResponseSizeInBytes.average,
                stats.ResponseSizeInBytes.max,
                stats.ResponseSizeInBytes.p95
            },
            resources = stats.Resources.Select(r => new
            {
                r.resource,
                r.totalRequestsCount
            }),
            responseCodes = stats.ResponseCodes.Select(c => new
            {
                c.code,
                c.totalResponsesCount
            }),
            requestsPerDate = stats.RequestsPerDate.Select(d => new
            {
                date = d.date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                d.weekday,
                totalRequestsCount = d.count,
                totalRequestsPercentage = d.percentage
            }),

            uniqueProtocols = stats.UniqueProtocols
        };

        return JsonSerializer.Serialize(obj, WriteIndented);
    }
}
