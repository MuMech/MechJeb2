using System;
using System.Threading;
using System.Threading.Tasks;

namespace MechJebLib.Utils
{
    public abstract class AsyncJob
    {
        public enum JobState
        {
            Ready,
            Running,
            Completed,
            Faulted,
            Cancelled
        }

        public bool IsReady     => State == JobState.Ready;
        public bool IsRunning   => State == JobState.Running;
        public bool IsCompleted => State == JobState.Completed;
        public bool IsFaulted   => State == JobState.Faulted;
        public bool IsCancelled => State == JobState.Cancelled;
        public bool IsFinished  => State >= JobState.Completed;

        private Task?                    _task;
        private CancellationTokenSource? _cts;
        private int                      _state = (int)JobState.Ready;

        public JobState State     => (JobState)Volatile.Read(ref _state);
        public string?  ExceptionMessage { get; private set; }

        protected CancellationToken CancelToken { get; private set; }

        protected abstract void Run(object? o);

        private readonly Action<object?> _runWrapped;

        protected AsyncJob() => _runWrapped = RunWrapped;

        /// <summary>
        /// Attempts to start the job. Returns false if not in Ready state.
        /// </summary>
        public bool TryStartJob(object? o)
        {
            if (Interlocked.CompareExchange(ref _state, (int)JobState.Running, (int)JobState.Ready) != (int)JobState.Ready)
                return false;

            ExceptionMessage = null;
            _cts             = new CancellationTokenSource();
            CancelToken      = _cts.Token;
            _task            = Task.Factory.StartNew(
                _runWrapped,
                o,
                _cts.Token,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
            return true;
        }

        private void RunWrapped(object? o)
        {
            try
            {
                Run(o);
                Interlocked.Exchange(ref _state, (int)JobState.Completed);
            }
            catch (OperationCanceledException)
            {
                Interlocked.Exchange(ref _state, (int)JobState.Cancelled);
            }
            catch (Exception ex)
            {
                ExceptionMessage = $"Exception in {GetType().Name}: {ex}";
                Interlocked.Exchange(ref _state, (int)JobState.Faulted);
            }
        }

        /// <summary>
        /// Consumer calls this after reading results to allow the next job to start.
        /// Returns false if job is still running.
        /// </summary>
        public bool TryMarkReady()
        {
            int current = Volatile.Read(ref _state);
            if (current == (int)JobState.Running)
                return false;

            return Interlocked.CompareExchange(ref _state, (int)JobState.Ready, current) == current;
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        public void CancelAfter(TimeSpan timeout)
        {
            _cts?.CancelAfter(timeout);
        }
    }
}
