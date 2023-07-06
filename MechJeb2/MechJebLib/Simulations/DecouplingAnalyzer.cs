#nullable enable

using System.Collections.Generic;
using MechJebLib.Simulations.PartModules;

namespace MechJebLib.Simulations
{
    public static class DecouplingAnalyzer
    {
        public static void Analyze(SimVessel v)
        {
            // FIXME: this whole algorithm should probably change to better simulate what staging
            // actually does to vessels.  It should work backwards from the current stage and fire
            // decouplers, for everyone one breaking the ship in two and deciding which half is
            // "jettisoned" and which half remains.  The algorithm for determining that should be
            // based on:
            //      1.  which side has more unfired staged decouplers left in it? (keep that side)
            //      2.  if tied, which side has seen more fired decouplers in that direction of the tree? (jettison that side)
            // The first rule optimizes for keeping as many staged parts around as possible, the
            // second rule is at least necessary for the final decoupler to break the 0=0 tie.
            // Doing the bookkeeping quickly will likely be challenging.  But this would eliminate
            // the root part requirement from the algorithm entirely.
            SimPart rootPart = FindRootPart(v.Parts);

            CalculateDecoupledInStageRecursively(v, rootPart, null, -1);
        }

        // BUG: This should at least be fixed to not pick the payload fairings if they are left in stage 0
        // BUG: Does this even do anything?  What part has a inverseStage of -1?
        // BUG: Even if we change this to p.InverseStage <= 0 below what happens with the last decoupler when
        //      it should detatch the payload and so the part decoupled from it should be the root, not the part itself?
        private static SimPart FindRootPart(IReadOnlyList<SimPart> parts)
        {
            return parts[0];
            /*
            SimPart? rootPart = null;

            for (int i = 0; i < parts.Count; i++)
            {
                SimPart p = parts[i];

                if (p.InverseStage < 0 && rootPart != null)
                    rootPart = p;

                p.DecoupledInStage = int.MinValue;
            }

            rootPart ??= parts[0];

            return rootPart;
            */
        }

        private static void CalculateDecoupledInStageRecursively(SimVessel v, SimPart p, SimPart? parent, int inheritedDecoupledInStage)
        {
            int childDecoupledInStage = CalculateDecoupledInStage(v, p, parent, inheritedDecoupledInStage);

            for (int i = 0; i < p.Links.Count; i++)
            {
                if (p.Links[i] == parent)
                    continue;

                CalculateDecoupledInStageRecursively(v, p.Links[i], p, childDecoupledInStage);
            }
        }

        private static int CalculateDecoupledInStage(SimVessel v, SimPart p, SimPart? parent, int parentDecoupledInStage)
        {
            // prevent recursion into already-visited nodes
            if (p.DecoupledInStage != int.MinValue)
                return p.DecoupledInStage;

            for (int i = 0; i < p.Modules.Count; i++)
            {
                SimPartModule m = p.Modules[i];

                switch (m)
                {
                    case SimModuleDecouple decouple:
                        if (decouple is { IsDecoupled: false, StagingEnabled: true } && p.StagingOn)
                        {
                            if (decouple.IsOmniDecoupler)
                            {
                                // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                                p.DecoupledInStage = p.InverseStage;
                                v.PartsDroppedInStage[p.DecoupledInStage].Add(p);
                                return p.DecoupledInStage;
                            }

                            if (decouple.AttachedPart != null)
                            {
                                if (decouple.AttachedPart == parent && decouple.Staged)
                                {
                                    // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                                    p.DecoupledInStage = p.InverseStage;
                                    v.PartsDroppedInStage[p.DecoupledInStage].Add(p);
                                    return p.DecoupledInStage;
                                }

                                // We are still attached to our traversalParent.  The part we decouple is dropped when we decouple.
                                // The part and other children are dropped with the traversalParent.
                                p.DecoupledInStage = parentDecoupledInStage;
                                v.PartsDroppedInStage[p.DecoupledInStage].Add(p);
                                CalculateDecoupledInStageRecursively(v, decouple.AttachedPart, p, p.InverseStage);
                                return p.DecoupledInStage;
                            }
                        }

                        break;
                    case SimModuleDockingNode dockingNode:
                        if (dockingNode.StagingEnabled && p.StagingOn)
                            if (dockingNode.AttachedPart != null)
                            {
                                if (dockingNode.AttachedPart == parent && dockingNode.Staged)
                                {
                                    // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                                    p.DecoupledInStage = p.InverseStage;
                                    v.PartsDroppedInStage[p.DecoupledInStage].Add(p);
                                    return p.DecoupledInStage;
                                }

                                // We are still attached to our traversalParent.  The part we decouple is dropped when we decouple.
                                // The part and other children are dropped with the traversalParent.
                                p.DecoupledInStage = parentDecoupledInStage;
                                v.PartsDroppedInStage[p.DecoupledInStage].Add(p);
                                CalculateDecoupledInStageRecursively(v, dockingNode.AttachedPart, p, p.InverseStage);
                                return p.DecoupledInStage;
                            }

                        break;
                    case SimProceduralFairingDecoupler procFairingDecoupler:
                        if (procFairingDecoupler is { IsDecoupled: false, StagingEnabled: true } && p.StagingOn)
                        {
                            // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                            p.DecoupledInStage = p.InverseStage;
                            v.PartsDroppedInStage[p.DecoupledInStage].Add(p);
                            return p.DecoupledInStage;
                        }

                        break;
                    case SimLaunchClamp _:
                        p.DecoupledInStage = p.InverseStage > parentDecoupledInStage
                            ? p.InverseStage
                            : parentDecoupledInStage;
                        v.PartsDroppedInStage[p.DecoupledInStage].Add(p);
                        return p.DecoupledInStage;
                }
            }

            // not decoupling this part
            p.DecoupledInStage = parentDecoupledInStage;
            v.PartsDroppedInStage[p.DecoupledInStage].Add(p);
            return p.DecoupledInStage;
        }
    }
}
