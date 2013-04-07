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
        public bool showThrusterStates = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool advancedOptions = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool debugInfo = false;

        // Advanced options
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool interpolateThrottle = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool alwaysRecalculate = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool multithreading = true;

        // Tuning parameters
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorTorque = 1;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorTranslate = 0.005;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorWaste = 1;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamWasteThreshold = 1;

        public string thrusterStates;

        // Variables for RCS solving.
        public int thrustersUsed = 0;
        public RCSSolverThread solverThread = new RCSSolverThread();
        private Vector3 lastDirection;
        private bool recalculate = true;
        private System.Random rand = new System.Random();
        private double[] throttles;
        private List<RCSSolver.Thruster> thrusters = new List<RCSSolver.Thruster>();

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

        public void ResetThrusters(bool enable = true)
        {
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!pm.isJustForShow)
                    {
                        if (enable)
                        {
                            if (!pm.isEnabled) pm.Enable();
                            pm.thrusterPower = 1;
                        }
                        else
                        {
                            if (pm.isEnabled) pm.Disable();
                            pm.thrusterPower = 0;
                        }
                    }
                }
            }
        }

        protected void AdjustRCSThrottles(FlightCtrlState s)
        {
            if (s.isNeutral)
            {
                ResetThrusters();
                return;
            }
            
            Vector3 direction = new Vector3(s.X, s.Y, s.Z);

            // We should only recalculate if the direction is unchanged. If the
            // user is holding down or tapping a translate button, the movement
            // direction is the same, and the center of mass doesn't change that
            // quickly!
            // TODO: Consider precalculating the six cardinal directions. This
            // would allow translation along any axis to occur without waiting
            // for calculations.
            if (!direction.Equals(lastDirection))
            {
                recalculate = true;
            }

            if (alwaysRecalculate) recalculate = true;

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
                                    Vector3 thrustDir = t.up * pm.thrusterPower;
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
                    throttles = new RCSSolver().run(thrusters, direction);
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

            int sumThrusters = 0;

            if (multithreading)
            {
                throttles = solverThread.get_throttles();
            }

            // If the throttles we got were bad (due to the vehicle staging or,
            // more likely, the threaded calculating not having completed yet),
            // throttle all RCS thrusters to 0. It's better to not move at all
            // than move in the wrong direction.
            if (throttles == null || throttles.Length != thrusters.Count)
            {
                Debug.Log("throttles weren't ready!");
                throttles = new double[thrusters.Count];
                for (int i = 0; i < thrusters.Count; i++)
                {
                    throttles[i] = 0;
                }
            }

            // Now we need to apply these throttle settings to the RCS part.
            // Keep in mind that each RCS part may have multiple thrusters and
            // thus multiple calculated throttles.

            if (!interpolateThrottle)
            {
                var rcsPartThrottles = new Dictionary<ModuleRCS, double>();
                for (int i = 0; i < thrusters.Count; i++)
                {
                    ModuleRCS pm = thrusters[i].partModule;
                    double throttle = 0;
                    rcsPartThrottles.TryGetValue(pm, out throttle);
                    throttle = Math.Max(throttle, throttles[i]);
                    rcsPartThrottles[pm] = throttle;
                }

                foreach (var pair in rcsPartThrottles)
                {
                    ModuleRCS pm = pair.Key;
                    pm.thrusterPower = (float)pair.Value;
                    pm.Enable();
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
                            sumThrusters++;
                        }
                    }
                }
            }

            thrustersUsed = sumThrusters;
        }

        private string GetThrusterStates()
        {
            string thrusterStates = "";
            foreach (Part p in vessel.parts)
            {
                bool hasRcs = false;
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    thrusterStates += hasRcs ? " " : "[";
                    thrusterStates += String.Format("{0},{1}", (pm.isEnabled ? "e" : "d"), pm.thrusterPower);
                    foreach (float f in pm.thrustForces)
                    {
                        thrusterStates += String.Format(",{0:F2}", f);
                    }
                    hasRcs = true;
                }
                if (hasRcs)
                {
                    thrusterStates += "] ";
                }
            }
            return thrusterStates;
        }

        public override void Drive(FlightCtrlState s)
        {
            if (smartTranslation)
            {
                AdjustRCSThrottles(s);
            }

            thrusterStates = GetThrusterStates();

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
