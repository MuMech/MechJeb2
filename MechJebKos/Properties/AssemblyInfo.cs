/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

// KSP load-order dependencies. The (major, minor) pair is a *minimum* version requirement;
// KSP refuses to load this assembly unless a matching-or-newer KSPAssembly is present.
[assembly: KSPAssemblyDependency("kOS", 1, 6)]
[assembly: KSPAssemblyDependency("MechJeb2", 2, 16)]
