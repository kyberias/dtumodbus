using System.Text;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Publishing;

namespace DtuModbus.Test
{
    public class ModbusToMqttTest
    {
        [Fact]
        public async Task ShouldPublishCorrectPowerAndProductionFromMqtt()
        {
            var config = new Mock<IConfiguration>();
            var log = new Mock<ILogger<ModbusToMqtt>>();
            var factory = new Mock<IMqttFactory>();
            var client = new Mock<IMqttClient>();

            factory.Setup(f => f.CreateMqttClient()).Returns(client.Object);

            var dtuModbus = new Mock<IDtuModbus>();

            const string powerTopic = "solar/power";
            const string energyTopic = "solar/energy";
            const int numPanels = 2;

            config.SetupGet(x => x[It.Is<string>(s => s == "panels:numPanels")]).Returns(numPanels.ToString());
            config.SetupGet(x => x[It.Is<string>(s => s == "mqtt:totalPowerTopic")]).Returns(powerTopic);
            config.SetupGet(x => x[It.Is<string>(s => s == "mqtt:totalTodayProductionTopic")]).Returns(energyTopic);

            var data = new[]
            {
                new PanelInfo { Power = 10, TodayProduction = 100 },
                new PanelInfo { Power = 20, TodayProduction = 150 }
            };

            var expectedTotalPower = data.Sum(d => d.Power);
            var expectedTotalEnergy = data.Sum(d => d.TodayProduction);

            dtuModbus.Setup(d => d.ReadPanels(numPanels)).Returns(data.ToAsyncEnumerable());

            var m = new ModbusToMqtt(log.Object, config.Object, factory.Object, dtuModbus.Object);

            var messages = new BufferBlock<MqttApplicationMessage>();

            client.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
                .Callback((MqttApplicationMessage msg, CancellationToken token) =>
                {
                    messages.Post(msg);
                }).Returns(Task.FromResult(new MqttClientPublishResult()));

            using (var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                var task = m.Run(cancel.Token);

                var msgs = new[]
                {
                    await messages.ReceiveAsync(cancel.Token),
                    await messages.ReceiveAsync(cancel.Token)
                };

                cancel.Cancel();

                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {

                }

                var power = Encoding.ASCII.GetString(msgs.Single(m => m.Topic == powerTopic).Payload);
                var energy = Encoding.ASCII.GetString(msgs.Single(m => m.Topic == energyTopic).Payload);

                Assert.Equal(expectedTotalPower, double.Parse(power));
                Assert.Equal(expectedTotalEnergy, double.Parse(energy));
            }
        }
    }
}