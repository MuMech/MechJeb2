using System;
using UnityEngine;

namespace MuMech
{
    public static class PartExtensions
    {
        public static bool HasModule<T>(this Part part) where T : PartModule
        {
            return part.FindModuleImplementing<T>() != null;
        }

        public static T GetModule<T>(this Part part) where T : PartModule
        {
            return part.FindModuleImplementing<T>();
        }

        // An allocation free version of GetModuleMass
        public static float GetModuleMassNoAlloc(this Part p, float defaultMass, ModifierStagingSituation sit)
        {
            float mass = 0f;

            for (int i = 0; i < p.Modules.Count; i++)
            {
                var m = p.Modules[i] as IPartMassModifier;
                if (m != null)
                {
                    mass += m.GetModuleMass(defaultMass, sit);
                }
            }

            return mass;
        }

        public static bool EngineHasFuel(this ModuleEngines me)
        {
            return !me.getFlameoutState && !me.engineShutdown;
        }

        public static bool EngineHasFuel(this Part p)
        {
            ModuleEngines eng = p.FindModuleImplementing<ModuleEngines>();
            return eng != null && eng.EngineHasFuel();
        }

        public static bool UnstableUllage(this Part p)
        {
            if (!VesselState.isLoadedRealFuels) // stock doesn't have this concept
                return false;
            
            ModuleEngines eng = p.FindModuleImplementing<ModuleEngines>();

            if (eng is null) // this case probably doesn't make any sense
                return false;
            
            if (eng.finalThrust > 0 || eng.requestedThrottle > 0 || eng.getFlameoutState || eng.EngineIgnited)
                return false;

            try
            {
                if (VesselState.RFignitedField.GetValue(eng) is bool ignited && ignited)
                    return false;
                if (VesselState.RFignitionsField.GetValue(eng) is int ignitions && ignitions == 0)
                    return false;
                if (VesselState.RFullageField.GetValue(eng) is bool ullage && !ullage)
                    return false;
                if (VesselState.RFullageSetField.GetValue(eng) is object ullageSet)
                    if (VesselState.RFGetUllageStabilityMethod.Invoke(ullageSet, Array.Empty<object>()) is double propellantStability)
                        if (propellantStability < VesselState.RFveryStableValue)
                            return true;
            }
            catch(ArgumentException)
            {
            }
            
            return false;
        }

        public static bool UnrestartableDeadEngine(this Part p)
        {
            if (!VesselState.isLoadedRealFuels) // stock doesn't have this concept
                return false;

            ModuleEngines eng = p.FindModuleImplementing<ModuleEngines>();

            if (eng is null) // this case probably doesn't make any sense
                return false;

            if (eng.finalThrust > 0)
                return false;

            try
            {
                if (VesselState.RFignitedField.GetValue(eng) is bool ignited && ignited)
                    return false;
                if (VesselState.RFignitionsField.GetValue(eng) is int ignitions)
                    return ignitions == 0;
            }
            catch (ArgumentException)
            {
            }

            return false;
        }

        public static double FlowRateAtConditions(this ModuleEngines e, double throttle, double flowMultiplier)
        {
            float minFuelFlow = e.minFuelFlow;
            float maxFuelFlow = e.maxFuelFlow;

            // Some brilliant engine mod seems to consider that FuelFlow is not something they should properly initialize
            if (minFuelFlow == 0 && e.minThrust > 0)
            {
                minFuelFlow = e.minThrust / (e.atmosphereCurve.Evaluate(0f) * e.g);
            }

            if (maxFuelFlow == 0 && e.maxThrust > 0)
            {
                maxFuelFlow = e.maxThrust / (e.atmosphereCurve.Evaluate(0f) * e.g);
            }

            return Mathf.Lerp(minFuelFlow, maxFuelFlow, (float)throttle * 0.01f * e.thrustPercentage) * flowMultiplier;
        }

