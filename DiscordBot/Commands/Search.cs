using Discord;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace DiscordBot.Commands
{
    class Search
    {
        public static void Ask(object s, MessageEventArgs e)
        {
            try
            {
                JObject Ask = null;
                string Type = "Neutral";
                while (Type == "Neutral")
                {
                    var reqString = "https://8ball.delegator.com/magic/JSON/" + Uri.EscapeUriString((string)s);
                    Ask = JObject.Parse(reqString.WebResponse());
                    Type = Ask["magic"]["type"].ToString();
                }

                e.Respond(e.User.Mention + " " + Ask["magic"]["answer"].ToString());
            }
            catch (Exception Ex)
            {
                $"Ask: {Ex}".Log();
            }
        }

        public static void Youtube(object s, MessageEventArgs e)
        {
            try
            {
                string Url = YoutubeResult((string)s);
                if (Url != string.Empty)
                {
                    e.Respond("I think I found it.. " + Url);
                    return;
                }
            }
            catch (Exception Ex)
            {
                $"YtSearch {Ex}".Log();
            }

            e.Respond(e.User.Mention + " " + Conversation.CantFind);
        }

        public static void Image(object s, MessageEventArgs e)
        {
            try
            {
                string Req = "https://www.googleapis.com/customsearch/v1?q=" + Uri.EscapeDataString((string)s) + "&cx=018084019232060951019%3Ahs5piey28-e&num=1&searchType=image&start=" + new Random().Next(1, 15) + "&fields=items%2Flink&key=" + Bot.GoogleAPI;
                JObject obj = JObject.Parse(Req.WebResponse());
                e.Respond(obj["items"][0]["link"].ToString());
            }
            catch
            {
                e.Respond(e.User.Mention + " " + Conversation.CantFind);
            }
        }

        public static void Osu(object s, MessageEventArgs e)
        {
            using (var cl = new WebClient())
            {
                cl.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                cl.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.2; Win64; x64)");
                cl.DownloadDataAsync(new Uri("http://lemmmy.pw/osusig/sig.php?uname=" + (string)s + "&flagshadow&xpbar&xpbarhex&pp=2"));
                cl.DownloadDataCompleted += (sender, cle) => {
                    Bot.Send(e.Channel, (string)s + ".png", new MemoryStream(cle.Result));
                    e.Respond("Profile Link: https://osu.ppy.sh/u/" + Uri.EscapeDataString((string)s));
                };
            }
        }

        public static void Avatar(object s, MessageEventArgs e)
        {
            if (e.Message.MentionedUsers.Count() > 0)
            {
                e.Respond(e.Message.MentionedUsers.First().AvatarUrl);
            }
        }

        public static void Define(object s, MessageEventArgs e)
        {
            string Query = (string)s;
            if (Query.StartsWith("a "))
            {
                Query = Query.Substring(2);
            }
            else if (Query.StartsWith("an "))
            {
                Query = Query.Substring(3);
            }

            DefineSimple(Query, e);
        }

        public static void DefineSimple(object s, MessageEventArgs e)
        {
            try
            {
                string Query = (string)s;

                if (Query != string.Empty)
                {
                    WebHeaderCollection Headers = new WebHeaderCollection();
                    Headers.Add("X-Mashape-Key", Bot.MashapeAPI);
                    JObject Json = JObject.Parse($"https://mashape-community-urban-dictionary.p.mashape.com/define?term={Uri.EscapeUriString(Query)}".WebResponse(Headers));
                    e.Respond(Json["list"][0]["definition"].ToString());
                }
            }
            catch
            {
                e.Respond("I have no idea");
            }
        }

        private static string AniToken = null;

        public static void AnimeInfo(object sObj, MessageEventArgs e)
        {
            string s = ((string)sObj).Replace('/', ' ');
            RestClient API = GetAniApi();

            RestRequest SearchRequest = new RestRequest("/anime/search/" + Uri.EscapeUriString(s));
            SearchRequest.AddParameter("access_token", AniToken);
            string SearchResString = API.Execute(SearchRequest).Content;
            
            if (SearchResString.Trim() != string.Empty && JToken.Parse(SearchResString) is JArray)
            {
                RestRequest InfoRequest = new RestRequest("/anime/" + JArray.Parse(SearchResString)[0]["id"]);
                InfoRequest.AddParameter("access_token", AniToken);

                JObject Info = JObject.Parse(API.Execute(InfoRequest).Content);

                string Title = "`" + Info["title_romaji"] + "`";
                if (Title != Info["title_english"].ToString())
                {
                    Title += " / `" + Info["title_english"] + "`";
                }

                string Extra = "";
                if (Info["total_episodes"].ToString() != "0" && Info["average_score"].ToString() != "0")
                {
                    Extra = Info["total_episodes"] + " Episodes (" + Info["airing_status"] + ") - Scored " + Info["average_score"] + "\n";
                }

                e.Respond(Title + "\n" + Extra +
                    "Synopsis: " + WebUtility.HtmlDecode(Info["description"].ToString()).Replace("<br>", "\n").Compact(250) + "\n" +
                    "More info at http://anilist.co/anime/" + Info["id"] + "\n" + Info["image_url_lge"]);
            }
            else
            {
                e.Respond(e.User.Mention + " " + Conversation.CantFind);
            }
        }

        public static void MangaInfo(object sObj, MessageEventArgs e)
        {
            string s = ((string)sObj).Replace('/', ' ');
            RestClient API = GetAniApi();

            RestRequest SearchRequest = new RestRequest("/manga/search/" + Uri.EscapeUriString(s));
            SearchRequest.AddParameter("access_token", AniToken);
            string SearchResString = API.Execute(SearchRequest).Content;

            if (SearchResString.Trim() != string.Empty && JToken.Parse(SearchResString) is JArray)
            {
                RestRequest InfoRequest = new RestRequest("/manga/" + JArray.Parse(SearchResString)[0]["id"]);
                InfoRequest.AddParameter("access_token", AniToken);

                JObject Info = JObject.Parse(API.Execute(InfoRequest).Content);

                string Title = "`" + Info["title_romaji"] + "`";
                if (Title != Info["title_english"].ToString())
                {
                    Title += " / `" + Info["title_english"] + "`";
                }

                string Extra = "";
                if (Info["total_chapters"].ToString() != "0" && Info["average_score"].ToString() != "0")
                {
                    Extra = Info["total_chapters"] + " Chapters (" + Info["publishing_status"] + ") - Scored " + Info["average_score"] + "\n";
                }

                e.Respond(Title + "\n" + Extra +
                    "Synopsis: " + WebUtility.HtmlDecode(Info["description"].ToString()).Replace("<br>", "\n").Compact(250) + "\n" +
                    "More info at http://anilist.co/manga/" + Info["id"] + "\n" + Info["image_url_lge"]);
            }
            else
            {
                e.Respond(e.User.Mention + " " + Conversation.CantFind);
            }
        }

        private static RestClient GetAniApi()
        {
            RestClient API = new RestClient("http://anilist.co/api");

            RestRequest TokenRequest = new RestRequest("/auth/access_token", RestSharp.Method.POST);
            TokenRequest.AddParameter("grant_type", "client_credentials");
            TokenRequest.AddParameter("client_id", Bot.AniIdAPI);
            TokenRequest.AddParameter("client_secret", Bot.AniSecretAPI);
            AniToken = JObject.Parse(API.Execute(TokenRequest).Content)["access_token"].ToString();

            return API;
        }

        private static YouTubeService YT = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = Bot.GoogleAPI
        });

        //Made by owner of Nadekobot
        public static string YoutubeResult(string Query)
        {
            try
            {
                var listRequest = YT.Search.List("snippet");
                listRequest.Q = Query;
                listRequest.MaxResults = 1;
                listRequest.Type = "video";
                foreach (SearchResult result in listRequest.Execute().Items)
                {
                    return "http://www.youtube.com/watch?v=" + result.Id.VideoId;
                }

                /*
                //maybe it is already a youtube url, in which case we will just extract the id and prepend it with youtube.com?v=
                Match Match = new Regex("(?:youtu\\.be\\/|v=)(?<id>[\\da-zA-Z\\-_]*)").Match(Query);
                if (Match.Length > 1)
                {
                    return "http://www.youtube.com?v=" + Match.Groups["id"].Value;
                }

                WebRequest wr = WebRequest.Create("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&q=" + Uri.EscapeDataString(Query) + "&key=" + Bot.GoogleAPI);
                StreamReader sr = new StreamReader((await wr.GetResponseAsync()).GetResponseStream());

                JObject obj = JObject.Parse(await sr.ReadToEndAsync());
                return "http://www.youtube.com/watch?v=" + obj["items"][0]["id"]["videoId"].ToString();
                */
            }
            catch { }

            return string.Empty;
        }
    }
}
