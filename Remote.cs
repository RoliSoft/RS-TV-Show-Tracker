namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    using RoliSoft.TVShowTracker.DynamicJson;

    /// <summary>
    /// Provides a fun way to communicate with lab.rolisoft.net/api by harnessing the power of dynamic.
    /// </summary>
    public class REST : DynamicObject
    {
        private static readonly REST _instance       = new REST(),
                                     _secureInstance = new REST(true);

        private readonly bool _secure;
        private readonly SymmetricAlgorithm _algo;

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
                        KeySize = 128, // 192 or 256 won't work, because PHP sucks at everything.
                        Mode    = CipherMode.CBC,
                        Key     = Encoding.ASCII.GetBytes("!rHkw@778rcrC5=+"),
                        IV      = Encoding.ASCII.GetBytes("0+R7L$O%Eq8Zieuo"),
                        Padding = PaddingMode.Zeros
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
                var jsw = new JsonWriter();
                jsw.StartObjectScope();
                jsw.WriteName("func");
                jsw.WriteValue(binder.Name);
                jsw.WriteName("args");
                jsw.WriteValue(args);
                jsw.EndScope();

                var post = jsw.Json;

                if (_secure)
                {
                    var tmp = Encoding.UTF8.GetBytes(post);
                    post = Convert.ToBase64String(_algo.CreateEncryptor().TransformFinalBlock(tmp, 0, tmp.Length));
                }

                var resp = Utils.GetURL(
                    url:       "http://localhost/update/",
                    postData:  post,
                    userAgent: "RS TV Show Tracker/" + Signature.Version,
                    headers:   new Dictionary<string, string> { { "X-UUID", Utils.GetUUID() } }
                );

                if (_secure)
                {
                    var tmp = Convert.FromBase64String(resp);
                    resp = Encoding.UTF8.GetString(_algo.CreateDecryptor().TransformFinalBlock(tmp, 0, tmp.Length)).TrimEnd('\0');
                }

                obj = new JsonReader(resp).ReadValue();
                obj.Success = !(obj.GetDynamicMemberNames() as IEnumerable<string>).Contains("Error");
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
    }
}
