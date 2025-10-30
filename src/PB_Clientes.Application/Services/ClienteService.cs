using PB_Clientes.Application.Repositories;
using PB_Clientes.Domain.Entities;
using PB_Clientes.Domain.Repositories;
using PB_Common.Events;
using System.Text.Json;

namespace PB_Clientes.Application.Services
{
    public class ClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IOutboxRepository _outboxRepository;

        public ClienteService(IClienteRepository clienteRepository, IOutboxRepository outboxRepository)
        {
            _clienteRepository = clienteRepository;
            _outboxRepository = outboxRepository;
        }

        public async Task<Cliente> CriarClienteAsync(string nome, string cpf, string? traceId = null, string? spanId = null, string? baggage = null, CancellationToken cancellationToken = default)
        {
            var cliente = new Cliente(nome, cpf);
            await _clienteRepository.AddAsync(cliente, cancellationToken);

            var evento = new ClienteCadastradoEvent(cliente.Id, cliente.Nome, cliente.Cpf, cliente.DataCriacao);
            var payload = JsonSerializer.Serialize(evento);

            var outbox = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = evento.GetType().AssemblyQualifiedName ?? evento.GetType().FullName ?? string.Empty,
                Payload = payload,
                OccurredUtc = DateTime.UtcNow,
                Status = "Pending",
                Attempts = 0,
                TraceId = traceId,
                SpanId = spanId,
                Baggage = baggage
            };

            await _outboxRepository.AddAsync(outbox, cancellationToken);

            return cliente;
        }
    }
}
