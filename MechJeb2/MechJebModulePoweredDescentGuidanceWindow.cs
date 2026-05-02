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

        private string _lastFocusedControl = "";

        private static string L(string key)
        {
            return Localizer.Format("#MechJeb_PDG_" + key);
        }

        public MechJebModulePoweredDescentGuidanceWindow(MechJebCore core)
            : base(core)
        {
        }

        public override string GetName()
        {
            return L("title");
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
                GUILayout.Label(L("unavailable"));
                GUILayout.EndVertical();
                _lastFocusedControl = GUI.GetNameOfFocusedControl();
                base.WindowGUI(windowID);
                return;
            }

            DrawTargetUI();

            GUILayout.Label(L("autopilot"));

            GUILayout.Space(5);
            GUILayout.Label(L("settings"));

            pdg.PulseThrottleMode =
                GUILayout.Toggle(pdg.PulseThrottleMode, L("pulseThrottle"));

            GUI.enabled = pdg.PulseThrottleMode;
            DrawDoubleField("pdg_pulse_period", L("pulsePeriod"), ref _pulsePeriodText, ref pdg.PulsePeriod, "s", 0.1, 10.0);
            GUI.enabled = true;

            DrawDoubleField("pdg_plan_throttle", L("planThrottle"), ref _planThrottleText, ref pdg.PlanThrottle, "", 0.01, 1.0);

            DrawDoubleField("pdg_min_throttle", L("minThrottle"), ref _minThrottleText, ref pdg.MinThrottle, "", 0.0, 1.0);
            DrawDoubleField("pdg_clearance", L("clearance"), ref _clearanceText, ref pdg.TargetClearance, "m", 0.0, 10000.0);
            DrawDoubleField("pdg_terminal_handover", L("terminalHandover"), ref _terminalHandoverText, ref pdg.TerminalHandoverDownrange, "m", 0.0, 100000.0);


            GUI.enabled = !pdg.UseApolloTerminal;
            DrawDoubleField("pdg_gt_cone", L("gtConeLimit"), ref _terminalGlideText, ref pdg.TerminalGlideConstraint, "deg", 0.0, 60.0);
            GUI.enabled = true;

            DrawTerminalModeButton(pdg);

            GUILayout.Space(5);

            if (pdg.Running)
            {
                if (GUILayout.Button(L("abort")))
                    pdg.StopGuidance();

                DrawLiveTelemetry(pdg);
            }
            else
            {
                if (!pdg.CanRunOnCurrentBody)
                {
                    GUILayout.Label(pdg.StartBlockedReason);
                }

                GUILayout.BeginHorizontal();

                bool canRun = pdg.CanRunOnCurrentBody && !Vessel.LandedOrSplashed;

                GUI.enabled = canRun && Core.Target.PositionTargetExists;

                if (GUILayout.Button(L("landAtTarget")))
                    pdg.StartTargetedLanding(this);

                GUI.enabled = canRun;

                if (GUILayout.Button(L("landSomewhere")))
                    pdg.StartUntargetedLanding(this);

                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            _lastFocusedControl = GUI.GetNameOfFocusedControl();
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

            GUILayout.Label(L("terminalMode"), GUILayout.Width(120));

            string terminalModeLabel = pdg.UseApolloTerminal
                ? L("apollo")
                : L("gravityTurn");

            if (GUILayout.Button(terminalModeLabel, GUILayout.Width(95)))
                pdg.UseApolloTerminal = !pdg.UseApolloTerminal;

            GUILayout.EndHorizontal();
        }

        private void DrawLiveTelemetry(MechJebModulePoweredDescentGuidance pdg)
        {
            GUILayout.Space(5);

            GUILayout.Label(L("status") + " " + pdg.PdgStatus);

            PDGGuidanceLoop step = pdg.CurrentGuidanceStep;
            if (step == null)
                return;

            GUILayout.Label(L("live"));
            GUILayout.Label(L("tgo") + ": " + step.TimeToGo.ToString("F1") + "s");
            GUILayout.Label(L("throttle") + ": " + (step.CurrentThrottle * 100.0f).ToString("F3") + "%");
            GUILayout.Label(L("predX") + ": " + step.PredDownrange.ToString("F0") + "m");
            GUILayout.Label(L("errX") + ": " + step.DownrangeError.ToString("F3") + "m");
            GUILayout.Label(L("twr") + ": " + step.CurrentTWR.ToString("F2") + "/" + step.AvailableTWR.ToString("F2"));
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