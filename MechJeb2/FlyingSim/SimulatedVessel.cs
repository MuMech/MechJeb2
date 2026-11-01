using System.Collections.Generic;
using Smooth.Pools;

namespace MuMech
{
    public class SimulatedVessel
    {
        public readonly List<SimulatedPart> parts = new List<SimulatedPart>();
        private int count;
        public double totalMass;

        private ReentrySimulation.SimCurves simCurves;

        private static readonly Pool<SimulatedVessel> pool = new Pool<SimulatedVessel>(Create, Reset);

        public static int PoolSize => pool.Size;

        private static SimulatedVessel Create() => new SimulatedVessel();

        public void Release() => pool.Release(this);

        private static void Reset(SimulatedVessel obj)
        {
            SimulatedPart.Release(obj.parts);
            obj.parts.Clear();
        }

        // dropStage: simulate what's left after that stage is activated, or -1 for the whole vessel
        public static SimulatedVessel Borrow(Vessel v, ReentrySimulation.SimCurves simCurves, double startTime, int limitChutesStage, int dropStage = -1)
        {
            SimulatedVessel vessel = pool.Borrow();
            vessel.Init(v, simCurves, startTime, limitChutesStage, dropStage);
            return vessel;
        }

        private void Init(Vessel v, ReentrySimulation.SimCurves _simCurves, double startTime, int limitChutesStage, int dropStage)
        {
            totalMass = 0;

            List<Part> oParts = v.Parts;
            count = oParts.Count;

            simCurves = _simCurves;

            if (parts.Capacity < count)
                parts.Capacity = count;

            for (int i = 0; i < count; i++)
            {
                if (dropStage >= 0 && oParts[i].IsDecoupledInStage(dropStage))
                    continue;

                SimulatedPart simulatedPart = null;
                bool special = false;
                for (int j = 0; j < oParts[i].Modules.Count; j++)
                {
                    var mp = oParts[i].Modules[j] as ModuleParachute;
                    if (mp != null && v.mainBody.atmosphere)
                    {
                        special = true;
                        simulatedPart = SimulatedParachute.Borrow(mp, simCurves, startTime, limitChutesStage);
                    }
                }

                if (!special)
                {
                    simulatedPart = SimulatedPart.Borrow(oParts[i], simCurves, dropStage);
                }

                parts.Add(simulatedPart);
                totalMass += simulatedPart.totalMass;
            }

            count = parts.Count;
        }

        public Vector3d Drag(Vector3d localVelocity, double dynamicPressurekPa, float mach)
        {
            Vector3d drag = Vector3d.zero;

            double dragFactor = dynamicPressurekPa * PhysicsGlobals.DragCubeMultiplier * PhysicsGlobals.DragMultiplier;

            for (int i = 0; i < count; i++)
            {
                drag += parts[i].Drag(localVelocity, dragFactor, mach);
            }

            return -localVelocity.normalized * drag.magnitude;
        }

        public Vector3d Lift(Vector3d localVelocity, float dynamicPressurekPa, float mach)
        {
            Vector3d lift = Vector3d.zero;

            double liftFactor = dynamicPressurekPa * simCurves.LiftMachCurve.Evaluate(mach);

            for (int i = 0; i < count; i++)
            {
                lift += parts[i].Lift(localVelocity, liftFactor);
            }

            return lift;
        }

        public bool WillChutesDeploy(double altAGL, double altASL, double probableLandingSiteASL, double pressure, double shockTemp, double t,
            double parachuteSemiDeployMultiplier)
        {
            for (int i = 0; i < count; i++)
            {
                if (parts[i].SimulateAndRollback(altAGL, altASL, probableLandingSiteASL, pressure, shockTemp, t, parachuteSemiDeployMultiplier))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Simulate(double altATGL, double altASL, double endASL, double pressure, double shockTemp, double time,
            double semiDeployMultiplier)
        {
            bool deploying = false;
            for (int i = 0; i < count; i++)
            {
                deploying |= parts[i].Simulate(altATGL, altASL, endASL, pressure, shockTemp, time, semiDeployMultiplier);
            }

            return deploying;
        }
    }
}
