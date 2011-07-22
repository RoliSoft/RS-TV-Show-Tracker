namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Engines;
    using Org.BouncyCastle.Crypto.Modes;
    using Org.BouncyCastle.Crypto.Paddings;
    using Org.BouncyCastle.Crypto.Parameters;

    /// <summary>
    /// Provides methods for encapsulating HTTP links into DLC, CCF and RSDF containers.
    /// </summary>
    public static class DLCAPI
    {
        /// <summary>
        /// The URL to jDownloader DLC encryption service.
        /// </summary>
        public static string DLCCrypt = "http://service.jdownloader.org/dlcrypt/service.php";

        /// <summary>
        /// The encryption key for a CCF container.
        /// </summary>
        public static byte[] CCFKey = new byte[]
            {
                0x5F, 0x67, 0x9C, 0x00, 0x54, 0x87, 0x37, 0xE1,
                0x20, 0xE6, 0x51, 0x8A, 0x98, 0x1B, 0xD0, 0xBA,
                0x11, 0xAF, 0x5C, 0x71, 0x9E, 0x97, 0x50, 0x29,
                0x83, 0xAD, 0x6A, 0xA3, 0x8E, 0xD7, 0x21, 0xC3
            };

        /// <summary>
        /// The initialization vector for a CCF container.
        /// </summary>
        public static byte[] CCFIV = new byte[]
            {
                0xE3, 0xD1, 0x53, 0xAD, 0x60, 0x9E, 0xF7, 0x35,
                0x8D, 0x66, 0x68, 0x41, 0x80, 0xC7, 0x33, 0x1A
            };

        /// <summary>
        /// The encryption key for an RSDF container.
        /// </summary>
        public static byte[] RSDFKey = new byte[]
            {
                0x8C, 0x35, 0x19, 0x2D, 0x96, 0x4D, 0xC3, 0x18,
                0x2C, 0x6F, 0x84, 0xF3, 0x25, 0x22, 0x39, 0xEB,
                0x4A, 0x32, 0x0D, 0x25, 0x00, 0x00, 0x00, 0x00
            };

        /// <summary>
        /// The initialization vector for an RSDF container.
        /// </summary>
        public static byte[] RSDFIV = new byte[]
            {
                0xA3, 0xD5, 0xA3, 0x3C, 0xB9, 0x5A, 0xC1, 0xF5,
                0xCB, 0xDB, 0x1A, 0xD2, 0x5C, 0xB0, 0xA7, 0xAA
            };

        /// <summary>
        /// Encapsulates the specified links into a DLC container.
        /// </summary>
        /// <param name="name">The name of the package.</param>
        /// <param name="links">The links.</param>
        /// <returns>
        /// Base-64-encoded DLC container.
        /// </returns>
        public static string CreateDLC(string name, string[] links)
        {
            var sb = new StringBuilder();

            sb.Append("<dlc>");
            sb.Append("<header>");
            sb.Append("<generator>");
            sb.Append("<app>" + Convert.ToBase64String(Encoding.UTF8.GetBytes("RS TV Show Tracker")) + "</app>");
            sb.Append("<version>" + Convert.ToBase64String(Encoding.UTF8.GetBytes(Signature.Version)) + "</version>");
            sb.Append("<url>" + Convert.ToBase64String(Encoding.UTF8.GetBytes("http://lab.rolisoft.net/")) + "</url>");
            sb.Append("</generator>");
            sb.Append("<dlcxmlversion>" + Convert.ToBase64String(Encoding.UTF8.GetBytes("20_02_2008")) + "</dlcxmlversion>");
            sb.Append("</header>");
            sb.Append("<content>");
            sb.Append("<package name=\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(name)) + "\">");

            foreach (var link in links)
            {
                sb.Append("<file>");
                sb.Append("<url>" + Convert.ToBase64String(Encoding.UTF8.GetBytes(link)) + "</url>");
              //sb.Append("<filename></filename>");
              //sb.Append("<size></size>");
                sb.Append("</file>");
            }

            sb.Append("</package>");
            sb.Append("</content>");
            sb.Append("</dlc>");

            var xml = Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
            var key = BitConverter.ToString(new SHA256CryptoServiceProvider().ComputeHash(BitConverter.GetBytes(DateTime.Now.ToBinary()))).Replace("-", string.Empty).Substring(0, 16);
            
            var srv = Utils.GetURL(DLCCrypt, "&data=" + key + "&lid=" + Convert.ToBase64String(Encoding.UTF8.GetBytes("JDOWNLOADER.ORG_" + DLCCrypt + "_3600")));
            var rcr = Regex.Match(srv, @"<rc>(.+)</rc>");

            if (!rcr.Groups[1].Success)
            {
                throw new Exception("The jDownloader DLC encryption service did not return an encryption key.");
            }

            var enc = rcr.Groups[1].Value;

            var aes = new AesEngine();
            var cbc = new CbcBlockCipher(aes);
            var pk7 = new Pkcs7Padding();
            var pad = new PaddedBufferedBlockCipher(cbc, pk7);

            pad.Init(true, new ParametersWithIV(new KeyParameter(Encoding.ASCII.GetBytes(key)), Encoding.ASCII.GetBytes(key)));

            var xm2 = Convert.ToBase64String(pad.DoFinal(Encoding.ASCII.GetBytes(xml)));

            return xm2 + enc;
        }

        /// <summary>
        /// Encapsulates the specified links into a CCF container.
        /// </summary>
        /// <param name="name">The name of the package.</param>
        /// <param name="links">The links.</param>
        /// <returns>
        /// CCF container.
        /// </returns>
        public static byte[] CreateCCF(string name, string[] links)
        {
            var sb = new StringBuilder();

            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.Append("<CryptLoad>");
            sb.Append("<Package service=\"\" name=\"" + name + "\" url=\"Directlinks\">");

            foreach (var link in links)
            {
                sb.Append("<Download Url=\"" + link + "\">");
                sb.Append("<Url>" + link + "</Url>");
              //sb.Append("<FileName></FileName>");
              //sb.Append("<FileSize></FileSize>");
                sb.Append("</Download>");
            }

            sb.Append("</Package>");
            sb.Append("</CryptLoad>");

            var aes = new AesEngine();
            var cbc = new CbcBlockCipher(aes);
            var pk7 = new Pkcs7Padding();
            var pad = new PaddedBufferedBlockCipher(cbc, pk7);

            pad.Init(true, new ParametersWithIV(new KeyParameter(CCFKey), CCFIV));

            return pad.DoFinal(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        /// <summary>
        /// Encapsulates the specified links into an RSDF container.
        /// </summary>
        /// <param name="links">The links.</param>
        /// <returns>
        /// Base-16-encoded RSDF container.
        /// </returns>
        public static string CreateRSDF(string[] links)
        {
            var aes = new AesEngine();
            var cfb = new CfbBlockCipher(aes, 8);
            var pad = new BufferedBlockCipher(cfb);
            var sb  = new StringBuilder();

            pad.Init(true, new ParametersWithIV(new KeyParameter(RSDFKey), RSDFIV));

            foreach (var link in links)
            {
                var input  = Encoding.UTF8.GetBytes(link);
                var output = new byte[input.Length];

                for (var i = 0; i < input.Length; i++)
                {
                    output[i] = pad.ProcessByte(input[i])[0];
                }

                sb.Append(Convert.ToBase64String(output));
                sb.Append(Environment.NewLine);
            }

            return BitConverter.ToString(Encoding.ASCII.GetBytes(sb.ToString())).Replace("-", string.Empty);
        }
    }
}
