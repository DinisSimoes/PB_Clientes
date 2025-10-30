using MediatR;

namespace PB_Clientes.Application.Commands.DeleteCliente
{
    public record DeleteClienteCommand(Guid Id) : IRequest<Unit>;
}
