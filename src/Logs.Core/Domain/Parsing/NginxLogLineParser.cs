using Logs.Core.Domain.Models.Stats;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Logs.Core.Domain.Parsing;

/// <summary>
/// Парсер строк лог-файла NGINX в формате combined log
/// </summary>
public sealed partial class NginxLogLineParser : ILogLineParser
{
    /// <summary>
    /// Парсит строку лог-файла NGINX в объект LogEntry
    /// </summary>
    public LogEntry? Parse(string line)
    {
        var match = Pattern().Match(line);

        if (!match.Success)
        {
            return null;
        }

        var timeRaw = match.Groups["time"].Value;

        if (!DateTimeOffset.TryParseExact(timeRaw, ["dd/MMM/yyyy:HH:mm:ss K", "d/MMM/yyyy:HH:mm:ss K"],
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ts))
        {
            return null;
        }

        var resource = match.Groups["resource"].Value;
        var protocol = match.Groups["protocol"].Value.Trim();
        var status = int.Parse(match.Groups["status"].Value, CultureInfo.InvariantCulture);
        var size = int.Parse(match.Groups["size"].Value, CultureInfo.InvariantCulture);

        return new LogEntry(ts.ToUniversalTime(), resource, protocol, status, size);
    }

    /// <summary>
    /// Скомпилированное на этапе сборки регулярное выражение для разбора строки лога NGINX
    /// </summary>
    [GeneratedRegex(@"^(?<ip>\S+) - \S+ \[(?<time>[^\]]+)\] ""(?<method>\S+) (?<resource>\S+) (?<protocol>[^""]+)"" (?<status>\d{3}) (?<size>\d+) ""(?<ref>[^""]*)"" ""(?<agent>[^""]*)""$", RegexOptions.Compiled)]
    private static partial Regex Pattern();
}
