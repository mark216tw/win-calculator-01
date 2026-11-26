using Microsoft.Extensions.Logging;

namespace Com.MiniCalculator.Services;

public interface ISingleInstanceService : IDisposable
{
    bool TryAcquire();

    void AttachLogger(ILogger<SingleInstanceService> logger);

    void StartListening(Action onSecondInstance);
}
