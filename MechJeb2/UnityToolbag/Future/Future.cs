using System;
using System.Collections.Generic;
using System.Threading;

namespace UnityToolbag
{
    /// <summary>
    /// Describes the state of a future.
    /// </summary>
    public enum FutureState
    {
        /// <summary>
        /// The future hasn't begun to resolve a value.
        /// </summary>
        Pending,

        /// <summary>
        /// The future is working on resolving a value.
        /// </summary>
        Processing,

        /// <summary>
        /// The future has a value ready.
        /// </summary>
        Success,

        /// <summary>
        /// The future failed to resolve a value.
        /// </summary>
        Error
    }

    /// <summary>
    /// Defines the interface of an object that can be used to track a future value.
    /// </summary>
    /// <typeparam name="T">The type of object being retrieved.</typeparam>
    public interface IFuture<T>
    {
        /// <summary>
        /// Gets the state of the future.
        /// </summary>
        FutureState state { get; }

        /// <summary>
        /// Gets the value if the State is Success.
        /// </summary>
        T value { get; }

        /// <summary>
        /// Gets the failure exception if the State is Error.
        /// </summary>
        Exception error { get; }

        /// <summary>
        /// Adds a new callback to invoke if the future value is retrieved successfully.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        IFuture<T> OnSuccess(FutureCallback<T> callback);

        /// <summary>
        /// Adds a new callback to invoke if the future has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        IFuture<T> OnError(FutureCallback<T> callback);

        /// <summary>
        /// Adds a new callback to invoke if the future value is retrieved successfully or has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        IFuture<T> OnComplete(FutureCallback<T> callback);
    }

    /// <summary>
    /// Defines the signature for callbacks used by the future.
    /// </summary>
    /// <param name="future">The future.</param>
    public delegate void FutureCallback<T>(IFuture<T> future);

    /// <summary>
    /// An implementation of <see cref="IFuture{T}"/> that can be used internally by methods that return futures.
    /// </summary>
    /// <remarks>
    /// Methods should always return the <see cref="IFuture{T}"/> interface when calling code requests a future.
    /// This class is intended to be constructed internally in the method to provide a simple implementation of
    /// the interface. By returning the interface instead of the class it ensures the implementation can change
    /// later on if requirements change, without affecting the calling code.
    /// </remarks>
    /// <typeparam name="T">The type of object being retrieved.</typeparam>
    public sealed class Future<T> : IFuture<T>
    {
        private volatile FutureState _state;
        private T _value;
        private Exception _error;

        private readonly List<FutureCallback<T>> _successCallbacks = new List<FutureCallback<T>>();
        private readonly List<FutureCallback<T>> _errorCallbacks = new List<FutureCallback<T>>();

        /// <summary>
        /// Gets the state of the future.
        /// </summary>
        public FutureState state { get { return _state; } }

        /// <summary>
        /// Gets the value if the State is Success.
        /// </summary>
        public T value
        {
            get
            {
                if (_state != FutureState.Success) {
                    throw new InvalidOperationException("value is not available unless state is Success.");
                }

                return _value;
            }
        }

        /// <summary>
        /// Gets the failure exception if the State is Error.
        /// </summary>
        public Exception error
        {
            get
            {
                if (_state != FutureState.Error) {
                    throw new InvalidOperationException("error is not available unless state is Error.");
                }

                return _error;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Future{T}"/> class.
        /// </summary>
        public Future()
        {
            _state = FutureState.Pending;
        }

        /// <summary>
        /// Adds a new callback to invoke if the future value is retrieved successfully.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public IFuture<T> OnSuccess(FutureCallback<T> callback)
        {
            if (_state == FutureState.Success) {
                if (Dispatcher.isMainThread) {
                    callback(this);
                }
                else {
                    Dispatcher.InvokeAsync(() => callback(this));
                }
            }
            else if (_state != FutureState.Error && !_successCallbacks.Contains(callback)) {
                _successCallbacks.Add(callback);
            }

            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the future has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public IFuture<T> OnError(FutureCallback<T> callback)
        {
            if (_state == FutureState.Error) {
                if (Dispatcher.isMainThread) {
                    callback(this);
                }
                else {
                    Dispatcher.InvokeAsync(() => callback(this));
                }
            }
            else if (_state != FutureState.Success && !_errorCallbacks.Contains(callback)) {
                _errorCallbacks.Add(callback);
            }

            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the future value is retrieved successfully or has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public IFuture<T> OnComplete(FutureCallback<T> callback)
        {
            if (_state == FutureState.Success || _state == FutureState.Error) {
                if (Dispatcher.isMainThread) {
                    callback(this);
                }
                else {
                    Dispatcher.InvokeAsync(() => callback(this));
                }
            }
            else {
                if (!_successCallbacks.Contains(callback)) {
                    _successCallbacks.Add(callback);
                }
                if (!_errorCallbacks.Contains(callback)) {
                    _errorCallbacks.Add(callback);
                }
            }

            return this;
        }

        /// <summary>
        /// Begins running a given function on a background thread to resolve the future's value, as long
        /// as it is still in the Pending state.
        /// </summary>
        /// <param name="func">The function that will retrieve the desired value.</param>
        public IFuture<T> Process(Func<T> func)
        {
            if (_state != FutureState.Pending) {
                throw new InvalidOperationException("Cannot process a future that isn't in the Pending state.");
            }

            _state = FutureState.Processing;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try {
                    // Directly call the Impl version to avoid the state validation of the public method
                    AssignImpl(func());
                }
                catch (Exception e) {
                    // Directly call the Impl version to avoid the state validation of the public method
                    FailImpl(e);
                }
            });

            return this;
        }

        /// <summary>
        /// Allows manually assigning a value to a future, as long as it is still in the pending state.
        /// </summary>
        /// <remarks>
        /// There are times where you may not need to do background processing for a value. For example,
        /// you may have a cache of values and can just hand one out. In those cases you still want to
        /// return a future for the method signature, but can just call this method to fill in the future.
        /// </remarks>
        /// <param name="value">The value to assign the future.</param>
        public void Assign(T value)
        {
            if (_state != FutureState.Pending) {
                throw new InvalidOperationException("Cannot assign a value to a future that isn't in the Pending state.");
            }

            AssignImpl(value);
        }

        /// <summary>
        /// Allows manually failing a future, as long as it is still in the pending state.
        /// </summary>
        /// <remarks>
        /// As with the Assign method, there are times where you may know a future value is a failure without
        /// doing any background work. In those cases you can simply fail the future manually and return it.
        /// </remarks>
        /// <param name="error">The exception to use to fail the future.</param>
        public void Fail(Exception error)
        {
            if (_state != FutureState.Pending) {
                throw new InvalidOperationException("Cannot fail future that isn't in the Pending state.");
            }

            FailImpl(error);
        }

        private void AssignImpl(T value)
        {
            _value = value;
            _error = null;
            _state = FutureState.Success;

            Dispatcher.InvokeAsync(FlushSuccessCallbacks);
        }

        private void FailImpl(Exception error)
        {
            _value = default(T);
            _error = error;
            _state = FutureState.Error;

            Dispatcher.InvokeAsync(FlushErrorCallbacks);
        }

        private void FlushSuccessCallbacks()
        {
            foreach (var callback in _successCallbacks) {
                callback(this);
            }

            _successCallbacks.Clear();
            _errorCallbacks.Clear();
        }

        private void FlushErrorCallbacks()
        {
            foreach (var callback in _errorCallbacks) {
                callback(this);
            }

            _successCallbacks.Clear();
            _errorCallbacks.Clear();
        }
    }
}
