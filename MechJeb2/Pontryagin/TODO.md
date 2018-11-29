
### TODO List

# Critical bugs

* launch to plane has a singularity at 90 degrees that needs to be fixed
* node executor for small burns warps too close to the node.
* "node executor should not fail back to coasting, should fail back to 5-constraint" (what was the actual bug here though?)
* test multi-stage node execution
* test immediate low-dV node execution
* test launch with 1.13 SLT, pitch start of 35 and pitch rate of 1.0 with 1.41 TWR upper stage -- zero AoA to orbit weirdness

# Critical items before merging to mainline MechJeb

* button to get back to old node executor behavior (plus porting old behavior on top of the refactor)
* display optimizer status
* display optimizer runtime
* display big warning about ISP/thrust in stage stats not matching current stage
* capture optimizer exceptions and log them from the main thread
* Track dV sensed like PEG does rather than counting down tgo
* Reconsider something about fixing the ISP and thrust of the current stage
    * need vacuum max thrust, without ISP curve, without spool up, but subject to testflight failures
    * need accurate mdot based on all consumables (including HTP)
    * TestFlight directly updates g and minthrust/maxthrust on the moduleengines module
    * spool up does not affect maxthrust?
* Debug the fucking fuelflowsimulation for RD-108 / HTP issues

# Near Term Minor Features

* Auto-staging fixes to support hot-staging
* Fairing manager to better support auto-staging of fairings
* Box to force the number of stages used in ascent guidance
* Get "use RCS for ullage" and "prevent unstable ignitions" and "RSS/RO special handling" into the ascent guidance + node executor menus
* "RSS/RO special handling" should leave the throttle on when the ascent guidance is disabled
* Fix "launch to plane" to update the inclination field

# Near Term Medium Tweaks

* full keplerian launch conditions including manual and target and free
* full suborbital launch conditions maximizing velocity at a target altitude
* RCS trim should use the optimizer starting conditions and stop the tick where the residual gets worse
* Properly use solution to update guess for costate for the next iteration
* Better reset behavior - copy old solution to PEG controller and use it even when the optimizer thread is hard reset

# Long Term Medium Tweaks

* Atmospheric ISP effects

# Wishlist

* RCS Node executor
* Principia Node executor
* Proper launch-to-rendezvous
* Deep throttling last tick for stock people

