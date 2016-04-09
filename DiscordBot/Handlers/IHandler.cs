using Discord;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.Handlers
{
    abstract class IHandler
    {
        public abstract string Name
        {
            get;
        }

        public void Send(Channel Channel, string Message, Stream Stream = null)
            => Bot.Send(Channel, "**" + Name + "** | " + Message, Stream);

        public Task<Message> SendAsync(Channel Channel, string Message, Stream Stream = null)
            => Bot.SendAsync(Channel, "**" + Name + "** | " + Message, Stream);

        public async Task EditAsync(Message M, string Message)
        {
            if (M != null)
            {
                await M.Edit("**" + Name + "** | " + Message);
            }
        }
    }
}
