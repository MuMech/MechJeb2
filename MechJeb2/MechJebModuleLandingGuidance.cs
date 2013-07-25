using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleLandingGuidance : DisplayModule
    {
        public MechJebModuleLandingPredictions predictor;
        public MechJebModuleLandingAutopilot autopilot;

        public override void OnStart(PartModule.StartState state)
        {
            predictor = core.GetComputerModule<MechJebModuleLandingPredictions>();
            autopilot = core.GetComputerModule<MechJebModuleLandingAutopilot>();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(150), GUILayout.Height(150) };
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (core.target.PositionTargetExists)
            {
                GUILayout.Label("Target coordinates:");

                core.target.targetLatitude.DrawEditGUI(EditableAngle.Direction.NS);
                core.target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);
            }
            else
            {
                if (GUILayout.Button("Enter target coordinates"))
                {
                    core.target.SetPositionTarget(mainBody, core.target.targetLatitude, core.target.targetLongitude);
                }
            }

            if (GUILayout.Button("Pick target on map")) core.target.PickPositionTargetOnMap();

            if (mainBody.bodyName.ToLower().Contains("kerbin"))
            {
                if (GUILayout.Button("Target KSC"))
                {
                    core.target.SetPositionTarget(mainBody, -0.10267, -74.57538);
                }
            }

            if (autopilot != null) core.node.autowarp = GUILayout.Toggle(core.node.autowarp, "Auto-warp");

            bool active = GUILayout.Toggle(predictor.enabled, "Show landing predictions");
            if (predictor.enabled != active)
            {
                if (active)
                {
                    predictor.users.Add(this);
                }
                else
                {
                    predictor.users.Remove(this);
                }
            }

            if (predictor.enabled)
            {
                predictor.makeAerobrakeNodes = GUILayout.Toggle(predictor.makeAerobrakeNodes, "Show aerobrake nodes");
                DrawGUIPrediction();
            }

            if (autopilot != null)
            {
                GUILayout.Label("Autopilot:");

                if (autopilot.enabled)
                {
                    if (GUILayout.Button("Abort autoland")) autopilot.StopLanding();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (!core.target.PositionTargetExists) GUI.enabled = false;
                    if (GUILayout.Button("Land at target")) autopilot.LandAtPositionTarget(this);
                    GUI.enabled = true;
                    if (GUILayout.Button("Land somewhere")) autopilot.LandUntargeted(this);
                    GUILayout.EndHorizontal();
                }

                GuiUtils.SimpleTextBox("Touchdown speed:", autopilot.touchdownSpeed, "m/s", 35);

                if (autopilot.enabled) GUILayout.Label("Status: " + autopilot.status);
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        void DrawGUIPrediction()
        {
            ReentrySimulation.Result result = predictor.GetResult();
            if (result != null)
            {
                switch (result.outcome)
                {
                    case ReentrySimulation.Outcome.LANDED:
                        GUILayout.Label("Predicted landing site:");
                        GUILayout.Label(Coordinates.ToStringDMS(result.endPosition.latitude, result.endPosition.longitude));
                        double error = Vector3d.Distance(mainBody.GetRelSurfacePosition(result.endPosition.latitude, result.endPosition.longitude, 0),
                                                         mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0));
                        GUILayout.Label("Difference from target = " + MuUtils.ToSI(error, 0) + "m");
                        if (result.maxDragGees > 0) GUILayout.Label("Predicted max drag gees: " + result.maxDragGees.ToString("F1"));
                        break;

                    case ReentrySimulation.Outcome.AEROBRAKED:
                        GUILayout.Label("Predicted orbit after aerobraking:");
                        Orbit o = result.EndOrbit();
                        if (o.eccentricity > 1) GUILayout.Label("Hyperbolic, eccentricity = " + o.eccentricity.ToString("F2"));
                        else GUILayout.Label(MuUtils.ToSI(o.PeA, 3) + "m x " + MuUtils.ToSI(o.ApA, 3) + "m");
                        break;

                    case ReentrySimulation.Outcome.NO_REENTRY:
                        GUILayout.Label("Orbit does not reenter:");
                        GUILayout.Label(MuUtils.ToSI(orbit.PeA, 3) + "m Pe > " + MuUtils.ToSI(mainBody.RealMaxAtmosphereAltitude(), 3) + "m atmosphere height");
                        break;

                    case ReentrySimulation.Outcome.TIMED_OUT:
                        GUILayout.Label("Reentry simulation timed out.");
                        break;
                }
            }
        }

        public override string GetName()
        {
            return "Landing Guidance";
        }

        public MechJebModuleLandingGuidance(MechJebCore core) : base(core) { }
    }
}
