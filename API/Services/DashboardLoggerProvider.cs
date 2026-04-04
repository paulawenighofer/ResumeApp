namespace API.Services;

public sealed class DashboardLoggerProvider : ILoggerProvider
{
    private readonly AppLogStore _logStore;

    public DashboardLoggerProvider(AppLogStore logStore)
    {
        _logStore = logStore;
    }

    public ILogger CreateLogger(string categoryName) => new DashboardLogger(categoryName, _logStore);

    public void Dispose()
    {
    }

    private sealed class DashboardLogger : ILogger
    {
        private static readonly IDisposable Scope = new NoopScope();
        private readonly string _categoryName;
        private readonly AppLogStore _logStore;

        public DashboardLogger(string categoryName, AppLogStore logStore)
        {
            _categoryName = categoryName;
            _logStore = logStore;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Scope;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

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

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
            {
                return;
            }

            if (exception is not null)
            {
                message = $"{message}{Environment.NewLine}{exception.Message}";
            }

            _logStore.Add(logLevel.ToString().ToUpperInvariant(), _categoryName, message.Trim());
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
