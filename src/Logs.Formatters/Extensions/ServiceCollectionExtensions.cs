using Logs.Core.Application.Abstractions.Reporting;
using Logs.Formatters.Adoc;
using Logs.Formatters.Json;
using Logs.Formatters.Markdown;
using Microsoft.Extensions.DependencyInjection;

namespace Logs.Formatters.Extensions;

/// <summary>
/// Методы расширения для регистрации форматтеров в DI контейнере
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все доступные форматтеры и резолвер форматтеров
    /// </summary>
    public static IServiceCollection AddLogsFormatters(this IServiceCollection services)
    {
        services.AddSingleton<IReportFormatter, JsonReportFormatter>();
        services.AddSingleton<IReportFormatter, MarkdownReportFormatter>();
        services.AddSingleton<IReportFormatter, AdocReportFormatter>();
        services.AddSingleton<IReportFormatterResolver, ReportFormatterResolver>();
        return services;
    }
}
