using Logs.Core.Application.Abstractions.Cli;
using Logs.Core.Application.Exceptions;
using Logs.Core.Domain.Models;
using System.Globalization;

namespace Logs.Core.Domain.Parsing;

/// <summary>
/// Парсер аргументов командной строки для приложения анализатора логов
/// </summary>
public sealed class ArgumentsParser : IArgumentsParser
{
    private static readonly HashSet<string> SupportedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "json", "markdown", "adoc"
    };

    private static readonly HashSet<string> SupportedParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "--path", "-p", "--format", "-f", "--output", "-o", "--from", "--to"
    };

    /// <inheritdoc/>
    public Arguments Parse(string[] args)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var paths = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];

            if (a is "--path" or "-p")
            {
                if (i + 1 >= args.Length || args[i + 1].StartsWith('-'))
                {
                    throw new CliException($"Отсутствует значение для аргумента: {a}");
                }

                i++;

                while (i < args.Length && !args[i].StartsWith('-'))
                {
                    paths.Add(args[i]);
                    i++;
                }

                i--;
            }
            else if (a.StartsWith("--"))
            {
                var parts = a.Split('=', 2);

                if (parts.Length == 2)
                {
                    map[parts[0]] = parts[1];
                }
                else
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                    {
                        map[a] = args[++i];
                    }
                    else
                    {
                        map[a] = "";
                    }
                }
            }
            else if (a.StartsWith('-'))
            {
                if (i + 1 >= args.Length)
                {
                    throw new CliException($"Отсутствует значение для аргумента: {a}");
                }

                map[a] = args[++i];
            }
            else
            {
                throw new CliException($"Неподдерживаемый параметр: {a}");
            }
        }

        foreach (var key in map.Keys)
        {
            if (!SupportedParameters.Contains(key))
            {
                throw new CliException($"Неподдерживаемый параметр: {key}");
            }
        }

        if (paths.Count == 0)
        {
            throw new CliException("Отсутствует обязательный параметр --path/-p");
        }

        if (!map.TryGetValue("--format", out var format) && !map.TryGetValue("-f", out format))
        {
            throw new CliException("Отсутствует обязательный параметр --format/-f");
        }

        if (!map.TryGetValue("--output", out var output) && !map.TryGetValue("-o", out output))
        {
            throw new CliException("Отсутствует обязательный параметр --output/-o");
        }

        if (!SupportedFormats.Contains(format))
        {
            throw new CliException($"Неподдерживаемый формат вывода: {format}");
        }

        DateTimeOffset? from = null, to = null;

        if (map.TryGetValue("--from", out var fromRaw) && !string.IsNullOrWhiteSpace(fromRaw))
        {
            if (!DateTimeOffset.TryParse(
                    fromRaw,
                    null,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var fromParsed))
            {
                throw new CliException("Некорректное значение даты для параметра --from");
            }

            from = fromParsed;
        }

        if (map.TryGetValue("--to", out var toRaw) && !string.IsNullOrWhiteSpace(toRaw))
        {
            if (!DateTimeOffset.TryParse(
                    toRaw,
                    null,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var toParsed))
            {
                throw new CliException("Некорректное значение даты для параметра --to");
            }

            to = toParsed;
        }

        if (from is not null && to is not null && from >= to)
        {
            throw new CliException("Параметр --from должен быть меньше значения параметра --to");
        }

        return new Arguments(paths, format, output, from, to);
    }
}
