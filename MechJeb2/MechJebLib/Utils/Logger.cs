/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System.Collections.Concurrent;

namespace MechJebLib.Utils
{
    public interface ILogger
    {
        void Log(string message);
    }

    public class NullLogger : ILogger
    {
        public void Log(string message) { }
    }
    
    /// <summary>
    /// Simple, mostly threadsafe logging utility.  There needs to be initializtion code
    /// that calls Register() and injects a concrete logger while also creating the singleton
    /// (singleton creation is not threadsafe), then every tick the main thread (and only the
    /// main threda) needs to call Drain().  Then just call Log() from whatever thread.
    /// </summary>
    public class Logger
    {
        private Logger() { }
        
        private static Logger _instance { get; } = new Logger();

        private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();

        private ILogger _logger = new NullLogger();

        private void LogImpl(string message)
        {
            _messages.Enqueue(message);
        }

        private void DrainImpl()
        {
            while (_messages.TryDequeue(out string message))
            {
                _logger.Log(message);
            }
        }
        
        public static void Register(ILogger logger)
        {
            _instance._logger = logger;
        }
        
        // Log is threadsafe and just adds to a queue to be drained later by the main thread
        public static void Log(string message)
        {
            _instance.LogImpl(message);
        }

        // Drain needs to be called only from the main Unity/KSP thread in MechJebCore, not from any threads
        public static void Drain()
        {
            _instance.DrainImpl();
        }
    }
}
