using Discord;
using Discord.Audio;
using DiscordBot.Commands;
using DiscordBot.Handlers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;

namespace DiscordBot
{
    class Bot
    {
        private static uint Msgs = 0;
        private static uint Spam = 0;

        public static DateTime Start = DateTime.Now;
        public static DiscordClient Client;
        public static string Mention
        {
            get
            {
                return Client.CurrentUser.Mention;
            }
        }

        public const string CredentialsFile = "data.credentials.txt";
        public const string TelegramFile = "data.telegram.txt";

        public static string Token;
        public static ulong AppId = 0;
        public static string InviteLink
        {
            get
            {
                if (AppId == 0)
                {
                    return "No invite link found";
                }

                return $"https://discordapp.com/oauth2/authorize?&client_id={AppId}&scope=bot";
            }
        }
        public static ulong Owner; //Amir 74779725393825792
        public static string MainDir = "./";
        public const string GoogleAPI = "AIzaSyAVrXiAHfLEbQbNJP80zbTuW2jL0wuEigQ";
        public const string SoundCloudAPI = "5c28ed4e5aef8098723bcd665d09041d";
        public const string MashapeAPI = "2OuTDTmiT6mshgokCwR10VwkNI40p125gP1jsnofSaiWBJFcUf";
        public const string AniIdAPI = "amirz-i0ev1";
        public const string AniSecretAPI = "E7HB4bm9SJ3wbfc5klnv1I";
        
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var StartInfo = Process.GetCurrentProcess().StartInfo;
                StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(StartInfo);

                Shutdown();
            };

            int Width = 93;
            int Height = 23;

            Console.SetWindowSize(Width, Height);
            Console.SetBufferSize(Width, 1024);

            Console.Title = "Loading..";

            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(string.Empty.PadLeft(Width - 1, '-') +
                "\n [<< Kuriyama Mirai Bot for Discord created by Amir Zaidi, built on the Discord.Net API >>] \n" +
                string.Empty.PadLeft(Width - 1, '-'));

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

            if (!File.Exists(CredentialsFile))
            {
                $"{CredentialsFile} not found".Log();
                Console.ReadKey();
                return;
            }

            string[] Credentials = File.ReadAllText(CredentialsFile).Replace("\r", string.Empty).Split('\n');
            Token = Credentials[0];
            if (ulong.TryParse(Credentials[1], out AppId))
            {
                InviteLink.Log();
            }

            if (Credentials.Length <= 2 || !ulong.TryParse(Credentials[2], out Owner))
            {
                "Couldn't load owner id from credentials".Log();
            }

            if (Credentials.Length > 3)
            {
                MainDir = Credentials[3];
            }

            Client = new DiscordClient();
            Client.AddService(new AudioService(new AudioServiceConfigBuilder()
            {
                Channels = 2,
                EnableEncryption = false,
                EnableMultiserver = true,
                Bitrate = AudioServiceConfig.MaxBitrate,
                BufferLength = 1000,
                Mode = AudioMode.Outgoing
            }));

            MusicHandler.Buffers = new ByteBuffer(1920 * 2, (int)Math.Pow(2, 16));

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Client.ExecuteAndWait(async () =>
                    {
                        await Client.Connect(Token);

                        InitCommands();

                        Client.Log.Message += ClientEvents.LogMessage;
                        Client.MessageReceived += ClientEvents.MessageReceived;

                        Client.UserJoined += ClientEvents.UserJoined;
                        Client.UserLeft += ClientEvents.UserLeft;

                        Client.JoinedServer += ClientEvents.JoinedServer;
                        Client.ServerAvailable += ClientEvents.ServerAvailable;
                        Client.LeftServer += ClientEvents.LeftServer;

                        ulong RefreshCount = 0;
                        Timer Updater = new Timer(1000);
                        Updater.Elapsed += (s, e) =>
                        {
                            try
                            {
                                Console.Title = $"[@{Client.CurrentUser.Name}] {CommandParser.Executed} Commands - {Msgs} Messages Sent - {Spam} Blocked";

                                if (TelegramIntegration.Me != null)
                                {
                                    Console.Title += $" - {TelegramIntegration.Msgs} Telegram Messages";
                                }

                                Console.Title += $" - {(DateTime.Now - Start).ToString("%d")} days, {(DateTime.Now - Start).ToString(@"%h\:mm\:ss")}";

                                if (RefreshCount++ % 15 == 0)
                                {
                                    int Playing = ServerData.Servers.Count(x => x.Value.Music.Playing);
                                    Client.SetGame("music in " + Playing + " server" + (Playing == 1 ? "" : "s"));
                                }
                            }
                            catch (Exception Ex)
                            {
                                $"IntervalUpdateException: {Ex}".Log();
                            }
                        };
                        Updater.AutoReset = true;
                        Updater.Start();

                        if (File.Exists(TelegramFile))
                        {
                            await TelegramIntegration.Start(File.ReadAllText(TelegramFile).Trim());
                            $"Logged into Telegram as {TelegramIntegration.Me.Username}".Log();
                        }

                        handler = new ConsoleEventDelegate(ConsoleEventCallback);
                        SetConsoleCtrlHandler(handler, true);

                        "Booted! Waiting for input".Log();
                    });

