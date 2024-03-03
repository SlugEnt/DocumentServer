using Microsoft.Extensions.Logging;

namespace Test_DocumentServer.SupportObjects;

/// <summary>
///     Mocks out a Logger
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class MockLogger<T> : ILogger<T>
{
    public abstract IDisposable BeginScope<TState>(TState state);


    public virtual bool IsEnabled(LogLevel logLevel) => true;


    void ILogger.Log<TState>(LogLevel logLevel,
                             EventId eventId,
                             TState state,
                             Exception exception,
                             Func<TState, Exception, string> formatter) =>
        Log(logLevel, formatter(state, exception));


    public abstract void Log(LogLevel logLevel,
                             string message);
}