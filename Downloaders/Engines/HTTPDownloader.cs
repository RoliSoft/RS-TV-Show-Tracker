namespace RoliSoft.TVShowTracker.Downloaders.Engines
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;

    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    /// <summary>
    /// Provides a simple HTTP downloader.
    /// </summary>
    public class HTTPDownloader : IDownloader
    {
        /// <summary>
        /// Occurs when a file download completes.
        /// </summary>
        public event EventHandler<EventArgs<string, string, string>> DownloadFileCompleted;

        /// <summary>
        /// Occurs when the download progress changes.
        /// </summary>
        public event EventHandler<EventArgs<int>> DownloadProgressChanged;

        private WebClient _wc;

        /// <summary>
        /// Asynchronously downloads the specified link.
        /// </summary>
        /// <param name="link">
        /// The object containing the link.
        /// This can be an URL in a string or a <c>Link</c>/<c>Subtitle</c> object.
        /// </param>
        /// <param name="target">The target location.</param>
        /// <param name="token">The user token.</param>
        public void Download(object link, string target, string token = null)
        {
            var id = Utils.Rand.Next(short.MaxValue);
            var st = DateTime.Now;
            
            _wc  = new Utils.SmarterWebClient();
            Uri uri;

            if (link is string)
            {
                uri = new Uri(link as string);
            }
            else if (link is Link)
            {
                uri = new Uri((link as Link).FileURL);

                if (!string.IsNullOrWhiteSpace((link as Link).Source.Cookies))
                {
                    _wc.Headers[HttpRequestHeader.Cookie] = (link as Link).Source.Cookies;
                }
            }
            else if (link is Subtitle)
            {
                uri = new Uri((link as Subtitle).FileURL);

                if (!string.IsNullOrWhiteSpace((link as Subtitle).Source.Cookies))
                {
                    _wc.Headers[HttpRequestHeader.Cookie] = (link as Subtitle).Source.Cookies;
                }
            }
            else
            {
                throw new Exception("The link object is an unsupported type.");
            }

            var domain = uri.Host.Replace("www.", string.Empty);

            Log.Debug("HTTP#{0} GET {1}", new[] { id.ToString(), uri.ToString() });
            
            _wc.Headers[HttpRequestHeader.Referer] = "http://" + uri.DnsSafeHost + "/";
            _wc.DownloadProgressChanged           += (s, e) => DownloadProgressChanged.Fire(this, e.ProgressPercentage);
            _wc.DownloadFileCompleted             += (s, e) =>
                {
                    Log.Debug("HTTP#" + id + " [" + domain + "] is " + Utils.GetFileSize(new FileInfo(target).Length) + " and took " + (DateTime.Now - st).TotalSeconds + "s.");
                    if (Log.IsTraceEnabled) Log.Trace("HTTP#" + id + " [" + domain + "] is " + (s as Utils.SmarterWebClient).ContentType + ", saved to " + target + " with token " + token);
                    DownloadFileCompleted.Fire(this, target, (s as Utils.SmarterWebClient).FileName, token ?? string.Empty);
                };

            var proxy = default(string);
            var proxyId = default(object);

            if (Settings.Get<Dictionary<string, object>>("Proxied Domains").TryGetValue(domain, out proxyId))
            {
                proxy = (string)Settings.Get<Dictionary<string, object>>("Proxies")[(string)proxyId];
            }

            if (proxy != null)
            {
                var proxyUri = new Uri(proxy.Replace("$domain.", string.Empty));

                switch (proxyUri.Scheme.ToLower())
                {
                    case "http":
                        if (proxy.Contains("$url"))
                        {
                            uri = new Uri(proxy.Replace("$url", Utils.EncodeURL(uri.ToString())));
                        }
                        else if (proxy.Contains("$domain") && proxy.Contains("$path"))
                        {
                            uri = new Uri(proxy.Replace("$domain", uri.DnsSafeHost).Replace("$path", uri.AbsolutePath));
                        }
                        else
                        {
                            _wc.Proxy = new WebProxy(proxyUri.Host + ":" + proxyUri.Port);
                        }
                        break;

                    case "socks4":
                    case "socks4a":
                    case "socks5":
                        var tunnel = new HttpToSocks { RemoteProxy = HttpToSocks.Proxy.ParseUri(proxyUri) };
                        tunnel.Listen();
                        _wc.Proxy = (WebProxy)tunnel.LocalProxy;
                        break;
                }

                Log.Debug("HTTP#" + id + " [" + domain + "] is proxied through " + proxyId + " (" + proxyUri + ")");
            }

            _wc.DownloadFileAsync(uri, target);
        }

        /// <summary>
        /// Cancels the asynchronous download.
        /// </summary>
        public void CancelAsync()
        {
            try { _wc.CancelAsync(); } catch { }
        }
    }
}
