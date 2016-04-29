using UnityEngine;

namespace MuMech
{
    public static class PartExtensions
    {
        public static bool HasModule<T>(this Part part) where T : PartModule
        {
            for (int i = 0; i < part.Modules.Count; i++)
            {
                if (part.Modules[i] is T)
                    return true;
            }
            return false;
        }

        public static T GetModule<T>(this Part part) where T : PartModule
        {
            for (int i = 0; i < part.Modules.Count; i++)
            {
                PartModule pm = part.Modules[i];
                T module = pm as T;
                if (module != null)
                    return module;
            }
            return null;
        }

        // An allocation free version of GetModuleMass
        public static float GetModuleMassNoAlloc(this Part p, float defaultMass)
        {
            float mass = 0f;

            for (int i = 0; i < p.Modules.Count; i++)
            {
                IPartMassModifier m = p.Modules[i] as IPartMassModifier;
                if (m != null)
                {
                    mass += m.GetModuleMass(defaultMass, ModifierStagingSituation.CURRENT);
                }
            }
            return mass;
        }

        public static bool EngineHasFuel(this Part p)
        {
            for (int i = 0; i < p.Modules.Count; i++)
            {
                PartModule m = p.Modules[i];
                ModuleEngines eng = m as ModuleEngines;
                if (eng != null) return !eng.getFlameoutState;

            }
            return false;
        }

        public static bool IsUnfiredDecoupler(this Part p)
        {
            for (int i = 0; i < p.Modules.Count; i++)
            {
                PartModule m = p.Modules[i];
                ModuleDecouple mDecouple = m as ModuleDecouple;
                if (mDecouple != null)
                {
                    if (!mDecouple.isDecoupled && mDecouple.stagingEnabled && p.stagingOn) return true;
                    break;
                }

                ModuleAnchoredDecoupler mAnchoredDecoupler = m as ModuleAnchoredDecoupler;
                if (mAnchoredDecoupler != null)
                {
                    if (!mAnchoredDecoupler.isDecoupled && mAnchoredDecoupler.stagingEnabled && p.stagingOn) return true;
                    break;
                }

                ModuleDockingNode mDockingNode = m as ModuleDockingNode;
                if (mDockingNode != null)
                {
                    if (mDockingNode.staged && mDockingNode.stagingEnabled  && p.stagingOn) return true;
                    break;
                }

                if (VesselState.isLoadedProceduralFairing && m.moduleName == "ProceduralFairingDecoupler")
                {
                    if (!m.Fields["decoupled"].GetValue<bool>(m) && p.stagingOn) return true;
                    break;
                }
            }
            return false;
        }


        //Any engine that is decoupled in the same stage in
        //which it activates we call a sepratron.
        public static bool IsSepratron(this Part p)
        {
            return p.ActivatesEvenIfDisconnected
                && p.IsEngine()
                && p.IsDecoupledInStage(p.inverseStage)
                && !p.isControlSource;
        }


        public static bool IsEngine(this Part p)
        {
            for (int i = 0; i < p.Modules.Count; i++)
            {
                PartModule m = p.Modules[i];
                if (m is ModuleEngines) return true;
            }
            return false;
        }

        public static bool IsParachute(this Part p)
        {
            for (int i = 0; i < p.Modules.Count; i++)
            {
                if (p.Modules[i] is ModuleParachute) return true;
            }
            return false;
        }

        // TODO add some kind of cache ? This is called a lot but reply false 99.9999% oif the time
        public static bool IsLaunchClamp(this Part p)
        {
            for (int i = 0; i < p.Modules.Count; i++)
            {
                if (p.Modules[i] is LaunchClamp) return true;
            }
            return false;
        }

        public static bool IsDecoupledInStage(this Part p, int stage)
        {
            if ((p.IsUnfiredDecoupler() || p.IsLaunchClamp()) && p.inverseStage == stage) return true;
            if (p.parent == null) return false;
            return p.parent.IsDecoupledInStage(stage);
        }

        public static bool IsPhysicallySignificant(this Part p)
        {
            bool physicallySignificant = (p.physicalSignificance != Part.PhysicalSignificance.NONE);

            // part.PhysicsSignificance is not initialized in the Editor for all part. but physicallySignificant is useful there.
            if (HighLogic.LoadedSceneIsEditor)
            {
                physicallySignificant = physicallySignificant && p.PhysicsSignificance != 1;

                // Testing for launch clamp only in the Editor helps with the frame rate.
                // TODO : cache which part are LaunchClamp ?
                if (p.HasModule<LaunchClamp>())
                {
                    //Launch clamp mass should be ignored.
                    physicallySignificant = false;
                }
            }
            return physicallySignificant;
        }

        public struct Vector3Pair
        {
            public Vector3 p1;

            public Vector3 p2;

            public Vector3Pair(Vector3 point1, Vector3 point2)
            {
                this.p1 = point1;
                this.p2 = point2;
            }
        }

        public static Vector3Pair GetBoundingBox(this Part part)
        {
            Vector3 minBounds = new Vector3();
            Vector3 maxBounds = new Vector3();

            foreach (Transform t in part.FindModelComponents<Transform>())
            {
                MeshFilter mf = t.GetComponent<MeshFilter>();
                if (mf == null)
                    continue;
                Mesh m = mf.mesh;

                if (m == null)
                    continue;

                var matrix = part.vessel.transform.worldToLocalMatrix * t.localToWorldMatrix;

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
