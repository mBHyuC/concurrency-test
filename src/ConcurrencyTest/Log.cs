using System.Diagnostics.Tracing;

namespace Concurrency
{
    [EventSource(Name = "Concurrency.Orchestrator")]
    class OrchestratorEvents : EventSource
    {
        [Event(1, Message = "Application Failure: {0}", Level = EventLevel.Error)]
        public void Failure(string message) { WriteEvent(1, message); }

        [Event(2, Message = "Starting up.", Level = EventLevel.Informational)]
        public void Startup() { WriteEvent(2); }

        [Event(3, Message = "{0}", Level = EventLevel.Verbose)]
        public void Message(string setting) { WriteEvent(3, setting); }

        [Event(4, Message = "{0}", Level = EventLevel.Warning)]
        public void Warning(string setting) { WriteEvent(4, setting); }

        [Event(5, Message = "{0}", Level = EventLevel.Verbose)]
        public void DebugMessage(string ex) { WriteEvent(5, ex); }

        public static OrchestratorEvents Log = new OrchestratorEvents();
    }


}