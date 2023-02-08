using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DtuModbus
{
    /// <summary>
    /// Modbus TCP implementation for Hoymiles 3Gen DTU-Pro
    /// See Technical note version 1.2 (December 2020)
    /// </summary>
    public class HoymilesDtuModbus : IDtuModbus
    {
        private readonly string hostname;
        private readonly int port;
        private readonly ILogger<HoymilesDtuModbus> log;
        private readonly ITcpClientFactory tcpClientFactory;

        public HoymilesDtuModbus(IConfiguration config, ILogger<HoymilesDtuModbus> log, ITcpClientFactory tcpClientFactory)
        {
            this.hostname = config["dtu:hostname"]!;
            this.port = int.Parse(config["dtu:port"]!);
            this.log = log;
            this.tcpClientFactory = tcpClientFactory;
        }

        public async IAsyncEnumerable<PanelInfo> ReadPanels(int numPanels)
        {
            using (var client = tcpClientFactory.Create(hostname, port))
            {
                using (var stream = client.GetStream())
                {
                    //await Task.Delay(TimeSpan.FromSeconds(0.5));

                    for (int i = 0; i < numPanels; i++)
                    {
                        yield return await ReadPanelInfo(i, stream);
                        //await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }
            }
        }

        async Task<PanelInfo> ReadPanelInfo(int panelIndex, Stream stream)
        {
            using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            ushort addr = (ushort)(0x1000 + 0x28 * panelIndex);

            var payload = await ReadRegisters(stream, addr, 12, cancel.Token);

            /*for (int i = 0; i < payload.Length; i++)
                {
                    Console.WriteLine("{0:X4} {1:X2}", addr, payload[i]);
                    addr++;
                }*/

            var inverterPort = payload[0x7];
            log.LogTrace($"Port: {inverterPort}");

            var pvVoltage = (payload[0x8] * 256 + payload[0x8]) / 10.0;
            log.LogTrace($"PV Voltage: {pvVoltage} V");
            var pvCurrent = (payload[0xA] * 256 + payload[0xB]) / 100.0;  // 2 decimals for HM-series
            log.LogTrace($"PV Current: {pvCurrent} A");
            var gridVoltage = (payload[0xC] * 256 + payload[0xD]) / 10.0;
            log.LogTrace($"Grid Voltage: {gridVoltage} V");
            var pvPower = (payload[0x10] * 256 + payload[0x11]) / 10.0;
            log.LogTrace($"PV Power: {pvPower} W");
            var todayProduction = (payload[0x12] * 256 + payload[0x13]);
            log.LogTrace($"Today production: {todayProduction} Wh");

            return new PanelInfo
            {
                Power = pvPower,
                Voltage = pvVoltage,
                Current = pvCurrent,
                TodayProduction = todayProduction
            };
        }

        Task ReadAndIgnore(Stream stream, int bytes, CancellationToken token)
        {
            var buf = new byte[bytes];
            return stream.ReadAsync(buf, 0, bytes, token);
        }

        async Task<byte[]> Read(Stream stream, int bytes, CancellationToken token)
        {
            var buf = new byte[bytes];
            int n = 0;
            while (n < bytes)
            {
                n += await stream.ReadAsync(buf, n, bytes - n, token);
            }

            return buf;
        }

        private const byte ModbusFcReadMultipleRegisters = 0x03;

        async Task<byte[]> ReadRegisters(Stream stream, ushort addr, byte num, CancellationToken token)
        {
            // 5.2.1 Read Single Device Status
            var msg = new byte[]
            {
                0, 1,
                0, 0,
                0, 6,
                255, ModbusFcReadMultipleRegisters,
                (byte)((addr >> 8) & 0xFF), (byte)(addr & 0xFF),
                0, num
            };

            stream.Write(msg);

            //response
            // 0-1 Transaction ID 
            // 2-3 Protocol ID
            // 4-5 Length
            // 6 Unit ID
            // 7 Function Code
            // 8 Data length
            // 9-.. Data...

            // Ignore most fields for now.
            await ReadAndIgnore(stream, 7, token);

            var buf = await Read(stream, 1, token);

            if (buf[0] != ModbusFcReadMultipleRegisters)
            {
                await ReadAndIgnore(stream, 1, token);
                throw new Exception("Protocol error");
            }

            var lenBuf = await Read(stream, 1, token);
            var len2 = lenBuf[0];

            log.LogDebug($"Read payload length: {len2}");

            return await Read(stream, len2, token);
        }
    }
}