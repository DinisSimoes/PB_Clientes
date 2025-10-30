using MediatR;
using Microsoft.Extensions.Logging;
using PB_Clientes.Application.Interfaces;
using PB_Clientes.Application.Services;
using System.Diagnostics;
using System.Text.Json;

namespace PB_Clientes.Application.Commands.CreateCliente
{
    public class CreateClienteHandler : IRequestHandler<CreateClienteCommand, Guid>
    {
        private readonly ClienteService _clienteService;
        private readonly ITelemetryService _telemetry;
        private readonly ILogger<CreateClienteHandler> _logger;

        public CreateClienteHandler(
            ClienteService clienteService,
            ITelemetryService telemetry,
            ILogger<CreateClienteHandler> logger)
        {
            _clienteService = clienteService;
            _telemetry = telemetry;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateClienteCommand request, CancellationToken cancellationToken)
        {
            var startTime = Stopwatch.GetTimestamp();

            using var activity = _telemetry.StartActivity(
                "CreateClienteHandler",
                ActivityKind.Server,
                tags: new Dictionary<string, object?>
                {
                    ["operation"] = "create_cliente",
                    ["request.nome"] = request.Nome,
                    ["request.cpf"] = request.Cpf
                });

            try
            {
                var cliente = await _clienteService.CriarClienteAsync(
                    request.Nome,
                    request.Cpf,
                    activity?.TraceId.ToString(),
                    activity?.SpanId.ToString(),
                    JsonSerializer.Serialize(activity?.Baggage.ToDictionary(b => b.Key, b => b.Value)),
                    cancellationToken);

                var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
                activity?.SetTag("execution.time_ms", elapsedMs);
                activity?.SetStatus(ActivityStatusCode.Ok);

                _logger.LogInformation(
                    "Cliente {ClienteId} criado com sucesso. TraceId={TraceId}, Tempo={ElapsedMs}ms",
                    cliente.Id, activity?.TraceId, elapsedMs);

                return cliente.Id;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Erro ao criar cliente. TraceId={TraceId}", activity?.TraceId);
                throw;
            }
        }
    }
}
