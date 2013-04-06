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

        [ValueInfoItem("Node burn time", InfoItem.Category.Misc)]
        public string NextManeuverNodeBurnTime()
        {
            if (!vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
            double time = node.GetBurnVector(node.patch).magnitude / vesselState.maxThrustAccel;
            return GuiUtils.TimeToDHMS(time);
        }

        [ValueInfoItem("Time to node", InfoItem.Category.Misc)]
        public string TimeToManeuverNode()
        {
            if (!vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            return GuiUtils.TimeToDHMS(vessel.patchedConicSolver.maneuverNodes[0].UT - vesselState.time);
        }

        [ValueInfoItem("Node dV", InfoItem.Category.Misc)]
        public string NextManeuverNodeDeltaV()
        {
            if (!vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            return MuUtils.ToSI(vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(orbit).magnitude, -1) + "m/s";
        }

        [ValueInfoItem("Surface TWR", InfoItem.Category.Vessel, format = "F2", showInEditor = true)]
        public double SurfaceTWR()
        {
            if (HighLogic.LoadedSceneIsEditor) return MaxAcceleration() / 9.81;
            else return vesselState.thrustAvailable / (vesselState.mass * mainBody.GeeASL * 9.81);
        }

        [ValueInfoItem("Atmospheric pressure", InfoItem.Category.Misc, format = "F3", units = "atm")]
        public double AtmosphericPressure()
        {
            return FlightGlobals.getStaticPressure(vesselState.CoM);
        }

        [ValueInfoItem("Coordinates", InfoItem.Category.Surface)]
        public string GetCoordinateString()
        {
            return Coordinates.ToStringDMS(vesselState.latitude, vesselState.longitude, true);
        }

        public string OrbitSummary(Orbit o)
        {
            if (o.eccentricity > 1) return "hyperbolic, Pe = " + MuUtils.ToSI(o.PeA, 2) + "m";
            else return MuUtils.ToSI(o.PeA, 2) + "m x " + MuUtils.ToSI(o.ApA, 2) + "m";
        }

        public string OrbitSummaryWithInclination(Orbit o)
        {
            return OrbitSummary(o) + ", inc. " + o.inclination.ToString("F1") + "º";
        }

        [ValueInfoItem("Orbit", InfoItem.Category.Orbit)]
        public string CurrentOrbitSummary()
        {
            return OrbitSummary(orbit);
        }

        [ValueInfoItem("Target orbit", InfoItem.Category.Target)]
        public string TargetOrbitSummary()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return OrbitSummary(core.target.Orbit);
        }

        [ValueInfoItem("Orbit", InfoItem.Category.Orbit, description = "Orbit shape w/ inc.")]
        public string CurrentOrbitSummaryWithInclination()
        {
            return OrbitSummaryWithInclination(orbit);
        }

        [ValueInfoItem("Target orbit", InfoItem.Category.Target, description = "Target orbit shape w/ inc.")]
        public string TargetOrbitSummaryWithInclination()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return OrbitSummaryWithInclination(core.target.Orbit);
        }


        //Todo: consider turning this into a binary search
        [ValueInfoItem("Time to impact", InfoItem.Category.Misc)]
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

        [ValueInfoItem("Suicide burn countdown", InfoItem.Category.Misc)]
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

        [ValueInfoItem("Current acceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")]
        public double CurrentAcceleration()
        {
            return vesselState.ThrustAccel(FlightInputHandler.state.mainThrottle);
        }

        [ValueInfoItem("Time to SoI switch", InfoItem.Category.Orbit)]
        public string TimeToSOITransition()
        {
            if (orbit.patchEndTransition == Orbit.PatchTransitionType.FINAL) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.EndUT - vesselState.time);
        }

        [ValueInfoItem("Surface gravity", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s²")]
        public double SurfaceGravity()
        {
            return mainBody.GeeASL * 9.81;
        }

        [ValueInfoItem("Vessel mass", InfoItem.Category.Vessel, format = "F2", units = "t", showInEditor = true)]
        public double VesselMass()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList
                                  .Where(p => p.physicalSignificance != Part.PhysicalSignificance.NONE).Sum(p => p.TotalMass());
            else return vesselState.mass;
        }

        [ValueInfoItem("Max thrust", InfoItem.Category.Vessel, format = "F0", units = "kN", showInEditor = true)]
        public double MaxThrust()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                var engines = (from part in EditorLogic.SortedShipList
                               where part.inverseStage == Staging.lastStage
                               from engine in part.Modules.OfType<ModuleEngines>()
                               select engine);
                return engines.Sum(e => e.maxThrust);
            }
            else
            {
                return vesselState.thrustAvailable;
            }
        }

        [ValueInfoItem("Min thrust", InfoItem.Category.Vessel, format = "F0", units = "kN", showInEditor = true)]
        public double MinThrust()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                var engines = (from part in EditorLogic.SortedShipList
                               where part.inverseStage == Staging.lastStage
                               from engine in part.Modules.OfType<ModuleEngines>()
                               select engine);
                return engines.Sum(e => (e.throttleLocked ? e.maxThrust : e.minThrust));
            }
            else
            {
                return vesselState.thrustMinimum;
            }
        }

        [ValueInfoItem("Max acceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²", showInEditor = true)]
        public double MaxAcceleration()
        {
            return MaxThrust() / VesselMass();
        }

        [ValueInfoItem("Min acceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²", showInEditor = true)]
        public double MinAcceleration()
        {
            return MinThrust() / VesselMass();
        }

        [ValueInfoItem("Drag coefficient", InfoItem.Category.Vessel, format = "F3", showInEditor = true)]
        public double DragCoefficient()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                return EditorLogic.SortedShipList.Where(p => p.physicalSignificance != Part.PhysicalSignificance.NONE)
                                  .Sum(p => p.TotalMass() * p.maximum_drag) / VesselMass();
            }
            else
            {
                return vesselState.massDrag / vesselState.mass;
            }
        }

        [ValueInfoItem("Part count", InfoItem.Category.Vessel, showInEditor = true)]
        public int PartCount()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.Count;
            else return vessel.parts.Count;
        }

        [ValueInfoItem("Strut count", InfoItem.Category.Vessel, showInEditor = true)]
        public int StrutCount()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.Count(p => p is StrutConnector);
            else return vessel.parts.Count(p => p is StrutConnector);
        }

        [ValueInfoItem("Vessel cost", InfoItem.Category.Vessel, showInEditor = true, format = ValueInfoItem.SI, units = "$")]
        public double VesselCost()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.Sum(p => p.partInfo.cost) * 1000;
            else return vessel.parts.Sum(p => p.partInfo.cost) * 1000;
        }

        [ValueInfoItem("Crew count", InfoItem.Category.Vessel)]
        public int CrewCount()
        {
            return vessel.GetCrewCount();
        }

        [ValueInfoItem("Crew capacity", InfoItem.Category.Vessel, showInEditor = true)]
        public int CrewCapacity()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.Sum(p => p.CrewCapacity);
            else return vessel.GetCrewCapacity();
        }

        [ValueInfoItem("Distance to target", InfoItem.Category.Target)]
        public string TargetDistance()
        {
            if (core.target.Target == null) return "N/A";
            return MuUtils.ToSI(core.target.Distance, -1) + "m";
        }

        [ValueInfoItem("Heading to target", InfoItem.Category.Target)]
        public string HeadingToTarget()
        {
            if (core.target.Target == null) return "N/A";
            return vesselState.HeadingFromDirection(-core.target.RelativePosition).ToString("F1");
        }

        [ValueInfoItem("Relative velocity", InfoItem.Category.Target)]
        public string TargetRelativeVelocity()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.RelativeVelocity.magnitude) + "m/s";
        }

        [ValueInfoItem("Time to closest approach", InfoItem.Category.Target)]
        public string TargetTimeToClosestApproach()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (!core.target.Orbit.referenceBody == orbit.referenceBody) return "N/A";
            return GuiUtils.TimeToDHMS(orbit.NextClosestApproachTime(core.target.Orbit, vesselState.time) - vesselState.time);
        }

        [ValueInfoItem("Closest approach distance", InfoItem.Category.Target)]
        public string TargetClosestApproachDistance()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (!core.target.Orbit.referenceBody == orbit.referenceBody) return "N/A";
            return MuUtils.ToSI(orbit.NextClosestApproachDistance(core.target.Orbit, vesselState.time), -1) + "m";
        }

        [ValueInfoItem("Rel. vel. at closest approach", InfoItem.Category.Target)]
        public string TargetClosestApproachRelativeVelocity()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (!core.target.Orbit.referenceBody == orbit.referenceBody) return "N/A";
            double UT = orbit.NextClosestApproachTime(core.target.Orbit, vesselState.time);
            double relVel = (orbit.SwappedOrbitalVelocityAtUT(UT) - core.target.Orbit.SwappedOrbitalVelocityAtUT(UT)).magnitude;
            return MuUtils.ToSI(relVel, -1) + "m/s";
        }

        [ValueInfoItem("Target apoapsis", InfoItem.Category.Target)]
        public string TargetApoapsis()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.Orbit.ApA, 2) + "m";
        }

        [ValueInfoItem("Target periapsis", InfoItem.Category.Target)]
        public string TargetPeriapsis()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.Orbit.PeA, 2) + "m";
        }

        [ValueInfoItem("Target inclination", InfoItem.Category.Target)]
        public string TargetInclination()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return core.target.Orbit.inclination.ToString("F2") + "º";
        }

        [ValueInfoItem("Target orbit period", InfoItem.Category.Target)]
        public string TargetOrbitPeriod()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.Orbit.period);
        }

        [ValueInfoItem("Target orbit speed", InfoItem.Category.Target)]
        public string TargetOrbitSpeed()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.Orbit.GetVel().magnitude) + "m/s";
        }

        [ValueInfoItem("Target time to Ap", InfoItem.Category.Target)]
        public string TargetOrbitTimeToAp()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.Orbit.timeToAp);
        }

        [ValueInfoItem("Target time to Pe", InfoItem.Category.Target)]
        public string TargetOrbitTimeToPe()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.Orbit.timeToPe);
        }

        [ValueInfoItem("Target LAN", InfoItem.Category.Target)]
        public string TargetLAN()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return core.target.Orbit.LAN.ToString("F2") + "º";
        }

        [ValueInfoItem("Target eccentricity", InfoItem.Category.Target)]
        public string TargetEccentricity()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.Orbit.eccentricity);
        }

        [ValueInfoItem("Target SMA", InfoItem.Category.Target)]
        public string TargetSMA()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.Orbit.semiMajorAxis, 2) + "m";
        }

        [ValueInfoItem("Atmospheric drag", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")]
        public double AtmosphericDrag()
        {
            return mainBody.DragAccel(vesselState.CoM, vesselState.velocityVesselOrbit, vesselState.massDrag / vesselState.mass).magnitude;
        }

        [ValueInfoItem("Synodic period", InfoItem.Category.Target)]
        public string SynodicPeriod()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (!core.target.Orbit.referenceBody == orbit.referenceBody) return "N/A";
            return GuiUtils.TimeToDHMS(orbit.SynodicPeriod(core.target.Orbit));
        }

        [ValueInfoItem("Phase angle to target", InfoItem.Category.Target)]
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

        [ValueInfoItem("Circular orbit speed", InfoItem.Category.Orbit, format = ValueInfoItem.SI, units = "m/s")]
        public double CircularOrbitSpeed()
        {
            return OrbitalManeuverCalculator.CircularOrbitSpeed(mainBody, vesselState.radius);
        }



        [Persistent(pass = (int)Pass.Global)]
        public bool showInitialMass = false;
        [Persistent(pass = (int)Pass.Global)]
        public bool showFinalMass = false;
        [Persistent(pass = (int)Pass.Global)]
        public bool showInitialTWR = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showMaxTWR = false;
        [Persistent(pass = (int)Pass.Global)]
        public bool showVacDeltaV = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showVacTime = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showAtmoDeltaV = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showAtmoTime = true;

        [GeneralInfoItem("Stage stats (all)", InfoItem.Category.Vessel, showInEditor = true)]
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
            	if (showInitialMass)
            	{
            		showInitialTWR = showVacDeltaV = showVacTime = showAtmoDeltaV = showAtmoTime = true;
            		showInitialMass = showFinalMass = showMaxTWR = false;
            	}
            	else
            	{
	                showInitialMass = showInitialTWR = showMaxTWR = showVacDeltaV = showVacTime = showAtmoDeltaV = showAtmoTime = true;
            	}
            }
            GUILayout.EndHorizontal();

            double geeASL = (HighLogic.LoadedSceneIsEditor ? 1 : mainBody.GeeASL);

            GUILayout.BeginHorizontal();
            DrawStageStatsColumn("Stage", stages.Select(s => s.ToString()));
            if (showInitialMass) showInitialMass = !DrawStageStatsColumn("Start mass", stages.Select(s => stats.vacStats[s].startMass.ToString("F1") + " t"));
            if (showFinalMass) showFinalMass = !DrawStageStatsColumn("End mass", stages.Select(s => stats.vacStats[s].endMass.ToString("F1") + " t"));
            if (showInitialTWR) showInitialTWR = !DrawStageStatsColumn("TWR", stages.Select(s => stats.vacStats[s].StartTWR(geeASL).ToString("F2")));
            if (showMaxTWR) showMaxTWR = !DrawStageStatsColumn("Max TWR", stages.Select(s => stats.vacStats[s].MaxTWR(geeASL).ToString("F2")));
            if (showAtmoDeltaV) showAtmoDeltaV = !DrawStageStatsColumn("Atmo ΔV", stages.Select(s => stats.atmoStats[s].deltaV.ToString("F0") + " m/s"));
            if (showAtmoTime) showAtmoTime = !DrawStageStatsColumn("Atmo time", stages.Select(s => GuiUtils.TimeToDHMS(stats.atmoStats[s].deltaTime)));
            if (showVacDeltaV) showVacDeltaV = !DrawStageStatsColumn("Vac ΔV", stages.Select(s => stats.vacStats[s].deltaV.ToString("F0") + " m/s"));
            if (showVacTime) showVacTime = !DrawStageStatsColumn("Vac time", stages.Select(s => GuiUtils.TimeToDHMS(stats.vacStats[s].deltaTime)));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        bool DrawStageStatsColumn(string header, IEnumerable<string> data)
        {
            GUILayout.BeginVertical();
            GUIStyle s = new GUIStyle(GuiUtils.yellowOnHover) { alignment = TextAnchor.MiddleRight, wordWrap = false, padding = new RectOffset(2,2,0,0) };
            bool ret = GUILayout.Button(header + "   ", s);
            
            foreach (string datum in data) GUILayout.Label(datum + "   ", s);

            GUILayout.EndVertical();

            return ret;
        }

        /*[ActionInfoItem("Update stage stats", InfoItem.Category.Vessel)]
        public void UpdateStageStats()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();

            stats.RequestUpdate();
        }*/

        [ValueInfoItem("Stage ΔV (vac)", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]
        public float StageDeltaVVacuum()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            if (stats.vacStats.Length == 0) return 0;

            return stats.vacStats[stats.vacStats.Length - 1].deltaV;
        }

        [ValueInfoItem("Stage ΔV (atmo)", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]
        public float StageDeltaVAtmosphere()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            if (stats.atmoStats.Length == 0) return 0;

            return stats.atmoStats[stats.atmoStats.Length - 1].deltaV;
        }
        
        [ValueInfoItem("Stage ΔV (atmo, vac)", InfoItem.Category.Vessel, units = "m/s")]
        public string StageDeltaVAtmosphereAndVac()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            float atmDv = (stats.atmoStats.Length == 0) ? 0 : stats.atmoStats[stats.atmoStats.Length - 1].deltaV;
            float vacDv = (stats.vacStats.Length  == 0) ? 0 : stats.vacStats[ stats.vacStats.Length  - 1].deltaV;
            
            return String.Format("{0:F0}, {1:F0}", atmDv, vacDv);
        }

        [ValueInfoItem("Stage time (full throttle)", InfoItem.Category.Vessel, format = ValueInfoItem.TIME, showInEditor = true)]
        public float StageTimeLeftFullThrottle()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            if (stats.vacStats.Length == 0 || stats.atmoStats.Length == 0) return 0;

            float vacTimeLeft = stats.vacStats[stats.vacStats.Length - 1].deltaTime;
            float atmoTimeLeft = stats.atmoStats[stats.atmoStats.Length - 1].deltaTime;
            float timeLeft = Mathf.Lerp(vacTimeLeft, atmoTimeLeft, Mathf.Clamp01((float)FlightGlobals.getStaticPressure()));

            return timeLeft;
        }

        [ValueInfoItem("Stage time (current throttle)", InfoItem.Category.Vessel, format = ValueInfoItem.TIME)]
        public float StageTimeLeftCurrentThrottle()
        {
            float fullThrottleTime = StageTimeLeftFullThrottle();
            if (fullThrottleTime == 0) return 0;

            return fullThrottleTime / FlightInputHandler.state.mainThrottle;
        }

        [ValueInfoItem("Stage time (hover)", InfoItem.Category.Vessel, format = ValueInfoItem.TIME)]
        public float StageTimeLeftHover()
        {
            float fullThrottleTime = StageTimeLeftFullThrottle();
            if (fullThrottleTime == 0) return 0;

            double hoverThrottle = vesselState.localg / vesselState.maxThrustAccel;
            return fullThrottleTime / (float)hoverThrottle;
        }

        [ValueInfoItem("Total ΔV (vacuum)", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]
        public float TotalDeltaVVaccum()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();
            return stats.vacStats.Sum(s => s.deltaV);
        }

        [ValueInfoItem("Total ΔV (atmo)", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]
        public float TotalDeltaVAtmosphere()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();
            return stats.atmoStats.Sum(s => s.deltaV);
        }

        [ValueInfoItem("Total ΔV (atmo, vac)", InfoItem.Category.Vessel, units = "m/s")]
        public string TotalDeltaVAtmosphereAndVac()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            float atmDv = stats.atmoStats.Sum(s => s.deltaV);
            float vacDv = stats.vacStats.Sum( s => s.deltaV);
            
            return String.Format("{0:F0}, {1:F0}", atmDv, vacDv);
        }

        [GeneralInfoItem("Docking guidance: velocity", InfoItem.Category.Target)]
        public void DockingGuidanceVelocity()
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label("Target-relative velocity: (N/A)");
                return;
            }

            Vector3d relVel = core.target.RelativeVelocity;
            double relVelX = Vector3d.Dot(relVel, vessel.GetTransform().right);
            double relVelY = Vector3d.Dot(relVel, vessel.GetTransform().forward);
            double relVelZ = Vector3d.Dot(relVel, vessel.GetTransform().up);
            GUILayout.BeginVertical();
            GUILayout.Label("Target-relative velocity:");
            GUILayout.Label("X: " + relVelX.ToString("F2") + " m/s  [L/J]");
            GUILayout.Label("Y: " + relVelY.ToString("F2") + " m/s  [I/K]");
            GUILayout.Label("Z: " + relVelZ.ToString("F2") + " m/s  [H/N]");
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("Docking guidance: position", InfoItem.Category.Target)]
        public void DockingGuidancePosition()
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label("Separation from target: (N/A)");
                return;
            }

            Vector3d sep = core.target.RelativePosition;
            double sepX = Vector3d.Dot(sep, vessel.GetTransform().right);
            double sepY = Vector3d.Dot(sep, vessel.GetTransform().forward);
            double sepZ = Vector3d.Dot(sep, vessel.GetTransform().up);
            GUILayout.BeginVertical();
            GUILayout.Label("Separation from target:");
            GUILayout.Label("X: " + sepX.ToString("F2") + " m  [L/J]");
            GUILayout.Label("Y: " + sepY.ToString("F2") + " m  [I/K]");
            GUILayout.Label("Z: " + sepZ.ToString("F2") + " m  [H/N]");
            GUILayout.EndVertical();
        }
    }
}
