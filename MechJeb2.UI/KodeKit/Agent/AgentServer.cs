using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

namespace KodeKit.Agent
{
    /// <summary>
    /// UiAgent over HTTP on 127.0.0.1 only, for an agent outside the game (an MCP server, a test script, curl). Off unless
    /// started; the host starts it when KSP is launched with <c>-mechjeb-ui-agent &lt;port&gt;</c> (or <c>-mechjeb-ui-agent=&lt;port&gt;</c>),
    /// or, failing that, when the environment variable MECHJEB_UI_AGENT_PORT holds a port.
    /// <list type="bullet">
    /// <item>Every request carries the token in an X-Agent-Token header: MECHJEB_UI_AGENT_TOKEN if set (16 characters at least),
    /// else a random one written to PluginData/agent-token next to this DLL, with that folder's permissions (on most installs every
    /// account of the machine can read it), and deleted when the game quits. 127.0.0.1 is reachable by every local account: the token
    /// is what keeps another account's process out.</item>
    /// <item>A request with an Origin header (a web page's) is refused, so a page open in a browser can't drive the game.</item>
    /// <item>GET for snapshot, query and changes; POST for act and undo, at most 64 KB.</item>
    /// <item>A listener thread takes the requests and hands them to <see cref="Workers"/> threads of the server's own (never the
    /// ThreadPool MechJeb computes on); at most <see cref="MaxWaiting"/> more wait for a worker, and one past that is answered 503 at
    /// once. A worker reads the body (at most 5 s, else 408) and waits while the main thread answers it in <see cref="Pump"/> (Unity's objects
    /// are main-thread only).</item>
    /// <item>No host pumping (the main menu, the space centre, a scene loading, the game in the background without "simulate in
    /// background"): a request is answered 503 at once, and those waiting when the host went are dropped. A request not answered in
    /// 15 s is answered 503 and dropped too: it never runs later.</item>
    /// <item>The server stops when the game quits.</item>
    /// </list>
    /// </summary>
    public static class AgentServer
    {
        sealed class Request
        {
            public string Path, Body;
            public Dictionary<string, string> Query;
            public string Response;
            public int State; // 0 waiting, 1 taken by Pump, 2 dropped after the timeout
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
        }

        const int MaxBody = 64 * 1024;

        /// <summary>The threads that read requests and wait for the main thread's answer.</summary>
        public const int Workers = 4;

        /// <summary>The requests that may wait for a worker; one more is answered 503 at once.</summary>
        public const int MaxWaiting = 8;

        static HttpListener listener;
        static BlockingCollection<HttpListenerContext> incoming;
        static readonly ConcurrentQueue<Request> pending = new ConcurrentQueue<Request>();
        static bool quitHooked;

        // when a host last pumped (Stopwatch ticks), and whether it went (its scene torn down): no host, no waiting
        static long lastPump;
        static volatile bool hostGone = true;
        static readonly long PumpGap = System.Diagnostics.Stopwatch.Frequency; // 1 s

        public static bool Running => listener != null && listener.IsListening;
        public static int Port { get; private set; }

        /// <summary>The token a client sends in X-Agent-Token.</summary>
        public static string Token { get; private set; }

        /// <summary>Where the token was written (null when it came from the environment).</summary>
        public static string TokenFile { get; private set; }

        /// <summary>The launch argument that turns the server on: <c>-mechjeb-ui-agent 8765</c> or <c>-mechjeb-ui-agent=8765</c>.</summary>
        public const string LaunchArgument = "-mechjeb-ui-agent";

        /// <summary>
        /// Starts the server when KSP was launched with <see cref="LaunchArgument"/> and a port, or else when MECHJEB_UI_AGENT_PORT
        /// holds one. A port that isn't a number in 1-65535 is logged and the server stays off.
        /// </summary>
        public static void StartFromLaunch()
        {
            if (Running) return;
            string p = PortArgument(Environment.GetCommandLineArgs()) ?? Environment.GetEnvironmentVariable("MECHJEB_UI_AGENT_PORT");
            if (string.IsNullOrEmpty(p)) return;
            if (int.TryParse(p, out int port) && port > 0 && port < 65536) Start(port);
            else Debug.LogWarning("[KodeKit] agent server: \"" + p + "\" is not a port; the server stays off");
        }

