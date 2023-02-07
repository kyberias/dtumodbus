namespace DtuModbus
{
    public interface ITcpClientFactory
    {
        ITcpClient Create(string hostname, int port);
    }
}