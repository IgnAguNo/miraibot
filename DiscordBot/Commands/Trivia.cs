using Discord;
using DiscordBot.Handlers;
using System;

namespace DiscordBot.Commands
{
    class Trivia
    {
        public static void Start(object s, MessageEventArgs e)
        {
            ServerData S = ServerData.Servers[e.Server.Id];

            if (!S.Trivia.ContainsKey(e.Channel.Id))
            {
                if (!S.Trivia.TryAdd(e.Channel.Id, new TriviaHandler(e.Channel)))
                {
                    return;
                }
            }

           S.Trivia[e.Channel.Id].Start();
        }

        public static void Leaderboards(object s, MessageEventArgs e)
        {
            ServerData S = ServerData.Servers[e.Server.Id];

            if (S.Trivia.ContainsKey(e.Channel.Id))
            {
                S.Trivia[e.Channel.Id].Leaderboards();
            }
        }

        public static void Stop(object s, MessageEventArgs e)
        {
            ServerData S = ServerData.Servers[e.Server.Id];

            if (S.Trivia.ContainsKey(e.Channel.Id))
            {
                S.Trivia[e.Channel.Id].Stop();
                e.Respond("Trivia will stop after this question");
            }
        }

        public static void Points(object s, MessageEventArgs e)
        {
            int DonutCount = Db.GetPoints(e.User.Id);
            string Text = "You have " + DonutCount + " pair(s) of glasses\n";
            int Emojis = (int)Math.Ceiling(Math.Log(DonutCount, 1.5));
            for (int i = 0; i < Emojis; i++)
            {
                if (Text.Length > 1988)
                {
                    break;
                }

                Text += ":eyeglasses:";
            }

            e.Respond(Text);
        }
    }
}
