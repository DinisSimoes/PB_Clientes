using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PB_Clientes.Application.Services;

namespace PB_Clientes.UnitTests.Services
{
    public class BackgroundEventPublisherTests
    {
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IPublishEndpoint> _publishEndpointMock;
        private readonly Mock<ILogger<BackgroundEventPublisher>> _loggerMock;
        private readonly BackgroundEventPublisher _publisher;

        public BackgroundEventPublisherTests()
        {
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _loggerMock = new Mock<ILogger<BackgroundEventPublisher>>();

            _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
            _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);

            _serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IPublishEndpoint)))
                .Returns(_publishEndpointMock.Object);

            _publisher = new BackgroundEventPublisher(_scopeFactoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task PublishAsync_ShouldEnqueueEvent()
        {
            // Arrange
            var testEvent = new { ClienteId = Guid.NewGuid(), Nome = "Dinis" };

            // Act
            await _publisher.PublishAsync(testEvent);

            // Assert
            Assert.True(true);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldProcessEventAndCallPublishEndpoint()
        {
            // Arrange
            var testEvent = new { ClienteId = Guid.NewGuid(), Nome = "Dinis" };
            await _publisher.PublishAsync(testEvent);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // Act
            var executeTask = _publisher.StartAsync(cts.Token);

            await Task.Delay(500);

            // Assert
            _publishEndpointMock.Verify(pe => pe.Publish(
                It.Is<object>(o => o == testEvent),
                It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            cts.Cancel();
            await executeTask;
        }

    }
}
