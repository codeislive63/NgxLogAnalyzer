using FluentAssertions;
using Logs.Core.Application.Exceptions;
using Logs.Core.Domain.Parsing;

namespace Logs.Test.Domain.Parsing;

/// <summary>
/// Тесты для парсера аргументов командной строки
/// </summary>
public class ArgumentsParserTests
{
    private readonly ArgumentsParser _parser = new();

    /// <summary>
    /// Проверяет успешный парсинг всех обязательных и опциональных параметров
    /// </summary>
    [Fact]
    public void Parse_ShouldReturnArguments_WhenAllParametersProvided()
    {
        var args = new[]
        {
            "--path", "logs/*.log",
            "--format", "json",
            "--output", "result.json",
            "--from", "2025-01-01T00:00:00Z",
            "--to", "2025-01-02T00:00:00Z"
        };

        var result = _parser.Parse(args);

        result.Paths.Should().ContainSingle().Which.Should().Be("logs/*.log");
        result.Format.Should().Be("json");
        result.Output.Should().Be("result.json");
        result.From.Should().Be(DateTimeOffset.Parse("2025-01-01T00:00:00Z"));
        result.To.Should().Be(DateTimeOffset.Parse("2025-01-02T00:00:00Z"));
    }

    /// <summary>
    /// Поддержка нескольких путей после --path/-p
    /// </summary>
    [Fact]
    public void Parse_ShouldSupportMultiplePaths_ForShortP()
    {
        var args = new[]
        {
            "-p", "logs/part1.txt", "logs/part2.txt",
            "--format", "json",
            "--output", "result.json"
        };

        var result = _parser.Parse(args);

        result.Paths.Should().Equal("logs/part1.txt", "logs/part2.txt");
        result.Format.Should().Be("json");
        result.Output.Should().Be("result.json");
    }

    /// <summary>
    /// Проверяет выброс исключения при отсутствии обязательного параметра --path
    /// </summary>
    [Fact]
    public void Parse_ShouldThrow_WhenRequiredParameterMissing()
    {
        var args = new[] { "--format", "json", "--output", "result.json" };

        Action act = () => _parser.Parse(args);

        act.Should().Throw<CliException>().WithMessage("*--path*");
    }

    /// <summary>
    /// Проверяет выброс исключения при указании неподдерживаемого формата вывода
    /// </summary>
    [Fact]
    public void Parse_ShouldThrow_WhenUnsupportedFormatProvided()
    {
        var args = new[] { "--path", "logs/*.log", "--format", "txt", "--output", "result.txt" };

        Action act = () => _parser.Parse(args);

        act.Should().Throw<CliException>().WithMessage("*Неподдерживаемый формат вывода*");
    }

    /// <summary>
    /// Проверяет выброс исключения при указании невалидной даты в параметре --from
    /// </summary>
    [Fact]
    public void Parse_ShouldThrow_WhenDateIsInvalid()
    {
        var args = new[]
        {
            "--path", "logs/*.log",
            "--format", "json",
            "--output", "result.json",
            "--from", "2025-13-01"
        };

        Action act = () => _parser.Parse(args);

        act.Should().Throw<CliException>().WithMessage("*Некорректное значение даты для параметра --from*");
    }

    /// <summary>
    /// Проверяет выброс исключения когда начальная дата больше или равна конечной
    /// </summary>
    [Fact]
    public void Parse_ShouldThrow_WhenFromIsGreaterThanTo()
    {
        var args = new[]
        {
            "--path", "logs/*.log",
            "--format", "json",
            "--output", "result.json",
            "--from", "2025-01-02T00:00:00Z",
            "--to", "2025-01-01T00:00:00Z"
        };

        Action act = () => _parser.Parse(args);

        act.Should().Throw<CliException>()
            .WithMessage("*Параметр --from должен быть меньше значения параметра --to*");
    }

    /// <summary>
    /// Проверяет выброс исключения при указании неподдерживаемого параметра
    /// </summary>
    [Fact]
    public void Parse_ShouldThrow_WhenUnsupportedParameterProvided()
    {
        var args = new[]
        {
            "--path", "logs/*.log",
            "--format", "json",
            "--output", "result.json",
            "--custom", "argument"
        };

        Action act = () => _parser.Parse(args);

        act.Should().Throw<CliException>().WithMessage("*Неподдерживаемый параметр*");
    }

    /// <summary>
    /// Проверяет выброс исключения при указании неподдерживаемого параметра в формате --param=value
    /// </summary>
    [Fact]
    public void Parse_ShouldThrow_WhenUnsupportedParameterProvidedWithEquals()
    {
        var args = new[]
        {
            "--path", "logs/*.log",
            "--format", "json",
            "--output", "result.json",
            "--custom=argument"
        };

        Action act = () => _parser.Parse(args);

        act.Should().Throw<CliException>().WithMessage("*Неподдерживаемый параметр*");
    }
}
