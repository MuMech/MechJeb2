using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRCSController : ComputerModule
    {
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool smartTranslation = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool runSolver = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool applyResult = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool thrusterPowerControl = false;

        public int thrustersDisabled = 0;

        public RCSSolver solver = new RCSSolver();

        private System.Random rand = new System.Random();
        private List<RCSSolver.Thruster> thrusters = new List<RCSSolver.Thruster>();

        public EditableDoubleMult thrusterPower = new EditableDoubleMult(1, 0.01);

        public Vector3d targetVelocity = Vector3d.zero;
        private Boolean driveToTarget = false;

        public PIDControllerV pid;

        public double Kp = 0.5, Ki = 0, Kd = 0;

        public MechJebModuleRCSController(MechJebCore core)
            : base(core)
        {
            priority = 600;
            pid = new PIDControllerV(Kp, Ki, Kd, 1, -1);
        }

        public override void OnModuleEnabled()
        {
            pid = new PIDControllerV(Kp, Ki, Kd, 1, -1);
            base.OnModuleEnabled();
        }

        public void SetTargetWorldVelocity(Vector3d vel)
        {
            targetVelocity = vel;
            driveToTarget = true;
        }

        public void SetTargetLocalVelocity(Vector3d vel)
        {
            targetVelocity = vessel.GetTransform().rotation * vel;
            driveToTarget = true;
        }

        public void ClearTargetVelocity()
        {
            driveToTarget = false;
        }

        public void EnableAllThrusters()
        {
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!pm.isJustForShow)
                    {
                        pm.Enable();
                    }
                }
            }
        }

        public void ApplyThrusterPower()
        {
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if ((pm.isEnabled) && (!pm.isJustForShow))
                    {
                        int numForces = pm.thrustForces.Count;
                        pm.Enable();
                        pm.thrusterPower = (float)thrusterPower.val;
                    }
                }
            }
        }

        protected void AdjustRCSThrottles(FlightCtrlState s)
        {
            if (s.isNeutral) return;
            
            Vector3 direction = new Vector3(s.X, s.Y, s.Z);
            thrusters.Clear();
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!pm.isJustForShow && p.Rigidbody != null)
                    {
                        Vector3 pos = p.Rigidbody.worldCenterOfMass - vesselState.CoM;
                        // Create an RCSSolver.Thruster for each RCS jet on this
                        // part.
                        foreach (Transform t in pm.thrusterTransforms)
                        {
                            Vector3 thrustDir = -t.up * pm.thrusterPower;
                            thrusters.Add(new RCSSolver.Thruster(pos, thrustDir, p, pm));
                        }
                    }
                }
            }

            if (!runSolver) return;

            double[] throttles;
            solver.run(thrusters, direction, out throttles);

            if (!applyResult) return;

            thrustersDisabled = 0;

            // Disable all thrusters.
            for (int i = 0; i < thrusters.Count; i++)
            {
                thrusters[i].partModule.Disable();
                thrustersDisabled++;
            }

            // This is extremely cheesy, but until I understand how
            // to throttle RCS thrusters properly, we'll simply
            // enable/disable them randomly to simulate a throttle.
            // This actually works pretty well.
            for (int i = 0; i < thrusters.Count; i++)
            {
                if (rand.NextDouble() < throttles[i])
                {
                    thrusters[i].partModule.Enable();
                    thrustersDisabled--;
                }
            }
        }

        public override void Drive(FlightCtrlState s)
        {
            if (smartTranslation)
            {
                AdjustRCSThrottles(s);
            }

            if (driveToTarget)
            {
                Vector3d worldVelocityDelta = vesselState.velocityVesselOrbit - targetVelocity;
                worldVelocityDelta += TimeWarp.fixedDeltaTime * vesselState.gravityForce; //account for one frame's worth of gravity
                Vector3d velocityDelta = Quaternion.Inverse(vessel.GetTransform().rotation) * worldVelocityDelta;
                Vector3d rcs = new Vector3d();

                foreach (Vector6.Direction dir in Enum.GetValues(typeof(Vector6.Direction)))
                {
                    if (vesselState.rcsThrustAvailable[dir] > 0)
                    {
                        double dV = Vector3d.Dot(velocityDelta, Vector6.directions[dir]) / (vesselState.rcsThrustAvailable[dir] * TimeWarp.fixedDeltaTime / vesselState.mass);
                        if (dV > 0)
                        {
                            rcs += Vector6.directions[dir] * dV;
                        }
                    }
                }

                rcs = pid.Compute(rcs);

                s.X = Mathf.Clamp((float)rcs.x, -1, 1);
                s.Y = Mathf.Clamp((float)rcs.z, -1, 1);
                s.Z = Mathf.Clamp((float)rcs.y, -1, 1);
            }

            base.Drive(s);
        }
    }
}
