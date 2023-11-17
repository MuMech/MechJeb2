using System;
using System.Threading;
using System.Threading.Tasks;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Utils
{
    // TODO:
    //  - cancellation
    //  - timeout
    public abstract class BackgroundJob<T>
    {
        private Task<T>? _task;
        public  T        Result = default!;
        public  bool     ResultReady;

        protected abstract T Run(object? o);

        public BackgroundJob()
        {
            _executeDelegate         = Run;
            _onTaskCompletedDelegate = OnTaskCompleted;
            _onTaskFaultedDelegate   = OnTaskFaulted;
            _onTaskCancelledDelegate = OnTaskCancelled;
        }

        public void Cancel() => throw new NotImplementedException();

        public bool IsRunning()
        {
            if (_task is null)
                return false;

            return _task.IsCompleted == false;
        }

        protected virtual void OnTaskCompleted(Task<T> task)
        {
            Result      = task.Result;
            ResultReady = true;
            _task       = null;
        }

        protected virtual void OnTaskFaulted(Task<T> task)
        {
            Result      = default!;
            ResultReady = false;
            _task       = null;
            Print($"Exception in {GetType()}: {task.Exception}");
        }

        protected virtual void OnTaskCancelled(Task<T> task) => _task = null;

        private readonly Func<object?, T> _executeDelegate;
        private readonly Action<Task<T>>  _onTaskCompletedDelegate;
        private readonly Action<Task<T>>  _onTaskFaultedDelegate;
        private readonly Action<Task<T>>  _onTaskCancelledDelegate;

        public void RunSync(object? o)
        {
            Result      = _executeDelegate(o);
            ResultReady = true;
        }

        public void StartJob(object? o)
        {
            ResultReady = false;
            _task = Task.Factory.StartNew(_executeDelegate, o, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _task.ContinueWith(_onTaskCompletedDelegate, TaskContinuationOptions.OnlyOnRanToCompletion);
            _task.ContinueWith(_onTaskFaultedDelegate, TaskContinuationOptions.OnlyOnFaulted);
            _task.ContinueWith(_onTaskCancelledDelegate, TaskContinuationOptions.OnlyOnCanceled);
        }
    }
}
