using Discord;
using Discord.Audio;
using DiscordBot.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Handlers
{
    class MusicHandler : IHandler
    {
        public static ByteBuffer Buffers;
        
        private IAudioClient AudioClient;
        private ConcurrentQueue<SongData> SongQueue = new ConcurrentQueue<SongData>();
        private const int MaxQueued = 30;

        private MusicProcessor CurrentSong;
        private Task Sending = null;

        public float Volume = 1.0f;

        public bool Playing
        {
            get
            {
                return CurrentSong != null && AudioClient != null && AudioClient.State == ConnectionState.Connected;
            }
        }

        public override string Name
        {
            get
            {
                return "Music";
            }
        }
        
        public async void Run()
        {
            //delayed start, force new thread
            await Task.Delay(100);

            SongData PlaySong;

            byte[] CurrentSend = null;
            byte[] NextSend = null;

            while (true)
            {
                try
                {
                    if (CurrentSong != null && CurrentSong.Skip)
                    {
                        CurrentSong.Dispose();
                        CurrentSong = null;
                    }

                    if (AudioClient != null && AudioClient.State == ConnectionState.Connected)
                    {
                        if (CurrentSong == null)
                        {
                            //Dequeue a song
                            if (SongQueue.TryDequeue(out PlaySong))
                            {
                                CurrentSong = new MusicProcessor(PlaySong);

                                foreach (Channel Channel in ServerData.Servers[AudioClient.Server.Id].ChannelsWithCategory(typeof(Music).Name))
                                {
                                    SendCurrentSong(Channel);
                                }
                            }
                            else
                            {
                                //No new song in queue
                                await Task.Delay(100);
                            }
                        }
                        else
                        {
                            if (NextSend != null)
                            {
                                if (Sending != null)
                                {
                                    Sending.Wait(1000);
                                }

                                Sending = AudioClient.OutputStream.WriteAsync(NextSend, 0, NextSend.Length);
                                if (CurrentSend != null)
                                {
                                    Buffers.Return(CurrentSend);
                                }

                                CurrentSend = NextSend;
                                NextSend = null;
                            }

                            if (CurrentSong.QueuedBuffers.Count > 0)
                            {
                                NextSend = CurrentSong.QueuedBuffers.Dequeue();
                                CurrentSong.Waiter.Release(1);

                                if (NextSend != null)
                                {
                                    NextSend = NextSend.AdjustVolume(Volume);
                                }
                            }
                            else if (CurrentSong.FinishedBuffer)
                            {
                                CurrentSong.Skip = true;
                            }
                        }
                    }
                    else
                    {
                        //Paused or not in voice chat
                        await Task.Delay(100);
                    }
                }
                catch (OperationCanceledException)
                { }
                catch (AggregateException)
                { }
                catch (Exception Ex)
                {
                    $"Music Handler Loop Exception: {Ex}".Log();
                }
            }
        }

        public async Task ConnectClient(Channel VoiceChannel)
        {
            if (AudioClient == null || AudioClient.Channel.Id != VoiceChannel.Id)
            {
                await DisconnectClient();
                AudioClient = await VoiceChannel.JoinAudio();
            }
        }

        public async Task DisconnectClient()
        {
            if (AudioClient != null)
            {
                try
                {
                    await AudioClient.Disconnect();
                }
                catch { }
                Sending = null;
                AudioClient = null;
            }
        }

        public void Enqueue(string Query, Channel Channel, bool Local = false)
        {
            if (SongQueue.Count < MaxQueued)
            {
                SongData Song = new SongData(Query, Local);
                if (Song.Found)
                {
                    SongQueue.Enqueue(Song);
                    Send(Channel, "Added `" + Song.Name + "`");
                }
                else
                {
                    Send(Channel, Conversation.CantFind);
                }
            }
            else
            {
                Send(Channel, "The queue has reached its limit of " + MaxQueued + " songs");
            }
        }

        public void DirectEnqueue(SongData Song)
        {
            if (SongQueue.Count < MaxQueued)
            {
                SongQueue.Enqueue(Song);
            }
        }

        public void Enqueue(List<string> Queries, Channel Channel, bool Local = false)
        {
            List<string> Added = new List<string>();
            foreach (string Query in Queries)
            {
                if (SongQueue.Count < MaxQueued)
                {
                    SongData Song = new SongData(Query, Local);
                    if (Song.Found)
                    {
                        SongQueue.Enqueue(Song);
                        Added.Add(Song.Name);
                    }
                }
            }

            if (Added.Count > 0)
            {
                Send(Channel, "Added " + Added.Count + " songs\n`" + string.Join("`\n`", Added) + "`");
            }
        }

        public void Shuffle()
        {
            Random Rand = new Random();
            ConcurrentQueue<SongData> Queue = new ConcurrentQueue<SongData>();
            IEnumerable<SongData> Songs = SongQueue.ToArray().OrderBy(x => Rand.Next());
            foreach (SongData Song in Songs)
            {
                Queue.Enqueue(Song);
            }
            
            SongQueue = Queue;
        }

        public void Push(int Place, Channel Channel)
        {
            ConcurrentQueue<SongData> NewQueue = new ConcurrentQueue<SongData>();
            SongData[] Songs = SongQueue.ToArray();
            if (Place > 0 && Songs.Length >= Place)
            {
                SongData Pushed = Songs[Place - 1];
                NewQueue.Enqueue(Songs[Place - 1]);

                int i = 1;
                foreach (SongData Video in Songs)
                {
                    if (i++ != Place)
                    {
                        NewQueue.Enqueue(Video);
                    }
                }

                SongQueue = NewQueue;
                Send(Channel, "Pushed `" + Pushed.Name + "` to the top");
            }
        }

        public void Repeat(int Count, Channel Channel)
        {
            if (CurrentSong != null)
            {
                SongData[] Songs = SongQueue.ToArray();

                if (Count + Songs.Length > MaxQueued)
                {
                    Count = MaxQueued - Songs.Length;
                }

                ConcurrentQueue<SongData> NewQueue = new ConcurrentQueue<SongData>();

                for (int i = 0; i < Count; i++)
                {
                    NewQueue.Enqueue(CurrentSong.Song);
                }

                foreach (SongData Video in Songs)
                {
                    NewQueue.Enqueue(Video);
                }

                SongQueue = NewQueue;
                Send(Channel, "Repeated `" + CurrentSong.Song.Name + "` " + Count + " times");
            }
        }

        public List<string> Remove(List<int> Places)
        {
            List<string> Removed = new List<string>();
            int i = 1;

            ConcurrentQueue<SongData> NewQueue = new ConcurrentQueue<SongData>();
            SongData[] Songs = SongQueue.ToArray();
            foreach (SongData Video in Songs)
            {
                if (Places.Contains(i++))
                {
                    Removed.Add("`" + Video.Name + "`");
                }
                else
                {
                    NewQueue.Enqueue(Video);
                }
            }

            if (Removed.Count > 0)
            {
                SongQueue = NewQueue;
            }

            return Removed;
        }

        public string PeekNextName()
        {
            SongData Result;
            if (SongQueue.TryPeek(out Result))
            {
                return Result.FullName;
            }

            return null;
        }

        public void Skip()
        {
            if (CurrentSong != null)
            {
                CurrentSong.Skip = true;
            }
        }

        public string GetCurrentSongName()
        {
            return CurrentSong?.Song.FullName.Compact(100);
        }

        public void SendCurrentSong(Channel Channel)
        {
            if (CurrentSong != null)
            {
                Send(Channel, "Now Playing `" + CurrentSong.Song.FullName.Compact(100) + "` (" + SongQueue.Count + " songs queued)");
            }
        }

        public int GetPlaylistCount()
        {
            return SongQueue.ToArray().Count();
        }

        public string GetCurrentPlaylist()
        {
            StringBuilder Builder = new StringBuilder();

            int Count = 0;
            foreach (SongData Entry in SongQueue.ToArray())
            {
                Builder.Append(++Count);
                Builder.Append(". ");
                Builder.Append(Entry.Name);
                Builder.Append("\n");
            }

            return Builder.ToString();
        }

        public void SendPlaylist(Channel Channel)
        {
            SongData[] Queued = SongQueue.ToArray();
            StringBuilder Text = new StringBuilder();

            Text.Append(Queued.Length);
            Text.Append(" Song(s) Queued\n");

            int Count = 0;
            foreach (SongData Entry in Queued)
            {
                Text.Append(++Count);
                Text.Append(". *");
                Text.Append(Entry.Name);
                Text.Append("*\n");
            }

            Send(Channel, Text.ToString());
        }

        public void Clear()
        {
            SongQueue = new ConcurrentQueue<SongData>();
        }

        public int Save(string DataFile)
        {
            SongData[] Data;
            if (CurrentSong != null)
            {
                Data = new SongData[SongQueue.Count + 1];
                Data[0] = CurrentSong.Song;
                SongQueue.CopyTo(Data, 1);
            }
            else
            {
                Data = SongQueue.ToArray();
            }

            using (BinaryWriter Writer = new BinaryWriter(File.Open(DataFile, FileMode.Create)))
            {
                Writer.Write(Data.Length);
                for (int i = 0; i < Data.Length; i++)
                {
                    Writer.Write(Data[i].Query);
                    Writer.Write(Data[i].Local);
                }
            }

            return Data.Length;
        }

        public int Load(string DataFile)
        {
            int Count = 0;
            ConcurrentQueue<SongData> Queue = new ConcurrentQueue<SongData>(SongQueue);

            using (BinaryReader Reader = new BinaryReader(File.Open(DataFile, FileMode.Open)))
            {
                Count = Reader.ReadInt32();
                Task<SongData>[] Tasks = new Task<SongData>[Count];
                
                for (int i = 0; i < Count; i++)
                {
                    string Query = Reader.ReadString();
                    bool Local = Reader.ReadBoolean();
                    Tasks[i] = Task.Run(() => { return new SongData(Query, Local); });
                }

                foreach (Task<SongData> T in Tasks)
                {
                    T.Wait();
                    Queue.Enqueue(T.Result);
                }
            }

            SongQueue = Queue;
            return Count;
        }

        ~MusicHandler()
        {
            DisconnectClient().Wait();
        }
    }
}
