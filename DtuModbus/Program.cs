using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;

namespace DtuModbus
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging();
                services.AddHostedService<DtuModbusService>();
                services.AddSingleton<IDtuModbus, HoymilesDtuModbus>();
                services.AddSingleton<ITcpClientFactory, TcpClientFactory>();
                services.AddSingleton<IMqttFactory, MqttFactory>();
                services.AddSingleton<ModbusToMqtt>();
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                config.AddCommandLine(args);
                config.AddEnvironmentVariables();
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                //config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
            });
    }
}