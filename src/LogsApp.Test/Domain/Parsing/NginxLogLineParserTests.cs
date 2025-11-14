using FluentAssertions;
using Logs.Core.Domain.Models.Stats;
using Logs.Core.Domain.Parsing;

namespace Logs.Test.Domain.Parsing;

/// <summary>
/// Тесты для парсера строк лог-файла NGINX
/// </summary>
public class NginxLogLineParserTests
{
    private readonly NginxLogLineParser _parser = new();

    /// <summary>
    /// Проверяет успешный парсинг валидной строки лог-файла
    /// </summary>
    [Fact]
    public void Parse_ShouldReturnEntry_ForValidLine()
    {
        const string line = "93.180.71.3 - - [17/May/2015:08:05:32 +0000] \"GET /downloads/product_1 HTTP/1.1\" 304 0 \"-\" \"Debian\"";

        LogEntry? entry = _parser.Parse(line);

        entry.Should().NotBeNull();
        entry!.Resource.Should().Be("/downloads/product_1");
        entry.Protocol.Should().Be("HTTP/1.1");
        entry.StatusCode.Should().Be(304);
        entry.ResponseSizeBytes.Should().Be(0);
        entry.TimestampUtc.Should().Be(DateTimeOffset.Parse("2015-05-17T08:05:32+00:00").ToUniversalTime());
    }

    /// <summary>
    /// Проверяет возврат null для невалидной строки лог-файла
    /// </summary>
    [Fact]
    public void Parse_ShouldReturnNull_ForInvalidLine()
    {
        const string line = "invalid line";

        LogEntry? entry = _parser.Parse(line);

        entry.Should().BeNull();
    }

    /// <summary>
    /// Проверяет возврат null когда дата в строке имеет неверный формат
    /// </summary>
    [Fact]
    public void Parse_ShouldReturnNull_WhenDateIsInvalid()
    {
        const string line = "93.180.71.3 - - [17/May/2015 08:05:32 +0000] \"GET /downloads/product_1 HTTP/1.1\" 304 0 \"-\" \"Debian\"";

        LogEntry? entry = _parser.Parse(line);

        entry.Should().BeNull();
    }
}
