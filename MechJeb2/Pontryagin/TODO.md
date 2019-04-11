
### TODO List

# Critical node executor bugs

* button to get back to old node executor behavior (plus porting old behavior on top of the refactor)
* node executor for small burns warps too close to the node.
* "node executor should not fail back to coasting, should fail back to 5-constraint" (what was the actual bug here though?)
* test multi-stage node execution
* test immediate low-dV node execution

# Near Term Minor Features

* Fairing manager to better support auto-staging of fairings
* Box to force the number of stages used in ascent guidance
* Get "use RCS for ullage" and "prevent unstable ignitions" and "RSS/RO special handling" into the ascent guidance + node executor menus
* "RSS/RO special handling" should leave the throttle on when the ascent guidance is disabled
* Fix "launch to plane" to update the inclination field
* wonky dV countdown

# Near Term Medium Tweaks

* fix bug where the solution keeps running during staging events when there's less than 2 second burn on the upper stage
* transversality conditions maximizing velocity at a target altitude
* optimize the time of the N-1 stage and have the Nth stage burn a fixed time and not be steerable.
* Properly use solution to update guess for costate for the next iteration
* Better reset behavior - copy old solution to PEG controller and use it even when the optimizer thread is hard reset

# Long Term Medium Tweaks

* Atmospheric ISP effects

# Wishlist

* RCS Node executor
* Principia Node executor
* Proper launch-to-rendezvous
* Deep throttling last tick for stock people
* Debugging stock terminal guidance wiggles

