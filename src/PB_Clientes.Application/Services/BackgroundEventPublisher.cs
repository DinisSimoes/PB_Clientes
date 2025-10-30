using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PB_Common.Events;
using System.Collections.Concurrent;
using System.Text.Json;

namespace PB_Clientes.Application.Services
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T evt);
    }

    public class BackgroundEventPublisher : BackgroundService, IEventPublisher
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackgroundEventPublisher> _logger;
        private readonly ConcurrentQueue<object> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);

        public BackgroundEventPublisher(IServiceScopeFactory scopeFactory, ILogger<BackgroundEventPublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task PublishAsync<T>(T evt)
        {
            _queue.Enqueue(evt! as object);
            _signal.Release();
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (_queue.TryDequeue(out var item))
                {
                    _ = ProcessAsync(item, stoppingToken); // fire-and-forget processing to allow parallelism
                }
            }
        }

        private async Task ProcessAsync(object item, CancellationToken token)
        {
            var maxAttempts = 3;
            var attempt = 0;
            var serialized = JsonSerializer.Serialize(item);
            var messageTypeName = item.GetType().AssemblyQualifiedName ?? item.GetType().FullName ?? string.Empty;

            while (attempt < maxAttempts && !token.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, token);

                    using var scope = _scopeFactory.CreateScope();
                    var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                    await publishEndpoint.Publish(item, linked.Token);
                    _logger.LogInformation("Published event {Type} on attempt {Attempt}", item.GetType().Name, attempt);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Publish attempt {Attempt} failed for {Type}", attempt, item.GetType().Name);
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt), token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            // after retries, publish failure event so orchestrator knows
            Guid clienteId = ExtractClienteId(item);

            var failure = new ClienteFalhaEvent(
                Guid.NewGuid(),
                clienteId,
                messageTypeName,
                serialized,
                Attempt: attempt,
                Reason: "Publish retries exhausted",
                OccurredUtc: DateTime.UtcNow
            );

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                await publishEndpoint.Publish(failure);
                _logger.LogInformation("Published failure event for original {Type}", item.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish failure event for {Type}", item.GetType().Name);
            }
        }

        private static Guid ExtractClienteId(object item)
        {
            try
            {
                var prop = item.GetType().GetProperty("ClienteId") ?? item.GetType().GetProperty("Id");
                if (prop != null && prop.GetValue(item) is Guid g)
                    return g;
            }
            catch { }
            return Guid.Empty;
        }
    }
}
