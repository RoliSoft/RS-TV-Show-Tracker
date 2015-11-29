using System.Globalization;
using System.Linq;

namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
    using NUnit.Framework;

    /// <summary>
    /// Provides support for using the official TVmaze API.
    /// </summary>
    [TestFixture]
    public class TVmaze : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "TVmaze"; }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get { return "http://www.tvmaze.com/"; }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get { return "https://github.com/drauch/"; }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get { return Utils.DateTimeToVersion("2015-11-29 6:00 PM"); }
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var result = Utils.GetJSON("http://api.tvmaze.com/search/shows?q=" + Utils.EncodeURL(name));

            foreach (var entry in result)
            {
                var show = entry["show"];

                var id = new ShowID(this);

                id.ID = ((int) show["id"]).ToString();
                id.URL = show["url"];
                id.Title = show["name"] + (show["premiered"] != null ? " (" + (show["premiered"]).Substring(0, 4) + ")" : string.Empty);
                id.Language = show["language"];

                if (show["image"] != null)
                {
                    if (show["image"]["original"] != null)
                        id.Cover = show["image"]["original"];
                    else if (show["image"]["medium"] != null)
                        id.Cover = show["image"]["medium"];
                }

                yield return id;
            }
        }

        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>TV show data.</returns>
        public override TVShow GetData(string id, string language = "en")
        {
            var main = Utils.GetJSON("http://api.tvmaze.com/shows/" + id + "?embed=episodes");
            var show = new TVShow();

            show.Title = main["name"];
            show.Source = GetType().Name;
            show.SourceID = id;
            show.Description = main["summary"];
            show.Airing = main["status"] == "Running";
            show.Runtime = main["runtime"];
            show.Network = (main["network"] == null || main["network"].Count == 0) ? null : (string)main["network"][0]["name"];
            show.Language = main["language"];
            show.URL = main["url"];
            show.Episodes = new List<Episode>();

            if (main["image"] != null && main["image"]["original"] != null)
            {
                show.Cover = main["image"]["original"];
            }
            else if (main["image"] != null && main["image"]["medium"] != null)
            {
                show.Cover = main["image"]["medium"];
            }

            if(main["genres"] != null)
                show.Genre = string.Join(", ", main["genres"]);

            if (main["_embedded"] != null && main["_embedded"]["episodes"] != null && main["_embedded"]["episodes"].Count > 0)
            {
                foreach (var episode in main["_embedded"]["episodes"])
                {
                    var ep = new Episode();
                    ep.Show = show;

                    ep.Season = (int) episode["season"];
                    ep.Number = (int) episode["number"];
                    ep.Title = (string) episode["name"];
                    ep.Summary = (string) episode["summary"];
                    ep.URL = (string) episode["url"];

                    if (episode["image"] != null && episode["image"]["original"] != null)
                    {
                        ep.Picture = main["image"]["original"];
                    }
                    else if (episode["image"] != null && episode["image"]["medium"] != null)
                    {
                        ep.Picture = main["image"]["medium"];
                    }

                    DateTime dt;
                    ep.Airdate = DateTime.TryParseExact((string) episode["airdate"] ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out dt)
                        ? dt
                        : Utils.UnixEpoch;

                    show.Episodes.Add(ep);
                }
            }

            if (show.Episodes.Count != 0)
            {
                show.AirDay = show.Episodes.Last().Airdate.DayOfWeek.ToString();
            }

            return show;
        }

        [Test]
        public void Test_GetID_SingleResult()
        {
            var guide = new TVmaze();
            var results = guide.GetID("american dad").ToList();

            Assert.That(results.Count, Is.EqualTo(1));
            var show = results.Single();
            Assert.That(show.ID, Is.EqualTo("215"));
            Assert.That(show.URL, Is.EqualTo("http://www.tvmaze.com/shows/215/american-dad"));
            Assert.That(show.Title, Is.EqualTo("American Dad"));
            Assert.That(show.Language, Is.EqualTo("English"));
            Assert.That(show.Cover, Is.EqualTo("http://tvmazecdn.com/uploads/images/original_untouched/1/3570.jpg"));
        }

        [Test]
        public void Test_GetID_MultipleResults()
        {
            var guide = new TVmaze();
            var results = guide.GetID("girls");

            Assert.That(results.Count(), Is.GreaterThan(1));
        }

        [Test]
        public void Test_GetID_NoResults()
        {
            var guide = new TVmaze();
            var results = guide.GetID("xxyyzz");

            Assert.That(results.Count(), Is.EqualTo(0));
        }

        [Test]
        public void Test_GetData()
        {
            var guide = new TVmaze();
            var result = guide.GetData("1");

            Assert.That(result.Title, Is.EqualTo("Under the Dome"));
            Assert.That(result.Source, Is.EqualTo("TVmaze"));
            Assert.That(result.SourceID, Is.EqualTo("1"));
            Assert.That(result.Description, Is.StringStarting("<p>Under the Dome is"));
            Assert.That(result.Airing, Is.EqualTo(false));
            Assert.That(result.Runtime, Is.EqualTo(60));
            Assert.That(result.Network, Is.EqualTo("CBS"));
            Assert.That(result.Language, Is.EqualTo("English"));
            Assert.That(result.URL, Is.EqualTo("http://www.tvmaze.com/shows/1/under-the-dome"));
            Assert.That(result.Cover, Is.EqualTo("http://tvmazecdn.com/uploads/images/original_untouched/0/1.jpg"));
            Assert.That(result.Genre, Is.EqualTo("Drama, Science-Fiction, Thriller"));
            Assert.That(result.AirDay, Is.EqualTo("Thursday"));

            Assert.That(result.Episodes.Count, Is.EqualTo(39));
            var firstEpisode = result.Episodes[0];

            Assert.That(firstEpisode.Show.ID, Is.EqualTo("1"));
            Assert.That(firstEpisode.Season, Is.EqualTo(1));
            Assert.That(firstEpisode.Number, Is.EqualTo(1));
            Assert.That(firstEpisode.Title, Is.EqualTo("Pilot"));
            Assert.That(firstEpisode.Summary, Is.StringStarting("<p>When the residents"));
            Assert.That(firstEpisode.URL, Is.EqualTo("http://www.tvmaze.com/episodes/1/under-the-dome-1x01-pilot"));
            Assert.That(firstEpisode.Picture, Is.EqualTo("http://tvmazecdn.com/uploads/images/original_untouched/1/4388.jpg"));
            Assert.That(firstEpisode.Airdate, Is.EqualTo(new DateTime(2013, 5, 24)));
        }
    }
}