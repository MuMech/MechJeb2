﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    public static class PartExtensions
    {
        public static bool HasModule<T>(this Part p) where T : PartModule
        {
            return p.Modules.OfType<T>().Any();
        }

        public static T GetModule<T>(this Part part) where T : PartModule
        {
            return part.Modules.OfType<T>().FirstOrDefault();
        }

        public static float TotalMass(this Part p)
        {
            return p.mass + p.GetResourceMass();
        }


        public static bool EngineHasFuel(this Part p)
        {
            foreach (PartModule m in p.Modules)
            {
                ModuleEngines eng = m as ModuleEngines;
                if (eng != null) return !eng.getFlameoutState;

                ModuleEnginesFX engFX = m as ModuleEnginesFX;
                if (engFX != null) return !engFX.getFlameoutState;
            }
            return false;
        }

        public static bool IsUnfiredDecoupler(this Part p)
        {
            foreach (PartModule m in p.Modules) {
                ModuleDecouple mDecouple = m as ModuleDecouple;
                if (mDecouple != null)
                {
                    if (!mDecouple.isDecoupled) return true;
                    break;
                }

                ModuleAnchoredDecoupler mAnchoredDecoupler = m as ModuleAnchoredDecoupler;
                if (mAnchoredDecoupler != null)
                {
                    if (!mAnchoredDecoupler.isDecoupled) return true;
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
            foreach (PartModule m in p.Modules)
            {
                if (m is ModuleEngines || m is ModuleEnginesFX) return true;
            }
            return false;
        }

        public static bool IsMFE(this Part p)
        {
            foreach(PartModule m in p.Modules)
            {
                if(m.ClassName == "ModuleEngineConfigs" || m.ClassName == "ModuleHybridEngine" || m.ClassName == "ModuleHybridEngines") return true;
            }
            return false;
        }

            

        public static bool IsParachute(this Part p)
        {
            foreach (PartModule m in p.Modules)
            {
                if (m is ModuleParachute) return true;
            }
            return false;
        }

        public static bool IsLaunchClamp(this Part p)
        {
            foreach (PartModule m in p.Modules)
            {
                if (m is LaunchClamp) return true;
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
