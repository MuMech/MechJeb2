using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRCSBalancer : ComputerModule
    {
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool smartTranslation = false;

        // Overdrive
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult overdrive = new EditableDoubleMult(1, 0.01);

        // Advanced options
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool advancedOptions = false;

        // This is left in just as an illustration of how the code would likely
        // change if per-thruster throttles were possible.
        private readonly bool perThrusterControl = false;

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
        public string throttleCalcStr = "";

        // Variables for RCS solving.
        public RCSSolverThread solverThread = new RCSSolverThread();
        private Vector3 lastDirection;
        private bool recalculate = true;
        private double[] throttles;
        private List<RCSSolver.Thruster> thrusters = new List<RCSSolver.Thruster>();

        public MechJebModuleRCSBalancer(MechJebCore core)
            : base(core)
        {
            priority = 700;
        }

        public override void OnModuleEnabled()
        {
            solverThread.start();

            base.OnModuleEnabled();
        }

        public override void OnModuleDisabled()
        {
            solverThread.stop();

            base.OnModuleDisabled();
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

            if (!perThrusterControl)
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

                // We should only bother calculating the throttle for this
                // thruster if the game is going to be using it.
                if (partForce.magnitude > 0)
                {
                    thrusters.Add(new RCSSolver.Thruster(pos, partForce, p, pm, 0));
                }
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

                    if (thrustDir.magnitude > 0)
                    {
                        thrusters.Add(new RCSSolver.Thruster(pos, thrustDir, p, pm, i));
                    }
                    i++;
                }
            }
        }

        // Throttles RCS thrusters to keep a vessel balanced during translation.
        // Assumes every thrusters has a max thrust (thrusterPower) of 1 (kN),
        // which is true of both RCS thruster parts in version 0.19.1.
        // TODO: Fix this code for RCS thrusters with thrusterPower != 1.
        protected void AdjustRCSThrottles(FlightCtrlState s)
        {
            if (s.isNeutral)
            {
                ResetThrusters();
                return;
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
            // more likely, the threaded calculation not having completed yet),
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
                if (perThrusterControl)
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

        public void ForceRecalculate()
        {
            recalculate = true;
        }

        private string GetThrusterStates()
        {
            string thrusterStates = "";
            bool firstRcsModule = true;
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!firstRcsModule) thrusterStates += " ";
                    firstRcsModule = false;
                    thrusterStates += String.Format("({0:F0}:", pm.thrusterPower * 9);
                    for (int i = 0; i < pm.thrustForces.Count; i++)
                    {
                        if (i != 0) thrusterStates += ",";
                        thrusterStates += (pm.thrustForces[i] * 9).ToString("F0");
                    }
                    thrusterStates += ")";
                }
            }

            if (thrusterStates != "")
            {
                thrusterStates = "thruster states:\n" + thrusterStates;
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

                if (debugInfo)
                {
                    if (status != "") status += "\n";
                    status += String.Format("control vector: {0:F2} {1:F2} {2:F2} {3:F2} {4:F2} {5:F2}",
                        s.X, s.Y, s.Z, s.roll, s.pitch, s.yaw);
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
            }

            base.Drive(s);
        }
    }
}
