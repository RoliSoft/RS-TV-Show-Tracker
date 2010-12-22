namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CookComputing.XmlRpc;

    /// <summary>
    /// Provides support for the OpenSubtitles XML-RPC API.
    /// </summary>
    public class OpenSubtitles : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "OpenSubtitles";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "http://static.opensubtitles.org/favicon.ico";
            }
        }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        /// <exception cref="Exception">XML-RPC failed.</exception>
        public override List<Subtitle> Search(string query)
        {
            var svc   = XmlRpcProxyGen.Create<IOpenSubtitles>();
            var login = svc.LogIn(string.Empty, string.Empty, "en", "RS TV Show Tracker 1.0");

            if (login["status"].ToString() == "401 Unauthorized")
            {
                throw new Exception("Login unauthorized.");
            }
            if (login["status"].ToString() == "407 Download limit reached")
            {
                throw new Exception("Download limit reached.");
            }

            var search = svc.SearchSubtitles(login["token"].ToString(), new[] { new { query } });

            if (ReferenceEquals(search["data"].GetType(), typeof(bool)))
            {
                return null;
            }

            return (search["data"] as object[])
                   .Cast<XmlRpcStruct>()
                   .Where(data => ShowNames.IsMatch(query, data["SubFileName"].ToString()))
                   .Select(data => new Subtitle
                   {
                       Site     = Name,
                       Release  = data["SubFileName"].ToString().Replace("." + data["SubFormat"], String.Empty),
                       Language = Addic7ed.ParseLanguage(data["LanguageName"].ToString()),
                       URL      = data["ZipDownloadLink"].ToString()
                   })
                   .ToList();
        }

        /// <summary>
        /// Interface for the OpenSubtitles XML-RPC API.
        /// </summary>
        [XmlRpcUrl("http://api.opensubtitles.org/xml-rpc")]
        public interface IOpenSubtitles : IXmlRpcProxy
        {
            /// <summary>
            /// Logs in into the service.
            /// </summary>
            /// <param name="username">The optional username.</param>
            /// <param name="password">The optional password.</param>
            /// <param name="language">The language to display the messages in.</param>
            /// <param name="useragent">The useragent of the software.</param>
            /// <returns>Token for later use.</returns>
            [XmlRpcMethod("LogIn")]
            XmlRpcStruct LogIn(string username, string password, string language, string useragent);

            /// <summary>
            /// Searches for the specified subtitle.
            /// </summary>
            /// <param name="token">The token.</param>
            /// <param name="subs">The searching criterias.</param>
            /// <returns>List of subtitles.</returns>
            [XmlRpcMethod("SearchSubtitles")]
            XmlRpcStruct SearchSubtitles(string token, object[] subs);

            /// <summary>
            /// Logs out from the service.
            /// </summary>
            /// <param name="token">The token.</param>
            /// <returns>Useless stuff.</returns>
            [XmlRpcMethod("LogOut")]
            XmlRpcStruct LogOut(string token);
        }
    }
}
