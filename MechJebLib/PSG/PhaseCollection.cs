/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG
{
    public class PhaseCollection : List<Phase>
    {
        public PhaseCollection DeepCopy()
        {
            var dup = new PhaseCollection();

            foreach (Phase phase in this)
                dup.Add(phase.DeepCopy());

            return dup;
        }

        public void FixLastShutdownStage()
        {
            int lastShutdownStage = -1;
            for (int i = Count - 1; i >= 0; i--)
            {
                if (!this[i].AllowShutdown || this[i].Coast)
                    continue;

                lastShutdownStage = i;
                break;
            }

            if (lastShutdownStage < 0)
                return;

            Phase phase = this[lastShutdownStage];
            double maxt = phase.Tau / phase.MinThrottle;
            phase.MaxT = maxt;
            phase.MinM = Sqrt(EPS);
            this[lastShutdownStage] = phase;

            if (lastShutdownStage > 0 && phase.MassContinuity)
            {
                // this is the coast before the massContinuity burn
                phase = this[lastShutdownStage - 1];
                phase.MinM = Sqrt(EPS);
                this[lastShutdownStage - 1] = phase;

                if (lastShutdownStage > 1)
                {
                    // this is the burn before the massContinuity coast
                    phase = this[lastShutdownStage - 2];
                    phase.MaxT = maxt;
                    phase.MinM = Sqrt(EPS);
                    this[lastShutdownStage - 2] = phase;
                }
            }
        }
    }
}
