namespace RoliSoft.TVShowTracker.ContextMenus
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a context menu item.
    /// </summary>
    public abstract class ContextMenu<T> : IPlugin
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        /// <value>
        /// The name of the plugin.
        /// </value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the icon.
        /// </summary>
        /// <value>
        /// The icon.
        /// </value>
        public abstract string Icon { get; }

        /// <summary>
        /// Gets the menu items to show for the specified item.
        /// </summary>
        /// <param name="item">The right-clicked item.</param>
        /// <returns>
        /// List of menu items.
        /// </returns>
        public abstract IEnumerable<ContextMenuItem<T>> GetMenuItems(T item);
    }
}
