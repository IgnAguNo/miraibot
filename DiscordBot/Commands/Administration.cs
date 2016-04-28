using Discord;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    class Administration
    {
        public static void Permission(object s, MessageEventArgs e)
        {
            string[] Parts = ((string)s).Split(' ');

            if (Parts.Length == 2)
            {
                int Rank;
                if (int.TryParse(Parts[1], out Rank) && Rank < 100)
                {
                    Db.SetPerm(Parts[0], Rank);
                    e.Respond("Updated `" + Parts[0] + "` to a minimum rank of " + Rank);
                }
            }
        }

        public static void Rank(object s, MessageEventArgs e)
        {
            if (e.Message.MentionedUsers.Count() == 1)
            {
                string[] Parts = ((string)s).Split(' ');
                if (Parts.Length == 2)
                {
                    int Rank;
                    if (int.TryParse(Parts[1], out Rank) && Rank < 100)
                    {
                        Db.SetRank(e.Message.MentionedUsers.First().Id, Rank);
                        e.Respond(e.Message.MentionedUsers.First().Mention + " is now rank " + Rank);
                    }
                }
            }
        }

        public static void Ranks(object s, MessageEventArgs e)
        {
            Dictionary<int, List<string>> Ranks = new Dictionary<int, List<string>>();
            IEnumerable<User> Users = e.Server.Users;
            if ((string)s != "all")
            {
                Users = Users.Where(x => x.Status == UserStatus.Online && x.Id != Bot.Client.CurrentUser.Id);
            }

            foreach (User User in Users.OrderBy(x => x.Name))
            {
                int Rank = Db.UserRank(User.Id);
                if (!Ranks.ContainsKey(Rank))
                {
                    Ranks.Add(Rank, new List<string>());
                }

                Ranks[Rank].Add(User.Mention);
            }

            var Msg = new StringBuilder();
            foreach (KeyValuePair<int, List<string>> KVP in Ranks.OrderBy(x => -x.Key))
            {
                Msg.Append("Rank ");
                Msg.Append(KVP.Key);
                Msg.Append(": ");
                Msg.Append(string.Join(", ", KVP.Value));
                Msg.Append("\n");
            }

            e.Respond(Msg.ToString());
        }

        public static void Sleep(object s, MessageEventArgs e)
        {
            e.Respond("Bye!");
            Bot.Shutdown();
        }

        public static async void SetName(object s, MessageEventArgs e)
        {
            await Bot.Client.CurrentUser.Edit(Bot.Token, username: (string)s);
        }

        public static async void SetAvatar(object s, MessageEventArgs e)
        {
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Uri.EscapeUriString((string)s));
                webRequest.Timeout = 5000;
                WebResponse webResponse = await webRequest.GetResponseAsync();

                Image image = Image.FromStream(webResponse.GetResponseStream());
                MemoryStream stream = new MemoryStream();
                image.Save(stream, ImageFormat.Png);
                stream.Position = 0;

                await Bot.Client.CurrentUser.Edit(Bot.Token, avatar: stream);
            }
            catch
            {
                e.Respond("Oops.. I couldn't use that file");
            }
        }

        public static async void Prune(object s, MessageEventArgs e)
        {
            int MsgCount = 0;
            List<ulong> ClearUsers = new List<ulong>();

            string[] Message = ((string)s).Split(' ');
            if (Message.Length > 0)
            {
                if (Message[0] == "all")
                {
                    MsgCount = int.MaxValue;
                }
                else
                {
                    int.TryParse(Message[0], out MsgCount);
                }
            }

            if (MsgCount == 0)
            {
                MsgCount = 5;
            }

            foreach (User ClearUser in e.Message.MentionedUsers)
            {
                ClearUsers.Add(ClearUser.Id);
            }

            if (ClearUsers.Count == 0)
            {
                ClearUsers.Add(Bot.Client.CurrentUser.Id);
            }

            IEnumerable<Message> Messages = await e.Channel.DownloadMessages(250);
            if (!e.Message.MentionedRoles.Contains(e.Server.EveryoneRole))
            {
                Messages = Messages.Where(m => m != null && m.User != null && ClearUsers.Contains(m.User.Id));
            }

            int i = 0;
            foreach (Message Msg in Messages)
            {
                if (++i > MsgCount)
                {
                    break;
                }

                try
                {
                    await Msg.Delete();
                }
                catch { }
                await Task.Delay(250);
            }
        }

        public static void Fix(object s, MessageEventArgs e)
        {
            Bot.Client.MessageQueue.Clear();
            Db.FlushCache();
            GC.Collect();
        }

        public static void Eval(object s, MessageEventArgs e)
        {
            var Parameters = new CompilerParameters();

            Parameters.ReferencedAssemblies.Add("system.dll");
            Parameters.ReferencedAssemblies.Add("discord.net.dll");

            Parameters.CompilerOptions = "/t:library";
            Parameters.GenerateInMemory = true;

            var Code = new StringBuilder();

            Code.Append("using System;\n");
            Code.Append("using Discord;\n");
            Code.Append("namespace CS { \n");
            Code.Append("public class Code { \n");
            Code.Append("public object Eval(MessageEventArgs e) {\n");
            Code.Append($"{(string)s}; \n");
            Code.Append($"return null; \n");
            Code.Append("} \n");
            Code.Append("} \n");
            Code.Append("}\n");

            var Compiled = new CSharpCodeProvider().CompileAssemblyFromSource(Parameters, Code.ToString());
            if (Compiled.Errors.Count > 0)
            {
                e.Respond($"Error ({Compiled.Errors[0].ErrorText}) evaluating {(string)s}");
            }
            else
            {
                object o = Compiled.CompiledAssembly.CreateInstance("CS.Code");
                var Result = o.GetType().GetMethod("Eval").Invoke(o, new object[] { e }).ToString();
                if (Result == null)
                {
                    e.Respond("There was no result");
                }
                else
                {
                    e.Respond(Result);
                }
            }
        }

        public static void JoinServer(object s, MessageEventArgs e)
        {
            e.Respond(Bot.InviteLink);
        }
        
        public static async void LeaveServer(object s, MessageEventArgs e)
        {
            if (e.Message.MentionedUsers.Any(x => x.Id == Bot.Client.CurrentUser.Id))
            {
                ServerData.Servers[e.Server.Id].StopHandlers();
                await ServerData.Servers[e.Server.Id].Server.Leave();
                ServerData.Servers.Remove(e.Server.Id);
            }
        }
    }
}
