namespace Logs.Core.Domain.Models;

/// <summary>
/// Представляет аргументы командной строки приложения
/// </summary>
public record Arguments(string Path, string Format, string Output, DateTimeOffset? From, DateTimeOffset? To);
