namespace RoliSoft.TVShowTracker.Parsers.Social
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a social engine with OAuth-based authentication.
    /// </summary>
    public abstract class OAuthEngine : SocialEngine
    {
        /// <summary>
        /// Gets or sets the OAuth tokens.
        /// </summary>
        /// <value>
        /// The OAuth tokens.
        /// </value>
        public abstract List<string> Tokens { get; set; }

        /// <summary>
        /// Generates an URL which will be opened in the user's web browser to authorize the application.
        /// </summary>
        /// <returns>
        /// Authorization URL.
        /// </returns>
        public abstract string GenerateAuthorizationLink();

        /// <summary>
        /// Finishes the authorization by using the user-specified PIN.
        /// </summary>
        /// <param name="pin">The PIN.</param>
        /// <returns>
        /// List of tokens required for further communication with the server.
        /// </returns>
        public abstract List<string> FinishAuthorizationWithPin(string pin);
    }
}
