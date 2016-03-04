using Discord;
using System.IO;

namespace DiscordBot.Handlers
{
    abstract class IHandler
    {
        public abstract string Name
        {
            get;
        }

        public void Send(Channel Channel, string Message, Stream Stream = null)
            => Bot.Send(Channel, Name + " | " + Message, Stream);
    }
}
