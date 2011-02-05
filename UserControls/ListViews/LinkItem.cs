namespace RoliSoft.TVShowTracker
{
    using System.ComponentModel;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Extended class of the original Link class to handle the context menu items.
    /// </summary>
    public class LinkItem : Link
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinkItem"/> class.
        /// </summary>
        /// <param name="link">The link.</param>
        public LinkItem(Link link) : base(link.Source)
        {
            // unfortunately .NET doesn't support upcasting, so we need to do it the hard way

            Release = link.Release;
            Quality = link.Quality;
            Size    = link.Size;
            URL     = link.URL;
        }

        /// <summary>
        /// Gets the image of the link type.
        /// </summary>
        /// <value>The type's image.</value>
        public string TypeImage
        {
            get
            {
                switch (Source.Type)
                {
                    case Types.Torrent:
                        return "/RSTVShowTracker;component/Images/torrent.png";

                    case Types.Usenet:
                        return "/RSTVShowTracker;component/Images/usenet.png";

                    default:
                        return "/RSTVShowTracker;component/Images/filehoster.png";
                }
            }
        }

        /// <summary>
        /// Gets the friendly name of the link quality.
        /// </summary>
        /// <value>The quality's friendly name.</value>
        public string QualityText
        {
            get
            {
                return Quality.GetAttribute<DescriptionAttribute>().Description;
            }
        }

        /// <summary>
        /// Gets a value whether to show the "Show open page" context menu item.
        /// </summary>
        /// <value>Visible or Collapsed.</value>
        public string ShowOpenPage
        {
            get
            {
                return Source.Type == Types.HTTP || Source.Downloader is ExternalDownloader ? "Visible" : "Collapsed";
            }
        }

        /// <summary>
        /// Gets a value whether to show the "Download file" context menu item.
        /// </summary>
        /// <value>Visible or Collapsed.</value>
        public string ShowDownloadFile
        {
            get
            {
                return Source.Type != Types.HTTP && !(Source.Downloader is ExternalDownloader) ? "Visible" : "Collapsed";
            }
        }

        /// <summary>
        /// Gets a value whether to show the "Send to associated application" context menu item.
        /// </summary>
        /// <value>Visible or Collapsed.</value>
        public string ShowSendToAssociated
        {
            get
            {
                return Source.Type != Types.HTTP && !(Source.Downloader is ExternalDownloader) ? "Visible" : "Collapsed";
            }
        }

        /// <summary>
        /// Gets a value whether to show the "Send to [torrent application]" context menu item.
        /// </summary>
        /// <value>Visible or Collapsed.</value>
        public string ShowSendToTorrent
        {
            get
            {
                return Source.Type == Types.Torrent && !(Source.Downloader is ExternalDownloader) && !string.IsNullOrWhiteSpace(MainWindow.Active.activeDownloadLinksPage.DefaultTorrent) ? "Visible" : "Collapsed";
            }
        }
    }
}
