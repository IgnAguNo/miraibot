﻿using DiscordBot.Handlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DiscordBot
{
    class MusicProcessor
    {
        private const ushort BufferSize = 1920 * 2;

        public SongData Song;
        public bool Skip = false;
        //public DualStream Buffer;
        public Queue<byte[]> QueuedBuffers = new Queue<byte[]>();
        private Process Ffmpeg;
        public long TotalSize = 0;
        public bool FinishedBuffer = false;

        public MusicProcessor(SongData PlaySong)
        {
            Song = PlaySong;
            //this.Buffer = new DualStream();
            Ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                //FileName = "C:/ffmpeg/bin/ffmpeg.exe",
                Arguments = "-i \"" + this.Song.Uri + "\" -f s16le -ar 48000 -ac 2 pipe:1 -loglevel quiet",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            MainLoop();
        }

        private async void MainLoop()
        {
            int Read = 0;

            byte[] ReadBuffer = MusicHandler.Buffers.Take();
            int ReadBufferUsed = 0;

            int Fails = 0;

            try
            {
                //int BlockSize = 1920 * 2 * 4;
                //byte[] ByteBuffer = new byte[BlockSize];
                while (!Skip && TotalSize < ((long)3).GB())
                {
                    if (ReadBufferUsed == ReadBuffer.Length)
                    {
                        QueuedBuffers.Enqueue(ReadBuffer);
                        ReadBuffer = MusicHandler.Buffers.Take();
                        ReadBufferUsed = 0;
                    }

                    Read = await Ffmpeg.StandardOutput.BaseStream.ReadAsync(ReadBuffer, ReadBufferUsed, ReadBuffer.Length - ReadBufferUsed);

                    if (Read == 0)
                    {
                        if (++Fails == 20)
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
                ((Skip ? "Stopped" : "Finished") + " buffering " + Song.Name + " (" + TotalSize / ((long)1).MB() + "MB)").Log();
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
        }
    }
}
