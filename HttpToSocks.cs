namespace RoliSoft.TVShowTracker
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;

    using Starksoft.Net.Proxy;

    /// <summary>
    /// Provides an HTTP proxy and forwards the incoming requests to an external SOCKSv5 proxy.
    /// </summary>
    public class HttpToSocks
    {
        /// <summary>
        /// A regular expression to parse the first line of the HTTP request.
        /// </summary>
        public static Regex FirstLineRegex = new Regex(@"^(?<method>[A-Z]{3,6}) https?://(?<host>[^/:]+)(?:\:(?<port>[0-9]+))?");

        /// <summary>
        /// Gets the local HTTP proxy.
        /// </summary>
        public WebProxy LocalProxy
        {
            get
            {
                return new WebProxy("127.0.0.1:" + ((IPEndPoint)_server.LocalEndpoint).Port);
            }
        }

        /// <summary>
        /// Gets or sets the remote SOCKS proxy.
        /// </summary>
        /// <value>
        /// The remote SOCKS proxy.
        /// </value>
        public string RemoteProxy { get; set; }

        private TcpListener _server;

        /// <summary>
        /// Starts listening for incoming connections at 127.0.0.1 on a random port.
        /// </summary>
        public void Listen()
        {
            _server = new TcpListener(IPAddress.Loopback, 0);
            _server.Start();
            _server.BeginAcceptSocket(AcceptClient, null);
        }

        /// <summary>
        /// Accepts the incoming request, processes it, and shuts down the whole server.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        private void AcceptClient(IAsyncResult asyncResult)
        {
            try
            {
                using (var client = _server.EndAcceptTcpClient(asyncResult))
                {
                    client.ReceiveTimeout = 100;

                    using (var stream = client.GetStream())
                    {
                        // read HTTP request

                        var requestData = new byte[0];

                        using (var ms = new MemoryStream())
                        {
                            CopyStreamToStream(stream, ms);
                            requestData = ms.ToArray();
                        }

                        // parse request data

                        var line  = Encoding.UTF8.GetString(requestData, 0, requestData.Length > 1024 ? 1024 : requestData.Length).Split('\n')[0];
                        var match = FirstLineRegex.Match(line);
                        var proxy = RemoteProxy.Split(':');

                        if (!match.Success)
                        {
                            throw new WebException("Unable to parse request.");
                        }

                        // forward request to SOCKS proxy

                        TunnelRequest(requestData, stream, match.Groups["host"].Value, match.Groups["port"].Success ? match.Groups["port"].Value.ToInteger() : 80, proxy[0], proxy[1].ToInteger());

                        stream.Close();
                        client.Close();
                    }
                }
            }
            finally
            {
                if (_server != null)
                {
                    try { _server.Stop(); } catch { }
                    _server = null;
                }
            }
        }

        /// <summary>
        /// Opens a connection through the proxy and forwards the received request.
        /// </summary>
        /// <param name="requestData">The HTTP proxy's request data.</param>
        /// <param name="responseStream">The stream to write the SOCKS proxy's response to.</param>
        /// <param name="destHost">The destination host.</param>
        /// <param name="destPort">The destination port.</param>
        /// <param name="proxyHost">The proxy host.</param>
        /// <param name="proxyPort">The proxy port.</param>
        private void TunnelRequest(byte[] requestData, NetworkStream responseStream, string destHost, int destPort, string proxyHost, int proxyPort)
        {
            var proxy = new Socks5ProxyClient(proxyHost, proxyPort);

            using (var client = proxy.CreateConnection(destHost, destPort))
            using (var stream = client.GetStream())
            {
                stream.Write(requestData, 0, requestData.Length);
                stream.Flush();

                CopyStreamToStream(stream, responseStream);
            }
        }

        /// <summary>
        /// Copies the first stream's content to the second stream.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="destionation">The destionation stream.</param>
        /// <param name="bufferLength">Length of the buffer.</param>
        private void CopyStreamToStream(Stream source, Stream destionation, int bufferLength = 256)
        {
            var buffer = new byte[bufferLength];

            while (true)
            {
                int i;

                try
                {
                    i = source.Read(buffer, 0, bufferLength);
                }
                catch
                {
                    break;
                }

                if (i == 0)
                {
                    break;
                }

                destionation.Write(buffer, 0, i);
            }
        }
    }
}
