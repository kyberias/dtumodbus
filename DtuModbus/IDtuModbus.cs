namespace DtuModbus
{
    public interface IDtuModbus
    {
        public IAsyncEnumerable<PanelInfo> ReadPanels(int numPanels);
    }
}