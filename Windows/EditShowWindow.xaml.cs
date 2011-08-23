namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Interaction logic for EditShowWindow.xaml
    /// </summary>
    public partial class EditShowWindow
    {
        private Guide _guide;
        private int _id;
        private string _show, _lang;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNewWindow"/> class.
        /// </summary>
        public EditShowWindow(int id, string show)
        {
            InitializeComponent();

            _id   = id;
            _show = show;

            nameTextBox.Text = _show;

            var release = Database.TVShows[_id];
            if (!string.IsNullOrWhiteSpace(release.Release))
            {
                customReleaseName.IsChecked = releaseTextBox.IsEnabled = true;
                releaseTextBox.Text = string.Join(" ", release.Release).ToLower();
            }
            else
            {
                customReleaseName.IsChecked = releaseTextBox.IsEnabled = false;
                releaseTextBox.Text = string.Join(" ", ShowNames.Parser.GetRoot(show)).ToLower();
            }
            
            switch (Database.ShowData(_id, "grabber"))
            {
                case "TVRage":
                    database.SelectedIndex = 1;
                    break;

                case "TVDB":
                    database.SelectedIndex = 2;
                    break;

                case "TVcom":
                    database.SelectedIndex = 3;
                    break;

                case "EPisodeWorld":
                    database.SelectedIndex = 4;
                    break;

                case "IMDb":
                    database.SelectedIndex = 5;
                    break;

                case "AniDB":
                    database.SelectedIndex = 6;
                    break;

                case "Anime News Network":
                    database.SelectedIndex = 7;
                    break;

                case "EPGuides":
                    database.SelectedIndex = 10;
                    break;
            }

            _guide = AddNewWindow.CreateGuide((((database.SelectedValue as ComboBoxItem).Content as StackPanel).Children[1] as Label).Content.ToString().Trim());
            _lang  = Database.ShowData(_id, _guide.GetType().Name + ".lang");

            var sel = 0;

            foreach (var lang in _guide.SupportedLanguages)
            {
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
                        Margin = new Thickness(0, 0, 0, 0)
                    });

                sp.Children.Add(new Label
                    {
                        Content = " " + Languages.List[lang],
                        Padding = new Thickness(0)
                    });

                language.Items.Add(sp);

                if (lang == _lang)
                {
                    sel = language.Items.Count - 1;
                }
            }

            language.SelectedIndex = sel;
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
        }

        /// <summary>
        /// Handles the Checked event of the customReleaseName control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CustomReleaseNameChecked(object sender, RoutedEventArgs e)
        {
            releaseTextBox.IsEnabled = true;
            releaseTextBox.Text = string.Join(" ", Database.GetReleaseName(_show)).ToLower();
        }

        /// <summary>
        /// Handles the Unchecked event of the customReleaseName control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CustomReleaseNameUnchecked(object sender, RoutedEventArgs e)
        {
            releaseTextBox.IsEnabled = false;
            releaseTextBox.Text = string.Join(" ", ShowNames.Parser.GetRoot(_show)).ToLower();
        }

        /// <summary>
        /// Handles the Click event of the cancelButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the Click event of the saveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (customReleaseName.IsChecked.Value && !string.IsNullOrWhiteSpace(releaseTextBox.Text))
            {
                var rel = Regex.Replace(releaseTextBox.Text.ToUpper().Trim(), @"\s+", " ");
                Database.Execute("update tvshows set release = ? where showid = ?", rel, _id);
                Database.TVShows[_id].Release = rel;
            }
            else
            {
                Database.Execute("update tvshows set release = ? where showid = ?", string.Empty, _id);
                Database.TVShows[_id].Release = string.Empty;
            }

            Database.ShowData(_id, _guide.GetType().Name + ".lang", (language.SelectedItem as StackPanel).Tag as string);

            DialogResult = true;
        }
    }
}
