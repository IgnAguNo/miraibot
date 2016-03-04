<<<<<<< HEAD
﻿using Discord;
using DiscordBot.Handlers;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DiscordBot
{
    class ServerData
    {
        public static Dictionary<ulong, ServerData> Servers = new Dictionary<ulong, ServerData>();

        private Server Server;
        public MusicHandler Music = new MusicHandler();
        public ConcurrentDictionary<ulong, TriviaHandler> Trivia = new ConcurrentDictionary<ulong, TriviaHandler>();
        private ConcurrentDictionary<ulong, List<Command>> ChannelCommands = new ConcurrentDictionary<ulong, List<Command>>();

        public ServerData(Server UseServer)
        {
            Server = UseServer;
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
=======
﻿using Discord;
using DiscordBot.Handlers;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DiscordBot
{
    class ServerData
    {
        public static Dictionary<ulong, ServerData> Servers = new Dictionary<ulong, ServerData>();

        private Server Server;
        public MusicHandler Music = new MusicHandler();
        public ConcurrentDictionary<ulong, TriviaHandler> Trivia = new ConcurrentDictionary<ulong, TriviaHandler>();
        private ConcurrentDictionary<ulong, List<Command>> ChannelCommands = new ConcurrentDictionary<ulong, List<Command>>();

        public ServerData(Server UseServer)
        {
            Server = UseServer;
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
>>>>>>> fc15c335f6e5e123a736f003fbf575ac756c084c
