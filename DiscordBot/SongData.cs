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
        private static string[] MusicExtentions = new string[] { "mp3", "mp4", "webm", "flac" };

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

        public SongData(string ToSearch, bool LocalOnly)
        {
            Found = false;
            Query = ToSearch;
            Local = LocalOnly;
            FullName = Query;
            Url = Query;

            try
            {
                if (MusicExtentions.Contains(Query.Split('.').Last().Trim()))
                {
                    if (Local)
                    {
                        FullName = Query.Substring(MusicDir.Length);
                        Found = true;
                    }
                    else if (Query.IsValidUrl())
                    {
                        Found = true;
                    }
                }
                else if (Regex.IsMatch(Query, "(.*)(soundcloud.com|snd.sc)(.*)"))
                {
                    Task<string> SC = ("http://api.soundcloud.com/resolve?url=" + Query + "&client_id=" + Bot.SoundCloudAPI).ResponseAsync();
                    SC.Wait();
                    if (SC.Result != string.Empty && !SC.Result.StartsWith("{\"kind\":\"track\""))
                    {
                        JObject Response = JObject.Parse(SC.Result);
                        FullName = Response["title"].ToString();
                        Url = Response["stream_url"] + "?client_id=" + Bot.SoundCloudAPI;
                        Found = true;
                    }
                }
                else
                {
                    Task<string> YtLink = Search.YoutubeResult(Query);
                    YtLink.Wait();

                    if (YtLink.Result != string.Empty)
                    {
                        Task<IEnumerable<YouTubeVideo>> YtVids = YouTube.Default.GetAllVideosAsync(YtLink.Result);
                        YtVids.Wait();
                        YouTubeVideo Video = YtVids.Result.Where(v => v.AdaptiveKind == AdaptiveKind.Audio).OrderByDescending(v => v.AudioBitrate).FirstOrDefault();

                        if (Video != null)
                        {
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
