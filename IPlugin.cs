namespace RoliSoft.TVShowTracker
{
    /// <summary>
    /// Represents a plugin.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets or sets the name of the plugin.
        /// </summary>
        /// <value>
        /// The name of the plugin.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>
        /// The icon.
        /// </value>
        string Icon { get; }
    }
}
