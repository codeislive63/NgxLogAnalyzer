namespace Logs.Core.Application.Exceptions;

/// <summary>
/// Исключение, возникающее при ошибках обработки аргументов командной строки
/// </summary>
/// <remarks>
/// Создает новый экземпляр исключения с указанным сообщением
/// </remarks>
public sealed class CliException(string message) : Exception(message)
{
}
