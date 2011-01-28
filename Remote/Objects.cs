namespace RoliSoft.TVShowTracker.Remote.Objects
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

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

        public bool New { get; set; }
        public string Version { get; set; }
        public string URL { get; set; }
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
    public class ShowInfo : IRemoteObject
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

        public string Title { get; set; }
        public string Description { get; set; }
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
        public long LastUpdated { get; set; }
    }

    /// <summary>
    /// Represents a dictionary of show informations.
    /// </summary>
    public class MultipleShowInfo : Dictionary<string, ShowInfo>, IRemoteObject
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
}