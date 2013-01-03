namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Xml.Linq;
    using System.Xml.XPath;

    using Hammock;
    using Hammock.Authentication.OAuth;

    using Parsers.Guides;

    /// <summary>
    /// Provides support for searching videos on Netflix.
    /// </summary>
    public class Netflix : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Netflix";
            }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://www.netflix.com/";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>
        /// The icon location.
        /// </value>
        public override string Icon
        {
            get
            {
                return "pack://application:,,,/RSTVShowTracker;component/Images/netflix.png";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2012-03-11 3:11 AM");
            }
        }

        /// <summary>
        /// Gets a number representing where should the engine be placed in the list.
        /// </summary>
        public override float Index
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// The consumer key of the application.
        /// </summary>
        public static string ConsumerKey
        {
            get
            {
                return "3yztzxjqpvmfpmquumru99jz";
            }
        }

        /// <summary>
        /// The shared secret of the application.
        /// </summary>
        public static string SharedSecret
        {
            get
            {
                return "XYxV2ewzt9";
            }
        }

        /// <summary>
        /// Searches for videos on Netflix.
        /// </summary>
        /// <param name="ep">The episode.</param>
        /// <returns>
        /// URL of the video.
        /// </returns>
        public override string Search(Episode ep)
        {
            var rest = new RestClient
                {
                    QueryHandling        = QueryHandling.AppendToParameters,
                    DecompressionMethods = DecompressionMethods.GZip,
                    UserAgent            = Signature.Software + "/" + Signature.Version,
                    FollowRedirects      = true,
                    Authority            = "http://api.netflix.com/",
                    Credentials          = new OAuthCredentials
                        {
                            Type              = OAuthType.ProtectedResource,
                            SignatureMethod   = OAuthSignatureMethod.HmacSha1,
                            ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                            ConsumerKey       = ConsumerKey,
                            ConsumerSecret    = SharedSecret
                        }
                };

            #region Detect and set proxy
            string proxy = null;
            object proxyId;
            if (Settings.Get<Dictionary<string, object>>("Proxied Domains").TryGetValue("netflix.com", out proxyId) || Settings.Get<Dictionary<string, object>>("Proxied Domains").TryGetValue("api.netflix.com", out proxyId))
            {
                proxy = (string)Settings.Get<Dictionary<string, object>>("Proxies")[(string)proxyId];
            }

            if (proxy != null)
            {
                var proxyUri = new Uri(proxy.Replace("$domain.", string.Empty));

                switch (proxyUri.Scheme.ToLower())
                {
                    case "http":
                        if (proxy.Contains("$url") || (proxy.Contains("$domain") && proxy.Contains("$path")))
                        {
                            throw new Exception("Web-based proxies are not supported with Netflix for now, because of OAuth signatures.");
                        }
                        else
                        {
                            rest.Proxy = proxyUri.Host + ":" + proxyUri.Port;
                        }
                        break;

                    case "socks4":
                    case "socks4a":
                    case "socks5":
                        var tunnel = new HttpToSocks { RemoteProxy = HttpToSocks.Proxy.ParseUri(proxyUri) };
                        tunnel.Listen();
                        rest.Proxy = tunnel.LocalProxy.Host + ":" + tunnel.LocalProxy.Port;
                        break;
                }
            }
            #endregion

            var request = new RestRequest { Path = "catalog/titles" };
            request.AddParameter("term", ep.Show.Name);
            request.AddParameter("max_results", "1");
            request.AddParameter("expand", "seasons");

            var resp  = XDocument.Parse(rest.Request(request).Content);
            var links = resp.XPathSelectElements("//link[@rel='http://schemas.netflix.com/catalog/title.season']");

            string seasonid = null;
            foreach (var link in links)
            {
                if (link.Attribute("title").Value.EndsWith(" " + ep.Season))
                {
                    seasonid = link.Attribute("href").Value;
                    break;
                }
            }

            if (seasonid != null)
            {
                request = new RestRequest { Path = seasonid.Replace("http://api.netflix.com/", string.Empty) + "/episodes" };
                resp    = XDocument.Parse(rest.Request(request).Content);
                var ids = resp.XPathSelectElements("//id").ToList();

                if (ids.Count >= ep.Number)
                {
                    return ids[ep.Number - 1].Value.Replace("http://api.netflix.com/catalog/titles/programs/", "http://movies.netflix.com/WiPlayer?movieid=");
                }
            }

            throw new OnlineVideoNotFoundException("No matching videos were found.", "Open Netflix search page", "http://movies.netflix.com/WiSearch?v1=" + Utils.EncodeURL(ep.Show.Name));
        }
    }
}
