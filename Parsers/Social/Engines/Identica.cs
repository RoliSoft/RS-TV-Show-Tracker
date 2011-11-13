namespace RoliSoft.TVShowTracker.Parsers.Social.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Authentication;

    using Hammock;
    using Hammock.Authentication.OAuth;
    using Hammock.Web;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides support for posting status updates to Identi.ca.
    /// </summary>
    public class Identica : OAuthEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Identi.ca";
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
                return "http://identi.ca/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/identica.png";
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
                return Utils.DateTimeToVersion("2011-09-04 2:39 AM");
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
        public static string ConsumerKey = "27a28acc5da8c9f80ca04190f372a954";

        /// <summary>
        /// The consumer secret of the application.
        /// </summary>
        public static string ConsumerSecret = "3dcd02846970bc4881312f598859f046";

        private static string _tempAuthToken;
        private static string _tempAuthTokenSecret;
        private static RestClient _restClient;

        /// <summary>
        /// Initializes the <see cref="Identica"/> class.
        /// </summary>
        public Identica()
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
            _restClient.Authority   = "https://identi.ca/api/oauth";
            _restClient.Credentials = new OAuthCredentials
                {
                    Type              = OAuthType.RequestToken,
                    SignatureMethod   = OAuthSignatureMethod.HmacSha1,
                    ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                    ConsumerKey       = ConsumerKey,
                    ConsumerSecret    = ConsumerSecret,
                    CallbackUrl       = "oob"
                };

            var response = _restClient.Request(new RestRequest { Path = "/request_token" });
            var parsed   = Utils.ParseQueryString(response.Content);

            if (!parsed.TryGetValue("oauth_token", out _tempAuthToken)
             || !parsed.TryGetValue("oauth_token_secret", out _tempAuthTokenSecret))
            {
                throw new Exception("Invalid response from server. (No tokens were returned.)");
            }

            return "https://identi.ca/api/oauth/authorize?oauth_token=" + _tempAuthToken;
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
            _restClient.Authority   = "https://identi.ca/api/oauth";
            _restClient.Credentials = new OAuthCredentials
                {
                    Type              = OAuthType.AccessToken,
                    SignatureMethod   = OAuthSignatureMethod.HmacSha1,
                    ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                    ConsumerKey       = ConsumerKey,
                    ConsumerSecret    = ConsumerSecret,
                    Token             = _tempAuthToken,
                    TokenSecret       = _tempAuthTokenSecret,
                    Verifier          = pin
                };

            var response = _restClient.Request(new RestRequest { Path = "/access_token" });
            var parsed   = Utils.ParseQueryString(response.Content);

            if (!parsed.ContainsKey("oauth_token")
             || !parsed.ContainsKey("oauth_token_secret"))
            {
                throw new Exception("Invalid response from server. (No tokens were returned.)");
            }
            
            _restClient.Authority   = "http://identi.ca/api";
            _restClient.Credentials = new OAuthCredentials
                {
                    Type              = OAuthType.ProtectedResource,
                    SignatureMethod   = OAuthSignatureMethod.HmacSha1,
                    ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                    ConsumerKey       = ConsumerKey,
                    ConsumerSecret    = ConsumerSecret,
                    Token             = parsed["oauth_token"],
                    TokenSecret       = parsed["oauth_token_secret"]
                };

            var response2 = _restClient.Request(new RestRequest { Path = "/account/verify_credentials.json" });
            var user      = JObject.Parse(response2.Content);

            return new List<string>
                {
                    user["id"].Value<int>().ToString(),
                    user["name"].Value<string>(),
                    parsed["oauth_token"],
                    parsed["oauth_token_secret"]
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

            _restClient.Authority   = "http://identi.ca/api";
            _restClient.Credentials = new OAuthCredentials
                {
                    Type              = OAuthType.ProtectedResource,
                    SignatureMethod   = OAuthSignatureMethod.HmacSha1,
                    ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                    ConsumerKey       = ConsumerKey,
                    ConsumerSecret    = ConsumerSecret,
                    Token             = Tokens[2],
                    TokenSecret       = Tokens[3]
                };

            var request = new RestRequest
                {
                    Path   = "/statuses/update.json",
                    Method = WebMethod.Post
                };

            request.AddParameter("status", message.CutIfLonger(140));

            _restClient.Request(request);
        }
    }
}