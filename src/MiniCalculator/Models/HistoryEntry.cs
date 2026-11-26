namespace Com.MiniCalculator.Models;

public sealed class HistoryEntry
{
    public string Expression { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
