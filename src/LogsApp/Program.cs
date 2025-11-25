using Logs.App;
using Logs.Core.Application.Abstractions.Sources;
using Logs.Formatters.Extensions;
using Logs.Infrastructure.Extensions;
using Logs.Infrastructure.Sources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(lb =>
{
    lb.ClearProviders();
    lb.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
    });
});

services.AddLogsCore();
services.AddLogsFormatters();

services.AddTransient<LogsApplication>();

services.AddSingleton<IGlobResolver, GlobResolver>();
services.AddSingleton<ILogSourceReader, LogSourceReader>();

services.AddHttpClient();

using var provider = services.BuildServiceProvider();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var ct = cts.Token;

var app = provider.GetRequiredService<LogsApplication>();

return await app.RunAsync(args, ct);
