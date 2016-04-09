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
                await ServerData.Servers[e.Server.Id].Music.ConnectClient(e.User.VoiceChannel);
            }
            else
            {
                Bot.Send(e.Channel, "..where do you want me to join?");
            }
        }

        public static async void Leave(object s, MessageEventArgs e)
        {
            await ServerData.Servers[e.Server.Id].Music.DisconnectClient();
        }

        public static void Add(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.Server.Id].Music.Enqueue((string)s, e.Channel);
        }

        private static string[] Files = null;
        public static void Local(object s, MessageEventArgs e)
        {
            string Search = ((string)s).ToLower();

            List<string> ToAdd = new List<string>();
            if (Files != null)
            {
                foreach (int Num in Search.ParseInts())
                {
                    ToAdd.Add(Files[Num - 1]);
                }

                Files = null;
            }

            if (ToAdd.Count == 0)
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

                    Bot.Send(e.Channel, "Multiple files found\n" + Info);
                }
            }
            else
            {
                ServerData.Servers[e.Server.Id].Music.Enqueue(ToAdd, e.Channel, true);
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
            MusicHandler Music = ServerData.Servers[e.Server.Id].Music;
            Music.Send(e.Channel, "Removed: " + Music.Remove(s.ParseInts()).Join(", ") );
        }

        public static void Volume(object s, MessageEventArgs e)
        {
            int Parse;
            if (int.TryParse((string)s, out Parse) && Parse >= 0 && Parse <= 15)
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

        public static void Save(object s, MessageEventArgs e)
        {
            int Count = ServerData.Servers[e.Server.Id].Music.Save(s, e.Server);
            Bot.Send(e.Channel, "Saved the playlist (" + Count + " songs). Use `#load` to load it again");
        }

        public static void Load(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.Server.Id].Music.Load(e.Server, (string)s, e.Channel);
        }

        public static void Pair(object s, MessageEventArgs e)
        {
            TelegramIntegration.NextPairChannel = e.Channel;
            Bot.Send(e.Channel, e.Server.Name + " is waiting to be paired to a Telegram server");
        }

        public static void Unpair(object s, MessageEventArgs e)
        {
            Db.RemoveDiscordServerId(e.Server.Id);
            Bot.Send(e.Channel, "Removed all links with Telegram");
        }

        public static void TgToggle(object s, MessageEventArgs e)
        {
            var Users = TelegramIntegration.UsernameIdCache.Where(x => x.Value == (string)s);
            if (Users.Count() > 0)
            {
                var User = Users.First();
                lock (TelegramIntegration.Blocked)
                {
                    if (TelegramIntegration.Blocked.Contains(User.Key))
                    {
                        TelegramIntegration.Blocked.Remove(User.Key);
                        Bot.Send(e.Channel, "Unblocked user " + User.Value);
                    }
                    else
                    {
                        TelegramIntegration.Blocked.Add(User.Key);
                        Bot.Send(e.Channel, "Blocked user " + User.Value);
                    }
                }
            }
            else
            {
                Bot.Send(e.Channel, "That username couldn't be found");
            }
        }

        public static void Adhd(object s, MessageEventArgs e)
        {
            ServerData.Servers[e.Server.Id].Music.ADHD = !ServerData.Servers[e.Server.Id].Music.ADHD;
        }
    }
}
