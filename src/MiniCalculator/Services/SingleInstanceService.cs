using Microsoft.Extensions.Logging;

namespace Com.MiniCalculator.Services;

public sealed class SingleInstanceService : ISingleInstanceService
{
    private const string MutexName = @"Local\MiniCalculator.SingleInstance.7F1C";
    private const string ActivationEventName = @"Local\MiniCalculator.SingleInstance.Event.7F1C";

    private readonly ILogger<SingleInstanceService> _logger;
    private Mutex? _mutex;
    private EventWaitHandle? _activationEvent;
    private RegisteredWaitHandle? _registeredWait;
    private Action? _onSecondInstance;
    private bool _disposed;

    public SingleInstanceService(ILogger<SingleInstanceService> logger)
    {
        _logger = logger;
    }

    public bool TryAcquire()
    {
        try
        {
            _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
            if (createdNew)
            {
                _activationEvent = new EventWaitHandle(
                    initialState: false,
                    EventResetMode.AutoReset,
                    ActivationEventName);
                return true;
            }

            _mutex.Dispose();
            _mutex = null;
            SignalExisting();
            return false;
        }
        catch (Exception ex)
        {
            LogMessages.MutexCreateFailed(_logger, ex);
            return true;
        }
    }

    public void StartListening(Action onSecondInstance)
    {
        _onSecondInstance = onSecondInstance;
        if (_activationEvent is null)
        {
            return;
        }

        _registeredWait = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            static (state, _) => ((SingleInstanceService)state!).OnActivated(),
            this,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    private void OnActivated()
    {
        try
        {
            _onSecondInstance?.Invoke();
        }
        catch (Exception ex)
        {
            LogMessages.WindowActivationFailed(_logger, ex);
        }
    }

    private void SignalExisting()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                using EventWaitHandle handle = EventWaitHandle.OpenExisting(ActivationEventName);
                handle.Set();
                return;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogMessages.SignalExistingFailed(_logger, ex);
                return;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _registeredWait?.Unregister(null);
        _registeredWait = null;

        _activationEvent?.Dispose();
        _activationEvent = null;

        if (_mutex is not null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }

            _mutex.Dispose();
            _mutex = null;
        }
    }
}
