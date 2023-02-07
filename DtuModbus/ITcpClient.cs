namespace DtuModbus
{
    public interface ITcpClient : IDisposable
    {
        Stream GetStream();
    }
}