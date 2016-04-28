using Discord;
using DiscordBot.Handlers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DiscordBot.Commands
{
    class Music
    {
        public static async void Join(object s, MessageEventArgs e)
        {
            if (e.User.VoiceChannel != null)
            {
                await e.Music().ConnectClient(e.User.VoiceChannel);
            }
            else
            {
                e.Respond("..where do you want me to join?");
            }
        }

        public static async void Leave(object s, MessageEventArgs e)
        {
            await e.Music().DisconnectClient();
        }

        public static void Add(object s, MessageEventArgs e)
        {
            e.Music().Send(e.Channel, e.Music().Enqueue((string)s));
            e.Music().OptionalConnectClient(e.User.VoiceChannel);
        }

        private static string[] Files = null;
        public static void Local(object s, MessageEventArgs e)
        {
            string Search = ((string)s).ToLower();
            if (Search != string.Empty)
            {
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
                        e.Respond(Conversation.CantFind);
                    }
                    else if (Files.Length == 1)
                    {
                        e.Music().Send(e.Channel, e.Music().Enqueue(Files[0], true));
                        e.Music().OptionalConnectClient(e.User.VoiceChannel);
                    }
                    else
                    {
                        string Info = "";
                        for (int i = 0; i < Files.Length; i++)
                        {
                            Info += (i + 1) + ". `" + Files[i].Substring(SongData.MusicDir.Length) + "`\n";
                        }

                        e.Respond("Multiple files found\n" + Info);
                    }
                }
                else
                {
                    var Added = e.Music().LocalMultipleEnqueue(ToAdd);
                    if (Added.Count > 0)
                    {
                        e.Music().Send(e.Channel, $"Added {Added.Count} songs\n` {string.Join("`\n`", Added)}`");
                    }
                }

                e.Music().OptionalConnectClient(e.User.VoiceChannel);
            }
        }

        public static void Push(object s, MessageEventArgs e)
        {
            var Split = ((string)s).Split(' ');

            int Place, ToPlace = 1;
            if (int.TryParse(Split[0], out Place))
            {
                if (Split.Length == 3)
                {
                    int.TryParse(Split[2], out ToPlace);
                }
                
                var Pushed = e.Music().Push(Place, ToPlace);
                if (Pushed != null)
                {
                    e.Music().Send(e.Channel, $"Pushed `{Pushed}` to #{ToPlace}");
                }
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
                e.Music().Send(e.Channel, $"Repeated `{e.Music().Repeat(Count)}` {Count} times");
            }
        }

        public static void Remove(object s, MessageEventArgs e)
        {
            e.Music().Send(e.Channel, "Removed: " + e.Music().Remove(s.ParseInts()).Join(", ") );
        }

        public static void Volume(object s, MessageEventArgs e)
        {
            int Parse;
            if (int.TryParse((string)s, out Parse) && Parse >= 0 && Parse <= 15)
            {
                e.Music().Volume = (float)Parse / 10;
            }
        }

        public static void CurrentSong(object s, MessageEventArgs e)
        {
            e.Music().SendCurrentSong(e.Channel);
        }

        public static void Playlist(object s, MessageEventArgs e)
        {
            e.Music().Send(e.Channel, e.Music().GetCurrentPlaylist());
        }

        public static void Skip(object s, MessageEventArgs e)
        {
            e.Music().Skip();
        }

        public static void Shuffle(object s, MessageEventArgs e)
        {
            e.Music().Send(e.Channel, e.Music().Shuffle());
        }

        public static void Clear(object s, MessageEventArgs e)
        {
            e.Music().Send(e.Channel, e.Music().Clear());
        }

        public static void Save(object s, MessageEventArgs e)
        {
            e.Respond($"Saved the playlist ({e.Music().Save(e.Server, (string)s)} songs). Use `#load` to load it again");
        }

        public static void Load(object s, MessageEventArgs e)
        {
            e.Music().Load(e.Server, (string)s, e.Channel);
        }

        public static void Pair(object s, MessageEventArgs e)
        {
            TelegramIntegration.NextPairChannel = e.Channel;
            e.Respond(e.Server.Name + " is waiting to be paired to a Telegram server");
        }

        public static void Unpair(object s, MessageEventArgs e)
        {
            Db.RemoveDiscordServerId(e.Server.Id);
            e.Respond("Removed all links with Telegram");
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
                        e.Respond("Unblocked user " + User.Value);
                    }
                    else
                    {
                        TelegramIntegration.Blocked.Add(User.Key);
                        e.Respond("Blocked user " + User.Value);
                    }
                }
            }
            else
            {
                e.Respond("That username couldn't be found");
            }
        }

        public static void Adhd(object s, MessageEventArgs e)
        {
            e.Music().ToggleAdhd();
        }
    }
}
