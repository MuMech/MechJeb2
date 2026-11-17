/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;

namespace MechJebLib.FuelFlowSimulation
{
    /// <summary>
    ///     Lines up the segments of the atmosphere and vacuum runs, so that one index means the same segment in both lists.
    ///     A run starts a segment whenever the number of running engines changes, and the two runs need not split a stage the
    ///     same way: jets don't run in vacuum, so a stage of jets and a rocket can have two segments in the atmosphere and one in
    ///     vacuum. Where one list has a segment the other lacks, a bookkeeping segment with no Δv, no burn time and no engines is
    ///     put in the other list. Nothing is merged or removed, so each list's sums stay the same.
    /// </summary>
    public class SegmentReconciler
    {
        private readonly List<FuelStats> _atmoOut = new List<FuelStats>();
        private readonly List<FuelStats> _vacOut  = new List<FuelStats>();

        /// <summary>
        ///     Both lists hold the segments latest first, as the simulation leaves them. On return they have the same length, and
        ///     the segments at one index are of the same KSP stage.
        /// </summary>
        public void Reconcile(List<FuelStats> atmo, List<FuelStats> vac)
        {
            _atmoOut.Clear();
            _vacOut.Clear();

            // walk both lists in the order the segments burn, one KSP stage at a time, from the highest stage down
            int i = atmo.Count - 1;
            int j = vac.Count - 1;

            while (i >= 0 || j >= 0)
            {
                int stage = int.MinValue;
                if (i >= 0) stage = atmo[i].KSPStage;
                if (j >= 0 && vac[j].KSPStage > stage) stage = vac[j].KSPStage;

                int iEnd = i;
                while (iEnd >= 0 && atmo[iEnd].KSPStage == stage) iEnd--;
                int jEnd = j;
                while (jEnd >= 0 && vac[jEnd].KSPStage == stage) jEnd--;

                ReconcileStage(stage, atmo, i, iEnd, vac, j, jEnd);

                i = iEnd;
                j = jEnd;
            }

            // back to latest first
            _atmoOut.Reverse();
            _vacOut.Reverse();

            atmo.Clear();
            atmo.AddRange(_atmoOut);
            vac.Clear();
            vac.AddRange(_vacOut);

            _atmoOut.Clear();
            _vacOut.Clear();
        }

        // one KSP stage: atmo[i] down to atmo[iEnd + 1] and vac[j] down to vac[jEnd + 1], in the order they burn. Each run's
        // stage starts with the engines its staging lit, so the first segments pair; a later one pairs with the other run's next
        // segment when one set of engines holds the other, and gets a bookkeeping partner when it is the other run's next
        // segment that pairs further on.
        private void ReconcileStage(int stage, List<FuelStats> atmo, int i, int iEnd, List<FuelStats> vac, int j, int jEnd)
        {
            FuelStats? lastAtmo = null;
            FuelStats? lastVac  = null;

            while (i > iEnd && j > jEnd)
            {
                FuelStats a = atmo[i];
                FuelStats v = vac[j];

                if (!Compatible(a, v))
                {
                    if (PairsLater(v, atmo, i - 1, iEnd))
                    {
                        Add(a, Bookkeeping(stage, lastVac, v));
                        lastAtmo = a;
                        i--;
                        continue;
                    }

                    if (PairsLater(a, vac, j - 1, jEnd))
                    {
                        Add(Bookkeeping(stage, lastAtmo, a), v);
                        lastVac = v;
                        j--;
                        continue;
                    }
                }

                // they pair, or neither pairs with anything further on: keep them side by side
                Add(a, v);
                lastAtmo = a;
                lastVac  = v;
                i--;
                j--;
            }

            for (; i > iEnd; i--)
            {
                Add(atmo[i], Bookkeeping(stage, lastVac, atmo[i]));
                lastAtmo = atmo[i];
            }

            for (; j > jEnd; j--)
            {
                Add(Bookkeeping(stage, lastAtmo, vac[j]), vac[j]);
                lastVac = vac[j];
            }
        }

        private void Add(FuelStats atmo, FuelStats vac)
        {
            _atmoOut.Add(atmo);
            _vacOut.Add(vac);
        }

        private static bool PairsLater(FuelStats s, List<FuelStats> other, int from, int end)
        {
            for (int k = from; k > end; k--)
                if (Compatible(s, other[k]))
                    return true;
            return false;
        }

        // the same engines as far as both runs run them: one set holds the other (the vacuum run has no jets). Two segments
        // with nothing running pair too.
        private static bool Compatible(FuelStats a, FuelStats b)
        {
            if (a.EngineSignatures.Count == 0 || b.EngineSignatures.Count == 0)
                return a.EngineSignatures.Count == b.EngineSignatures.Count;

            return Holds(a, b) || Holds(b, a);
        }

        private static bool Holds(FuelStats outer, FuelStats inner)
        {
            if (inner.EngineSignatures.Count > outer.EngineSignatures.Count)
                return false;

            foreach (EngineSignature s in inner.EngineSignatures)
            {
                bool found = false;
                foreach (EngineSignature o in outer.EngineSignatures)
                {
                    if (s != o) continue;
                    found = true;
                    break;
                }

                if (!found)
                    return false;
            }

            return true;
        }

        // a segment of the stage with nothing burning, at the mass its own run has at that point: the end of that run's
        // previous segment in the stage, or with none, the start of the partner segment
        private static FuelStats Bookkeeping(int stage, FuelStats? previous, FuelStats partner)
        {
            if (previous == null)
                return new FuelStats
                {
                    KSPStage = stage, StartMass = partner.StartMass, EndMass = partner.StartMass, StartTime = partner.StartTime,
                    ControllableMass = partner.ControllableMass
                };

            return new FuelStats
            {
                KSPStage         = stage,
                StartMass        = previous.EndMass,
                EndMass          = previous.EndMass,
                StartTime        = previous.StartTime + previous.DeltaTime,
                ControllableMass = previous.ControllableMass
            };
        }
    }
}