        // for a single EngineModule, determine its flowMultiplier, subject to atmDensity + machNumber
        public static double FlowMultiplierAtConditions(this ModuleEngines e, double atmDensity, double machNumber)
        {
            double flowMultiplier = 1;

            if (e.atmChangeFlow)
            {
                if (e.useAtmCurve)
                    flowMultiplier = e.atmCurve.Evaluate((float)atmDensity * 40 / 49);
                else
                    flowMultiplier = atmDensity * 40 / 49;
            }

            // we take the middle of the thrust curve and hope it looks something like the average
            // (the ends are often very far from the average)
            if (e.useThrustCurve)
                flowMultiplier *= e.thrustCurve.Evaluate(0.5f);

            if (e.useVelCurve)
                flowMultiplier *= e.velCurve.Evaluate((float)machNumber);

            if (flowMultiplier > e.flowMultCap)
            {
                double excess = flowMultiplier - e.flowMultCap;
                flowMultiplier = e.flowMultCap + excess / (e.flowMultCapSharpness + excess / e.flowMultCap);
            }

            // some engines have e.CLAMP set to float.MaxValue so we have to have the e.CLAMP < 1 sanity check here
            if (flowMultiplier < e.CLAMP && e.CLAMP < 1)
                flowMultiplier = e.CLAMP;

            return flowMultiplier;
        }

        // for a single EngineModule, evaluate its ISP, subject to all the different possible curves
        public static double ISPAtConditions(this ModuleEngines e, double throttle, double atmPressure, double atmDensity, double machNumber)
        {
            double isp = 0;
            isp = e.atmosphereCurve.Evaluate((float)atmPressure);
            if (e.useThrottleIspCurve)
                isp *= Mathf.Lerp(1f, e.throttleIspCurve.Evaluate((float)throttle), e.throttleIspCurveAtmStrength.Evaluate((float)atmPressure));
            if (e.useAtmCurveIsp)
                isp *= e.atmCurveIsp.Evaluate((float)atmDensity * 40 / 49);
            if (e.useVelCurveIsp)
                isp *= e.velCurveIsp.Evaluate((float)machNumber);
            return isp;
        }

        public static bool IsDecoupler(this Part p)
        {
            return p != null && (p.FindModuleImplementing<ModuleDecouplerBase>() != null ||
                                 p.FindModuleImplementing<ModuleDockingNode>() != null ||
                                 p.Modules.Contains("ProceduralFairingDecoupler"));
        }

        public static bool IsUnfiredDecoupler(this ModuleDecouplerBase decoupler, out Part decoupledPart)
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

        public static bool IsUnfiredDecoupler(this ModuleDockingNode mDockingNode, out Part decoupledPart)
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

        public static bool IsUnfiredProceduralFairingDecoupler(this PartModule decoupler, out Part decoupledPart)
        {
            if (VesselState.isLoadedProceduralFairing && decoupler.moduleName == "ProceduralFairingDecoupler")
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
            if (m is ModuleDecouplerBase && IsUnfiredDecoupler(m as ModuleDecouplerBase, out decoupledPart)) return true;
            if (m is ModuleDockingNode && IsUnfiredDecoupler(m as ModuleDockingNode, out decoupledPart)) return true;
            if (VesselState.isLoadedProceduralFairing && m.moduleName == "ProceduralFairingDecoupler" &&
                m.IsUnfiredProceduralFairingDecoupler(out decoupledPart)) return true;
            decoupledPart = null;
            return false;
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
        public static bool IsSepratron(this Part p)
        {
            return p.ActivatesEvenIfDisconnected
                   && p.IsThrottleLockedEngine()
                   && p.IsDecoupledInStage(p.inverseStage)
                   && p.isControlSource == Vessel.ControlLevel.NONE;
        }

        public static bool IsEngine(this Part p)
        {
            return p.FindModuleImplementing<ModuleEngines>() != null;
        }

        public static bool IsThrottleLockedEngine(this Part p)
        {
            ModuleEngines me = p.FindModuleImplementing<ModuleEngines>();
            return me != null && me.throttleLocked;
        }

        public static bool IsParachute(this Part p)
        {
            return p.FindModuleImplementing<ModuleParachute>() != null;
        }

        public static bool IsLaunchClamp(this Part p)
        {
            return p.FindModuleImplementing<LaunchClamp>() != null;
        }

        public static bool IsDecoupledInStage(this Part p, int stage)
        {
            Part decoupledPart;
            if (((p.IsUnfiredDecoupler(out decoupledPart) && p == decoupledPart) || p.IsLaunchClamp()) && p.inverseStage == stage) return true;
            if (p.parent == null) return false;
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
            public Vector3 p1;

            public Vector3 p2;

            public Vector3Pair(Vector3 point1, Vector3 point2)
            {
                p1 = point1;
                p2 = point2;
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
