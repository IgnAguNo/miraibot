using Discord;
using DiscordBot.Handlers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot
{
    class ServerData
    {
        public static Dictionary<ulong, ServerData> Servers = new Dictionary<ulong, ServerData>();

        public Server Server;
        public MusicHandler Music;
        public ConcurrentDictionary<ulong, TriviaHandler> Trivia = new ConcurrentDictionary<ulong, TriviaHandler>();
        private ConcurrentDictionary<ulong, List<Command>> ChannelCommands = new ConcurrentDictionary<ulong, List<Command>>();

        public string Name
        {
            get
            {
                return Server.Name;
            }
        }

        public ServerData(Server UseServer)
        {
            Server = UseServer;

            Music = new MusicHandler(UseServer);
            Music.Run();
        }

        public List<Channel> ChannelsWithCategory(string Category)
        {
            List<Channel> UpdateChannels = new List<Channel>();

            if (CommandParser.Categories.ContainsKey(Category))
            {
                string CmdSearch = CommandParser.Categories[Category].First().Keys[0];

                foreach (Channel Channel in Server.TextChannels)
                {
                    if (GetCommands(Channel.Id).Any(x => x.Keys.Contains(CmdSearch)))
                    {
                        //$"Channel {Channel.Name} {Category}".Log();
                        UpdateChannels.Add(Channel);
                    }
                }
            }

            return UpdateChannels;
        }

        public void ReloadCommands(ulong ChannelId)
        {
            List<Command> New = ParseCommands(ChannelId);

            List<Command> OldList;
            if (ChannelCommands.TryGetValue(ChannelId, out OldList))
            {
                ChannelCommands.TryUpdate(ChannelId, New, OldList);
            }
            else
            {
                ChannelCommands.TryAdd(ChannelId, New);
            }
        }

        public List<Command> GetCommands(ulong ChannelId)
        {
            List<Command> Commands;
            if (!ChannelCommands.TryGetValue(ChannelId, out Commands))
            {
                Commands = ParseCommands(ChannelId);
                ChannelCommands.TryAdd(ChannelId, Commands);
            }

            return Commands;
        }

        private List<Command> ParseCommands(ulong ChannelId)
        {
            List<Command> ChannelCommands = new List<Command>();

            foreach (KeyValuePair<string, Command[]> CommandCategory in CommandParser.Categories)
            {
                if (!Db.ChannelDisabledCategory(ChannelId, CommandCategory.Key))
                {
                    ChannelCommands.AddRange(CommandCategory.Value);
                }
            }
            
            return ChannelCommands;
        }

        ~ServerData()
        {
            Music = null;
            foreach (KeyValuePair<ulong, TriviaHandler> KVP in Trivia)
            {
                KVP.Value.Stop();
            }
        }
    }
}