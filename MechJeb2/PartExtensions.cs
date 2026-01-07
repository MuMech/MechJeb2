using System;
using MechJebLibBindings;
using UnityEngine;

namespace MuMech
{
    public static class PartExtensions
    {
        public static bool HasModule<T>(this Part part) where T : PartModule => part.FindModuleImplementing<T>() != null;

        public static T GetModule<T>(this Part part) where T : PartModule => part.FindModuleImplementing<T>();

        public static bool EngineHasFuel(this ModuleEngines me) => !me.getFlameoutState && !me.engineShutdown;

        public static bool EngineHasFuel(this Part p)
        {
            ModuleEngines eng = p.FindModuleImplementing<ModuleEngines>();
            return eng != null && eng.EngineHasFuel();
        }

        public static bool UnstableUllage(this Part p)
        {
            if (!ReflectionUtils.IsLoadedRealFuels) // stock doesn't have this concept
                return false;

            ModuleEngines eng = p.FindModuleImplementing<ModuleEngines>();

            if (eng is null) // this case probably doesn't make any sense
                return false;

            if (eng.finalThrust > 0 || eng.requestedThrottle > 0 || eng.getFlameoutState || eng.EngineIgnited)
                return false;

            try
            {
                if (!VesselState.RFModuleEnginesRFType.IsInstanceOfType(eng))
                    return false;
                if (VesselState.RFignitedField.GetValue(eng) is bool ignited && ignited)
                    return false;
                if (VesselState.RFignitionsField.GetValue(eng) is int ignitions && ignitions == 0)
                    return false;
                if (VesselState.RFullageField.GetValue(eng) is bool ullage && !ullage)
                    return false;
                if (VesselState.RFullageSetField.GetValue(eng) is object ullageSet)
                    if (VesselState.RFGetUllageStabilityMethod.Invoke(ullageSet, Array.Empty<object>()) is double propellantStability)
                        if (propellantStability < 0.996)
                            return true;
            }
            catch (ArgumentException)
            {
            }

            return false;
        }

        public static bool IsDecoupler(this Part p) =>
            p != null && (p.FindModuleImplementing<ModuleDecouplerBase>() != null ||
                p.FindModuleImplementing<ModuleDockingNode>() != null ||
                p.Modules.Contains("ProceduralFairingDecoupler"));

        private static bool IsUnfiredDecoupler(this ModuleDecouplerBase decoupler, out Part decoupledPart)
        {
            if (!decoupler.isDecoupled && decoupler.stagingEnabled && decoupler.part.stagingOn)
            {
                decoupledPart = decoupler.ExplosiveNode.attachedPart;
                if (decoupledPart == decoupler.part.parent)
                    decoupledPart = decoupler.part;
                return true;
            }

            decoupledPart = null;
            return false;
        }

        private static bool IsUnfiredDecoupler(this ModuleDockingNode mDockingNode, out Part decoupledPart)
        {
            if (mDockingNode.staged && mDockingNode.stagingEnabled && mDockingNode.part.stagingOn)
            {
                decoupledPart = mDockingNode.referenceNode.attachedPart;
                if (decoupledPart == mDockingNode.part.parent)
                    decoupledPart = mDockingNode.part;
                return true;
            }

            decoupledPart = null;
            return false;
        }

        private static bool IsUnfiredProceduralFairingDecoupler(this PartModule decoupler, out Part decoupledPart)
        {
            if (ReflectionUtils.IsLoadedProceduralFairing && decoupler.moduleName == "ProceduralFairingDecoupler")
            {
                if (!decoupler.Fields["decoupled"].GetValue<bool>(decoupler) && decoupler.part.stagingOn)
                {
                    // ProceduralFairingDecoupler always decouple from their parents
                    decoupledPart = decoupler.part;
                    return true;
                }
            }

            decoupledPart = null;
            return false;
        }

        public static bool IsUnfiredDecoupler(this PartModule m, out Part decoupledPart)
        {
            if (m is ModuleDecouplerBase @base && IsUnfiredDecoupler(@base, out decoupledPart)) return true;
            if (m is ModuleDockingNode node && IsUnfiredDecoupler(node, out decoupledPart)) return true;
            if (ReflectionUtils.IsLoadedProceduralFairing && m.moduleName == "ProceduralFairingDecoupler" &&
                m.IsUnfiredProceduralFairingDecoupler(out decoupledPart)) return true;
            decoupledPart = null;
            return false;
        }

