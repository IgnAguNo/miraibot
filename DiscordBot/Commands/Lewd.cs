using Discord;
using System;
using System.Text.RegularExpressions;

namespace DiscordBot.Commands
{
    class Lewd
    {
        public static async void RandomLewd(object s, MessageEventArgs e)
        {
            string Query = (string)s + "+sex";
            var RNG = new Random();

            try
            {
                if (Query.Contains("loli"))
                {
                    Query = Query.Replace("loli", "flat chest");
                }

                string Result = await ("http://danbooru.donmai.us/posts?page=0&tags=" + Query.Replace(" ", "_")).ResponseAsync();
                MatchCollection Matches = Regex.Matches(Result, "data-large-file-url=\"(?<id>.*?)\"");
                if (Matches.Count > 0 && !Result.ToLower().Contains("kyoukai") && !Result.ToLower().Contains("kuriyama"))
                {
                    Bot.Send(e.Channel, "http://danbooru.donmai.us" + Matches[RNG.Next(0, Matches.Count)].Groups["id"].Value);
                    return;
                }

                Result = await ("http://gelbooru.com/index.php?page=post&s=list&pid=0&tags=" + Query.Replace(" ", "_")).ResponseAsync();
                Matches = Regex.Matches(Result, "span id=\"s(?<id>\\d*)\"");
                if (Matches.Count > 0 && !Result.ToLower().Contains("kyoukai") && !Result.ToLower().Contains("kuriyama"))
                {
                    Bot.Send(e.Channel, Regex.Match(await ("http://gelbooru.com/index.php?page=post&s=view&id=" + Matches[RNG.Next(0, Matches.Count)].Groups["id"].Value).ResponseAsync(), "\"(?<url>http://simg4.gelbooru.com//images.*?)\"").Groups["url"].Value);
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