        /// <summary>The port given after <see cref="LaunchArgument"/> ("-arg 8765" or "-arg=8765"); null when the argument isn't there.</summary>
        public static string PortArgument(IList<string> args)
        {
            for (int i = 0; i < args.Count; i++)
            {
                string a = args[i];
                if (string.Equals(a, LaunchArgument, StringComparison.OrdinalIgnoreCase)) return i + 1 < args.Count ? args[i + 1] : "";
                if (a.StartsWith(LaunchArgument + "=", StringComparison.OrdinalIgnoreCase)) return a.Substring(LaunchArgument.Length + 1);
            }
            return null;
        }

        public static void Start(int port)
        {
            if (Running) return;
            try
            {
                MakeToken();
                listener = new HttpListener();
                listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
                listener.Start();
                Port = port;
                UiAgent.Listen();
                incoming = new BlockingCollection<HttpListenerContext>(new ConcurrentQueue<HttpListenerContext>(), MaxWaiting);
                new Thread(Listen) { IsBackground = true, Name = "KodeKit agent server" }.Start();
                for (int i = 0; i < Workers; i++) new Thread(Work) { IsBackground = true, Name = "KodeKit agent worker " + i }.Start(incoming);
                if (!quitHooked)
                {
                    quitHooked = true;
                    Application.quitting += Stop;
                }
                Debug.Log("[KodeKit] agent server on http://127.0.0.1:" + port + "/ (snapshot, query, changes, act, undo), token " +
                          (TokenFile != null ? "in " + TokenFile : "from MECHJEB_UI_AGENT_TOKEN"));
            }
            catch (Exception e)
            {
                Debug.LogError("[KodeKit] agent server could not start on port " + port + ": " + e.Message);
                listener = null;
            }
        }

        static void MakeToken()
        {
            string env = Environment.GetEnvironmentVariable("MECHJEB_UI_AGENT_TOKEN");
            if (!string.IsNullOrEmpty(env))
            {
                if (env.Length < 16) throw new ArgumentException("MECHJEB_UI_AGENT_TOKEN must have 16 characters at least");
                Token = env;
                TokenFile = null;
                return;
            }
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            Token = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            string dir = Path.Combine(Path.GetDirectoryName(typeof(AgentServer).Assembly.Location) ?? ".", "PluginData");
            Directory.CreateDirectory(dir);
            TokenFile = Path.Combine(dir, "agent-token");
            File.WriteAllText(TokenFile, Token);
        }

        public static void Stop()
        {
            try
            {
                listener?.Stop();
                listener?.Close();
            }
            catch (Exception)
            {
                // closing
            }
            listener = null;
            // the token dies with the server: a stale file would let a client think the game still listens
            try
            {
                if (TokenFile != null && File.Exists(TokenFile)) File.Delete(TokenFile);
            }
            catch (Exception)
            {
                // read-only install: the next launch writes a new token anyway
            }
            incoming?.CompleteAdding();
            incoming = null;
            HostGone();
            UiAgent.StopListening();
        }

        /// <summary>
        /// The host's scene is torn down (KodeHost destroyed): requests waiting for the main thread are answered 503 and dropped, and new
        /// ones are answered 503 at once until a host pumps again.
        /// </summary>
        public static void HostGone()
        {
            hostGone = true;
            while (pending.TryDequeue(out Request r))
                if (Interlocked.CompareExchange(ref r.State, 2, 0) == 0)
                    r.Done.Set();
        }

