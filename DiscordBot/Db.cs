﻿using System;
using System.Collections.Concurrent;
using System.Data.SQLite;

namespace DiscordBot
{
    class Db
    {
        private static SQLiteConnection Connect
        {
            get
            {
                SQLiteConnection Connect = new SQLiteConnection("data source=" + Bot.DbDir + "discord.sqlite; Version=3;");
                Connect.Open();
                return Connect;
            }
        }

        private static SQLiteCommand Command(SQLiteConnection Conn, string Query, object Var)
            => Command(Conn, Query, new object[] { Var });

        private static SQLiteCommand Command(SQLiteConnection Conn, string Query, object Var1, object Var2)
            => Command(Conn, Query, new object[] { Var1, Var2 });

        private static SQLiteCommand Command(SQLiteConnection Conn, string Query, object[] Vars = null)
        {
            SQLiteCommand Cmd = Conn.CreateCommand();
            Cmd.CommandText = Query;

            if (Vars != null)
            {
                string Key;
                for (int i = 0; i < Vars.Length; i++)
                {
                    Key = "@var" + i.ToString();
                    Query = Query.ReplaceFirst("?", Key);
                    Cmd.Parameters.AddWithValue(Key, Vars[i]);
                }
            }

            return Cmd;
        }

        public static void AddPoints(ulong UserId, int Points = 1)
        {
            using (SQLiteConnection Conn = Connect)
            {
                Command(Conn, "UPDATE users SET points = points + " + Points + " WHERE id = ?", UserId).ExecuteDispose();
            }
        }

        public static int GetPoints(ulong UserId)
        {
            using (SQLiteConnection Conn = Connect)
            {
                using (SQLiteCommand Cmd = Command(Conn, "SELECT points FROM users WHERE id = ?", UserId))
                {
                    using (SQLiteDataReader Reader = Cmd.ExecuteReader())
                    {
                        if (!Reader.Read())
                        {
                            return 0;
                        }

                        return Convert.ToInt32(Reader["points"]);
                    }
                }
            }
        }

        public static bool HasPermission(ulong UserId, string Name)
        {
            return (UserId == Bot.Owner || UserRank(UserId) >= PermissionRank(Name));
        }

        private static ConcurrentDictionary<ulong, int> UserRankCache = new ConcurrentDictionary<ulong, int>();
        public static int UserRank(ulong UserId)
        {
            int Rank;
            if (!UserRankCache.TryGetValue(UserId, out Rank))
            {
                using (SQLiteConnection Conn = Connect)
                {
                    using (SQLiteCommand Cmd = Command(Conn, "SELECT rank FROM users WHERE id = ?", UserId))
                    {
                        using (SQLiteDataReader Reader = Cmd.ExecuteReader())
                        {
                            if (Reader.Read())
                            {
                                Rank = Convert.ToInt32(Reader["rank"]);
                            }
                            else
                            {
                                ForceAddAccount(UserId);
                                Rank = 1;
                            }
                        }
                    }
                }

                UserRankCache.TryAdd(UserId, Rank);
            }

            return Rank;
        }

        public static void SetRank(ulong UserId, int Rank)
        {
            using (SQLiteConnection Conn = Connect)
            {
                Command(Conn, "UPDATE users SET rank = ? WHERE id = ?", Rank, UserId).ExecuteDispose();
            }

            int OldRank;
            if (UserRankCache.TryGetValue(UserId, out OldRank))
            {
                UserRankCache.TryUpdate(UserId, Rank, OldRank);
            }
            else
            {
                UserRankCache.TryAdd(UserId, Rank);
            }
        }

