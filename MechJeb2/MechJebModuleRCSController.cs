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
        public bool onlyWhenMoving = true;
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
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool thrusterPowerControl = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult thrusterPower = new EditableDoubleMult(1, 0.01);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool globalThrusterPower = false;

        // Tuning parameters
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorTorque = 1;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorTranslate = 0.005;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorWaste = 1;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamWasteThreshold = 1;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableInt thrusterTransformMode = 1;

        public string thrusterStates;
        public string controlVector;
        public string status;

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
            
            // Note that Y and Z are swapped! FlightCtrlState doesn't use the
            // same axes as the vehicle's reference frame.
            Vector3 direction = new Vector3(s.X, s.Z, s.Y);

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
                                // Find our distance from the vessel's center of
                                // mass, in world coordinates.
                                Vector3 pos = p.Rigidbody.worldCenterOfMass - vesselState.CoM;

                                switch ((thrusterTransformMode / 10) % 10)
                                {
                                    case 1:
                                        // Translate to the part's reference frame.
                                        pos = Quaternion.Inverse(p.transform.rotation) * pos;
                                        break;
                                    case 2:
                                        // Translate to the vessel's reference frame.
                                        pos = Quaternion.Inverse(vessel.GetTransform().rotation) * pos;
                                        break;
                                }

                                // Create an RCSSolver.Thruster for each RCS jet
                                // on this part.
                                int i = 0;
                                foreach (Transform t in pm.thrusterTransforms)
                                {
                                    Vector3 thrustDir = -t.up;

                                    // Translate to the ship's reference frame?!!
                                    Vector3 tdT = Quaternion.Inverse(t.rotation) * thrustDir;
                                    Vector3 tdP = Quaternion.Inverse(p.transform.rotation) * thrustDir;
                                    Vector3 tdV = Quaternion.Inverse(vessel.GetTransform().rotation) * thrustDir;
                                    Vector3 tdPV = Quaternion.Inverse(vessel.GetTransform().rotation) * tdP;
                                    Vector3 tdDuck = vessel.GetTransform().InverseTransformDirection(thrustDir);

                                    switch (thrusterTransformMode % 10)
                                    {
                                        case 1: thrustDir = tdP; break;
                                        case 2: thrustDir = tdV; break;
                                        case 3: thrustDir = tdPV; break;
                                        case 4: thrustDir = tdDuck; break;
                                    }

                                    //thrustDir *= pm.thrusterPower;

                                    thrusters.Add(new RCSSolver.Thruster(pos, thrustDir, p, pm, i));
                                    i++;
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

            status += "throttles:\n";
            for (int i = 0; i < throttles.Length; i++)
            {
                status += String.Format("{0:F0}", throttles[i] * 9);
                if ((i + 1) % 16 == 0)
                {
                    status += "\n";
                }
                else
                {
                    if ((i + 1) % 4 == 0) status += ",";
                    status += " ";
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
                    if (thrusterPowerControl)
                    {
                        double force = globalThrusterPower ? 1 : throttles[i];
                        pm.thrustForces[thrusters[i].partModuleIndex] = (float)(force * thrusterPower);
                        rcsPartThrottles[pm] = thrusterPower;
                    }
                    else
                    {
                        double throttle = 0;
                        rcsPartThrottles.TryGetValue(pm, out throttle);
                        throttle = Math.Max(throttle, throttles[i]);
                        rcsPartThrottles[pm] = throttle;
                    }
                }

                foreach (var pair in rcsPartThrottles)
                {
                    ModuleRCS pm = pair.Key;
                    pm.thrusterPower = (float)pair.Value;
                    pm.Enable();
                }
                Debug.Log("applied throttles");
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
            bool first = true;
            foreach (Part p in vessel.parts)
            {
                bool hasRcs = false;
                int count = 0;
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (first)
                    {
                        first = false;
                        try
                        {
                            Vector3 pos1 = p.Rigidbody.worldCenterOfMass - vesselState.CoM;
                            Vector3 pos2 = Quaternion.Inverse(p.transform.rotation) * pos1;
                            Vector3 pos3 = Quaternion.Inverse(vessel.GetTransform().rotation) * pos1;
                            Vector3 pos4 = Quaternion.Inverse(vessel.GetTransform().rotation) * pos2;

                            status += String.Format("wcom {0}, lcom {1}, pwcom {2}, plcom {3}, pos1 {4}, posP {5}, posV {6}, posPV {7}",
                                vessel.findWorldCenterOfMass(), vessel.findLocalCenterOfMass(),
                                p.Rigidbody.worldCenterOfMass, p.Rigidbody.centerOfMass, pos1, pos2, pos3, pos4);

                            bool firstThruster = true;
                            foreach (Transform t in pm.thrusterTransforms)
                            {
                                Vector3 thrustDir = -t.up;

                                // Translate to the ship's reference frame?!!
                                Vector3 tdT = Quaternion.Inverse(t.rotation) * thrustDir;
                                Vector3 tdP = Quaternion.Inverse(p.transform.rotation) * thrustDir;
                                Vector3 tdV = Quaternion.Inverse(vessel.GetTransform().rotation) * thrustDir;
                                Vector3 tdPV = Quaternion.Inverse(vessel.GetTransform().rotation) * tdP;
                                Vector3 tdDuck = vessel.GetTransform().InverseTransformDirection(thrustDir);
                                if (firstThruster)
                                {
                                    firstThruster = false;
                                    status += String.Format(", tdir1 {0}, P {1}, V {2}, PV {3}, duck {4}",
                                        thrustDir.normalized, tdP.normalized, tdV.normalized, tdPV.normalized,
                                        tdDuck.normalized);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            status = "bad format string: " + e.Message;
                        }
                    }

                    thrusterStates += hasRcs ? " " : "[";
                    thrusterStates += "(";
                    thrusterStates += String.Format("({0:F0}", pm.thrusterPower * 9);
                    bool firstForce = false;
                    foreach (float f in pm.thrustForces)
                    {
                        if (firstForce) firstForce = false;
                        else thrusterStates += " ";

                        thrusterStates += String.Format("{0:F0}", f * 9);
                    }
                    thrusterStates += ")";
                    hasRcs = true;
                    count++;
                    if (count % 2 == 0) thrusterStates += "\n";
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
            if (!(s.isNeutral && core.rcs.onlyWhenMoving))
            {
                status = "";
                thrusterStates = GetThrusterStates();
                controlVector = String.Format("{0:F2} {1:F2} {2:F2}", s.X, s.Y, s.Z);
            }

            if (smartTranslation)
            {
                if ((s.X == 0 ? 0 : 1) + (s.Y == 0 ? 0 : 1) + (s.Z == 0 ? 0 : 1) > 1)
                {
                    status = "disabled (translating on multiple axes\n";
                }
                else
                {
                    AdjustRCSThrottles(s);
                }
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
