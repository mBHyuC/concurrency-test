using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;

class CustomEventListener :
    EventListener
{
    protected override void OnEventWritten(EventWrittenEventArgs data)
    {
        var message = string.Format(data.Message, data.Payload?.ToArray() ?? new object[0]);
        Console.WriteLine($"{data.EventId} {data.Channel} {data.Level} {message}");
    }
}

namespace Concurrency
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var listener = new CustomEventListener())
            {
                listener.EnableEvents(OrchestratorEvents.Log, EventLevel.Verbose);
                listener.EnableEvents(Model.SQLServerEventSource.Log, EventLevel.Warning);

                var exe = new Executer("SomeName", "Server=(localdb)\\MSSQLLocalDB;Database=ABCD");
                await exe.StartAsync(new CancellationToken());
            }
        }
    }



}
