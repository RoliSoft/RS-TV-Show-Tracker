namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using DictList = System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>;

    /// <summary>
    /// Provides access to the default database.
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// Gets or sets the active connection.
        /// </summary>
        /// <value>The active connection.</value>
        public static SQLiteConnection Connection { get; set; }

        /// <summary>
        /// Gets or sets the XML document in which the settings are stored.
        /// </summary>
        /// <value>The XML settings document.</value>
        public static XDocument XmlSettings { get; set; }

        /// <summary>
        /// Gets or sets the cache container.
        /// </summary>
        /// <value>The cache container.</value>
        public static Dictionary<string, dynamic> Cache { get; set; }

        /// <summary>
        /// Gets or sets the date when the data was last changed. This field is used for caching purposes, and it's not automatically updated by <c>Execute()</c>.
        /// </summary>
        /// <value>The date of last change.</value>
        public static DateTime DataChange { get; set; }

        /// <summary>
        /// Initializes the <see cref="Database"/> class.
        /// </summary>
        static Database()
        {
            Connection  = new SQLiteConnection(@"Data Source=C:\Users\RoliSoft\Documents\Visual Studio 2010\Projects\RS TV Show Tracker\RS TV Show Tracker\TVShows.db3");
            XmlSettings = XDocument.Load(@"C:\Users\RoliSoft\Documents\Visual Studio 2010\Projects\RS TV Show Tracker\RS TV Show Tracker\Settings.xml");
            Cache       = new Dictionary<string, dynamic>();
            DataChange  = DateTime.Now;

            Connection.Open();
        }

        /// <summary>
        /// Queries the SQL database.
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <param name="args">The arguments in the SQL query.</param>
        /// <returns>List of dictionary of key-value.</returns>
        public static DictList Query(string sql, params dynamic[] args)
        {
            using (var cmd = new SQLiteCommand(sql, Connection))
            {
                if (args.Length != 0)
                {
                    foreach (var arg in args)
                    {
                        cmd.Parameters.Add(new SQLiteParameter { Value = arg != null ? arg.ToString() : string.Empty });
                    }
                }

                lock (Connection)
                {
                    using (var dr = cmd.ExecuteReader())
                    {
                        var al = new DictList();

                        while (dr.Read())
                        {
                            var dic = new Dictionary<string, string>();

                            for (var i = 0; i != dr.FieldCount; i++)
                            {
                                dic[dr.GetName(i)] = dr[i].ToString();
                            }

                            al.Add(dic);
                        }

                        return al;
                    }
                }
            }
        }

        /// <summary>
        /// Executes an SQL statement.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <param name="args">The arguments in the SQL statement.</param>
        /// <returns>Number of changed rows.</returns>
        public static int Execute(string sql, params dynamic[] args)
        {
            using (var cmd = new SQLiteCommand(sql, Connection))
            {
                if (args.Length != 0)
                {
                    foreach (var arg in args)
                    {
                        cmd.Parameters.Add(new SQLiteParameter { Value = arg != null ? arg.ToString() : string.Empty });
                    }
                }

                lock (Connection)
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Executes an SQL statement on a transaction.
        /// </summary>
        /// <param name="transaction">The open transaction.</param>
        /// <param name="sql">The SQL statement.</param>
        /// <param name="args">The arguments in the SQL statement.</param>
        /// <returns>Number of changed rows.</returns>
        public static int ExecuteOnTransaction(SQLiteTransaction transaction, string sql, params dynamic[] args)
        {
            using(var cmd = new SQLiteCommand(sql, Connection, transaction))
            {
                if (args.Length != 0)
                {
                    foreach (var arg in args)
                    {
                        cmd.Parameters.Add(new SQLiteParameter { Value = arg != null ? arg.ToString() : string.Empty });
                    }
                }

                lock (Connection)
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Retrieves the key from the SQL settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string Setting(string key)
        {
            if (Cache.ContainsKey(key))
            {
                return Cache[key];
            }

            using (var cmd = new SQLiteCommand("select value from settings where key = ?", Connection))
            {
                cmd.Parameters.Add(new SQLiteParameter { Value = key });

                lock (Connection)
                {
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            Cache[key] = dr["value"].ToString();

                            return dr["value"].ToString();
                        }

                        return String.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the key from the show data table.
        /// </summary>
        /// <param name="id">The id of the show.</param>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string ShowData(string id, string key)
        {
            using (var cmd = new SQLiteCommand("select value from showdata where showid = ? and key = ?", Connection))
            {
                cmd.Parameters.Add(new SQLiteParameter { Value = id  });
                cmd.Parameters.Add(new SQLiteParameter { Value = key });

                lock (Connection)
                {
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return dr["value"].ToString();
                        }

                        return String.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Stores the key and value into the SQL settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static int Setting(string key, string value)
        {
            Cache[key] = value;

            using (var cmd = new SQLiteCommand("insert into settings values (?, ?)", Connection))
            {
                cmd.Parameters.Add(new SQLiteParameter { Value = key   });
                cmd.Parameters.Add(new SQLiteParameter { Value = value });

                lock (Connection)
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Stores the key and value into the show data table.
        /// </summary>
        /// <param name="id">The id of the show.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static int ShowData(string id, string key, string value)
        {
            var data = (id + "|" + key).GetHashCode();

            using (var cmd = new SQLiteCommand("insert into showdata values (?, ?, ?, ?)", Connection))
            {
                cmd.Parameters.Add(new SQLiteParameter { Value = data  });
                cmd.Parameters.Add(new SQLiteParameter { Value = id    });
                cmd.Parameters.Add(new SQLiteParameter { Value = key   });
                cmd.Parameters.Add(new SQLiteParameter { Value = value });

                lock (Connection)
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets the ID of a show in the database.
        /// </summary>
        /// <param name="show">The name of the show.</param>
        /// <returns>ID of the show or empty string.</returns>
        public static string GetShowID(string show)
        {
            var showid = Query("select showid from tvshows where name = ? limit 1", show);

            return showid.Count != 0
                   ? showid[0]["showid"]
                   : string.Empty;
        }

        /// <summary>
        /// Gets the ID of an episode in the database.
        /// </summary>
        /// <param name="show">The name of the show.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The number of the episode.</param>
        /// <returns>ID of the show or empty string.</returns>
        public static string GetEpisodeID(string show, int season, int episode)
        {
            var episodeid = Query("select episodeid from episodes where showid = ? and season = ? and episode = ? limit 1", show, season, episode);

            return episodeid.Count != 0
                   ? episodeid[0]["episodeid"]
                   : string.Empty;
        }

        /// <summary>
        /// Retrieves the key from the XML settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string XmlSetting(string key)
        {
            if(Cache.ContainsKey(key))
            {
                return Cache[key];
            }

            try
            {
                return Cache[key] = XmlSettings
                                    .Descendants("setting")
                                    .Single(node => node.Attribute("key").Value == key)
                                    .Attribute("value")
                                    .Value;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Stores the key and value into the XML settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void XmlSetting(string key, string value)
        {
            Cache[key] = XmlSettings
                         .Descendants("setting")
                         .Single(node => node.Attribute("key").Value == key)
                         .Attribute("value")
                         .Value = value;

            SaveXml();
        }

        /// <summary>
        /// Saves the XML settings into the file.
        /// </summary>
        public static void SaveXml()
        {
            File.WriteAllText(@"C:\Users\RoliSoft\Documents\Visual Studio 2010\Projects\RS TV Show Tracker\RS TV Show Tracker\Settings.xml", "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n" + XmlSettings);
        }
    }
}
