using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PB_Clientes.Application.Interfaces;
using PB_Clientes.Application.Repositories;
using PB_Clientes.Domain.Entities;
using System.Diagnostics;
using System.Text.Json;

namespace PB_Clientes.Infrastructure.Outbox
{
    public class OutboxDispatcher : BackgroundService
    {
        private readonly ILogger<OutboxDispatcher> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITelemetryService _telemetry;
        private const int BatchSize = 50;
        private const int RetryDelaySeconds = 5;
        private static readonly ActivitySource ActivitySource = new("PB_Clientes.Api.Outbox");

        public OutboxDispatcher(
            ILogger<OutboxDispatcher> logger,
            IServiceScopeFactory scopeFactory,
            ITelemetryService telemetry)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _telemetry = telemetry;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxDispatcher iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro inesperado no OutboxDispatcher.");
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
                }
            }

            _logger.LogInformation("OutboxDispatcher encerrado.");
        }

        private async Task ProcessPendingMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var pendingMessages = (await outboxRepo.GetPendingAsync(BatchSize, stoppingToken)).ToList();

            if (!pendingMessages.Any())
            {
                await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
                return;
            }

            foreach (var message in pendingMessages)
            {
                await HandleMessageAsync(message, outboxRepo, stoppingToken);
            }
        }

        private async Task HandleMessageAsync(
            OutboxMessage message,
            IOutboxRepository outboxRepo,
            CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var activityContext = CreateActivityContext(message);
                using var activity = ActivitySource.StartActivity("PublishOutboxMessage", ActivityKind.Producer, activityContext);

                _telemetry.RestoreBaggage(activity, message.Baggage);

                var payload = DeserializePayload(message);

                await publisher.Publish(payload, context =>
                {
                    _telemetry.InjectTraceContext(context, activity);
                }, stoppingToken);

                await outboxRepo.MarkPublishedAsync(message.Id, stoppingToken);
                _logger.LogInformation("Outbox {Id} publicada com sucesso (TraceId={TraceId}).",
                    message.Id, activity?.TraceId);
            }
            catch (Exception ex)
            {
                await HandlePublishFailureAsync(message, outboxRepo, ex, stoppingToken);
            }
        }

        private static ActivityContext CreateActivityContext(OutboxMessage msg)
        {
            if (string.IsNullOrEmpty(msg.TraceId) || string.IsNullOrEmpty(msg.SpanId))
                return default;

            return new ActivityContext(
                ActivityTraceId.CreateFromString(msg.TraceId.AsSpan()),
                ActivitySpanId.CreateFromString(msg.SpanId.AsSpan()),
                ActivityTraceFlags.Recorded);
        }

        private static object DeserializePayload(OutboxMessage msg)
        {
            var messageType = Type.GetType(msg.MessageType);
            if (messageType == null)
                throw new InvalidOperationException($"Tipo de mensagem desconhecido: {msg.MessageType}");

            return JsonSerializer.Deserialize(msg.Payload, messageType)
                   ?? throw new InvalidOperationException("Falha ao desserializar payload da outbox.");
        }

        private async Task HandlePublishFailureAsync(
            OutboxMessage msg,
            IOutboxRepository repo,
            Exception ex,
            CancellationToken token)
        {
            _logger.LogWarning(ex, "Falha ao publicar outbox {Id} (Tentativa {Attempt})", msg.Id, msg.Attempts + 1);

            await repo.IncrementAttemptAsync(msg.Id, token);

            if (msg.Attempts + 1 >= 3)
            {
                await repo.MarkFailedAsync(msg.Id, ex.ToString(), token);
                _logger.LogError("Outbox {Id} marcada como falha definitiva após 3 tentativas.", msg.Id);
            }
        }

        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            using var scope = _scopeFactory.CreateScope();
        //            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        //            var pending = (await outbox.GetPendingAsync(50, stoppingToken)).ToList();
        //            if (!pending.Any())
        //            {
        //                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        //                continue;
        //            }

        //            foreach (var msg in pending)
        //            {
        //                try
        //                {
        //                    using var innerScope = _scopeFactory.CreateScope();
        //                    var publishEndpoint = innerScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        //                    var type = Type.GetType(msg.MessageType);
        //                    if (type == null)
        //                        throw new InvalidOperationException("Unknown message type: " + msg.MessageType);

        //                    var payloadObj = JsonSerializer.Deserialize(msg.Payload, type)!;

        //                    // Reconstruir ActivityContext
        //                    ActivityContext parentContext = default;
        //                    if (!string.IsNullOrEmpty(msg.TraceId) && !string.IsNullOrEmpty(msg.SpanId))
        //                    {
        //                        parentContext = new ActivityContext(
        //                            ActivityTraceId.CreateFromString(msg.TraceId.AsSpan()),
        //                            ActivitySpanId.CreateFromString(msg.SpanId.AsSpan()),
        //                            ActivityTraceFlags.Recorded
        //                        );
        //                    }

        //                    using var activity = ActivitySource.StartActivity("PublishOutboxMessage", ActivityKind.Producer, parentContext);

        //                    // Recriar baggage
        //                    if (!string.IsNullOrEmpty(msg.Baggage))
        //                    {
        //                        var baggageItems = JsonSerializer.Deserialize<Dictionary<string, string>>(msg.Baggage);
        //                        if (baggageItems != null)
        //                        {
        //                            foreach (var item in baggageItems)
        //                                activity?.AddBaggage(item.Key, item.Value);
        //                        }
        //                    }

        //                    await publishEndpoint.Publish(payloadObj, context =>
        //                    {
        //                        var currentActivity = Activity.Current;
        //                        if (currentActivity != null)
        //                        {
        //                            // Passa traceparent e tracestate atuais
        //                            context.Headers.Set("traceparent", currentActivity.Id);
        //                            if (!string.IsNullOrEmpty(currentActivity.TraceStateString))
        //                                context.Headers.Set("tracestate", currentActivity.TraceStateString);

        //                            // Propaga baggage
        //                            foreach (var item in currentActivity.Baggage)
        //                                context.Headers.Set(item.Key, item.Value);
        //                        }
        //                    }, stoppingToken);

        //                    await outbox.MarkPublishedAsync(msg.Id, stoppingToken);
        //                    _logger.LogInformation("Published outbox message {Id}", msg.Id);
        //                }
        //                catch (Exception ex)
        //                {
        //                    _logger.LogWarning(ex, "Failed to publish outbox message {Id}", msg.Id);
        //                    await outbox.IncrementAttemptAsync(msg.Id, stoppingToken);

        //                    if (msg.Attempts + 1 >= 3)
        //                    {
        //                        await outbox.MarkFailedAsync(msg.Id, ex.ToString(), stoppingToken);
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "OutboxDispatcher failed");
        //            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        //        }
        //    }
        //}
    }
}
