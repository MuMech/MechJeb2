using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleSpaceplaneGuidance : DisplayModule
    {
        MechJebModuleSpaceplaneAutopilot autopilot;

        protected bool _showLandingTarget = false;
        public bool showLandingTarget
        {
            get { return _showLandingTarget; }
            set
            {
                if (value && !_showLandingTarget) core.target.SetDirectionTarget("ILS Guidance");
                if (!value && (core.target.Target is DirectionTarget && core.target.Name == "ILS Guidance")) core.target.Unset();
                _showLandingTarget = value;
            }
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Landing", s);

            Runway[] runways = MechJebModuleSpaceplaneAutopilot.runways;
            int runwayIndex = Array.IndexOf(runways, autopilot.runway);
            runwayIndex = GuiUtils.ArrowSelector(runwayIndex, runways.Length, autopilot.runway.name);
            autopilot.runway = runways[runwayIndex];

            GUILayout.Label("Distance to runway: " + MuUtils.ToSI(Vector3d.Distance(vesselState.CoM, autopilot.runway.Start(vesselState.CoM)), 0) + "m");

            showLandingTarget = GUILayout.Toggle(showLandingTarget, "Show landing navball guidance");

            if (GUILayout.Button("Autoland")) autopilot.Autoland(this);
            if (autopilot.enabled && autopilot.mode == MechJebModuleSpaceplaneAutopilot.Mode.AUTOLAND
                && GUILayout.Button("Abort")) autopilot.Abort();

            GuiUtils.SimpleTextBox("Autoland glideslope:", autopilot.glideslope, "º");

            GUILayout.Label("Hold", s);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Initiate hold:")) autopilot.HoldHeadingAndAltitude(this);
            GUILayout.Label("Heading:");
            autopilot.targetHeading.text = GUILayout.TextField(autopilot.targetHeading.text, GUILayout.Width(40));
            GUILayout.Label("º Altitude:");
            autopilot.targetAltitude.text = GUILayout.TextField(autopilot.targetAltitude.text, GUILayout.Width(40));
            GUILayout.Label("m");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Takeoff Pitch:");
            autopilot.takeoffPitch = float.Parse(GUILayout.TextField(autopilot.takeoffPitch.ToString(), GUILayout.Width(40)));
            GUILayout.EndHorizontal();

            //temp
            //GUILayout.Label(autopilot.noseRoll.ToString());
            //autopilot.rollLowPass.TimeConstant = double.Parse(GUILayout.TextField(autopilot.rollLowPass.TimeConstant.ToString(), GUILayout.Width(40)));
            //autopilot.pitchLowPass.TimeConstant = double.Parse(GUILayout.TextField(autopilot.pitchLowPass.TimeConstant.ToString(), GUILayout.Width(40)));
            /*
            //GUILayout.Label(autopilot.desiredAoA.ToString());
            //GUILayout.Label(autopilot.vesselState.vesselPitch.ToString());
            //GUILayout.Label(autopilot.pitchCorrection.ToString());
            GUILayout.BeginHorizontal();
            GUILayout.Label("pitch PID:");
            autopilot.pitchPID.Kp = double.Parse(GUILayout.TextField(autopilot.pitchPID.Kp.ToString(), GUILayout.Width(40)));
            autopilot.pitchPID.Ki = double.Parse(GUILayout.TextField(autopilot.pitchPID.Ki.ToString(), GUILayout.Width(40)));
            autopilot.pitchPID.Kd = double.Parse(GUILayout.TextField(autopilot.pitchPID.Kd.ToString(), GUILayout.Width(40)));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("pitch correction PID:");
            autopilot.pitchCorrectionPID.Kp = double.Parse(GUILayout.TextField(autopilot.pitchCorrectionPID.Kp.ToString(), GUILayout.Width(40)));
            autopilot.pitchCorrectionPID.Ki = double.Parse(GUILayout.TextField(autopilot.pitchCorrectionPID.Ki.ToString(), GUILayout.Width(40)));
            autopilot.pitchCorrectionPID.Kd = double.Parse(GUILayout.TextField(autopilot.pitchCorrectionPID.Kd.ToString(), GUILayout.Width(40)));
            GUILayout.EndHorizontal();

            //GUILayout.Label(autopilot.desiredRoll.ToString());
            //GUILayout.Label(autopilot.vesselState.vesselRoll.ToString());
            //GUILayout.Label(autopilot.rollCorrection.ToString());
            GUILayout.BeginHorizontal();
            GUILayout.Label("roll PID:");
            autopilot.rollPID.Kp = double.Parse(GUILayout.TextField(autopilot.rollPID.Kp.ToString(), GUILayout.Width(40)));
            autopilot.rollPID.Ki = double.Parse(GUILayout.TextField(autopilot.rollPID.Ki.ToString(), GUILayout.Width(40)));
            autopilot.rollPID.Kd = double.Parse(GUILayout.TextField(autopilot.rollPID.Kd.ToString(), GUILayout.Width(40)));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("roll correction PID:");
            autopilot.rollCorrectionPID.Kp = double.Parse(GUILayout.TextField(autopilot.rollCorrectionPID.Kp.ToString(), GUILayout.Width(40)));
            autopilot.rollCorrectionPID.Ki = double.Parse(GUILayout.TextField(autopilot.rollCorrectionPID.Ki.ToString(), GUILayout.Width(40)));
            autopilot.rollCorrectionPID.Kd = double.Parse(GUILayout.TextField(autopilot.rollCorrectionPID.Kd.ToString(), GUILayout.Width(40)));
            GUILayout.EndHorizontal();
            */
            //GUILayout.Label(autopilot.pitchPID.intAccum.ToString());
            //GUILayout.Label(autopilot.pitchCorrectionPID.intAccum.ToString());
            //GUILayout.Label(autopilot.rollPID.intAccum.ToString());
            //GUILayout.Label(autopilot.rollCorrectionPID.intAccum.ToString());

            if (autopilot.enabled && autopilot.mode == MechJebModuleSpaceplaneAutopilot.Mode.HOLD
                && GUILayout.Button("Abort")) autopilot.Abort();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(350), GUILayout.Height(200) };
        }

        public override void OnFixedUpdate()
        {
            if (showLandingTarget && autopilot != null)
            {
                if (!(core.target.Target is DirectionTarget && core.target.Name == "ILS Guidance")) showLandingTarget = false;
                else
                {
                    core.target.UpdateDirectionTarget(autopilot.ILSAimDirection());
                }
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            autopilot = core.GetComputerModule<MechJebModuleSpaceplaneAutopilot>();
        }

        public MechJebModuleSpaceplaneGuidance(MechJebCore core) : base(core) { }

        public override string GetName()
        {
            return "Spaceplane Guidance";
        }
    }
}
