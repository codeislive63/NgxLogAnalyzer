namespace Logs.Core.Application.Abstractions.Sources;

/// <summary>
/// Интерфейс для разрешения glob-паттернов в список файлов
/// </summary>
public interface IGlobResolver
{
    /// <summary>
    /// Разрешает glob-паттерн в список путей к файлам
    /// </summary>
    IEnumerable<string> Resolve(string pattern);
}
