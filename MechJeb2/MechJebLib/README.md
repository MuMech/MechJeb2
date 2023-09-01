### MechJebLib

A library of core astrodynamics functions rewritten and used by MechJeb.

### Features

* Gooding Lambert Solver
* Shepperd's method of orbit propagation
* Lots of reusable math functions
* An IVP/ODE solver with DP5 and BS3 Runge Kutta implementations
* A rewrite of PVG
* A library of orbital maneuver solvers
* A very permissive license

### Conventions

Unlike KSP there are numerous different conventions in MJLib.

# Radians instead of Degrees

The convention within Unity, KSP and MJ is to use degrees.  This code
in MJLib breaks with this convention and uses radians exclusively.  It
makes writing complex mathematical code substantially easier.

# Right Handed Primitives

The V3, M3 and Q3 primitives are double-precision right handed 3-vectors,
3x3 matricies and quaternions.  This again makes dealing with complex
mathematical algorithms substantially easier and avoids tracking down
annoying handedness issues.  Vectors passed into MJLib should also be
"derotated" and recentered so that they are in proper BCI coordinate
systems.

# No Linking to KSP or Unity

This keeps the library fully self-contained and portable.

* No use of the KSP Orbit class, everything is typically done with state
  vectors and propagation by Shepperd's method.
* No use of Debug.Log, the logger gets dependency injected by the
  MechJebCore class
* This keeps MJLib testable by avoiding the problem of linking against
  KSP/Unity pulling in the Unityplayer and that getting into fights
  with the test runner class.  KSP Mods have a difficult problem with
  tests since they link against the shipped KSP binaries.

There is an exception to this rule where the VesselBuilder/VesselUpdater
used in the FuelFlowSimulation links to KSP/Unity, but there's also a
comment there about how that needs to be moved to an auxiliary dll of
glue code in between MJLib and KSP/Unity.  Generally it is more the
responsibility of MJ itself to convert between KSP/Unity and MJLib.

# NED (North, East, Down) and RPY conventions for orientation

KSP used a crazy convention where the rocket standing on the pad has an
'up' orientation which is the pointy end.  The 'forward' end is usually
pointing across the water from KSC and down points south.  This is all
due ultimately to Unity's left handed coordinate systems along with the
choice to orient rockets like they are standing on the pad.

The V3.forward/up/right vectors are very different from Unity and KSP and
correspond to NED (which means that V3.up is actually 0,0,-1).  These
correspond, though, to the axes of the RPY Euler angle rotations and are
correct for dealing with spacecraft/aircraft.

# Tests

Tests are good, and there still aren't enough tests here.  I was able to
completely rewrite the ODE integrator in PVG and ship it without firing
up KSP and without breaking anyone because I had test coverage.

