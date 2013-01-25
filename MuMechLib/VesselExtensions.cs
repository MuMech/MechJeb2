using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public static class VesselExtensions
    {
        public static List<T> GetParts<T>(this Vessel vessel) where T : Part
        {
            return vessel.Parts.FindAll(a => a.GetType() == typeof(T)).Cast<T>().ToList<T>();
        }

        public static List<T> GetModules<T>(this Vessel vessel) where T : PartModule
        {
            return (from modules in vessel.Parts from module in modules.Modules.OfType<T>() select module).Cast<T>().ToList<T>();
        }

        public static MechJebCore GetMasterMechJeb(this Vessel vessel)
        {
            return vessel.GetModules<MechJebCore>().Max();
        }

        public static bool LiftedOff(this Vessel vessel)
        {
            return vessel.situation != Vessel.Situations.PRELAUNCH;
        }


        public static Orbit GetPatchAtUT(this Vessel vessel, double UT)
        {
            IEnumerable<ManeuverNode> earlierNodes = vessel.patchedConicSolver.maneuverNodes.Where(n => n.UT < UT);
            Orbit o = vessel.orbit;
            if (earlierNodes.Count() > 0)
            {
                o = earlierNodes.OrderByDescending(n => n.UT).First().nextPatch;
            }
            while (o.nextPatch != null && o.nextPatch.activePatch && o.nextPatch.StartUT < UT)
            {
                o = o.nextPatch;
            }
            return o;
        }

        //If there is a maneuver node on this patch, returns the patch that follows that maneuver node
        //Otherwise, if this patch ends in an SOI transition, returns the patch that follows that transition
        //Otherwise, returns null
        public static Orbit GetNextPatch(this Vessel vessel, Orbit patch, ManeuverNode ignoreNode = null)
        {
            //Determine whether this patch ends in an SOI transition or if it's the final one:
            bool finalPatch = (patch.patchEndTransition == Orbit.PatchTransitionType.FINAL);

            //See if any maneuver nodes occur during this patch. If so return the
            //patch that comes after the first maneuver node:
            var nodes = from node in vessel.patchedConicSolver.maneuverNodes
                        where (node.UT > patch.StartUT && (finalPatch || node.UT < patch.EndUT) && node != ignoreNode)
                        orderby node.UT ascending
                        select node;
            if (nodes.Count() > 0) return nodes.First().nextPatch;

            //return the next patch, or null if there isn't one:
            if (!finalPatch) return patch.nextPatch;
            else return null;
        }


        //input dV should be in world coordinates
        public static ManeuverNode PlaceManeuverNode(this Vessel vessel, Orbit patch, Vector3d dV, double UT)
        {
            //placing a maneuver node with bad dV values can really mess up the game, so try to protect against that
            //and log an exception if we get a bad dV vector:
            for (int i = 0; i < 3; i++)
            {
                if (double.IsNaN(dV[i]) || double.IsInfinity(dV[i]))
                {
                    throw new Exception("MechJeb VesselExtensions.PlaceManeuverNode: bad dV: " + dV);
                }
            }

            //convert a dV in world coordinates into the coordinate system of the maneuver node,
            //which uses (x, y, z) = (radial+, normal-, prograde)
            Vector3d nodeDV = patch.DeltaVToManeuverNodeCoordinates(UT, dV);
            ManeuverNode mn = vessel.patchedConicSolver.AddManeuverNode(UT);
            mn.OnGizmoUpdated(nodeDV, UT);
            return mn;
        }

        public static void RemoveAllManeuverNodes(this Vessel vessel)
        {
            while (vessel.patchedConicSolver.maneuverNodes.Count > 0)
            {
                vessel.patchedConicSolver.RemoveManeuverNode(vessel.patchedConicSolver.maneuverNodes.Last());
            }
        }
    }
}
