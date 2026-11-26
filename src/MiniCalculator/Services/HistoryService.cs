using System.Collections.ObjectModel;
using System.IO;
using Com.MiniCalculator.Models;
using Microsoft.Extensions.Logging;

namespace Com.MiniCalculator.Services;

public sealed class HistoryService : IHistoryService, IDisposable
{
    private const int MaxEntries = 100;

    private readonly JsonFileStore<List<HistoryEntry>> _store;
    private bool _loaded;

    public HistoryService(ILogger<HistoryService> logger)
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MiniCalculator");
        HistoryPath = Path.Combine(folder, "history.json");

        _store = new JsonFileStore<List<HistoryEntry>>(
            HistoryPath,
            AppJsonContext.Default.ListHistoryEntry,
            logger);

        Entries = new ObservableCollection<HistoryEntry>();
    }

    public ObservableCollection<HistoryEntry> Entries { get; }

    public string HistoryPath { get; }

    public void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        List<HistoryEntry> loaded = _store.Load() ?? new List<HistoryEntry>();
        foreach (HistoryEntry entry in loaded.Take(MaxEntries))
        {
            Entries.Add(entry);
        }
    }

    public void Add(string expression, string result)
    {
        EnsureLoaded();
        Entries.Insert(0, new HistoryEntry
        {
            Expression = expression,
            Result = result,
            CreatedAt = DateTimeOffset.Now,
        });

        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }

        Save();
    }

    public void Clear()
    {
        Entries.Clear();
        Save();
    }

    private void Save() => _store.RequestSave(Entries.ToList());

    public void Dispose() => _store.Dispose();
}
