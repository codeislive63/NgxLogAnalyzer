using Logs.Core.Application.Exceptions;
using System.Runtime.Intrinsics.X86;

namespace Logs.Core.Application.Validation;

/// <summary>
/// Валидатор пути выходного файла и его расширения
/// </summary>
public sealed class OutputPathValidator
{
    /// <summary>
    /// Проверяет корректность пути выходного файла, его расширения и существование директории
    /// </summary>
    public static void Validate(string outputPath, string format)
    {
        if (File.Exists(outputPath))
        {
            throw new CliException("Выходной файл уже существует");
        }

        var dir = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            throw new CliException("Указанная выходная директория не существует");
        }

        var ext = Path.GetExtension(outputPath).ToLowerInvariant();
        var fmt = format.ToLowerInvariant();

        var expectedExtensions = new Dictionary<string, string>
        {
            ["json"] = ".json",
            ["markdown"] = ".md",
            ["adoc"] = ".ad"
        };

        if (!expectedExtensions.TryGetValue(fmt, out var expectedExt))
        {
            throw new CliException($"Неизвестный формат вывода: {format}");
        }

        if (ext != expectedExt)
        {
            throw new CliException($"Файл вывода должен иметь расширение {expectedExt}");
        }
    }
}
