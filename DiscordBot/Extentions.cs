<<<<<<< HEAD
﻿using System;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
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
                if (Input.Length <= (MaxPart * 2 + 3))
                {
                    return Input;
                }

                Input = Input.Replace("\n", String.Empty).Replace("\r", String.Empty);
                return Input.Substring(0, MaxPart) + "..." + Input.Substring(Input.Length - MaxPart);
            }
            catch
            {
            }

            return String.Empty;
        }

        public static async Task<string> ResponseAsync(this string Url, WebHeaderCollection Headers = null)
        {
            try
            {
                WebRequest Request = WebRequest.Create(Url);
                if (Headers != null)
                {
                    Request.Headers = Headers;
                }

                return await new StreamReader((await Request.GetResponseAsync()).GetResponseStream()).ReadToEndAsync();
            }
            catch (Exception Ex)
            {
                $"HTTP Load Error: {Ex}".Log();
            }

            return String.Empty;
        }

        public static async Task<string> ShortUrl(this string url)
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
            catch (Exception ex) { $"HttpException: {ex}".Log(); return url; }
        }

        public static long MB(this long Input)
        {
            return Input * 1024 * 1024;
        }

        public static long GB(this long Input)
        {
            return Input * 1024 * 1024 * 1024;
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
    }
}
=======
﻿using System;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
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
                if (Input.Length <= (MaxPart * 2 + 3))
                {
                    return Input;
                }

                Input = Input.Replace("\n", String.Empty).Replace("\r", String.Empty);
                return Input.Substring(0, MaxPart) + "..." + Input.Substring(Input.Length - MaxPart);
            }
            catch
            {
            }

            return String.Empty;
        }

        public static async Task<string> ResponseAsync(this string Url, WebHeaderCollection Headers = null)
        {
            try
            {
                WebRequest Request = WebRequest.Create(Url);
                if (Headers != null)
                {
                    Request.Headers = Headers;
                }

                return await new StreamReader((await Request.GetResponseAsync()).GetResponseStream()).ReadToEndAsync();
            }
            catch (Exception Ex)
            {
                $"HTTP Load Error: {Ex}".Log();
            }

            return String.Empty;
        }

        public static async Task<string> ShortUrl(this string url)
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
            catch (Exception ex) { $"HttpException: {ex}".Log(); return url; }
        }

        public static long MB(this long Input)
        {
            return Input * 1024 * 1024;
        }

        public static long GB(this long Input)
        {
            return Input * 1024 * 1024 * 1024;
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
    }
}
>>>>>>> fc15c335f6e5e123a736f003fbf575ac756c084c
