Dispatcher
===

`Dispatcher` is an incredibly simple little component/system that makes it possible to invoke code on the main thread from other threads. Usage is simple:

1. Add the `Dispatcher` component to an object in your scene.
2. Call the static methods `InvokeAsync(Action)` or `Invoke(Action)` to run actions on the main thread.

`InvokeAsync` will queue up the action and return immediately so your thread can keep on going. `Invoke`, on the other hand, will block the calling thread until the action has run. In both cases the `Dispatcher` will immediately invoke the action if the calling thread is the main thread (this also prevents deadlocks in case you call `Invoke` from the main thread).

`InvokeAsync` will not generate any garbage because it simply queues up the action. `Invoke` currently allocates a new lambda that will invoke the given argument and set a boolean to know when to return from the method. This could be fixed later, but most places are likely fine using `InvokeAsync`.
