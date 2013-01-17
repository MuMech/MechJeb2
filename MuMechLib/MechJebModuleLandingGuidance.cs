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

        protected override void FlightWindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("Pick target")) core.target.PickPositionTargetOnMap();

            predictor.enabled = GUILayout.Toggle(predictor.enabled, "Predictor enabled");

            if (!autopilot.enabled)
            {
                if (GUILayout.Button("Land at target")) autopilot.LandAtPositionTarget();
                if (GUILayout.Button("Land somewhere")) autopilot.LandUntargeted();
            }
            else
            {
                if (GUILayout.Button("Stop landing")) autopilot.StopLanding();
            }


            GUILayout.Label("Autopilot status: " + autopilot.status);

            ReentrySimulation.Result prediction = predictor.GetResult();
            if (prediction != null && prediction.outcome == ReentrySimulation.Outcome.LANDED)
            {
                double error = Vector3d.Distance(mainBody.GetRelSurfacePosition(prediction.endPosition.latitude, prediction.endPosition.longitude, 0),
                                                 mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0));
                GUILayout.Label("Landing position error = " + error.ToString("F0") + "m");
            }

            predictor.makeAerobrakeNodes = GUILayout.Toggle(predictor.makeAerobrakeNodes, "Show aerobrake nodes");

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        public override string GetName()
        {
            return "Landing Guidance";
        }

        public MechJebModuleLandingGuidance(MechJebCore core) : base(core) { }
    }
}
