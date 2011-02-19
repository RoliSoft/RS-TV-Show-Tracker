namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Data.SQLite;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.Parsers.Guides.Engines;
    using RoliSoft.TVShowTracker.Remote;

    /// <summary>
    /// Provides methods to keep the database up-to-date.
    /// </summary>
    public class Updater
    {
        /// <summary>
        /// Occurs when the update is done.
        /// </summary>
        public event EventHandler<EventArgs> UpdateDone;

        /// <summary>
        /// Occurs when the update has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception, bool, bool>> UpdateError;

        /// <summary>
        /// Occurs when the progress has changed on the update.
        /// </summary>
        public event EventHandler<EventArgs<string, double>> UpdateProgressChanged;

        /// <summary>
        /// Does the update.
        /// </summary>
        public void Update()
        {
            // create transaction
            SQLiteTransaction tr;
            try
            {
                tr = Database.Connection.BeginTransaction();
            }
            catch (Exception ex)
            {
                UpdateError.Fire(this, "Could not begin SQLite transaction.", ex, false, true);
                return;
            }

            // get list of active shows
            var shows = Database.Query("select showid, name, (select value from showdata where showdata.showid = tvshows.showid and key = 'grabber') as grabber from tvshows where (select value from showdata where showdata.showid = tvshows.showid and key = 'airing') = 'True' order by rowid asc");
            
            var i = 0d;
            foreach (var r in shows)
            {
                // fire event
                UpdateProgressChanged.Fire(this, r["name"], ++i / shows.Count * 100);

                // get guide
                Guide guide;
                string gname;
                try
                {
                    guide = CreateGuide(r["grabber"]);
                    gname = guide.GetType().Name;
                }
                catch (Exception ex)
                {
                    UpdateError.Fire(this, "Could not get guide object for '" + r["name"] + "'", ex, true, false);
                    continue;
                }

                // get ID on guide
                string id;
                try
                {
                    id = Database.ShowData(r["showid"], gname + ".id");
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        id = guide.GetID(r["name"]);
                        Database.ShowData(r["showid"], gname + ".id", id);
                    }
                }
                catch (Exception ex)
                {
                    UpdateError.Fire(this, "Could not get guide ID for '" + r["name"] + "'", ex, true, false);
                    continue;
                }

                // get data from guide
                TVShow tv;
                try
                {
                    tv = guide.GetData(id);
                }
                catch (Exception ex)
                {
                    UpdateError.Fire(this, "Could not get guide data for '" + r["name"] + "'", ex, true, false);
                    continue;
                }

                // update showdata fields
                Database.ShowData(r["showid"], "genre",   tv.Genre);
                Database.ShowData(r["showid"], "descr",   tv.Description);
                Database.ShowData(r["showid"], "cover",   tv.Cover);
                Database.ShowData(r["showid"], "airing",  tv.Airing.ToString());
                Database.ShowData(r["showid"], "airtime", tv.AirTime);
                Database.ShowData(r["showid"], "airday",  tv.AirDay);
                Database.ShowData(r["showid"], "network", tv.Network);
                Database.ShowData(r["showid"], "runtime", tv.Runtime.ToString());
                Database.ShowData(r["showid"], "url",     tv.URL);

                // update episodes
                foreach (var ep in tv.Episodes)
                {
                    try
                    {
                        Database.ExecuteOnTransaction(tr, "insert into episodes values (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                                                      ep.Number + (ep.Season * 1000) + (r["showid"].ToInteger() * 100 * 1000),
                                                      r["showid"],
                                                      ep.Season,
                                                      ep.Number,
                                                      string.IsNullOrWhiteSpace(tv.AirTime) || ep.Airdate == Utils.UnixEpoch
                                                       ? ep.Airdate.ToUnixTimestamp()
                                                       : DateTime.Parse(ep.Airdate.ToString("yyyy-MM-dd ") + tv.AirTime).ToLocalTimeZone().ToUnixTimestamp(),
                                                      ep.Title,
                                                      ep.Summary,
                                                      ep.Picture,
                                                      ep.URL);
                    }
                    catch (Exception ex)
                    {
                        UpdateError.Fire(this, string.Format("Could not insert '{0} S{1:00}E{2:00}' into database.", r["name"], ep.Season, ep.Number), ex, false, false);
                    }
                }

                // asynchronously update lab.rolisoft.net's cache
                UpdateRemoteCache(new Tuple<string, string>(gname, id), tv);
            }

            // commit the changes
            try
            {
                tr.Commit();
            }
            catch (Exception ex)
            {
                    UpdateError.Fire(this, "Could not commit changes to database.", ex, false, true);
                return;
            }

            // set last updated and vacuum database
            try
            {
                Database.Setting("last update", DateTime.Now.ToUnixTimestamp().ToString());
                Database.Execute("vacuum");
            }
            catch (Exception ex)
            {
                UpdateError.Fire(this, "Could not vacuum the database.", ex, false, false);
            }

            // fire data change event
            MainWindow.Active.DataChanged();

            // fire event
            UpdateDone.Fire(this);
        }

        /// <summary>
        /// Does the update asynchronously.
        /// </summary>
        public void UpdateAsync()
        {
            new Task(() =>
                {
                    try
                    {
                        Update();
                    }
                    catch (Exception ex)
                    {
                        UpdateError.Fire(this, "The update function has quit with an exception.", ex, false, true);
                    }
                }).Start();
        }

        /// <summary>
        /// Creates the guide object from name.
        /// </summary>
        /// <param name="name">The name of the class.</param>
        /// <returns>New guide class.</returns>
        /// <exception cref="NotSupportedException">When a name is specified which is not supported.</exception>
        public static Guide CreateGuide(string name)
        {
            switch(name)
            {
                case "TVRage":
                    return new TVRage();

                case "TVDB":
                    return new TVDB();

                case "TVcom":
                    return new TVcom();

                case "EPGuides":
                    return new EPGuides();

                case "AniDB":
                    return new AniDB();

                case "Guess":
                    return new Guess();

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Sends metadata information about the TV show into lab.rolisoft.net cache.
        /// </summary>
        /// <param name="guide">The guide name and show ID on it.</param>
        /// <param name="tv">The TV show.</param>
        public static void UpdateRemoteCache(Tuple<string, string> guide, TVShow tv)
        {
            if (guide.Item1 == "Guess")
            {
                return;
            }

            new Thread(() => { try
            {
                var info = new Remote.Objects.ShowInfo
                    {
                        Title       = tv.Title,
                        Description = tv.Description,
                        Genre       = (tv.Genre ?? string.Empty).Split(new[] {", "}, StringSplitOptions.RemoveEmptyEntries),
                        Cover       = tv.Cover,
                        Started     = (long)tv.Episodes[0].Airdate.ToUnixTimestamp(),
                        Airing      = tv.Airing,
                        AirTime     = tv.AirTime,
                        AirDay      = tv.AirDay,
                        Network     = tv.Network,
                        Runtime     = tv.Runtime,
                        Seasons     = tv.Episodes.Last().Season,
                        Episodes    = tv.Episodes.Count,
                        Source      = guide.Item1,
                        SourceID    = guide.Item2
                    };

                var hash = BitConverter.ToString(new HMACSHA256(Encoding.ASCII.GetBytes(Utils.GetUUID() + "\0" + Signature.Version)).ComputeHash(Encoding.UTF8.GetBytes(
                    info.Title + info.Description + string.Join(string.Empty, info.Genre) + info.Cover + info.Started + (info.Airing ? "true" : "false") + info.AirTime + info.AirDay + info.Network + info.Runtime + info.Seasons + info.Episodes + info.Source + info.SourceID
                ))).ToLower().Replace("-", string.Empty);
                
                API.SetShowInfo(info, hash);
            } catch { } }).Start();
        }
    }
}
