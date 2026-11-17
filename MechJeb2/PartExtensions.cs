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

        /// <summary>
        ///     The part's engine in use: a multi-mode engine's current mode, else an ignited engine module, else the first. The first
        ///     module alone is a multi-mode engine's primary mode, which keeps its flameout after the switch to the secondary.
        /// </summary>
        public static ModuleEngines CurrentEngine(this Part p)
        {
            MultiModeEngine multiMode = p.FindModuleImplementing<MultiModeEngine>();
            if (multiMode != null)
            {
                ModuleEngines mode = multiMode.runningPrimary ? multiMode.PrimaryEngine : multiMode.SecondaryEngine;
                if (mode != null)
                    return mode;
            }

            ModuleEngines first = null;
            for (int i = 0; i < p.Modules.Count; i++)
            {
                if (!(p.Modules[i] is ModuleEngines engine))
                    continue;
                if (engine.EngineIgnited)
                    return engine;
                first ??= engine;
            }

            return first;
        }

        public static bool EngineHasFuel(this Part p)
        {
            ModuleEngines eng = p.CurrentEngine();
            return eng != null && eng.EngineHasFuel();
        }

        public static bool UnstableUllage(this Part p)
        {
            if (!VesselState.IsRealFuelsCorrectlyInitialized) // stock doesn't have this concept
                return false;

            ModuleEngines eng = p.CurrentEngine();

            if (eng is null) // this case probably doesn't make any sense
                return false;

            if (eng.finalThrust > 0 || eng.requestedThrottle > 0 || eng.getFlameoutState || eng.EngineIgnited)
                return false;

            if (!VesselState.RFModuleEnginesRFType.IsInstance(eng))
                return false;
            if (VesselState.RFignitedField.GetValue<bool>(eng))
                return false;
            if (VesselState.RFignitionsField.GetValue<int>(eng) is 0)
                return false;
            if (!VesselState.RFullageField.GetValue<bool>(eng))
                return false;
            object ullageSet = VesselState.RFullageSetField.GetValue<object>(eng);
            double propellantStability = (double)VesselState.RFGetUllageStabilityMethod.Invoke(ullageSet, Array.Empty<object>());

            return propellantStability < 0.996;
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
            if (m is ModuleDecouplerBase @base && @base.IsUnfiredDecoupler(out decoupledPart)) return true;
            if (m is ModuleDockingNode node && node.IsUnfiredDecoupler(out decoupledPart)) return true;
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
            ModuleEngines me = p.CurrentEngine();
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

        // The part's box in the vessel's axes, from the corners of each active mesh's bounds: P1 is the maximum, P2 the minimum.
        // A part without a mesh gives P1 = -infinity and P2 = +infinity, which leaves a union unchanged.
        // sharedMesh, as MeshFilter.mesh would give the filter its own copy of the mesh, and Mesh.vertices copies the vertex array.
        public static Vector3Pair GetBoundingBox(this Part part)
        {
            var minBounds = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var maxBounds = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            Matrix4x4 toVessel = part.vessel.transform.worldToLocalMatrix;

            foreach (MeshFilter filter in part.FindModelComponents<MeshFilter>())
            {
                if (filter.sharedMesh == null || !filter.gameObject.activeInHierarchy)
                    continue;

                Matrix4x4 matrix = toVessel * filter.transform.localToWorldMatrix;
                Bounds bounds = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var sign = new Vector3((corner & 1) * 2 - 1, (corner & 2) - 1, (corner & 4) / 2 - 1);
                    Vector3 v = matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, sign));
                    minBounds = Vector3.Min(minBounds, v);
                    maxBounds = Vector3.Max(maxBounds, v);
                }
            }

            return new Vector3Pair(maxBounds, minBounds);
        }
    }
}
