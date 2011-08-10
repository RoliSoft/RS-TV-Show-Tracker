namespace RoliSoft.TVShowTracker
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    using Starksoft.Net.Proxy;

    /// <summary>
    /// Provides an HTTP proxy and forwards the incoming requests to an external SOCKSv5 proxy.
    /// </summary>
    public class HttpToSocks
    {
        /// <summary>
        /// A regular expression to find the URL in the HTTP request.
        /// </summary>
        public static Regex URLRegex = new Regex(@"https?://(?<host>[^/:]+)(?:\:(?<port>[0-9]+))?(?<path>/[^\s$]*)");
        
        /// <summary>
        /// Gets the local HTTP proxy.
        /// </summary>
        public Proxy LocalProxy
        {
            get
            {
                if (_server == null)
                {
                    return null;
                }

                return new Proxy
                    {
                        Type = ProxyType.Http,
                        Host = "[::1]",
                        Port = ((IPEndPoint)_server.LocalEndpoint).Port
                    };
            }
        }

        /// <summary>
        /// Gets or sets the remote SOCKS proxy.
        /// </summary>
        /// <value>
        /// The remote SOCKS proxy.
        /// </value>
        public Proxy RemoteProxy { get; set; }

        private TcpListener _server;
        private Thread _timeout;
        
        /// <summary>
        /// Starts listening for incoming connections at ::1 on a random port.
        /// </summary>
        public void Listen()
        {
            _server = new TcpListener(IPAddress.IPv6Loopback, 0);
            _server.Start();
            _server.BeginAcceptSocket(AcceptClient, null);

            _timeout = new Thread(Timeout);
            _timeout.Start();
        }

        /// <summary>
        /// Kills the server after a minute.
        /// </summary>
        private void Timeout()
        {
            Thread.Sleep(TimeSpan.FromMinutes(1));

            if (_server != null)
            {
                try { _server.Stop(); } catch { }
                _server = null;
            }
        }

        /// <summary>
        /// Accepts the incoming request, processes it, and shuts down the whole server.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        private void AcceptClient(IAsyncResult asyncResult)
        {
            _server.BeginAcceptSocket(AcceptClient, null);

            using (var client = _server.EndAcceptTcpClient(asyncResult))
            using (var stream = client.GetStream())
            {
                stream.ReadTimeout = 100;
                ProcessHTTPRequest(stream);
            }
        }

        /// <summary>
        /// Reads and processes the HTTP request from the stream.
        /// </summary>
        /// <param name="requestStream">The request stream.</param>
        private void ProcessHTTPRequest(NetworkStream requestStream)
        {
            string host, path, postData;
            string[] request;

            var headers = new StringBuilder();
            var port    = 80;

            using (var ms = new MemoryStream())
            {
                CopyStreamToStream(requestStream, ms);

                ms.Position = 0;

                using (var sr = new StreamReader(ms, Encoding.UTF8, false))
                {
                    // [0]GET [1]/index.php [2]HTTP/1.1
                    request = (sr.ReadLine() ?? string.Empty).Split(' ');

                    if (request[0] == "CONNECT")
                    {
                        sr.Close();
                        var dest = request[1].Split(':');
                        TunnelRequest(requestStream, dest[0], dest[1].ToInteger());
                        return;
                    }

                    var m = URLRegex.Match(request[1]);

                    host = m.Groups["host"].Value;
                    path = m.Groups["path"].Value;

                    if (m.Groups["port"].Success)
                    {
                        port = m.Groups["port"].Value.ToInteger();
                    }

                    // read headers

                    while (true)
                    {
                        var header = (sr.ReadLine() ?? string.Empty).Trim();
                        
                        if (string.IsNullOrWhiteSpace(header))
                        {
                            break;
                        }

                        if (header.StartsWith("Connection:") || header.StartsWith("Proxy-Connection:"))
                        {
                            continue;
                        }

                        headers.AppendLine(header);
                    }

                    headers.AppendLine("Connection: close");

                    // read post data

                    if (request[0] == "POST")
                    {
                        postData = sr.ReadToEnd();
                    }
                    else
                    {
                        postData = null;
                    }
                }
            }

            var finalRequest = new StringBuilder();

            finalRequest.AppendLine(request[0] + " " + path + " " + request[2]);
            finalRequest.Append(headers);
            finalRequest.AppendLine();

            if (postData != null)
            {
                finalRequest.Append(postData);
            }

            ForwardRequest(Encoding.UTF8.GetBytes(finalRequest.ToString()), requestStream, host, port);
        }

        /// <summary>
        /// Opens a connection through the proxy and forwards the received request.
        /// </summary>
        /// <param name="requestData">The HTTP proxy's request data.</param>
        /// <param name="responseStream">The stream to write the SOCKS proxy's response to.</param>
        /// <param name="destHost">The destination host.</param>
        /// <param name="destPort">The destination port.</param>
        private void ForwardRequest(byte[] requestData, NetworkStream responseStream, string destHost, int destPort)
        {
            using (var client = RemoteProxy.CreateProxy().CreateConnection(destHost, destPort))
            using (var stream = client.GetStream())
            {
                stream.ReadTimeout = 100;

                CopyBytesToStream(requestData, stream);
                CopyStreamToStream(stream, responseStream);
            }
        }

        /// <summary>
        /// Opens a connection through the proxy and creates a tunnel between the two streams.
        /// </summary>
        /// <param name="responseStream">The stream to write the SOCKS proxy's response to.</param>
        /// <param name="destHost">The destination host.</param>
        /// <param name="destPort">The destination port.</param>
        private void TunnelRequest(NetworkStream responseStream, string destHost, int destPort)
        {
            CopyStringToStream("HTTP/1.1 200 Tunnel established\r\nProxy-Connection: Keep-Alive\r\n\r\n", responseStream);

            using (var client = RemoteProxy.CreateProxy().CreateConnection(destHost, destPort))
            using (var stream = client.GetStream())
            {
                stream.ReadTimeout = 100;

                while (true)
                {
                    try
                    {
                        if (responseStream.DataAvailable)
                        {
                            CopyStreamToStream(responseStream, stream);
                        }

                        if (stream.DataAvailable)
                        {
                            CopyStreamToStream(stream, responseStream);
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Copies the first stream's content from the current position to the second stream.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="destionation">The destionation stream.</param>
        /// <param name="flush">if set to <c>true</c>, <c>Flush()</c> will be called on the destination stream after finish.</param>
        /// <param name="bufferLength">Length of the buffer.</param>
        public static void CopyStreamToStream(Stream source, Stream destionation, bool flush = true, int bufferLength = 4096)
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

            if (flush)
            {
                destionation.Flush();
            }
        }

        /// <summary>
        /// Copies the specified byte array to the destination stream.
        /// </summary>
        /// <param name="source">The source byte array.</param>
        /// <param name="destionation">The destionation stream.</param>
        /// <param name="flush">if set to <c>true</c>, <c>Flush()</c> will be called on the destination stream after finish.</param>
        public static void CopyBytesToStream(byte[] source, Stream destionation, bool flush = true)
        {
            destionation.Write(source, 0, source.Length);

            if (flush)
            {
                destionation.Flush();
            }
        }

        /// <summary>
        /// Copies the specified string to the destination stream.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="destionation">The destionation stream.</param>
        /// <param name="flush">if set to <c>true</c>, <c>Flush()</c> will be called on the destination stream after finish.</param>
        /// <param name="encoding">The encoding to use for conversion.</param>
        public static void CopyStringToStream(string source, Stream destionation, bool flush = true, Encoding encoding = null)
        {
            CopyBytesToStream((encoding ?? Encoding.UTF8).GetBytes(source), destionation, flush);
        }

        /// <summary>
        /// Represents a proxy.
        /// </summary>
        public class Proxy
        {
            /// <summary>
            /// Gets or sets the host name.
            /// </summary>
            /// <value>
            /// The host name.
            /// </value>
            public string Host { get; set; }

            /// <summary>
            /// Gets or sets the port number.
            /// </summary>
            /// <value>
            /// The port number.
            /// </value>
            public int Port { get; set; }

            /// <summary>
            /// Gets or sets the proxy type.
            /// </summary>
            /// <value>
            /// The proxy type.
            /// </value>
            public ProxyType Type { get; set; }

            /// <summary>
            /// Gets or sets the name of the user.
            /// </summary>
            /// <value>
            /// The name of the user.
            /// </value>
            public string UserName { get; set; }

            /// <summary>
            /// Gets or sets the password.
            /// </summary>
            /// <value>
            /// The password.
            /// </value>
            public string Password { get; set; }

            /// <summary>
            /// Parses the proxy URL and creates a new <c>Proxy</c> object.
            /// </summary>
            /// <param name="uri">The proxy's URI.</param>
            /// <returns>
            /// The parsed <c>Proxy</c> object.
            /// </returns>
            public static Proxy ParseUri(Uri uri)
            {
                var proxy = new Proxy();

                proxy.Host = uri.Host;
                proxy.Port = uri.Port;

                ProxyType type;
                if (ProxyType.TryParse(uri.Scheme, true, out type))
                {
                    proxy.Type = type;
                }

                if (!string.IsNullOrEmpty(uri.UserInfo) && uri.UserInfo.IndexOf(':') != -1)
                {
                    var auth = uri.UserInfo.Split(":".ToCharArray(), 2);

                    proxy.UserName = auth[0];
                    proxy.Password = auth[1];
                }

                return proxy;
            }

            /// <summary>
            /// Creates the proxy from the values within this object.
            /// </summary>
            /// <returns>
            /// The proxy.
            /// </returns>
            public IProxyClient CreateProxy()
            {
                var pcf = new ProxyClientFactory();

                if (string.IsNullOrWhiteSpace(UserName))
                {
                    return pcf.CreateProxyClient(Type, Host, Port);
                }

                return pcf.CreateProxyClient(Type, Host, Port, UserName, Password ?? string.Empty);
            }

            /// <summary>
            /// Performs an implicit conversion from <see cref="RoliSoft.TVShowTracker.HttpToSocks.Proxy"/> to <see cref="System.Net.WebProxy"/>.
            /// </summary>
            /// <param name="proxy">The proxy.</param>
            /// <returns>
            /// The result of the conversion.
            /// </returns>
            public static implicit operator WebProxy(Proxy proxy)
            {
                return new WebProxy(proxy.Host + ":" + proxy.Port);
            }
        }
    }
}
