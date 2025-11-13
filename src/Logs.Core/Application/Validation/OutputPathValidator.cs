using Logs.Core.Application.Exceptions;

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

        var expectedExt = format.ToLowerInvariant() switch
        {
            "json" => ".json",
            "markdown" => ".md",
            "adoc" => ".ad",
            _ => throw new CliException($"Неизвестный формат вывода: {format}")
        };

        if (ext != expectedExt)
        {
            throw new CliException($"Файл вывода должен иметь расширение {expectedExt}");
        }
    }
}
