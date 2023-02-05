using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace DtuModbus
{
    /// <summary>
    /// Modbus TCP implementation for Hoymiles 3Gen DTU-Pro
    /// See Technical note version 1.2 (December 2020)
    /// </summary>
    class Dtu
    {
        string hostname;
        int port;
        ILogger log;

        public Dtu(string hostname, int port, ILogger log)
        {
            this.hostname = hostname;
            this.port = port;
            this.log = log;
        }

        public async IAsyncEnumerable<PanelInfo> ReadPanels(int numPanels)
        {
            using (var client = new TcpClient(hostname, port))
            {
                using (var stream = client.GetStream())
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.5));

                    for (int i = 0; i < numPanels; i++)
                    {
                        yield return await ReadPanelInfo(i, stream);
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }
            }
        }

        async Task<PanelInfo> ReadPanelInfo(int panelIndex, Stream stream)
        {
            using (var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                ushort addr = (ushort)(0x1000 + 0x28 * panelIndex);

                var payload = await ReadRegisters(stream, addr, 12, cancel.Token);

                /*for (int i = 0; i < payload.Length; i++)
                {
                    Console.WriteLine("{0:X4} {1:X2}", addr, payload[i]);
                    addr++;
                }*/

                var port = payload[0x7];
                log.LogTrace($"Port: {port}");

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
        }

        async Task<byte[]> ReadRegisters(Stream stream, ushort addr, byte num, CancellationToken token)
        {
            // 5.2.1 Read Single Device Status
            var msg = new byte[12];

            // transaction id
            msg[0] = 0;
            msg[1] = 1;
            // protocol id, 0 for modbus TCP
            msg[2] = 0;
            msg[3] = 0;

            // length
            msg[4] = 0;
            msg[5] = 6;

            // unit id, server address
            msg[6] = 255;

            // function code
            msg[7] = 0x03;

            msg[8] = (byte)((addr >> 8) & 0xFF);
            msg[9] = (byte)(addr & 0xFF);

            // number of registers
            msg[10] = 0;
            msg[11] = num;

            stream.Write(msg);

            //response
            // 0-1 Transaction ID 
            // 2-3 Protocol ID
            // 4-5 Length
            // 6 Unit ID
            // 7 Function Code
            // 8 Data length
            // 9-.. Data...

            byte[] buf = new byte[2];

            await stream.ReadAsync(buf, 0, 2, token);
            await stream.ReadAsync(buf, 0, 2, token);
            await stream.ReadAsync(buf, 0, 2, token);

            var len = buf[0] * 256 + buf[1];

            await stream.ReadAsync(buf, 0, 1, token);

            await stream.ReadAsync(buf, 0, 1, token);

            if (buf[0] != 0x03)
            {
                await stream.ReadAsync(buf, 0, 1, token);
                throw new Exception("Protocol error");
            }

            await stream.ReadAsync(buf, 0, 1, token);
            var len2 = buf[0];

            var data = new byte[len2];
            await stream.ReadAsync(data, 0, len2, token);
            return data;
        }
    }
}