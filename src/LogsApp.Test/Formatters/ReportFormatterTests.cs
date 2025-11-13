using FluentAssertions;
using Logs.Core.Application.Abstractions.Reporting;
using Logs.Core.Domain.Models;
using Logs.Core.Domain.Models.Stats;
using Logs.Formatters;
using Logs.Formatters.Adoc;
using Logs.Formatters.Json;
using Logs.Formatters.Markdown;
using System.Text.Json;

namespace Logs.Tests.Formatters;

/// <summary>
/// Тесты для форматтеров отчетов
/// </summary>
public class ReportFormatterTests
{
    /// <summary>
    /// Создает тестовую статистику для проверки форматтеров
    /// </summary>
    private static LogStats CreateStatistics() => new(
        Files: ["file1.log", "file2.log"],
        TotalRequestsCount: 3,
        ResponseSizeInBytes: (average: 123.45, max: 500, p95: 400),
        Resources:
        [
            ("/a", 2),
            ("/b", 1)
        ],
        ResponseCodes:
        [
            (200, 2),
            (404, 1)
        ],
        RequestsPerDate:
        [
            (new DateOnly(2024, 3, 1), "Friday", 2, 66.67),
            (new DateOnly(2024, 3, 2), "Saturday", 1, 33.33)
        ],
        UniqueProtocols: ["HTTP/1.1", "grpc"]);

    /// <summary>
    /// Создает тестовый контекст отчета с временным диапазоном
    /// </summary>
    private static ReportContext CreateContext() => new()
    {
        From = DateTimeOffset.Parse("2024-03-01T00:00:00Z"),
        To = DateTimeOffset.Parse("2024-03-02T23:59:59Z")
    };

    /// <summary>
    /// Проверяет что JSON форматтер генерирует валидный JSON
    /// </summary>
    [Fact]
    public void JsonFormatter_ShouldProduceValidJson()
    {
        var formatter = new JsonReportFormatter();

        var result = formatter.Format(CreateStatistics(), CreateContext());

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("totalRequestsCount").GetInt32().Should().Be(3);
        doc.RootElement.GetProperty("files").GetArrayLength().Should().Be(2);
    }

    /// <summary>
    /// Проверяет что Markdown форматтер содержит таблицы и заголовки
    /// </summary>
    [Fact]
    public void MarkdownFormatter_ShouldContainTables()
    {
        var formatter = new MarkdownReportFormatter();

        var result = formatter.Format(CreateStatistics(), CreateContext());

        result.Should().Contain("#### Общая информация");
        result.Should().Contain("|        Метрика        |     Значение |");
        result.Should().Contain("/a");
    }

    /// <summary>
    /// Проверяет что ADOC форматтер содержит секции и таблицы
    /// </summary>
    [Fact]
    public void AdocFormatter_ShouldContainSections()
    {
        var formatter = new AdocReportFormatter();

        var result = formatter.Format(CreateStatistics(), CreateContext());

        result.Should().Contain("== Общая информация");
        result.Should().Contain("| Метрика | Значение");
        result.Should().Contain("| Код | Количество");
    }

    /// <summary>
    /// Проверяет что резолвер форматтеров возвращает правильный форматтер по имени
    /// </summary>
    [Fact]
    public void ReportFormatterResolver_ShouldReturnFormatterByName()
    {
        var formatters = new IReportFormatter[]
        {
            new JsonReportFormatter(),
            new MarkdownReportFormatter()
        };
        var resolver = new ReportFormatterResolver(formatters);

        resolver.Resolve("json").Should().BeOfType<JsonReportFormatter>();
        resolver.Resolve("markdown").Should().BeOfType<MarkdownReportFormatter>();
    }

    /// <summary>
    /// Проверяет выброс исключения когда форматтер не найден
    /// </summary>
    [Fact]
    public void ReportFormatterResolver_ShouldThrow_WhenFormatterNotFound()
    {
        var resolver = new ReportFormatterResolver(Array.Empty<IReportFormatter>());

        Action act = () => resolver.Resolve("txt");

        act.Should().Throw<InvalidOperationException>();
    }
}
