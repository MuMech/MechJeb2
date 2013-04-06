using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        public bool genuineThrottle = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool forceRecalculate = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool multithreading = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool thrusterPowerControl = false;

        public int thrustersUsed = 0;

        private RCSSolverThread solverThread = new RCSSolverThread();

        private Vector3 lastDirection;
        private bool recalculate = true;
        private System.Random rand = new System.Random();
        private double[] throttles;
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
            base.OnModuleEnabled();
            solverThread.start();
            pid = new PIDControllerV(Kp, Ki, Kd, 1, -1);
        }

        public override void OnModuleDisabled()
        {
            solverThread.stop();
            base.OnModuleDisabled();
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

        public void ResetThrusters()
        {
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!pm.isJustForShow)
                    {
                        pm.Enable();
                        pm.thrusterPower = 1;
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
            if (s.isNeutral)
            {
                ResetThrusters();
                recalculate = true;
                return;
            }
            
            Vector3 direction = new Vector3(s.X, s.Y, s.Z);

            // We should only recalculate if the direction is unchanged. If the
            // user is holding down a translate button, the movement direction
            // is the same, and the center of mass doesn't change that quickly!
            if (!direction.Equals(lastDirection))
            {
                recalculate = true;
            }

            if (!runSolver) return;

            if (forceRecalculate) recalculate = true;

            // TODO: This should be a task in a thread pool to avoid UI lag.
            if (recalculate)
            {
                lastDirection = direction;
                recalculate = false;

                thrusters.Clear();
                foreach (Part p in vessel.parts)
                {
                    if (p.Rigidbody != null)
                    {
                        foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                        {
                            if (!pm.isJustForShow)
                            {
                                Vector3 pos = p.Rigidbody.worldCenterOfMass - vesselState.CoM;
                                // Create an RCSSolver.Thruster for each RCS jet
                                // on this part.
                                foreach (Transform t in pm.thrusterTransforms)
                                {
                                    Vector3 thrustDir = -t.up * pm.thrusterPower;
                                    thrusters.Add(new RCSSolver.Thruster(pos, thrustDir, p, pm));
                                }
                            }
                        }
                    }
                }
                if (multithreading)
                {
                    solverThread.post_task(thrusters, direction);
                }
                else
                {
                    new RCSSolver().run(thrusters, direction, out throttles);
                }
            }
            else
            {
                // Make sure all the thrusters we know about are still part of
                // the craft (in case the user de-staged while translating).
                foreach (RCSSolver.Thruster thruster in thrusters)
                {
                    if (!vessel.parts.Contains(thruster.part))
                    {
                        recalculate = true;
                        return;
                    }
                }
            }

            if (!applyResult) return;

            thrustersUsed = 0;

            throttles = solverThread.get_throttles();

            if (throttles.Length != thrusters.Count) return;

            // Now we need to apply these throttle settings to the RCS part.
            // Keep in mind that each RCS part may have multiple thrusters and
            // thus multiple calculated throttles.

            if (genuineThrottle)
            {
                for (int i = 0; i < thrusters.Count; i++)
                {
                    thrusters[i].partModule.thrusterPower = 0;
                }

                for (int i = 0; i < thrusters.Count; i++)
                {
                    ModuleRCS pm = thrusters[i].partModule;
                    if (throttles[i] > pm.thrusterPower)
                    {
                        if (pm.thrusterPower == 0)
                        {
                            pm.Enable();
                            thrustersUsed++;
                        }
                        pm.thrusterPower = (float) throttles[i];
                    }
                }
            }
            else
            {
                // Disable all thrusters.
                for (int i = 0; i < thrusters.Count; i++)
                {
                    thrusters[i].partModule.Disable();
                }

                // This is extremely cheesy, but until I understand how
                // to throttle RCS thrusters properly, we'll simply
                // enable/disable them randomly to simulate a throttle.
                // This actually sort of works.
                for (int i = 0; i < thrusters.Count; i++)
                {
                    if (rand.NextDouble() < throttles[i])
                    {
                        ModuleRCS pm = thrusters[i].partModule;
                        if (!pm.isEnabled)
                        {
                            pm.Enable();
                            thrustersUsed++;
                        }
                    }
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
