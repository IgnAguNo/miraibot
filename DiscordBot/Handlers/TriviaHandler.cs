using Discord;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.Handlers
{
    class TriviaHandler : IHandler
    {
        private Channel Channel;
        private CancellationTokenSource CancelToken = null;
        private Dictionary<ulong, int> Points = new Dictionary<ulong, int>();

        private string Question = null;
        private string Answer = null;
        private const int PointLimit = 10;

        public string HardHint
        {
            get
            {
                if (Answer == null)
                {
                    return string.Empty;
                }

                char[] Letters = Answer.ToArray();
                int Count = 0;
                for (int i = 1; i < Letters.Length; i++)
                {
                    if (char.IsLetterOrDigit(Letters[i]) && ++Count % 3 != 0)
                    {
                        Letters[i] = '_';
                    }

                }

                return string.Join(" ", Letters);
            }
        }

        public string EasyHint
        {
            get
            {
                if (Answer == null)
                {
                    return string.Empty;
                }

                char[] Letters = Answer.ToArray();
                int Count = 0;
                for (int i = 1; i < Letters.Length; i++)
                {
                    if (char.IsLetterOrDigit(Letters[i]) && ++Count % 3 == 2)
                    {
                        Letters[i] = '_';
                    }

                }

                return string.Join(" ", Letters);
            }
        }

        public override string Name
        {
            get
            {
                return "Trivia";
            }
        }

        private User Winner = null;
        private static Regex RemoveHtml = new Regex("<.*?>", RegexOptions.Compiled);

        public TriviaHandler(Channel Channel)
        {
            this.Channel = Channel;
        }

        public void Start()
        {
            if (CancelToken == null)
            {
                CancelToken = new CancellationTokenSource();

                Task.Run(async () =>
                {
                    string Json;
                    JObject Trivia = null;
                    Send(Channel, "Welcome to the trivia! To win, you need " + PointLimit + " points");
                    
                    while (!CancelToken.IsCancellationRequested && !Points.ContainsValue(PointLimit))
                    {
                        try
                        {
                            Question = string.Empty;
                            while (Question == string.Empty)
                            {
                                Json = await "http://jservice.io/api/random?count=1".ResponseAsync();
                                Trivia = JObject.Parse(Json.Substring(1, Json.Length - 2));
                                Question = Trivia["question"].ToString().Trim();
                            }

                            Answer = RemoveHtml.Replace(Trivia["answer"].ToString(), string.Empty).Replace("\\", "").Replace("(", "").Replace(")", "").Trim('"');
                            if (Answer.StartsWith("a "))
                            {
                                Answer = Answer.Substring(2);
                            }
                            else if (Answer.StartsWith("an "))
                            {
                                Answer = Answer.Substring(3);
                            }

                            $"{Question.Compact()} | {Answer}".Log();
                        }
                        catch (Exception Ex)
                        {
                            $"Trivia Exception {Ex}".Log();
                            continue;
                        }

                        await Task.Delay(100);
                        Send(Channel, Question);
                        this.Winner = null;

                        for (int i = 0; i < 60; i++)
                        {
                            await Task.Delay(500);
                            if (this.Winner != null)
                            {
                                if (!Points.ContainsKey(this.Winner.Id))
                                {
                                    Points.Add(this.Winner.Id, 0);
                                }

                                Points[this.Winner.Id]++;
                                break;
                            }

                            if (i == 20)
                            {
                                Send(Channel, "Hint: `" + HardHint + "`");
                            }
                            else if (i == 40)
                            {
                                Send(Channel, "Hint: `" + EasyHint + "`");
                            }
                        }

                        if (this.Winner == null)
                        {
                            Send(Channel, "Time's up! The answer was `" + this.Answer + "`");
                        }
                    }

                    await Task.Delay(250);

                    string SendStr = "End of the trivia";
                    KeyValuePair<ulong, int> Winner = Points.OrderBy(u => u.Value).LastOrDefault();
                    if (Winner.Value != 0)
                    {
                        Db.AddPoints(Winner.Key, Winner.Value);
                        try
                        {
                            SendStr += ", " + Channel.GetUser(Winner.Key).Mention + " won with " + Winner.Value + " point(s) - added " + Winner.Value + " extra pairs of glasses!";
                        }
                        catch
                        {
                            SendStr += " - did the winner leave the channel?";
                        }
                    }

                    Points.Clear();
                    Send(Channel, SendStr);

                    CancelToken = null;
                });
            }
        }

        public void Leaderboards()
        {
            string Text = string.Empty;
            foreach (KeyValuePair<ulong, int> KVP in this.Points.OrderBy(u => u.Value))
            {
                Text += Channel.GetUser(KVP.Key).Mention + " has " + KVP.Value + " point(s)\n";
            }

            if (Text != string.Empty)
            {
                Send(Channel, Text);
            }
            else
            {
                Send(Channel, "There are no scores yet!");
            }
        }

        public void Stop()
        {
            if (this.CancelToken != null)
            {
                this.CancelToken.Cancel();
            }
        }

        public bool Try(MessageEventArgs e)
        {
            if (Answer != null && Winner == null && e.Message.RawText.ToLower().Contains(Answer.ToLower()))
            {
                Winner = e.User;
                Send(Channel, "That's correct " + e.User.Mention + " - won one pair of glasses");

                Db.AddPoints(e.User.Id);
                return true;
            }

            return false;
        }
    }
}
