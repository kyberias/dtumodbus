using Microsoft.Extensions.Hosting;

namespace DtuModbus
{
    class DtuModbusService : BackgroundService
    {
        readonly ModbusToMqtt programLogic;

        public DtuModbusService(ModbusToMqtt programLogic)
        {
            this.programLogic = programLogic;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return programLogic.Run(stoppingToken);
       }
    }
}