using Logs.Core.Application.Abstractions.Sources;
using Logs.Core.Application.Exceptions;
using Logs.Core.Domain.Models.Stats;
using System.Runtime.CompilerServices;

namespace Logs.Infrastructure.Sources;

/// <summary>
/// Реализация чтения источников лог-файлов: локальные файлы и удаленные URL
/// </summary>
public sealed class LogSourceReader(IGlobResolver globResolver, IHttpClientFactory httpClientFactory) : ILogSourceReader
{
    private readonly IGlobResolver _globResolver = globResolver;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <inheritdoc />
    public async IAsyncEnumerable<LogSource> EnumerateSourcesAsync(string pattern, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var s in _globResolver.Resolve(pattern))
        {
            ct.ThrowIfCancellationRequested();

            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(s);
                yield return new LogSource(null, uri, uri.Segments.Last());
            }
            else
            {
                if (!File.Exists(s))
                {
                    throw new FileNotFoundException($"Файл не найден: {s}");
                }

                var ext = Path.GetExtension(s).ToLowerInvariant();

                yield return ext is not ".txt" and not ".log"
                    ? throw new CliException($"Неподдерживаемое расширение входного файла: {ext}")
                    : new LogSource(s, null, Path.GetFileName(s));
            }
        }

        await Task.Yield();
    }

    /// <summary>
    /// Читает строки из локального файла или удаленного URL построчно
    /// </summary>
    public async IAsyncEnumerable<string> ReadLinesAsync(LogSource source, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (source.LocalPath != null)
        {
            using var fs = File.OpenRead(source.LocalPath);
            using var sr = new StreamReader(fs);

            while (!sr.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();

                var line = await sr.ReadLineAsync(ct);

                if (line != null)
                {
                    yield return line;
                }
            }

            yield break;
        }

        var client = _httpClientFactory.CreateClient();

        using var response = await client.GetAsync(source.RemoteUri!, HttpCompletionOption.ResponseHeadersRead, cancellationToken: ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException("Ошибка при чтении удалённого файла", null, response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);

            if (line != null)
            {
                yield return line;
            }
        }
    }
}
