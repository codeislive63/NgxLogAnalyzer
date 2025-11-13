namespace Logs.Core.Application.Abstractions.Reporting;

/// <summary>
/// Интерфейс для получения форматтера по имени формата
/// </summary>
public interface IReportFormatterResolver
{
    /// <summary>
    /// Возвращает форматтер по имени формата
    /// </summary>
    IReportFormatter Resolve(string name);
}
