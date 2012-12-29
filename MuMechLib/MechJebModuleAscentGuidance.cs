using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //When enabled, the ascent guidance module makes the purple navball target point
    //along the ascent path. The ascent path can be set via SetPath. The ascent guidance
    //module disables itself if the player selects a different target.
    class MechJebModuleAscentGuidance : DisplayModule
    {
        public MechJebModuleAscentGuidance(MechJebCore core) : base(core) { }


        public IAscentPath ascentPath; //other modules can edit this to control what path the guidance uses
        DirectionTarget target = new DirectionTarget("Ascent Path Guidance");

        public override void OnModuleEnabled()
        {
            ascentPath = new DefaultAscentPath();
            FlightGlobals.fetch.SetVesselTarget(target);
        }

        public override void OnModuleDisabled()
        {
            if (FlightGlobals.fetch.VesselTarget == target) FlightGlobals.fetch.SetVesselTarget(null);
        }

        public override void OnFixedUpdate()
        {
            if (!enabled) return;

            if (FlightGlobals.fetch.VesselTarget != target)
            {
                enabled = false;
                return;
            }

            if (ascentPath == null) return;

            double angle = Math.PI / 180 * ascentPath.FlightPathAngle(vesselState.altitudeASL);
            Vector3d dir = Math.Cos(angle) * vesselState.east + Math.Sin(angle) * vesselState.up;
            target.Update(dir);
        }

        protected override void FlightWindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("The purple circle on the navball points along the ascent path.");

            if (GUILayout.Button("Turn off guidance")) enabled = false;

            GUILayout.EndVertical();

            base.FlightWindowGUI(windowID);
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[]{ GUILayout.Width(200), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return "Ascent Guidance";
        }
    }
}
