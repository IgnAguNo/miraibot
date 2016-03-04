using System.Collections.Concurrent;

namespace DiscordBot
{
    class ByteBuffer
    {
        private int BufferSize;
        private int MaxQueued;
        private ConcurrentStack<byte[]> Buffers = new ConcurrentStack<byte[]>();

        public ByteBuffer(int UseBufferSize, int UseMaxQueued)
        {
            BufferSize = UseBufferSize;
            MaxQueued = UseMaxQueued;
        }

        public byte[] Take()
        {
            byte[] Return;
            if (!Buffers.TryPop(out Return))
            {
                Return = new byte[BufferSize];
            }

            return Return;
        }

        public void Return(byte[] ToReturn)
        {
            if (Buffers.Count < MaxQueued)
            {
                Buffers.Push(ToReturn);
            }
        }
    }
}
