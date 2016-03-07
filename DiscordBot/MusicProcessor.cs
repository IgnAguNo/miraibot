using DiscordBot.Handlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot
{
    class MusicProcessor
    {
        private static int MaxBuffer = (int)Math.Pow(2, 15);
        private const ushort BufferSize = 1920 * 2;

        public SongData Song;
        public bool Skip = false;
        public Queue<byte[]> QueuedBuffers = new Queue<byte[]>();
        private Process Ffmpeg;

        public long TotalSize = 0;
        public bool FinishedBuffer = false;

        public Semaphore Waiter = new Semaphore(MaxBuffer, MaxBuffer + 1);

        public MusicProcessor(SongData PlaySong)
        {
            Song = PlaySong;
            Ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-i \"" + this.Song.Uri + "\" -f s16le -ar 48000 -ac 2 pipe:1 -loglevel quiet",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            MainLoop();
        }

        private async void MainLoop()
        {
            int Read = 0;

            byte[] ReadBuffer = new byte[0];
            int ReadBufferUsed = 0;

            int Fails = 0;

            try
            {
                while (true)
                {
                    if (Skip)
                    {
                        MusicHandler.Buffers.Return(ReadBuffer);
                        break;
                    }

                    if (ReadBufferUsed == ReadBuffer.Length)
                    {
                        if (ReadBufferUsed != 0)
                        {
                            QueuedBuffers.Enqueue(ReadBuffer);
                            Waiter.WaitOne();

                            ReadBufferUsed = 0;
                        }

                        ReadBuffer = MusicHandler.Buffers.Take();
                    }

                    Read = await Ffmpeg.StandardOutput.BaseStream.ReadAsync(ReadBuffer, ReadBufferUsed, ReadBuffer.Length - ReadBufferUsed);

                    if (Read == 0)
                    {
                        if (++Fails == 10)
                        {
                            QueuedBuffers.Enqueue(ReadBuffer);
                            break;
                        }

                        await Task.Delay(50);
                    }
                    else
                    {
                        ReadBufferUsed += Read;
                        TotalSize += Read;
                        Fails = 0;
                    }
                }

                FinishedBuffer = true;
                ((Skip ? "Stopped" : "Finished") + " buffering " + Song.Name + " (" + TotalSize / 1.MB() + "MB)").Log();
            }
            catch (Exception Ex)
            {
                Bot.Client.Log.Log(Discord.LogSeverity.Error, "MusicProcessor", null, Ex);
            }

            try
            {
                if (Ffmpeg != null)
                {
                    Ffmpeg.Close();
                    Ffmpeg.Dispose();
                }
            }
            catch (Exception Ex)
            {
                Bot.Client.Log.Log(Discord.LogSeverity.Error, "ProcessDispose", null, Ex);
            }

            Ffmpeg = null;
        }

        public void Dispose()
        {
            Task.Run(() =>
            {
                try
                {
                    while (QueuedBuffers.Count > 0)
                    {
                        MusicHandler.Buffers.Return(QueuedBuffers.Dequeue());
                    }
                }
                catch (Exception Ex)
                {
                    Bot.Client.Log.Log(Discord.LogSeverity.Error, "DequeueMemoryFix", null, Ex);
                }
            });
        }
    }
}
