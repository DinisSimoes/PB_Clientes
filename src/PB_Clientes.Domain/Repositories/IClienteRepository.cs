using PB_Clientes.Domain.Entities;

namespace PB_Clientes.Domain.Repositories
{
    public interface IClienteRepository
    {
        Task AddAsync(Cliente cliente, CancellationToken cancellationToken);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Cliente>> GetAllAsync(CancellationToken cancellationToken);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    }
}
