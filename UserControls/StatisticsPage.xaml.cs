namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for StatisticsPage.xaml
    /// </summary>
    public partial class StatisticsPage : UserControl, IRefreshable
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
        /// Represents a TV show on the statistics list view.
        /// </summary>
        public class StatisticsListViewItem
        {
            public string Name { get; set; }
            public string Runtime { get; set; }
            public string Episodes { get; set; }
            public string TimeWasted { get; set; }
        }

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
            /*
Tvshows
    .OrderBy(c => c.Rowid)
    .Select(c => new {
        Name = c.Name,
        Runtime = Showdata
            .Single(s => s.Showid == c.Showid && s.Key == "runtime").Value,
        Count = Tracking
            .Where(t => t.Showid == c.Showid)
            .Count()
    })
             */
            foreach (var show in shows)
            {
                episodes += int.Parse(show["count"]);
                minutes  += TimeSpan.FromMinutes(double.Parse(show["runtime"]) * double.Parse(show["count"]));

                StatisticsListViewItemCollection.Add(new StatisticsListViewItem
                    {
                        Name       = show["name"],
                        Runtime    = show["runtime"] + " minutes",
                        Episodes   = int.Parse(show["count"]).ToString("#,###"),
                        TimeWasted = TimeSpan.FromMinutes(double.Parse(show["runtime"]) * double.Parse(show["count"])).ToFullRelativeTime()
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
