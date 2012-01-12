namespace RoliSoft.TVShowTracker.Tables
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

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
        /// Generates a regular expression which matches this show's name.
        /// </summary>
        /// <returns>
        /// A regular expression which matches this show's name.
        /// </returns>
        public Regex GenerateRegex()
        {
            if (!string.IsNullOrWhiteSpace(Release))
            {
                return new Regex(Release);
            }
            else
            {
                return ShowNames.Parser.GenerateTitleRegex(Name);
            }
        }

        /// <summary>
        /// Gets the foreign title.
        /// </summary>
        /// <param name="language">The ISO 639-1 code of the language.</param>
        /// <param name="askRemote">if set to <c>true</c> lab.rolisoft.net's API will be asked then a foreign title provider engine.</param>
        /// <param name="statusCallback">The method to call to report a status change.</param>
        /// <returns>
        /// Foreign title or <c>null</c>.
        /// </returns>
        public string GetForeignTitle(string language, bool askRemote = false, Action<string> statusCallback = null)
        {
            return Database.GetForeignTitle(ShowID, language, askRemote, statusCallback);
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
