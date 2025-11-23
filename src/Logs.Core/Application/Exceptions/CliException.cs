namespace Logs.Core.Application.Exceptions;

/// <summary>
/// Исключение, возникающее при ошибках обработки аргументов командной строки
/// </summary>
public sealed class CliException(string message) : Exception(message)
{
}
