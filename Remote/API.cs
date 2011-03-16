namespace RoliSoft.TVShowTracker.Remote
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Text;

    using Newtonsoft.Json;

    using RoliSoft.TVShowTracker.Remote.Objects;

    /// <summary>
    /// Provides method proxies for the lab.rolisoft.net API.
    /// </summary>
    public static class API
    {
        private static readonly SymmetricAlgorithm _algo = new RijndaelManaged
            {
                KeySize   = 256,
                BlockSize = 256,
                Mode      = CipherMode.CBC,
                IV        = Encoding.ASCII.GetBytes("0+R7L$O%Eq8Zieuo!rHkw@778rcrC5=+"),
                Padding   = PaddingMode.Zeros
            };
        private static byte[] _key;

        #region Remote method invocation
        /// <summary>
        /// Invokes a remote method.
        /// </summary>
        /// <typeparam name="T">The answer type of the method.</typeparam>
        /// <param name="func">The name of the method.</param>
        /// <param name="args">The arguments of the method.</param>
        /// <returns>Answer deserialized to type <c>T</c>.</returns>
        public static T InvokeRemoteMethod<T>(string func, params object[] args) where T : IRemoteObject, new()
        {
            return InternalInvokeRemoteMethod<T>(func, false, args);
        }

        /// <summary>
        /// Invokes a remote method, with encryption enabled.
        /// </summary>
        /// <typeparam name="T">The answer type of the method.</typeparam>
        /// <param name="func">The name of the method.</param>
        /// <param name="args">The arguments of the method.</param>
        /// <returns>Answer deserialized to type <c>T</c>.</returns>
        public static T InvokeSecureRemoteMethod<T>(string func, params object[] args) where T : IRemoteObject, new()
        {
            return InternalInvokeRemoteMethod<T>(func, true, args);
        }

        /// <summary>
        /// Invokes a remote method.
        /// </summary>
        /// <typeparam name="T">The answer type of the method.</typeparam>
        /// <param name="func">The name of the method.</param>
        /// <param name="secure">if set to <c>true</c> encryption will be enabled.</param>
        /// <param name="args">The arguments of the method.</param>
        /// <returns>Answer deserialized to type <c>T</c>.</returns>
        private static T InternalInvokeRemoteMethod<T>(string func, bool secure, object[] args) where T : IRemoteObject, new()
        {
            T obj;
            var sw = Stopwatch.StartNew();

            try
            {
                if (secure && _key == null)
                {
                    DoKeyExchange();
                }

                var post = Utils.EscapeUTF8(JsonConvert.SerializeObject(new Request(func, args)));

                if (secure)
                {
                    var tmp = Encoding.UTF8.GetBytes(post);
                    post    = Convert.ToBase64String(_algo.CreateEncryptor(_key, _algo.IV).TransformFinalBlock(tmp, 0, tmp.Length));
                }

                var resp = Utils.GetURL(
                    url:       "http://lab.rolisoft.net/api/",
                    postData:  post,
                    userAgent: "RS TV Show Tracker/" + Signature.Version,
                    headers:   new Dictionary<string, string> { { "X-UUID", Utils.GetUUID() } }
                );

                if (secure)
                {
                    var tmp = Convert.FromBase64String(resp);
                    resp    = Encoding.UTF8.GetString(_algo.CreateDecryptor(_key, _algo.IV).TransformFinalBlock(tmp, 0, tmp.Length)).TrimEnd('\0');
                }

                obj = JsonConvert.DeserializeObject<T>(resp);
                obj.Success = string.IsNullOrWhiteSpace(obj.Error);
            }
            catch (Exception ex)
            {
                obj = new T
                    {
                        Success = false,
                        Error   = ex.Message
                    };
            }

            sw.Stop();
            obj.Time = sw.Elapsed.TotalSeconds;

            return obj;
        }
        #endregion

        #region Helper functions
        /// <summary>
        /// Does a Diffie-Hellman key exchange with the remote server.
        /// </summary>
        private static void DoKeyExchange()
        {
            var rnd = new Random();
            var bob = new Dictionary<char, BigInteger>();
            
            var p = BigInteger.Parse("183682834604905165125374810562602240615039986742318115450988359927262634871970663686082391591571623296491813572206401878197607636471172058124265110443906080939593540162506781839597172463988741080705606095776622355713840538525653792028784953754106620637366292156337482013251106492137087709430744178761665741403");
            var g = BigInteger.Parse("61227611534968388375124936854200746871679995580772705150329453309087544957323554562027463863857207765497271190735467292732535878823724019374755036814635360313197846720835593946532390821329580360235202031925540785237946846175217930676261651251368873545788764052112494004417035497379029236476914726253888580467");

            bob['x'] = (BigInteger)rnd.Next(int.MaxValue) * rnd.Next(int.MaxValue) + 2;
            bob['a'] = BigInteger.ModPow(g, bob['x'], p);

            var alice = ExchangeKeys(Convert.ToBase64String(Encoding.ASCII.GetBytes(bob['a'].ToString())));

            bob['b'] = BigInteger.Parse(Encoding.ASCII.GetString(Convert.FromBase64String(alice.PublicKey)));
            bob['k'] = BigInteger.ModPow(bob['b'], bob['x'], p);

            _key = new HMACSHA256(Encoding.ASCII.GetBytes(Utils.GetUUID())).ComputeHash(Encoding.ASCII.GetBytes(bob['k'].ToString()));
        }
        #endregion

        #region API methods
        /// <summary>
        /// Adds two numbers together. [API test]
        /// </summary>
        /// <param name="number1">The first number.</param>
        /// <param name="number2">The second number.</param>
        /// <returns>Sum of the two numbers.</returns>
        public static Generic<double> Add(double number1, double number2)
        {
            return InvokeRemoteMethod<Generic<double>>("Add", number1, number2);
        }

        /// <summary>
        /// Calculates the arithmetic mean of the specified numbers. [API test]
        /// </summary>
        /// <param name="numbers">The numbers.</param>
        /// <returns>Arithmetic mean of the numbers.</returns>
        public static Generic<double> Mean(params double[] numbers)
        {
            return InvokeRemoteMethod<Generic<double>>("Mean", numbers);
        }

        /// <summary>
        /// Check for software update.
        /// </summary>
        /// <returns>URL of the new version, if available.</returns>
        public static UpdateCheck CheckForUpdate()
        {
            return InvokeRemoteMethod<UpdateCheck>("CheckForUpdate");
        }

        /// <summary>
        /// Sends the specified public key to the remote server for a Diffie-Hellman key exchange.
        /// </summary>
        /// <param name="key">The local public key.</param>
        /// <returns>Remote public key.</returns>
        public static KeyExchange ExchangeKeys(string key)
        {
            return InvokeRemoteMethod<KeyExchange>("ExchangeKeys", key);
        }

        /// <summary>
        /// Gets a list of known shows on the server.
        /// </summary>
        /// <returns>List of shows.</returns>
        public static ListOfShows GetListOfShows()
        {
            return InvokeRemoteMethod<ListOfShows>("GetListOfShows");
        }

        /// <summary>
        /// Gets the informations available on the server about the specified show.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="filter">The name of the fields to return or <c>null</c> for all available.</param>
        /// <returns>Informations about the show.</returns>
        public static RemoteShowInfo GetShowInfo(string name, IEnumerable<string> filter = null)
        {
            return InvokeRemoteMethod<RemoteShowInfo>("GetShowInfo", name, filter);
        }

        /// <summary>
        /// Sends the specified show information to the remote database.
        /// By caching little bits of information at lab.rolisoft.net, users can later retrieve it when
        /// additional information is required for a show, without putting a huge load on TVRage's server.
        /// </summary>
        /// <param name="info">The show object.</param>
        /// <param name="hash">The checksum of the object.</param>
        /// <returns><c>true</c> if operation was successful.</returns>
        public static General SetShowInfo(object info, string hash)
        {
            return InvokeRemoteMethod<General>("SetShowInfo", info, hash);
        }

        /// <summary>
        /// Gets the informations available on the server about the specified shows.
        /// </summary>
        /// <param name="shows">The name of the shows.</param>
        /// <param name="filter">The name of the fields to return or <c>null</c> for all available.</param>
        /// <returns>Informations about the shows.</returns>
        public static MultipleShowInfo GetMultipleShowInfo(IEnumerable<string> shows, IEnumerable<string> filter = null)
        {
            return InvokeRemoteMethod<MultipleShowInfo>("GetMultipleShowInfo", shows, filter);
        }

        /// <summary>
        /// Reports the unhandled exception anonymously.
        /// </summary>
        /// <param name="stacktrace">The stacktrace of the error.</param>
        /// <returns><c>true</c> if operation was successful.</returns>
        public static General ReportError(string stacktrace)
        {
            return InvokeSecureRemoteMethod<General>("ReportError", stacktrace);
        }

        /// <summary>
        /// Reports the unhandled exception anonymously.
        /// </summary>
        /// <param name="type">The type of the feedback.</param>
        /// <param name="name">The name of the user.</param>
        /// <param name="email">The email of the user.</param>
        /// <param name="message">The message.</param>
        /// <returns><c>true</c> if operation was successful.</returns>
        public static General SendFeedback(string type, string name, string email, string message)
        {
            return InvokeSecureRemoteMethod<General>("SendFeedback", type, name, email, message);
        }
        #endregion
    }
}