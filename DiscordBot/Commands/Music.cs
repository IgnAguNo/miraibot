using Discord;
using DiscordBot.Handlers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoLibrary;

namespace DiscordBot.Commands
{
    class Music
    {
        public static async void Join(object s, MessageEventArgs e)
        {
            if (e.User.VoiceChannel != null)
            {
                MusicHandler Music = ServerData.Servers[e.User.Server.Id].Music;
                await Music.ConnectClient(e.User.VoiceChannel);
            }
            else
            {
                Bot.Send(e.Channel, "..where do you want me to join?");
            }
        }

        public static async void Leave(object s, MessageEventArgs e)
        {
            ServerData ServerData = ServerData.Servers[e.User.Server.Id];
            await ServerData.Music.DisconnectClient();
        }

        public static async void Add(object s, MessageEventArgs e)
        {
            string Query = (string)s;

            if (!Query.EndsWith(".mp3") && !Query.EndsWith(".mp4") && !Query.EndsWith(".webm") && !Query.EndsWith(".flac"))
            {
                if (Regex.IsMatch(Query, "(.*)(soundcloud.com|snd.sc)(.*)"))
                {
                    try
                    {
                        string SoundCloudResponse = await ("http://api.soundcloud.com/resolve?url=" + Query + "&client_id=" + Bot.SoundCloudAPI).ResponseAsync();
                        if (SoundCloudResponse == string.Empty || !SoundCloudResponse.StartsWith("{\"kind\":\"track\""))
                        {
                            Bot.Send(e.Channel, "Sorry, the SoundCloud link doesn't seem to be working");
                        }
                        else
                        {
                            JObject Response = JObject.Parse(SoundCloudResponse);
                            string Name = Response["title"].ToString();
                            if (Response["streamable"].ToString().ToLower() != "false")
                            {
                                ServerData.Servers[e.User.Server.Id].Music.Enqueue(Name, Response["stream_url"].ToString() + "?client_id=" + Bot.SoundCloudAPI);
                                Bot.Send(e.Channel, "Added `" + Name + "`");
                            }
                            else
                            {
                                Bot.Send(e.Channel, "This file cannot be streamed");
                            }
                        }
                    }
                    catch (Exception Ex)
                    {
                        Bot.Client.Log.Log(LogSeverity.Error, "SoundCloudLink", Query, Ex);
                    }
                }
                else //Youtube
                {
                    YouTubeVideo Video = null;
                    Query = await Search.YoutubeResult(Query);

                    if (Query == string.Empty)
                    {
                        Bot.Send(e.Channel, Conversation.CantFind);
                        return;
                    }
                    else
                    {
                        IEnumerable<YouTubeVideo> Videos = await YouTube.Default.GetAllVideosAsync(Query);
                        try
                        {
                            Video = Videos.Where(v => v.AdaptiveKind == AdaptiveKind.Audio).OrderByDescending(v => v.AudioBitrate).FirstOrDefault();
                        }
                        catch { }

                        if (Video == null)
                        {
                            Bot.Send(e.Channel, "That video isn't compatible with me");
                            return;
                        }

                        string Name = Video.Title.Substring(0, Video.Title.Length - 10);
                        ServerData.Servers[e.User.Server.Id].Music.Enqueue(Name, Video.Uri);
                        Bot.Send(e.Channel, "Added `" + Name + "`");
                    }
                }
            }
            else
            {
                ServerData.Servers[e.User.Server.Id].Music.Enqueue(Query.Compact(), Query);
                Bot.Send(e.Channel, "Added `" + Query.Compact() + "`");
            }
        }

