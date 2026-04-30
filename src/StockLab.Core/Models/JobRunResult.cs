namespace StockLab.Core.Models;

public class JobRunResult<T>
{
    public string JobName { get; init; } = string.Empty;

    public DateTimeOffset StartedAt { get; init; }

    public DateOnly? TradeDate { get; init; }

    public string Source { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public T? Data { get; init; }
}
