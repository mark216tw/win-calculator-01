using System.IO;
using Microsoft.Extensions.Logging;

namespace Com.MiniCalculator.Services;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly object _gate = new();

    public FileLoggerProvider(string directory)
    {
        _directory = directory;
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _directory, _gate);

    public void Dispose()
    {
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _category;
        private readonly string _directory;
        private readonly object _gate;

        public FileLogger(string category, string directory, object gate)
        {
            _category = category;
            _directory = directory;
            _gate = gate;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = formatter(state, exception);
            string line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{logLevel}] {_category}: {message}";
            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            line += Environment.NewLine;
            string path = Path.Combine(_directory, $"app-{DateTimeOffset.Now:yyyyMMdd}.log");

            try
            {
                lock (_gate)
                {
                    Directory.CreateDirectory(_directory);
                    File.AppendAllText(path, line);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
