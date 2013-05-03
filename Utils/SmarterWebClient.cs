namespace RoliSoft.TVShowTracker
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides various little utility functions.
    /// </summary>
    public static partial class Utils
    {
        /// <summary>
        /// I got fucking tired of the <c>WebClient</c> class so I extended it and added support to extract file name automatically and keep track of the redirections.
        /// </summary>
        public class SmarterWebClient : WebClient
        {
            /// <summary>
            /// Gets the final URL.
            /// </summary>
            /// <value>The response URI.</value>
            public Uri ResponseUri { get; private set; }

            /// <summary>
            /// Gets the type of the content.
            /// </summary>
            /// <value>The type of the content.</value>
            public string ContentType { get; private set; }

            /// <summary>
            /// Gets the name of the file.
            /// </summary>
            /// <value>The name of the file.</value>
            public string FileName
            {
                get
                {
                    if (ResponseUri == null)
                    {
                        return string.Empty;
                    }

                    // try to get the file name from Content-Disposition
                    if (ResponseHeaders["Content-Disposition"] != null)
                    {
                        var m = Regex.Match(ResponseHeaders["Content-Disposition"], @"filename=[""']?([^$;]+)", RegexOptions.IgnoreCase);

                        if (m.Success)
                        {
                            return SanitizeFileName(m.Groups[1].Value.TrimEnd(new[] { ' ', '\'', '"' }));
                        }
                    }

                    // try to get the file name from the last URL
                    return SanitizeFileName(new FileInfo(ResponseUri.LocalPath).Name);
                }
            }

            /// <summary>
            /// Returns the <see cref="T:System.Net.WebResponse"/> for the specified <see cref="T:System.Net.WebRequest"/>.
            /// </summary>
            /// <param name="request">A <see cref="T:System.Net.WebRequest"/> that is used to obtain the response.</param>
            /// <returns>
            /// A <see cref="T:System.Net.WebResponse"/> containing the response for the specified <see cref="T:System.Net.WebRequest"/>.
            /// </returns>
            protected override WebResponse GetWebResponse(WebRequest request)
            {
                var response = base.GetWebResponse(request);
                if (response != null)
                {
                    this.ResponseUri = response.ResponseUri;
                    this.ContentType = response.ContentType;
                }
                return response;
            }

            /// <summary>
            /// Returns the <see cref="T:System.Net.WebResponse"/> for the specified <see cref="T:System.Net.WebRequest"/> using the specified <see cref="T:System.IAsyncResult"/>.
            /// </summary>
            /// <param name="request">A <see cref="T:System.Net.WebRequest"/> that is used to obtain the response.</param>
            /// <param name="result">An <see cref="T:System.IAsyncResult"/> object obtained from a previous call to <see cref="M:System.Net.WebRequest.BeginGetResponse(System.AsyncCallback,System.Object)"/> .</param>
            /// <returns>
            /// A <see cref="T:System.Net.WebResponse"/> containing the response for the specified <see cref="T:System.Net.WebRequest"/>.
            /// </returns>
            protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
            {
                var response = base.GetWebResponse(request);
                if (response != null)
                {
                    this.ResponseUri = response.ResponseUri;
                    this.ContentType = response.ContentType;
                }
                return response;
            }
        }
    }
}
