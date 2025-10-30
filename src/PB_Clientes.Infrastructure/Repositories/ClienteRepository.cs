using Microsoft.EntityFrameworkCore;
using PB_Clientes.Domain.Entities;
using PB_Clientes.Domain.Repositories;
using PB_Clientes.Infrastructure.Data;

namespace PB_Clientes.Infrastructure.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly ClientesDbContext _ctx;
        public ClienteRepository(ClientesDbContext ctx) => _ctx = ctx;

        public async Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            _ctx.Clientes.Add(cliente);
            await _ctx.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _ctx.Clientes.FindAsync(new object[] { id }, cancellationToken);
            if (entity != null)
            {
                _ctx.Clientes.Remove(entity);
                await _ctx.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<Cliente>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _ctx.Clientes
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _ctx.Clientes
                .AnyAsync(c => c.Id == id, cancellationToken);
        }
    }
}
