namespace RoliSoft.TVShowTracker.Tables
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a TV show in the SQLite database.
    /// </summary>
    public class TVShow
    {
        /// <summary>
        /// Gets or sets the row ID.
        /// </summary>
        /// <value>
        /// The row ID.
        /// </value>
        public int RowID { get; set; }

        /// <summary>
        /// Gets or sets the show ID.
        /// </summary>
        /// <value>
        /// The show ID.
        /// </value>
        public int ShowID { get; set; }

        /// <summary>
        /// Gets or sets the name of the show.
        /// </summary>
        /// <value>
        /// The name of the show.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the release name used by the scene.
        /// </summary>
        /// <value>
        /// The release name used by the scene.
        /// </value>
        public string Release { get; set; }

        /// <summary>
        /// Gets the key-value store associated with this TV show.
        /// </summary>
        public Dictionary<string, string> Data
        {
            get
            {
                return Database.ShowDatas[ShowID];
            }
        }

        /// <summary>
        /// Gets the episodes associated with this TV show.
        /// </summary>
        public IEnumerable<Episode> Episodes
        {
            get
            {
                return Database.Episodes.Where(ep => ep.ShowID == ShowID);
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} [{1}]", Name, ShowID);
        }
    }
}
