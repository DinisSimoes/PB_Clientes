using MediatR;
using PB_Clientes.Application.Dtos;
using PB_Clientes.Domain.Repositories;

namespace PB_Clientes.Application.Queries.GetAllClientes
{
    public class GetAllClientesHandler : IRequestHandler<GetAllClientesQuery, IEnumerable<ClienteDto>>
    {
        private readonly IClienteRepository _repo;
        public GetAllClientesHandler(IClienteRepository repo) => _repo = repo;

        public async Task<IEnumerable<ClienteDto>> Handle(GetAllClientesQuery request, CancellationToken cancellationToken)
        {
            var clientes = await _repo.GetAllAsync(cancellationToken);
            return clientes.Select(c => new ClienteDto(c.Id, c.Nome, c.Cpf, c.DataCriacao));
        }
    }
}
