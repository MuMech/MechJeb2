extern alias JetBrainsAnnotations;
using System;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    // The warp to an event: it runs all flight, with its window (MechJebModuleWarpHelperWindow) open or closed
    public class MechJebModuleWarpHelper : ComputerModule
    {
        public enum WarpTarget { Periapsis, Apoapsis, Node, SoI, Time, PhaseAngleT, HoverslamBurn, AtmosphericEntry }

        internal static readonly string[] warpTargetStrings = { Localizer.Format("#MechJeb_WarpHelper_Combobox_text1"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text2"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text3"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text4"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text5"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text6"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text7"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text8") }; //"periapsis""apoapsis""maneuver node""SoI transition""Time""Phase angle""hoverslam burn""atmospheric entry"

        [Persistent(pass = (int)Pass.GLOBAL)]
        public WarpTarget warpTarget = WarpTarget.Periapsis;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableTime leadTime = 0;

        public bool warping;
        public readonly EditableTime timeOffset = 0;

        private double targetUT;

        [UsedImplicitly, Persistent(pass = (int)(Pass.LOCAL | Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble phaseAngle = 0;

        // Resolve the target time for the current warpTarget setting and begin warping. OnFixedUpdate
        // then drives Core.Warp toward (targetUT - leadTime). Callable directly (e.g. from kOS) as the
        // high-level entry point; the GUI Warp button just calls this.
        public void StartWarp()
        {
            // only start warping once the target is resolved, otherwise we would warp to the previous targetUT
            bool valid = true;

            switch (warpTarget)
            {
                case WarpTarget.Periapsis:
                    targetUT = Orbit.NextPeriapsisTime(VesselState.Time);
                    break;

                case WarpTarget.Apoapsis:
                    if (Orbit.eccentricity < 1) targetUT = Orbit.NextApoapsisTime(VesselState.Time);
                    else valid = false;
                    break;

                case WarpTarget.SoI:
                    if (Orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL) targetUT = Orbit.EndUT;
                    else valid = false;
                    break;

                case WarpTarget.Node:
                    if (Vessel.patchedConicsUnlocked() && Vessel.patchedConicSolver.maneuverNodes.Any())
                        targetUT = Vessel.patchedConicSolver.maneuverNodes[0].UT;
                    else valid = false;
                    break;

                case WarpTarget.Time:
                    targetUT = VesselState.Time + timeOffset;
                    break;

                case WarpTarget.PhaseAngleT:
                    if (Core.Target.NormalTargetExists)
                    {
                        Orbit reference;
                        if (Core.Target.TargetOrbit.referenceBody == Orbit.referenceBody)
                            reference = Orbit; // we orbit arround the same body
                        else
                            reference = Orbit.referenceBody.orbit; // null when we orbit the Sun and the target does not
                        if (reference == null)
                        {
                            valid = false;
                            break;
                        }

                        // From Kerbal Alarm Clock
                        double angleChangePerSec = 360 / Core.Target.TargetOrbit.period - 360 / reference.period;
                        double currentAngle = reference.PhaseAngle(Core.Target.TargetOrbit, VesselState.Time);
                        double angleDigff = currentAngle - phaseAngle;
                        if (angleDigff > 0 && angleChangePerSec > 0)
                            angleDigff -= 360;
                        if (angleDigff < 0 && angleChangePerSec < 0)
                            angleDigff += 360;
                        double TimeToTarget = Math.Floor(Math.Abs(angleDigff / angleChangePerSec));
                        // equal periods: the phase angle never changes
                        if (double.IsNaN(TimeToTarget) || double.IsInfinity(TimeToTarget))
                            valid = false;
                        else
                            targetUT = VesselState.Time + TimeToTarget;
                    }
                    else
                    {
                        valid = false;
                    }

                    break;

                case WarpTarget.AtmosphericEntry:
                    try
                    {
                        targetUT = Vessel.orbit.NextTimeOfRadius(VesselState.Time,
                            MainBody.Radius + MainBody.RealMaxAtmosphereAltitude());
                    }
                    catch
                    {
                        valid = false;
                    }

                    break;

                case WarpTarget.HoverslamBurn:
                    try
                    {
                        targetUT = Core.GetComputerModule<MechJebModuleHoverslamSimulation>().IgnitionUT;
                    }
                    catch
                    {
                        valid = false;
                    }

                    break;

                default:
                    targetUT = VesselState.Time;
                    break;
            }

            if (valid)
                warping = true;
        }

        // Stop warping and drop back to minimum (1x) warp.
        public void AbortWarp()
        {
            warping = false;
            Core.Warp.MinimumWarp(true);
        }

        public override void OnFixedUpdate()
        {
            if (!warping) return;

            if (warpTarget == WarpTarget.HoverslamBurn)
            {
                try
                {
                    targetUT = Core.GetComputerModule<MechJebModuleHoverslamSimulation>().IgnitionUT;
                }
                catch
                {
                    warping = false;
                }
            }

            double target = targetUT - leadTime;

            if (target < VesselState.Time + 1)
            {
                Core.Warp.MinimumWarp(true);
                warping = false;
            }
            else
            {
                Core.Warp.WarpToUT(target);
            }
        }

        public MechJebModuleWarpHelper(MechJebCore core) : base(core) => Enabled = true;
    }
}
