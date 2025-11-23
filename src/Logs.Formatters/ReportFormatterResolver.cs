using Logs.Core.Application.Abstractions.Reporting;

namespace Logs.Formatters;

/// <summary>
/// Резолвер форматтеров по имени формата
/// </summary>
/// <remarks>
/// Создает новый экземпляр резолвера с указанным набором форматтеров
/// </remarks>
public sealed class ReportFormatterResolver(IEnumerable<IReportFormatter> formatters) : IReportFormatterResolver
{
    private readonly IEnumerable<IReportFormatter> _formatters = formatters;

    /// <summary>
    /// Возвращает форматтер по имени формата или выбрасывает исключение если не найден
    /// </summary>
    public IReportFormatter Resolve(string name)
    {
        var fmt = _formatters.FirstOrDefault(f =>
            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)
        );

        return fmt ?? throw new InvalidOperationException($"Неподдерживаемый формат вывода: {name}");
    }
}
