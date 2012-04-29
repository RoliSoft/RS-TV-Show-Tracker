namespace RoliSoft.TVShowTracker.Parsers.News
{
    using System;

    using ProtoBuf;

    /// <summary>
    /// Represents an article.
    /// </summary>
    [ProtoContract]
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
        [ProtoMember(1)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the summary of the article.
        /// </summary>
        /// <value>The summary.</value>
        [ProtoMember(2)]
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the date when the article was published.
        /// </summary>
        /// <value>The publish date.</value>
        [ProtoMember(3)]
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the URL to the article.
        /// </summary>
        /// <value>The URL.</value>
        [ProtoMember(4)]
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