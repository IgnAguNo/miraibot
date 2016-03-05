﻿using Discord;
using DiscordBot.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot
{
    class CommandParser
    {
        public static ulong Executed = 0;
        public static Dictionary<string, Command[]> Categories = new Dictionary<string, Command[]>();

        public static void Handle(MessageEventArgs e)
        {
            try
            {
                ServerData S = ServerData.Servers[e.Server.Id];
                if (S.Trivia.ContainsKey(e.Channel.Id) && S.Trivia[e.Channel.Id].Try(e))
                {
                    return;
                }

                List<Command> Cmds = S.GetCommands(e.Channel.Id);

                string Raw = e.Message.RawText;

                if (e.Message.Text.StartsWith("{") && e.Message.Text.EndsWith("}") && Db.HasPermission(e.User.Id, "anime"))
                {
                    Search.AnimeInfo(Raw.TrimStart('{').TrimEnd('}'), e);
                }
                else if (e.Message.Text.StartsWith("<") && e.Message.Text.EndsWith(">") && Db.HasPermission(e.User.Id, "manga"))
                {
                    Search.MangaInfo(Raw.TrimStart('<').TrimEnd('>'), e);
                }

                string Key;
                string Lower;
                foreach (Command Cmd in Cmds)
                {
                    string Prefix = "#";
                    Lower = Raw.ToLower();

                    if (Cmd.Prefix != Command.PrefixType.None)
                    {
                        if (Db.HasPermission(e.User.Id, Cmd.Keys[0]))
                        {
                            if (Cmd.Prefix == Command.PrefixType.Mention)
                            {
                                Prefix = Bot.Mention + " ";
                            }

                            foreach (string CmdKey in Cmd.Keys)
                            {
                                Key = Prefix + CmdKey;

                                if (Lower.StartsWith(Key) && (Raw.Length == Key.Length || Raw.Substring(Key.Length, 1) == " " || Raw.Substring(Key.Length, 1) == "." || Raw.Substring(Key.Length, 1) == "?"))
                                {
                                    Cmd.Handler(Raw.Substring(Key.Length).TrimStart(' '), e);
                                    Executed++;
                                    return;
                                }
                            }
                        }
                    }
                    else if (Lower.Contains(Cmd.Keys[0]))
                    {
                        Executed++;
                        Cmd.Handler(Raw, e);
                        return;
                    }
                }

                if (Raw.EndsWith("?") && Raw.Length > 1 && Db.HasPermission(e.User.Id, "ask"))
                {
                    Search.Ask(Raw, e);
                }
            }
            catch (Exception Ex)
            {
                $"CommandExeption: {Ex}".Log();
            }
        }

        public static void ToggleCat(object s, MessageEventArgs e)
        {
            string Cat = (string)s;

            if (Cat.Trim() != string.Empty && Categories.ContainsKey(Cat))
            {
                if (Db.ChannelToggleCategory(e.Channel.Id, Cat))
                {
                    Bot.Send(e.Channel, Cat + " has now been enabled");
                }
                else
                {
                    Bot.Send(e.Channel, Cat + " has now been disabled");
                }

                ServerData.Servers[e.Server.Id].ReloadCommands(e.Channel.Id);
            }
        }

        public static async void Help(object s, MessageEventArgs e)
        {
            foreach (KeyValuePair<string, Command[]> Cat in Categories)
            {
                string CatInfo = string.Empty;

                foreach (Command Cmd in Cat.Value)
                {
                    string Start = "#";

                    if (Cmd.Prefix == Command.PrefixType.None)
                    {
                        continue;
                    }
                    else if (Cmd.Prefix == Command.PrefixType.Mention)
                    {
                        Start = Bot.Mention + " ";
                    }

                    if (Db.HasPermission(e.User.Id, Cmd.Keys[0]))
                    {
                        if (e.User.Id == Bot.Owner)
                        {
                            CatInfo += "(" + Db.PermissionRank(Cmd.Keys[0]) + ") ";
                        }

                        CatInfo += Start + string.Join("/", Cmd.Keys) + " ~ `" + Cmd.Description + "`\n";
                    }
                }

                if (CatInfo != string.Empty)
                {
                    if (Db.ChannelDisabledCategory(e.Channel.Id, Cat.Key))
                    {
                        if (e.User.Id == Bot.Owner)
                        {
                            CatInfo = "Disabled";
                        }
                        else
                        {
                            continue;
                        }
                    }

                    Bot.Send(e.Channel, (Cat.Key == String.Empty ? "**Main**" : "**" + Cat.Key + "**") + "\n" + CatInfo);
                    await Task.Delay(150);
                }
            }
        }
    }
}
