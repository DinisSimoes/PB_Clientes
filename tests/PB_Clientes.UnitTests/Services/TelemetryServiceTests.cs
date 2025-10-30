using PB_Clientes.Application.Services;
using System.Diagnostics;
using System.Text.Json;

namespace PB_Clientes.UnitTests.Services
{
    public class TelemetryServiceTests
    {
        private readonly TelemetryService _service;

        public TelemetryServiceTests()
        {
            _service = new TelemetryService();
        }

        [Fact]
        public void StartActivity_ShouldCreateActivityWithTags()
        {
            // Arrange
            var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "PB_Clientes.Api",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            var tags = new Dictionary<string, object?>
            {
                ["key1"] = "value1",
                ["key2"] = "123"
            };

            // Act
            using var activity = _service.StartActivity("TestActivity", ActivityKind.Internal, null, tags);

            // Assert
            Assert.NotNull(activity);
            Assert.Equal("TestActivity", activity.DisplayName);
            Assert.Equal("value1", activity.Tags.First(t => t.Key == "key1").Value);
            Assert.Equal("123", activity.Tags.First(t => t.Key == "key2").Value);
        }

        [Fact]
        public void RestoreBaggage_ShouldAddItemsToActivity()
        {
            // Arrange
            using var activity = new Activity("TestBaggage");
            activity.Start();

            var baggageDict = new Dictionary<string, string>
            {
                ["b1"] = "v1",
                ["b2"] = "v2"
            };
            var json = JsonSerializer.Serialize(baggageDict);

            // Act
            _service.RestoreBaggage(activity, json);

            // Assert
            Assert.Equal("v1", activity.Baggage.First(b => b.Key == "b1").Value);
            Assert.Equal("v2", activity.Baggage.First(b => b.Key == "b2").Value);
        }
    }
}
