using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace Com.MiniCalculator.Services;

public sealed class JsonFileStore<T> : IDisposable
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(400);

    private readonly string _path;
    private readonly string? _directory;
    private readonly JsonTypeInfo<T> _typeInfo;
    private readonly ILogger _logger;
    private readonly object _pendingGate = new();
    private readonly object _writeGate = new();
    private Timer? _timer;
    private string? _pendingJson;
    private bool _disposed;

    public JsonFileStore(string path, JsonTypeInfo<T> typeInfo, ILogger logger)
    {
        _path = path;
        _directory = Path.GetDirectoryName(path);
        _typeInfo = typeInfo;
        _logger = logger;
    }

    public T? Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return default;
            }

            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize(json, _typeInfo);
        }
        catch (Exception ex)
        {
            LogMessages.JsonReadFailed(_logger, _path, ex);
            return default;
        }
    }

    public void RequestSave(T value)
    {
        if (_disposed)
        {
            return;
        }

        string json;
        try
        {
            json = JsonSerializer.Serialize(value, _typeInfo);
        }
        catch (Exception ex)
        {
            LogMessages.JsonSerializeFailed(_logger, _path, ex);
            return;
        }

        lock (_pendingGate)
        {
            if (_disposed)
            {
                return;
            }

            _pendingJson = json;
            _timer ??= new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _timer.Change(DebounceDelay, Timeout.InfiniteTimeSpan);
        }
    }

    public void Flush()
    {
        lock (_pendingGate)
        {
            if (!_disposed)
            {
                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        string? json = TakePending();
        if (json is not null)
        {
            Write(json);
        }
    }

    private void OnTimerElapsed(object? state)
    {
        string? json = TakePending();
        if (json is not null)
        {
            Write(json);
        }
    }

    private string? TakePending()
    {
        lock (_pendingGate)
        {
            string? json = _pendingJson;
            _pendingJson = null;
            return json;
        }
    }

    private void Write(string json)
    {
        lock (_writeGate)
        {
            try
            {
                if (_directory is not null)
                {
                    Directory.CreateDirectory(_directory);
                }

                string temporary = _path + ".tmp";
                File.WriteAllText(temporary, json);
                File.Move(temporary, _path, overwrite: true);
            }
            catch (Exception ex)
            {
                LogMessages.JsonWriteFailed(_logger, _path, ex);
            }
        }
    }

    public void Dispose()
    {
        Timer? timer;
        lock (_pendingGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            timer = _timer;
            _timer = null;
        }

        timer?.Dispose();
        Flush();
    }
}
