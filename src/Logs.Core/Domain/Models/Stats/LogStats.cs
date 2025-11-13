namespace Logs.Core.Domain.Models.Stats;

/// <summary>
/// Собранная статистика по лог-файлам
/// </summary>
public sealed record LogStats(
    IReadOnlyList<string> Files,
    int TotalRequestsCount,
    (double average, double max, double p95) ResponseSizeInBytes,
    IReadOnlyList<(string resource, int totalRequestsCount)> Resources,
    IReadOnlyList<(int code, int totalResponsesCount)> ResponseCodes,
    IReadOnlyList<(DateOnly date, string weekday, int count, double percentage)> RequestsPerDate,
    IReadOnlyList<string> UniqueProtocols
);
