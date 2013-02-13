using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //This class just exists to collect miscellaneous info item functions in one place. 
    //If any of these functions are useful in another module, they should be moved there.
    public class MechJebModuleInfoItems : ComputerModule
    {
        public MechJebModuleInfoItems(MechJebCore core) : base(core) { }

        [ValueInfoItem(name = "Node burn time")]
        public string NextManeuverNodeBurnTime()
        {
            if (!vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
            double time = node.GetBurnVector(node.patch).magnitude / vesselState.maxThrustAccel;
            return GuiUtils.TimeToDHMS(time);
        }

        [ValueInfoItem(name = "Surface TWR")]
        public string SurfaceTWR()
        {
            return (vesselState.thrustAvailable / (vesselState.mass * mainBody.GeeASL * 9.81)).ToString("F2");
        }

        [ValueInfoItem(name = "Atmospheric pressure", units = "atm")]
        public double AtmosphericPressure()
        {
            return FlightGlobals.getStaticPressure(vesselState.CoM);
        }

        [ValueInfoItem(name = "Coordinates")]
        public string GetCoordinateString()
        {
            return Coordinates.ToStringDMS(vesselState.latitude, vesselState.longitude);
        }

        [ValueInfoItem(name = "Orbit shape")]
        public string OrbitSummary()
        {
            if (orbit.eccentricity > 1) return "hyperbolic, Pe = " + MuUtils.ToSI(orbit.PeA, 2) + "m";
            else return MuUtils.ToSI(orbit.PeA, 2) + "m x " + MuUtils.ToSI(orbit.ApA, 2) + "m";
        }

        [ValueInfoItem(name = "Orbit shape 2")]
        public string OrbitSummaryWithInclination()
        {
            return OrbitSummary() + ", inc. " + orbit.inclination.ToString("F1") + "º";
        }


        //Todo: consider turning this into a binary search
        [ValueInfoItem(name = "Time to impact")]
        public string TimeToImpact()
        {
            if (orbit.PeA > 0) return "N/A";

            double impactTime = vesselState.time;

            for (int iter = 0; iter < 10; iter++)
            {
                Vector3d impactPosition = orbit.SwappedAbsolutePositionAtUT(impactTime);
                double terrainRadius = mainBody.Radius + mainBody.TerrainAltitude(impactPosition);
                impactTime = orbit.NextTimeOfRadius(vesselState.time, terrainRadius);
            }

            return GuiUtils.TimeToDHMS(impactTime - vesselState.time);
        }

        [ValueInfoItem(name = "Suicide burn countdown")]
        public string SuicideBurnCountdown()
        {
            if (orbit.PeA > 0) return "N/A";
            
            double angleFromHorizontal = 90 - Vector3d.Angle(-vesselState.velocityVesselSurface, vesselState.up);
            angleFromHorizontal = MuUtils.Clamp(angleFromHorizontal, 0, 90);
            double sine = Math.Sin(angleFromHorizontal * Math.PI / 180);
            double g = vesselState.localg;
            double T = vesselState.maxThrustAccel;

            double effectiveDecel = 0.5 * (-2 * g * sine + Math.Sqrt((2 * g * sine) * (2 * g * sine) + 4 * (T * T - g * g)));
            double decelTime = vesselState.speedSurface / effectiveDecel;

            Vector3d estimatedLandingSite = vesselState.CoM + 0.5 * decelTime * vesselState.velocityVesselSurface;
            double terrainRadius = mainBody.Radius + mainBody.TerrainAltitude(estimatedLandingSite);
            double impactTime = orbit.NextTimeOfRadius(vesselState.time, terrainRadius);

            return GuiUtils.TimeToDHMS(impactTime - decelTime / 2 - vesselState.time);
        }

        [ValueInfoItem(name = "Current acceleration", units = "m/s²")]
        public double CurrentAcceleration()
        {
            return vesselState.ThrustAccel(FlightInputHandler.state.mainThrottle);
        }

        [ValueInfoItem(name="Time to SoI switch")]
        public string TimeToSOITransition()
        {
            if (orbit.patchEndTransition == Orbit.PatchTransitionType.FINAL) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.EndUT - vesselState.time);
        }

        [ValueInfoItem(name = "Surface gravity", units="m/s²")]
        public double SurfaceGravity()
        {
            return mainBody.GeeASL * 9.81;
        }

        [ValueInfoItem(name = "Part count")]
        public int PartCount()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.Count;
            else return vessel.parts.Count;
        }

        [ValueInfoItem(name = "Strut count")]
        public int StrutCount()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.Count(p => p is StrutConnector);
            else return vessel.parts.Count(p => p is StrutConnector);
        }

        [ValueInfoItem(name = "Vessel cost", units = "k$")]
        public int VesselCost()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.Sum(p => p.partInfo.cost);
            else return vessel.parts.Sum(p => p.partInfo.cost);
        }

        [ValueInfoItem(name = "Crew count")]
        public int CrewCount()
        {
            return vessel.GetCrewCount();
        }

        [ValueInfoItem(name = "Crew capacity")]
        public int CrewCapacity()
        {
            return vessel.GetCrewCapacity();
        }

        [ValueInfoItem(name = "Distance to target")]
        public string TargetDistance()
        {
            if (core.target.Target == null) return "N/A";
            return MuUtils.ToSI(core.target.Distance, -1) + "m";
        }

        [ValueInfoItem(name = "Relative velocity")]
        public string TargetRelativeVelocity()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.RelativeVelocity.magnitude) + "m/s";
        }

        [ValueInfoItem(name = "Time to closest approach")]
        public string TargetTimeToClosestApproach()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (!core.target.Orbit.referenceBody == orbit.referenceBody) return "N/A";
            return GuiUtils.TimeToDHMS(orbit.NextClosestApproachTime(core.target.Orbit, vesselState.time) - vesselState.time);
        }

        [ValueInfoItem(name = "Closest approach distance")]
        public string TargetClosestApproachDistance()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (!core.target.Orbit.referenceBody == orbit.referenceBody) return "N/A";
            return MuUtils.ToSI(orbit.NextClosestApproachDistance(core.target.Orbit, vesselState.time), -1) + "m";
        }

        [ValueInfoItem(name = "Atmospheric drag", units = "m/s²")]
        public double AtmosphericDrag()
        {
            return mainBody.DragAccel(vesselState.CoM, vesselState.velocityVesselOrbit, vesselState.massDrag / vesselState.mass).magnitude;
        }

        [ValueInfoItem(name = "Synodic period")]
        public string SynodicPeriod()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (!core.target.Orbit.referenceBody == orbit.referenceBody) return "N/A";
            return GuiUtils.TimeToDHMS(orbit.SynodicPeriod(core.target.Orbit));
        }

        [ValueInfoItem(name = "Phase angle to target")]
        public string PhaseAngle()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (!core.target.Orbit.referenceBody == orbit.referenceBody) return "N/A";
            Vector3d projectedTarget = Vector3d.Exclude(orbit.SwappedOrbitNormal(), core.target.Position - mainBody.position);
            double angle = Vector3d.Angle(vesselState.CoM - mainBody.position, projectedTarget);
            if (Vector3d.Dot(Vector3d.Cross(orbit.SwappedOrbitNormal(), vesselState.CoM - mainBody.position), projectedTarget) < 0)
            {
                angle = 360 - angle;
            }
            return angle.ToString("F2") + "º";
        }

        [ValueInfoItem(name = "Circular orbit speed", units = "m/s")]
        public double CircularOrbitSpeed()
        {
            return OrbitalManeuverCalculator.CircularOrbitSpeed(mainBody, vesselState.radius);
        }



        [Persistent(pass = (int)Pass.Global)]
        public bool showInitialMass = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showFinalMass = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showInitialTWR = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showMaxTWR = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showVacDeltaV = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showVacTime = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showAtmoDeltaV = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showAtmoTime = true;

        [GeneralInfoItem(name = "Stage stats (all)")]
        public void AllStageStats()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();

            stats.RequestUpdate();

            int numStages = stats.atmoStats.Length;
            var stages = Enumerable.Range(0, numStages);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Stage stats", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("All stats", GUILayout.ExpandWidth(false)))
            {
                showInitialMass = showInitialTWR = showMaxTWR = showVacDeltaV = showVacTime = showAtmoDeltaV = showAtmoTime = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawStageStatsColumn("Stage", stages.Select(s => s.ToString()));
            if (showInitialMass) showInitialMass = !DrawStageStatsColumn("Start mass", stages.Select(s => stats.vacStats[s].startMass.ToString("F1") + " t"));
            if (showFinalMass) showFinalMass = !DrawStageStatsColumn("End mass", stages.Select(s => stats.vacStats[s].endMass.ToString("F1") + " t"));
            if (showInitialTWR) showInitialTWR = !DrawStageStatsColumn("TWR", stages.Select(s => stats.vacStats[s].StartTWR(mainBody).ToString("F2")));
            if (showMaxTWR) showMaxTWR = !DrawStageStatsColumn("Max TWR", stages.Select(s => stats.vacStats[s].MaxTWR(mainBody).ToString("F2")));
            if (showVacDeltaV) showVacDeltaV = !DrawStageStatsColumn("Vac ΔV", stages.Select(s => stats.vacStats[s].deltaV.ToString("F0") + " m/s"));
            if (showVacTime) showVacTime = !DrawStageStatsColumn("Vac time", stages.Select(s => GuiUtils.TimeToDHMS(stats.vacStats[s].deltaTime)));
            if (showAtmoDeltaV) showAtmoDeltaV = !DrawStageStatsColumn("Atmo ΔV", stages.Select(s => stats.atmoStats[s].deltaV.ToString("F0") + " m/s"));
            if (showAtmoTime) showAtmoTime = !DrawStageStatsColumn("Atmo time", stages.Select(s => GuiUtils.TimeToDHMS(stats.atmoStats[s].deltaTime)));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        bool DrawStageStatsColumn(string header, IEnumerable<string> data)
        {
            GUILayout.BeginVertical();
            GUIStyle s = new GUIStyle(GuiUtils.yellowOnHover);
            s.wordWrap = false;
            bool ret = GUILayout.Button(header, s);

            s = new GUIStyle(GUI.skin.label);
            s.alignment = TextAnchor.MiddleRight;
            s.wordWrap = false;
            foreach (string datum in data) GUILayout.Label(datum, s);

            GUILayout.EndVertical();

            return ret;
        }

        [ActionInfoItem(name = "Update stage stats")]
        public void UpdateStageStats()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();

            stats.RequestUpdate();
        }

        [GeneralInfoItem(name = "Docking guidance: velocity")]
        public void DockingGuidanceVelocity()
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label("Target-relative velocity: (N/A)");
                return;
            }

            Vector3d relVel = core.target.RelativeVelocity;
            double relVel_x = Vector3d.Dot(relVel, vessel.GetTransform().right);
            double relVel_y = Vector3d.Dot(relVel, vessel.GetTransform().forward);
            double relVel_z = Vector3d.Dot(relVel, vessel.GetTransform().up);
            GUILayout.BeginVertical();
            GUILayout.Label("Target-relative velocity:");
            GUILayout.Label("X: " + relVel_x.ToString("F2") + " m/s  [L/J]");
            GUILayout.Label("Y: " + relVel_y.ToString("F2") + " m/s  [I/K]");
            GUILayout.Label("Z: " + relVel_z.ToString("F2") + " m/s  [H/N]");
            GUILayout.EndVertical();
        }

        [GeneralInfoItem(name = "Docking guidance: velocity")]
        public void DockingGuidancePosition()
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label("Separation from target: (N/A)");
                return;
            }

            Vector3d sep = core.target.RelativePosition;
            double sep_x = Vector3d.Dot(sep, vessel.GetTransform().right);
            double sep_y = Vector3d.Dot(sep, vessel.GetTransform().forward);
            double sep_z = Vector3d.Dot(sep, vessel.GetTransform().up);
            GUILayout.BeginVertical();
            GUILayout.Label("Separation from target:");
            GUILayout.Label("X: " + sep_x.ToString("F2") + " m  [L/J]");
            GUILayout.Label("Y: " + sep_y.ToString("F2") + " m  [I/K]");
            GUILayout.Label("Z: " + sep_z.ToString("F2") + " m  [H/N]");
            GUILayout.EndVertical();
        }
    }
}
