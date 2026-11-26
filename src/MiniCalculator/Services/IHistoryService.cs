using System.Collections.ObjectModel;
using Com.MiniCalculator.Models;

namespace Com.MiniCalculator.Services;

public interface IHistoryService
{
    ObservableCollection<HistoryEntry> Entries { get; }

    void EnsureLoaded();

    void Add(string expression, string result);

    void Clear();
}
