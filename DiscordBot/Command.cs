﻿using Discord;
using System;

namespace DiscordBot
{
    class Command
    {
        public enum PrefixType
        {
            Mention,
            Command,
            None
        }

        public PrefixType Prefix;
        public string[] Keys;
        public string Description;
        public EventHandler<MessageEventArgs> Handler;

        public Command(PrefixType Prefix, string[] Key, string Description, EventHandler<MessageEventArgs> Handler)
        {
            Init(Prefix, Key, Description, Handler);
        }

        public Command(PrefixType Prefix, string Key, string Description, EventHandler<MessageEventArgs> Handler)
        {
            Init(Prefix, new string[] { Key }, Description, Handler);
        }

        public Command(PrefixType Prefix, string[] Key, string Description, string Response)
        {
            Init(Prefix, Key, Description, (s, e) => {
                e.Respond(Response);
            });
        }

        public Command(PrefixType Prefix, string Key, string Description, string Response)
        {
            Init(Prefix, new string[] { Key }, Description, (s, e) => {
                e.Respond(Response);
            });
        }

        private void Init(PrefixType Prefix, string[] Key, string Description, EventHandler<MessageEventArgs> Handler)
        {
            this.Prefix = Prefix;
            this.Keys = Key;
            this.Description = Description;
            this.Handler = Handler;
        }
    }
}