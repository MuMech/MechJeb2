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
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.OfType<T>().ToList();
            if (vessel == null) return new List<T>();
            return vessel.Parts.OfType<T>().ToList();
        }

        public static List<T> GetModules<T>(this Vessel vessel) where T : PartModule
        {
            List<Part> parts;
            if (HighLogic.LoadedSceneIsEditor) parts = EditorLogic.SortedShipList;
            else if (vessel == null) return new List<T>();
            else parts = vessel.Parts;

            return (from part in parts from module in part.Modules.OfType<T>() select module).ToList();
        }

        private static float lastFixedTime = 0;
        private static Dictionary<Guid, MechJebCore> masterMechjebs = new Dictionary<Guid, MechJebCore>();

        public static MechJebCore GetMasterMechJeb(this Vessel vessel)
        {
            if (vessel == null) // there will be no cache in the editor, but thats not as usefull as in flight
                return vessel.GetModules<MechJebCore>().Max();
            if (lastFixedTime != Time.fixedTime)
            {
                masterMechjebs = new Dictionary<Guid, MechJebCore>();
                lastFixedTime = Time.fixedTime;
            }
            if (!masterMechjebs.ContainsKey(vessel.id))
            {
                MechJebCore mj = vessel.GetModules<MechJebCore>().Max();
                if (mj != null)
                    masterMechjebs.Add(vessel.id, mj);
                return mj;
            }
            return masterMechjebs[vessel.id];
        }

        public static double TotalResourceMass(this Vessel vessel, string resourceName)
        {
            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.SortedShipList : vessel.parts);
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            if (definition == null) return 0;

            double amount = 0;
            foreach (Part p in parts)
            {
                foreach (PartResource r in p.Resources)
                {
                    if (r.info.id == definition.id) amount += r.amount;
                }
            }

            return amount * definition.density;
        }

        public static bool HasElectricCharge(this Vessel vessel)
        {
            if (vessel == null || vessel.GetReferenceTransformPart() == null)
                return false;

            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.SortedShipList : vessel.parts);
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");
            if (definition == null) return false;

            PartResource r = vessel.GetReferenceTransformPart().Resources.Get(definition.id);
            // check the command pod first since most have their batteries
            if (r != null && r.amount > 0)
                return true;

            foreach (Part p in parts)
            {
                r = p.Resources.Get(definition.id);
                if (r != null && r.amount > 0)
                    return true;
            }
            return false;
        }



        public static bool LiftedOff(this Vessel vessel)
        {
            return vessel.situation != Vessel.Situations.PRELAUNCH;
        }


        public static Orbit GetPatchAtUT(this Vessel vessel, double UT)
        {
            IEnumerable<ManeuverNode> earlierNodes = vessel.patchedConicSolver.maneuverNodes.Where(n => (n.UT <= UT));
            Orbit o = vessel.orbit;
            if (earlierNodes.Count() > 0)
            {
                o = earlierNodes.OrderByDescending(n => n.UT).First().nextPatch;
            }
            while (o.patchEndTransition != Orbit.PatchTransitionType.FINAL && o.nextPatch.StartUT <= UT)
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

            //See if any maneuver nodes occur during this patch. If there is one
            //return the patch that follows it
            var nodes = vessel.patchedConicSolver.maneuverNodes.Where(n => (n.patch == patch && n != ignoreNode));
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

            if (double.IsNaN(UT) || double.IsInfinity(UT))
            {
                throw new Exception("MechJeb VesselExtensions.PlaceManeuverNode: bad UT: " + UT);
            }

            //It seems that sometimes the game can freak out if you place a maneuver node in the past, so this
            //protects against that.
            UT = Math.Max(UT, Planetarium.GetUniversalTime());

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