        private static ConcurrentDictionary<string, int> PermissionRankCache = new ConcurrentDictionary<string, int>();
        public static int PermissionRank(string Name)
        {
            int Rank;

            if (!PermissionRankCache.TryGetValue(Name, out Rank))
            {
                using (SQLiteConnection Conn = Connect)
                {
                    using (SQLiteCommand Cmd = Command(Conn, "SELECT minrank FROM permissions WHERE name = ?", Name))
                    {
                        using (SQLiteDataReader Reader = Cmd.ExecuteReader())
                        {
                            if (Reader.Read())
                            {
                                Rank = Convert.ToInt32(Reader["minrank"]);
                            }
                            else
                            {
                                Reader.Close();
                                ("Permission is not in the database: " + Name).Log();
                                Command(Conn, "INSERT INTO permissions (name, minrank) VALUES (?, 1)", Name).ExecuteDispose();
                                Rank = 1;
                            }
                        }
                    }
                }
                
                PermissionRankCache.TryAdd(Name, Rank);
            }

            return Rank;
        }

        public static void SetPerm(string Name, int Rank)
        {
            using (SQLiteConnection Conn = Connect)
            {
                Command(Conn, "UPDATE permissions SET minrank = ? WHERE name = ?", Rank, Name).ExecuteDispose();
            }

            int OldRank;
            if (PermissionRankCache.TryGetValue(Name, out OldRank))
            {
                PermissionRankCache.TryUpdate(Name, Rank, OldRank);
            }
            else
            {
                PermissionRankCache.TryAdd(Name, Rank);
            }
        }

        public static void ForceAddAccount(ulong UserId)
        {
            using (SQLiteConnection Conn = Connect)
            {
                Command(Conn, "INSERT OR IGNORE INTO users (id) VALUES (?)", UserId).ExecuteDispose();
            }
        }

        public static bool ChannelToggleCategory(ulong ChannelId, string Category)
        {
            using (SQLiteConnection Conn = Connect)
            {
                if (ChannelDisabledCategory(ChannelId, Category))
                {
                    Command(Conn, "DELETE FROM disabledcats WHERE channel = ? AND category = ?", ChannelId, Category).ExecuteDispose();
                    return true;
                }

                Command(Conn, "INSERT INTO disabledcats (channel, category) VALUES (?, ?)", ChannelId, Category).ExecuteDispose();
                return false;
            }
        }

        public static bool ChannelDisabledCategory(ulong ChannelId, string Category)
        {
            using (SQLiteConnection Conn = Connect)
            {
                using (SQLiteCommand Cmd = Command(Conn, "SELECT COUNT(*) FROM disabledcats WHERE channel = ? AND category = ?", ChannelId, Category))
                {
                    return (Convert.ToInt32(Cmd.ExecuteScalar()) > 0);
                }
            }
        }

        private static ConcurrentDictionary<long, ulong> DiscordServerCache = new ConcurrentDictionary<long, ulong>();
        public static void SetDiscordServerId(long TelegramId, ulong DiscordServerId)
        {
            using (SQLiteConnection Conn = Connect)
            {
                Command(Conn, "DELETE FROM tglinks WHERE tgid = ?", TelegramId).ExecuteDispose();
                Command(Conn, "INSERT INTO tglinks (tgid, discordid) VALUES (?, ?)", TelegramId, DiscordServerId).ExecuteDispose();
            }

            ulong OldDiscordServerId;
            if (DiscordServerCache.TryGetValue(TelegramId, out OldDiscordServerId))
            {
                DiscordServerCache.TryUpdate(TelegramId, DiscordServerId, OldDiscordServerId);
            }
            else
            {
                DiscordServerCache.TryAdd(TelegramId, DiscordServerId);
            }
        }

        public static ulong GetDiscordServerId(long TelegramId)
        {
            ulong Server;
            if (!DiscordServerCache.TryGetValue(TelegramId, out Server))
            {
                using (SQLiteConnection Conn = Connect)
                {
                    using (SQLiteCommand Cmd = Command(Conn, "SELECT discordid FROM tglinks WHERE tgid = ?", TelegramId))
                    {
                        using (SQLiteDataReader Reader = Cmd.ExecuteReader())
                        {
                            if (!Reader.Read())
                            {
                                return 0;
                            }

                            Server = Convert.ToUInt64(Reader["discordid"]);
                        }
                    }
                }

                DiscordServerCache.TryAdd(TelegramId, Server);
            }

            return Server;
        }

        public static void FlushCache()
        {
            UserRankCache.Clear();
            PermissionRankCache.Clear();
            DiscordServerCache.Clear();
        }
    }
}
