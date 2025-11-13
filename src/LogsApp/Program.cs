using Logs.Core.Application.Abstractions.Cli;
using Logs.Core.Application.Abstractions.Reporting;
using Logs.Core.Application.Abstractions.Sources;
using Logs.Core.Application.Exceptions;
using Logs.Core.Application.Validation;
using Logs.Core.Domain.Aggregation;
using Logs.Core.Domain.Models;
using Logs.Core.Domain.Models.Stats;
using Logs.Core.Domain.Parsing;
using Logs.Formatters.Extensions;
using Logs.Infrastructure.Extensions;
using Logs.Infrastructure.Sources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

using var host = Host.CreateDefaultBuilder()
    .ConfigureLogging(lb =>
    {
        lb.ClearProviders();
        lb.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        });
    })
    .ConfigureServices(services =>
    {
        services.AddLogsCore();
        services.AddSingleton<IGlobResolver, GlobResolver>();
        services.AddSingleton<ILogSourceReader, LogSourceReader>();
        services.AddLogsFormatters();
        services.AddHttpClient();
    })
    .Build();

var logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("LogsApp");


using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var appLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, appLifetime.ApplicationStopping);
var ct = linkedCts.Token;

try
{
    var parser = host.Services.GetRequiredService<IArgumentsParser>();
    var arguments = parser.Parse(args);

    var formatterResolver = host.Services.GetRequiredService<IReportFormatterResolver>();
    var formatter = formatterResolver.Resolve(arguments.Format);

    var sourceReader = host.Services.GetRequiredService<ILogSourceReader>();
    var lineParser = host.Services.GetRequiredService<ILogLineParser>();
    var aggregator = host.Services.GetRequiredService<ILogStatsAggregator>();

    var entries = new List<LogEntry>();
    var filesList = new List<string>();

    await foreach (var source in sourceReader.EnumerateSourcesAsync(arguments.Path, ct).WithCancellation(ct))
    {
        filesList.Add(source.DisplayName);

        await foreach (var line in sourceReader.ReadLinesAsync(source, ct).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();

            var entry = lineParser.Parse(line);

            if (entry == null)
            {
                logger.LogWarning("Skipping malformed line: {Line}", line);
                continue;
            }

            if (arguments.From is { } from && entry.TimestampUtc < from)
            {
                continue;
            }

            if (arguments.To is { } to && entry.TimestampUtc > to)
            {
                continue;
            }

            entries.Add(entry);
        }
    }

    var stats = aggregator.Aggregate(entries, filesList);
    OutputPathValidator.Validate(arguments.Output, arguments.Format);

    var report = formatter.Format(stats, new ReportContext
    {
        From = arguments.From,
        To = arguments.To
    });

    await File.WriteAllTextAsync(arguments.Output, report, ct);
    return 0;
}
catch (CliException ex)
{
    logger.LogError(ex, "Неверный вызов: {Message}", ex.Message);
    return 2;
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    logger.LogError(ex, "Удалённый файл не найден");
    return 2;
}
catch (FileNotFoundException ex)
{
    logger.LogError(ex, "Входной файл не найден");
    return 2;
}
catch (Exception ex)
{
    logger.LogError(ex, "Непредвиденная ошибка...");
    return 1;
}
