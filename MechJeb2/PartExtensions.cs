using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    public static class PartExtensions
    {
        public static bool HasModule<T>(this Part p) where T : PartModule
        {
            return p.Modules.OfType<T>().Count() > 0;
        }

        public static float TotalMass(this Part p)
        {
            return p.mass + p.GetResourceMass();
        }


        public static bool EngineHasFuel(this Part p)
        {
            if (p is LiquidEngine || p is LiquidFuelEngine || p is AtmosphericEngine)
            {
                //I don't really know the details of how you're supposed to use RequestFuel, but this seems to work to
                //test whether something can get fuel.
                return p.RequestFuel(p, 0, Part.getFuelReqId());
            }
            else if (p.HasModule<ModuleEngines>())
            {
                return !p.Modules.OfType<ModuleEngines>().First().getFlameoutState;
            }
            else return false;
        }

        public static bool IsDecoupler(this Part p)
        {
            return (p is Decoupler ||
             p is DecouplerGUI ||
             p is RadialDecoupler ||
             p.HasModule<ModuleDecouple>() ||
             p.HasModule<ModuleAnchoredDecoupler>());
        }

        //we assume that any SRB with ActivatesEvenIfDisconnected = True is a sepratron:
        public static bool IsSepratron(this Part p)
        {
            if (!(p.ActivatesEvenIfDisconnected && p.IsSRB())) return false;

            //Guess that an SRB is a sepratron if its thrust axis is oriented
            //more than 30 degrees from the axis of the ship.
            Vector3d worldShipAxis;
            if (HighLogic.LoadedSceneIsEditor) worldShipAxis = EditorLogic.SortedShipList[0].transform.up;
            else worldShipAxis = p.vessel.GetTransform().up;

            Vector3d worldThrustAxis = -p.Modules.OfType<ModuleEngines>().First().thrustTransforms[0].forward;

            if (Vector3d.Angle(worldShipAxis, worldThrustAxis) > 30) return true;
            else return false;
        }

        public static bool IsSRB(this Part p)
        {
            if (p is SolidRocket) return true;

            //new-style SRBs:
            if (!p.HasModule<ModuleEngines>()) return false; //sepratrons are motors
            return p.Modules.OfType<ModuleEngines>().First().throttleLocked; //throttleLocked signifies an SRB
        }


        public static bool IsEngine(this Part p)
        {
            return (p is SolidRocket ||
                p is LiquidEngine ||
                p is LiquidFuelEngine ||
                p is AtmosphericEngine ||
                p.HasModule<ModuleEngines>());
        }

        public static bool IsParachute(this Part p)
        {
            return p is Parachutes ||
                p is HParachutes ||
                p.HasModule<ModuleParachute>();
        }

        public static bool IsDecoupledInStage(this Part p, int stage)
        {
            if (p.IsDecoupler() && p.inverseStage == stage) return true;
            if (p.parent == null) return false;
            return p.parent.IsDecoupledInStage(stage);
        }
    }
}
