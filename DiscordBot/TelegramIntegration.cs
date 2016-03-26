using DiscordBot.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiscordBot
{
    class TelegramIntegration
    {
        private static Api Api;
        private static User Me;
        public static MusicHandler Music = null;
        public static ConcurrentDictionary<int, string> UsernameIdCache = new ConcurrentDictionary<int, string>();
        public static List<int> Blocked = new List<int>();
        
        public static void Respond(MessageEventArgs e, string Text)
        {
            Api.SendTextMessage(e.Message.Chat.Id, Text, replyToMessageId: e.Message.MessageId);
        }

        public static async void Start()
        {
            Api = new Api("api key");
            Me = await Api.GetMe();

            var Commands = new Dictionary<string, EventHandler<MessageEventArgs>>();

            Commands.Add("add", (s, e) => 
            {
                SongData Song = new SongData((string)s);
                if (Song.Found)
                {
                    Music.DirectEnqueue(Song);
                    Respond(e, "Added " + Song.FullName.Compact(100));
                }
                else
                {
                    Respond(e, "The song could not be found");
                }
            });

            Commands.Add("song", (s, e) =>
            {
                string Song = Music.GetCurrentSongName();
                if (Song == null)
                {
                    Respond(e, "Nothing is playing right now");
                }
                else
                {
                    Respond(e, "Currently playing " + Song);
                }
            });

            Commands.Add("queue", (s, e) =>
            {
                string Playlist = Music.GetCurrentPlaylist();
                if (Playlist == string.Empty)
                {
                    Respond(e, "The queue is empty");
                }
                else
                {
                    Respond(e, Playlist);
                }
            });

            Commands.Add("skip", (s, e) =>
            {
                string NextTitle = Music.PeekNextName();
                if (NextTitle == null)
                {
                    Respond(e, "Reached the end of the queue");
                }
                else
                {
                    Respond(e, "Now playing " + NextTitle.Compact(100));
                }

                Music.Skip();
            });

            Commands.Add("remove", (s, e) =>
            {
                Respond(e, "Removed " + Music.Remove(s.ParseInts()).Join(", "));
            });

            Api.MessageReceived += new EventHandler<MessageEventArgs>((object s, MessageEventArgs e) =>
            {
                try
                {
                    var User = e.Message.From;

                    string OldUsername;
                    if (!UsernameIdCache.TryGetValue(User.Id, out OldUsername) || (User.Username != OldUsername && !UsernameIdCache.TryUpdate(User.Id, User.Username, OldUsername)))
                    {
                        UsernameIdCache.TryAdd(User.Id, User.Username);
                    }

                    if (e.Message.Type == MessageType.TextMessage && Music != null && !Blocked.Contains(User.Id))
                    {
                        string Text = e.Message.Text;

                        foreach (KeyValuePair<string, EventHandler<MessageEventArgs>> KVP in Commands)
                        {
                            if (Text.StartsWith("/" + KVP.Key))
                            {
                                string Substring = Text.Substring(KVP.Key.Length + 1);
                                if (Substring == string.Empty || Substring.StartsWith(" "))
                                {
                                    KVP.Value(Substring, e);
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    $"Telegram Exception {Ex}".Log();
                }
            });

            Api.StartReceiving();
            $"Logged into Telegram as {Me.Username}".Log();
        }
    }
}
