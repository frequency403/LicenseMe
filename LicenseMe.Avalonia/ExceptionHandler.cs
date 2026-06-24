using Microsoft.Extensions.Logging;

namespace LicenseMe.Avalonia;

public class ExceptionHandler(ILogger<ExceptionHandler> logger) : IObserver<Exception>
{
    public void OnCompleted()
    {
        logger.LogDebug("Completed");
    }

    public void OnError(Exception error)
    {
        logger.LogError(error, "Unhandled exception");
    }

    public void OnNext(Exception value)
    {
        logger.LogError(value, "Unhandled exception");  
    }
}