
namespace Logs.Core.Domain.Models;

/// <summary>
/// Представляет аргументы командной строки приложения
/// </summary>
public record Arguments(IReadOnlyList<string> Paths, string Format, string Output, DateTimeOffset? From, DateTimeOffset? To);
