namespace RoliSoft.TVShowTracker.Parsers.Social.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Authentication;

    using Hammock;
    using Hammock.Authentication.OAuth;
    using Hammock.Web;

    /// <summary>
    /// Provides support for posting status updates to Twitter.
    /// </summary>
    public class Twitter : OAuthEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Twitter";
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
                return "http://twitter.com/";
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
                return "/RSTVShowTracker;component/Images/twitter.png";
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
        public static string ConsumerKey = "4e8qhi2hiCQXO84kijKBg";

        /// <summary>
        /// The consumer secret of the application.
        /// </summary>
        public static string ConsumerSecret = "AoUTWWKkVnHALa00M1TEoSzvIHaaWN18MKvqqX2Tiic";

        private string _tempAuthToken;
        private string _tempAuthTokenSecret;
        private RestClient _restClient;

        /// <summary>
        /// Initializes the <see cref="Twitter"/> class.
        /// </summary>
        public Twitter()
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
            _restClient.Authority   = "https://api.twitter.com/oauth";
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

            return "https://api.twitter.com/oauth/authorize?oauth_token=" + _tempAuthToken;
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
            _restClient.Authority   = "https://api.twitter.com/oauth";
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

            return new List<string>
                {
                    parsed["user_id"],
                    parsed["screen_name"],
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

            _restClient.Authority   = "http://api.twitter.com";
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