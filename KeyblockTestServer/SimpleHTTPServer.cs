// MIT License - Copyright (c) 2016 Can Güney Aksakalli
// https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html

using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading;
using log4net;

namespace KeyblockTestServer
{
    class SimpleHttpServer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SimpleHttpServer));

        private readonly string[] _indexFiles =
        {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        private static readonly IDictionary<string, string> MimeTypeMappings =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                #region extension to MIME type list

                {".asf", "video/x-ms-asf"},
                {".asx", "video/x-ms-asf"},
                {".avi", "video/x-msvideo"},
                {".bin", "application/octet-stream"},
                {".cco", "application/x-cocoa"},
                {".crt", "application/x-x509-ca-cert"},
                {".css", "text/css"},
                {".deb", "application/octet-stream"},
                {".der", "application/x-x509-ca-cert"},
                {".dll", "application/octet-stream"},
                {".dmg", "application/octet-stream"},
                {".ear", "application/java-archive"},
                {".eot", "application/octet-stream"},
                {".exe", "application/octet-stream"},
                {".flv", "video/x-flv"},
                {".gif", "image/gif"},
                {".hqx", "application/mac-binhex40"},
                {".htc", "text/x-component"},
                {".htm", "text/html"},
                {".html", "text/html"},
                {".ico", "image/x-icon"},
                {".img", "application/octet-stream"},
                {".iso", "application/octet-stream"},
                {".jar", "application/java-archive"},
                {".jardiff", "application/x-java-archive-diff"},
                {".jng", "image/x-jng"},
                {".jnlp", "application/x-java-jnlp-file"},
                {".jpeg", "image/jpeg"},
                {".jpg", "image/jpeg"},
                {".js", "application/x-javascript"},
                {".mml", "text/mathml"},
                {".mng", "video/x-mng"},
                {".mov", "video/quicktime"},
                {".mp3", "audio/mpeg"},
                {".mpeg", "video/mpeg"},
                {".mpg", "video/mpeg"},
                {".msi", "application/octet-stream"},
                {".msm", "application/octet-stream"},
                {".msp", "application/octet-stream"},
                {".pdb", "application/x-pilot"},
                {".pdf", "application/pdf"},
                {".pem", "application/x-x509-ca-cert"},
                {".pl", "application/x-perl"},
                {".pm", "application/x-perl"},
                {".png", "image/png"},
                {".prc", "application/x-pilot"},
                {".ra", "audio/x-realaudio"},
                {".rar", "application/x-rar-compressed"},
                {".rpm", "application/x-redhat-package-manager"},
                {".rss", "text/xml"},
                {".run", "application/x-makeself"},
                {".sea", "application/x-sea"},
                {".shtml", "text/html"},
                {".sit", "application/x-stuffit"},
                {".swf", "application/x-shockwave-flash"},
                {".tcl", "application/x-tcl"},
                {".tk", "application/x-tcl"},
                {".txt", "text/plain"},
                {".war", "application/java-archive"},
                {".wbmp", "image/vnd.wap.wbmp"},
                {".wmv", "video/x-ms-wmv"},
                {".xml", "text/xml"},
                {".xpi", "application/x-xpinstall"},
                {".zip", "application/zip"},

                #endregion
            };

        private Thread _serverThread;
        private string _rootDirectory;
        private HttpListener _listener;
        private int _port;
        private string _hostname = "*";
        private readonly string _fallbackServer;

        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="host">Hostname of the server.</param>
        /// <param name="port">Port of the server.</param>
        /// <param name="fallback">Fallback webserver to query if file is not found</param>
        public SimpleHttpServer(string path, string host,  int port, string fallback)
        {
            _fallbackServer = fallback;
            Initialize(path, host, port);
        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            _serverThread.Abort();
            _listener.Stop();
        }

        private void Listen()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://{_hostname}:{_port}/");
                _listener.Start();
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Failed to start HTTP file server: {ex.Message}");
                return;
            }
            while (true)
            {
                try
                {
                    var context = _listener.GetContext();
                    Process(context);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Something went wrong when handling a request: {ex.Message}");
                    //ignore
                }
            }
        }

        private void Process(HttpListenerContext context)
        {
            var uri = context.Request.Url.AbsolutePath;
            Logger.Info($"Handle request for: {uri}");
            uri = uri.Substring(1);

            if (string.IsNullOrEmpty(uri))
            {
                foreach (var indexFile in _indexFiles)
                {
                    if (!File.Exists(Path.Combine(_rootDirectory, indexFile))) continue;
                    uri = indexFile;
                    break;
                }
            }

            var filename = Path.Combine(_rootDirectory, uri);

            if (File.Exists(filename))
            {
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open);

                    //Adding permanent http response headers
                    context.Response.ContentType = MimeTypeMappings.TryGetValue(Path.GetExtension(filename), out var mime)
                        ? mime
                        : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified",
                        File.GetLastWriteTime(filename).ToString("r"));

                    var buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    context.Response.StatusCode = (int) HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                    Logger.Info($"Served file from {filename}");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Internal server error: {ex.Message}");
                    context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                }

            }
            else if(!DownloadFromFallbackServer(uri, context))
            {
                Logger.Warn("File not found");
                context.Response.StatusCode = (int) HttpStatusCode.NotFound;
            }

            context.Response.OutputStream.Close();
        }

        private bool DownloadFromFallbackServer(string uri, HttpListenerContext context)
        {
            try
            {
                var fallbackUrl = $"{_fallbackServer}{uri}";
                using (var client = new WebClient())
                {
                    var data = client.DownloadData(fallbackUrl);

                    //Adding permanent http response headers
                    var extension = Path.GetExtension(uri) ?? string.Empty;
                    context.Response.ContentType = MimeTypeMappings.TryGetValue(extension, out var mime)
                        ? mime
                        : "application/octet-stream";
                    context.Response.ContentLength64 = data.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.OutputStream.Write(data,0,data.Length);
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                }
                Logger.Info($"Served file from {fallbackUrl}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to load {uri} from {_fallbackServer}: {ex.Message}");
                return false;
            }
        }

        private void Initialize(string path, string host,  int port)
        {
            _rootDirectory = path;
            _hostname = host;
            _port = port;
            _serverThread = new Thread(Listen);
            _serverThread.Start();
        }


    }
}