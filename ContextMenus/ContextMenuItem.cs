namespace RoliSoft.TVShowTracker.ContextMenus
{
    using System;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Represents a menu item.
    /// </summary>
    public class ContextMenuItem<T>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>
        /// The icon.
        /// </value>
        public Image Icon { get; set; }

        /// <summary>
        /// Gets or sets the method to call when clicked.
        /// </summary>
        /// <value>
        /// The method to call when clicked.
        /// </value>
        public Action<T> Click { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuItem&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="icon">The icon.</param>
        /// <param name="click">The click.</param>
        public ContextMenuItem(string name, Image icon, Action<T> click)
        {
            Name  = name;
            Icon  = icon;
            Click = click;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuItem&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="icon">The icon.</param>
        /// <param name="click">The click.</param>
        public ContextMenuItem(string name, string icon, Action<T> click)
        {
            Name  = name;
            Icon  = new Image { Source = new BitmapImage(new Uri(icon)) };
            Click = click;
        }
    }
}
