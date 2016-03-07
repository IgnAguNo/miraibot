﻿using Discord;
using Discord.Audio;
using DiscordBot.Commands;
using DiscordBot.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace DiscordBot
{
    class Bot
    {
        public static DiscordClient Client;

        public const string CredentialsFile = "data.credentials.txt";

        public static string Mail;
        public static string Password;
        public static ulong Owner; //Amir 74779725393825792
        public static string GoogleAPI = "AIzaSyAVrXiAHfLEbQbNJP80zbTuW2jL0wuEigQ";
        public static string SoundCloudAPI = "5c28ed4e5aef8098723bcd665d09041d";
        public static string MashapeAPI = "2OuTDTmiT6mshgokCwR10VwkNI40p125gP1jsnofSaiWBJFcUf";
        public static string AniIdAPI = "amirz-i0ev1";
        public static string AniSecretAPI = "E7HB4bm9SJ3wbfc5klnv1I";
        public static string Mention
        {
            get
            {
                return Bot.Client.CurrentUser.Mention;
            }
        }
        
        private static uint Msgs = 0;
        private static uint Spam = 0;
        
        public static User OwnerAccount = null;

        static void Main(string[] args)
        {
            DateTime Start = DateTime.Now;
            Console.Title = "Loading..";

            /*
            string[] Splitted = File.ReadAllText("data.test.txt").Split(new string[] { "id=\"" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < Splitted.Length; i++)
            {
                File.AppendAllText("data.test2.txt", "                \"http://i.imgur.com/" + Splitted[i].Split('"')[0] + ".png\",\r\n");
            }
            return;
            /*
            */

            try
            {
                string[] Credentials = File.ReadAllText(CredentialsFile).Replace("\r", string.Empty).Split('\n');
                Mail = Credentials[0];
                Password = Credentials[1];

                if (Credentials.Length != 3 || !ulong.TryParse(Credentials[2], out Owner))
                {
                    "Couldn't load owner id from credentials".Log();
                }
            }
            catch (Exception Ex)
            {
                Ex.Log();
                Console.ReadKey();
                return;
            }
            
            Client = new DiscordClient();

            Client.AddService(new AudioService(new AudioServiceConfigBuilder()
            {
                Channels = 2,
                EnableEncryption = false,
                EnableMultiserver = true,
                Bitrate = AudioServiceConfig.MaxBitrate,
                BufferLength = 50,
                Mode = AudioMode.Outgoing
            }));

            MusicHandler.Buffers = new ByteBuffer(1920 * 2, (int)Math.Pow(2, 16));
            "Discord Audio Client Service Loaded".Log();

            Client.Log.Message += ClientEvents.LogMessage;
            Client.MessageReceived += ClientEvents.MessageReceived;
            Client.UserJoined += ClientEvents.UserJoined;
            Client.UserLeft += ClientEvents.UserLeft;
            "Handlers Loaded".Log();
            
            Client.ExecuteAndWait(async () =>
            {
                await Client.Connect(Mail, Password);

                int ChannelCount = 0;
                foreach (Server Server in Client.Servers)
                {
                    foreach (User User in Server.Users)
                    {
                        Db.ForceAddAccount(User.Id);
                    }

                    ServerData.Servers.Add(Server.Id, new ServerData(Server));
                    ChannelCount += Server.TextChannels.Count();
                    
                    if (OwnerAccount == null)
                    {
                        OwnerAccount = Server.GetUser(Owner);
                    }
                }

                if (OwnerAccount != null)
                {
                    $"Set {OwnerAccount.Name} as owner".Log();
                }

                $"Joined {ChannelCount} channels in {Client.Servers.Count()} servers".Log();

                InitCommands();
                "Initialised commands".Log();

                Timer Updater = new Timer(1000);
                Updater.Elapsed += (s, e) =>
                {
                    try
                    {
                        Console.Title = $"[@{Client.CurrentUser.Name}] {CommandParser.Executed} Command Executed - {Msgs} Messages Sent - {Spam} Spam Blocked - Running {(DateTime.Now - Start).ToString("%d")} days, {(DateTime.Now - Start).ToString(@"%h\:mm\:ss")}";
                        int Playing = ServerData.Servers.Count(x => x.Value.Music.Playing);
                        Client.SetGame(Playing + " song" + (Playing == 1 ? "" : "s"));
                    }
                    catch (Exception Ex)
                    {
                        $"IntervalUpdateException: {Ex}".Log();
                    }
                };
                Updater.AutoReset = true;
                Updater.Start();

                "Done!".Log();
            });
        }

        public static async void Send(Channel Channel, string Message, Stream Stream = null, bool SpamProtection = true)
        {
            try
            {
                if (Message == null || Message == string.Empty)
                {
                    return;
                }

                if (SpamProtection && Client.MessageQueue.Count > 3)
                {
                    $"Spam: {Message.Compact()}".Log();
                    Client.MessageQueue.Clear();
                    Spam++;

                    return;
                }

                int Max = 2000;
                if (Message.Length > Max)
                {
                    Message = Message.Substring(0, Max - 3) + "...";
                }

                Msgs++;
                if (Stream != null)
                {
                    await Channel.SendFile(Message, Stream);
                }
                else
                {
                    await Channel.SendMessage(Message);
                }
            }
            catch (Exception Ex)
            {
                Client.Log.Log(LogSeverity.Error, Channel.Name, Message.Compact(), Ex);
            }
        }

        public static async void Shutdown()
        {
            await Task.Delay(300);
            await Client.Disconnect();
        }

        private static void InitCommands()
        {
            CommandParser.Categories.Add(string.Empty, new Command[] {
                new Command(Command.PrefixType.Command, new string[] { "help", "commands" }, "Shows all your commands", CommandParser.Help),
                new Command(Command.PrefixType.Command, new string[] { "toggle", "togglecat" }, "Turns a category on/off", CommandParser.ToggleCat)
            });

            CommandParser.Categories.Add(typeof(Administration).Name, new Command[] {
                new Command(Command.PrefixType.Command, "minrank", "Sets a necessary rank to use a command", Administration.Permission),
                new Command(Command.PrefixType.Command, "giverank", "Sets someone's rank", Administration.Rank),
                new Command(Command.PrefixType.Command, "ranks", "See all special ranks in this server", Administration.Ranks),

                new Command(Command.PrefixType.Command, new string[] { "sleep", "shutdown" }, "Shuts me down", Administration.Sleep),
                new Command(Command.PrefixType.Command, "setname", "Changes my name", Administration.SetName),
                new Command(Command.PrefixType.Command, "setavatar", "Changes my avatar", Administration.SetAvatar),
                new Command(Command.PrefixType.Command, "clear", "Removes some message history", Administration.Clear),
                new Command(Command.PrefixType.Command, "fix", "Clears the message queue", Administration.Fix)
            });

            CommandParser.Categories.Add(typeof(Music).Name, new Command[] {
                new Command(Command.PrefixType.Command, "join", "Joins your current voice channel", Music.Join),
                new Command(Command.PrefixType.Command, "leave", "Leaves any voice channel", Music.Leave),
                new Command(Command.PrefixType.Command, new string[] { "add", "q" }, "Adds a song title to the music queue", Music.Add),
                new Command(Command.PrefixType.Command, new string[] { "local", "addlocal" }, "Adds a local song title to the music queue", Music.Local),
                new Command(Command.PrefixType.Command, new string[] { "push", "p" }, "Pushes a song to the top of the music queue", Music.Push),
                new Command(Command.PrefixType.Command, new string[] { "remove", "r" }, "Removes a song from the music queue", Music.Remove),
                new Command(Command.PrefixType.Command, new string[] { "volume", "vol" }, "Changes the volume of the music player", Music.Volume),
                new Command(Command.PrefixType.Command, new string[] { "playing", "song", "np" }, "Shows the current song", Music.CurrentSong),
                new Command(Command.PrefixType.Command, new string[] { "playlist", "lq", "queue" }, "Lists the current playlist", Music.Playlist),
                new Command(Command.PrefixType.Command, new string[] { "skip", "next", "n" }, "Skips the current song", Music.Skip),
                new Command(Command.PrefixType.Command, new string[] { "shuffle", "s" }, "Shuffles the current queue", Music.Shuffle),
                new Command(Command.PrefixType.Command, "save", "Saves the current playlist", Music.Save),
                new Command(Command.PrefixType.Command, "load", "Loads the current playlist from", Music.Load)
            });

            CommandParser.Categories.Add(typeof(Trivia).Name, new Command[] {
                new Command(Command.PrefixType.Command, new string[] { "starttrivia", "t" }, "Starts a trivia match in your channel", Trivia.Start),
                new Command(Command.PrefixType.Command, new string[] { "leaderboard", "tl" }, "Shows the current trivia's leaderboard", Trivia.Leaderboards),
                new Command(Command.PrefixType.Command, new string[] { "stoptrivia", "tq" }, "Stops a trivia match in your channel", Trivia.Stop)
            });

            CommandParser.Categories.Add(typeof(Search).Name, new Command[] {
                new Command(Command.PrefixType.Command, new string[] { "ask", "8ball" }, "Ask me a question", Search.Ask),
                new Command(Command.PrefixType.Command, new string[] { "youtube", "yt" }, "Searches for a youtube video", Search.Youtube),
                new Command(Command.PrefixType.Command, new string[] { "image", "img" }, "Search for an image", Search.Image),
                new Command(Command.PrefixType.Command, "osu", "Show someone's osu stats", Search.Osu),
                new Command(Command.PrefixType.Command, new string[] { "avatar", "av" }, "Show someone's avatar", Search.Avatar),
                new Command(Command.PrefixType.Command, new string[] { "define", "ud" }, "Search for a term", Search.Define),
                new Command(Command.PrefixType.Command, new string[] { "lewd", "booru", "nsfw" }, "Search for a lewd image", Search.Lewd),
                new Command(Command.PrefixType.Command, "anime", "Search for an anime - shorthand {name}", Search.AnimeInfo),
                new Command(Command.PrefixType.Command, "manga", "Search for a manga - shorthand <name>", Search.MangaInfo)
            });

            string Spam = "O - oooooooooo AAAAE - A - A - I - A - U - JO - oooooooooooo AAE - O - A - A - U - U - A - E - eee - ee - eee AAAAE - A - E - I - E - A - JO - ooo - oo - oo - oo EEEEO - A - AAA - AAAA";
            string FullSpam = "";

            while (FullSpam.Length + Spam.Length < 1992)
            {
                FullSpam += Spam + "\n";
            }

            CommandParser.Categories.Add(typeof(Conversation).Name, new Command[] {
                new Command(Command.PrefixType.Mention, new string[] { "choose from" }, "Choose from a list", Conversation.Choose),
                new Command(Command.PrefixType.Mention, "how are you", "Check if my owner is online", Conversation.Status),
                new Command(Command.PrefixType.Mention, new string[] { "do you like me", "do you love me" }, "...", Conversation.Love),
                new Command(Command.PrefixType.Mention, new string[] { "insult", "hate on" }, "Insult a person", Conversation.Insult),
                new Command(Command.PrefixType.Mention, new string[] { "praise", "compliment" }, "Praise a person", Conversation.Praise),
                new Command(Command.PrefixType.Mention, new string[] { "attack", "stab" }, "Stab a person", Conversation.Stab),
                new Command(Command.PrefixType.Mention, new string[] { "welcome", "say hi to" }, "Welcome someone", Conversation.Hi),
                new Command(Command.PrefixType.Mention, "say bye to", "Say bye to someone", Conversation.Bye),
                new Command(Command.PrefixType.Mention, new string[] { "go out with me", "will you go out with me" }, "...", Conversation.GoOut),
                new Command(Command.PrefixType.Mention, new string[] { "you're best girl", "you are best girl" }, "Compliment me", Conversation.Best),
                new Command(Command.PrefixType.Mention, new string[] { "you're not best girl", "you are not best girl", "cry" }, "Don't be heartless", Conversation.Cry),
                new Command(Command.PrefixType.Mention, new string[] { "take it", "take this", "here" }, "Give me a reward", Conversation.TakeIt),
                new Command(Command.PrefixType.Mention, new string[] { "no bully", "stop bully" }, "Tell me to enforce a no bully zone", Conversation.NoBully),
                new Command(Command.PrefixType.Mention, "sing", "Ask me to sing", Conversation.Sing),
                new Command(Command.PrefixType.Mention, "dance", "Ask me to dance", Conversation.Dance),
                new Command(Command.PrefixType.Mention, new string[] { "good night", "bye" }, "Say good night to me", Conversation.GoodNight),
                new Command(Command.PrefixType.Mention, new string[] { "watch out", "trip", "stop" }, "Will make me trip", Conversation.Trip),
                new Command(Command.PrefixType.Mention, new string[] { "you're weird", "you are weird" }, "Make me sad", Conversation.Weird),
                new Command(Command.PrefixType.Mention, new string[] { "you're cute", "you are cute" }, "Make me happy", Conversation.Cute),
                new Command(Command.PrefixType.Mention, new string[] { "do you even lewd", "try to be lewd" }, "Send semi-lewd pictures", Conversation.Lewd),
                new Command(Command.PrefixType.Mention, new string[] { "what's", "what is", "who's", "who is" }, "Search for a term", Search.Define),
                new Command(Command.PrefixType.Mention, new string[] { "what are", "what're" }, "Search for a plural term", Search.DefineSimple),
                //new Command(Command.PrefixType.Mention, "shitpost", "Send a shitpost", Conversation.Shitpost),
                //new Command(Command.PrefixType.Mention, new string[] { "send oc", "stealie" }, "Stealie a mealie", Conversation.Dogman),

                new Command(Command.PrefixType.None, "megane", "", "Fuyukai desu!"),
                new Command(Command.PrefixType.None, "burn the chat", "", "🔥 ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้ 🔥"),
                new Command(Command.PrefixType.None, "kuriyama?", "", "Yes?"),
                new Command(Command.PrefixType.None, "mirai?", "", "Please call me Kuriyama"),
                new Command(Command.PrefixType.None, new string[] { "fuck mirai", "fuck you mirai" }, "", Conversation.Cry),
                
                new Command(Command.PrefixType.None, "aaaae", "", "**__" + FullSpam + "__**"),
                new Command(Command.PrefixType.None, "fap", "", "ಠ.ಠ"),
                new Command(Command.PrefixType.None, "fuyukai desu", "", "That's my joke!"),
                new Command(Command.PrefixType.None, "\\o\\", "", "/o/"),
                new Command(Command.PrefixType.None, "/o/", "", "\\o\\"),
                new Command(Command.PrefixType.None, "\\o/", "", "\\o/"),
                new Command(Command.PrefixType.None, "$$$", "", Trivia.Points)
            });
        }
    }
}
