using MediatR;

namespace PB_Clientes.Application.Commands.CreateCliente
{
    public record CreateClienteCommand(string Nome, string Cpf) : IRequest<Guid>;
}
