using Discord;
using DiscordBot.Handlers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiscordBot.Commands
{
    class Music
    {
        public static async void Join(object s, MessageEventArgs e)
        {
            if (e.User.VoiceChannel != null)
            {
                MusicHandler Music = ServerData.Servers[e.Server.Id].Music;
                await Music.ConnectClient(e.User.VoiceChannel);
            }
            else
            {
                Bot.Send(e.Channel, "..where do you want me to join?");
            }
        }

        public static async void Leave(object s, MessageEventArgs e)
        {
            ServerData ServerData = ServerData.Servers[e.Server.Id];
            await ServerData.Music.DisconnectClient();
        }

        public static void Add(object s, MessageEventArgs e)
        {
            string Query = (string)s;
            ServerData.Servers[e.Server.Id].Music.Enqueue(Query, e.Channel);

            /*
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
                                ServerData.Servers[e.Server.Id].Music.Enqueue(Name, Response["stream_url"].ToString() + "?client_id=" + Bot.SoundCloudAPI, e.Channel);
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
                        ServerData.Servers[e.Server.Id].Music.Enqueue(Name, Video.Uri, e.Channel);
                    }
                }
            }
            else
            {
                ServerData.Servers[e.Server.Id].Music.Enqueue(Query, e.Channel);
            }*/
        }

        private static string[] Files = null;
        public static void Local(object s, MessageEventArgs e)
        {
            string Search = ((string)s).ToLower();

            List<string> Added = new List<string>();
            if (Files != null)
            {
                int Num;
                foreach (string StringNum in Search.Split(','))
                {
                    if (int.TryParse(StringNum.Trim(), out Num) && Num > 0 && Num <= Files.Length)
                    {
                        string Name = Files[Num - 1].Substring(SongData.MusicDir.Length);
                        ServerData.Servers[e.Server.Id].Music.Enqueue(Files[Num - 1], e.Channel, true);
                        Added.Add(Name);
                    }
                }

                Files = null;
            }

            if (Added.Count == 0)
            {
                Files = Directory.GetFiles(SongData.MusicDir).Where(x => x.EndsWith(".mp3") && x.ToLower().Contains(Search)).ToArray();
                if (Files.Length == 0)
                {
                    Bot.Send(e.Channel, Conversation.CantFind);
                }
                else if (Files.Length == 1)
                {
                    string Name = Files[0].Substring(SongData.MusicDir.Length);
                    ServerData.Servers[e.Server.Id].Music.Enqueue(Files[0], e.Channel, true);
                }
                else
                {
                    string Info = "";
                    for (int i = 0; i < Files.Length; i++)
                    {
                        Info += (i + 1) + ". `" + Files[i].Substring(SongData.MusicDir.Length).Compact() + "`\n";
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
                ServerData.Servers[e.Server.Id].Music.Push(Place, e.Channel);
            }
        }

        public static void Repeat(object s, MessageEventArgs e)
        {
            int Count = 1;
            if ((string)s != string.Empty)
            {
                int.TryParse((string)s, out Count);
            }

            if (Count > 0)
            {
                ServerData.Servers[e.Server.Id].Music.Repeat(Count, e.Channel);
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

            ServerData.Servers[e.Server.Id].Music.Remove(ToRemove, e.Channel);
        }

        public static void Volume(object s, MessageEventArgs e)
        {
            string Query = (string)s;

            int Parse;
            if (int.TryParse(Query, out Parse) && Parse >= 0 && Parse <= 15)
            {
                ServerData.Servers[e.Server.Id].Music.Volume = (float)Parse / 10;
            }
        }

        public static void CurrentSong(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.Server.Id].Music.SendCurrentSong(e.Channel);
        }

        public static void Playlist(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.Server.Id].Music.SendPlaylist(e.Channel);
        }

        public static void Skip(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.Server.Id].Music.Skip();
        }

        public static void Shuffle(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.Server.Id].Music.Shuffle();
            ServerData.Servers[e.Server.Id].Music.SendPlaylist(e.Channel);
        }

        public static void Clear(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.Server.Id].Music.Clear();
            ServerData.Servers[e.Server.Id].Music.SendPlaylist(e.Channel);
        }

        private static Regex AlphaNum = new Regex("[^a-zA-Z0-9 -]");
        public static void Save(object s, MessageEventArgs e)
        {
            string Identifier = AlphaNum.Replace(((string)s).Trim().ToLower(), "");
            if (Identifier == string.Empty)
            {
                Identifier = e.Server.Id.ToString();
            }

            int Count = ServerData.Servers[e.Server.Id].Music.Save("data.playlist." + Identifier + ".txt");
            Bot.Send(e.Channel, "Saved the playlist (" + Count + " songs). Use `#load` to load it again");
        }

        public static void Load(object s, MessageEventArgs e)
        {
            string Identifier = e.Server.Id.ToString();

            string Query = AlphaNum.Replace(((string)s).Trim().ToLower(), "");
            if (Query != string.Empty)
            {
                Identifier += "." + Query;
            }

            string Playlist = "data.playlist." + Identifier + ".txt";
            if (File.Exists(Playlist))
            {
                int Count = ServerData.Servers[e.Server.Id].Music.Load(Playlist);
                Bot.Send(e.Channel, "Loaded the saved playlist (" + Count + " songs). Use `#playlist` to view it");
            }
        }
    }
}
