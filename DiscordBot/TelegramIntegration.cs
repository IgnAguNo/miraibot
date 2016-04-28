using DiscordBot.Commands;
using DiscordBot.Handlers;
using Google.Apis.YouTube.v3.Data;
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
            var Id = Db.GetDiscordServerId(TgChat);
            if (Id == 0 || !ServerData.Servers.ContainsKey(Id))
            {
                return null;
            }

            return ServerData.Servers[Id].Music;
        }

        public static async void Respond(Message Msg, string Text, bool DisablePreview = false)
        {
            try
            {
                await Api.SendTextMessage(Msg.Chat.Id, Text.Replace('`', '"'), DisablePreview, Msg.MessageId);
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
                var Music = GetMusic(e);
                if (Music != null)
                {
                    var Files = Directory.GetFiles(SongData.MusicDir).Where(x => x.EndsWith(".mp3") && x.ToLower().Contains(((string)s).ToLower())).ToArray();
                    if (Files.Length == 0)
                    {
                        Respond(e, Music.Enqueue((string)s));
                    }
                    else
                    {
                        Respond(e, Music.Enqueue(Files[0], true), true);
                    }
                }
            });

            Commands.Add("song", (s, e) =>
            {
                var Music = GetMusic(e);
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
                var Music = GetMusic(e);
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
                var Music = GetMusic(e);
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
                var Music = GetMusic(e);
                if (Music != null)
                {
                    var Removed = Music.Remove(s.ParseInts());

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

            $"Logged into Telegram as {Me.Username}".Log();

            Api.MessageReceived += Api_MessageReceived;
            Api.InlineQueryReceived += Api_InlineQueryReceived;
            Api.StartReceiving();
        }

        private static void Api_MessageReceived(object s, MessageEventArgs e)
        {
            try
            {
                Msgs++;
                var Msg = e.Message;
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

                    foreach (var KVP in Commands)
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

        private static async void Api_InlineQueryReceived(object s, InlineQueryEventArgs e)
        {
            try
            {
                var Results = new List<InlineQueryResult>();
                var Files = Directory.GetFiles(SongData.MusicDir).Where(x => x.EndsWith(".mp3") && x.ToLower().Contains(e.InlineQuery.Query)).ToArray();

                int i = 1;
                InlineQueryResultVideo Vid;

                if (Files.Length <= 20)
                {
                    foreach (var Result in Files)
                    {
                        Vid = new InlineQueryResultVideo();
                        Vid.Id = e.InlineQuery.Id + i++;
                        Vid.Title = Result.Substring(SongData.MusicDir.Length);
                        Vid.Title = Vid.Title.Substring(0, Vid.Title.Length - 4);
                        Vid.Description = "Local Mp3 File";
                        Vid.ThumbUrl = "https://github.com/google/material-design-icons/blob/master/av/2x_web/ic_play_arrow_black_48dp.png?raw=true";
                        Vid.MimeType = "video/mp4";
                        Vid.Url = "https://github.com/google/material-design-icons/blob/master/av/2x_web/ic_play_arrow_black_48dp.png?raw=true";
                        Vid.MessageText = $"/add@{Me.Username} {Vid.Title}";
                        Results.Add(Vid);
                    }
                }

                var ListRequest = Search.YT.Search.List("snippet");
                ListRequest.Q = e.InlineQuery.Query;
                ListRequest.MaxResults = 5;
                ListRequest.Type = "video";
                foreach (var Result in ListRequest.Execute().Items)
                {
                    Vid = new InlineQueryResultVideo();
                    Vid.Id = e.InlineQuery.Id + i++;
                    Vid.Title = Result.Snippet.Title;
                    Vid.Description = Result.Snippet.Description;
                    Vid.ThumbUrl = Result.Snippet.Thumbnails.Maxres?.Url ?? Result.Snippet.Thumbnails.Default__?.Url ?? string.Empty;
                    Vid.MimeType = "text/html";
                    Vid.Url = $"http://www.youtube.com/watch?v={Result.Id.VideoId}";
                    Vid.MessageText = $"/add@{Me.Username} http://www.youtube.com/watch?v={Result.Id.VideoId}";
                    Results.Add(Vid);
                }

                await Api.AnswerInlineQuery(e.InlineQuery.Id, Results.ToArray(), 0);
            }
            catch (Exception Ex)
            {
                Ex.Log();
            }
        }

        public static void StopPoll()
        {
            if (Api != null)
            {
                Api.StopReceiving();
                Api = null;
            }
        }
    }
}
