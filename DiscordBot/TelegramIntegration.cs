using DiscordBot.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiscordBot
{
    class TelegramIntegration
    {
        public static uint Msgs = 0;

        private static Api Api;
        private static User Me;
        public static ConcurrentDictionary<int, string> UsernameIdCache = new ConcurrentDictionary<int, string>();
        public static List<int> Blocked = new List<int>();
        private static Dictionary<string, EventHandler<MessageEventArgs>> Commands = new Dictionary<string, EventHandler<MessageEventArgs>>();
        public static ulong NextPairId = 0;

        private static MusicHandler GetMusic(MessageEventArgs e)
            => GetMusic(e.Message.Chat.Id);

        private static MusicHandler GetMusic(long TgChat)
        {
            ulong Id = Db.GetDiscordServerId(TgChat);
            if (Id == 0 || !ServerData.Servers.ContainsKey(Id))
            {
                return null;
            }

            return ServerData.Servers[Id].Music;
        }

        public static async void Respond(MessageEventArgs e, string Text)
        {
            try
            {
                await Api.SendTextMessage(e.Message.Chat.Id, Text, replyToMessageId: e.Message.MessageId);
            }
            catch
            {

            }
        }

        public static async void Start()
        {
            Api = new Api("");
            Me = await Api.GetMe();

            Commands.Add("add", (s, e) => 
            {
                MusicHandler Music = GetMusic(e);
                if (Music != null)
                {
                    SongData Song = new SongData(s);
                    if (Song.Found)
                    {
                        Music.DirectEnqueue(Song);
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
                            await Api.SendChatAction(e.Message.Chat.Id, ChatAction.Typing);

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

                            await Api.SendTextMessage(e.Message.Chat.Id, Music.GetCurrentPlaylist() + "\nWhich song should I remove?", replyToMessageId: e.Message.MessageId, replyMarkup: KeyboardMarkup);
                        }
                    }
                }
            });

            Commands.Add("pair", (s, e) =>
            {
                if (NextPairId > 0)
                {
                    Db.SetDiscordServerId(e.Message.Chat.Id, NextPairId);
                    Respond(e, "This chat has now been succesfully paired with " + ServerData.Servers[NextPairId].Name);

                    NextPairId = 0;
                }
            });

            //Api.ChosenInlineResultReceived += Api_ChosenInlineResultReceived;
            //Api.InlineQueryReceived += Api_InlineQueryReceived;
            Api.MessageReceived += Api_MessageReceived;
            //Api.UpdateReceived += Api_UpdateReceived;

            Api.StartReceiving();
            $"Logged into Telegram as {Me.Username}".Log();
        }

        private static void Api_ChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs e)
        {
            //$"ChosenInlineResult {e.ChosenInlineResult?.Query}".Log();
        }

        private static void Api_InlineQueryReceived(object sender, InlineQueryEventArgs e)
        {
            //$"Inline Query {e.InlineQuery?.Query}".Log();
        }

        private static void Api_MessageReceived(object sender, MessageEventArgs e)
        {
            //$"Message {e.Message.From.Username}".Log();
            try
            {
                Msgs++;
                var User = e.Message.From;

                string OldUsername;
                if (!UsernameIdCache.TryGetValue(User.Id, out OldUsername) || (User.Username != OldUsername && !UsernameIdCache.TryUpdate(User.Id, User.Username, OldUsername)))
                {
                    UsernameIdCache.TryAdd(User.Id, User.Username);
                }

                if (e.Message.Type == MessageType.TextMessage && !Blocked.Contains(User.Id))
                {
                    string Text = e.Message.Text;
                    string Mention = "@" + Me.Username;

                    foreach (KeyValuePair<string, EventHandler<MessageEventArgs>> KVP in Commands)
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
        }

        private static void Api_UpdateReceived(object sender, UpdateEventArgs e)
        {
            //$"Update {e.Update.InlineQuery?.Query}".Log();
        }
    }
}
