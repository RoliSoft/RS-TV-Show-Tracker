namespace RoliSoft.TVShowTracker
{
    /// <summary>
    /// Interface to refresh the data on UserControls which implement it.
    /// </summary>
    public interface IRefreshable
    {
        /// <summary>
        /// Refreshes the data on this instance.
        /// </summary>
        void Refresh();
    }
}
