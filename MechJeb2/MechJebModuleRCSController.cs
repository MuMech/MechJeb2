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

        // Overdrive
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult overdrive = new EditableDoubleMult(1, 0.01);

        // Advanced options
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool advancedOptions = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool multiAxisFix = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool perThrusterControl = false;

        // Advanced: overdrive scale. While 'overdrive' will range from 0..1,
        // we should reduce it slightly before using it to control the 'waste
        // threshold' tuning parameter, because waste thresholds of 1 or above
        // cause problems by allowing unhelpful thrusters to fire.
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble overdriveScale = 0.9;

        // Advanced: tuning parameters
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorTorque = 1;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorTranslate = 0.005;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorWaste = 1;

        // Debug info
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool debugInfo = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool showThrusterStates = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool onlyWhenMoving = true;

        public string status;
        public string thrusterStateStr = "";
        public string throttleCalcStr  = "";

        // Variables for RCS solving.
        public RCSSolverThread solverThread = new RCSSolverThread();
        private Vector3 lastDirection;
        private bool recalculate = true;
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

        public void ResetThrusters()
        {
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (pm.isEnabled && !pm.isJustForShow)
                    {
                        pm.thrusterPower = 1;
                    }
                }
            }
        }

        private void AddThrusters(Vector3 direction, List<RCSSolver.Thruster> thrusters, Part p, ModuleRCS pm)
        {
            // Find our distance from the vessel's center of
            // mass, in world coordinates.
            Vector3 pos = p.Rigidbody.worldCenterOfMass - vesselState.CoM;

            // Translate to the vessel's reference frame.
            pos = Quaternion.Inverse(vessel.GetTransform().rotation) * pos;

            if (multiAxisFix)
            {
                // Create a single RCSSolver.Thruster for this part. This
                // requires some assumptions about how the game's RCS code will
                // drive the individual thrusters (which we can't control).
                Vector3 partForce = Vector3.zero;
                foreach (Transform t in pm.thrusterTransforms)
                {
                    // The game appears to throttle a thruster based on its
                    // angle from the direction vector. (Each throttle is also
                    // multiplied by the magnitude of the direction vector, but
                    // we can ignore this, since that applies equally to all
                    // thrusters.)
                    Vector3 thrusterDir = Quaternion.Inverse(vessel.GetTransform().rotation) * -t.up;
                    double thrusterThrottle = Vector3.Dot(direction, thrusterDir.normalized);

                    if (thrusterThrottle > 0)
                    {
                        float a = (float)thrusterThrottle;
                        thrusterDir = Vector3.Scale(thrusterDir, new Vector3(a, a, a));
                        partForce += thrusterDir;
                    }
                }
                thrusters.Add(new RCSSolver.Thruster(pos, partForce, p, pm, 0));
            }
            else
            {
                // Create an RCSSolver.Thruster for each RCS jet on this part.
                int i = 0;
                foreach (Transform t in pm.thrusterTransforms)
                {
                    // 'up' is the direction of exhaust, so thrust is the
                    // opposite direction.
                    Vector3 thrustDir = -t.up;

                    // Translate to the vessel's reference frame.
                    thrustDir = Quaternion.Inverse(vessel.GetTransform().rotation) * thrustDir;

                    thrusters.Add(new RCSSolver.Thruster(pos, thrustDir, p, pm, i));
                    i++;
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

            if (!multiAxisFix)
            {
                // We can only set the throttle on a per-part basis, not a per-
                // thruster one. This means we'll run into problems if a part
                // wants to fire multiple thrusters, which is quite likely if
                // the user is translating on multiple axes.
                int axesUsed = (s.X == 0 ? 0 : 1) + (s.Y == 0 ? 0 : 1) + (s.Z == 0 ? 0 : 1);
                if (axesUsed > 1)
                {
                    status += "disabled (translating on multiple axes)";
                    return;
                }
            }
            
            // Note that FlightCtrlState doesn't use the same axes as the
            // vehicle's reference frame. FlightCtrlState coordinates are right-
            // handed, with vessel prograde being -Z. Vessel coordinates
            // are left-handed, with vessel prograde being +Y. Here's how
            // FlightCtrlState relates to various ship directions (and their
            // default keyboard shortcuts):
            //           up (i): y -1
            //         down (k): y +1
            //         left (j): x +1
            //        right (l): x -1
            //      forward (h): z -1
            //     backward (n): z +1
            // To turn this vector into a vessel-relative one, we need to negate
            // each value and also swap the Y and Z values.
            Vector3 direction = new Vector3(-s.X, -s.Z, -s.Y);

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

            if (recalculate)
            {
                lastDirection = direction;
                recalculate = false;

                thrusters.Clear();
                foreach (Part p in vessel.parts)
                {
                    foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                    {
                        if (p.Rigidbody != null && pm.isEnabled && !pm.isJustForShow)
                        {
                            AddThrusters(direction, thrusters, p, pm);
                        }
                    }
                }
                solverThread.post_task(thrusters, direction);
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

            throttles = solverThread.get_throttles();

            // If the throttles we got were bad (due to the vehicle staging or,
            // more likely, the threaded calculating not having completed yet),
            // throttle all RCS thrusters to 0. It's better to not move at all
            // than move in the wrong direction.
            if (throttles == null || throttles.Length != thrusters.Count)
            {
                Debug.Log("MechJeb: RCS throttles not yet calculated");
                throttles = new double[thrusters.Count];
                for (int i = 0; i < thrusters.Count; i++)
                {
                    throttles[i] = 0;
                }
            }

            throttleCalcStr = "";
            throttleCalcStr += "throttles:\n";
            for (int i = 0; i < throttles.Length; i++)
            {
                throttleCalcStr += String.Format("{0:F0}", throttles[i] * 9);
                if (i < throttles.Length - 1)
                {
                    throttleCalcStr += ((i + 1) % 16 == 0) ? "\n" : " ";
                }
            }

            // Apply the calculated throttles to all RCS parts. Keep in mind
            // that each RCS part may have multiple thrusters and thus multiple
            // calculated throttles.

            var rcsPartThrottles = new Dictionary<ModuleRCS, double>();
            for (int i = 0; i < thrusters.Count; i++)
            {
                ModuleRCS pm = thrusters[i].partModule;
                if (perThrusterControl && !multiAxisFix)
                {
                    // Throttle the individual thrusters and set the part-wide
                    // throttle to 1. This would be the ideal behavior, but the
                    // game appears to override these based on the magnitude of
                    // FlightCtrlState.s and the angle from the thrust vector to
                    // the direction vector.
                    pm.thrustForces[thrusters[i].partModuleIndex] = (float)throttles[i];
                    rcsPartThrottles[pm] = 1;
                }
                else
                {
                    // Leave the individual throttles alone. Instead, set the
                    // part-wide throttle to the maximum desired throttle among
                    // its thrusters.
                    double throttle = 0;
                    rcsPartThrottles.TryGetValue(pm, out throttle);
                    throttle = Math.Max(throttle, throttles[i]);
                    rcsPartThrottles[pm] = throttle;
                }
            }

            // Apply part-wide throttles.
            foreach (var pair in rcsPartThrottles)
            {
                pair.Key.thrusterPower = (float)pair.Value;
            }
        }

        private string GetThrusterStates()
        {
            string thrusterStates = "";
            foreach (Part p in vessel.parts)
            {
                bool firstRcsModule = true;
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    thrusterStates += (firstRcsModule ? "[" : " ") + "(";
                    thrusterStates += String.Format("({0:F0}:", pm.thrusterPower * 9);
                    firstRcsModule = false;
                    foreach (float f in pm.thrustForces)
                    {
                        thrusterStates += String.Format(" {0:F0}", f * 9);
                    }
                    thrusterStates += ")";
                }
                if (!firstRcsModule)
                {
                    thrusterStates += "] ";
                }
            }

            if (thrusterStates != "")
            {
                thrusterStates = "thruster states: " + thrusterStates;
            }
            return thrusterStates;
        }

        public override void Drive(FlightCtrlState s)
        {
            status = "";

            // Get thruster states -before- we adjust them.
            if (showThrusterStates && !(s.isNeutral && onlyWhenMoving))
            {
                thrusterStateStr = GetThrusterStates();
            }

            if (smartTranslation)
            {
                AdjustRCSThrottles(s);
            }

            if (smartTranslation && debugInfo)
            {
                if (status != "") status += "\n";
                status += String.Format("control vector: {0:F2} {1:F2} {2:F2}", s.X, s.Y, s.Z);
                status += "\n(thruster/throttle values are 0..9)";
                if (showThrusterStates && thrusterStateStr != "")
                {
                    status += "\n" + thrusterStateStr;
                }
                if (throttleCalcStr != "")
                {
                    status += "\n" + throttleCalcStr;
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
