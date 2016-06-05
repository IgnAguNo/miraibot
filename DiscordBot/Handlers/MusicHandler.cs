using Discord;
using Discord.Audio;
using DiscordBot.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot.Handlers
{
    class MusicHandler : IHandler
    {
        public static ByteBuffer Buffers;
        
        private IAudioClient AudioClient;
        private Channel voiceChannel = null;
        public Channel VoiceChannel
        {
            get
            {
                return voiceChannel;
            }
            set
            {
                voiceChannel = value;
                DisconnectClient().ContinueWith(async (e) =>
                {
                    await new Func<Task>(async delegate
                    {
                        AudioClient = await VoiceChannel.JoinAudio();
                    }).UntilNoExceptionAsync(10);
                });
            }
        }
        private ConcurrentQueue<SongData> SongQueue = new ConcurrentQueue<SongData>();
        public SongData[] Songs
        {
            get
            {
                return SongQueue.ToArray();
            }
        }
        public int SongCount
        {
            get
            {
                return SongQueue.Count;
            }
        }
        private const int MaxQueued = 30;

        private MusicProcessor CurrentSong;
        private Task Sending = null;
        private bool StopRequested = false;

        public float Volume = 1.0f;
        private bool Adhd = false;

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

        public MusicHandler(Server S)
        {
            Load(S).Forget();
        }
        
        public async Task Run()
        {
            //delayed start, force new thread
            await Task.Delay(100);

            SongData PlaySong;

            byte[] CurrentSend = null;
            byte[] NextSend = null;

            while (!StopRequested)
            {
                try
                {
                    if (CurrentSong != null && CurrentSong.Skip)
                    {
                        $"Finished sending {CurrentSong.Song.Name}".Log();
                        CurrentSong.Dispose();
                        CurrentSong = null;
                    }

                    if (AudioClient != null)
                    {
                        if (AudioClient.State != ConnectionState.Connected)
                        {
                            try
                            {
                                AudioClient = await AudioClient.Channel.JoinAudio();
                            }
                            catch
                            {
                                await DisconnectClient();
                                continue;
                            }
                        }

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
                                    await Task.WhenAny(Sending, Task.Delay(1000));
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
                                //Speedtest
                                if (Adhd && CurrentSong.QueuedBuffers.Count > 1)
                                {
                                    CurrentSong.QueuedBuffers.Dequeue();
                                    CurrentSong.Waiter.Release(1);
                                }

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

            try
            {
                if (CurrentSong != null)
                {
                    CurrentSong.Skip = true;
                }
            }
            catch { }

            await DisconnectClient();
        }

        public void OptionalConnectClient(Channel NewVoiceChannel)
        {
            try
            {
                if (NewVoiceChannel != null && (AudioClient == null || AudioClient.State != ConnectionState.Connected))
                {
                    VoiceChannel = NewVoiceChannel;
                }
            }
            catch { }
        }

        public async Task DisconnectClient()
        {
            try
            {
                if (AudioClient != null)
                {
                    try
                    {
                        await AudioClient.Disconnect();
                    }
                    catch (Exception Ex)
                    {
                        Ex.Log();
                    }
                    
                    Sending = null;
                    AudioClient = null;
                }
            }
            catch { }
        }

        public string Enqueue(string Query, bool Local = false)
        {
            if (SongQueue.Count < MaxQueued)
            {
                var Song = new SongData(Query, Local);
                if (Song.Found)
                {
                    SongQueue.Enqueue(Song);
                    return $"Added `{Song.FullName.Compact(100)}` at #{SongQueue.Count}";
                }
                else
                {
                    return Conversation.CantFind;
                }
            }
            else
            {
                return $"The queue has reached its limit of {MaxQueued} songs";
            }
        }

        public List<string> LocalMultipleEnqueue(List<string> Queries)
        {
            var Added = new List<string>();
            foreach (string Query in Queries)
            {
                if (SongQueue.Count < MaxQueued)
                {
                    var Song = new SongData(Query, true);
                    if (Song.Found)
                    {
                        SongQueue.Enqueue(Song);
                        Added.Add($"{Song.FullName} (#{SongQueue.Count})");
                    }
                }
            }

            return Added;
        }

        public string Shuffle()
        {
            Random Rand = new Random();
            ConcurrentQueue<SongData> Queue = new ConcurrentQueue<SongData>();
            foreach (var Song in SongQueue.ToArray().OrderBy(x => Rand.Next()))
            {
                Queue.Enqueue(Song);
            }
            
            SongQueue = Queue;
            return GetCurrentPlaylist();
        }

        public string Push(int Place, int ToPlace)
        {
            ConcurrentQueue<SongData> NewQueue = new ConcurrentQueue<SongData>();
            var Songs = SongQueue.ToList();
            if (Place > 0 && Songs.Count >= Place && ToPlace > 0 && Songs.Count >= ToPlace)
            {
                SongData Pushed = Songs[Place - 1];
                Songs.Remove(Pushed);
                Songs.Insert(ToPlace - 1, Pushed);
                
                foreach (SongData Video in Songs)
                {
                    NewQueue.Enqueue(Video);
                }

                SongQueue = NewQueue;
                return Pushed.FullName;
            }

            return null;
        }

        public string Repeat(int Count)
        {
            if (CurrentSong != null)
            {
                var Queue = Songs;

                if (Count + Queue.Length > MaxQueued)
                {
                    Count = MaxQueued - Queue.Length;
                }

                var NewQueue = new ConcurrentQueue<SongData>();

                for (int i = 0; i < Count; i++)
                {
                    NewQueue.Enqueue(CurrentSong.Song);
                }

                foreach (var Song in Queue)
                {
                    NewQueue.Enqueue(Song);
                }

                SongQueue = NewQueue;
                return CurrentSong.Song.FullName;
            }

            return null;
        }

        public List<string> Remove(List<int> Places)
        {
            var Removed = new List<string>();
            int i = 1;

            var NewQueue = new ConcurrentQueue<SongData>();
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

        public SongData GetCurrentSong()
        {
            return CurrentSong?.Song;
        }

        public void SendCurrentSong(Channel Channel)
        {
            if (CurrentSong != null)
            {
                Send(Channel, "Now Playing `" + CurrentSong.Song.FullName.Compact(100) + "` (" + SongQueue.Count + " songs queued)");
            }
        }

        public string GetCurrentPlaylist()
        {
            StringBuilder Text = new StringBuilder();

            Text.Append(SongQueue.Count);
            Text.Append(" Song(s) Queued\n");

            int Count = 0;
            foreach (var Entry in Songs)
            {
                Text.Append(++Count);
                Text.Append(". ");
                Text.Append(Entry.Name);
                Text.Append("\n");
            }

            return Text.ToString();
        }

        /*public void SendPlaylist(Channel Channel)
        {
            Send(Channel, GetCurrentPlaylist());
        }*/

        public string Clear()
        {
            SongQueue = new ConcurrentQueue<SongData>();
            return GetCurrentPlaylist();
        }

        private static Regex AlphaNum = new Regex("[^a-zA-Z0-9 -]");
        public int Save(Server S, string s = "")
        {
            string Identifier = AlphaNum.Replace(s.Trim().ToLower(), "");
            if (Identifier == string.Empty)
            {
                Identifier = S.Id.ToString();
            }

            SongData[] Data;
            if (CurrentSong != null)
            {
                Data = new SongData[SongQueue.Count + 1];
                Data[0] = CurrentSong.Song;
                SongQueue.CopyTo(Data, 1);
            }
            else
            {
                Data = Songs;
            }

            using (BinaryWriter Writer = new BinaryWriter(File.Open(Bot.MainDir + "data.playlist." + Identifier + ".txt", FileMode.Create)))
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

        public async Task Load(Server S, string s = "", Channel C = null)
        {
            string Identifier = AlphaNum.Replace(s.Trim().ToLower(), "");
            Message M = null;
            if (Identifier == string.Empty)
            {
                Identifier = S.Id.ToString();
            }
            else if (C != null)
            {
                M = await SendAsync(C, "Adding..");
            }

            string DataFile = Bot.MainDir + "data.playlist." + Identifier + ".txt";
            if (!File.Exists(DataFile))
            {
                await EditAsync(M, "Can't find " + DataFile);
                return;
            }

            int Count = 0;

            using (BinaryReader Reader = new BinaryReader(File.Open(DataFile, FileMode.Open)))
            {
                Count = Reader.ReadInt32();
                SongData Song;
                
                await Task.Run(async () =>
                {
                    for (int i = 0; i < Count; i++)
                    {
                        Song = new SongData(Reader.ReadString(), Reader.ReadBoolean());
                        SongQueue.Enqueue(Song);

                        await EditAsync(M, $"[{i}/{Count}] Added `{Song.Name}` at #{SongQueue.Count}");
                    }
                });

                await Task.Delay(500);
                await EditAsync(M, "Loaded the saved playlist (" + Count + " songs). Use `#playlist` to view it");
            }
        }

        public void ToggleAdhd()
        {
            Adhd = !Adhd;
        }

        public void Stop()
        {
            StopRequested = true;
        }

        ~MusicHandler()
        {
            DisconnectClient().Wait();
        }
    }
}
