using System;
using System.Threading.Tasks;

#nullable enable

namespace MechJebLib.Utils
{
    // TODO:
    //  - cancellation
    //  - timeout
    public abstract class BackgroundJob<T>
    {
        private Task<T>? _task;
        public T        Result = default!;

        protected abstract T Execute();

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public bool IsRunning()
        {
            return _task is not { IsCompleted: true };
        }

        protected virtual void OnTaskCompleted(Task<T> task)
        {
            Result = task.Result;
            _task   = null;
        }

        protected virtual void OnTaskFaulted(Task<T> task)
        {
            _task = null;
        }

        protected virtual void OnTaskCancelled(Task<T> task)
        {
            _task = null;
        }

        public void Run()
        {
            _task = Task.Run(Execute);
            _task.ContinueWith(OnTaskCompleted, TaskContinuationOptions.OnlyOnRanToCompletion);
            _task.ContinueWith(OnTaskFaulted, TaskContinuationOptions.OnlyOnFaulted);
            _task.ContinueWith(OnTaskCancelled, TaskContinuationOptions.OnlyOnCanceled);
        }
    }
}
