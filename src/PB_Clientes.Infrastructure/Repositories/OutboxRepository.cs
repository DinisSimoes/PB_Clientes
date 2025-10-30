using Microsoft.EntityFrameworkCore;
using PB_Clientes.Application.Repositories;
using PB_Clientes.Domain.Entities;
using PB_Clientes.Infrastructure.Data;

namespace PB_Clientes.Infrastructure.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly ClientesDbContext _ctx;
        public OutboxRepository(ClientesDbContext ctx) => _ctx = ctx;

        public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            _ctx.OutboxMessages.Add(message);
            await _ctx.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<OutboxMessage>> GetPendingAsync(int max = 50, CancellationToken cancellationToken = default)
        {
            return await _ctx.OutboxMessages
                .Where(o => o.Status == "Pending")
                .OrderBy(o => o.OccurredUtc)
                .Take(max)
                .ToListAsync(cancellationToken);
        }

        public async Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var message = await _ctx.OutboxMessages.FindAsync(new object[] { id }, cancellationToken);
            if (message != null)
            {
                message.Status = "Published";
                message.ProcessedAt = DateTime.UtcNow;
                await _ctx.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
        {
            var message = await _ctx.OutboxMessages.FindAsync(new object[] { id }, cancellationToken);
            if (message != null)
            {
                message.Status = "Failed";
                message.LastError = error;
                message.ProcessedAt = DateTime.UtcNow;
                await _ctx.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task IncrementAttemptAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var message = await _ctx.OutboxMessages.FindAsync(new object[] { id }, cancellationToken);
            if (message != null)
            {
                message.Attempts++;
                await _ctx.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
