namespace RoliSoft.TVShowTracker.Parsers.Subtitles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    using NUnit.Framework;

    using Downloaders;
    using Downloaders.Engines;

    /// <summary>
    /// Represents a subtitle search engine.
    /// </summary>
    public abstract class SubtitleSearchEngine : ParserEngine
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
        /// Occurs when a download link search found a new link.
        /// </summary>
        public event EventHandler<EventArgs<Subtitle>> SubtitleSearchNewLink;

        /// <summary>
        /// Occurs when a subtitle search is done.
        /// </summary>
        public event EventHandler<EventArgs> SubtitleSearchDone;

        /// <summary>
        /// Occurs when a subtitle search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception>> SubtitleSearchError;

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public abstract IEnumerable<Subtitle> Search(string query);

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

        private Thread _job;

        /// <summary>
        /// Searches for subtitles on the service asynchronously.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        public void SearchAsync(string query)
        {
            CancelAsync();

            _job = new Thread(() =>
                {
                    try
                    {
                        var list = Search(query);

                        foreach (var sub in list)
                        {
                            SubtitleSearchNewLink.Fire(this, sub);
                        }

                        SubtitleSearchDone.Fire(this);
                    }
                    catch (InvalidCredentialException)
                    {
                        var info = Settings.Get(Name + " Login");

                        if (!CanLogin || string.IsNullOrWhiteSpace(info))
                        {
                            SubtitleSearchDone.Fire(this);
                            return;
                        }

                        try
                        {
                            var usrpwd  = Utils.Decrypt(info, GetType().FullName + Environment.NewLine + Utils.GetUUID()).Split(new[] { '\0' }, 2);
                            var cookies = Login(usrpwd[0], usrpwd[1]);

                            Cookies = cookies;
                            Settings.Set(Name + " Cookies", cookies);

                            var list = Search(query);

                            foreach (var link in list)
                            {
                                SubtitleSearchNewLink.Fire(this, link);
                            }

                            SubtitleSearchDone.Fire(this);
                        }
                        catch (InvalidCredentialException)
                        {
                            SubtitleSearchDone.Fire(this);
                        }
                        catch (Exception ex)
                        {
                            SubtitleSearchError.Fire(this, "There was an error while searching for subtitles.", ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        SubtitleSearchError.Fire(this, "There was an error while searching for subtitles.", ex);
                    }
                });
            _job.Start();
        }

        /// <summary>
        /// Cancels the active asynchronous search.
        /// </summary>
        public void CancelAsync()
        {
            if (_job != null)
            {
                _job.Abort();
                _job = null;
            }
        }

        /// <summary>
        /// Tests the parser by searching for "House S08E01" on the site.
        /// </summary>
        [Test]
        public virtual void TestSearchEpisode()
        {
            if (Private)
            {
                Cookies = Settings.Get(Name + " Cookies");

                if (string.IsNullOrWhiteSpace(Cookies))
                {
                    Assert.Inconclusive("Cookies are required to test a private site.");
                }
            }

            var list = Search("Bones S08E01").ToList();

            Assert.Greater(list.Count, 0, "Failed to grab any subtitles for Bones S08E01 on {0}.".FormatWith(Name));

            Console.WriteLine("┌────────────────────────────────────────────────────┬────────────┬──────────────────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Release name                                       │ Language   │ Details page URL                                             │ Downloadable file URL                                        │");
            Console.WriteLine("├────────────────────────────────────────────────────┼────────────┼──────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────┤");
            list.ForEach(item => Console.WriteLine("│ {0,-50} │ {1,-10} │ {2,-60} │ {3,-60} │".FormatWith(item.Release.Transliterate().CutIfLonger(50), item.Language.ToString().CutIfLonger(10), (item.InfoURL ?? string.Empty).CutIfLonger(60), (item.FileURL ?? string.Empty).CutIfLonger(60))));
            Console.WriteLine("└────────────────────────────────────────────────────┴────────────┴──────────────────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────┘");
        }

        /// <summary>
        /// Tests the parser by searching for "House" on the site.
        /// </summary>
        [Test]
        public virtual void TestSearchShow()
        {
            if (Private)
            {
                Cookies = Settings.Get(Name + " Cookies");

                if (string.IsNullOrWhiteSpace(Cookies))
                {
                    Assert.Inconclusive("Cookies are required to test a private site.");
                }
            }

            var list = Search("Bones").ToList();

            Assert.Greater(list.Count, 0, "Failed to grab any subtitles for Bones on {0}.".FormatWith(Name));

            Console.WriteLine("┌────────────────────────────────────────────────────┬────────────┬──────────────────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Release name                                       │ Language   │ Details page URL                                             │ Downloadable file URL                                        │");
            Console.WriteLine("├────────────────────────────────────────────────────┼────────────┼──────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────┤");
            list.ForEach(item => Console.WriteLine("│ {0,-50} │ {1,-10} │ {2,-60} │ {3,-60} │".FormatWith(item.Release.Transliterate().CutIfLonger(50), item.Language.ToString().CutIfLonger(10), (item.InfoURL ?? string.Empty).CutIfLonger(60), (item.FileURL ?? string.Empty).CutIfLonger(60))));
            Console.WriteLine("└────────────────────────────────────────────────────┴────────────┴──────────────────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────┘");
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

            var usrpwd = Utils.Decrypt(info, GetType().FullName + Environment.NewLine + Utils.GetUUID()).Split(new[] { '\0' }, 2);

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
