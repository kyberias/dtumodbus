using System.Threading.Tasks.Dataflow;

namespace DtuModbus.Test;

public class FakeStream : Stream
{
    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = readBuf.Receive();
        }

        return count;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = await readBuf.ReceiveAsync(cancellationToken);
        }

        return count;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    BufferBlock<byte> writeBuf = new BufferBlock<byte>();
    BufferBlock<byte> readBuf = new BufferBlock<byte>();

    public override void Write(byte[] buffer, int offset, int count)
    {
        for (int i = offset; i < offset + count; i++)
        {
            writeBuf.Post(buffer[i]);
        }
    }

    public Task<byte> ReadFromWriteBuf(CancellationToken cancel)
    {
        return writeBuf.ReceiveAsync(cancel);
    }

    public async Task<byte[]> ReadFromWriteBuf(int numBytes, CancellationToken cancel)
    {
        var buf = new byte[numBytes];
        for (int i = 0; i < numBytes; i++)
        {
            buf[i] = await writeBuf.ReceiveAsync(cancel);
        }

        return buf;
    }

    public void WriteToReadBuf(byte[] bytes)
    {
        foreach (var b in bytes)
        {
            readBuf.Post(b);
        }
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length { get; }
    public override long Position { get; set; }
}