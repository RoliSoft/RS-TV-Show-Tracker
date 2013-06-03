namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    using TaskDialogInterop;

    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.Parsers.Guides.Engines;
    using RoliSoft.TVShowTracker.TaskDialogs;
    using RoliSoft.TVShowTracker.Dependencies.GreyableImage;

    /// <summary>
    /// Interaction logic for EditShowWindow.xaml
    /// </summary>
    public partial class EditShowWindow
    {
        private Guide _guide;
        private int _id, _sidx;
        private string _show, _lang, _not, _newSource, _newSID, _newLang;
        private bool _editWarn, _upReq, _loaded, _change;

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
            if (!string.IsNullOrWhiteSpace(release.Data.Get("regex")))
            {
                customReleaseName.IsChecked = releaseTextBox.IsEnabled = true;
                releaseTextBox.Text = release.Data.Get("regex");
            }
            else
            {
                customReleaseName.IsChecked = releaseTextBox.IsEnabled = false;
                releaseTextBox.Text = ShowNames.Parser.GenerateTitleRegex(show, release).ToString();
            }

            if (!string.IsNullOrWhiteSpace(release.Data.Get("scene")))
            {
                customSceneName.IsChecked = sceneTextBox.IsEnabled = true;
                sceneTextBox.Text = release.Data.Get("scene");
            }
            else
            {
                customSceneName.IsChecked = sceneTextBox.IsEnabled = false;
                sceneTextBox.Text = ShowNames.Regexes.Exclusions.ContainsKey(show) ? ShowNames.Regexes.Exclusions[show] : show;
            }

            switch (Database.TVShows[_id].Source)
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

                case "AnimeNewsNetwork":
                    database.SelectedIndex = 7;
                    break;

                case "EPGuides":
                    database.SelectedIndex = 10;
                    break;
            }

            _sidx = database.SelectedIndex;

            _guide = CreateGuide((((database.SelectedValue as ComboBoxItem).Content as StackPanel).Children[1] as Label).Content.ToString().Trim());
            _lang  = Database.TVShows[_id].Language;

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

            switch (_not = FileNames.Parser.GetEpisodeNotationType(_id))
            {
                default:
                case "standard":
                    standardRadioButton.IsChecked = true;
                    break;

                case "airdate":
                    dateRadioButton.IsChecked = true;

                    if (FileNames.Parser.AirdateNotationShows.Contains(Utils.CreateSlug(show)))
                    {
                        standardRadioButton.IsEnabled = false;
                    }
                    break;
            }
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

            _loaded = true;
        }

        /// <summary>
        /// Handles the GotFocus event of the nameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void NameTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (!_editWarn)
            {
                _editWarn = true;
                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon        = VistaTaskDialogIcon.Warning,
                        Title           = "Possible name mismatch after edit",
                        MainInstruction = "Possible name mismatch after edit",
                        Content         = "Don't edit the name for other purposes than capitalization, punctuation, country and year notations!\r\n\r\nIf you alter the words in the title, the software will use the new title to search for files, download links, subtitles, etc, and since you've altered the words, it might not find anything.",
                        CustomButtons   = new[] { "OK" }
                    });
            }
        }

        /// <summary>
        /// Handles the Checked event of the standardRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void StandardRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            _not = "standard";
        }

        /// <summary>
        /// Handles the Checked event of the dateRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DateRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            _not = "airdate";
        }

        /// <summary>
        /// Handles the Checked event of the customReleaseName control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CustomReleaseNameChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            releaseTextBox.IsEnabled = true;
            releaseTextBox.Text = Database.GetReleaseName(_show).ToString();
        }

        /// <summary>
        /// Handles the Unchecked event of the customReleaseName control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CustomReleaseNameUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            releaseTextBox.IsEnabled = false;
            releaseTextBox.Text = ShowNames.Parser.GenerateTitleRegex(_show, Database.TVShows[_id]).ToString();
        }

        /// <summary>
        /// Handles the Checked event of the customSceneName control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CustomSceneNameChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            sceneTextBox.IsEnabled = true;
            sceneTextBox.Text = Database.TVShows[_id].Data.Get("scene") ?? (ShowNames.Regexes.Exclusions.ContainsKey(_show) ? ShowNames.Regexes.Exclusions[_show] : _show);
        }

        /// <summary>
        /// Handles the Unchecked event of the customSceneName control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CustomSceneNameUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            sceneTextBox.IsEnabled = false;
            sceneTextBox.Text = ShowNames.Regexes.Exclusions.ContainsKey(_show) ? ShowNames.Regexes.Exclusions[_show] : _show;
        }

        /// <summary>
        /// Handles the OnSelectionChanged event of the database control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DatabaseOnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            searchButton.IsEnabled = _sidx != database.SelectedIndex;

            var guide = CreateGuide((((database.SelectedValue as ComboBoxItem).Content as StackPanel).Children[1] as Label).Content.ToString().Trim());
            var sel   = 0;

            language.Items.Clear();

            foreach (var lang in guide.SupportedLanguages)
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

                //((Image)sp.Children[0]).SetValue(ImageGreyer.IsGreyableProperty, true); // very slow

                language.Items.Add(sp);

                if (lang == _lang)
                {
                    sel = language.Items.Count - 1;
                }
            }

            language.SelectedIndex = sel;
        }

        /// <summary>
        /// Handles the Click event of the searchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (!_loaded || _sidx == database.SelectedIndex)
            {
                return;
            }

            database.IsEnabled = language.IsEnabled = searchButton.IsEnabled = saveButton.IsEnabled = cancelButton.IsEnabled = nameTextBox.IsEnabled = false;

            Guide guide;
            switch (database.SelectedIndex)
            {
                case 1:
                    guide = new TVRage();
                    break;

                case 2:
                    guide = new TVDB();
                    break;

                case 3:
                    guide = new TVcom();
                    break;

                case 4:
                    guide = new EPisodeWorld();
                    break;

                case 5:
                    guide = new IMDb();
                    break;

                case 6:
                    guide = new AniDB();
                    break;

                case 7:
                    guide = new AnimeNewsNetwork();
                    break;

                case 9:
                    guide = new EPGuides();
                    break;

                case 10:
                    guide = new EPGuides();
                    break;

                default:
                    database.IsEnabled = language.IsEnabled = searchButton.IsEnabled = saveButton.IsEnabled = cancelButton.IsEnabled = nameTextBox.IsEnabled = true;
                    return;
            }

            new ShowGuideTaskDialog().Search(guide, nameTextBox.Text, (language.SelectedItem as StackPanel).Tag as string, (id, title, lang) =>
                {
                    if (id == null)
                    {
                        Dispatcher.Invoke((Action)(() =>
                            {
                                switch (Database.TVShows[_id].Source)
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

                                    case "AnimeNewsNetwork":
                                        database.SelectedIndex = 7;
                                        break;

                                    case "EPGuides":
                                        database.SelectedIndex = 10;
                                        break;
                                }

                                database.IsEnabled = language.IsEnabled = searchButton.IsEnabled = saveButton.IsEnabled = cancelButton.IsEnabled = nameTextBox.IsEnabled = true;
                            }));

                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon        = VistaTaskDialogIcon.Error,
                                Title           = "Not found",
                                MainInstruction = _show,
                                Content         = "The currently set show title was not found on the selected guide.\r\nYour selection will be reverted. Try editing the official title.",
                                CustomButtons   = new[] { "OK" }
                            });
                    }
                    else
                    {
                        _newSource = guide.GetType().Name;
                        _newSID    = id;
                        _newLang   = lang;
                        
                        _change = _upReq = true;

                        Dispatcher.Invoke((Action)(() =>
                            {
                                nameTextBox.Text = title;
                                _sidx = database.SelectedIndex;

                                database.IsEnabled = language.IsEnabled = saveButton.IsEnabled = cancelButton.IsEnabled = nameTextBox.IsEnabled = true;
                            }));

                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon        = VistaTaskDialogIcon.Information,
                                Title           = "Modified",
                                MainInstruction = _show,
                                Content         = "The guide was successfully changed!\r\nAn update will be initiated to this TV show after you click Save.",
                                CustomButtons   = new[] { "OK" }
                            });
                    }
                });
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
            try
            {
                new Regex(releaseTextBox.Text.Trim());
            }
            catch (Exception ex)
            {
                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon        = VistaTaskDialogIcon.Warning,
                        Title           = "Invalid regular expression",
                        MainInstruction = "Invalid regular expression",
                        Content         = ex.Message.ToUppercaseFirst(),
                        CustomButtons   = new[] { "OK" }
                    });
                return;
            }

            if (_change)
            {
                Database.TVShows[_id].Source   = _newSource;
                Database.TVShows[_id].SourceID = _newSID;
                Database.TVShows[_id].Language = _newLang;
            }

            if (nameTextBox.Text != _show && !string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                Database.TVShows[_id].Title = nameTextBox.Text.Trim();
            }

            if (customSceneName.IsChecked.Value && !string.IsNullOrWhiteSpace(sceneTextBox.Text) && sceneTextBox.Text.Trim() != _show)
            {
                var rel = Regex.Replace(sceneTextBox.Text.Trim(), @"\s+", " ");
                Database.TVShows[_id].Data["scene"] = rel;
            }
            else
            {
                Database.TVShows[_id].Data.Remove("scene");
            }

            if (customReleaseName.IsChecked.Value && !string.IsNullOrWhiteSpace(releaseTextBox.Text))
            {
                var rel = Regex.Replace(releaseTextBox.Text.Trim(), @"\s+", " ");
                Database.TVShows[_id].Data["regex"] = rel;
            }
            else
            {
                Database.TVShows[_id].Data.Remove("regex");
            }

            if (Database.TVShows[_id].Language != (language.SelectedItem as StackPanel).Tag as string)
            {
                Database.TVShows[_id].Language = (language.SelectedItem as StackPanel).Tag as string;

                _upReq = true;
            }

            Database.TVShows[_id].Data["notation"] = _not;

            Database.TVShows[_id].SaveData();

            MainWindow.Active.DataChanged();

            if (_upReq)
            {
                MainWindow.Active.activeGuidesPage.ShowGeneralUpdateMouseLeftButtonUp(null, null);
            }

            DialogResult = true;
        }

        /// <summary>
        /// Creates the guide based on the ComboBox item name.
        /// </summary>
        /// <param name="grabber">The grabber.</param>
        /// <returns>The guide.</returns>
        public static Guide CreateGuide(string grabber)
        {
            switch (grabber)
            {
                default:
                case "TVRage":
                    return new TVRage();

                case "The TVDB":
                    return new TVDB();

                case "TV.com":
                    return new TVcom();

                case "EPisodeWorld":
                    return new EPisodeWorld();

                case "IMDb":
                    return new IMDb();

                case "EPGuides - TVRage":
                    return new EPGuides("tvrage.com");

                case "EPGuides - TV.com":
                    return new EPGuides("tv.com");

                case "AniDB":
                    return new AniDB();

                case "Anime News Network":
                    return new AnimeNewsNetwork();
            }
        }
    }
}
