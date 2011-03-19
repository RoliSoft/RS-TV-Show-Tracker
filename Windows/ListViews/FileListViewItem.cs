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

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Enabled"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Opacity"));
                }
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
                return Processed && !string.IsNullOrWhiteSpace(Information.Show)
                       ? Utils.SanitizeFileName(Parser.FormatFileName(RenamerWindow.Format, Information))
                       : string.Empty;
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
