using FluentAssertions;
using Logs.Infrastructure.Sources;

namespace Logs.Test.Infrastructure;

/// <summary>
/// Тесты для резолвера glob-паттернов
/// </summary>
public class GlobResolverTests : IDisposable
{
    private readonly string _root;
    private readonly GlobResolver _resolver = new();

    /// <summary>
    /// Создает временную структуру директорий и файлов для тестов
    /// </summary>
    public GlobResolverTests()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "sub"));

        File.WriteAllText(Path.Combine(_root, "file1.log"), "test");
        File.WriteAllText(Path.Combine(_root, "file2.txt"), "test");
        File.WriteAllText(Path.Combine(_root, "sub", "file3.log"), "test");
    }

    /// <summary>
    /// Проверяет разрешение паттерна с одним символом подстановки *
    /// </summary>
    [Fact]
    public void Resolve_ShouldReturnMatchingFiles_ForSingleWildcard()
    {
        var pattern = Path.Combine(_root, "*.log");

        var result = _resolver.Resolve(pattern).ToArray();

        result.Should().HaveCount(1);
        result.Single().Should().EndWith("file1.log");
    }

    /// <summary>
    /// Проверяет разрешение паттерна без подстановок
    /// </summary>
    [Fact]
    public void Resolve_ShouldReturnExactMatch_WhenPatternIsLiteral()
    {
        var pattern = Path.Combine(_root, "file1.log");

        var result = _resolver.Resolve(pattern).ToArray();

        result.Should().ContainSingle()
            .Which.Should().EndWith("file1.log");
    }

    /// <summary>
    /// Проверяет поддержку одиночного символа подстановки ?
    /// </summary>
    [Fact]
    public void Resolve_ShouldSupportSingleCharacterWildcard()
    {
        var pattern = Path.Combine(_root, "file?.log");

        var result = _resolver.Resolve(pattern).ToArray();

        result.Should().ContainSingle()
            .Which.Should().EndWith("file1.log");
    }

    /// <summary>
    /// Проверяет что URL возвращается как есть без обработки
    /// </summary>
    [Fact]
    public void Resolve_ShouldReturnUrl_WhenInputIsUrl()
    {
        var result = _resolver.Resolve("https://example.com/file.log").Single();

        result.Should().Be("https://example.com/file.log");
    }

    /// <summary>
    /// Проверяет, что резолвер корректно обрабатывает паттерн с двойной звездочкой (**)
    /// </summary>
    [Fact]
    public void Resolve_ShouldSupportDoubleStarRecursive()
    {
        var pattern = Path.Combine(_root, "**", "*.log");

        var result = _resolver.Resolve(pattern).ToArray();

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Удаляет временную директорию после тестов
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }
}
