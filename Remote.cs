namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Text;

    using Newtonsoft.Json;

    /// <summary>
    /// Provides a fun way to communicate with lab.rolisoft.net/api by harnessing the power of dynamic.
    /// </summary>
    public class REST : DynamicObject
    {
        private static readonly REST _instance       = new REST(),
                                     _secureInstance = new REST(true);

        private readonly bool _secure;
        private readonly SymmetricAlgorithm _algo;
        private byte[] _key;

        /// <summary>
        /// Initializes a new instance of the <see cref="REST"/> class.
        /// </summary>
        /// <param name="secure">if set to <c>true</c> the connection will be encrypted.</param>
        private REST(bool secure = false)
        {
            _secure = secure;

            if (secure)
            {
                _algo = new RijndaelManaged
                    {
                        KeySize   = 256,
                        BlockSize = 256,
                        Mode      = CipherMode.CBC,
                        IV        = Encoding.ASCII.GetBytes("0+R7L$O%Eq8Zieuo!rHkw@778rcrC5=+"),
                        Padding   = PaddingMode.Zeros
                    };
            }
        }

        /// <summary>
        /// Gets the method proxy to lab.rolisoft.net/api
        /// </summary>
        /// <value>The instance.</value>
        public static dynamic Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Gets the encrypted method proxy to lab.rolisoft.net/api
        /// </summary>
        /// <value>The instance.</value>
        public static dynamic SecureInstance
        {
            get
            {
                return _secureInstance;
            }
        }

        /// <summary>
        /// Provides the implementation for operations that invoke a member. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as calling a method.
        /// </summary>
        /// <param name="binder">Provides information about the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleMethod". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="args">The arguments that are passed to the object member during the invoke operation. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="args"/>[0] is equal to 100.</param>
        /// <param name="result">The result of the member invocation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            dynamic obj;
            var sw = Stopwatch.StartNew();

            try
            {
                if (_secure && _key == null)
                {
                    _key = InitiateKeyExchange();
                }

                var post = JsonConvert.SerializeObject(new { func = binder.Name, args });

                if (_secure)
                {
                    var tmp = Encoding.UTF8.GetBytes(post);
                    post = Convert.ToBase64String(_algo.CreateEncryptor(_key, _algo.IV).TransformFinalBlock(tmp, 0, tmp.Length));
                }

                var resp = Utils.GetURL(
                    url:       "http://lab.rolisoft.net/api/",
                    postData:  post,
                    userAgent: "RS TV Show Tracker/" + Signature.Version,
                    headers:   new Dictionary<string, string> { { "X-UUID", Utils.GetUUID() } }
                );
                
                if (_secure)
                {
                    var tmp = Convert.FromBase64String(resp);
                    resp = Encoding.UTF8.GetString(_algo.CreateDecryptor(_key, _algo.IV).TransformFinalBlock(tmp, 0, tmp.Length)).TrimEnd('\0');
                }

                obj = JsonConvert.DeserializeObject<DynamicDictionary>(resp);
                obj.Success = obj.ContainsKey("Error");
            }
            catch (Exception ex)
            {
                obj = new ExpandoObject();
                obj.Success = false;
                obj.Error = ex.Message;
            }

            sw.Stop();
            obj.Time = sw.Elapsed.TotalSeconds;
            result = obj;
            return true;
        }

        /// <summary>
        /// Initiates a Diffie-Hellman key exchange with the remote server.
        /// </summary>
        /// <returns>Mutual private key.</returns>
        public byte[] InitiateKeyExchange()
        {
            var rnd = new Random();
            var bob = new Dictionary<char, BigInteger>();
            
            var p = BigInteger.Parse("183682834604905165125374810562602240615039986742318115450988359927262634871970663686082391591571623296491813572206401878197607636471172058124265110443906080939593540162506781839597172463988741080705606095776622355713840538525653792028784953754106620637366292156337482013251106492137087709430744178761665741403");
            var g = BigInteger.Parse("61227611534968388375124936854200746871679995580772705150329453309087544957323554562027463863857207765497271190735467292732535878823724019374755036814635360313197846720835593946532390821329580360235202031925540785237946846175217930676261651251368873545788764052112494004417035497379029236476914726253888580467");

            bob['x'] = (BigInteger)rnd.Next(int.MaxValue) * (BigInteger)rnd.Next(int.MaxValue) + 2;
            bob['a'] = BigInteger.ModPow(g, bob['x'], p);

            var alice = Instance.ExchangeKeys(Convert.ToBase64String(Encoding.ASCII.GetBytes(bob['a'].ToString())));

            bob['b'] = BigInteger.Parse(Encoding.ASCII.GetString(Convert.FromBase64String(alice.PublicKey)));
            bob['k'] = BigInteger.ModPow(bob['b'], bob['x'], p);

            var key = new HMACSHA256(Encoding.ASCII.GetBytes(Utils.GetUUID())).ComputeHash(Encoding.ASCII.GetBytes(bob['k'].ToString()));

            return key;
        }
    }
}