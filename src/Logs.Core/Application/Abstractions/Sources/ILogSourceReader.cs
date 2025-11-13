using Logs.Core.Domain.Models.Stats;

namespace Logs.Core.Application.Abstractions.Sources;

/// <summary>
/// Интерфейс для чтения источников лог-файлов
/// </summary>
public interface ILogSourceReader
{
    /// <summary>
    /// Перечисляет все источники по заданному паттерну
    /// </summary>
    IAsyncEnumerable<LogSource> EnumerateSourcesAsync(string pattern, CancellationToken ct = default);

    /// <summary>
    /// Читает строки из указанного источника
    /// </summary>
    IAsyncEnumerable<string> ReadLinesAsync(LogSource source, CancellationToken ct = default);
}