        private static string[] Files = null;
        public static void Local(object s, MessageEventArgs e)
        {
            string MusicRoot = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") + "\\Music\\";
            string Search = ((string)s).ToLower();

            List<string> Added = new List<string>();
            if (Files != null)
            {
                int Num;
                foreach (string StringNum in Search.Split(','))
                {
                    if (int.TryParse(StringNum.Trim(), out Num) && Num > 0 && Num <= Files.Length)
                    {
                        string Name = Files[Num - 1].Substring(MusicRoot.Length).Compact();
                        ServerData.Servers[e.User.Server.Id].Music.Enqueue(Name, Files[Num - 1]);
                        Added.Add(Name);
                    }
                }

                Files = null;
            }

            if (Added.Count > 0)
            {
                Bot.Send(e.Channel, "Added: \n`" + string.Join("\n", Added) + "`");
            }
            else
            {
                Files = Directory.GetFiles(MusicRoot).Where(x => x.EndsWith(".mp3") && x.ToLower().Contains(Search)).ToArray();
                if (Files.Length == 0)
                {
                    Bot.Send(e.Channel, Conversation.CantFind);
                }
                else if (Files.Length == 1)
                {
                    string Name = Files[0].Substring(MusicRoot.Length).Compact();

                    ServerData.Servers[e.User.Server.Id].Music.Enqueue(Name, Files[0]);
                    Bot.Send(e.Channel, "Added `" + Name + "`");
                }
                else
                {
                    string Info = "";
                    for (int i = 0; i < Files.Length; i++)
                    {
                        Info += (i + 1) + ". `" + Files[i].Substring(MusicRoot.Length).Compact() + "`\n";
                    }

                    Bot.Send(e.Channel, "Local: \n" + Info);
                }
            }
        }

        public static void Push(object s, MessageEventArgs e)
        {
            int Place = 0;
            if (int.TryParse((string)s, out Place))
            {
                Bot.Send(e.Channel, "Pushed `" + ServerData.Servers[e.User.Server.Id].Music.Push(Place).Name + "` to the top");
            }
        }

        public static void Remove(object s, MessageEventArgs e)
        {
            int Place;
            string[] ToRemoveString = ((string)s).Split(',');
            List<int> ToRemove = new List<int>();

            foreach (string Remove in ToRemoveString)
            {
                if (int.TryParse(Remove.Trim(), out Place))
                {
                    ToRemove.Add(Place);
                }
            }

            List<SongData> Removed = ServerData.Servers[e.User.Server.Id].Music.Remove(ToRemove);
            string RemovedText = "**Removed**\n";
            foreach (SongData Song in Removed)
            {
                RemovedText += "`" + Song.Name + "`\n";
            }

            Bot.Send(e.Channel, RemovedText);
        }

        public static void Volume(object s, MessageEventArgs e)
        {
            string Query = (string)s;

            int Parse;
            if (int.TryParse(Query, out Parse) && Parse >= 0 && Parse <= 15)
            {
                ServerData.Servers[e.User.Server.Id].Music.Volume = (float)Parse / 10;
            }
        }

        public static void CurrentSong(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.User.Server.Id].Music.SendCurrentSong(e.Channel);
        }

        public static void Playlist(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.User.Server.Id].Music.SendPlaylist(e.Channel);
        }

        public static void Skip(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.User.Server.Id].Music.Skip();
        }

        public static void Shuffle(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.Server.Id].Music.Shuffle();
            Bot.Send(e.Channel, "Shuffled! Type `#playlist` to see the new playlist");
        }

        public static void Save(object s, MessageEventArgs e)
        {
            string Playlist = "data.playlist" + e.Server.Id.ToString() + ".txt";
            int Count = ServerData.Servers[e.Server.Id].Music.Save(Playlist);
            Bot.Send(e.Channel, "Saved the playlist (" + Count + " songs). Use `#load` to load it again");
        }

        public static void Load(object s, MessageEventArgs e)
        {
            string Playlist = "data.playlist" + e.Server.Id.ToString() + ".txt";
            if (File.Exists(Playlist))
            {
                int Count = ServerData.Servers[e.Server.Id].Music.Load(Playlist);
                Bot.Send(e.Channel, "Loaded the saved playlist (" + Count + " songs). Use `#playlist` to view it");
            }
        }
    }
}
