namespace RoliSoft.TVShowTracker.Parsers.News
{
    using System;

    /// <summary>
    /// Represents an article.
    /// </summary>
    public class Article
    {
        /// <summary>
        /// Gets the source of the article.
        /// </summary>
        /// <value>The site.</value>
        public FeedReaderEngine Source { get; internal set; }

        /// <summary>
        /// Gets or sets the title of the article.
        /// </summary>
        /// <value>The title</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the summary of the article.
        /// </summary>
        /// <value>The summary.</value>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the date when the article was published.
        /// </summary>
        /// <value>The publish date.</value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the URL to the article.
        /// </summary>
        /// <value>The URL.</value>
        public string Link { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Article"/> class.
        /// </summary>
        public Article()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Article"/> class.
        /// </summary>
        /// <param name="source">The source of the article.</param>
        public Article(FeedReaderEngine source)
        {
            Source = source;
        }
    }
}