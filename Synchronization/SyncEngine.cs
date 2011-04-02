namespace RoliSoft.TVShowTracker.Synchronization
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a synchronization engine.
    /// </summary>
    public abstract class SyncEngine
    {
        /// <summary>
        /// Adds a new TV show.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        public abstract void AddShow(string showid);

        /// <summary>
        /// Modified an existing TV show.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="modification">The array of modified parameters.</param>
        public abstract void ModifyShow(string showid, string[] modification);

        /// <summary>
        /// Removes an existing TV show.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        public abstract void RemoveShow(string showid);

        /// <summary>
        /// Marks one or more episodes as seen.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="episodes">The list of episodes.</param>
        public abstract void MarkEpisodes(string showid, IEnumerable<int> episodes);

        /// <summary>
        /// Marks one or more episodes as seen.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="episodes">The list of episode ranges. A range consists of two numbers from the same season.</param>
        public abstract void MarkEpisodes(string showid, IEnumerable<int[]> episodes);

        /// <summary>
        /// Unmarks one or more episodes.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="episodes">The list of episodes.</param>
        public abstract void UnmarkEpisodes(string showid, IEnumerable<int> episodes);

        /// <summary>
        /// Unmarks one or more episodes.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="episodes">The list of episode ranges. A range consists of two numbers from the same season.</param>
        public abstract void UnmarkEpisodes(string showid, IEnumerable<int[]> episodes);

        /// <summary>
        /// Sends the reordered TV show list.
        /// </summary>
        public abstract void ReorderList();

        /// <summary>
        /// Serializes and sends the full database.
        /// </summary>
        /// <returns><c>true</c> if sent successfully.</returns>
        public abstract bool SendDatabase();

        /// <summary>
        /// Retrieves and applies the changes which have been made to the remote database.
        /// </summary>
        /// <returns>Number of changes since last synchronization or -1 on sync failure.</returns>
        public abstract int GetRemoteChanges();
    }
}
