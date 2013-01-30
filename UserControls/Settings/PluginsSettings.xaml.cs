namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Documents;

    /// <summary>
    /// Interaction logic for PluginsSettings.xaml
    /// </summary>
    public partial class PluginsSettings
    {
        /// <summary>
        /// Gets or sets the plugins list view item collection.
        /// </summary>
        /// <value>The plugins list view item collection.</value>
        public ObservableCollection<PluginsListViewItem> PluginsListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginsSettings"/> class.
        /// </summary>
        public PluginsSettings()
        {
            InitializeComponent();
        }

        private bool _loaded;

        /// <summary>
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_loaded) return;

            try
            {
                PluginsListViewItemCollection = new ObservableCollection<PluginsListViewItem>();
                pluginsListView.ItemsSource = PluginsListViewItemCollection;

                ReloadPlugins();
            }
            catch (Exception ex)
            {
                MainWindow.HandleUnexpectedException(ex);
            }

            _loaded = true;
        }

        /// <summary>
        /// Reloads the plugins list view.
        /// </summary>
        /// <param name="inclInternal">if set to <c>true</c> internal classes will be included too.</param>
        private void ReloadPlugins(bool inclInternal = false)
        {
            PluginsListViewItemCollection.Clear();

            var types = new[]
                {
                    typeof(Parsers.Guides.Guide),
                    typeof(Parsers.Downloads.DownloadSearchEngine),
                    typeof(Parsers.Subtitles.SubtitleSearchEngine),
                    typeof(Parsers.LinkCheckers.LinkCheckerEngine),
                    typeof(Parsers.OnlineVideos.OnlineVideoSearchEngine),
                    typeof(Parsers.Recommendations.RecommendationEngine), 
                    typeof(Parsers.Social.SocialEngine),
                    typeof(Parsers.WebSearch.WebSearchEngine),
                    typeof(Parsers.ForeignTitles.ForeignTitleEngine),
                    typeof(Parsers.Senders.SenderEngine),
                    typeof(Parsers.News.FeedReaderEngine),
                    typeof(ContextMenus.Menus.OverviewContextMenu),
                    typeof(ContextMenus.Menus.UpcomingListingContextMenu),
                    typeof(ContextMenus.Menus.EpisodeListingContextMenu),
                    typeof(ContextMenus.Menus.DownloadLinkContextMenu),
                    typeof(ContextMenus.Menus.SubtitleContextMenu),
                    typeof(ContextMenus.Menus.StatisticsContextMenu),
                    typeof(ContextMenus.Menus.RecommendationContextMenu),
                    typeof(LocalProgrammingPlugin),
                    typeof(Scripting.ScriptingPlugin),
                    typeof(StartupPlugin),
                    typeof(IPlugin)
                };
            
            var icons = new[]
                {
                    "/RSTVShowTracker;component/Images/guides.png",
                    "/RSTVShowTracker;component/Images/torrents.png",
                    "/RSTVShowTracker;component/Images/subtitles.png",
                    "/RSTVShowTracker;component/Images/tick.png",
                    "/RSTVShowTracker;component/Images/monitor.png",
                    "/RSTVShowTracker;component/Images/information.png",
                    "/RSTVShowTracker;component/Images/bird.png",
                    "/RSTVShowTracker;component/Images/magnifier.png",
                    "/RSTVShowTracker;component/Images/language.png",
                    "/RSTVShowTracker;component/Images/server-cast.png",
                    "/RSTVShowTracker;component/Images/feed.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/table-select-row.png",
                    "/RSTVShowTracker;component/Images/code.png",
                    "/RSTVShowTracker;component/Images/document-insert.png",
                    "/RSTVShowTracker;component/Images/dll.gif"
                };

            foreach (var engine in Extensibility.GetNewInstances<IPlugin>(inclInternal: inclInternal).OrderBy(engine => engine.Name))
            {
                var type   = engine.GetType();
                var parent = string.Empty;
                var picon  = string.Empty;
                var i      = 0;

                foreach (var ptype in types)
                {
                    if (type.IsSubclassOf(ptype))
                    {
                        parent = ptype.Name;
                        picon  = icons[i];
                        break;
                    }

                    i++;
                }

                var file = type.Assembly.ManifestModule.Name;

                if (file == "<In Memory Module>")
                {
                    var script = Extensibility.Scripts.FirstOrDefault(s => s.Type == type);

                    if (script != null)
                    {
                        file = Path.GetFileName(script.File);
                    }
                }

                PluginsListViewItemCollection.Add(new PluginsListViewItem
                    {
                        Icon    = engine.Icon,
                        Name    = engine.Name,
                        Type    = parent,
                        Icon2   = picon,
                        Version = engine.Version.ToString().PadRight(14, '0'),
                        File    = file
                    });
            }
        }

        /// <summary>
        /// Handles the Checked event of the showInternal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ShowInternalChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            ReloadPlugins(true);
        }

        /// <summary>
        /// Handles the Unchecked event of the showInternal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ShowInternalUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            ReloadPlugins();
        }

        /// <summary>
        /// Handles the Click event of the Hyperlink control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void HyperlinkClick(object sender, RoutedEventArgs e)
        {
            Utils.Run(((Hyperlink)sender).NavigateUri.ToString());
        }
    }
}
