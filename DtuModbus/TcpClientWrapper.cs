using System.Net.Sockets;

namespace DtuModbus
{
    class TcpClientWrapper : ITcpClient
    {
        TcpClient client;

        public TcpClientWrapper(TcpClient client)
        {
            this.client = client;
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public Stream GetStream()
        {
            return client.GetStream();
        }
    }
}