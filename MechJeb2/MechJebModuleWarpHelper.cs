extern alias JetBrainsAnnotations;
using System;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleWarpHelper : DisplayModule
    {
        public enum WarpTarget { Periapsis, Apoapsis, Node, SoI, Time, PhaseAngleT, SuicideBurn, AtmosphericEntry }

        private static readonly string[] warpTargetStrings =
        {
            Localizer.Format("#MechJeb_WarpHelper_Combobox_text1"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text2"),
            Localizer.Format("#MechJeb_WarpHelper_Combobox_text3"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text4"),
            Localizer.Format("#MechJeb_WarpHelper_Combobox_text5"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text6"),
            Localizer.Format("#MechJeb_WarpHelper_Combobox_text7"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text8")
        }; //"periapsis""apoapsis""maneuver node""SoI transition""Time""Phase angle""suicide burn""atmospheric entry"

        [Persistent(pass = (int)Pass.GLOBAL)]
        public WarpTarget warpTarget = WarpTarget.Periapsis;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableTime leadTime = 0;

        public           bool         warping;
        private readonly EditableTime timeOffset = 0;

        private double targetUT;

        [UsedImplicitly]
        [Persistent(pass = (int)(Pass.LOCAL | Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble phaseAngle = 0;

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_WarpHelper_label1"), GUILayout.ExpandWidth(false)); //"Warp to: "
            warpTarget = (WarpTarget)GuiUtils.ComboBox.Box((int)warpTarget, warpTargetStrings, this);
            GUILayout.EndHorizontal();

            if (warpTarget == WarpTarget.Time)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_WarpHelper_label2"), GUILayout.ExpandWidth(true)); //"Warp for: "
                timeOffset.Text = GUILayout.TextField(timeOffset.Text, GUILayout.Width(100));
                GUILayout.EndHorizontal();
            }
            else if (warpTarget == WarpTarget.PhaseAngleT)
            {
                // I wonder if I should check for target that don't make sense
                if (!Core.Target.NormalTargetExists)
                    GUILayout.Label(Localizer.Format("#MechJeb_WarpHelper_label3")); //"You need a target"
                else
                    GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_WarpHelper_label4"), phaseAngle, "º", 60); //"Phase Angle:"
            }

            GUILayout.BeginHorizontal();

            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_WarpHelper_label5"), leadTime, ""); //"Lead time: "

            if (warping)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_WarpHelper_button1"))) //"Abort"
                {
                    warping = false;
                    Core.Warp.MinimumWarp(true);
                }
            }
            else
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_WarpHelper_button2"))) //"Warp"
                {
                    warping = true;

                    switch (warpTarget)
                    {
                        case WarpTarget.Periapsis:
                            targetUT = Orbit.NextPeriapsisTime(VesselState.time);
                            break;

                        case WarpTarget.Apoapsis:
                            if (Orbit.eccentricity < 1) targetUT = Orbit.NextApoapsisTime(VesselState.time);
                            break;

                        case WarpTarget.SoI:
                            if (Orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL) targetUT = Orbit.EndUT;
                            break;

                        case WarpTarget.Node:
                            if (Vessel.patchedConicsUnlocked() && Vessel.patchedConicSolver.maneuverNodes.Any())
                                targetUT = Vessel.patchedConicSolver.maneuverNodes[0].UT;
                            break;

                        case WarpTarget.Time:
                            targetUT = VesselState.time + timeOffset;
                            break;

                        case WarpTarget.PhaseAngleT:
                            if (Core.Target.NormalTargetExists)
                            {
                                Orbit reference;
                                if (Core.Target.TargetOrbit.referenceBody == Orbit.referenceBody)
                                    reference = Orbit; // we orbit arround the same body
                                else
                                    reference = Orbit.referenceBody.orbit;
                                // From Kerbal Alarm Clock
                                double angleChangePerSec = 360 / Core.Target.TargetOrbit.period - 360 / reference.period;
                                double currentAngle = reference.PhaseAngle(Core.Target.TargetOrbit, VesselState.time);
                                double angleDigff = currentAngle - phaseAngle;
                                if (angleDigff > 0 && angleChangePerSec > 0)
                                    angleDigff -= 360;
                                if (angleDigff < 0 && angleChangePerSec < 0)
                                    angleDigff += 360;
                                double TimeToTarget = Math.Floor(Math.Abs(angleDigff / angleChangePerSec));
                                targetUT = VesselState.time + TimeToTarget;
                            }

                            break;

                        case WarpTarget.AtmosphericEntry:
                            try
                            {
                                targetUT = Vessel.orbit.NextTimeOfRadius(VesselState.time,
                                    VesselState.mainBody.Radius + VesselState.mainBody.RealMaxAtmosphereAltitude());
                            }
                            catch
                            {
                                warping = false;
                            }

                            break;

                        case WarpTarget.SuicideBurn:
                            try
                            {
                                targetUT = OrbitExtensions.SuicideBurnCountdown(Orbit, VesselState, Vessel) + VesselState.time;
                            }
                            catch
                            {
                                warping = false;
                            }

                            break;

                        default:
                            targetUT = VesselState.time;
                            break;
                    }
                }
            }

            GUILayout.EndHorizontal();

            Core.Warp.useQuickWarpInfoItem();

            if (warping)
                GUILayout.Label(Localizer.Format("#MechJeb_WarpHelper_label6") + (leadTime > 0 ? GuiUtils.TimeToDHMS(leadTime) + " before " : "") +
                                warpTargetStrings[(int)warpTarget] + "."); //"Warping to "

            Core.Warp.ControlWarpButton();

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override void OnFixedUpdate()
        {
            if (!warping) return;

            if (warpTarget == WarpTarget.SuicideBurn)
            {
                try
                {
                    targetUT = OrbitExtensions.SuicideBurnCountdown(Orbit, VesselState, Vessel) + VesselState.time;
                }
                catch
                {
                    warping = false;
                }
            }

            double target = targetUT - leadTime;

            if (target < VesselState.time + 1)
            {
                Core.Warp.MinimumWarp(true);
                warping = false;
            }
            else
            {
                Core.Warp.WarpToUT(target);
            }
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(240), GUILayout.Height(50) };

        public override bool IsActive() => warping;

        public override string GetName() => Localizer.Format("#MechJeb_WarpHelper_title"); //"Warp Helper"

        public override string IconName() => "Warp Helper";

        public MechJebModuleWarpHelper(MechJebCore core) : base(core) { }
    }
}
