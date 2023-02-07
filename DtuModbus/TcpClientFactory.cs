namespace DtuModbus
{
    class TcpClientFactory : ITcpClientFactory
    {
        public ITcpClient Create(string hostname, int port)
        {
            return new TcpClientWrapper(new System.Net.Sockets.TcpClient(hostname, port));
        }
    }
}