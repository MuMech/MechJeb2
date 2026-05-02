using System;
using KSP.Localization;
using MuMech.Landing;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModulePoweredDescentGuidanceWindow : DisplayModule
    {
        private string _planThrottleText = "";
        private string _minThrottleText = "";
        private string _clearanceText = "";
        private string _terminalHandoverText = "";
        private string _terminalGlideText = "";
        private string _pulsePeriodText = "";

        public MechJebModulePoweredDescentGuidanceWindow(MechJebCore core)
            : base(core)
        {
        }

        public override string GetName()
        {
            return "Powered Descent Guidance";
        }

        public override string IconName()
        {
            return "Landing Guidance";
        }

        protected override bool IsSpaceCenterUpgradeUnlocked()
        {
            return Vessel.patchedConicsUnlocked();
        }

        protected override GUILayoutOption[] WindowOptions()
        {
            return new[] { GUILayout.Width(260), GUILayout.Height(180) };
        }

        protected override void WindowGUI(int windowID)
        {
            MechJebModulePoweredDescentGuidance pdg =
                Core.GetComputerModule<MechJebModulePoweredDescentGuidance>();

            GUILayout.BeginVertical();

            if (pdg == null)
            {
                GUILayout.Label("Powered Descent Guidance unavailable.");
                GUILayout.EndVertical();
                _lastFocusedControl = GUI.GetNameOfFocusedControl();
                base.WindowGUI(windowID);
                return;
            }

            DrawTargetUI();

            GUILayout.Label("Autopilot:");

            GUILayout.Space(5);
            GUILayout.Label("PDG Settings");

            pdg.PulseThrottleMode =
                GUILayout.Toggle(pdg.PulseThrottleMode, "Pulse throttle mode");

            GUI.enabled = pdg.PulseThrottleMode;
            DrawDoubleField("pdg_pulse_period", "Pulse period", ref _pulsePeriodText, ref pdg.PulsePeriod, "s", 0.1, 10.0);
            GUI.enabled = true;

            DrawDoubleField("pdg_plan_throttle", "Plan throttle", ref _planThrottleText, ref pdg.PlanThrottle, "", 0.01, 1.0);

            DrawDoubleField("pdg_min_throttle", "Min throttle", ref _minThrottleText, ref pdg.MinThrottle, "", 0.0, 1.0);
            DrawDoubleField("pdg_clearance", "Clearance", ref _clearanceText, ref pdg.TargetClearance, "m", 0.0, 10000.0);
            DrawDoubleField("pdg_terminal_handover", "Terminal handover", ref _terminalHandoverText, ref pdg.TerminalHandoverDownrange, "m", 0.0, 100000.0);


            GUI.enabled = !pdg.UseApolloTerminal;
            DrawDoubleField("pdg_gt_cone", "GT Cone limit", ref _terminalGlideText, ref pdg.TerminalGlideConstraint, "deg", 0.0, 60.0);
            GUI.enabled = true;

            DrawTerminalModeButton(pdg);

            GUILayout.Space(5);

            if (pdg.Running)
            {
                if (GUILayout.Button("Abort PDG"))
                    pdg.StopGuidance();

                DrawLiveTelemetry(pdg);
            }
            else
            {
                GUILayout.BeginHorizontal();

                if (!Core.Target.PositionTargetExists || Vessel.LandedOrSplashed)
                    GUI.enabled = false;

                if (GUILayout.Button("Land at target"))
                    pdg.StartTargetedLanding(this);

                GUI.enabled = !Vessel.LandedOrSplashed;

                if (GUILayout.Button("Land somewhere"))
                    pdg.StartUntargetedLanding(this);

                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        private void DrawTargetUI()
        {
            if (Core.Target.PositionTargetExists)
            {
                double asl = Core.vessel.mainBody.TerrainAltitude(
                    Core.Target.targetLatitude,
                    Core.Target.targetLongitude
                );

                GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label1")); // Target coordinates:

                GUILayout.BeginHorizontal();
                Core.Target.targetLatitude.DrawEditGUI(EditableAngle.Direction.NS);

                if (GUILayout.Button("▲"))
                    MoveByMeter(ref Core.Target.targetLatitude, 10.0, asl);

                GUILayout.Label("10m");

                if (GUILayout.Button("▼"))
                    MoveByMeter(ref Core.Target.targetLatitude, -10.0, asl);

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                Core.Target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);

                if (GUILayout.Button("◄"))
                    MoveByMeter(ref Core.Target.targetLongitude, -10.0, asl);

                GUILayout.Label("10m");

                if (GUILayout.Button("►"))
                    MoveByMeter(ref Core.Target.targetLongitude, 10.0, asl);

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("ASL: " + asl.ToSI() + "m");
                GUILayout.Label(Core.Target.targetBody.GetExperimentBiomeSafe(
                    Core.Target.targetLatitude,
                    Core.Target.targetLongitude
                ));
                GUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button1"))) // Enter target coordinates
                    Core.Target.SetPositionTarget(MainBody, Core.Target.targetLatitude, Core.Target.targetLongitude);
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button2"))) // Pick target on map
                Core.Target.PickPositionTargetOnMap();
        }

        private void DrawTerminalModeButton(MechJebModulePoweredDescentGuidance pdg)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("Terminal mode", GUILayout.Width(120));

            string terminalModeLabel = pdg.UseApolloTerminal
                ? "Apollo"
                : "Gravity Turn";

            if (GUILayout.Button(terminalModeLabel, GUILayout.Width(95)))
                pdg.UseApolloTerminal = !pdg.UseApolloTerminal;

            GUILayout.EndHorizontal();
        }

        private void DrawLiveTelemetry(MechJebModulePoweredDescentGuidance pdg)
        {
            GUILayout.Space(5);

            GUILayout.Label("Status: " + pdg.PdgStatus);

            PDGGuidanceLoop step = pdg.CurrentGuidanceStep;
            if (step == null)
                return;

            GUILayout.Label("PDG Live");
            GUILayout.Label("TGO: " + step.TimeToGo.ToString("F1") + "s");
            GUILayout.Label("Throttle: " + (step.CurrentThrottle * 100.0f).ToString("F3") + "%");
            GUILayout.Label("Pred X: " + step.PredDownrange.ToString("F0") + "m");
            GUILayout.Label("Err X: " + step.DownrangeError.ToString("F3") + "m");
            GUILayout.Label("TWR: " + step.CurrentTWR.ToString("F2") + "/" + step.AvailableTWR.ToString("F2"));
        }

        private void MoveByMeter(ref EditableAngle angle, double distance, double alt)
        {
            double angularDelta = distance * UtilMath.Rad2Deg / (alt + MainBody.Radius);
            angle += angularDelta;
        }

       private void DrawDoubleField(
            string controlName,
            string label,
            ref string text,
            ref double value,
            string units,
            double min,
            double max)
        {
            if (string.IsNullOrEmpty(text))
                text = value.ToString("F3");

            GUILayout.BeginHorizontal();

            GUILayout.Label(label, GUILayout.Width(120));

            GUI.SetNextControlName(controlName);
            text = GUILayout.TextField(text, GUILayout.Width(55));

            if (!string.IsNullOrEmpty(units))
                GUILayout.Label(units, GUILayout.Width(25));

            GUILayout.EndHorizontal();

            bool enterPressed =
                Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) &&
                GUI.GetNameOfFocusedControl() == controlName;

            bool focusLost =
                _lastFocusedControl == controlName &&
                GUI.GetNameOfFocusedControl() != controlName;

            if (enterPressed || focusLost)
            {
                CommitDoubleField(ref text, ref value, min, max);

                if (enterPressed)
                {
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }
        }

        private string _lastFocusedControl = "";

        private void CommitDoubleField(ref string text, ref double value, double min, double max)
        {
            double parsed;
            if (double.TryParse(text, out parsed))
            {
                value = Math.Max(min, Math.Min(max, parsed));
                text = value.ToString("F3");
            }
            else
            {
                text = value.ToString("F3");
            }
        }
    }
}