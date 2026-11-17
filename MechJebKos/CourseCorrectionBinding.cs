/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:MANEUVERPLANNER:COURSECORRECTION - fine tune the closest approach to the target.
    //
    // For a celestial target: the arrival PERIAPSIS (and INCLINATION when INCLINATIONFLAG), with the burn at TIMEREF (COMPUTED lets
    // MechJeb choose). For any other target: the cheapest burn to pass INTERCEPTDISTANCE from it; TIMEREF is not used then.
    [KOSNomenclature("MechJebCourseCorrection")]
    public class CourseCorrectionBinding : OperationBinding<OperationCourseCorrection>
    {
        public CourseCorrectionBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("PERIAPSIS", new SetSuffix<ScalarValue>(() => Op.Periapsis.Val, value => Op.Periapsis.Val = value,
                "Approximate final periapsis altitude in meters at a celestial target."));
            AddSuffix("INCLINATIONFLAG", new SetSuffix<BooleanValue>(() => Op.InclinationFlag, value => Op.InclinationFlag = value,
                "Also target INCLINATION at a celestial target."));
            AddSuffix("INCLINATION", new SetSuffix<ScalarValue>(() => Op.Inclination.Val, value => Op.Inclination.Val = value,
                "Final inclination in degrees at a celestial target, used when INCLINATIONFLAG is true."));
            AddSuffix("INTERCEPTDISTANCE", new SetSuffix<ScalarValue>(() => Op.InterceptDistance.Val, value => Op.InterceptDistance.Val = value,
                "Closest approach distance in meters to a target that is not a celestial body."));
        }
    }
}
