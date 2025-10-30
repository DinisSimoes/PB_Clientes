using MediatR;
using PB_Clientes.Domain.Repositories;

namespace PB_Clientes.Application.Commands.DeleteCliente
{
    public class DeleteClienteHandler : IRequestHandler<DeleteClienteCommand, Unit>
    {
        private readonly IClienteRepository _repo;
        public DeleteClienteHandler(IClienteRepository repo) => _repo = repo;

        public async Task<Unit> Handle(DeleteClienteCommand request, CancellationToken cancellationToken)
        {
            if (!await _repo.ExistsAsync(request.Id, cancellationToken))
                return Unit.Value;

            await _repo.DeleteAsync(request.Id, cancellationToken);
            return Unit.Value;
        }
    }
}
