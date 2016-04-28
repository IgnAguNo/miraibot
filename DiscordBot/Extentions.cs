using Discord;
using DiscordBot.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace DiscordBot
{
    static class Extentions
    {
        public static string Compact(this object ObjInput, int MaxPart = 35)
        {
            try
            {
                string Input = ObjInput.ToString();
                if (Input.Length <= (MaxPart * 2 + 2))
                {
                    return Input;
                }

                Input = Input.Replace("\n", string.Empty).Replace("\r", string.Empty);
                return Input.Substring(0, MaxPart) + ".." + Input.Substring(Input.Length - MaxPart);
            }
            catch
            {
            }

            return string.Empty;
        }

        public static string WebResponse(this string Url, WebHeaderCollection Headers = null)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    WebRequest Request = WebRequest.Create(Url);
                    if (Headers != null)
                    {
                        Request.Headers = Headers;
                    }

                    return new StreamReader(Request.GetResponse().GetResponseStream()).ReadToEnd();
                }
                catch //(Exception Ex)
                {
                    //$"HTTP Load Error: {Ex}".Log();
                }
            }

            return string.Empty;
        }

        public static List<int> ParseInts(this object Text, char Separator = ',', int Offset = 0)
        {
            string[] SplitString = ((string)Text).Split(',');
            List<int> Ints = new List<int>();

            int Num;
            foreach (string Part in SplitString)
            {
                if (int.TryParse(Part.Trim(), out Num))
                {
                    Ints.Add(Num + Offset);
                }
            }

            return Ints;
        }

        public static string Join(this IEnumerable<string> values, string separator)
            => string.Join(separator, values);

        /*public static async Task<string> ShortUrl(this string url)
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/urlshortener/v1/url?key=" + Bot.GoogleAPI);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync()))
                {
                    string json = "{\"longUrl\":\"" + url + "\"}";
                    streamWriter.Write(json);
                }

                var httpResponse = (await httpWebRequest.GetResponseAsync()) as HttpWebResponse;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    string responseText = await streamReader.ReadToEndAsync();
                    string MATCH_PATTERN = @"""id"": ?""(?<id>.+)""";
                    return Regex.Match(responseText, MATCH_PATTERN).Groups["id"].Value;
                }
            }
            catch
            {
                return url;
            }
        }*/

        public static long MB(this int Input)
        {
            return (long)Input * 1024 * 1024;
        }

        public static byte[] AdjustVolume(this byte[] AudioSamples, float Volume)
        {
            if (Volume == 1.0f)
            {
                return AudioSamples;
            }

            byte[] Array = new byte[AudioSamples.Length];
            for (int i = 0; i < Array.Length; i += 2)
            {
                short buf1 = AudioSamples[i + 1];
                short buf2 = AudioSamples[i];

                buf1 = (short)((buf1 & 0xff) << 8);
                buf2 = (short)(buf2 & 0xff);

                short res = (short)(buf1 | buf2);
                res = (short)(res * Volume);

                Array[i] = (byte)res;
                Array[i + 1] = (byte)(res >> 8);
            }

            return Array;
        }

        public static void Log(this object Info)
        {
            Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] " + Info.ToString());
        }

        public static string ReplaceFirst(this string Text, string Search, string Replace)
        {
            int Position = Text.IndexOf(Search);
            if (Position < 0)
            {
                return Text;
            }
            return Text.Substring(0, Position) + Replace + Text.Substring(Position + Search.Length);
        }

        public static void ExecuteDispose(this SQLiteCommand Cmd)
        {
            Cmd.ExecuteNonQuery();
            Cmd.Dispose();
        }

        public static bool IsValidUrl(this string Text)
        {
            Uri WebRes;
            return Uri.TryCreate(Text, UriKind.Absolute, out WebRes);
        }

        public static T Dequeue<T>(this ConcurrentQueue<T> Queue)
        {
            T Result = default(T);
            while (Queue.Count > 0 && !Queue.TryDequeue(out Result));
            return Result;
        }

        public static void Respond(this MessageEventArgs e, string Text)
        {
            Bot.Send(e.Channel, Text);
        }

        public static void UntilNoException(this Action Action, byte Max = byte.MaxValue)
            => UntilNoException<Exception>(Action, Max);

        public static void UntilNoException<ExceptionType>(this Action Action, byte Max = byte.MaxValue) where ExceptionType : Exception
        {
            for (byte i = 0; i < Max; i++)
            {
                try
                {
                    Action();
                    break;
                }
                catch (ExceptionType) { }
            }
        }

        public static async Task UntilNoExceptionAsync(this Func<Task> Action, byte Max = byte.MaxValue)
            => await UntilNoExceptionAsync<Exception>(Action, Max);

        public static async Task UntilNoExceptionAsync<ExceptionType>(this Func<Task> Action, byte Max = byte.MaxValue) where ExceptionType : Exception
        {
            for (byte i = 0; i < Max; i++)
            {
                try
                {
                    await Action();
                    break;
                }
                catch (ExceptionType) { }
            }
        }

        public static MusicHandler Music(this MessageEventArgs e)
        {
            return ServerData.Servers[e.Server.Id].Music;
        }
    }
}