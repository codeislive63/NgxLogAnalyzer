using FluentAssertions;
using Logs.Core.Application.Abstractions.Sources;
using Logs.Core.Application.Exceptions;
using Logs.Core.Domain.Models.Stats;
using Logs.Infrastructure.Sources;
using System.Net;

namespace Logs.Test.Infrastructure;

/// <summary>
/// Тесты для читателя источников лог-файлов
/// </summary>
public class LogSourceReaderTests
{
    /// <summary>
    /// Проверяет перечисление и чтение локальных файлов
    /// </summary>
    [Fact]
    public async Task EnumerateSourcesAsync_ShouldReturnLocalFiles()
    {
        var tempFile = Path.GetTempFileName().Replace(".tmp", ".log");
        await File.WriteAllTextAsync(tempFile, "line1\nline2", TestContext.Current.CancellationToken);

        var globResolver = new FakeGlobResolver([tempFile]);
        var reader = new LogSourceReader(globResolver, new DelegatingClientFactory(_ => new HttpClient()));

        var sources = new List<LogSource>();

        await foreach (var source in reader.EnumerateSourcesAsync("pattern", TestContext.Current.CancellationToken))
        {
            sources.Add(source);
        }

        sources.Should().HaveCount(1);
        sources[0].LocalPath.Should().Be(tempFile);
        sources[0].DisplayName.Should().Be(Path.GetFileName(tempFile));

        var lines = new List<string>();

        await foreach (var line in reader.ReadLinesAsync(sources[0], TestContext.Current.CancellationToken))
        {
            lines.Add(line);
        }

        lines.Should().Equal("line1", "line2");

        File.Delete(tempFile);
    }

