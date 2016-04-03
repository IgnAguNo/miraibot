using DiscordBot.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoLibrary;

namespace DiscordBot
{
    class SongData
    {
        public static string MusicDir = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") + "\\Music\\";
        //private static string[] MusicExtentions = new string[] { "mp3", "mp4", "webm", "flac" };

        public bool Found;
        public string Query;
        public bool Local;
        public string Name
        {
            get
            {
                return FullName.Compact(20);
            }
        }
        public string FullName;
        public string Url;

        public SongData(object ToSearch, bool LocalOnly = false)
        {
            Found = false;
            Query = ((string)ToSearch).Trim();
            Local = LocalOnly;
            FullName = Query;
            Url = Query;

            if (Query == string.Empty)
            {
                return;
            }

            try
            {
                if (Local)
                {
                    FullName = Query.Substring(MusicDir.Length);
                    Found = true;
                }
                else if (Regex.IsMatch(Query, "(.*)(soundcloud.com|snd.sc)(.*)"))
                {
                    Task<string> SC = ("http://api.soundcloud.com/resolve?url=" + Query + "&client_id=" + Bot.SoundCloudAPI).ResponseAsync();
                    SC.Wait();
                    if (SC.Result != string.Empty && SC.Result.StartsWith("{\"kind\":\"track\""))
                    {
                        JObject Response = JObject.Parse(SC.Result);
                        FullName = Response["title"].ToString();
                        Url = Response["stream_url"] + "?client_id=" + Bot.SoundCloudAPI;
                        Found = true;
                    }
                }
                else if (Query.IsValidUrl() && !Regex.IsMatch(Query, @"http(s)?://(www\.)?(youtu\.be|youtube\.com)[\w-/=&?]+"))
                {
                    Found = true;
                }
                else
                {
                    Task<string> YtLink = Search.YoutubeResult(Query);
                    YtLink.Wait();

                    if (YtLink.Result != string.Empty)
                    {
                        Task<IEnumerable<YouTubeVideo>> YtVids = YouTube.Default.GetAllVideosAsync(YtLink.Result);
                        YtVids.Wait();
                        IOrderedEnumerable<YouTubeVideo> Videos = YtVids.Result.Where(v => v.AdaptiveKind == AdaptiveKind.Audio).OrderByDescending(v => v.AudioBitrate);

                        if (Videos.Count() > 0)
                        {
                            YouTubeVideo Video = Videos.First();
                            FullName = Video.Title.Substring(0, Video.Title.Length - 10);
                            Url = Video.Uri;
                            Found = true;
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                $"SongData Resolve Exception {Ex}".Log();
            }
        }
    }
}
