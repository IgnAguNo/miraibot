using Discord;
using System;
using System.Text.RegularExpressions;

namespace DiscordBot.Commands
{
    class Lewd
    {
        public static void RandomLewd(object s, MessageEventArgs e)
        {
            Bot.Send(e.Channel, GetRandomLewd(s, true));
        }

        public static string GetRandomLewd(object s, bool FilterKnK)
        {
            try
            {
                var RNG = new Random();

                string Query = (string)s;
                if (Query.StartsWith("."))
                {
                    Query = Query.Substring(1);
                }
                else
                {
                    Query += "+sex";
                }

                if (Query.Contains("loli"))
                {
                    Query = Query.Replace("loli", "flat chest");
                }

                Query = Query.Replace(" ", "_");

                string Result = ("http://danbooru.donmai.us/posts?page=0&tags=" + Query).WebResponse();
                MatchCollection Matches = Regex.Matches(Result, "data-large-file-url=\"(?<id>.*?)\"");
                if (Matches.Count > 0 && (!FilterKnK || (!Result.ToLower().Contains("kyoukai") && !Result.ToLower().Contains("kuriyama"))))
                {
                    return "http://danbooru.donmai.us" + Matches[RNG.Next(0, Matches.Count)].Groups["id"].Value;
                }

                Result = ("http://gelbooru.com/index.php?page=post&s=list&pid=0&tags=" + Query).WebResponse();
                Matches = Regex.Matches(Result, "span id=\"s(?<id>\\d*)\"");
                if (Matches.Count > 0 && (!FilterKnK || (!Result.ToLower().Contains("kyoukai") && !Result.ToLower().Contains("kuriyama"))))
                {
                    return Regex.Match(("http://gelbooru.com/index.php?page=post&s=view&id=" + Matches[RNG.Next(0, Matches.Count)].Groups["id"].Value).WebResponse(), "\"(?<url>http://simg4.gelbooru.com//images.*?)\"").Groups["url"].Value;
                }
            }
            catch (Exception Ex)
            {
                $"Lewd Search {Ex}".Log();
            }

            return "I couldn't find anything";
        }
    }
}
