namespace RoliSoft.TVShowTracker.Social
{
    using System.Collections.Generic;
    using System.Security.Authentication;

    using Twitterizer;

    /// <summary>
    /// Provides support for posting status updates to Twitter.
    /// </summary>
    public static class Twitter
    {
        /// <summary>
        /// The consumer key of the application.
        /// </summary>
        public static string ConsumerKey = "4e8qhi2hiCQXO84kijKBg";

        /// <summary>
        /// The consumer secret of the application.
        /// </summary>
        public static string ConsumerSecret = "AoUTWWKkVnHALa00M1TEoSzvIHaaWN18MKvqqX2Tiic";

        /// <summary>
        /// The default status format.
        /// </summary>
        public static string DefaultStatusFormat = "Watching $show S$seasonE$episode - $title";

        private static string _tempAuthToken;

        /// <summary>
        /// Checks whether the software has authorization to use Twitter.
        /// </summary>
        /// <returns></returns>
        public static bool OAuthTokensAvailable()
        {
            return Settings.Get("Twitter OAuth", new List<string>()).Count == 2;
        }

        /// <summary>
        /// Generates an URL which will be opened in the users web browser to authorize the application.
        /// </summary>
        /// <returns>
        /// Authorization URL.
        /// </returns>
        public static string GenerateAuthorizationLink()
        {
            var resp = OAuthUtility.GetRequestToken(ConsumerKey, ConsumerSecret, "oob");
            return OAuthUtility.BuildAuthorizationUri(_tempAuthToken = resp.Token).ToString();
        }

        /// <summary>
        /// Finishes the authorization by using the user-specified PIN and saves the token to the settings.
        /// </summary>
        /// <param name="pin">The PIN.</param>
        public static void FinishAuthorizationWithPin(string pin)
        {
            var resp = OAuthUtility.GetAccessToken(ConsumerKey, ConsumerSecret, _tempAuthToken, pin);
            Settings.Set("Twitter OAuth", new List<string> { resp.Token, resp.TokenSecret });
        }

        /// <summary>
        /// Posts the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// Server response.
        /// </returns>
        public static TwitterResponse<TwitterStatus> PostMessage(string message)
        {
            var accessTokens = Settings.Get<List<string>>("Twitter OAuth");
            if (accessTokens == null || accessTokens.Count != 2)
            {
                throw new InvalidCredentialException();
            }

            var tokens = new OAuthTokens
                {
                    AccessToken       = accessTokens[0],
                    AccessTokenSecret = accessTokens[1],
                    ConsumerKey       = ConsumerKey,
                    ConsumerSecret    = ConsumerSecret
                };
            
            return TwitterStatus.Update(tokens, message);
        }
    }
}