/*using System.IO;

namespace DiscordBot
{
    public class DualStream : MemoryStream
    {
        public long ReadPosition = 0;
        public long WritePostion = 0;
        //private object Locker = new object();

        public override int Read(byte[] Buffer, int Offset, int Count)
        {
            //lock (this.Locker)
            lock (this)
                {
                this.Position = this.ReadPosition;
                int Read = base.Read(Buffer, Offset, Count);
                this.ReadPosition = this.Position;
                return Read;
            }
        }

        public override void Write(byte[] Buffer, int Offset, int Count)
        {
            //lock (this.Locker)
            lock (this)
            {
                this.Position = this.WritePostion;
                base.Write(Buffer, Offset, Count);
                this.WritePostion = this.Position;
            }
        }
    }
}*/
