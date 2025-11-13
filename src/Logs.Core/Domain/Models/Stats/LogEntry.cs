namespace Logs.Core.Domain.Models.Stats;

/// <summary>
/// Представляет одну запись из лог-файла NGINX
/// </summary>
public sealed record LogEntry(DateTimeOffset TimestampUtc, string Resource, string Protocol, int StatusCode, int ResponseSizeBytes);
