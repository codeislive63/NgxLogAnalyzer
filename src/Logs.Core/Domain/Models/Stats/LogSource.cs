namespace Logs.Core.Domain.Models.Stats;

/// <summary>
/// Представляет источник лог-файла: локальный файл или удаленный URL
/// </summary>
public readonly record struct LogSource(string? LocalPath, Uri? RemoteUri, string DisplayName);
