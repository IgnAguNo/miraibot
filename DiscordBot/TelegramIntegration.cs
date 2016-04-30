using DiscordBot.Commands;
using DiscordBot.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

        public static async void Respond(Message Msg, string Text, bool DisablePreview = true, ReplyMarkup Markup = null)
        {
            try
            {
                await Api.SendTextMessage(Msg.Chat.Id, Text.Replace('`', '"'), DisablePreview, Msg.MessageId, Markup);
            }
            catch { }
        }

        public static async Task Respond(string InlineId, Message Msg, string Text, bool DisablePreview = true, ReplyMarkup Markup = null)
        {
            Text = Text.Replace('`', '"');

            if (InlineId != null)
            {
                try
                {
                    await Api.EditInlineMessageText(InlineId, Text, disableWebPagePreview: DisablePreview);
                    await Api.EditInlineMessageReplyMarkup(InlineId, Markup);
                }
                catch (Newtonsoft.Json.JsonSerializationException) { }
            }
            else
            {
                Respond(Msg, Text, DisablePreview, Markup);
            }
        }

        private static async Task<string[]> ParseInlineId(string Command)
        {
            long ResultId = -1;
            string InlineId = null;

            var Explode = Command.Split(new char[] { ' ' }, 2);
            if (Explode[0].StartsWith("[") && Explode[0].EndsWith("]") && long.TryParse(Explode[0].Substring(1, Explode[0].Length - 2), out ResultId) && ResultId > 0)
            {
                for (int i = 0; i < 15; i++)
                {
                    if (InlineMessageIds.TryRemove(ResultId, out InlineId))
                    {
                        Command = Explode.Length == 1 ? string.Empty : Explode[1];
                        break;
                    }

                    await Task.Delay(100);
                }
            }

            return new string[] { InlineId, Command };
        }

        public static async void Start(string ApiKey)
        {
            try
            {
                Api = new Api(ApiKey);
                Me = await Api.GetMe();
            }
            catch (Exception Ex)
            {
                Ex.Log();
                "Telegram integration could not be loaded".Log();
                return;
            }

            Commands.Add("add", async (s, e) =>
            {
                var Music = GetMusic(e);
                if (Music != null && (string)s != string.Empty)
                {
                    string[] Data = await ParseInlineId((string)s);
                    if (Data[0] != null)
                    {
                        await Respond(Data[0], e, "Resolving song..");
                    }

                    var Files = Directory.GetFiles(SongData.MusicDir).Where(x => x.EndsWith(".mp3") && x.ToLower().Contains(Data[1].ToLower())).ToArray();
                    string Song;
                    if (Files.Length == 0)
                    {
                        Song = Music.Enqueue(Data[1]);
                    }
                    else
                    {
                        Song = Music.Enqueue(Files[0], true);
                    }

                    await Respond(Data[0], e, Song);
                }
            });

            Commands.Add("song", async (s, e) =>
            {
                var Music = GetMusic(e);
                if (Music != null)
                {
                    string[] Data = await ParseInlineId((string)s);
                    string Song = Music.GetCurrentSong()?.FullName;
                    if (Song == null)
                    {
                        await Respond(Data[0], e, "Nothing is playing right now");
                    }
                    else
                    {
                        await Respond(Data[0], e, "I'm playing " + Song);
                    }
                }
            });

            Commands.Add("queue", async (s, e) =>
            {
                var Music = GetMusic(e);
                if (Music != null)
                {
                    string[] Data = await ParseInlineId((string)s);
                    string Playlist = Music.GetCurrentPlaylist();
                    if (Playlist == string.Empty)
                    {
                        await Respond(Data[0], e, "The queue is empty");
                    }
                    else
                    {
                        await Respond(Data[0], e, Playlist);
                    }
                }
            });

            Commands.Add("skip", async (s, e) =>
            {
                var Music = GetMusic(e);
                if (Music != null)
                {
                    string[] Data = await ParseInlineId((string)s);
                    if (Music.Playing)
                    {
                        string NextTitle = Music.PeekNextName();
                        if (NextTitle == null)
                        {
                            await Respond(Data[0], e, "Skipped, no more songs left");
                        }
                        else
                        {
                            await Respond(Data[0], e, "Skipped to " + NextTitle.Compact(100));
                        }

                        Music.Skip();
                    }
                    else
                    {
                        await Respond(Data[0], e, "I'm not playing any music at the moment");
                    }
                }
            });

            Commands.Add("remove", async (s, e) =>
            {
                var Music = GetMusic(e);
                if (Music != null)
                {
                    string[] Data = await ParseInlineId((string)s);
                    var Removed = Music.Remove(Data[1].ParseInts());

                    if (Removed.Count > 0)
                    {
                        await Respond(Data[0], e, "Removed " + Removed.Join(", "));
                    }
                    else
                    {
                        await Respond(Data[0], e, "I couldn't remove that song");
                    }
                    /*else
                    {
                        int Count = Music.SongCount;
                        if (Count == 0)
                        {
                            await Respond(Data[0], e, "There is nothing I can remove");
                        }
                        else
                        {
                            var KeyboardMarkup = new ReplyKeyboardMarkup();
                            int Row = -1;

                            KeyboardMarkup.Keyboard = new KeyboardButton[(int)Math.Ceiling((double)Count / 2)][];
                            for (int i = 0; i < (2 * KeyboardMarkup.Keyboard.Length); i++)
                            {
                                if (i % 2 == 0)
                                {
                                    KeyboardMarkup.Keyboard[++Row] = new KeyboardButton[2];
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

                            await Respond(Data[0], e, Music.GetCurrentPlaylist() + "\nWhich song should I remove?", Markup: KeyboardMarkup);
                        }
                    }*/
                }
            });

            Commands.Add("pair", (s, e) =>
            {
                if (NextPairChannel != null)
                {
                    Db.SetDiscordServerId(e.Chat.Id, NextPairChannel.Server.Id);
                    Respond(e, "This chat has now been succesfully paired with " + NextPairChannel.Server.Name);
                    Bot.Send(NextPairChannel, "This server has been paired with " + ((e.Chat.Type == ChatType.Private) ? "" : e.Chat.Title + " by ") + e.From.Username);

                    NextPairChannel = null;
                }
            });

            Commands.Add("hrc", (s, e) =>
            {
                try
                {
                    if (e.Chat.Type == ChatType.Private || e.Chat.Title.StartsWith("H"))
                    {
                        Respond(e, Lewd.GetRandomLewd(s, (e.From.Username != "Akkey" && e.Chat.Type != ChatType.Private)), false);
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
            Api.ChosenInlineResultReceived += Api_ChosenInlineResultReceived;
            Api.CallbackQueryReceived += Api_CallbackQueryReceived;
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
                            string SubText = Text.Substring(KVP.Key.Length + 1);
                            if (SubText.StartsWith(Mention))
                            {
                                SubText = SubText.Substring(Mention.Length);
                            }

                            if (SubText == string.Empty || SubText.StartsWith(" "))
                            {
                                KVP.Value(SubText.Trim(), Msg);
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

        private static InlineKeyboardMarkup IKM(string Text, string Url, string CallbackData)
        {
            var Markup = new InlineKeyboardMarkup();
            Markup.InlineKeyboard = new InlineKeyboardButton[][] { new InlineKeyboardButton[] { new InlineKeyboardButton() } };
            Markup.InlineKeyboard[0][0].Text = Text;
            Markup.InlineKeyboard[0][0].Url = Url;
            Markup.InlineKeyboard[0][0].CallbackData = CallbackData;
            return Markup;
        }

        private static long Identifier = 0;
        private static async void Api_InlineQueryReceived(object s, InlineQueryEventArgs e)
        {
            const int MaxResults = 20;

            try
            {
                long MsgId = Interlocked.Increment(ref Identifier) * MaxResults;
                var Results = new List<InlineQueryResult>();
                InlineQueryResultVideo Result;

                int i = 0;

                if (e.InlineQuery.Query == "remove")
                {
                    while (i < MaxResults)
                    {
                        Result = Result = new InlineQueryResultVideo();
                        Result.Id = (MsgId + i++).ToString();
                        Result.Title = "Remove Song";
                        Result.Description = $"#{i}";
                        Result.ThumbUrl = "https://github.com/google/material-design-icons/blob/master/av/2x_web/ic_note_black_48dp.png?raw=true";
                        Result.MimeType = "text/html";
                        Result.Url = Result.ThumbUrl;
                        Result.InputMessageContent = new InputTextMessageContent();
                        ((InputTextMessageContent)Result.InputMessageContent).MessageText = $"/remove@{Me.Username} [{MsgId}] {i}";
                        Result.ReplyMarkup = IKM("Loading..", "http://www.google.nl", "/");

                        Results.Add(Result);
                    }
                }
                else
                {
                    foreach (var Key in new string[] { "song", "queue", "skip" })
                    {
                        if (Key.StartsWith(e.InlineQuery.Query))
                        {
                            Result = Result = new InlineQueryResultVideo();
                            Result.Id = (MsgId + i++).ToString();
                            Result.Title = "Use command";
                            Result.Description = Key;
                            Result.ThumbUrl = "https://github.com/google/material-design-icons/blob/master/av/2x_web/ic_note_black_48dp.png?raw=true";
                            Result.MimeType = "text/html";
                            Result.Url = Result.ThumbUrl;
                            Result.InputMessageContent = new InputTextMessageContent();
                            ((InputTextMessageContent)Result.InputMessageContent).MessageText = $"/{Key}@{Me.Username} [{MsgId}]";
                            Result.ReplyMarkup = IKM("Loading..", "http://www.google.com", "/");

                            Results.Add(Result);
                        }
                    }

                    var Files = Directory.GetFiles(SongData.MusicDir).Where(x => x.EndsWith(".mp3") && x.ToLower().Contains(e.InlineQuery.Query)).ToArray();

                    var ListRequest = Search.YT.Search.List("snippet");
                    ListRequest.MaxResults = MaxResults - i;

                    if (Files.Length < ListRequest.MaxResults)
                    {
                        ListRequest.MaxResults -= Files.Length;

                        foreach (var File in Files)
                        {
                            Result = new InlineQueryResultVideo();
                            Result.Id = (MsgId + i++).ToString();
                            Result.Title = File.Substring(SongData.MusicDir.Length);
                            Result.Title = Result.Title.Substring(0, Result.Title.Length - 4);
                            Result.Caption = Result.Title;
                            Result.Description = "Local Mp3 File";
                            Result.ThumbUrl = "https://github.com/google/material-design-icons/blob/master/av/2x_web/ic_play_arrow_black_48dp.png?raw=true";
                            Result.MimeType = "video/mp4";
                            Result.Url = Result.ThumbUrl;
                            Result.InputMessageContent = new InputTextMessageContent();
                            ((InputTextMessageContent)Result.InputMessageContent).MessageText = $"/add@{Me.Username} [{MsgId}] {Result.Title}";
                            Result.ReplyMarkup = IKM("Loading..", Result.ThumbUrl, "/");
                            Results.Add(Result);
                        }
                    }

                    if (e.InlineQuery.Query.IsValidUrl() && ListRequest.MaxResults != 0)
                    {
                        ListRequest.MaxResults--;

                        Result = new InlineQueryResultVideo();
                        Result.Id = (MsgId + i++).ToString();
                        Result.Title = e.InlineQuery.Query;
                        Result.Caption = Result.Title;
                        Result.Description = "Remote File";
                        Result.ThumbUrl = "https://github.com/google/material-design-icons/blob/master/av/2x_web/ic_play_arrow_black_48dp.png?raw=true";
                        Result.MimeType = "video/mp4";
                        Result.Url = Result.ThumbUrl;
                        Result.InputMessageContent = new InputTextMessageContent();
                        ((InputTextMessageContent)Result.InputMessageContent).MessageText = $"/add@{Me.Username} [{MsgId}] {e.InlineQuery.Query}";
                        Result.ReplyMarkup = IKM("Loading..", Result.ThumbUrl, "/");
                        Results.Add(Result);
                    }

                    if (ListRequest.MaxResults != 0)
                    {
                        ListRequest.Q = e.InlineQuery.Query;
                        ListRequest.Type = "video";

                        foreach (var Video in ListRequest.Execute().Items)
                        {
                            Result = new InlineQueryResultVideo();
                            Result.Id = (MsgId + i++).ToString();
                            Result.Title = Video.Snippet.Title;
                            Result.Caption = Result.Title;
                            Result.Description = Video.Snippet.Description;
                            Result.ThumbUrl = Video.Snippet.Thumbnails.Maxres?.Url ?? Video.Snippet.Thumbnails.Default__?.Url ?? string.Empty;
                            Result.MimeType = "text/html";
                            Result.Url = $"https://youtu.be/{Video.Id.VideoId}";
                            Result.InputMessageContent = new InputTextMessageContent();
                            ((InputTextMessageContent)Result.InputMessageContent).MessageText = $"/add@{Me.Username} [{MsgId}] {Result.Url}";
                            Result.ReplyMarkup = IKM("Open Song", Result.Url, "/");
                            Results.Add(Result);
                        }
                    }
                }

                await Api.AnswerInlineQuery(e.InlineQuery.Id, Results.ToArray(), 0);
            }
            catch (Exception Ex)
            {
                Ex.Log();
            }
        }

        private static ConcurrentDictionary<long, string> InlineMessageIds = new ConcurrentDictionary<long, string>();

        private static async void Api_ChosenInlineResultReceived(object s, ChosenInlineResultEventArgs e)
        {
            try
            {
                long Result;
                if (long.TryParse(e.ChosenInlineResult.ResultId, out Result))
                {
                    Result -= (Result % 10);
                    if (InlineMessageIds.TryAdd(Result, e.ChosenInlineResult.InlineMessageId))
                    {
                        await Task.Delay(3000);
                        string Value;
                        if (InlineMessageIds.TryRemove(Result, out Value))
                        {
                            "The inline message id was not used".Log();
                        }
                    }
                }
                else
                {
                    "Could not add the inline message id".Log();
                }

                //await Api.EditInlineMessageText(e.ChosenInlineResult.InlineMessageId, "Added Song");
            }
            catch (Exception Ex)
            {
                Ex.Log();
            }
        }

        private static void Api_CallbackQueryReceived(object s, CallbackQueryEventArgs e)
        {
            //await Api.EditInlineMessageText(e.CallbackQuery.InlineMessageId, "test2");
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
