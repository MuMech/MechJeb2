using System;
using System.Collections.Generic;
using UniLinq;
using Smooth.Slinq;
using UnityEngine;

namespace MuMech
{
    public static class VesselExtensions
    {
        public static List<T> GetParts<T>(this Vessel vessel) where T : Part
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.fetch.ship.parts.OfType<T>().ToList();
            if (vessel == null) return new List<T>();
            return vessel.Parts.OfType<T>().ToList();
        }

        public static List<ITargetable> GetTargetables(this Vessel vessel)
        {
            List<Part> parts;
            if (HighLogic.LoadedSceneIsEditor) parts = EditorLogic.fetch.ship.parts;
            else if (vessel == null) return new List<ITargetable>();
            else parts = vessel.Parts;

            return (from part in parts from targetable in part.Modules.OfType<ITargetable>() select targetable).ToList();
        }

        public static List<T> GetModules<T>(this Vessel vessel) where T : PartModule
        {
            List<Part> parts;
            if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null) parts = EditorLogic.fetch.ship.parts;
            else if (vessel == null || vessel.Parts == null) return new List<T>();
            else parts = vessel.Parts;

            List<T> list = new List<T>();
            for (int p = 0; p < parts.Count; p++)
            {
                Part part = parts[p];

                if (part.Modules == null)
                    continue;

                int count = part.Modules.Count;
                for (int m = 0; m < count; m++)
                {
                    if (part.Modules[m] is T mod)
                    {  
                        list.Add(mod);;
                    }
                }
            }
            return list;
        }

        public static T GetModule<T>(this Vessel vessel, Predicate<T> predicate) where T : PartModule
        {
            List<Part> parts;
            if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null) parts = EditorLogic.fetch.ship.parts;
            else if (vessel == null || vessel.Parts == null) return default;
            else parts = vessel.Parts;

            for (int p = 0; p < parts.Count; p++)
            {
                Part part = parts[p];

                if (part.Modules == null)
                    continue;

                int count = part.Modules.Count;
                for (int m = 0; m < count; m++)
                {
                    if (part.Modules[m] is T mod && predicate(mod))
                    {
                        return mod;
                    }
                }
            }
            return default;
        }

        private static float lastFixedTime = 0;
        private static readonly Dictionary<Guid, MechJebCore> masterMechJeb = new Dictionary<Guid, MechJebCore>();

        public static MechJebCore GetMasterMechJeb(this Vessel vessel)
        {
            if (lastFixedTime != Time.fixedTime)
            {
                masterMechJeb.Clear();
                lastFixedTime = Time.fixedTime;
            }
            Guid vesselKey = vessel == null ? Guid.Empty : vessel.id;

            if (!masterMechJeb.TryGetValue(vesselKey, out MechJebCore mj))
            {
                mj = vessel.GetModule<MechJebCore>(p => p.running);
                if (mj != null)
                    masterMechJeb.Add(vesselKey, mj);
                return mj;
            }
            return mj;
        }

        public static double TotalResourceAmount(this Vessel vessel, PartResourceDefinition definition)
        {
            if (definition == null) return 0;
            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);

            double amount = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                for (int j = 0; j < p.Resources.Count; j++)
                {
                    PartResource r = p.Resources[j];

                    if (r.info.id == definition.id)
                    {
                        amount += r.amount;
                    }
                }
            }

            return amount;
        }

        public static double TotalResourceAmount(this Vessel vessel, string resourceName)
        {
            return vessel.TotalResourceAmount(PartResourceLibrary.Instance.GetDefinition(resourceName));
        }

        public static double TotalResourceAmount(this Vessel vessel, int resourceId)
        {
            return vessel.TotalResourceAmount(PartResourceLibrary.Instance.GetDefinition(resourceId));
        }

        public static double TotalResourceMass(this Vessel vessel, string resourceName)
        {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            return vessel.TotalResourceAmount(definition) * definition.density;
        }

        public static double TotalResourceMass(this Vessel vessel, int resourceId)
        {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceId);
            return vessel.TotalResourceAmount(definition) * definition.density;
        }

        public static double MaxResourceAmount(this Vessel vessel, PartResourceDefinition definition)
        {
            if (definition == null) return 0;
            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);

            double amount = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                for (int j = 0; j < p.Resources.Count; j++)
                {
                    PartResource r = p.Resources[j];

                    if (r.info.id == definition.id)
                    {
                        amount += r.maxAmount;
                    }
                }
            }

            return amount;
        }

        public static double MaxResourceAmount(this Vessel vessel, int id)
        {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(id);
            return vessel.MaxResourceAmount(definition);
        }

        public static double MaxResourceAmount(this Vessel vessel, string resourceName)
        {
            return vessel.MaxResourceAmount(PartResourceLibrary.Instance.GetDefinition(resourceName));
        }

        public static bool HasElectricCharge(this Vessel vessel)
        {
            if (vessel == null)
                return false;

            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(PartResourceLibrary.ElectricityHashcode);
            if (definition == null) return false;

            PartResource r;
            if (vessel.GetReferenceTransformPart() != null)
            {
                r = vessel.GetReferenceTransformPart().Resources.Get(definition.id);
                // check the command pod first since most have their batteries
                if (r != null && r.amount > 0)
                    return true;
            }

            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                r = p.Resources.Get(definition.id);
                if (r != null && r.amount > 0)
                {
                    return true;
                }
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
            if (earlierNodes.Any())
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
            if (patch == null)
            {
                return null;
            }

            //Determine whether this patch ends in an SOI transition or if it's the final one:
            bool finalPatch = (patch.patchEndTransition == Orbit.PatchTransitionType.FINAL);


            if (vessel.patchedConicSolver == null)
            {
                vessel.patchedConicSolver = vessel.gameObject.AddComponent<PatchedConicSolver>();
                vessel.patchedConicSolver.Load(vessel.flightPlanNode);
            }

            //See if any maneuver nodes occur during this patch. If there is one
            //return the patch that follows it
            var nodes = vessel.patchedConicSolver.maneuverNodes.Slinq().Where((n,p) => n.patch == p && n != ignoreNode, patch);
            // Slinq is nice but you can only enumerate it once
            var first = nodes.FirstOrDefault();
            if (first != null) return first.nextPatch;

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
            mn.DeltaV = nodeDV;
            vessel.patchedConicSolver.UpdateFlightPlan();
            return mn;
        }

        public static void RemoveAllManeuverNodes(this Vessel vessel)
        {
            if (!vessel.patchedConicsUnlocked())
                return;

            while (vessel.patchedConicSolver.maneuverNodes.Count > 0)
            {
                vessel.patchedConicSolver.maneuverNodes.Last().RemoveSelf();
            }
        }

        // From FAR with ferram4 authorisation
        public static MechJebModuleDockingAutopilot.Box3d GetBoundingBox(this Vessel vessel, bool debug=false)
        {
            Vector3 minBounds = new Vector3();
            Vector3 maxBounds = new Vector3();

            if (debug)
            {
                MonoBehaviour.print("[GetBoundingBox] Start " + vessel.vesselName);
            }

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                PartExtensions.Vector3Pair partBox = p.GetBoundingBox();

                if (debug)
                {
                    MonoBehaviour.print("[GetBoundingBox] " + p.name + " " + (partBox.p1 - partBox.p2).magnitude.ToString("F3"));
                }

                maxBounds.x = Mathf.Max(maxBounds.x, partBox.p1.x);
                minBounds.x = Mathf.Min(minBounds.x, partBox.p2.x);
                maxBounds.y = Mathf.Max(maxBounds.y, partBox.p1.y);
                minBounds.y = Mathf.Min(minBounds.y, partBox.p2.y);
                maxBounds.z = Mathf.Max(maxBounds.z, partBox.p1.z);
                minBounds.z = Mathf.Min(minBounds.z, partBox.p2.z);

                //foreach (var sympart in p.symmetryCounterparts)
                //{
                //    partBox = sympart.GetBoundingBox();

                //    maxBounds.x = Mathf.Max(maxBounds.x, partBox.p1.x);
                //    minBounds.x = Mathf.Min(minBounds.x, partBox.p2.x);
                //    maxBounds.y = Mathf.Max(maxBounds.y, partBox.p1.y);
                //    minBounds.y = Mathf.Min(minBounds.y, partBox.p2.y);
                //    maxBounds.z = Mathf.Max(maxBounds.z, partBox.p1.z);
                //    minBounds.z = Mathf.Min(minBounds.z, partBox.p2.z);
                //}
            }

            if (debug)
            {
                MonoBehaviour.print("[GetBoundingBox] End " + vessel.vesselName);
            }

            MechJebModuleDockingAutopilot.Box3d box = new MechJebModuleDockingAutopilot.Box3d();

            box.center = new Vector3d((maxBounds.x + minBounds.x) / 2, (maxBounds.y + minBounds.y) / 2, (maxBounds.z + minBounds.z) / 2);
            box.size = new Vector3d(Math.Abs(box.center.x - maxBounds.x), Math.Abs(box.center.y - maxBounds.y), Math.Abs(box.center.z - maxBounds.z));

            return box;
        }

        // 0.90 added a building upgrade to unlock Orbit visualization and patched conics
        // Unfortunately when patchedConics are disabled vessel.patchedConicSolver is null
        // So we need to add a lot of sanity check and/or disable modules
        public static bool patchedConicsUnlocked(this Vessel vessel)
        {
            return GameVariables.Instance.GetOrbitDisplayMode(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation)) == GameVariables.OrbitDisplayMode.PatchedConics;
        }

        public static void UpdateNode(this ManeuverNode node, Vector3d dV, double ut)
        {
            node.DeltaV = dV;
            node.UT = ut;
            node.solver.UpdateFlightPlan();
            if (node.attachedGizmo == null)
                return;
            node.attachedGizmo.patchBefore = node.patch;
            node.attachedGizmo.patchAhead = node.nextPatch;
        }

        public static Vector3d WorldDeltaV(this ManeuverNode node)
        {
            return node.patch.Prograde(node.UT) * node.DeltaV.z + node.patch.RadialPlus(node.UT) * node.DeltaV.x + -node.patch.NormalPlus(node.UT) * node.DeltaV.y;
        }

        // The part loop in VesselState could expose this, but it gets disabled when the RCS action group is disabled.
        // This method is also useful when the RCS AG is off.
        public static bool hasEnabledRCSModules(this Vessel vessel)
        {
            var rcsModules = vessel.FindPartModulesImplementing<ModuleRCS>();

            for (int m = 0; m < rcsModules.Count; m++)
            {
                ModuleRCS rcs = rcsModules[m];

                if (rcs == null)
                    continue;

                if (rcs.rcsEnabled && rcs.isEnabled && !rcs.isJustForShow)
                    return true;
            }

            return false;
        }
    }
}
