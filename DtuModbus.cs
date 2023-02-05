using System.Net.Sockets;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System.Text;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DtuModbus
{
    class DtuModbus : BackgroundService
    {
        readonly ILogger<DtuModbus> log;
        readonly IConfiguration config;

        public DtuModbus(ILogger<DtuModbus> log, IConfiguration config)
        {
            this.log = log;
            this.config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("DtuModBus")
                .WithTcpServer(config["mqtt:broker"])
                .WithCredentials(config["mqtt:username"], config["mqtt:password"])
                .Build();

            var totalPowerTopic = config["mqtt:totalPowerTopic"];
            var totalTodayProductionTopic = config["mqtt:totalTodayProductionTopic"];
            var dtuHostname = config["dtu:hostname"]!;
            var dtuPort = int.Parse(config["dtu:port"]!);

            var numPanels = int.Parse(config["panels:numPanels"]!);

            var factory = new MqttFactory();
            var dtu = new Dtu(dtuHostname, dtuPort, log);

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var mqttClient = factory.CreateMqttClient())
                {
                    try
                    {
                        var res = await mqttClient.ConnectAsync(mqttOptions, stoppingToken);
                        var panels = await dtu.ReadPanels(numPanels).ToListAsync();

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
}