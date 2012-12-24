namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;

    using Org.BouncyCastle.Crypto.Engines;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Math;

    /// <summary>
    /// Contains informations about the assembly.
    /// </summary>
    public static class Signature
    {
        /// <summary>
        /// Gets the name of the software.
        /// </summary>
        /// <value>The software name.</value>
        public static string Software { get; private set; }

        /// <summary>
        /// Gets the version number of the executing assembly.
        /// </summary>
        /// <value>The software version.</value>
        public static string Version { get; private set; }

        /// <summary>
        /// Gets the formatted version number of the executing assembly.
        /// </summary>
        /// <value>The formatted software version.</value>
        public static string VersionFormatted { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this is a nightly build.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this is a nightly build; otherwise, <c>false</c>.
        /// </value>
        public static bool IsNightly
        {
            get
            {
                return
#if NIGHTLY
                    true
#else
                    false
#endif
                ;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a donation key has been entered.
        /// </summary>
        /// <value>
        ///   <c>true</c> if a donation key has been entered; otherwise, <c>false</c>.
        /// </value>
        public static bool IsActivated
        {
            get
            {
#if ACTIVATE_WITHOUT_DONATION
                return true;
#else
                // TODO check for activation.
                return true;
#endif
            }
        }

        /// <summary>
        /// Gets the date and time when the executing assembly was compiled.
        /// </summary>
        /// <value>The compile time.</value>
        public static DateTime CompileTime { get; private set; }

        /// <summary>
        /// Gets the full path to the executing assembly.
        /// </summary>
        /// <value>The full path.</value>
        public static string FullPath { get; private set; }

        /// <summary>
        /// This number is used for various purposes where a non-random unique number is required.
        /// </summary>
        public static long MagicNumber
        {
            get
            {
                return 0xFEEDFACEC0FFEE;
            }
        }

        /// <summary>
        /// Gets the Aperture Science Turret Testing Facility Access Codes.
        /// </summary>
        private static BigInteger Exponent = new BigInteger(new byte[] { 0x01, 0x00, 0x01 });

        /// <summary>
        /// Gets Cave Johnson's known phone numbers.
        /// </summary>
        private static BigInteger[] Moduluses =
            {
                new BigInteger(new byte[] { 0x00, 0xce, 0x23, 0xb3, 0xd1, 0x54, 0x92, 0xf7, 0x56, 0x2c, 0x0c, 0x7c, 0xaa, 0xc8, 0xe0, 0x63, 0xf3, }),
                new BigInteger(new byte[] { 0x00, 0xce, 0xab, 0x66, 0x25, 0xac, 0x61, 0x54, 0xfb, 0x4e, 0x3c, 0xdf, 0x32, 0x0e, 0x4b, 0x0a, 0xc9, }),
                new BigInteger(new byte[] { 0x00, 0xaf, 0x49, 0x2f, 0xfc, 0x6c, 0x95, 0xfc, 0xd9, 0x89, 0x80, 0x8f, 0x7e, 0x74, 0x6d, 0xe9, 0x7f, }),
                new BigInteger(new byte[] { 0x00, 0xa2, 0x8f, 0x41, 0x94, 0x39, 0xed, 0x95, 0xb9, 0xee, 0xa0, 0x82, 0xa4, 0x36, 0x4e, 0x81, 0x17, }),
                new BigInteger(new byte[] { 0x00, 0xb2, 0x58, 0x49, 0xd8, 0xac, 0x4b, 0x96, 0x34, 0x31, 0x61, 0x74, 0x14, 0x1b, 0xba, 0x7b, 0x59, }),
                new BigInteger(new byte[] { 0x00, 0xc6, 0x82, 0x67, 0x1a, 0x39, 0x22, 0x81, 0x87, 0xbe, 0xe5, 0x9c, 0x9c, 0x36, 0x4f, 0x7f, 0x0d, }),
                new BigInteger(new byte[] { 0x00, 0x9d, 0xa6, 0x44, 0x89, 0xfe, 0xed, 0x21, 0x05, 0xbd, 0x65, 0x41, 0xfe, 0x12, 0xc9, 0x42, 0x03, }),
                new BigInteger(new byte[] { 0x00, 0xa5, 0x53, 0x73, 0xda, 0x6b, 0xf2, 0x00, 0xf1, 0xac, 0xb8, 0x63, 0x8a, 0xc7, 0x11, 0x74, 0x4d, }),
                new BigInteger(new byte[] { 0x00, 0xd1, 0x8b, 0x4c, 0x02, 0xef, 0x79, 0xa7, 0xe6, 0xd4, 0xbd, 0x2e, 0xe6, 0x01, 0xe5, 0x7b, 0xd1, }),
                new BigInteger(new byte[] { 0x00, 0xc3, 0x55, 0xa1, 0xa2, 0xe3, 0xe2, 0xad, 0x7e, 0x62, 0xcc, 0xaa, 0x98, 0x34, 0xb7, 0x15, 0x6d, }),
            };

        /// <summary>
        /// Initializes static members of the <see cref="Signature"/> class. 
        /// </summary>
        static Signature()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;

            Software         = "RS TV Show Tracker";
            Version          = ver.Major + "." + ver.Minor + (ver.Build != 0 ? "." + ver.Build : string.Empty);
            VersionFormatted = "v" + ver.Major + "." + ver.Minor + (ver.Build != 0 ? " build " + ver.Build : string.Empty) + (IsNightly ? " nightly" : string.Empty);
            CompileTime      = RetrieveLinkerTimestamp();
            try { FullPath   = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar; } catch (ArgumentException) { }
        }

        /// <summary>
        /// Gets the numbers. This is an easter egg. ;)
        /// </summary>
        /// <returns>
        /// The numbers.
        /// </returns>
        public static IEnumerable<int> GetNumbers()
        {
            for (var x = 1; x != 6; x++)
            {
                yield return (int)(60 + 4.25 * Math.Pow(x * x, 2) + 91.75 * x * x - 29.375 * x * Math.Pow(x, 2) - 0.22499999 * x * Math.Pow(x, 2) * Math.Pow(x, 2) - 122.4 * x);
            }
        }

        /// <summary>
        /// Retrieves the linker timestamp from the PE header embedded in the executable file.
        /// </summary>
        /// <returns>
        /// Compilation date.
        /// </returns>
        private static DateTime RetrieveLinkerTimestamp()
        {
            try
            {
                var pe = new byte[2048];

                using (var fs = new FileStream(Assembly.GetExecutingAssembly().Location, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(pe, 0, 2048);
                }

                return Extensions.GetUnixTimestamp(BitConverter.ToInt32(pe, BitConverter.ToInt32(pe, 60) + 8));
            }
            catch
            {
                return Utils.UnixEpoch;
            }
        }

        /// <summary>
        /// Determines whether the specified activation data is valid.
        /// </summary>
        /// <param name="user">The email address of the user.</param>
        /// <param name="key">The corresponding donation key.</param>
        /// <returns>
        ///   <c>true</c> if the activation was successful; otherwise, <c>false</c>.
        /// </returns>
        public static bool Activate(string user, string key)
        {
            var email = Encoding.UTF8.GetBytes(user.ToLower().Trim());
            var hash0 = new HMACSHA512(MD5.Create().ComputeHash(email)).ComputeHash(email);
            var hash1 = Utils.Base85.Decode(key);

            if (hash1.Length != 16)
            {
                return false;
            }

            foreach (var modulus in Moduluses)
            {
                var rsa = new RsaEngine();
                rsa.Init(false, new RsaKeyParameters(false, modulus, Exponent));
                var hash2 = rsa.ProcessBlock(hash1, 0, hash1.Length);
                var match = true;

                for (var i = 0; i < 16; i++)
                {
                    if (hash2[i] != hash0[i])
                    {
                        match = false;
                    }
                }

                if (match)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
