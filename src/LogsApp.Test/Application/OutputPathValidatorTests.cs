using FluentAssertions;
using Logs.Core.Application.Exceptions;
using Logs.Core.Application.Validation;

namespace Logs.Test.Application;

/// <summary>
/// Тесты для валидатора выходного файла
/// </summary>
public class OutputPathValidatorTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Создает временную директорию для тестов
    /// </summary>
    public OutputPathValidatorTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Проверяет успешную валидацию для всех поддерживаемых форматов и расширений
    /// </summary>
    [Theory]
    [InlineData("json", ".json")]
    [InlineData("markdown", ".md")]
    [InlineData("adoc", ".ad")]
    public void Validate_ShouldPass_ForSupportedExtensions(string format, string extension)
    {
        var output = Path.Combine(_tempDirectory, "result" + extension);

        Action act = () => OutputPathValidator.Validate(output, format);

        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет выброс исключения когда выходной файл уже существует
    /// </summary>
    [Fact]
    public void Validate_ShouldThrow_WhenFileAlreadyExists()
    {
        var output = Path.Combine(_tempDirectory, "existing.json");
        File.WriteAllText(output, string.Empty);

        Action act = () => OutputPathValidator.Validate(output, "json");

        act.Should().Throw<CliException>().WithMessage("*Выходной файл уже существует*");
    }

    /// <summary>
    /// Проверяет выброс исключения когда директория для выходного файла не существует
    /// </summary>
    [Fact]
    public void Validate_ShouldThrow_WhenDirectoryDoesNotExist()
    {
        var output = Path.Combine(_tempDirectory, "does", "not", "exist.json");

        Action act = () => OutputPathValidator.Validate(output, "json");

        act.Should().Throw<CliException>().WithMessage("*Указанная выходная директория не существует*");
    }

    /// <summary>
    /// Проверяет выброс исключения когда расширение файла не соответствует формату
    /// </summary>
    [Fact]
    public void Validate_ShouldThrow_WhenExtensionIsIncorrect()
    {
        var output = Path.Combine(_tempDirectory, "result.txt");

        Action act = () => OutputPathValidator.Validate(output, "json");

        act.Should().Throw<CliException>().WithMessage("*Файл вывода должен иметь расширение .json*");
    }

    /// <summary>
    /// Удаляет временную директорию после тестов
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }
}
