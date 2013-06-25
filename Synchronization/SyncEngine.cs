namespace RoliSoft.TVShowTracker.Synchronization
{
    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Represents a synchronization engine.
    /// </summary>
    public abstract class SyncEngine
    {
        /// <summary>
        /// Adds a new TV show.
        /// </summary>
        /// <param name="show">The newly added TV show.</param>
        public abstract void AddShow(TVShow show);

        /// <summary>
        /// Modifies one or more properties of an existing TV show.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="modification">The array of modified parameters.</param>
        public abstract void ModifyShow(TVShow show, params string[] modification);

        /// <summary>
        /// Removes an existing TV show.
        /// </summary>
        /// <param name="show">The TV show to be removed.</param>
        public abstract void RemoveShow(TVShow show);

        /// <summary>
        /// Marks one or more episodes as seen.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="episodes">The list of episodes.</param>
        public abstract void MarkEpisodes(TVShow show, params int[] episodes);

        /// <summary>
        /// Marks one or more episodes as seen.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="episodes">The list of episode ranges. A range consists of two numbers from the same season.</param>
        public abstract void MarkEpisodes(TVShow show, params int[][] episodes);

        /// <summary>
        /// Unmarks one or more episodes.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="episodes">The list of episodes.</param>
        public abstract void UnmarkEpisodes(TVShow show, params int[] episodes);

        /// <summary>
        /// Unmarks one or more episodes.
        /// </summary>
        /// <param name="show">The TV show in the database.</param>
        /// <param name="episodes">The list of episode ranges. A range consists of two numbers from the same season.</param>
        public abstract void UnmarkEpisodes(TVShow show, params int[][] episodes);

        /// <summary>
        /// Serializes and sends the full database.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if sent successfully.
        /// </returns>
        public abstract bool SendDatabase();

        /// <summary>
        /// Retrieves and applies the changes which have been made to the remote database.
        /// </summary>
        /// <returns>
        /// Number of changes since last synchronization or -1 on sync failure.
        /// </returns>
        public abstract int GetRemoteChanges();
    }
}
