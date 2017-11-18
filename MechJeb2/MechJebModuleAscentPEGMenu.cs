using System;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAscentPEGMenu : MechJebModuleAscentMenuBase
    {
        public MechJebModuleAscentPEGMenu(MechJebCore core)
            : base(core)
        {
            hidden = true;
        }

        public MechJebModuleAscentPEG path { get { return autopilot.ascentPath as MechJebModuleAscentPEG; } }
        private MechJebModulePEGController peg { get { return core.GetComputerModule<MechJebModulePEGController>(); } }
        public MechJebModuleAscentAutopilot autopilot;

        public override void OnStart(PartModule.StartState state)
        {
            autopilot = core.GetComputerModule<MechJebModuleAscentAutopilot>();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(100) };
        }

        protected override void WindowGUI(int windowID)
        {
            if (path == null)
            {
                GUILayout.Label("Path is null!!!1!!1!1!1111!11eleven");
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            GuiUtils.SimpleTextBox("Target Periapsis:", autopilot.desiredOrbitAltitude, "km");
            GuiUtils.SimpleTextBox("Target Apoapsis:", path.desiredApoapsis, "km");
            if ( path.desiredApoapsis >= 0 && path.desiredApoapsis < autopilot.desiredOrbitAltitude )
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.textColor = Color.yellow;
                GUILayout.Label("Apoapsis < Periapsis: circularizing orbit at periapsis", s);
            }
            GuiUtils.SimpleTextBox("leading angle to plane: ", autopilot.launchLANDifference, "°");

            GuiUtils.SimpleTextBox("Booster Pitch start:", path.pitchStartTime, "s");
            GuiUtils.SimpleTextBox("Booster Pitch rate:", path.pitchRate, "°/s");
            GUILayout.BeginHorizontal();
            path.pitchEndToggle = GUILayout.Toggle(path.pitchEndToggle, "Booster Pitch end:");
            if (path.pitchEndToggle)
                GuiUtils.SimpleTextBox("", path.pitchEndTime, "s");
            GUILayout.EndHorizontal();
            if (path.pitchEndToggle)
                GUILayout.Label(String.Format("ending pitch: {0:F1}°", 90.0 - (path.pitchEndTime - path.pitchStartTime)*path.pitchRate));
            GUILayout.BeginHorizontal();
            path.pegAfterStageToggle = GUILayout.Toggle(path.pegAfterStageToggle, "Start PEG after KSP Stage #");
            if (path.pegAfterStageToggle)
                GuiUtils.SimpleTextBox("", path.pegAfterStage);
            GUILayout.EndHorizontal();
            GuiUtils.SimpleTextBox("Terminal Guidance Period:", peg.terminalGuidanceTime, "s");
            GuiUtils.SimpleTextBox("PEG Update Interval:", peg.pegInterval, "s");
            GUILayout.Label("Stage Stats");
            if (GUILayout.Button("Reset PEG"))
                peg.Reset();

            for(int i = peg.stages.Count - 1; i >= 0; i--) {
                GUILayout.Label(String.Format("{0:D}: {1:D} {2:F1} {3:F1}", i, peg.stages[i].kspStage, peg.stages[i].dt, peg.stages[i].Li));
            }
            GUILayout.Label(String.Format("Dv: {0:F1}", peg.Dv.magnitude));
            GUILayout.Label(String.Format("tgo: {0:F1}", peg.tgo));
            GUILayout.Label(String.Format("heading: {0:F1}", peg.heading));
            GUILayout.Label(String.Format("pitch: {0:F1}", peg.pitch));
            GUILayout.Label(String.Format("primer mag: {0:F2}", peg.primerMag));
            GUILayout.Label(String.Format("phi: {0:F2}", peg.phi * UtilMath.Rad2Deg));
            GUILayout.Label(String.Format("K: {0:F1}", peg.K ));
            GUILayout.Label(String.Format("Fv: {0:F4}", peg.Fv ));
            GUILayout.Label(String.Format("iy inc: {0:F4}", Math.Acos(-Vector3d.Dot(-Planetarium.up, peg.iy)) * UtilMath.Rad2Deg));
            GUILayout.Label(String.Format("orth. test: {0:F5}", Vector3d.Dot(peg.lambda, peg.lambdaDot)));

            GuiUtils.SimpleTextBox("Emergency pitch adj.:", path.pitchBias, "°");
            GUILayout.Label("Debug Settings");
            GuiUtils.SimpleTextBox("Vmiss gain (0.5 recommended):", peg.vmissGain);


            if (autopilot.enabled)
            {
                GUILayout.Label("Autopilot status: " + autopilot.status);
            }


            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }

        public override string GetName()
        {
            return "Space Shuttle PEG Pitch Program";
        }
    }
}