        /// <summary>
        ///     Determines if a given part is a ProceduralFairingDecoupler
        /// </summary>
        /// <param name="p">the part to check</param>
        /// <returns>if the part is a procfairing payload decoupler</returns>
        public static bool IsProceduralFairing(this Part p)
        {
            if (!ReflectionUtils.IsLoadedProceduralFairing) return false;
            return p.Modules.Contains("ProceduralFairingDecoupler");
        }

        /// <summary>
        ///     Determines if a given part is a ProceduralFairingDecoupler which is attached to a payload ProceduralFairingBase
        /// </summary>
        /// <param name="p">the part to check</param>
        /// <returns>if the part is a procfairing payload decoupler</returns>
        public static bool IsProceduralFairingPayloadFairing(this Part p)
        {
            if (!p.IsProceduralFairing()) return false;
            Part basepart = p.parent;
            if (basepart is null)
                throw new Exception("ProceduralFairingDecoupler parent is null--fix your root staging?");
            PartModule fairingbase = basepart.Modules.GetModule("ProceduralFairingBase");
            if (fairingbase is null)
                throw new Exception("ProceduralFairingBase not found in parent part, weird.");
            return fairingbase.Fields["mode"].GetValue<string>(fairingbase) == "Payload";
        }

        public static bool IsUnfiredDecoupler(this Part p, out Part decoupledPart)
        {
            foreach (PartModule m in p.Modules)
                if (m.IsUnfiredDecoupler(out decoupledPart))
                    return true;
            decoupledPart = null;
            return false;
        }

        //Any engine that is decoupled in the same stage in
        //which it activates we call a sepratron.
        public static bool IsSepratron(this Part p) =>
            p.ActivatesEvenIfDisconnected
            && p.IsThrottleLockedEngine()
            && p.IsDecoupledInStage(p.inverseStage)
            && p.isControlSource == Vessel.ControlLevel.NONE;

        public static bool IsEngine(this Part p) => p.FindModuleImplementing<ModuleEngines>() != null;

        public static bool IsThrottleLockedEngine(this Part p)
        {
            ModuleEngines me = p.FindModuleImplementing<ModuleEngines>();
            return me != null && me.throttleLocked;
        }

        public static bool IsParachute(this Part p) => p.FindModuleImplementing<ModuleParachute>() != null;

        public static bool IsLaunchClamp(this Part p) => p.FindModuleImplementing<LaunchClamp>() != null;

        public static bool IsDecoupledInStage(this Part p, int stage)
        {
            if (((p.IsUnfiredDecoupler(out Part decoupledPart) && p == decoupledPart) || p.IsLaunchClamp()) && p.inverseStage == stage) return true;
            if (p.parent is null) return false;
            if (p.parent.IsUnfiredDecoupler(out decoupledPart) && p == decoupledPart && p.parent.inverseStage == stage) return true;
            return p.parent.IsDecoupledInStage(stage);
        }

        public static bool IsPhysicallySignificant(this Part p)
        {
            bool physicallySignificant = p.physicalSignificance != Part.PhysicalSignificance.NONE;

            // part.PhysicsSignificance is not initialized in the Editor for all part. but physicallySignificant is useful there.
            if (HighLogic.LoadedSceneIsEditor)
                physicallySignificant &= p.PhysicsSignificance != 1 && !p.IsLaunchClamp();

            return physicallySignificant;
        }

        public struct Vector3Pair
        {
            public Vector3 P1;

            public Vector3 P2;

            public Vector3Pair(Vector3 point1, Vector3 point2)
            {
                P1 = point1;
                P2 = point2;
            }
        }

        public static Vector3Pair GetBoundingBox(this Part part)
        {
            var minBounds = new Vector3();
            var maxBounds = new Vector3();

            foreach (Transform t in part.FindModelComponents<Transform>())
            {
                MeshFilter mf = t.GetComponent<MeshFilter>();
                if (mf == null)
                    continue;
                Mesh m = mf.mesh;

                if (m == null)
                    continue;

                Matrix4x4 matrix = part.vessel.transform.worldToLocalMatrix * t.localToWorldMatrix;

                foreach (Vector3 vertex in m.vertices)
                {
                    Vector3 v = matrix.MultiplyPoint3x4(vertex);
                    maxBounds.x = Mathf.Max(maxBounds.x, v.x);
                    minBounds.x = Mathf.Min(minBounds.x, v.x);
                    maxBounds.y = Mathf.Max(maxBounds.y, v.y);
                    minBounds.y = Mathf.Min(minBounds.y, v.y);
                    maxBounds.z = Mathf.Max(maxBounds.z, v.z);
                    minBounds.z = Mathf.Min(minBounds.z, v.z);
                }
            }

            return new Vector3Pair(maxBounds, minBounds);
        }
    }
}
