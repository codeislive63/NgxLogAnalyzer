using Logs.Core.Application.Abstractions.Cli;
using Logs.Core.Application.Validation;
using Logs.Core.Domain.Aggregation;
using Logs.Core.Domain.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Logs.Infrastructure.Extensions;

/// <summary>
/// Методы расширения для регистрации сервисов ядра приложения в DI контейнере
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует основные сервисы приложения: парсер аргументов, агрегатор статистики, парсер логов и валидатор вывода
    /// </summary>
    public static IServiceCollection AddLogsCore(this IServiceCollection services)
    {
        services.AddSingleton<IArgumentsParser, ArgumentsParser>();
        services.AddSingleton<ILogStatsAggregator, LogStatsAggregator>();
        services.AddSingleton<ILogLineParser, NginxLogLineParser>();
        services.AddSingleton<OutputPathValidator>();
        return services;
    }
}