    /// <summary>
    /// Проверяет чтение строк из удаленного URL
    /// </summary>
    [Fact]
    public async Task ReadLinesAsync_ShouldReturnRemoteLines()
    {
        var globResolver = new FakeGlobResolver(["https://example.org/log"]);

        var httpFactory = new DelegatingClientFactory(_ =>
        {
            var handler = new InlineMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("line1\nline2")
            });
            return new HttpClient(handler, disposeHandler: true);
        });

        var reader = new LogSourceReader(globResolver, httpFactory);

        LogSource? remoteSource = null;

        await foreach (var source in reader.EnumerateSourcesAsync("pattern", TestContext.Current.CancellationToken))
        {
            remoteSource = source;
        }

        remoteSource.Should().NotBeNull();

        var lines = new List<string>();

        await foreach (var line in reader.ReadLinesAsync(remoteSource!.Value, TestContext.Current.CancellationToken))
        {
            lines.Add(line);
        }

        lines.Should().Equal("line1", "line2");
    }

    /// <summary>
    /// Проверяет выброс исключения при неподдерживаемом расширении файла
    /// </summary>
    [Fact]
    public async Task EnumerateSourcesAsync_ShouldThrow_OnUnsupportedExtension()
    {
        var tempFile = Path.GetTempFileName().Replace(".tmp", ".docx");
        File.WriteAllText(tempFile, "content");

        var globResolver = new FakeGlobResolver([tempFile]);
        var reader = new LogSourceReader(globResolver, new DelegatingClientFactory(_ => new HttpClient()));

        Func<Task> act = async () =>
        {
            await foreach (var _ in reader.EnumerateSourcesAsync("pattern")) { }
        };

        await act.Should().ThrowAsync<CliException>();

        File.Delete(tempFile);
    }

    /// <summary>
    /// Проверяет выброс исключения при ошибке HTTP запроса
    /// </summary>
    [Fact]
    public async Task ReadLinesAsync_ShouldThrow_OnHttpError()
    {
        var globResolver = new FakeGlobResolver(["https://example.org/notfound"]);
        var httpFactory = new DelegatingClientFactory(_ =>
        {
            var handler = new InlineMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            return new HttpClient(handler, disposeHandler: true);
        });
        var reader = new LogSourceReader(globResolver, httpFactory);

        LogSource? remoteSource = null;

        await foreach (var source in reader.EnumerateSourcesAsync("pattern", TestContext.Current.CancellationToken))
        {
            remoteSource = source;
        }

        remoteSource.Should().NotBeNull();

        Func<Task> act = async () =>
        {
            await foreach (var _ in reader.ReadLinesAsync(remoteSource!.Value)) { }
        };

        await act.Should().ThrowAsync<HttpRequestException>()
            .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Проверяет, что чтение локального файла корректно прерывается
    /// при срабатывании CancellationToken во время стримингового чтения
    /// </summary>
    [Fact]
    public async Task ReadLinesAsync_Local_ShouldCancel_Midway()
    {
        var tmp = Path.ChangeExtension(Path.GetTempFileName(), ".log");
        await File.WriteAllLinesAsync(tmp, Enumerable.Range(0, 50_000).Select(i => $"line {i}"), TestContext.Current.CancellationToken);

        var globResolver = new FakeGlobResolver([tmp]);
        var reader = new LogSourceReader(globResolver, new DelegatingClientFactory(_ => new HttpClient()));
        using var cts = new CancellationTokenSource();

        var read = 0;
        var t = Task.Run(async () =>
        {
            await foreach (var src in reader.EnumerateSourcesAsync("pattern", cts.Token).WithCancellation(cts.Token))
            {
                await foreach (var line in reader.ReadLinesAsync(src, cts.Token).WithCancellation(cts.Token))
                {
                    read++;

                    if (read == 10_000)
                    {
                        cts.Cancel();
                    }
                }
            }
        }, TestContext.Current.CancellationToken);

        try
        {
            await Task.WhenAny(t, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            read.Should().BeLessThan(50_000);
        }
        catch (OperationCanceledException) { }
        finally
        {
            File.Delete(tmp);
        }
    }

    /// <summary>
    /// Проверяет отмену при чтении удалённого HTTP-ресурса
    /// </summary>
    [Fact]
    public async Task ReadLinesAsync_Http_ShouldCancel_Midway()
    {
        var globResolver = new FakeGlobResolver(["https://example.org/log"]);

        var httpFactory = new DelegatingClientFactory(_ =>
            new HttpClient(new SlowHttpHandler(lines: 50_000), disposeHandler: true));

        var reader = new LogSourceReader(globResolver, httpFactory);

        using var cts = new CancellationTokenSource();

        var count = 0;
        var t = Task.Run(async () =>
        {
            await foreach (var src in reader.EnumerateSourcesAsync("pattern", cts.Token).WithCancellation(cts.Token))
            {
                await foreach (var line in reader.ReadLinesAsync(src, cts.Token).WithCancellation(cts.Token))
                {
                    if (++count == 10_000)
                    {
                        cts.Cancel();
                    }
                }
            }
        }, TestContext.Current.CancellationToken);

        try
        {
            await Task.WhenAny(t, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            count.Should().BeLessThan(50_000);
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Проверяет корректное завершение EnumerateSourcesAsync при отмене,
    /// когда источников очень много
    /// </summary>
    [Fact]
    public async Task EnumerateSourcesAsync_ShouldCancel_DuringEnumeration()
    {
        var total = 20_000;
        var created = new List<string>(capacity: total);

        for (int i = 0; i < total; i++)
        {
            var p = Path.Combine(Path.GetTempPath(), $"enum_{Guid.NewGuid():N}.log");
            await File.WriteAllTextAsync(p, "x", TestContext.Current.CancellationToken);
            created.Add(p);
        }

        try
        {
            var globResolver = new FakeGlobResolver(created);
            var reader = new LogSourceReader(globResolver, new DelegatingClientFactory(_ => new HttpClient()));
            using var cts = new CancellationTokenSource();

            var seen = 0;
            var t = Task.Run(async () =>
            {
                await foreach (var src in reader.EnumerateSourcesAsync("pattern", cts.Token).WithCancellation(cts.Token))
                {
                    if (++seen == 5000)
                    {
                        cts.Cancel();
                    }
                }
            }, TestContext.Current.CancellationToken);

            try
            {
                await Task.WhenAny(t, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
                seen.Should().BeLessThan(total);
            }
            catch (OperationCanceledException) { }
        }
        finally
        {
            foreach (var p in created)
            {
                try
                {
                    File.Delete(p);
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// Заглушка для IGlobResolver для тестирования
    /// </summary>
    private sealed class FakeGlobResolver(IEnumerable<string> items) : IGlobResolver
    {
        private readonly IEnumerable<string> _items = items;

        /// <summary>
        /// Возвращает предопределенный список путей
        /// </summary>
        public IEnumerable<string> Resolve(string pattern) => _items;
    }

    /// <summary>
    /// Заглушка для IHttpClientFactory для тестирования
    /// </summary>
    private sealed class DelegatingClientFactory(Func<string, HttpClient> factory) : IHttpClientFactory
    {
        private readonly Func<string, HttpClient> _factory = factory;

        /// <summary>
        /// Создает HttpClient используя переданную фабрику
        /// </summary>
        public HttpClient CreateClient(string name = "") => _factory(name);
    }

    /// <summary>
    /// Обработчик HTTP сообщений для тестирования, возвращает предопределенные ответы
    /// </summary>
    private sealed class InlineMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler = handler;

        /// <summary>
        /// Обрабатывает HTTP запрос используя переданную функцию
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }

    /// <summary>
    /// Тестовый HTTP-обработчик, имитирующий медленный удалённый источник.
    /// Используется для проверки корректного реагирования на CancellationToken
    /// </summary>
    private sealed class SlowHttpHandler(int lines = 50_000, int delayPerRequestMs = 0) : HttpMessageHandler
    {
        private readonly string _payload = string.Join('\n', Enumerable.Range(0, lines).Select(i => $"L{i}"));
        private readonly int _delayPerRequestMs = delayPerRequestMs;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (_delayPerRequestMs > 0)
            {
                await Task.Delay(_delayPerRequestMs, ct);
            }

            var content = new StringContent(_payload);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }
    }
}
