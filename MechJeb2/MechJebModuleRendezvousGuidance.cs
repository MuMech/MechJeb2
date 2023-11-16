using System.Linq;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleRendezvousGuidance : DisplayModule
    {
        public MechJebModuleRendezvousGuidance(MechJebCore core) : base(core) { }

        private readonly EditableDoubleMult phasingOrbitAltitude = new EditableDoubleMult(200000, 1000);

        protected override void WindowGUI(int windowID)
        {
            if (!Core.Target.NormalTargetExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_RZplan_label1")); //"Select a target to rendezvous with."
                base.WindowGUI(windowID);
                return;
            }

            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_RZplan_label2")); //"Rendezvous target must be in the same sphere of influence."
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            //Information readouts:

            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RZplan_label3"), Core.Target.Name); //"Rendezvous target"

            const double leadTime = 30;
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RZplan_label4"),
                Core.Target.TargetOrbit.PeA.ToSI(3) + "m x " + Core.Target.TargetOrbit.ApA.ToSI(3) + "m");                          //"Target orbit"
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RZplan_label5"), Orbit.PeA.ToSI(3) + "m x " + Orbit.ApA.ToSI(3) + "m"); //"Current orbit"
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RZplan_label6"),
                Orbit.RelativeInclination(Core.Target.TargetOrbit).ToString("F2") + "º"); //"Relative inclination"

            double closestApproachTime = Orbit.NextClosestApproachTime(Core.Target.TargetOrbit, VesselState.time);
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RZplan_label7"),
                GuiUtils.TimeToDHMS(closestApproachTime - VesselState.time)); //"Time until closest approach"
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RZplan_label8"),
                Orbit.Separation(Core.Target.TargetOrbit, closestApproachTime).ToSI() + "m"); //"Separation at closest approach"


            //Maneuver planning buttons:

            if (GUILayout.Button(Localizer.Format("#MechJeb_RZplan_button1"))) //"Align Planes"
            {
                double UT;
                Vector3d dV;
                if (Orbit.AscendingNodeExists(Core.Target.TargetOrbit))
                {
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(Orbit, Core.Target.TargetOrbit, VesselState.time, out UT);
                }
                else
                {
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(Orbit, Core.Target.TargetOrbit, VesselState.time, out UT);
                }

                Vessel.RemoveAllManeuverNodes();
                Vessel.PlaceManeuverNode(Orbit, dV, UT);
            }


            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("#MechJeb_RZplan_button2"))) //"Establish new orbit at"
            {
                double phasingOrbitRadius = phasingOrbitAltitude + MainBody.Radius;

                Vessel.RemoveAllManeuverNodes();
                if (Orbit.ApR < phasingOrbitRadius)
                {
                    double UT1 = VesselState.time + leadTime;
                    Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(Orbit, UT1, phasingOrbitRadius);
                    Vessel.PlaceManeuverNode(Orbit, dV1, UT1);
                    Orbit transferOrbit = Vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                    double UT2 = transferOrbit.NextApoapsisTime(UT1);
                    Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                    Vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
                }
                else if (Orbit.PeR > phasingOrbitRadius)
                {
                    double UT1 = VesselState.time + leadTime;
                    Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(Orbit, UT1, phasingOrbitRadius);
                    Vessel.PlaceManeuverNode(Orbit, dV1, UT1);
                    Orbit transferOrbit = Vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                    double UT2 = transferOrbit.NextPeriapsisTime(UT1);
                    Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                    Vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
                }
                else
                {
                    double UT = Orbit.NextTimeOfRadius(VesselState.time, phasingOrbitRadius);
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(Orbit, UT);
                    Vessel.PlaceManeuverNode(Orbit, dV, UT);
                }
            }

            phasingOrbitAltitude.Text = GUILayout.TextField(phasingOrbitAltitude.Text, GUILayout.Width(70));
            GUILayout.Label("km", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            if (GUILayout.Button(Localizer.Format("#MechJeb_RZplan_button3"))) //"Intercept with Hohmann transfer"
            {
                (Vector3d dV, double UT, _, _) =
                    OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(Orbit, Core.Target.TargetOrbit, VesselState.time, coplanar: false);
                Vessel.RemoveAllManeuverNodes();
                Vessel.PlaceManeuverNode(Orbit, dV, UT);
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_RZplan_button4"))) //"Match velocities at closest approach"
            {
                double UT = closestApproachTime;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, UT, Core.Target.TargetOrbit);
                Vessel.RemoveAllManeuverNodes();
                Vessel.PlaceManeuverNode(Orbit, dV, UT);
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_RZplan_button5"))) //"Get closer"
            {
                double UT = VesselState.time;
                (Vector3d dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(Orbit, UT, Core.Target.TargetOrbit, 100, 10);
                Vessel.RemoveAllManeuverNodes();
                Vessel.PlaceManeuverNode(Orbit, dV, UT);
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_RZplan_button9"))) //Remove ALL nodes
            {
                Vessel.RemoveAllManeuverNodes();
            }

            if (Core.Node != null)
            {
                if (Vessel.patchedConicSolver.maneuverNodes.Any() && !Core.Node.Enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_RZplan_button6"))) //"Execute next node"
                    {
                        Core.Node.ExecuteOneNode(this);
                    }

                    if (Vessel.patchedConicSolver.maneuverNodes.Count > 1)
                    {
                        if (GUILayout.Button(Localizer.Format("#MechJeb_RZplan_button7"))) //"Execute all nodes"
                        {
                            Core.Node.ExecuteAllNodes(this);
                        }
                    }
                }
                else if (Core.Node.Enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_RZplan_button8"))) //"Abort node execution"
                    {
                        Core.Node.Abort();
                    }
                }

                GUILayout.BeginHorizontal();
                Core.Node.Autowarp =
                    GUILayout.Toggle(Core.Node.Autowarp, Localizer.Format("#MechJeb_RZplan_checkbox"), GUILayout.ExpandWidth(true)); //"Auto-warp"
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(300), GUILayout.Height(150) };

        public override string GetName() => Localizer.Format("#MechJeb_RZplan_title"); //"Rendezvous Planner"

        public override string IconName() => "Rendezvous Planner";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();
    }
}
