namespace RoliSoft.TVShowTracker
{
    using System.ComponentModel;

    using RoliSoft.TVShowTracker.FileNames;

    /// <summary>
    /// Represents a file on the list view.
    /// </summary>
    public class FileListViewItem : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _enabled;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FileListViewItem"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }
        }

        private bool _checked;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FileListViewItem"/> is checked.
        /// </summary>
        /// <value><c>true</c> if checked; otherwise, <c>false</c>.</value>
        public bool Checked
        {
            get
            {
                return _checked;
            }
            set
            {
                _checked = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Checked"));
                }
            }
        }

        private bool _processed;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FileListViewItem"/> is processed.
        /// </summary>
        /// <value><c>true</c> if processed; otherwise, <c>false</c>.</value>
        public bool Processed
        {
            get
            {
                return _processed;
            }
            set
            {
                _processed = value;

                RefreshTarget();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FileListViewItem"/> is recognized successfully.
        /// </summary>
        /// <value><c>true</c> if recognized successfully; otherwise, <c>false</c>.</value>
        public bool Recognized { get; set; }

        /// <summary>
        /// Gets or sets the location of the file.
        /// </summary>
        /// <value>The location.</value>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the parsed information.
        /// </summary>
        /// <value>The parsed information.</value>
        public ShowFile Information { get; set; }

        /// <summary>
        /// Gets or sets the target file name.
        /// </summary>
        /// <value>The target file name.</value>
        public string Target
        {
            get
            {
                if (!Processed)
                {
                    return "[waiting to be processed]";
                }

                if (!string.IsNullOrWhiteSpace(Information.Show))
                {
                    return Utils.SanitizeFileName(Parser.FormatFileName(RenamerWindow.Format, Information));
                }

                switch (Information.ParseError)
                {
                    case ShowFile.FailureReasons.EpisodeNumberingNotFound:
                        return "[could not extract the episode numbering]";

                    case ShowFile.FailureReasons.ShowNameNotFound:
                        return "[could not extract the show name]";

                    case ShowFile.FailureReasons.ShowNotIdentified:
                        return "[could not identify the show in any databases]";

                    case ShowFile.FailureReasons.ExceptionOccurred:
                        return "[exception occurred while processing]";

                    default:
                        return "[unknown error]";
                }
            }
        }

        /// <summary>
        /// Gets the value whether to show the checkbox or not.
        /// </summary>
        /// <value>The checkbox visibility.</value>
        public string ShowCheckBox { get; set; }

        /// <summary>
        /// Gets the image of the file status.
        /// </summary>
        /// <value>The status image.</value>
        public string StatusImage { get; set; }

        /// <summary>
        /// Gets the value whether to show the status image or not.
        /// </summary>
        /// <value>The status image visibility.</value>
        public string ShowStatusImage { get; set; }

        /// <summary>
        /// Fires a <c>PropertyChanged</c> event for the enabled and opacity fields.
        /// </summary>
        public void RefreshEnabled()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Enabled"));
                PropertyChanged(this, new PropertyChangedEventArgs("Opacity"));
                PropertyChanged(this, new PropertyChangedEventArgs("StatusImage"));
                PropertyChanged(this, new PropertyChangedEventArgs("ShowStatusImage"));
                PropertyChanged(this, new PropertyChangedEventArgs("ShowCheckBox"));
            }
        }

        /// <summary>
        /// Fires a <c>PropertyChanged</c> event for the target field.
        /// </summary>
        public void RefreshTarget()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Target"));
            }
        }

        /// <summary>
        /// Gets the opacity.
        /// </summary>
        /// <value>The opacity.</value>
        public double Opacity
        {
            get
            {
                return _enabled ? 1 : 0.5;
            }
        }
    }
}
