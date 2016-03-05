﻿using Discord;
using DiscordBot.Handlers;
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
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                e.Message.Log();
            }
        }

        public static void MessageReceived(object s, MessageEventArgs e)
        {
            if (!e.Message.IsAuthor)
            {
                if (e.Message.Channel.IsPrivate)
                {
                    if (Bot.OwnerAccount != null)
                    {
                        Bot.OwnerAccount.SendMessage(e.User.Name + " sent: `" + e.Message.Text.Replace("`", "") + "`");
                    }
                }
                else
                {
                    CommandParser.Handle(e);
                }
            }
        }

        public static void UserJoined(object s, UserEventArgs e)
        {
            Db.ForceAddAccount(e.User.Id);

            foreach (Channel Channel in ServerData.Servers[e.Server.Id].ChannelsWithCategory("Conversation"))
            {
                Bot.Send(Channel, "Hi " + e.User.Mention + "! Welcome to the server :)", null, false);
            }
        }

        public static void UserLeft(object s, UserEventArgs e)
        {
            foreach (Channel Channel in ServerData.Servers[e.Server.Id].ChannelsWithCategory("Conversation"))
            {
                Bot.Send(Channel, "I hope I'll see you again soon, " + e.User.Name + "!", null, false);
            }
        }
    }
}
