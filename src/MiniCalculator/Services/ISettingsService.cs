using Com.MiniCalculator.Models;

namespace Com.MiniCalculator.Services;

public interface ISettingsService
{
    AppSettings Current { get; }

    string SettingsPath { get; }

    void Save();
}
