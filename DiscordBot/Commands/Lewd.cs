using Discord;
using System;
using System.Text.RegularExpressions;

namespace DiscordBot.Commands
{
    class Lewd
    {
        public static async void RandomLewd(object s, MessageEventArgs e)
        {
            string Query = (string)s;
            var RNG = new Random();

            try
            {
                if (Query == "loli")
                {
                    Query = "flat_chest";
                }

                MatchCollection Matches = Regex.Matches(await ("http://danbooru.donmai.us/posts?page=" + RNG.Next(0, 15) + "&tags=" + Query.Replace(" ", "_")).ResponseAsync(), "data-large-file-url=\"(?<id>.*?)\"");
                if (Matches.Count > 0)
                {
                    Bot.Send(e.Channel, await ("http://danbooru.donmai.us" + Matches[RNG.Next(0, Matches.Count)].Groups["id"].Value).ShortUrl());
                    return;
                }

                Matches = Regex.Matches(await ("http://gelbooru.com/index.php?page=post&s=list&pid=" + RNG.Next(0, 10) * 42 + "&tags=" + Query.Replace(" ", "_")).ResponseAsync(), "span id=\"s(?<id>\\d*)\"");
                if (Matches.Count > 0)
                {
                    Bot.Send(e.Channel, await (Regex.Match(await ("http://gelbooru.com/index.php?page=post&s=view&id=" + Matches[RNG.Next(0, Matches.Count)].Groups["id"].Value).ResponseAsync(), "\"(?<url>http://simg4.gelbooru.com//images.*?)\"").Groups["url"].Value).ShortUrl());
                    return;
                }
            }
            catch (Exception Ex)
            {
                Bot.Client.Log.Log(LogSeverity.Error, "Lewd Search", null, Ex);
            }

            Bot.Send(e.Channel, "I couldn't find anything");
        }
    }
}
