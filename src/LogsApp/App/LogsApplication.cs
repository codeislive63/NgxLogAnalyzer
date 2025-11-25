using Logs.Core.Application.Abstractions.Cli;
using Logs.Core.Application.Abstractions.Reporting;
using Logs.Core.Application.Abstractions.Sources;
using Logs.Core.Application.Exceptions;
using Logs.Core.Application.Validation;
using Logs.Core.Domain.Aggregation;
using Logs.Core.Domain.Models;
using Logs.Core.Domain.Models.Stats;
using Logs.Core.Domain.Parsing;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Logs.App;

public class LogsApplication(
    IArgumentsParser parser,
    IReportFormatterResolver formatterResolver,
    ILogSourceReader sourceReader,
    ILogLineParser lineParser,
    ILogStatsAggregator aggregator,
    ILogger<LogsApplication> logger)
{
    private readonly IArgumentsParser _parser = parser;
    private readonly IReportFormatterResolver _formatterResolver = formatterResolver;
    private readonly ILogSourceReader _sourceReader = sourceReader;
    private readonly ILogLineParser _lineParser = lineParser;
    private readonly ILogStatsAggregator _aggregator = aggregator;
    private readonly ILogger<LogsApplication> _logger = logger;

    public async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        try
        {
            var arguments = _parser.Parse(args);
            OutputPathValidator.Validate(arguments.Output, arguments.Format);

            var formatter = _formatterResolver.Resolve(arguments.Format);

            var entries = new List<LogEntry>();
            var filesList = new List<string>();

            foreach (var path in arguments.Paths)
            {
                await foreach (var source in _sourceReader.EnumerateSourcesAsync(path, ct).WithCancellation(ct))
                {
                    filesList.Add(source.DisplayName);

                    await foreach (var line in _sourceReader.ReadLinesAsync(source, ct).WithCancellation(ct))
                    {
                        var entry = _lineParser.Parse(line);

                        if (entry == null)
                        {
                            _logger.LogWarning("Skipping malformed line: {Line}", line);
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
            }

            var stats = _aggregator.Aggregate(entries, filesList);

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
            _logger.LogError(ex, "Неверный вызов: {Message}", ex.Message);
            return 2;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogError(ex, "Удалённый файл не найден");
            return 2;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Входной файл не найден");
            return 2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Непредвиденная ошибка...");
            return 1;
        }
    }
}