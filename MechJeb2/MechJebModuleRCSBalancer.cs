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
        private Vector3 lastRotation;
        private int lastPartCount;
        private bool recalculate = true;

        private List<RCSSolver.Thruster> thrusters = new List<RCSSolver.Thruster>();

        public MechJebModuleRCSBalancer(MechJebCore core)
            : base(core)
        {
            priority = 700;
        }

        public override void OnModuleEnabled()
        {
            UpdateTuningParameters();
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
            foreach (var t in thrusters)
            {
                t.RestoreOriginalForce();
            }
        }

        private void AddThrusters(Vector3 direction, Vector3 rotation, List<RCSSolver.Thruster> thrusters, Part p, ModuleRCS pm)
        {
            // Find our distance from the vessel's center of
            // mass, in world coordinates.
            Vector3 pos = p.Rigidbody.worldCenterOfMass - vesselState.CoM;

            // Translate to the vessel's reference frame.
            pos = Quaternion.Inverse(vessel.GetTransform().rotation) * pos;

            // Create a single RCSSolver.Thruster for this part. This
            // requires some assumptions about how the game's RCS code will
            // drive the individual thrusters (which we can't control).
            Vector3 partForce = Vector3.zero;
            foreach (Transform t in pm.thrusterTransforms)
            {
                // The game appears to throttle a thruster based on the dot
                // product of its thrust vector (normalized) and the
                // direction vector (not normalized!).
                Vector3 thrustDir = Quaternion.Inverse(vessel.GetTransform().rotation) * -t.up;
                thrustDir.Normalize();
                float translateThrottle = Mathf.Clamp01(Vector3.Dot(direction, thrustDir));
                float rotateThrottle = 0; // TODO
                float throttle = Mathf.Max(translateThrottle, rotateThrottle);

                partForce += thrustDir * throttle;
            }

            // We should only bother calculating the throttle for this
            // thruster if the game is going to be using it.
            if (partForce.magnitude > 0)
            {
                thrusters.Add(new RCSSolver.Thruster(pos, partForce * pm.thrusterPower, p, pm));
            }
        }

        // Throttles RCS thrusters to keep a vessel balanced during translation.
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
            Vector3 rotation = new Vector3(s.pitch, s.roll, s.yaw);
            int partCount = vessel.parts.Count;

            // We should only recalculate if the direction is unchanged. If the
            // user is holding down or tapping a translate button, the movement
            // direction is the same, and the center of mass doesn't change that
            // quickly!
            // TODO: Consider precalculating the six cardinal directions. This
            // would allow translation along any axis to occur without waiting
            // for calculations.
            if (direction != lastDirection || rotation != lastRotation || partCount != lastPartCount)
            {
                recalculate = true;
            }
            lastDirection = direction;
            lastRotation = rotation;
            lastPartCount = partCount;

            if (recalculate)
            {
                recalculate = false;

                // ModuleRCS has no originalThrusterPower attribute, so we have
                // to explicitly reset it.
                ResetThrusters();
                thrusters.Clear();
                foreach (Part p in vessel.parts)
                {
                    foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                    {
                        if (p.Rigidbody != null && pm.isEnabled && !pm.isJustForShow)
                        {
                            AddThrusters(direction, rotation, thrusters, p, pm);
                        }
                    }
                }
                solverThread.post_task(thrusters, direction, rotation);
            }

            double[] throttles = solverThread.get_throttles();

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

            // Apply the calculated throttles to all RCS parts.
            for (int i = 0; i < thrusters.Count; i++)
            {
                thrusters[i].partModule.thrusterPower = (float)throttles[i];
            }
        }

        public void UpdateTuningParameters()
        {
            double wasteThreshold = overdrive * overdriveScale;
            solverThread.solver.wasteThreshold = wasteThreshold;
            solverThread.solver.factorTorque = tuningParamFactorTorque;
            solverThread.solver.factorTranslate = tuningParamFactorTranslate;
            solverThread.solver.factorWaste = tuningParamFactorWaste;
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
