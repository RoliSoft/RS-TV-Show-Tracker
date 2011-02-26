namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;

    /// <summary>
    /// Interaction logic for StatisticsPage.xaml
    /// </summary>
    public partial class StatisticsPage : IRefreshable
    {
        /// <summary>
        /// Gets or sets the date when this control was loaded.
        /// </summary>
        /// <value>The load date.</value>
        public DateTime LoadDate { get; set; } 

        /// <summary>
        /// Gets or sets the statistics list view item collection.
        /// </summary>
        /// <value>The statistics list view item collection.</value>
        public ObservableCollection<StatisticsListViewItem> StatisticsListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatisticsPage"/> class.
        /// </summary>
        public StatisticsPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            if (StatisticsListViewItemCollection == null)
            {
                StatisticsListViewItemCollection = new ObservableCollection<StatisticsListViewItem>();
                listView.ItemsSource             = StatisticsListViewItemCollection;
            }

            if (MainWindow.Active != null && MainWindow.Active.IsActive && LoadDate < Database.DataChange)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Refreshes the data on this instance.
        /// </summary>
        public void Refresh()
        {
            LoadStatisticsListView();

            LoadDate = DateTime.Now;
        }

        /// <summary>
        /// Loads the statistics list view.
        /// </summary>
        private void LoadStatisticsListView()
        {
            StatisticsListViewItemCollection.Clear();

            var episodes = 0;
            var minutes  = new TimeSpan(0);
            var shows = Database.Query("select name, (select value from showdata where showdata.showid = tvshows.showid and key = 'runtime') as runtime, (select count(episodeid) from tracking where tracking.showid = tvshows.showid) as count from tvshows order by rowid asc");
            
            foreach (var show in shows)
            {
                episodes += show["count"].ToInteger();
                minutes  += TimeSpan.FromMinutes(show["runtime"].ToDouble() * show["count"].ToDouble());

                StatisticsListViewItemCollection.Add(new StatisticsListViewItem
                    {
                        Name       = show["name"],
                        Runtime    = show["runtime"] + " minutes",
                        Episodes   = show["count"].ToInteger().ToString("#,###"),
                        TimeWasted = TimeSpan.FromMinutes(show["runtime"].ToDouble() * show["count"].ToDouble()).ToFullRelativeTime()
                    });
            }

            StatisticsListViewItemCollection.Add(new StatisticsListViewItem
                {
                    Name       = "— Total of " + Utils.FormatNumber(shows.Count, "TV show") + " —",
                    Episodes   = episodes.ToString("#,###"),
                    TimeWasted = minutes.ToFullRelativeTime()
                });
        }
        
        /// <summary>
        /// Handles the MouseDoubleClick event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ListViewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            MainWindow.Active.tabControl.SelectedIndex = 1;
            MainWindow.Active.activeGuidesPage.LoadShowList();
            MainWindow.Active.activeGuidesPage.SelectShow(((StatisticsListViewItem)listView.SelectedValue).Name);
        }
    }
}
