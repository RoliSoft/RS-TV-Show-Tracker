﻿namespace RoliSoft.TVShowTracker
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
                }
            }
        }

        private bool _parsed;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FileListViewItem"/> is parsed.
        /// </summary>
        /// <value><c>true</c> if parsed; otherwise, <c>false</c>.</value>
        public bool Parsed
        {
            get
            {
                return _parsed;
            }
            set
            {
                _parsed = value;

                RefreshTarget();
            }
        }

        /// <summary>
        /// Gets or sets the location of the file.
        /// </summary>
        /// <value>The location.</value>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        /// <value>The file.</value>
        public string File { get; set; }

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
                return Parsed && !string.IsNullOrWhiteSpace(Information.Show)
                       ? Utils.SanitizeFileName(RenamerWindow.GenerateName(Information))
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
    }
}
