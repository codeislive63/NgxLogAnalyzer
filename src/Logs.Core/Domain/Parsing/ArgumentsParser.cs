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

        ParseArguments(args, map, paths);
        ValidateParameters(map, paths);

        var format = GetRequired(map, "--format", "-f",
            "Отсутствует обязательный параметр --format/-f"
        );

        var output = GetRequired(map, "--output", "-o",
            "Отсутствует обязательный параметр --output/-o"
        );

        if (!SupportedFormats.Contains(format))
        {
            throw new CliException($"Неподдерживаемый формат вывода: {format}");
        }

        var from = ParseDateOrNull(map, "--from",
            "Некорректное значение даты для параметра --from"
        );

        var to = ParseDateOrNull(map, "--to",
            "Некорректное значение даты для параметра --to"
        );

        if (from is not null && to is not null && from >= to)
        {
            throw new CliException("Параметр --from должен быть меньше значения параметра --to");
        }

        return new Arguments(paths, format, output, from, to);
    }

    private static void ParseArguments(
        string[] args,
        Dictionary<string, string> map,
        List<string> paths)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];

            if (a is "--path" or "-p")
            {
                ParsePaths(args, ref i, paths);
            }
            else if (a.StartsWith("--", StringComparison.Ordinal))
            {
                ParseLongOption(args, ref i, map);
            }
            else if (a.StartsWith("-", StringComparison.Ordinal))
            {
                ParseShortOption(args, ref i, map);
            }
            else
            {
                throw new CliException($"Неподдерживаемый параметр: {a}");
            }
        }
    }

    private static void ParsePaths(string[] args, ref int i, List<string> paths)
    {
        var key = args[i];

        if (i + 1 >= args.Length || args[i + 1].StartsWith('-'))
        {
            throw new CliException($"Отсутствует значение для аргумента: {key}");
        }

        i++;

        while (i < args.Length && !args[i].StartsWith('-'))
        {
            paths.Add(args[i]);
            i++;
        }

        i--;
    }

    private static void ParseLongOption(
        string[] args,
        ref int i,
        Dictionary<string, string> map)
    {
        var a = args[i];
        var parts = a.Split('=', 2);

        if (parts.Length == 2)
        {
            map[parts[0]] = parts[1];
            return;
        }

        if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
        {
            map[a] = args[++i];
        }
        else
        {
            map[a] = "";
        }
    }

    private static void ParseShortOption(
        string[] args,
        ref int i,
        Dictionary<string, string> map)
    {
        var a = args[i];

        if (i + 1 >= args.Length)
        {
            throw new CliException($"Отсутствует значение для аргумента: {a}");
        }

        map[a] = args[++i];
    }

    private static void ValidateParameters(
        Dictionary<string, string> map,
        List<string> paths)
    {
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
    }

    private static string GetRequired(
        Dictionary<string, string> map,
        string longKey,
        string shortKey,
        string errorMessage)
    {
        if (!map.TryGetValue(longKey, out var value) &&
            !map.TryGetValue(shortKey, out value))
        {
            throw new CliException(errorMessage);
        }

        return value;
    }

    private static DateTimeOffset? ParseDateOrNull(
        Dictionary<string, string> map,
        string key,
        string errorMessage)
    {
        if (!map.TryGetValue(key, out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(
                raw,
                null,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            throw new CliException(errorMessage);
        }

        return parsed;
    }
}
