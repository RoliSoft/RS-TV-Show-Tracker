namespace RoliSoft.TVShowTracker.Parsers.Social.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Authentication;

    using Hammock;
    using Hammock.Web;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides support for posting status updates to Facebook.
    /// </summary>
    public class Facebook : OAuthEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Facebook";
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
                return "http://facebook.com/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/facebook.png";
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
                return Utils.DateTimeToVersion("2011-09-04 5:18 AM");
            }
        }

        /// <summary>
        /// Gets the default status format.
        /// </summary>
        public override string DefaultStatusFormat
        {
            get
            {
                return "Watching $show S$seasonE$episode - $title";
            }
        }

        /// <summary>
        /// The consumer key of the application.
        /// </summary>
        public static string ConsumerKey = "156073624477549";

        /// <summary>
        /// The consumer secret of the application.
        /// </summary>
        public static string ConsumerSecret = "50b2109fede1a08b8528f5bf7be37271";

        private RestClient _restClient;

        /// <summary>
        /// Initializes the <see cref="Facebook"/> class.
        /// </summary>
        public Facebook()
        {
            ServicePointManager.Expect100Continue = false;

            _restClient = new RestClient
                {
                    QueryHandling        = QueryHandling.AppendToParameters,
                    DecompressionMethods = DecompressionMethods.GZip,
                    UserAgent            = Signature.Software + "/" + Signature.Version,
                    FollowRedirects      = true
                };
        }

        /// <summary>
        /// Gets or sets the OAuth tokens.
        /// </summary>
        /// <value>
        /// The OAuth tokens.
        /// </value>
        public override List<string> Tokens { get; set; }

        /// <summary>
        /// Generates an URL which will be opened in the user's web browser to authorize the application.
        /// </summary>
        /// <returns>
        /// Authorization URL.
        /// </returns>
        public override string GenerateAuthorizationLink()
        {
            return "https://www.facebook.com/dialog/oauth?client_id=" + ConsumerKey + "&scope=publish_stream,offline_access&redirect_uri=https://www.facebook.com/connect/login_success.html";
        }

        /// <summary>
        /// Finishes the authorization by using the user-specified PIN.
        /// </summary>
        /// <param name="pin">The PIN.</param>
        /// <returns>
        /// List of tokens required for further communication with the server.
        /// </returns>
        public override List<string> FinishAuthorizationWithPin(string pin)
        {
            _restClient.Authority = "https://graph.facebook.com/oauth";

            var request = new RestRequest { Path = "/access_token" };
            request.AddParameter("client_id",     ConsumerKey);
            request.AddParameter("redirect_uri",  "https://www.facebook.com/connect/login_success.html");
            request.AddParameter("client_secret", ConsumerSecret);
            request.AddParameter("code",          pin);

            var response = _restClient.Request(request);
            var parsed   = Utils.ParseQueryString(response.Content);

            if (!parsed.ContainsKey("access_token"))
            {
                throw new Exception("Invalid response from server. (No tokens were returned.)");
            }

            _restClient.Authority = "https://graph.facebook.com";

            var request2 = new RestRequest { Path = "/me" };
            request2.AddParameter("access_token", parsed["access_token"]);

            var response2 = _restClient.Request(request2);
            var user      = JObject.Parse(response2.Content);

            return new List<string>
                {
                    user["id"].Value<string>(),
                    user["name"].Value<string>(),
                    user["username"].Value<string>(),
                    parsed["access_token"]
                };
        }

        /// <summary>
        /// Posts the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void PostMessage(string message)
        {
            if (Tokens == null || Tokens.Count != 4)
            {
                throw new InvalidCredentialException();
            }

            _restClient.Authority = "https://graph.facebook.com";

            var request = new RestRequest
                {
                    Path = "/me/feed",
                    Method = WebMethod.Post
                };

            request.AddParameter("access_token", Tokens[3]);
            request.AddParameter("message",      message.CutIfLonger(420));

            _restClient.Request(request);
        }
    }
}