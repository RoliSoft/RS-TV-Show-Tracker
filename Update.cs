namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Data.SQLite;
    using System.Threading.Tasks;

    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Occurs when the update is done.
    /// </summary>
    public delegate void UpdateDone();

    /// <summary>
    /// Occurs when the update has encountered an error.
    /// </summary>
    public delegate void UpdateError(string message, Exception exception, bool fatalToShow, bool fatalToWholeUpdate);

    /// <summary>
    /// Occurs when the progress has changed on the update.
    /// </summary>
    public delegate void UpdateProgressChanged(string show, double percentage);

    /// <summary>
    /// Provides methods to keep the database up-to-date.
    /// </summary>
    public class Updater
    {
        /// <summary>
        /// Occurs when the update is done.
        /// </summary>
        public event UpdateDone UpdateDone;

        /// <summary>
        /// Occurs when the update has encountered an error.
        /// </summary>
        public event UpdateError UpdateError;

        /// <summary>
        /// Occurs when the progress has changed on the update.
        /// </summary>
        public event UpdateProgressChanged UpdateProgressChanged;

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
                if (UpdateError != null)
                {
                    UpdateError("Could not begin SQLite transaction.", ex, false, true);
                }
                return;
            }

            // get list of active shows
            var shows = Database.Query("select showid, name, (select value from showdata where showdata.showid = tvshows.showid and key = 'grabber') as grabber from tvshows where (select value from showdata where showdata.showid = tvshows.showid and key = 'airing') = 'True' order by rowid asc");
            
            var i = 0d;
            foreach (var r in shows)
            {
                // fire event
                if (UpdateProgressChanged != null)
                {
                    UpdateProgressChanged(r["name"], ++i / shows.Count * 100);
                }

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
                    if (UpdateError != null)
                    {
                        UpdateError("Could not get guide object for '" + r["name"] + "'", ex, true, false);
                    }
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
                    if (UpdateError != null)
                    {
                        UpdateError("Could not get guide ID for '" + r["name"] + "'", ex, true, false);
                    }
                    continue;
                }

                // get data from guide
                Guide.TVShow tv;
                try
                {
                    tv = guide.GetData(id);
                }
                catch (Exception ex)
                {
                    if (UpdateError != null)
                    {
                        UpdateError("Could not get guide data for '" + r["name"] + "'", ex, true, false);
                    }
                    continue;
                }

                // update showdata fields
                Database.ShowData(r["showid"], "genre",   tv.Genre);
                Database.ShowData(r["showid"], "actors",  tv.Actors);
                Database.ShowData(r["showid"], "descr",   tv.Description);
                Database.ShowData(r["showid"], "cover",   tv.Cover);
                Database.ShowData(r["showid"], "airing",  tv.Airing.ToString());
                Database.ShowData(r["showid"], "airtime", tv.AirTime);
                Database.ShowData(r["showid"], "airday",  tv.AirDay);
                Database.ShowData(r["showid"], "network", tv.Network);
                Database.ShowData(r["showid"], "runtime", tv.Runtime.ToString());

                // update episodes
                foreach (var ep in tv.Episodes)
                {
                    try
                    {
                        Database.ExecuteOnTransaction(tr, "insert into episodes values (?, ?, ?, ?, ?, ?, ?, ?)",
                                                      ep.Number + (ep.Season * 1000) + (int.Parse(r["showid"]) * 100 * 1000),
                                                      r["showid"],
                                                      ep.Season,
                                                      ep.Number,
                                                      tv.AirTime == String.Empty || ep.AirDate == Utils.UnixEpoch
                                                       ? Utils.DateTimeToUnix(ep.AirDate)
                                                       : Utils.DateTimeToUnix(DateTime.Parse(ep.AirDate.ToString("yyyy-MM-dd ") + tv.AirTime).ToLocalTimeZone()),
                                                      ep.Title,
                                                      ep.Summary,
                                                      ep.Picture);
                    }
                    catch (Exception ex)
                    {
                        if (UpdateError != null)
                        {
                            UpdateError(string.Format("Could not insert '{0} S{1:00}E{2:00}' into database.", r["name"], ep.Season, ep.Number), ex, false, false);
                        }
                    }
                }
            }

            // commit the changes
            try
            {
                tr.Commit();
            }
            catch (Exception ex)
            {
                if (UpdateError != null)
                {
                    UpdateError("Could not commit changes to database.", ex, false, true);
                }
                return;
            }

            // set last updated and vacuum database
            try
            {
                Database.Setting("lastupdate", Utils.DateTimeToUnix(DateTime.Now).ToString());
                Database.Execute("vacuum");
            }
            catch (Exception ex)
            {
                if (UpdateError != null)
                {
                    UpdateError("Could not vacuum the database.", ex, false, false);
                }
            }

            // fire data change event
            MainWindow.Active.DataChanged();

            // fire event
            if (UpdateDone != null)
            {
                UpdateDone();
            }
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
                        if (UpdateError != null)
                        {
                            UpdateError("The update function has quit with an exception.", ex, false, true);
                        }
                    }
                }).Start();
        }

        /// <summary>
        /// Creates the guide object from name.
        /// </summary>
        /// <param name="name">The name of the class.</param>
        /// <returns>New guide class.</returns>
        /// <exception cref="NotSupportedException">When a name is specified which is not supported.</exception>
        public Guide CreateGuide(string name)
        {
            switch(name)
            {
                case "TVRage":
                    return new TVRage();

                case "TVDB":
                    return new TVDB();

                case "EPGuides":
                    return new EPGuides();

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
