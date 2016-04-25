using Discord;
using DiscordBot.Commands;
using System;

namespace DiscordBot
{
    class ClientEvents
    {
        public static void LogMessage(object s, LogMessageEventArgs e)
        {
            if (e.Severity != LogSeverity.Info)
            {
                try
                {
                    string Text = e.Severity.ToString() + " ";
                    if (e.Message != null)
                    {
                        Text += "Message " + e.Message;
                    }

                    if (e.Message != "Failed to send message")
                    {
                        if (e.Exception != null)
                        {
                            Text += "\nException " + e.Exception;
                        }

                        if (e.Source != null)
                        {
                            Text += "\nFrom " + e.Source;
                        }
                    }

                    Text.Log();
                }
                catch (Exception Ex)
                {
                    Ex.Log();
                }
            }
#if DEBUG
            else
            {
                e.Message.Log();
            }
#endif
        }

        public static async void MessageReceived(object s, MessageEventArgs e)
        {
            try
            {
                if (!e.Message.IsAuthor)
                {
                    if (e.Message.Channel.IsPrivate)
                    {
                        if (e.Message.RawText.StartsWith("#"))
                        {
                            await e.Message.User.SendMessage(Bot.InviteLink);
                        }
                        
                        string sUserId = e.Message.Text.Split(' ')[0];
                        ulong UserId;

                        if (Bot.Owner == e.User.Id)
                        {
                            if (ulong.TryParse(sUserId, out UserId))
                            {
                                Channel PM = await Bot.Client.CreatePrivateChannel(UserId);
                                await PM.SendMessage(e.Message.Text.Substring(sUserId.Length).Trim());
                                await PM.Delete();
                            }
                        }
                        else
                        {
                            Channel PM = await Bot.Client.CreatePrivateChannel(Bot.Owner);
                            await PM.SendMessage($"[{e.User.Id}] {e.User.Name} sent: `{e.Message.Text.Replace("`", "")}`");
                            await PM.Delete();
                        }

                        await e.Message.Channel.Delete();
                    }
                    else
                    {
                        //OptionalAddServer(e.Server);
                        CommandParser.Handle(e);
                    }
                }
            }
            catch (Exception Ex)
            {
                Ex.Log();
            }
        }

        public static void UserJoined(object s, UserEventArgs e)
        {
            Db.ForceAddAccount(e.User.Id);

            if (e.User.Id != Bot.Client.CurrentUser.Id)
            {
                OptionalAddServer(e.Server);

                foreach (Channel Channel in ServerData.Servers[e.Server.Id].ChannelsWithCategory(typeof(Conversation).Name))
                {
                    Bot.Send(Channel, "Hi " + e.User.Mention + "! Welcome to the server :)", null, false);
                }
            }
        }

        public static void UserLeft(object s, UserEventArgs e)
        {
            if (e.User.Id != Bot.Client.CurrentUser.Id)
            {
                OptionalAddServer(e.Server);

                foreach (Channel Channel in ServerData.Servers[e.Server.Id].ChannelsWithCategory(typeof(Conversation).Name))
                {
                    Bot.Send(Channel, "I hope I'll see you again soon, " + e.User.Name + "!", null, false);
                }
            }
        }

        public static void JoinedServer(object s, ServerEventArgs e)
        {
            OptionalAddServer(e.Server);
        }

        public static void ServerAvailable(object s, ServerEventArgs e)
        {
            OptionalAddServer(e.Server);
        }

        public static void LeftServer(object s, ServerEventArgs e)
        {
            if (ServerData.Servers.ContainsKey(e.Server.Id))
            {
                ServerData.Servers[e.Server.Id].StopHandlers();
                ServerData.Servers.Remove(e.Server.Id);
                $"Left Server {e.Server.Name}".Log();
            }
        }

        private static void OptionalAddServer(Server Server)
        {
            if (!ServerData.Servers.ContainsKey(Server.Id))
            {
                ServerData.Servers.Add(Server.Id, new ServerData(Server));
                $"Joined server {Server.Name}".Log();
            }
        }
    }
}