        static void Listen()
        {
            while (Running)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = listener.GetContext();
                }
                catch (Exception)
                {
                    return; // stopped
                }
                BlockingCollection<HttpListenerContext> queue = incoming;
                bool taken;
                try
                {
                    taken = queue != null && queue.TryAdd(ctx);
                }
                catch (InvalidOperationException)
                {
                    taken = false; // stopping
                }
                if (taken) continue;
                try
                {
                    Answer(ctx, 503, Err("busy: " + (Workers + MaxWaiting) + " requests already wait; send fewer at once"));
                }
                catch (Exception)
                {
                    // the client went
                }
            }
        }

        static void Work(object queue)
        {
            var q = (BlockingCollection<HttpListenerContext>)queue;
            try
            {
                foreach (HttpListenerContext ctx in q.GetConsumingEnumerable()) Serve(ctx);
            }
            catch (Exception)
            {
                // stopped
            }
        }

        static bool HostPumping => !hostGone && System.Diagnostics.Stopwatch.GetTimestamp() - Interlocked.Read(ref lastPump) < PumpGap;

        // the body, read for at most 5 s: a client that announces a body and doesn't send it holds no worker for good. Null: timed out
        static byte[] ReadBody(HttpListenerRequest q, out int n)
        {
            var buf = new byte[MaxBody + 1];
            n = 0;
            Stream s = q.InputStream;
            long end = System.Diagnostics.Stopwatch.GetTimestamp() + 5 * System.Diagnostics.Stopwatch.Frequency;
            while (n < buf.Length)
            {
                int left = (int)((end - System.Diagnostics.Stopwatch.GetTimestamp()) * 1000 / System.Diagnostics.Stopwatch.Frequency);
                if (left <= 0) return null;
                IAsyncResult ar = s.BeginRead(buf, n, buf.Length - n, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(left)) return null;
                int k = s.EndRead(ar);
                if (k <= 0) break;
                n += k;
            }
            return buf;
        }

        static void Answer(HttpListenerContext ctx, int status, string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            ctx.Response.ContentType = "application/json; charset=utf-8";
            ctx.Response.StatusCode = status;
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }

        static string Err(string m) => new JsonWriter().Open().Prop("version", UiAgent.Version).Prop("error", m).Close().ToString();

        static void Serve(HttpListenerContext ctx)
        {
            try
            {
                HttpListenerRequest q = ctx.Request;
                if (q.Headers["Origin"] != null)
                {
                    Answer(ctx, 403, Err("requests from web pages are refused"));
                    return;
                }
                if (q.Headers["X-Agent-Token"] != Token)
                {
                    Answer(ctx, 403, Err("missing or wrong X-Agent-Token"));
                    return;
                }
                string path = q.Url.AbsolutePath.TrimEnd('/');
                bool act = path == "/act" || path == "/undo";
                if (act ? q.HttpMethod != "POST" : q.HttpMethod != "GET")
                {
                    Answer(ctx, 405, Err((act ? "POST" : "GET") + " only for " + path));
                    return;
                }
                if (q.ContentLength64 > MaxBody)
                {
                    Answer(ctx, 413, Err("a body of at most " + MaxBody + " bytes"));
                    return;
                }
                var r = new Request { Path = path, Query = new Dictionary<string, string>() };
                foreach (string k in q.QueryString.AllKeys)
                    if (k != null) r.Query[k] = q.QueryString[k];
                if (q.HasEntityBody)
                {
                    byte[] buf = ReadBody(q, out int n);
                    if (buf == null)
                    {
                        // the body didn't come: answered and dropped, nothing runs (the connection closes with the answer)
                        Answer(ctx, 408, Err("the body did not arrive within 5 s; the request was dropped"));
                        return;
                    }
                    if (n > MaxBody)
                    {
                        Answer(ctx, 413, Err("a body of at most " + MaxBody + " bytes"));
                        return;
                    }
                    r.Body = Encoding.UTF8.GetString(buf, 0, n);
                }
                if (!HostPumping)
                {
                    Answer(ctx, 503, Err("no window host is running (the main menu, the space centre, a scene loading, or the game in the background); try again in flight or the editor"));
                    return;
                }
                pending.Enqueue(r);
                if (!r.Done.Wait(TimeSpan.FromSeconds(15)) && Interlocked.CompareExchange(ref r.State, 2, 0) == 0)
                {
                    // dropped before Pump took it: an act that timed out must not run in the next scene
                    Answer(ctx, 503, Err("the game did not answer in 15 s (paused or loading?); the request was dropped"));
                    return;
                }
                r.Done.Wait(); // taken (answered), or dropped when the host went
                if (r.State == 2)
                {
                    Answer(ctx, 503, Err("the scene ended before the game answered; the request was dropped"));
                    return;
                }
                Answer(ctx, 200, r.Response);
            }
            catch (Exception e)
            {
                // before an answer (a body that doesn't read, a client gone): close the connection rather than leave it open
                try
                {
                    ctx.Response.Abort();
                }
                catch (Exception)
                {
                    // already closed
                }
                Debug.LogWarning("[KodeKit] agent request failed: " + e.Message);
            }
        }

        /// <summary>Answers the waiting requests; call once a frame on the main thread (the change feed records itself, by event).</summary>
        public static void Pump()
        {
            Interlocked.Exchange(ref lastPump, System.Diagnostics.Stopwatch.GetTimestamp());
            hostGone = false;
            while (pending.TryDequeue(out Request r))
            {
                if (Interlocked.CompareExchange(ref r.State, 1, 0) != 0) continue; // dropped after its timeout
                r.Response = UiAgent.Handle(r.Path, r.Query, r.Body);
                r.Done.Set();
            }
        }
    }
}
