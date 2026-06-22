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
        public enum WarpTarget { Periapsis, Apoapsis, Node, SoI, Time, PhaseAngleT, HoverslamBurn, AtmosphericEntry }

        private static readonly string[] warpTargetStrings = { Localizer.Format("#MechJeb_WarpHelper_Combobox_text1"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text2"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text3"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text4"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text5"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text6"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text7"), Localizer.Format("#MechJeb_WarpHelper_Combobox_text8") }; //"periapsis""apoapsis""maneuver node""SoI transition""Time""Phase angle""hoverslam burn""atmospheric entry"

        [Persistent(pass = (int)Pass.GLOBAL)]
        public WarpTarget warpTarget = WarpTarget.Periapsis;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableTime leadTime = 0;

        public bool warping;
        public readonly EditableTime timeOffset = 0;

        private double targetUT;

        [UsedImplicitly, Persistent(pass = (int)(Pass.LOCAL | Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble phaseAngle = 0;

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_WarpHelper_label1"), GuiUtils.LayoutNoExpandWidth); //"Warp to: "
            warpTarget = (WarpTarget)GuiUtils.ComboBox.Box((int)warpTarget, warpTargetStrings, this);
            GUILayout.EndHorizontal();

            if (warpTarget == WarpTarget.Time)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_WarpHelper_label2"), GuiUtils.LayoutExpandWidth); //"Warp for: "
                timeOffset.Text = GUILayout.TextField(timeOffset.Text, GuiUtils.LayoutWidth(100));
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
                    AbortWarp();
            }
            else
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_WarpHelper_button2"))) //"Warp"
                    StartWarp();
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

        // Resolve the target time for the current warpTarget setting and begin warping. OnFixedUpdate
        // then drives Core.Warp toward (targetUT - leadTime). Callable directly (e.g. from kOS) as the
        // high-level entry point; the GUI Warp button just calls this.
        public void StartWarp()
        {
            warping = true;

            switch (warpTarget)
            {
                case WarpTarget.Periapsis:
                    targetUT = Orbit.NextPeriapsisTime(VesselState.Time);
                    break;

                case WarpTarget.Apoapsis:
                    if (Orbit.eccentricity < 1) targetUT = Orbit.NextApoapsisTime(VesselState.Time);
                    break;

                case WarpTarget.SoI:
                    if (Orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL) targetUT = Orbit.EndUT;
                    break;

                case WarpTarget.Node:
                    if (Vessel.patchedConicsUnlocked() && Vessel.patchedConicSolver.maneuverNodes.Any())
                        targetUT = Vessel.patchedConicSolver.maneuverNodes[0].UT;
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
                            reference = Orbit.referenceBody.orbit;
                        // From Kerbal Alarm Clock
                        double angleChangePerSec = 360 / Core.Target.TargetOrbit.period - 360 / reference.period;
                        double currentAngle = reference.PhaseAngle(Core.Target.TargetOrbit, VesselState.Time);
                        double angleDigff = currentAngle - phaseAngle;
                        if (angleDigff > 0 && angleChangePerSec > 0)
                            angleDigff -= 360;
                        if (angleDigff < 0 && angleChangePerSec < 0)
                            angleDigff += 360;
                        double TimeToTarget = Math.Floor(Math.Abs(angleDigff / angleChangePerSec));
                        targetUT = VesselState.Time + TimeToTarget;
                    }

                    break;

                case WarpTarget.AtmosphericEntry:
                    try
                    {
                        targetUT = Vessel.orbit.NextTimeOfRadius(VesselState.Time,
                            VesselState.MainBody.Radius + VesselState.MainBody.RealMaxAtmosphereAltitude());
                    }
                    catch
                    {
                        warping = false;
                    }

                    break;

                case WarpTarget.HoverslamBurn:
                    try
                    {
                        targetUT = Core.GetComputerModule<MechJebModuleHoverslamSimulation>().IgnitionUT;
                    }
                    catch
                    {
                        warping = false;
                    }

                    break;

                default:
                    targetUT = VesselState.Time;
                    break;
            }
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

        protected override GUILayoutOption[] WindowOptions() => new[] { GuiUtils.LayoutWidth(240), GUILayout.Height(50) };

        public override bool IsActive() => warping;

        public override string GetName() => Localizer.Format("#MechJeb_WarpHelper_title"); //"Warp Helper"

        public override string IconName() => "Warp Helper";

        public MechJebModuleWarpHelper(MechJebCore core) : base(core) { }
    }
}
