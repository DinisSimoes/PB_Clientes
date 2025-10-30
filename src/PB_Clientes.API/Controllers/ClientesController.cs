using MediatR;
using Microsoft.AspNetCore.Mvc;
using PB_Clientes.Application.Commands.CreateCliente;
using PB_Clientes.Application.Commands.DeleteCliente;
using PB_Clientes.Application.Dtos;
using PB_Clientes.Application.Queries.GetAllClientes;

namespace PB_Clientes.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ClientesController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateClienteDto dto)
        {
            var cmd = new CreateClienteCommand(dto.Nome, dto.Cpf);
            var id = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetAll), new { id }, new { id });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteClienteCommand(id));
            return NoContent();
        }

        [HttpGet]
        public async Task<IEnumerable<ClienteDto>> GetAll()
        {
            return await _mediator.Send(new GetAllClientesQuery());
        }
    }
}

public record CreateClienteDto(string Nome, string Cpf);
