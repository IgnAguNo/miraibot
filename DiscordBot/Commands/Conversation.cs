using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Commands
{
    class Conversation
    {
        public static string CantFind {
            get
            {
                if (new Random().Next(1, 4) > 1)
                {
                    return "I can't find it :(";
                }

                return "http://i.imgur.com/BCiKzNk.png";
            }
        }

        public static void Choose(object s, MessageEventArgs e)
        {
            if (((string)s).Trim() != String.Empty)
            {
                string[] Split = ((string)s).Replace(" and ", ",").Split(',');
                e.Respond("I choose " + Split[new Random().Next(0, Split.Length)].Trim());
            }
        }

        public static void Status(object s, MessageEventArgs e)
        {
            var OwnerAccount = e.Server.GetUser(Bot.Owner);
            if (OwnerAccount != null && OwnerAccount.Status == UserStatus.Online)
            {
                e.Respond("I'm fine :D");
            }
            else
            {
                e.Respond("I feel lonely");
            }
        }

        public static void Love(object s, MessageEventArgs e)
        {
            if (e.User.Id == Bot.Owner)
            {
                e.Respond("Yes!");
            }
            else
            {
                e.Respond("No..");
            }
        }

        public static void Insult(object s, MessageEventArgs e)
        {
            IEnumerable<User> Mentions = e.Message.MentionedUsers;
            if (Mentions.Count() == 2)
            {
                User Insult = e.Message.MentionedUsers.FirstOrDefault(m => m.Id != Bot.Client.CurrentUser.Id);
                if (Insult != null)
                {
                    if (Insult.Id == Bot.Owner)
                    {
                        e.Respond("I would never do that!");
                    }
                    else
                    {
                        string[] Insults = new string[] {
                            " is a faggot",
                            " is a weeaboo",
                            " has shit taste",
                            "'s waifu is trash",
                            " your memes aren't even dank",
                            " you weird glasses-fetishist"
                        };

                        e.Respond(Insult.Mention + Insults[new Random().Next(0, Insults.Length)]);
                    }
                }
            }
        }

        public static void Praise(object s, MessageEventArgs e)
        {
            IEnumerable<User> Mentions = e.Message.MentionedUsers;
            if (Mentions.Count() == 2)
            {
                User Insult = e.Message.MentionedUsers.FirstOrDefault(m => m.Id != Bot.Client.CurrentUser.Id);
                if (Insult != null)
                {
                    if (Insult.Id == Bot.Owner)
                    {
                        e.Respond("I don't need you for that!");
                    }
                    else
                    {
                        string[] Compliments = new string[] {
                            " is a nice person",
                            " is not unpleasant",
                            " is awesome"
                        };

                        e.Respond(Insult.Mention + Compliments[new Random().Next(0, Compliments.Length)]);
                    }
                }
            }
        }

        public static void Stab(object s, MessageEventArgs e)
        {
            IEnumerable<User> Mentions = e.Message.MentionedUsers;
            if (Mentions.Count() == 2)
            {
                User Insult = e.Message.MentionedUsers.FirstOrDefault(m => m.Id != Bot.Client.CurrentUser.Id);
                if (Insult != null)
                {
                    if (Insult.Id == Bot.Owner)
                    {
                        e.Respond("Don't make me do this.. Please..");
                    }
                    else
                    {
                        string[] StabImgs = new string[] {
                            "http://vignette1.wikia.nocookie.net/kyoukainokanata/images/5/5e/Stabbing-Akihito.png/revision/latest?cb=20131110041214",
                            "https://s-media-cache-ak0.pinimg.com/736x/1b/4a/2e/1b4a2e27fe20c0152131504a73498dd1.jpg",
                            "http://4.bp.blogspot.com/-nSDlmNZwBHw/Ul-bVRN5XvI/AAAAAAADrFE/ayEpwqy_gtc/s1600/KnK+-+03+-1.jpg",
                            "http://49.media.tumblr.com/9a8f4bc0fd0393cd6661da0e0012c2c3/tumblr_ne2iy1lRFQ1t9kr5po7_500.gif",
                            "http://45.media.tumblr.com/bc540ab848899db1584a0a04b4ef47a2/tumblr_mvgpuub6Pr1qbvovho1_500.gif"
                        };

                        e.Respond(Insult.Mention + " " + StabImgs[new Random().Next(0, StabImgs.Length)]);
                    }
                }
            }
        }

        public static void Hi(object s, MessageEventArgs e)
        {
            IEnumerable<User> Mentions = e.Message.MentionedUsers;
            if (Mentions.Count() == 2)
            {
                User Welcome = e.Message.MentionedUsers.FirstOrDefault(m => m.Id != Bot.Client.CurrentUser.Id);
                if (Welcome != null)
                {
                    e.Respond("Hi, " + Welcome.Mention + "!");
                }
            }
        }

        public static void Bye(object s, MessageEventArgs e)
        {
            IEnumerable<User> Mentions = e.Message.MentionedUsers;
            if (Mentions.Count() == 2)
            {
                User Goodbye = e.Message.MentionedUsers.FirstOrDefault(m => m.Id != Bot.Client.CurrentUser.Id);
                if (Goodbye != null)
                {
                    e.Respond("Bye " + Goodbye.Mention + "!");
                }
            }
        }

        public static void GoOut(object s, MessageEventArgs e)
        {
            if (e.User.Id == Bot.Owner)
            {
                e.Respond("I'm short on money so only if you pay");
            }
            else
            {
                e.Respond("I'm sorry, I don't really want to");
            }
        }

        public static void Best(object s, MessageEventArgs e)
        {
            e.Respond("*blush*");
        }

        public static void Cry(object s, MessageEventArgs e)
        {
            string[] Cry = new string[] {
                "http://45.media.tumblr.com/44ad73ea3def8d8a9d19d373af8efdd1/tumblr_myatwgWsuu1qbvovho1_500.gif",
                "https://38.media.tumblr.com/882f4ddd8246714ae90d8e14c1128462/tumblr_nwlxaomS7g1rvbl4vo1_500.gif"
            };

            e.Respond(Cry[new Random().Next(0, Cry.Length)]);
        }

        public static void TakeIt(object s, MessageEventArgs e)
        {
            string[] Accept = new string[] {
                "http://24.media.tumblr.com/127501c52a5eed326b363ed85087f777/tumblr_mus8rkat4J1qkcwzfo1_250.gif",
                "https://s-media-cache-ak0.pinimg.com/originals/e6/d5/8d/e6d58d221ae3df61580d95b34558e2d8.gif"
            };

            e.Respond(Accept[new Random().Next(0, Accept.Length)]);
        }

        public static void NoBully(object s, MessageEventArgs e)
        {
            string[] NoBullyResponses = new string[] {
                "Stop bullying! This is a no bully zone!",
                "Nobody likes bullies, I will hate you if you bully someone :(",
                "http://i.imgur.com/j4XBx5X.png"
            };

            e.Respond(NoBullyResponses[new Random().Next(0, NoBullyResponses.Length)]);
        }

        public static void Sing(object s, MessageEventArgs e)
        {
            string[] Sing = new string[] {
                "http://25.media.tumblr.com/50a3b7c75244eb0882e5706474489d7a/tumblr_mvzx00vMG61r2b9f8o1_400.gif",
                "http://25.media.tumblr.com/47e605a8b088ca71a1713838a11e736a/tumblr_mvw7o93rfK1soj51lo6_250.gif"
            };

            e.Respond(Sing[new Random().Next(0, Sing.Length)]);
        }

        public static void Dance(object s, MessageEventArgs e)
        {
            string[] Dance = new string[] {
                "http://2.bp.blogspot.com/-zZw7eAzkOEM/U6HVaOztqZI/AAAAAAAAaD0/zZny7-LTDsE/s1600/tumblr_inline_mvv1cu8lDl1rrn5dn.gif",
                "https://media.giphy.com/media/MDEWuO3nwkG4w/giphy.gif",
                "http://25.media.tumblr.com/07a02819a2bcc8b85c04df36ba324c4b/tumblr_mvv2oaj8To1rxaojso5_250.gif"
            };

            e.Respond(Dance[new Random().Next(0, Dance.Length)]);
        }

        public static void GoodNight(object s, MessageEventArgs e)
        {
            string[] Sleep = new string[] {
                "https://s-media-cache-ak0.pinimg.com/originals/3b/f4/ee/3bf4ee39efe4771ebbb599a95b3198ea.gif",
                "http://ic.pics.livejournal.com/huneybunni/43864531/23871/23871_600.jpg"
            };

            e.Respond("Good.. night.. " + Sleep[new Random().Next(0, Sleep.Length)]);
        }

        public static void Trip(object s, MessageEventArgs e)
        {
            string[] Stop = new string[] {
                "http://31.media.tumblr.com/db1578cae52f259bf19691a532f5d3f9/tumblr_mwjuhnXqkE1rvr5jyo1_500.gif",
                "http://25.media.tumblr.com/c8ace875dd06705abd5396656c34fcdd/tumblr_mu1w6ymQxH1r3rdh2o1_500.gif"
            };

            e.Respond(Stop[new Random().Next(0, Stop.Length)]);
        }

        public static void Weird(object s, MessageEventArgs e)
        {
            string[] Weird = new string[] {
                "https://lh3.googleusercontent.com/-evDNAFp07ZA/VgsZ5be1HQI/AAAAAAAAgsY/ifpfpZsnOo0/w506-h750/tumblr_n9lwug8BNZ1sjkemoo1_500.gif",
                "http://31.media.tumblr.com/33c10ce596f63406b55af9778f4496f2/tumblr_my2h5q4UNP1qztgoio1_250.gif",
                "Fuyukai desu."
            };

            e.Respond(Weird[new Random().Next(0, Weird.Length)]);
        }

        public static void Cute(object s, MessageEventArgs e)
        {
            string[] Cute = new string[] {
                "https://33.media.tumblr.com/61647a89950e6347b81b2f89b67bc0f2/tumblr_n51baeNqrg1tpn1qwo1_500.gif",
                "http://i.imgur.com/O0O0hEM.gifv",
                "http://pa1.narvii.com/5756/007f887f69b504cf53c8efe85d4496dbf2c62f6e_hq.gif",
                "http://38.media.tumblr.com/22ace82070d587408d7201243ed5b101/tumblr_inline_myhhim3d6L1svgbgx.gif"
            };

            e.Respond(Cute[new Random().Next(0, Cute.Length)]);
        }

        public static void Lewd(object s, MessageEventArgs e)
        {
            string[] Lewd = new string[] {
                "http://38.media.tumblr.com/e6761f9e234e08818509621c8e51eafa/tumblr_n4243oj6b51rercezo2_500.gif",
                "https://media.giphy.com/media/64hqWmBgN3F8Q/giphy.gif",
                "https://45.media.tumblr.com/1abc9678077d4bed98067e1fdc99c5e3/tumblr_n192soAPQ91sgn94ro1_500.gif"
            };

            e.Respond(Lewd[new Random().Next(0, Lewd.Length)]);
        }

        public static void Meme(object s, MessageEventArgs e)
        {
            char[] CharArray = ((string)s).ToUpper().ToCharArray();
            string Text = string.Join(" ", CharArray);
            for (int i = 1; i < CharArray.Length; i++)
            {
                Text += "\n" + CharArray[i];
            }
            
            e.Respond(Text);
        }

        private static string[] Shitposts = new string[] {
                "What the fuck did you just fucking say about dogman, you little bitch? I’ll have you know I graduated top of dankmemers in the anime_irl discourse channel, and I’ve been involved in numerous collective maymay creations, and I have over 300 confirmed shitposts. I am made into a meme 4chan and I’m the top shitposter on the entire internet. You are nothing to me but just another OC creator. I will copy your fucking memes with speeds the likes of which has never been seen before on this internet, mark my fucking words. You think you can get away with creating OC for us on the Internet? Think again, fucker. As we speak I am contacting my secret network of autists across the USA through Google Ultron and your IP is being traced right now by Adobe Reader so you better prepare for broken arms, maggot. The broken arms that wipe out the pathetic nonexistent thing you call your sexlife. You’re fucking dank, kid. I can be anywhere, anytime, and I can rip off OC in over seven hundred ways,  and that’s just with my mouse. Not only am I extensively trained in keyboard shortcuts, but I have access to the entire collection of macros and I will use it to its full extent to repost your miserable memes on reddit, you little shit. If only you could have known what unholy retribution your little “clever” meme was about to bring down upon you, maybe you would have kept reposting. But you couldn’t, you didn’t, and now you’re paying the price, you goddamn normie. I will send all mods to you and you will drown in abusive content reports. You’re fucking dead, memer.",
                "Here's the thing. You said a 'trilby is a fedora.' Is it in the same family? Yes. No one's arguing that. As someone who is an atheist who studies euphoria, I am telling you, specifically, in atheism, no one calls trilbys fedoras. If you want to be 'specific' like you said, then you should too. They're not the same thing. If you're saying 'fedora family' you're referring to the euphoric grouping of le reddit army, which includes things from neckbearded gentlesirs to highly intelligent intellectual like Smoke Degrasse Tyson, Reddit's Chief Supreme Ambassador of atheism and logic, and myself, the Deputy of Science and Crows. So your reasoning for calling a trilby a fedora is because random people 'say that only neckbeards wear fedoras?' Let's get Mountain Dew and Doritos in there, then, too. It's okay to just admit you're wrong, you know?",
                "Here's the thing. You said a 'stealy is a mealy'\nIs it in the same family? Yes. No one's arguing that.\nAs someone who is a scientist who studies dogman oc, I am telling you, specifically, in memeology, no one calls stealys mealys. If you want to be 'specific' like you said, then you shouldn't either. They're not the same thing.\nIf you're saying 'stealy family' you're referring to the taxonomic grouping of Memevidae, which includes things from animememes to dogmania to mealie-chan.\nSo your reasoning for calling a stealie a mealy is because random people 'call the low-pixel ones dank?' Let's get anime_irl and /a/ in there, then, too.\nAlso, calling someone a human or an ape? It's not one or the other, that's not how taxonomy works. They're both. A stealy is a stealy and a member of the theify family. But that's not what you said. You said a stealy is a mealy, which is not true unless you're okay with calling all members of the theify family stealy, which means you'd call animememes, dogmania, and other memes stealys, too. Which you said you don't.\nIt's okay to just admit you're wrong, you know?",
                "http://i.imgur.com/HpYQGi1.png", //birgerz sucks dick
                "http://i.imgur.com/yxdL5T3.png", //masturbate to jackson
                "http://i.imgur.com/C3oI1oM.png", //cum in hair
                "http://i.imgur.com/9pTfqqh.png", //daddy hanging out
                "http://i.imgur.com/Fybypx7.png", //futa minion
                "http://i.imgur.com/CsJLi0y.png", //wanna know the meme
                "http://i.imgur.com/uoWjpai.png", //own shitposts
                "http://i.imgur.com/u418ply.png", //shaku talks about porn
                "http://i.imgur.com/3LQABcI.png", //tacoposts
                "http://i.imgur.com/TWQxXPR.png", //ethnic dick
                "http://i.imgur.com/tdwDZev.png", //fuck sam
                "http://i.imgur.com/q9o8Y3e.png", //worldmap
                "http://i.imgur.com/9iwBkOL.png", //one in the butt
                "http://i.imgur.com/SqowoXV.png", //sams penis
                "http://i.imgur.com/hn5NKeP.png", //retards
                "http://i.imgur.com/HEVzmcE.png", //discourse mistake
                "http://i.imgur.com/JKTGRZS.jpg", //whitey
                "http://i.imgur.com/M46VQxl.jpg", //whitey 2
                "http://i.imgur.com/mQ2OxR6.png", //dickminion
                "http://i.imgur.com/jVEI4ZX.png", //double team aaragon
                "http://i.imgur.com/AafEVpG.png", //save aaragon
                "http://i.imgur.com/FBQv3ZQ.png", //how to double team aaragon
                "http://i.imgur.com/4ekqZv8.png", //penis to samhams grave
                "http://i.imgur.com/ivmURGT.png", //gaston shitpost
                "http://i.imgur.com/c0wlwf3.png", //gaston fabulous
                "http://i.imgur.com/PLPCqrv.png", //princess gaston
                "http://i.imgur.com/GPOAzf1.png", //melon smell
                "https://www.youtube.com/watch?v=vGHovgSLe6Q", //Ahegaoboy's Birthday
        };

        public static void Shitpost(object s, MessageEventArgs e)
        {
            e.Respond(Shitposts[new Random().Next(0, Shitposts.Length)]);
        }

        private static string[] Dogmans = new string[] {
                "http://i.imgur.com/oT1Wr4q.jpg", //mirai shitpost
                "http://i.imgur.com/LvRr3Uq.jpg", //fifty shades of dogman
                "http://i.imgur.com/y56CUzJ.png", //three little dogman
                "http://i.imgur.com/vmq562Q.png", //influence by dogman
                "http://i.imgur.com/wufn6Xa.jpg", //dogman wallpaper
                "http://i.imgur.com/BP9ZIKX.png", //dogman oc time
                "http://i.imgur.com/cg9xx10.png", //creepy dogman
                "http://i.imgur.com/Pp369Di.png", //seen enough dogman
                "http://i.imgur.com/093brkc.gif", //25x Danker
                "http://i.imgur.com/KJ4dC1Z.png", //START CapnKrunch Album http://imgur.com/a/B3k4T
                "http://i.imgur.com/NbUoNfN.png",
                "http://i.imgur.com/oOv7dxI.png",
                "http://i.imgur.com/nWw6CXE.png",
                "http://i.imgur.com/QpOjk3B.png",
                "http://i.imgur.com/AQ0Ck1s.png",
                "http://i.imgur.com/xEmTYSN.png",
                "http://i.imgur.com/kdV9Ej4.png",
                "http://i.imgur.com/QPtSY5E.png",
                "http://i.imgur.com/1qKrsXW.png",
                "http://i.imgur.com/ixhc8iV.png", //END
                "http://i.imgur.com/EKgtVaO.jpg", //START eolian Album http://imgur.com/a/NdTGs
                "http://i.imgur.com/ETWN48x.png",
                "http://i.imgur.com/AeBRQt5.png",
                "http://i.imgur.com/TaKNeyV.jpg",
                "http://i.imgur.com/YExpYji.png",
                "http://i.imgur.com/yXUY1rA.png",
                "http://i.imgur.com/x3wUIWw.png",
                "http://i.imgur.com/P7cei7X.png",
                "http://i.imgur.com/RsE0jYn.jpg",
                "http://i.imgur.com/M7TCH61.jpg",
                "http://i.imgur.com/QK7DAFp.png",
                "http://i.imgur.com/QK7DAFp.png",
                "http://i.imgur.com/QYybiud.png",
                "http://i.imgur.com/W9b0Og6.png",
                "http://i.imgur.com/dbo60zg.png",
                "http://i.imgur.com/muexqtP.png",
                "http://i.imgur.com/BUSSxiu.png",
                "http://i.imgur.com/SUdD9kv.png",
                "http://i.imgur.com/RUBWsUT.png",
                "http://i.imgur.com/YdiKX1Z.png",
                "http://i.imgur.com/vu83mfF.png",
                "http://i.imgur.com/3waXvVg.png",
                "http://i.imgur.com/mavuoJL.png",
                "http://i.imgur.com/kKvKvTr.png",
                "http://i.imgur.com/sqKa9Ad.png",
                "http://i.imgur.com/KWizrIM.png",
                "http://i.imgur.com/eZzvsV9.png",
                "http://i.imgur.com/h3ZkDA9.png",
                "http://i.imgur.com/xkE2CA1.png",
                "http://i.imgur.com/cLUsmFk.png",
                "http://i.imgur.com/g3RBq3Z.png",
                "http://i.imgur.com/quvuNqR.png",
                "http://i.imgur.com/Fg030p0.png",
                "http://i.imgur.com/5xGhcZ3.png",
                "http://i.imgur.com/J8rYG0L.png",
                "http://i.imgur.com/rWmpAnN.png",
                "http://i.imgur.com/JDnRyUn.png",
                "http://i.imgur.com/aGIjtkC.png",
                "http://i.imgur.com/b0tHYyY.png",
                "http://i.imgur.com/PcdmORz.png",
                "http://i.imgur.com/5tYfi34.png",
                "http://i.imgur.com/ffzLajx.png",
                "http://i.imgur.com/FAKr31p.png",
                "http://i.imgur.com/p5BkJ4H.png",
                "http://i.imgur.com/qivlW0g.png",
                "http://i.imgur.com/sVLJt9I.png",
                "http://i.imgur.com/bFv31ic.png",
                "http://i.imgur.com/nBRPaJd.png",
                "http://i.imgur.com/TqN7ku6.png",
                "http://i.imgur.com/Yvmv4bh.png",
                "http://i.imgur.com/dgjL5Ve.png",
                "http://i.imgur.com/li8wds9.png",
                "http://i.imgur.com/VbFTUeP.png",
                "http://i.imgur.com/IpXM0qK.png",
                "http://i.imgur.com/Tg1zoX4.png",
                "http://i.imgur.com/DhaWKSi.png",
                "http://i.imgur.com/iR5pYod.png",
                "http://i.imgur.com/U3HH9ly.png",
                "http://i.imgur.com/mPqPZDC.png",
                "http://i.imgur.com/gYXctKv.png",
                "http://i.imgur.com/sfEE78d.png",
                "http://i.imgur.com/HTn51zd.png",
                "http://i.imgur.com/RQAeaS9.png",
                "http://i.imgur.com/KafOuX7.png",
                "http://i.imgur.com/XvGS6bt.png",
                "http://i.imgur.com/xFQD8Y5.png",
                "http://i.imgur.com/JRvJNHZ.png",
                "http://i.imgur.com/tI0w31f.png",
                "http://i.imgur.com/RW9rCPO.png",
                "http://i.imgur.com/FiORYYo.png",
                "http://i.imgur.com/InGFX2q.png",
                "http://i.imgur.com/yDmF5ci.png",
                "http://i.imgur.com/DqUTUk9.png",
                "http://i.imgur.com/sD0pqEp.png",
                "http://i.imgur.com/cXIz3CD.png",
                "http://i.imgur.com/Oujk934.png",
                "http://i.imgur.com/aFQ5wQa.png",
                "http://i.imgur.com/oR21NHP.png",
                "http://i.imgur.com/D8bAdAC.png",
                "http://i.imgur.com/2K0nGqi.png",
                "http://i.imgur.com/Xr0B81b.png",
                "http://i.imgur.com/IRjMIiH.png",
                "http://i.imgur.com/6nVcFvQ.png",
                "http://i.imgur.com/GK1PxI0.png",
                "http://i.imgur.com/EfmdiUM.png",
                "http://i.imgur.com/f9LKMl1.png",
                "http://i.imgur.com/VNGAQYI.png",
                "http://i.imgur.com/X1oInMS.png",
                "http://i.imgur.com/pYH1bzi.png",
                "http://i.imgur.com/H1ekV9V.png",
                "http://i.imgur.com/0SSCrSF.png",
                "http://i.imgur.com/Tjau8Px.png",
                "http://i.imgur.com/XGl7bZ7.png",
                "http://i.imgur.com/RSllQDz.png",
                "http://i.imgur.com/QpFRBAF.png",
                "http://i.imgur.com/tFctZbu.png",
                "http://i.imgur.com/b5t90kd.png",
                "http://i.imgur.com/59uitH8.png",
                "http://i.imgur.com/LAY0E1A.png",
                "http://i.imgur.com/FCGYSqt.png",
                "http://i.imgur.com/16CC8k9.png",
                "http://i.imgur.com/IDQbls5.png",
                "http://i.imgur.com/g9RrSIC.png",
                "http://i.imgur.com/BLLD3SZ.png",
                "http://i.imgur.com/ebYOioh.png",
                "http://i.imgur.com/lh9rT3p.png",
                "http://i.imgur.com/5HDnp1p.png",
                "http://i.imgur.com/GvxCD5q.png",
                "http://i.imgur.com/Ae7Rxab.png",
                "http://i.imgur.com/ImqJcml.png",
                "http://i.imgur.com/mo26eBg.png",
                "http://i.imgur.com/0fxy2dR.png",
                "http://i.imgur.com/eEGwPoi.png",
                "http://i.imgur.com/R6M2A5Y.png",
                "http://i.imgur.com/wnuMTHS.png",
                "http://i.imgur.com/MOoXk4O.png",
                "http://i.imgur.com/y31UWwa.png",
                "http://i.imgur.com/GjL4roa.png",
                "http://i.imgur.com/R3Ig9sU.png",
                "http://i.imgur.com/jAijbLO.png",
                "http://i.imgur.com/Q6hoVQc.png",
                "http://i.imgur.com/TRm7R10.png",
                "http://i.imgur.com/2norkAP.png",
                "http://i.imgur.com/zpyQWjG.png",
                "http://i.imgur.com/oXMn9Ek.png",
                "http://i.imgur.com/apIgfYE.png",
                "http://i.imgur.com/pRw4yyw.png",
                "http://i.imgur.com/Wpy97tQ.png",
                "http://i.imgur.com/N35IvJm.png",
                "http://i.imgur.com/RKQSHSI.png",
                "http://i.imgur.com/CfXeXDv.png",
                "http://i.imgur.com/iEVRLMs.png",
                "http://i.imgur.com/vIp21Pj.png",
                "http://i.imgur.com/nPua9Me.png",
                "http://i.imgur.com/JEo105Q.png",
                "http://i.imgur.com/52AeUoh.png",
                "http://i.imgur.com/um94cdS.png",
                "http://i.imgur.com/bJ71ScV.png",
                "http://i.imgur.com/Ub0E2C6.png",
                "http://i.imgur.com/WLGeGep.png",
                "http://i.imgur.com/R13ydzJ.png",
                "http://i.imgur.com/U4xiOia.png",
                "http://i.imgur.com/VRO4Uwa.png",
                "http://i.imgur.com/Jq4Sjke.png",
                "http://i.imgur.com/RYRrtml.png",
                "http://i.imgur.com/BNLHcxx.png",
                "http://i.imgur.com/xitu68B.png",
                "http://i.imgur.com/ItcDr6B.png",
                "http://i.imgur.com/Aa9jhyr.png",
                "http://i.imgur.com/GXNfSep.png",
                "http://i.imgur.com/CLE70BA.png", //END
                "http://i.imgur.com/Lu4lwlL.jpg", //START eolian Album http://imgur.com/a/5eASf
                "http://i.imgur.com/GegOvrV.jpg",
                "http://i.imgur.com/Cqu9c79.png",
                "http://i.imgur.com/cEq1WfG.png",
                "http://i.imgur.com/EgKEnYA.png",
                "http://i.imgur.com/6eW9tMa.png",
                "http://i.imgur.com/31dSnjB.png",
                "http://i.imgur.com/JieWgws.png",
                "http://i.imgur.com/Gq1B6SG.png",
                "http://i.imgur.com/x6lfbja.png",
                "http://i.imgur.com/znKUwzQ.png",
                "http://i.imgur.com/PBtRVkI.png",
                "http://i.imgur.com/wKtRrxh.png",
                "http://i.imgur.com/ZB7cOGL.png",
                "http://i.imgur.com/d2R3sjE.png",
                "http://i.imgur.com/5rkHbVK.png",
                "http://i.imgur.com/jvgajNS.png",
                "http://i.imgur.com/EVF9lpF.png",
                "http://i.imgur.com/ii4elJ5.png",
                "http://i.imgur.com/gzwEJnt.png",
                "http://i.imgur.com/zWiSD2I.png",
                "http://i.imgur.com/NYbye6X.png",
                "http://i.imgur.com/RecVt5J.png",
                "http://i.imgur.com/Pim45zs.png",
                "http://i.imgur.com/Tkw6Bkb.png",
                "http://i.imgur.com/KwKdiuM.png",
                "http://i.imgur.com/Ges3Q4N.png",
                "http://i.imgur.com/0Wfnm05.png", //END
                "http://i.imgur.com/MoUgry3.png", //START eolian Album http://imgur.com/a/2UE2R
                "http://i.imgur.com/2PvC8EW.gif",
                "http://i.imgur.com/tIrk8wK.png",
                "http://i.imgur.com/ezXa7uA.png",
                "http://i.imgur.com/muazzzG.png",
                "http://i.imgur.com/7O37S1V.png",
                "http://i.imgur.com/TLUgUal.png",
                "http://i.imgur.com/AIIIltE.png",
                "http://i.imgur.com/SsA1d2u.png",
                "http://i.imgur.com/TBeakHZ.png",
                "http://i.imgur.com/TbvW2oW.png",
                "http://i.imgur.com/QSIeAL7.png",
                "http://i.imgur.com/k3eKMm9.png",
                "http://i.imgur.com/YmUd3KA.png",
                "http://i.imgur.com/ROmgx5f.png",
                "http://i.imgur.com/t5iaucq.png",
                "http://i.imgur.com/cutSyzS.png",
                "http://i.imgur.com/hgiiUOH.png",
                "http://i.imgur.com/4JeN71g.png",
                "http://i.imgur.com/T6DqUbW.png",
                "http://i.imgur.com/2w3zQue.png",
                "http://i.imgur.com/83ZBD54.png",
                "http://i.imgur.com/ftokDNZ.png",
                "http://i.imgur.com/dO48CPI.png",
                "http://i.imgur.com/cuh19ON.png",
                "http://i.imgur.com/EtlImQ5.png",
                "http://i.imgur.com/RLnouLh.png",
                "http://i.imgur.com/KI0fOT9.png",
                "http://i.imgur.com/G5bbdRI.png",
                "http://i.imgur.com/D9FQ9eu.png",
                "http://i.imgur.com/2ZHzdXq.png",
                "http://i.imgur.com/toJCX7B.png",
                "http://i.imgur.com/wAYiumx.png",
                "http://i.imgur.com/kcLZo2g.png",
                "http://i.imgur.com/RJJn7js.png",
                "http://i.imgur.com/1imjrZG.png",
                "http://i.imgur.com/AXapbu6.png",
                "http://i.imgur.com/Q0WWJUh.png",
                "http://i.imgur.com/i8NMwQm.png",
                "http://i.imgur.com/r3OivlC.png",
                "http://i.imgur.com/uH0iwwI.png",
                "http://i.imgur.com/ELkXZtL.png",
                "http://i.imgur.com/oDpzXoG.png",
                "http://i.imgur.com/5w83fBX.png",
                "http://i.imgur.com/QWd6bjB.png",
                "http://i.imgur.com/aLsYlqb.png",
                "http://i.imgur.com/rw2QcMa.png",
                "http://i.imgur.com/NZqluzC.png",
                "http://i.imgur.com/VKQ0xZ6.png",
                "http://i.imgur.com/4S5WLvF.png",
                "http://i.imgur.com/N63n2Cb.png",
                "http://i.imgur.com/yZUW6mm.png",
                "http://i.imgur.com/kFN9Gdi.png",
                "http://i.imgur.com/PF81Vpb.png",
                "http://i.imgur.com/MSMF9Ue.png",
                "http://i.imgur.com/X0Do62X.png",
                "http://i.imgur.com/T9Xtm4j.png",
                "http://i.imgur.com/J50G694.png",
                "http://i.imgur.com/zuFgwVs.png",
                "http://i.imgur.com/CT9ipB3.png",
                "http://i.imgur.com/h3QNIls.png",
                "http://i.imgur.com/QbxNATo.png",
                "http://i.imgur.com/k729d0g.png",
                "http://i.imgur.com/CBIRctl.png",
                "http://i.imgur.com/vpHiI3b.png",
                "http://i.imgur.com/Svs497M.png",
                "http://i.imgur.com/A8ubLSK.png",
                "http://i.imgur.com/DvbIXv2.png",
                "http://i.imgur.com/b8a53Ma.png",
                "http://i.imgur.com/GUVqRNP.png",
                "http://i.imgur.com/ECn89nO.png",
                "http://i.imgur.com/88KPaOi.png",
                "http://i.imgur.com/0ayEAXw.png",
                "http://i.imgur.com/1WO3JLC.png", //END
            };

        public static void Dogman(object s, MessageEventArgs e)
        {
            e.Respond(Dogmans[new Random().Next(0, Dogmans.Length)]);
        }
    }
}
