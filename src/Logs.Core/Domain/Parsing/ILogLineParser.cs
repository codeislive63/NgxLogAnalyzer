using Logs.Core.Domain.Models.Stats;

namespace Logs.Core.Domain.Parsing;

/// <summary>
/// Интерфейс для парсинга строк лог-файла
/// </summary>
public interface ILogLineParser
{
    /// <summary>
    /// Парсит строку лог-файла в объект LogEntry или возвращает null при ошибке парсинга
    /// </summary>
    LogEntry? Parse(string line);
}
