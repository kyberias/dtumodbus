using MQTTnet;
using MQTTnet.Client.Options;
using System.Text;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DtuModbus
{
    public class ModbusToMqtt
    {
        private readonly ILogger<ModbusToMqtt> log;
        private readonly IConfiguration config;
        private readonly IMqttFactory factory;
        private readonly IDtuModbus dtu;

        public ModbusToMqtt(ILogger<ModbusToMqtt> log, IConfiguration config, IMqttFactory factory, IDtuModbus dtu)
        {
            this.log = log;
            this.config = config;
            this.factory = factory;
            this.dtu = dtu;
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            var mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("DtuModBus")
                .WithTcpServer(config["mqtt:broker"])
                .WithCredentials(config["mqtt:username"], config["mqtt:password"])
                .Build();

            var totalPowerTopic = config["mqtt:totalPowerTopic"];
            var totalTodayProductionTopic = config["mqtt:totalTodayProductionTopic"];

            var numPanels = int.Parse(config["panels:numPanels"]!);

            while (!stoppingToken.IsCancellationRequested)
            {
                using var mqttClient = factory.CreateMqttClient();

                try
                {
                    var res = await mqttClient.ConnectAsync(mqttOptions, stoppingToken);

                    var panels = await dtu.ReadPanels(numPanels).ToListAsync(stoppingToken);

                    var totalPower = panels.Sum(p => p.Power);
                    log.LogInformation($"Total power: {totalPower} W");
                    totalPower = Math.Round(totalPower, 1);

                    var totalTodayProduction = panels.Sum(p => p.TodayProduction);
                    log.LogInformation($"Total today production: {totalTodayProduction} Wh");

                    await mqttClient.PublishAsync(new MqttApplicationMessage
                    {
                        Topic = totalPowerTopic,
                        Payload = Encoding.ASCII.GetBytes(totalPower.ToString(CultureInfo.InvariantCulture))
                    }, stoppingToken);

                    await mqttClient.PublishAsync(new MqttApplicationMessage
                    {
                        Topic = totalTodayProductionTopic,
                        Payload = Encoding.ASCII.GetBytes(totalTodayProduction.ToString(CultureInfo.InvariantCulture))
                    }, stoppingToken);

                    log.LogDebug("Waiting for a minute before next read.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                    continue;
                }
                catch (OperationCanceledException)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    log.LogWarning("Communication timeout");
                }
                catch (Exception ex)
                {
                    log.LogError("Error: " + ex.Message);
                }
                log.LogDebug("Waiting for 10 sec. before next try.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}