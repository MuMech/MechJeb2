using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleLandingGuidance : DisplayModule
    {
        MechJebModuleLandingPredictions predictor;
        MechJebModuleLandingAutopilot autopilot;

        public override void OnStart(PartModule.StartState state)
        {
            predictor = core.GetComputerModule<MechJebModuleLandingPredictions>();
            autopilot = core.GetComputerModule<MechJebModuleLandingAutopilot>();
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Target coordinates:");

            core.target.targetLatitude.DrawEditGUI(EditableAngle.Direction.NS);
            core.target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);

            if (GUILayout.Button("Pick target on map")) core.target.PickPositionTargetOnMap();

            predictor.enabled = GUILayout.Toggle(predictor.enabled, "Show landing predictions");

            if (predictor.enabled)
            {
                predictor.makeAerobrakeNodes = GUILayout.Toggle(predictor.makeAerobrakeNodes, "Show aerobrake nodes");
                DrawGUIPrediction();
            }

            GUILayout.Label("Autopilot:");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Land at target")) autopilot.LandAtPositionTarget();
            if (GUILayout.Button("Land somewhere")) autopilot.LandUntargeted();
            GUILayout.EndHorizontal();

            GuiUtils.SimpleTextBox("Touchdown speed:", autopilot.touchdownSpeed, "m/s");

            if (autopilot.enabled) GUILayout.Label("Status: " + autopilot.status);

            GUILayout.EndVertical();

            GUI.DragWindow();
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
