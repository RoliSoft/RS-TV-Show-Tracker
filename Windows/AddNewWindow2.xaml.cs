namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    using Xceed.Wpf.Toolkit.Primitives;

    /// <summary>
    /// Interaction logic for AddNewWindow2.xaml
    /// </summary>
    public partial class AddNewWindow2
    {
        private static bool _loaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNewWindow"/> class.
        /// </summary>
        public AddNewWindow2()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (AeroGlassCompositionEnabled)
            {
                SetAeroGlassTransparency();
            }

            databaseCheckListBox.SelectedItems.Add(databaseCheckListBox.Items[0]);
            _loaded = true;
            databaseCheckListBox.SelectedItems.Add(databaseCheckListBox.Items[1]);
        }

        /// <summary>
        /// Handles the OnItemSelectionChanged event of the databaseCheckListBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemSelectionChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void DatabaseCheckListBoxOnItemSelectionChanged(object sender, ItemSelectionChangedEventArgs e)
        {
            if (!_loaded) return;

            var sellang = language.SelectedIndex != -1 ? (language.SelectedItem as StackPanel).Tag as string : string.Empty;
            var selidx  = 0;

            language.Items.Clear();

            if (databaseCheckListBox.SelectedItems.Count == 0)
            {
                return;
            }

            var langs = Languages.List.Keys.ToList();

            foreach (ListBoxItem item in databaseCheckListBox.SelectedItems)
            {
                langs = langs.Intersect(Updater.CreateGuide(item.Tag as string).SupportedLanguages).ToList();
            }

            foreach (var lang in langs)
            {
                if (sellang == lang)
                {
                    selidx = language.Items.Count;
                }

                var sp = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Tag         = lang
                    };

                sp.Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/flag-" + lang + ".png", UriKind.Relative)),
                        Height = 16,
                        Width  = 16,
                        Margin = new Thickness(0, 1, 0, 0)
                    });

                sp.Children.Add(new Label
                    {
                        Content = " " + Languages.List[lang],
                        Padding = new Thickness(0)
                    });

                language.Items.Add(sp);
            }

            language.SelectedIndex = selidx;
        }

        /// <summary>
        /// Handles the Click event of the dbMoveUpButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DbMoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            if (databaseCheckListBox.SelectedItems.Count == 0) return;

            var sel = databaseCheckListBox.SelectedItems[databaseCheckListBox.SelectedItems.Count - 1];
            var idx = databaseCheckListBox.Items.IndexOf(sel);

            if (idx > 0)
            {
                databaseCheckListBox.Items.Remove(sel);
                databaseCheckListBox.Items.Insert(idx - 1, sel);
            }
        }

        /// <summary>
        /// Handles the Click event of the dbMoveDownButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DbMoveDownButtonClick(object sender, RoutedEventArgs e)
        {
            if (databaseCheckListBox.SelectedItems.Count == 0) return;

            var sel = databaseCheckListBox.SelectedItems[databaseCheckListBox.SelectedItems.Count - 1];
            var idx = databaseCheckListBox.Items.IndexOf(sel);

            if (idx < databaseCheckListBox.Items.Count - 1)
            {
                databaseCheckListBox.Items.Remove(sel);
                databaseCheckListBox.Items.Insert(idx + 1, sel);
            }
        }
    }
}
