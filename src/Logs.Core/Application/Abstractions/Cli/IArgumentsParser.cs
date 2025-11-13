using Logs.Core.Domain.Models;

namespace Logs.Core.Application.Abstractions.Cli;

/// <summary>
/// Интерфейс для парсинга аргументов командной строки
/// </summary>
public interface IArgumentsParser
{
    /// <summary>
    /// Парсит массив аргументов командной строки в объект Arguments
    /// </summary>
    Arguments Parse(string[] args);
}
