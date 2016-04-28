using DiscordBot.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VideoLibrary;

namespace DiscordBot
{
    class SongData
    {
        public static string MusicDir = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") + "\\Music\\";

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
                    return;
                }

                if (Regex.IsMatch(Query, "(.*)(soundcloud.com|snd.sc)(.*)"))
                {
                    string SC = ("http://api.soundcloud.com/resolve?url=" + Query + "&client_id=" + Bot.SoundCloudAPI).WebResponse();
                    if (SC != string.Empty && SC.StartsWith("{\"kind\":\"track\""))
                    {
                        JObject Response = JObject.Parse(SC);
                        FullName = Response["title"].ToString();
                        Url = Response["stream_url"] + "?client_id=" + Bot.SoundCloudAPI;
                        Found = true;
                    }

                    return;
                }

                string YouTubeUrl = string.Empty;
                if (Query.IsValidUrl())
                {
                    if (Regex.IsMatch(Query, @"http(s)?://(www\.)?(youtu\.be|youtube\.com)[\w-/=&?]+"))
                    {
                        YouTubeUrl = Query;
                    }
                    else
                    {
                        Found = true;
                        return;
                    }
                }
                else
                {
                    YouTubeUrl = Search.YoutubeResult(Query);
                }

                if (YouTubeUrl != string.Empty)
                {
                    IEnumerable<YouTubeVideo> Videos = YouTube.Default.GetAllVideos(YouTubeUrl);
                    Videos = Videos.Where(v => v.AdaptiveKind == AdaptiveKind.Audio);
                    Videos = Videos.OrderByDescending(v => v.AudioBitrate);

                    if (Videos.Count() > 0)
                    {
                        YouTubeVideo Video = Videos.First();
                        FullName = Video.Title.Substring(0, Video.Title.Length - 10);
                        Url = Video.Uri;
                        Found = true;
                    }
                }
            }
            catch { }
        }
    }
}
