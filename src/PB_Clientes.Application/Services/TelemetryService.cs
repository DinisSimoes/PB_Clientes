using PB_Clientes.Application.Interfaces;
using System.Diagnostics;
using System.Text.Json;

namespace PB_Clientes.Application.Services
{
    public class TelemetryService : ITelemetryService
    {
        private static readonly ActivitySource ActivitySource = new("PB_Clientes.Api");

        public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal, ActivityContext? parentContext = null, IDictionary<string, object?>? tags = null)
        {
            var activity = parentContext.HasValue
                ? ActivitySource.StartActivity(name, kind, parentContext.Value)
                : ActivitySource.StartActivity(name, kind);

            if (activity != null && tags != null)
            {
                foreach (var tag in tags)
                    activity.SetTag(tag.Key, tag.Value);
            }

            return activity;
        }

        public void RestoreBaggage(Activity? activity, string? baggageJson)
        {
            if (activity == null || string.IsNullOrEmpty(baggageJson))
                return;

            try
            {
                var items = JsonSerializer.Deserialize<Dictionary<string, string>>(baggageJson);
                if (items == null)
                    return;

                foreach (var item in items)
                    activity.AddBaggage(item.Key, item.Value);
            }
            catch
            {
            }
        }

        public void InjectTraceContext<T>(T context, Activity? activity) where T : class
        {
            if (activity == null || context == null)
                return;

            // MassTransit PublishContext
            if (context is MassTransit.PublishContext publishContext)
            {
                publishContext.Headers.Set("traceparent", activity.Id);
                if (!string.IsNullOrEmpty(activity.TraceStateString))
                    publishContext.Headers.Set("tracestate", activity.TraceStateString);

                foreach (var item in activity.Baggage)
                    publishContext.Headers.Set(item.Key, item.Value);
            }
        }

    }
}
