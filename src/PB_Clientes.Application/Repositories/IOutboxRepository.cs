using PB_Clientes.Domain.Entities;

namespace PB_Clientes.Application.Repositories
{
    public interface IOutboxRepository
    {
        Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);
        Task<IEnumerable<OutboxMessage>> GetPendingAsync(int max, CancellationToken cancellationToken);
        Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken);
        Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken);
        Task IncrementAttemptAsync(Guid id, CancellationToken cancellationToken);
    }
}
