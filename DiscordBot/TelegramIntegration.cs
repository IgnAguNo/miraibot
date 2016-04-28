using DiscordBot.Commands;
using DiscordBot.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiscordBot
{
    class TelegramIntegration
    {
        public static uint Msgs = 0;

        private static Api Api;
        public static User Me;
        public static ConcurrentDictionary<int, string> UsernameIdCache = new ConcurrentDictionary<int, string>();
        public static List<int> Blocked = new List<int>();
        private static Dictionary<string, EventHandler<Message>> Commands = new Dictionary<string, EventHandler<Message>>();
        public static Discord.Channel NextPairChannel = null;

        private static MusicHandler GetMusic(Message e)
            => GetMusic(e.Chat.Id);

        private static MusicHandler GetMusic(long TgChat)
        {
            ulong Id = Db.GetDiscordServerId(TgChat);
            if (Id == 0 || !ServerData.Servers.ContainsKey(Id))
            {
                return null;
            }

            return ServerData.Servers[Id].Music;
        }

        public static async void Respond(Message Msg, string Text)
        {
            try
            {
                await Api.SendTextMessage(Msg.Chat.Id, Text, replyToMessageId: Msg.MessageId);
            }
            catch
            {
            }
        }

        public static async void Start(string ApiKey)
        {
            Api = new Api(ApiKey);
            Me = await Api.GetMe();

            Commands.Add("add", (s, e) => 
            {
                MusicHandler Music = GetMusic(e);
                if (Music != null)
                {
                    var Files = Directory.GetFiles(SongData.MusicDir).Where(x => x.EndsWith(".mp3") && x.ToLower().Contains((string)s)).ToArray();
                    SongData Song;
                    if (Files.Length > 0)
                    {
                        Song = new SongData(Files[0], true);
                        Music.DirectEnqueue(Song);
                    }
                    else
                    {
                        Song = new SongData(s);
                        if (Song.Found)
                        {
                            Music.DirectEnqueue(Song);
                        }
                    }

                    if (Song.Found)
                    {
                        Respond(e, "Added " + Song.FullName.Compact(100));
                    }
                    else
                    {
                        Respond(e, "The song could not be found");
                    }
                }
            });

            Commands.Add("song", (s, e) =>
            {
                MusicHandler Music = GetMusic(e);
                if (Music != null)
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
                }
            });

            Commands.Add("queue", (s, e) =>
            {
                MusicHandler Music = GetMusic(e);
                if (Music != null)
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
                }
            });

            Commands.Add("skip", (s, e) =>
            {
                MusicHandler Music = GetMusic(e);
                if (Music != null)
                {
                    if (Music.Playing)
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
                    }
                    else
                    {
                        Respond(e, "I'm not playing any music at the moment");
                    }
                }
            });

            Commands.Add("remove", async (s, e) =>
            {
                MusicHandler Music = GetMusic(e);
                if (Music != null)
                {
                    List<string> Removed = Music.Remove(s.ParseInts());

                    if (Removed.Count > 0)
                    {
                        Respond(e, "Removed " + Removed.Join(", "));
                    }
                    else
                    {
                        int Count = Music.GetPlaylistCount();
                        if (Count == 0)
                        {
                            Respond(e, "There is nothing I can remove");
                        }
                        else
                        {
                            await Api.SendChatAction(e.Chat.Id, ChatAction.Typing);

                            var KeyboardMarkup = new ReplyKeyboardMarkup();
                            int Row = -1;

                            KeyboardMarkup.Keyboard = new string[(int)Math.Ceiling((double)Count / 2)][];
                            for (int i = 0; i < (2 * KeyboardMarkup.Keyboard.Length); i++)
                            {
                                if (i % 2 == 0)
                                {
                                    KeyboardMarkup.Keyboard[++Row] = new string[2];
                                }

                                if (i < Count)
                                {
                                    KeyboardMarkup.Keyboard[Row][i % 2] = "/remove " + (i + 1).ToString();
                                }
                                else
                                {
                                    KeyboardMarkup.Keyboard[Row][i % 2] = string.Empty;
                                }
                            }

                            KeyboardMarkup.OneTimeKeyboard = true;
                            KeyboardMarkup.ResizeKeyboard = true;
                            KeyboardMarkup.Selective = true;

                            await Api.SendTextMessage(e.Chat.Id, Music.GetCurrentPlaylist() + "\nWhich song should I remove?", replyToMessageId: e.MessageId, replyMarkup: KeyboardMarkup);
                        }
                    }
                }
            });

            Commands.Add("pair", (s, e) =>
            {
                if (NextPairChannel != null)
                {
                    Db.SetDiscordServerId(e.Chat.Id, NextPairChannel.Server.Id);
                    Respond(e, "This chat has now been succesfully paired with " + NextPairChannel.Server.Name);
                    Bot.Send(NextPairChannel, "This server has been paired with " + ((e.Chat.Type == ChatType.Private) ? e.Chat.Title + " by " : "") + e.From.Username);

                    NextPairChannel = null;
                }
            });

            Commands.Add("hrc", (s, e) =>
            {
                try
                {
                    if (e.Chat.Type == ChatType.Private || e.Chat.Title.StartsWith("H"))
                    {
                        Respond(e, Lewd.GetRandomLewd(s, (e.From.Username != "Akkey" && e.Chat.Type != ChatType.Private)));
                    }
                }
                catch (Exception Ex)
                {
                    $"HRC {Ex}".Log();
                }
            });

            //Api.ChosenInlineResultReceived += Api_ChosenInlineResultReceived;
            //Api.InlineQueryReceived += Api_InlineQueryReceived;
            //Api.MessageReceived += Api_MessageReceived;
            //Api.UpdateReceived += Api_UpdateReceived;

            //Api.StartReceiving();


            $"Logged into Telegram as {TelegramIntegration.Me.Username}".Log();
            int UpdateOffset = 0;

            while (Api != null)
            {
                foreach (var Update in await Api.GetUpdates(UpdateOffset, int.MaxValue))
                {
                    //$"{Update.Type} {Update.Message?.Text}".Log();

                    if (Update.Type == UpdateType.MessageUpdate)
                    {
                        Api_MessageReceived(null, Update.Message);
                    }

                    UpdateOffset = Update.Id + 1;
                }
            }
        }

        /*private static void Api_ChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs e)
        {
            $"ChosenInlineResult {e.ChosenInlineResult?.Query}".Log();
        }

        private static void Api_InlineQueryReceived(object sender, InlineQueryEventArgs e)
        {
            $"Inline Query {e.InlineQuery?.Query}".Log();
        }*/

        //private static void Api_MessageReceived(object sender, MessageEventArgs e)
        private static void Api_MessageReceived(object sender, Message Msg)
        {
            //$"Message {e.Message.From.Username}".Log();
            try
            {
                Msgs++;
                var User = Msg.From;

                string OldUsername;
                if (!UsernameIdCache.TryGetValue(User.Id, out OldUsername) || (User.Username != OldUsername && !UsernameIdCache.TryUpdate(User.Id, User.Username, OldUsername)))
                {
                    UsernameIdCache.TryAdd(User.Id, User.Username);
                }

                if (Msg.Type == MessageType.TextMessage && !Blocked.Contains(User.Id))
                {
                    string Text = Msg.Text;
                    string Mention = "@" + Me.Username;

                    foreach (KeyValuePair<string, EventHandler<Message>> KVP in Commands)
                    {
                        if (Text.StartsWith("/" + KVP.Key))
                        {
                            string Substring = Text.Substring(KVP.Key.Length + 1);
                            if (Substring.StartsWith(Mention))
                            {
                                Substring = Substring.Substring(Mention.Length);
                            }

                            if (Substring == string.Empty || Substring.StartsWith(" "))
                            {
                                KVP.Value(Substring.Trim(), Msg);
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
        }

        /*private static void Api_UpdateReceived(object sender, UpdateEventArgs e)
        {
            $"Update {e.Update.InlineQuery?.Query}".Log();
        }*/

        public static void StopPoll()
        {
            Api = null;
            /*try
            {
                if (Api != null)
                {
                    //Api.StopReceiving();
                }
            }
            catch { }*/
        }
    }
}
