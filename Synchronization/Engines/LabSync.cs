namespace RoliSoft.TVShowTracker.Synchronization.Engines
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Provides synchronization with the API located at lab.rolisoft.net/api.
    /// </summary>
    public class LabSync : SyncEngine
    {
        private readonly string _user, _pass;
        private readonly ConcurrentQueue<Change> _changes;
        private readonly Timer _timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabSync"/> class.
        /// </summary>
        /// <param name="user">The username.</param>
        /// <param name="pass">The password.</param>
        public LabSync(string user, string pass)
        {
            _user = user;
            _pass = pass;

            _changes = new ConcurrentQueue<Change>();
            _timer   = new Timer
                {
                    AutoReset = false,
                    Interval  = 3000
                };
            _timer.Elapsed += DelayTimerOnElapsed;
        }

        /// <summary>
        /// Handles the Elapsed event of the DelayTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="elapsedEventArgs">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        private void DelayTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_changes.IsEmpty)
            {
                Log.Debug("Changes queue is empty.");
                return;
            }

            var chg = _changes.ToArray();

            Log.Debug("Pushing " + Utils.FormatNumber(chg.Length, "change") + " to sync server...");

            var req = Remote.API.SendDatabaseChanges(chg.OrderBy(c => c.Time), _user, _pass);

            if (!req.Success || !req.OK)
            {
                Log.Warn("Error while pushing changes to sync server." + Environment.NewLine + req.Error);
                Array.ForEach(chg, _changes.Enqueue);
            }
            else
            {
                Log.Debug("Changes pushed in " + req.Time + "s.");
            }
        }

        /// <summary>
        /// Adds a new TV show.
        /// </summary>
        /// <param name="show">The newly added TV show.</param>
        public override void AddShow(TVShow show)
        {
            Log.Debug("Queued change: add " + show.Title);
            _changes.Enqueue(new Change(show, ChangeType.AddShow, new Dictionary<string, string>(show.Data)));
            _timer.Start();
        }

        /// <summary>
        /// Modifies one or more properties of an existing TV show.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="modification">The array of modified parameters.</param>
        public override void ModifyShow(TVShow show, params string[] modification)
        {
            Log.Debug("Queued change: modify " + show.Title);
            _changes.Enqueue(new Change(show, ChangeType.ModifyShow, new Dictionary<string, string>(show.Data)));
            _timer.Start();
        }

        /// <summary>
        /// Removes an existing TV show.
        /// </summary>
        /// <param name="show">The TV show to be removed.</param>
        public override void RemoveShow(TVShow show)
        {
            Log.Debug("Queued change: remove " + show.Title);
            _changes.Enqueue(new Change(show, ChangeType.RemoveShow));
            _timer.Start();
        }

        /// <summary>
        /// Marks one or more episodes as seen.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="episodes">The list of episodes.</param>
        public override void MarkEpisodes(TVShow show, params int[] episodes)
        {
            Log.Debug("Queued change: mark " + show.Title + " episodes " + string.Join(", ", episodes));
            _changes.Enqueue(new Change(show, ChangeType.MarkEpisode, episodes));
            _timer.Start();
        }

        /// <summary>
        /// Marks one or more episodes as seen.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="episodes">The list of episode ranges. A range consists of two numbers from the same season.</param>
        public override void MarkEpisodes(TVShow show, params int[][] episodes)
        {
            Log.Debug("Queued change: mark " + show.Title + " episodes " + episodes.Aggregate(string.Empty, (c, i) => c + ", " + string.Join(", ", i)).TrimStart(", ".ToCharArray()));
            _changes.Enqueue(new Change(show, ChangeType.MarkEpisode, episodes));
            _timer.Start();
        }

        /// <summary>
        /// Unmarks one or more episodes.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="episodes">The list of episodes.</param>
        public override void UnmarkEpisodes(TVShow show, params int[] episodes)
        {
            Log.Debug("Queued change: unmark " + show.Title + " episodes " + string.Join(", ", episodes));
            _changes.Enqueue(new Change(show, ChangeType.UnmarkEpisode, episodes));
            _timer.Start();
        }

        /// <summary>
        /// Unmarks one or more episodes.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="episodes">The list of episode ranges. A range consists of two numbers from the same season.</param>
        public override void UnmarkEpisodes(TVShow show, params int[][] episodes)
        {
            Log.Debug("Queued change: unmark " + show.Title + " episodes " + episodes.Aggregate(string.Empty, (c, i) => c + ", " + string.Join(", ", i)).TrimStart(", ".ToCharArray()));
            _changes.Enqueue(new Change(show, ChangeType.UnmarkEpisode, episodes));
            _timer.Start();
        }

        /// <summary>
        /// Serializes and sends the full database.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if sent successfully.
        /// </returns>
        public override bool SendDatabase()
        {
            Log.Debug("Serializing full database for synchronization...");

            var db = new List<Change>();

            foreach (var show in Database.TVShows.Values)
            {
                db.Add(new Change(show, ChangeType.AddShowNMark, new[] { new Dictionary<string, string>(show.Data), SerializeMarkedEpisodes(show) }));
            }

            Log.Debug("Pushing " + Utils.FormatNumber(db.Count, "change") + " to sync server...");

            var req = Remote.API.SendDatabaseChanges(db, _user, _pass);

            if (!req.Success || !req.OK)
            {
                Log.Warn("Error while pushing database to sync server." + Environment.NewLine + req.Error);
                return false;
            }
            else
            {
                Log.Debug("Database pushed in " + req.Time + "s.");
                return true;
            }
        }

        /// <summary>
        /// Retrieves and applies the changes which have been made to the remote database.
        /// </summary>
        /// <returns>
        /// Number of changes since last synchronization or -1 on sync failure.
        /// </returns>
        public override int GetRemoteChanges()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Serializes the list of marked episodes for the specified TV show.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="range">if set to <c>true</c> sequential episode numbering will be turned into ranges.</param>
        /// <returns>
        /// List of marked episode ranges.
        /// </returns>
        public static object SerializeMarkedEpisodes(TVShow show, bool range = true)
        {
            var eps = show.Episodes.Where(e => e.Watched).OrderBy(e => e.ID).Select(e => e.ID - e.Show.ID * 1000000);

            if (!range)
            {
                return eps;
            }

            var list  = new List<object>();
            var start = 0;
            var prev  = 0;

            foreach (var ep in eps)
            {
                if (prev == ep - 1)
                {
                    prev = ep;
                }
                else
                {
                    if (start != 0 && prev != 0)
                    {
                        list.Add(start == prev ? start : (object)new[] { start, prev });
                    }

                    start = prev = ep;
                }
            }

            if ((list.Count == 0 && start != 0 && prev != 0) || (list.Count != 0 && list.Last() != new[] { start, prev }))
            {
                list.Add(start == prev ? start : (object)new[] { start, prev });
            }

            return list;
        }

        /// <summary>
        /// Represents a change in the outgoing queue.
        /// </summary>
        [Serializable]
        public class Change
        {
            /// <summary>
            /// Gets or sets the GMT unix timestamp which indicates when did this change occur.
            /// </summary>
            /// <value>
            /// The GMT unix timestamp.
            /// </value>
            public double Time { get; set; }

            /// <summary>
            /// Gets or sets the title.
            /// </summary>
            /// <value>
            /// The title.
            /// </value>
            public string Title { get; set; }

            /// <summary>
            /// Gets or sets the show ID.
            /// </summary>
            /// <value>
            /// The show ID.
            /// </value>
            public int ShowID { get; set; }

            /// <summary>
            /// Gets or sets the source.
            /// </summary>
            /// <value>
            /// The source.
            /// </value>
            public string Source { get; set; }

            /// <summary>
            /// Gets or sets the source ID.
            /// </summary>
            /// <value>
            /// The source ID.
            /// </value>
            public string SourceID { get; set; }

            /// <summary>
            /// Gets or sets the language.
            /// </summary>
            /// <value>
            /// The language.
            /// </value>
            public string Language { get; set; }

            /// <summary>
            /// Gets or sets the type of the change.
            /// </summary>
            /// <value>
            /// The type of the change.
            /// </value>
            [JsonConverter(typeof(StringEnumConverter))]
            public ChangeType Type { get; set; }

            /// <summary>
            /// Gets or sets the data.
            /// </summary>
            /// <value>
            /// The data.
            /// </value>
            public object Data { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Change" /> class.
            /// </summary>
            /// <param name="show">The show.</param>
            /// <param name="type">The type.</param>
            /// <param name="data">The data.</param>
            public Change(TVShow show, ChangeType type, object data = null)
            {
                Time = (DateTime.UtcNow.Ticks - 621355968000000000d) / 10000000d;

                Title    = show.Title;
                ShowID   = show.ID;
                Source   = show.Source;
                SourceID = show.SourceID;
                Language = show.Language;

                Type = type;
                Data = data ?? string.Empty;
            }
        }

        /// <summary>
        /// Describes the type of the change.
        /// </summary>
        public enum ChangeType
        {
            /// <summary>
            /// Adds a show.
            /// </summary>
            AddShow,
            /// <summary>
            /// Adds a show with marked episodes.
            /// </summary>
            AddShowNMark,
            /// <summary>
            /// Removes a show.
            /// </summary>
            RemoveShow,
            /// <summary>
            /// Modifies a show.
            /// </summary>
            ModifyShow,
            /// <summary>
            /// Marks an episode.
            /// </summary>
            MarkEpisode,
            /// <summary>
            /// Unmarks an episode.
            /// </summary>
            UnmarkEpisode,
        }
    }
}