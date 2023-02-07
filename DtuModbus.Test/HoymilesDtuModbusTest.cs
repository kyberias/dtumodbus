using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DtuModbus.Test;

public class HoymilesDtuModbusTest
{
    [Fact]
    public async Task ShouldReadPanelInfo()
    {
        var config = new Mock<IConfiguration>();
        var log = new Mock<ILogger<HoymilesDtuModbus>>();
        var factory = new Mock<ITcpClientFactory>();
        var client = new Mock<ITcpClient>();

        var stream = new FakeStream();
        client.Setup(c => c.GetStream()).Returns(stream);

        factory.Setup(f => f.Create("hostname", 123)).Returns(client.Object);

        config.SetupGet(x => x[It.Is<string>(s => s == "dtu:hostname")]).Returns("hostname");
        config.SetupGet(x => x[It.Is<string>(s => s == "dtu:port")]).Returns("123");

        var dtuModbus = new HoymilesDtuModbus(config.Object, log.Object, factory.Object);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var task = dtuModbus.ReadPanels(2).ToArrayAsync(timeout.Token);

        var cmd = await stream.ReadFromWriteBuf(12, timeout.Token);

        var response = new byte[]
        {
            0, 0,
            0, 0,
            0, 0, 0,
            0x03, 24,
            0, 0,
            0, 0,
            0, 0,
            0, 0,
            0, 0,
            0, 0,
            0, 0,
            0, 0,
            0, 255,
            0, 155,
            0, 0,
            0, 0,
        };

        stream.WriteToReadBuf(response);

        cmd = await stream.ReadFromWriteBuf(12, timeout.Token);

        var response2 = new byte[]
        {
            0, 0,
            0, 0,
            0, 0, 0,
            0x03, 24,
            0, 0,
            0, 0,
            0, 0,
            0, 0,
            0, 0,
            0, 0,
            0, 0,
            0, 0,
            0, 225,
            0, 125,
            0, 0,
            0, 0,
        };

        stream.WriteToReadBuf(response2);

        var panels = await task;

        Assert.Equal(22.5, panels[1].Power);
        Assert.Equal(125, panels[1].TodayProduction);
    }
}