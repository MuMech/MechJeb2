Future
===

Futures are a common pattern in many software projects and you can read more at [http://en.wikipedia.org/wiki/Futures_and_promises](http://en.wikipedia.org/wiki/Futures_and_promises). Typically in C# you'd use the [System.Threading.Tasks](http://msdn.microsoft.com/en-us/library/vstudio/system.threading.tasks(v=vs.110).aspx) namespace, but Unity is using a woefully out of date version of Mono that lacks this functionality. `Future.cs` provides an extremely simple implementation that works for basic use cases.

The core of the system for game code is the `IFuture<T>` interface. The interface provides the user with the state of the future, the value (if one was retrieved), an exception (if an error occurred while retrieving the value), and the ability to register callbacks for completion.

The file also provides an implementation of `IFuture<T>` in `Future<T>`. Game code that is _consuming_ futures should always use the interface. Game code that is _creating_ futures can either implement the interface themselves or use the included implementation. The included implementation provides a `Process(Func<T>)` method that simply uses the `ThreadPool` class to execute the logic.

The `Future<T>` implementation of using delegates is not generally going to be garbage free, however the futures are generally used for things like web resources or game saves which almost always require some allocation of memory to function (such as creating stream writers or readers).

Additionally `Future<T>` does depend on the [Dispatcher](https://github.com/nickgravelyn/UnityToolbag/tree/master/Dispatcher) class from UnityToolbag (to ensure callbacks are invoked on the main game thread), so you must either include that class in your project or remove/change the `Future<T>` implementation.
