using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MechJebLib.Utils
{
    public static class AsyncDevLogger
    {
#if DEBUG
        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private static readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private static Thread? _writerThread;
        private static bool _isRunning;
        private static string _filePath = string.Empty;
#endif

        [Conditional("DEBUG")]
        public static void Initialize(string directoryPath)
        {
#if DEBUG
            if (_isRunning) return;

            Directory.CreateDirectory(directoryPath);
            _filePath = Path.Combine(directoryPath, "MechJebLib_AsyncSimulation.log");
            File.WriteAllText(_filePath, $"=== Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n", Encoding.UTF8);

            _isRunning = true;
            _writerThread = new Thread(ProcessWriteQueue)
            {
                Name = "MechJebLib_FileLogWriter",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _writerThread.Start();
#endif
        }

        [Conditional("DEBUG")]
        public static void Log(string message)
        {
#if DEBUG
            if (!_isRunning) return;

            string timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] [Thread:{Thread.CurrentThread.ManagedThreadId}] {message}";
            _logQueue.Enqueue(timestamped);
            _signal.Set();
#endif
        }

        private static void ProcessWriteQueue()
        {
#if DEBUG
            using (var writer = new StreamWriter(_filePath, true, Encoding.UTF8, 65536))
            {
                while (_isRunning || !_logQueue.IsEmpty)
                {
                    if (_logQueue.IsEmpty && _isRunning)
                    {
                        writer.Flush();
                        _signal.WaitOne(1000);
                    }

                    while (_logQueue.TryDequeue(out string message))
                    {
                        writer.WriteLine(message);
                    }
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void Shutdown()
        {
#if DEBUG
            _isRunning = false;
            _signal.Set();
            _writerThread?.Join(2000);
#endif
        }
    }
}
