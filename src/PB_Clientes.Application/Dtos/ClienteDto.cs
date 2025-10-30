namespace PB_Clientes.Application.Dtos
{
    public record ClienteDto(Guid Id, string Nome, string Cpf, DateTime DataCriacao);
}
