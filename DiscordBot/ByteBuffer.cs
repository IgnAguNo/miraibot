using System;
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
            if (ToReturn != null && ToReturn.Length == BufferSize && Buffers.Count < MaxQueued)
            {
                Array.Clear(ToReturn, 0, ToReturn.Length);
                Buffers.Push(ToReturn);
            }
        }
    }
}
