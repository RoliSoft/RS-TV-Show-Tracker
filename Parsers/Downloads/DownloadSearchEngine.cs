namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Represents a download link search engine.
    /// </summary>
    public abstract class DownloadSearchEngine : ParserEngine
    {
        /// <summary>
        /// Gets a value indicating whether the site requires authentication.
        /// </summary>
        /// <value><c>true</c> if requires authentication; otherwise, <c>false</c>.</value>
        public virtual bool Private
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the cookies used to access the site.
        /// </summary>
        /// <value>The cookies in the same format in which <c>alert(document.cookie)</c> returns in a browser.</value>
        public virtual string Cookies { get; set; }

        /// <summary>
        /// Gets the names of the required cookies for the authentication.
        /// </summary>
        /// <value>The required cookies for authentication.</value>
        public virtual string[] RequiredCookies { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this search engine can login using a username and password.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this search engine can login; otherwise, <c>false</c>.
        /// </value>
        public virtual bool CanLogin
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the URL to the login page.
        /// </summary>
        /// <value>The URL to the login page.</value>
        public virtual string LoginURL
        {
            get
            {
                return Site + "login.php";
            }
        }

        /// <summary>
        /// Gets the input fields of the login form.
        /// </summary>
        /// <value>The input fields of the login form.</value>
        public virtual Dictionary<string, object> LoginFields { get; internal set; }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public abstract Types Type { get; }

        /// <summary>
        /// Returns an <c>IDownloader</c> object which can be used to download the URLs provided by this parser.
        /// </summary>
        /// <value>The downloader.</value>
        public virtual IDownloader Downloader
        {
            get
            {
                return new HTTPDownloader();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this site is deprecated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if deprecated; otherwise, <c>false</c>.
        /// </value>
        public virtual bool Deprecated
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Occurs when a download link search found a new link.
        /// </summary>
        public event EventHandler<EventArgs<Link>> DownloadSearchNewLink;

        /// <summary>
        /// Occurs when a download link search is done.
        /// </summary>
        public event EventHandler<EventArgs> DownloadSearchDone;

        /// <summary>
        /// Occurs when a download link search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception>> DownloadSearchError;

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public abstract IEnumerable<Link> Search(string query);

        /// <summary>
        /// Authenticates with the site and returns the cookies.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Cookies on success, <c>string.Empty</c> on failure.</returns>
        public virtual string Login(string username, string password)
        {
            throw new NotImplementedException();
        }

        private Task _task;
        private CancellationTokenSource _cts;

        /// <summary>
        /// Searches for download links on the service asynchronously.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>The search task.</returns>
        public Task SearchAsync(string query)
        {
            CancelAsync();

            _cts  = new CancellationTokenSource();
            _task = Task.Factory.StartNew(SearchInternal, query, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            return _task;
        }

        /// <summary>
        /// Synchronous search helper.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        private void SearchInternal(object query)
        {
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                var list = Search((string)query);
                _cts.Token.ThrowIfCancellationRequested();

                foreach (var link in list)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    DownloadSearchNewLink.Fire(this, link);
                    _cts.Token.ThrowIfCancellationRequested();
                }

                _cts.Token.ThrowIfCancellationRequested();
                DownloadSearchDone.Fire(this);
            }
            catch (InvalidCredentialException)
            {
                Log.Warn(Name + " requires authentication.");

                var info = Settings.Get(Name + " Login");

                if (!CanLogin || string.IsNullOrWhiteSpace(info) || _cts.IsCancellationRequested)
                {
                    DownloadSearchDone.Fire(this);
                    return;
                }

                Log.Debug("Trying to authenticate with " + Name + "...");

                try
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var usrpwd  = Utils.Decrypt(this, info);
                    var cookies = Login(usrpwd[0], usrpwd[1]);

                    Cookies = cookies;
                    Settings.Set(Name + " Cookies", Utils.Encrypt(this, cookies));

                    _cts.Token.ThrowIfCancellationRequested();
                    var list = Search((string)query);
                    _cts.Token.ThrowIfCancellationRequested();

                    foreach (var link in list)
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        DownloadSearchNewLink.Fire(this, link);
                        _cts.Token.ThrowIfCancellationRequested();
                    }

                    _cts.Token.ThrowIfCancellationRequested();
                    DownloadSearchDone.Fire(this);
                }
                catch (InvalidCredentialException)
                {
                    Log.Warn("Failed to authenticate with " + Name + ": invalid credentials or broken plugin.");
                    DownloadSearchDone.Fire(this);
                }
                catch (Exception ex)
                {
                    DownloadSearchError.Fire(this, "There was an error while searching for download links.", ex);
                }
            }
            catch (Exception ex)
            {
                DownloadSearchError.Fire(this, "There was an error while searching for download links.", ex);
            }
        }

        /// <summary>
        /// Cancels the active asynchronous search.
        /// </summary>
        public void CancelAsync()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        /// <summary>
        /// Tests the parser by searching for "House" on the tracker.
        /// </summary>
        [Test]
        public virtual void TestSearch()
        {
            if (Private)
            {
                Cookies = Utils.Decrypt(this, Settings.Get(Name + " Cookies"))[0];

                if (string.IsNullOrWhiteSpace(Cookies))
                {
                    Assert.Inconclusive("Cookies are required to test a private site.");
                }
            }

            var list = Search("Bones").ToList();

            if (!Deprecated || list.Count > 0)
            {
                Assert.Greater(list.Count, 0, "Failed to grab any download links for Bones on {0}.".FormatWith(Name));
            }
            else
            {
                Assert.Inconclusive("Failed to grab any download links for Bones on {0}, but because the plugin is marked as Deprecated, this test results in Inconclusive rather than Error.".FormatWith(Name));
            }

            Console.WriteLine("┌────────────────────────────────────────────────────┬────────────┬────────────┬──────────────────────────────────────────┬──────────────────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Release name                                       │ Size       │ Quality    │ Additional informations                  │ Details page URL                                             │ Downloadable file URL                                        │");
            Console.WriteLine("├────────────────────────────────────────────────────┼────────────┼────────────┼──────────────────────────────────────────┼──────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────┤");
            list.ForEach(item => Console.WriteLine("│ {0,-50} │ {1,-10} │ {2,-10} │ {3,-40} │ {4,-60} │ {5,-60} │".FormatWith(item.Release.Transliterate().CutIfLonger(50), (item.Size ?? string.Empty).CutIfLonger(10), item.Quality, (item.Infos ?? string.Empty).CutIfLonger(40), (item.InfoURL ?? string.Empty).CutIfLonger(60), (item.FileURL ?? string.Empty).Replace("\0", "	␀").CutIfLonger(60))));
            Console.WriteLine("└────────────────────────────────────────────────────┴────────────┴────────────┴──────────────────────────────────────────┴──────────────────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────┘");
        }

        /// <summary>
        /// Tests the login.
        /// </summary>
        [Test]
        public virtual void TestLogin()
        {
            if (!CanLogin)
            {
                Assert.Inconclusive("This parser does not support authentication.");
            }

            var info = Settings.Get(Name + " Login");
            if (string.IsNullOrWhiteSpace(info))
            {
                Assert.Inconclusive("Login information is required to test the authentication.");
            }

            var usrpwd = Utils.Decrypt(this, info);

            Console.WriteLine("Logging in as '" + usrpwd[0] + "':");

            var cookies = Login(usrpwd[0], usrpwd[1]);

            Assert.IsNotNullOrEmpty(cookies, "Didn't receive any cookies from the login page.");

            Console.WriteLine("┌────────────────────────────────┬────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Cookie name                    │ Cookie value                                       │");
            Console.WriteLine("├────────────────────────────────┼────────────────────────────────────────────────────┤");

            foreach (var cookie in Regex.Split(cookies, @";\s"))
            {
                var kv = cookie.Split(new[] { '=' }, 2);
                if (kv.Length < 2) continue;

                Console.WriteLine("│ {0,-30} │ {1,-50} │".FormatWith(kv[0].CutIfLonger(30), kv[1].CutIfLonger(50)));
            }
            
            Console.WriteLine("└────────────────────────────────┴────────────────────────────────────────────────────┘");
        }

        /// <summary>
        /// Checks if a Gazelle or TBSource-based tracker requires authentication.
        /// </summary>
        /// <param name="node">The HTML document node.</param>
        /// <returns>
        ///   <c>true</c> if login is required; otherwise, <c>false</c>.
        /// </returns>
        protected bool GazelleTrackerLoginRequired(HtmlNode node)
        {
            return node.SelectSingleNode("//form[@method = 'post' and contains(@action, 'login')]") != null;
        }

        /// <summary>
        /// Initiates a login on a Gazelle or TBSource-based tracker.
        /// </summary>
        /// <param name="username">The username, if such value is required.</param>
        /// <param name="password">The password, if such value is required.</param>
        /// <param name="captcha">The captcha code entered by the user, if such value is required.</param>
        /// <param name="cookies">The cookies to send to the login page.</param>
        /// <returns>
        /// Cookies.
        /// </returns>
        protected string GazelleTrackerLogin(string username = null, string password = null, string captcha = null, string cookies = null)
        {
            var cookiez = string.Empty;
            
            Utils.GetURL(
                LoginURL,
                GenerateLoginPostData(username, password, captcha),
                cookies,
                request: req =>
                    {
                        req.Referer = Site;
                        req.AllowAutoRedirect = false;
                    },
                response: resp => cookiez = Utils.EatCookieCollection(resp.Cookies, !RequiredCookies.Contains("PHPSESSID"))
            );

            return cookiez;
        }

        /// <summary>
        /// Generates the login POST data using the <c>LoginFields</c> object on this current instance.
        /// </summary>
        /// <param name="username">The username, if such value is required.</param>
        /// <param name="password">The password, if such value is required.</param>
        /// <param name="captcha">The captcha code entered by the user, if such value is required.</param>
        /// <returns>
        /// URL-encoded data to POST to the login page.
        /// </returns>
        protected string GenerateLoginPostData(string username = null, string password = null, string captcha = null)
        {
            var post = new StringBuilder();

            foreach (var field in LoginFields)
            {
                if (post.Length != 0)
                {
                    post.Append("&");
                }

                post.Append(field.Key + "=");
                var value = string.Empty;

                if (field.Value is string)
                {
                    value = (string)field.Value;
                }
                else if (field.Value is Func<string>)
                {
                    value = ((Func<string>)field.Value)();
                }
                else if (field.Value is LoginFieldTypes)
                {
                    switch ((LoginFieldTypes)field.Value)
                    {
                        case LoginFieldTypes.UserName:
                            value = username;
                            break;

                        case LoginFieldTypes.Password:
                            value = password;
                            break;

                        case LoginFieldTypes.Captcha:
                            value = captcha;
                            break;

                        case LoginFieldTypes.ReturnTo:
                            value = "/";
                            break;
                    }
                }

                post.Append(Utils.EncodeURL(value));
            }

            return post.ToString();
        }
    }
}
