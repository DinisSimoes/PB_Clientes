using MediatR;
using PB_Clientes.Application.Dtos;

namespace PB_Clientes.Application.Queries.GetAllClientes
{
    public record GetAllClientesQuery() : IRequest<IEnumerable<ClienteDto>>;
}