                    break;
                }
                catch (Discord.Net.WebSocketException) { }
            }
        }

        public static void Send(Channel Channel, string Message, Stream Stream = null, bool SpamProtection = true)
        {
            Task<Message> Sending = SendAsync(Channel, Message, Stream, SpamProtection);
            if (Sending != null)
            {
                Sending.Wait();
            }
        }

        public static async Task<Message> SendAsync(Channel Channel, string Message, Stream Stream = null, bool SpamProtection = true)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    if (Channel == null || Message == null || Message == string.Empty || !Channel.GetUser(Client.CurrentUser.Id).GetPermissions(Channel).SendMessages)
                    {
                        return null;
                    }

                    if (SpamProtection && Client.MessageQueue.Count > 3)
                    {
                        $"Spam: {Message.Compact()}".Log();
                        Client.MessageQueue.Clear();
                        Spam++;

                        return null;
                    }

                    int Max = 2000;
                    if (Message.Length > Max)
                    {
                        Message = Message.Substring(0, Max - 3) + "...";
                    }

                    Msgs++;
                    if (Stream != null)
                    {
                        return await Channel.SendFile(Message, Stream);
                    }
                    else
                    {
                        return await Channel.SendMessage(Message);
                    }
                }
                catch (Exception Ex)
                {
                    Client.Log.Log(LogSeverity.Error, Channel.Name, Message.Compact(), Ex);
                    await Task.Delay(500);
                }
            }

            return null;
        }

        public static void Shutdown()
        {
            if (Client != null)
            {
                try
                {
                    "Disconnecting..".Log();

                    foreach (ServerData SD in ServerData.Servers.Values)
                    {
                        $"{SD.Music.Save(SD.Server)} songs saved in {SD.Name}".Log();
                        SD.StopHandlers();
                    }

                    //Task.Delay(350).Wait();
                    Client.Disconnect().Wait();
                }
                catch { }
                finally
                {
                    Client = null;
                }
            }
        }

        private static void InitCommands()
        {
            CommandParser.Categories.Add(string.Empty, new Command[] {
                new Command(Command.PrefixType.Command, new string[] { "help", "commands" }, "Shows all your commands", CommandParser.Help),
                new Command(Command.PrefixType.Command, new string[] { "toggle", "togglecat" }, "Turns a category on/off", CommandParser.ToggleCat)
            });

            CommandParser.Categories.Add(typeof(Administration).Name, new Command[] {
                new Command(Command.PrefixType.Command, "minrank", "Sets a necessary rank to use a command", Administration.Permission),
                new Command(Command.PrefixType.Command, new string[] { "giverank", "setrank" }, "Sets someone's rank", Administration.Rank),
                new Command(Command.PrefixType.Command, "ranks", "See all special ranks in this server", Administration.Ranks),

                new Command(Command.PrefixType.Command, new string[] { "sleep", "shutdown" }, "Shuts me down", Administration.Sleep),
                new Command(Command.PrefixType.Command, "setname", "Changes my name", Administration.SetName),
                new Command(Command.PrefixType.Command, "setavatar", "Changes my avatar", Administration.SetAvatar),
                new Command(Command.PrefixType.Command, "prune", "Removes some message history", Administration.Prune),
                new Command(Command.PrefixType.Command, "fix", "Clears the message queue", Administration.Fix),
                new Command(Command.PrefixType.Command, "joinserver", "Sends the invite link to add me", Administration.JoinServer),
                new Command(Command.PrefixType.Command, "leaveserver", "Leaves this server, add my mention to confirm", Administration.LeaveServer)
            });

            CommandParser.Categories.Add(typeof(Music).Name, new Command[] {
                new Command(Command.PrefixType.Command, "join", "Joins your current voice channel", Music.Join),
                new Command(Command.PrefixType.Command, "leave", "Leaves any voice channel", Music.Leave),
                new Command(Command.PrefixType.Command, new string[] { "add", "q" }, "Adds a song title to the music queue", Music.Add),
                new Command(Command.PrefixType.Command, new string[] { "local", "addlocal", "l" }, "Adds a local song title to the music queue", Music.Local),
                new Command(Command.PrefixType.Command, new string[] { "push", "p" }, "Pushes a song to the top of the music queue", Music.Push),
                new Command(Command.PrefixType.Command, new string[] { "repeat" }, "Repeats the currently playing song", Music.Repeat),
                new Command(Command.PrefixType.Command, new string[] { "remove", "r" }, "Removes a song from the music queue", Music.Remove),
                new Command(Command.PrefixType.Command, new string[] { "volume", "vol" }, "Changes the volume of the music player", Music.Volume),
                new Command(Command.PrefixType.Command, new string[] { "playing", "song", "np" }, "Shows the current song", Music.CurrentSong),
                new Command(Command.PrefixType.Command, new string[] { "playlist", "lq", "queue" }, "Lists the current playlist", Music.Playlist),
                new Command(Command.PrefixType.Command, new string[] { "skip", "next", "n" }, "Skips the current song", Music.Skip),
                new Command(Command.PrefixType.Command, new string[] { "shuffle", "s" }, "Shuffles the current queue", Music.Shuffle),
                new Command(Command.PrefixType.Command, "clear", "Clears the current queue", Music.Clear),
                new Command(Command.PrefixType.Command, "save", "Saves the current playlist", Music.Save),
                new Command(Command.PrefixType.Command, "load", "Loads the current playlist from", Music.Load),
                new Command(Command.PrefixType.Command, "tgpair", "Pairs a Telegram channel to a Discord channel", Music.Pair),
                new Command(Command.PrefixType.Command, "tgunpair", "Unpairs all Telegram channels", Music.Unpair),
                new Command(Command.PrefixType.Command, "tgtoggle", "Allows or disallows someone from using Telegram commands", Music.TgToggle),
                new Command(Command.PrefixType.Command, "adhd", "Toggles ADHD", Music.Adhd)
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
                new Command(Command.PrefixType.Command, "anime", "Search for an anime - shorthand {name}", Search.AnimeInfo),
                new Command(Command.PrefixType.Command, "manga", "Search for a manga - shorthand <name>", Search.MangaInfo)
            });

            CommandParser.Categories.Add(typeof(Lewd).Name, new Command[] {
                new Command(Command.PrefixType.Command, new string[] { "lewd", "booru", "nsfw" }, "Search for a lewd image", Lewd.RandomLewd)
            });

            string Spam = "O - oooooooooo AAAAE - A - A - I - A - U - JO - oooooooooooo AAE - O - A - A - U - U - A - E - eee - ee - eee AAAAE - A - E - I - E - A - JO - ooo - oo - oo - oo EEEEO - A - AAA - AAAA";
            string FullSpam = "";

            while (FullSpam.Length + Spam.Length < 1992)
            {
                FullSpam += Spam + "\n";
            }

            CommandParser.Categories.Add(typeof(Conversation).Name, new Command[] {
                new Command(Command.PrefixType.Mention, new string[] { "hi", "hey", "hello" }, "Say hello to me", "Hi!"),
                new Command(Command.PrefixType.Mention, "choose from", "Choose from a list", Conversation.Choose),
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
                new Command(Command.PrefixType.Command, "meme", "Memeify a text", Conversation.Meme),

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
                new Command(Command.PrefixType.None, "/lenny", "", "( ͡° ͜ʖ ͡°)"),
                new Command(Command.PrefixType.None, "$$$", "", Trivia.Points)
            });
        }

        static ConsoleEventDelegate handler; 
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                "Caught close-button press, shutting down..".Log();
                Shutdown();
                return false;
            }

            return true;
        }
    }
}
