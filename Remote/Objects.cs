namespace RoliSoft.TVShowTracker.Remote.Objects
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Represents a request.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the name of the function.
        /// </summary>
        /// <value>The function name.</value>
        [JsonProperty("func")]
        public string Function { get; set; }

        /// <summary>
        /// Gets or sets the arguments of the function.
        /// </summary>
        /// <value>The arguments.</value>
        [JsonProperty("args")]
        public object[] Arguments { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Request"/> class.
        /// </summary>
        /// <param name="func">The function name.</param>
        /// <param name="args">The arguments.</param>
        public Request(string func, params object[] args)
        {
            Function  = func;
            Arguments = args;
        }
    }

    /// <summary>
    /// Represents a remote object.
    /// </summary>
    public interface IRemoteObject
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request was successfully fulfilled.
        /// </summary>
        /// <value><c>true</c> if request was successful; otherwise, <c>false</c>.</value>
        bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds it took for the request to finish.
        /// </summary>
        /// <value>The number of seconds.</value>
        double Time { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        /// <value>The error message.</value>
        string Error { get; set; }
    }

    /// <summary>
    /// Represents a simple boolean answer.
    /// </summary>
    public class General : IRemoteObject
    {
        #region Implementation of IRemoteObject
        /// <summary>
        /// Gets or sets a value indicating whether the request was successfully fulfilled.
        /// </summary>
        /// <value><c>true</c> if request was successful; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds it took for the request to finish.
        /// </summary>
        /// <value>The number of seconds.</value>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        /// <value>The error message.</value>
        public string Error { get; set; }
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether the operation successfully completed on the remote server.
        /// This is not the same with <c>Success</c>, because that shows whether the API request failed or not.
        /// </summary>
        /// <value><c>true</c> if OK; otherwise, <c>false</c>.</value>
        public bool OK { get; set; }
    }

    /// <summary>
    /// Represents a generic answer, where the <c>Result</c> field is casted to <c>T</c>.
    /// </summary>
    public class Generic<T> : IRemoteObject
    {
        #region Implementation of IRemoteObject
        /// <summary>
        /// Gets or sets a value indicating whether the request was successfully fulfilled.
        /// </summary>
        /// <value><c>true</c> if request was successful; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds it took for the request to finish.
        /// </summary>
        /// <value>The number of seconds.</value>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        /// <value>The error message.</value>
        public string Error { get; set; }
        #endregion

        /// <summary>
        /// Gets or sets the result of the operation casted to type <c>T</c>.
        /// </summary>
        /// <value>The result of the request.</value>
        public T Result { get; set; }
    }

    /// <summary>
    /// Represents a key exchange response containing a public key.
    /// </summary>
    public class KeyExchange : IRemoteObject
    {
        #region Implementation of IRemoteObject
        /// <summary>
        /// Gets or sets a value indicating whether the request was successfully fulfilled.
        /// </summary>
        /// <value><c>true</c> if request was successful; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds it took for the request to finish.
        /// </summary>
        /// <value>The number of seconds.</value>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        /// <value>The error message.</value>
        public string Error { get; set; }
        #endregion

        /// <summary>
        /// Gets or sets the remote public key.
        /// </summary>
        /// <value>The remote public key.</value>
        public string PublicKey { get; set; }
    }

    /// <summary>
    /// Represents an update check answer.
    /// </summary>
    public class UpdateCheck : IRemoteObject
    {
        #region Implementation of IRemoteObject
        /// <summary>
        /// Gets or sets a value indicating whether the request was successfully fulfilled.
        /// </summary>
        /// <value><c>true</c> if request was successful; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds it took for the request to finish.
        /// </summary>
        /// <value>The number of seconds.</value>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        /// <value>The error message.</value>
        public string Error { get; set; }
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether there is a new version.
        /// </summary>
        /// <value><c>true</c> if there is; otherwise, <c>false</c>.</value>
        public bool New { get; set; }

        /// <summary>
        /// Gets or sets the new version.
        /// </summary>
        /// <value>The new version.</value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get; set; }

        /// <summary>
        /// Gets or sets the change list.
        /// </summary>
        /// <value>The change list.</value>
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents a list of known TV shows.
    /// </summary>
    public class ListOfShows : List<string>, IRemoteObject
    {
        #region Implementation of IRemoteObject
        /// <summary>
        /// Gets or sets a value indicating whether the request was successfully fulfilled.
        /// </summary>
        /// <value><c>true</c> if request was successful; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds it took for the request to finish.
        /// </summary>
        /// <value>The number of seconds.</value>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        /// <value>The error message.</value>
        public string Error { get; set; }
        #endregion
    }

    /// <summary>
    /// Represents a show information.
    /// </summary>
    public class ShowInfo
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tagline { get; set; }
        public IEnumerable<string> Genre { get; set; }
        public string Cover { get; set; }
        public long Started { get; set; }
        public bool Airing { get; set; }
        public string AirTime { get; set; }
        public string AirDay { get; set; }
        public string Network { get; set; }
        public int Runtime { get; set; }
        public int Seasons { get; set; }
        public int Episodes { get; set; }
        public string Source { get; set; }
        public string SourceID { get; set; }
    }

    /// <summary>
    /// Represents a received show information.
    /// </summary>
    public class RemoteShowInfo : ShowInfo, IRemoteObject
    {
        #region Implementation of IRemoteObject
        /// <summary>
        /// Gets or sets a value indicating whether the request was successfully fulfilled.
        /// </summary>
        /// <value><c>true</c> if request was successful; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds it took for the request to finish.
        /// </summary>
        /// <value>The number of seconds.</value>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        /// <value>The error message.</value>
        public string Error { get; set; }
        #endregion

        /// <summary>
        /// Gets or sets the date of the last update.
        /// </summary>
        /// <value>The last updated date.</value>
        public long LastUpdated { get; set; }
    }

    /// <summary>
    /// Represents a dictionary of show informations.
    /// </summary>
    public class MultipleShowInfo : Dictionary<string, RemoteShowInfo>, IRemoteObject
    {
        #region Implementation of IRemoteObject
        /// <summary>
        /// Gets or sets a value indicating whether the request was successfully fulfilled.
        /// </summary>
        /// <value><c>true</c> if request was successful; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds it took for the request to finish.
        /// </summary>
        /// <value>The number of seconds.</value>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        /// <value>The error message.</value>
        public string Error { get; set; }
        #endregion
    }

    /// <summary>
    /// Represents a serialized show information including its marked episodes.
    /// </summary>
    public class SerializedShowInfo
    {
        /// <summary>
        /// Gets or sets the row ID.
        /// </summary>
        /// <value>The row ID.</value>
        public int? RowID { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the source ID.
        /// </summary>
        /// <value>The source ID.</value>
        public string SourceID { get; set; }

        /// <summary>
        /// Gets or sets the source language.
        /// </summary>
        /// <value>The source language.</value>
        public string SourceLanguage { get; set; }

        /// <summary>
        /// Gets or sets the list of marked episodes.
        /// </summary>
        /// <value>The list of marked episodes.</value>
        public List<int[]> MarkedEpisodes { get; set; }
    }

    /// <summary>
    /// Represents a show information change.
    /// </summary>
    [Serializable]
    public class ShowInfoChange
    {
        /// <summary>
        /// Gets or sets the show.
        /// </summary>
        /// <value>An array with 4 items: 1 - title of the show; 2 - grabber name; 3 - language; 4 - ID on the grabber.</value>
        public string Show { get; set; }

        /// <summary>
        /// Gets or sets the GMT unix timestamp which indicates when did this change occur.
        /// </summary>
        /// <value>The GMT unix timestamp.</value>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets the data which contains the changed information.
        /// </summary>
        /// <value>The changed information.</value>
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets the type of the change.
        /// </summary>
        /// <value>The type of the change.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeType Change { get; set; }

        /// <summary>
        /// Describes the type of the change.
        /// </summary>
        public enum ChangeType
        {
            /// <summary>
            /// Adds a show.
            /// </summary>
            AddShow,
            /// <summary>
            /// Removes a show.
            /// </summary>
            RemoveShow,
            /// <summary>
            /// Modifies a show.
            /// </summary>
            ModifyShow,
            /// <summary>
            /// Marks an episode.
            /// </summary>
            MarkEpisode,
            /// <summary>
            /// Unmarks an episode.
            /// </summary>
            UnmarkEpisode,
            /// <summary>
            /// Reorders the list.
            /// </summary>
            ReorderList
        }
    }

    /// <summary>
    /// Represents a list of show changes since the last request.
    /// </summary>
    public class ShowInfoChangeList : IRemoteObject
    {
        #region Implementation of IRemoteObject
        /// <summary>
        /// Gets or sets a value indicating whether the request was successfully fulfilled.
        /// </summary>
        /// <value><c>true</c> if request was successful; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds it took for the request to finish.
        /// </summary>
        /// <value>The number of seconds.</value>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        /// <value>The error message.</value>
        public string Error { get; set; }
        #endregion

        /// <summary>
        /// Gets or sets the date of the last synchronization.
        /// </summary>
        /// <value>The last synchronization date.</value>
        public long LastSync { get; set; }

        /// <summary>
        /// Gets or sets the change list.
        /// </summary>
        /// <value>The change list.</value>
        public List<ShowInfoChange> Changes { get; set; }
    }
}