using Moq;
using PB_Clientes.Application.Repositories;
using PB_Clientes.Application.Services;
using PB_Clientes.Domain.Entities;
using PB_Clientes.Domain.Repositories;
using PB_Common.Events;
using System.Text.Json;

namespace PB_Clientes.UnitTests.Services
{
    public class ClienteServiceTests
    {
        private readonly Mock<IClienteRepository> _clienteRepoMock;
        private readonly Mock<IOutboxRepository> _outboxRepoMock;
        private readonly ClienteService _service;

        public ClienteServiceTests()
        {
            _clienteRepoMock = new Mock<IClienteRepository>();
            _outboxRepoMock = new Mock<IOutboxRepository>();
            _service = new ClienteService(_clienteRepoMock.Object, _outboxRepoMock.Object);
        }

        [Theory]
        [InlineData("João Silva", "12345678901")]
        [InlineData("Maria Souza", "98765432100")]
        public async Task CriarClienteAsync_DeveCriarClienteEAdicionarOutbox(string nome, string cpf)
        {
            // Arrange
            Cliente? addedCliente = null;
            OutboxMessage? addedOutbox = null;

            _clienteRepoMock.Setup(x => x.AddAsync(It.IsAny<Cliente>(), default))
                .Callback<Cliente, CancellationToken>((c, _) => addedCliente = c)
                .Returns(Task.CompletedTask);

            _outboxRepoMock.Setup(x => x.AddAsync(It.IsAny<OutboxMessage>(), default))
                .Callback<OutboxMessage, CancellationToken>((o, _) => addedOutbox = o)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CriarClienteAsync(nome, cpf);

            // Assert - Cliente
            Assert.NotNull(result);
            Assert.Equal(nome, result.Nome);
            Assert.Equal(cpf, result.Cpf);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(addedCliente, result);

            // Assert
            Assert.NotNull(addedOutbox);
            var evt = JsonSerializer.Deserialize<ClienteCadastradoEvent>(addedOutbox.Payload);
            Assert.NotNull(evt);
            Assert.Equal(result.Id, evt!.ClienteId);
            Assert.Equal(result.Nome, evt.Nome);
            Assert.Equal(result.Cpf, evt.Cpf);
            Assert.Equal("Pending", addedOutbox.Status);
        }
    }
}
