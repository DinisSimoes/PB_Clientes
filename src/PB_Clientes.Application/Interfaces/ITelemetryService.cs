using System.Diagnostics;

namespace PB_Clientes.Application.Interfaces
{
    public interface ITelemetryService
    {
        Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal, ActivityContext? parentContext = null, IDictionary<string, object?>? tags = null);
        void RestoreBaggage(Activity? activity, string? baggageJson);
        void InjectTraceContext<T>(T context, Activity? activity) where T : class;
    }
}
