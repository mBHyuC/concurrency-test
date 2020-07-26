using System;
using System.Diagnostics.Tracing;

using Microsoft.Extensions.Logging;

namespace Concurrency.Model
{
    [EventSource(Name = "Concurrency.SQLServer")]
    public class SQLServerEventSource : EventSource
    {
        const int TraceEventId = 1;
        const int DebugEventId = 2;
        const int InfoEventId = 3;
        const int WarningEventId = 4;
        const int ErrorEventId = 5;

        public class Keywords
        {
            public const EventKeywords TraceEventKeyword = (EventKeywords)1;
            public const EventKeywords DebugEventKeyword = (EventKeywords)(1 << 1);
        }

        public static SQLServerEventSource Log = new SQLServerEventSource();

        SQLServerEventSource()
        {
        }

        public bool IsTraceEnabled => this.IsEnabled(EventLevel.Verbose, Keywords.TraceEventKeyword);

        public bool IsDebugEnabled => this.IsEnabled(EventLevel.Verbose, Keywords.DebugEventKeyword);

        public bool IsInfoEnabled => this.IsEnabled(EventLevel.Informational, EventKeywords.None);

        public bool IsWarningEnabled => this.IsEnabled(EventLevel.Warning, EventKeywords.None);

        public bool IsErrorEnabled => this.IsEnabled(EventLevel.Error, EventKeywords.None);

        [NonEvent]
        public void Trace(string source, string message) => this.Trace(source, message, string.Empty);

        [NonEvent]
        public void Trace(string source, string message, Exception exception)
        {
            if (this.IsTraceEnabled)
            {
                this.Trace(source, message, exception?.ToString() ?? string.Empty);
            }
        }


        [Event(TraceEventId, Message = "{0} - {1} {2}", Level = EventLevel.Verbose, Keywords = Keywords.TraceEventKeyword)]
        public void Trace(string source, string message, string info)
        {
            if (this.IsTraceEnabled)
            {
                this.WriteEvent(TraceEventId, source, message, info);
            }
        }

        [NonEvent]
        public void Debug(string source, string message) => this.Debug(source, message, string.Empty);

        [NonEvent]
        public void Debug(string source, string message, Exception exception)
        {
            if (this.IsDebugEnabled)
            {
                this.Debug(source, message, exception?.ToString() ?? string.Empty);
            }
        }

        [Event(DebugEventId, Message = "{0} - {1} {2}", Level = EventLevel.Verbose, Keywords = Keywords.DebugEventKeyword)]
        public void Debug(string source, string message, string info)
        {
            if (this.IsDebugEnabled)
            {
                this.WriteEvent(DebugEventId, source, message, info);
            }
        }

        [NonEvent]
        public void Info(string source, string message) => this.Info(source, message, string.Empty);

        [NonEvent]
        public void Info(string source, string message, Exception exception)
        {
            if (this.IsInfoEnabled)
            {
                this.Info(source, message, exception?.ToString() ?? string.Empty);
            }
        }

        [Event(InfoEventId, Message = "{0} - {1} {2}", Level = EventLevel.Informational)]
        public void Info(string source, string message, string info)
        {
            if (this.IsInfoEnabled)
            {
                this.WriteEvent(InfoEventId, source, message, info);
            }
        }

        [NonEvent]
        public void Warning(string source, string message) => this.Warning(source, message, string.Empty);

        [NonEvent]
        public void Warning(string source, string message, Exception exception)
        {
            if (this.IsWarningEnabled)
            {
                this.Warning(source, message, exception?.ToString() ?? string.Empty);
            }
        }

        [Event(WarningEventId, Message = "{0} - {1} - {2}", Level = EventLevel.Warning)]
        public void Warning(string source, string message, string exception)
        {
            if (this.IsWarningEnabled)
            {
                this.WriteEvent(WarningEventId, source, message, exception);
            }
        }

        [NonEvent]
        public void Error(string source, string message) => this.Error(source, message, string.Empty);

        [NonEvent]
        public void Error(string source, string message, Exception exception)
        {
            if (this.IsErrorEnabled)
            {
                this.Error(source, message, exception?.ToString() ?? string.Empty);
            }
        }

        [Event(ErrorEventId, Message = "{0} - {1} - {2}", Level = EventLevel.Error)]
        public void Error(string source, string message, string exception)
        {
            if (this.IsErrorEnabled)
            {
                this.WriteEvent(ErrorEventId, source, message, exception);
            }
        }
    }

    sealed class SQLServerEventSourceLogger : ILogger
    {
        readonly string name;

        public SQLServerEventSourceLogger(string name)
        {
            this.name = name;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    SQLServerEventSource.Log.Trace(this.name, formatter(state, exception), exception);
                    break;
                case LogLevel.Debug:
                    SQLServerEventSource.Log.Debug(this.name, formatter(state, exception), exception);
                    break;
                case LogLevel.Information:
                    SQLServerEventSource.Log.Info(this.name, formatter(state, exception), exception);
                    break;
                case LogLevel.Warning:
                    SQLServerEventSource.Log.Warning(this.name, formatter(state, exception), exception);
                    break;
                case LogLevel.Error:
                    SQLServerEventSource.Log.Error(this.name, formatter(state, exception), exception);
                    break;
                case LogLevel.Critical:
                    SQLServerEventSource.Log.Error(this.name, formatter(state, exception), exception);
                    break;
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return SQLServerEventSource.Log.IsTraceEnabled;
                case LogLevel.Debug:
                    return SQLServerEventSource.Log.IsDebugEnabled;
                case LogLevel.Information:
                    return SQLServerEventSource.Log.IsInfoEnabled;
                case LogLevel.Warning:
                    return SQLServerEventSource.Log.IsWarningEnabled;
                case LogLevel.Error:
                    return SQLServerEventSource.Log.IsErrorEnabled;
                case LogLevel.Critical:
                    return SQLServerEventSource.Log.IsErrorEnabled;
                case LogLevel.None:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }

        public IDisposable BeginScope<TState>(TState state) => NoOpDisposable.Instance;

        sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new NoOpDisposable();

            NoOpDisposable()
            {
            }

            public void Dispose()
            {
            }
        }
    }


    public class SQLServerEventSourceLoggerProvider : ILoggerProvider
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName) => new SQLServerEventSourceLogger(categoryName);
    }
}