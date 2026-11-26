namespace Com.MiniCalculator.Services;

public interface ISingleInstanceService : IDisposable
{
    bool TryAcquire();

    void StartListening(Action onSecondInstance);
}
