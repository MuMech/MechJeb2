using System;
using System.Collections.Generic;
using System.Text;
using CompoundParts;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.UI.Screens;
using Smooth.Pools;
using UniLinq;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    //This class just exists to collect miscellaneous info item functions in one place.
    //If any of these functions are useful in another module, they should be moved there.

    [UsedImplicitly]
    public class MechJebModuleInfoItems : ComputerModule
    {
        public MechJebModuleInfoItems(MechJebCore core) : base(core) { }

        //Provides a unified interface for getting the parts list in the editor or in flight:
        private List<Part> parts =>
            HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts :
            Vessel == null                ? new List<Part>() : Vessel.Parts;

        [ValueInfoItem("#MechJeb_NodeBurnTime", InfoItem.Category.Misc)] //Node burn time
        public string NextManeuverNodeBurnTime()
        {
            if (!Vessel.patchedConicsUnlocked() || !Vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            ManeuverNode node = Vessel.patchedConicSolver.maneuverNodes.First();
            double burnTime = node.GetBurnVector(node.patch).magnitude / VesselState.limitedMaxThrustAccel;
            return GuiUtils.TimeToDHMS(burnTime);
        }

        [ValueInfoItem("#MechJeb_TimeToNode", InfoItem.Category.Misc)] //Time to node
        public string TimeToManeuverNode()
        {
            if (!Vessel.patchedConicsUnlocked() || !Vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            return GuiUtils.TimeToDHMS(Vessel.patchedConicSolver.maneuverNodes[0].UT - VesselState.time);
        }

        [ValueInfoItem("#MechJeb_NodedV", InfoItem.Category.Misc)] //Node dV
        public string NextManeuverNodeDeltaV()
        {
            if (!Vessel.patchedConicsUnlocked() || !Vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            return Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit).magnitude.ToSI() + "m/s";
        }

        [ValueInfoItem("#MechJeb_SurfaceTWR", InfoItem.Category.Vessel, format = "F2", showInEditor = true)] //Surface TWR
        public double SurfaceTWR()
        {
            return HighLogic.LoadedSceneIsEditor
                ? MaxAcceleration() / 9.81
                : VesselState.thrustAvailable / (VesselState.mass * MainBody.GeeASL * 9.81);
        }

        [ValueInfoItem("#MechJeb_LocalTWR", InfoItem.Category.Vessel, format = "F2", showInEditor = false)] //Local TWR
        public double LocalTWR()
        {
            return VesselState.thrustAvailable / (VesselState.mass * VesselState.gravityForce.magnitude);
        }

        [ValueInfoItem("#MechJeb_ThrottleTWR", InfoItem.Category.Vessel, format = "F2", showInEditor = false)] //Throttle TWR
        public double ThrottleTWR()
        {
            return VesselState.thrustCurrent / (VesselState.mass * VesselState.gravityForce.magnitude);
        }

        [ValueInfoItem("#MechJeb_AtmosphericPressurePa", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "Pa")] //Atmospheric pressure (Pa)
        public double AtmosphericPressurekPA()
        {
            return FlightGlobals.getStaticPressure(VesselState.CoM) * 1000;
        }

        [ValueInfoItem("#MechJeb_AtmosphericPressure", InfoItem.Category.Misc, format = "F3", units = "atm")] //Atmospheric pressure
        public double AtmosphericPressure()
        {
            return FlightGlobals.getStaticPressure(VesselState.CoM) * PhysicsGlobals.KpaToAtmospheres;
        }

        [ValueInfoItem("#MechJeb_Coordinates", InfoItem.Category.Surface)] //Coordinates
        public string GetCoordinateString()
        {
            return Coordinates.ToStringDMS(VesselState.latitude, VesselState.longitude, true);
        }

        public string OrbitSummary(Orbit o)
        {
            return o.eccentricity > 1 ? $"hyperbolic, Pe = {o.PeA.ToSI()}m" : $"{o.PeA.ToSI()}m x {o.ApA.ToSI()}m";
        }

        public string OrbitSummaryWithInclination(Orbit o)
        {
            return OrbitSummary(o) + ", inc. " + o.inclination.ToString("F1") + "º";
        }

        [ValueInfoItem("#MechJeb_MeanAnomaly", InfoItem.Category.Orbit, format = ValueInfoItem.ANGLE)] //Mean Anomaly
        public double MeanAnomaly()
        {
            return Orbit.meanAnomaly * UtilMath.Rad2Deg;
        }

        [ValueInfoItem("#MechJeb_Orbit", InfoItem.Category.Orbit)] //Orbit
        public string CurrentOrbitSummary()
        {
            return OrbitSummary(Orbit);
        }

        [ValueInfoItem("#MechJeb_TargetOrbit", InfoItem.Category.Target)] //Target orbit
        public string TargetOrbitSummary()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return OrbitSummary(Core.Target.TargetOrbit);
        }

        [ValueInfoItem("#MechJeb_OrbitWithInc", InfoItem.Category.Orbit, description = "#MechJeb_OrbitWithInc_desc")] //Orbit||Orbit shape w/ inc.
        public string CurrentOrbitSummaryWithInclination()
        {
            return OrbitSummaryWithInclination(Orbit);
        }

        [ValueInfoItem("#MechJeb_TargetOrbitWithInc", InfoItem.Category.Target,
            description = "#MechJeb_TargetOrbitWithInc_desc")] //Target orbit|Target orbit shape w/ inc.
        public string TargetOrbitSummaryWithInclination()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return OrbitSummaryWithInclination(Core.Target.TargetOrbit);
        }

        [ValueInfoItem("#MechJeb_OrbitalEnergy", InfoItem.Category.Orbit, description = "#MechJeb_OrbitalEnergy_desc", format = ValueInfoItem.SI,
            units = "J/kg")] //Orbital energy||Specific orbital energy
        public double OrbitalEnergy()
        {
            return Orbit.orbitalEnergy;
        }

        [ValueInfoItem("#MechJeb_PotentialEnergy", InfoItem.Category.Orbit, description = "#MechJeb_PotentialEnergy_desc", format = ValueInfoItem.SI,
            units = "J/kg")] //Potential energy||Specific potential energy
        public double PotentialEnergy()
        {
            return -Orbit.referenceBody.gravParameter / Orbit.radius;
        }

        [ValueInfoItem("#MechJeb_KineticEnergy", InfoItem.Category.Orbit, description = "#MechJeb_KineticEnergy_desc", format = ValueInfoItem.SI,
            units = "J/kg")] //Kinetic energy||Specific kinetic energy
        public double KineticEnergy()
        {
            return Orbit.orbitalEnergy + Orbit.referenceBody.gravParameter / Orbit.radius;
        }

        //TODO: consider turning this into a binary search
        [ValueInfoItem("#MechJeb_TimeToImpact", InfoItem.Category.Misc)] //Time to impact
        public string TimeToImpact()
        {
            if (Orbit.PeA > 0 || Vessel.Landed) return "N/A";

            double impactTime = VesselState.time;
            try
            {
                for (int iter = 0; iter < 10; iter++)
                {
                    Vector3d impactPosition = Orbit.WorldPositionAtUT(impactTime);
                    double terrainRadius = MainBody.Radius + MainBody.TerrainAltitude(impactPosition);
                    impactTime = Orbit.NextTimeOfRadius(VesselState.time, terrainRadius);
                }
            }
            catch (ArgumentException)
            {
                return GuiUtils.TimeToDHMS(0);
            }
            catch (ArithmeticException)
            {
                return GuiUtils.TimeToDHMS(0);
            }

            return GuiUtils.TimeToDHMS(impactTime - VesselState.time);
        }

        [ValueInfoItem("#MechJeb_SuicideBurnCountdown", InfoItem.Category.Misc)] //Suicide burn countdown
        public string SuicideBurnCountdown()
        {
            try
            {
                return GuiUtils.TimeToDHMS(OrbitExtensions.SuicideBurnCountdown(Orbit, VesselState, Vessel), 1);
            }
            catch
            {
                return "N/A";
            }
        }

        private readonly MovingAverage rcsThrustAvg = new MovingAverage();

        [ValueInfoItem("#MechJeb_RCSthrust", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "N")] //RCS thrust
        public double RCSThrust()
        {
            rcsThrustAvg.value = RCSThrustNow();
            return rcsThrustAvg.value * 1000; // kN to N
        }

        // Returns a value in kN.
        private double RCSThrustNow()
        {
            double rcsThrust = 0;

            for (int i = 0; i < Vessel.parts.Count; i++)
            {
                Part p = Vessel.parts[i];
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (p.Rigidbody == null || !pm.isEnabled || pm.isJustForShow)
                    {
                        continue;
                    }

                    for (int j = 0; j < pm.thrustForces.Length; j++)
                    {
                        rcsThrust += pm.thrustForces[j] * pm.thrusterPower;
                    }
                }
            }

            return rcsThrust;
        }

        private readonly MovingAverage rcsTranslationEfficiencyAvg = new MovingAverage();

        [ValueInfoItem("#MechJeb_RCSTranslationEfficiency", InfoItem.Category.Misc)] //RCS translation efficiency
        public string RCSTranslationEfficiency()
        {
            double totalThrust = RCSThrustNow();
            double effectiveThrust = 0;
            FlightCtrlState s = FlightInputHandler.state;

            // FlightCtrlState and a vessel have different coordinate systems.
            // See MechJebModuleRCSController for a comment explaining this.
            var direction = new Vector3(-s.X, -s.Z, -s.Y);
            if (totalThrust == 0 || direction.magnitude == 0)
            {
                return "--";
            }

            direction.Normalize();

            for (int index = 0; index < Vessel.parts.Count; index++)
            {
                Part p = Vessel.parts[index];
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (p.Rigidbody == null || !pm.isEnabled || pm.isJustForShow)
                    {
                        continue;
                    }

                    // Find our distance from the vessel's center of mass, in
                    // world coordinates.
                    Vector3 pos = p.Rigidbody.worldCenterOfMass - VesselState.CoM;

                    // Translate to the vessel's reference frame.
                    pos = Quaternion.Inverse(Vessel.GetTransform().rotation) * pos;

                    for (int i = 0; i < pm.thrustForces.Length; i++)
                    {
                        float force = pm.thrustForces[i];
                        Transform t = pm.thrusterTransforms[i];

                        Vector3 thrusterDir = Quaternion.Inverse(Vessel.GetTransform().rotation) * -t.up;
                        double thrusterEfficiency = Vector3.Dot(direction, thrusterDir.normalized);

                        effectiveThrust += thrusterEfficiency * pm.thrusterPower * force;
                    }
                }
            }

            rcsTranslationEfficiencyAvg.value = effectiveThrust / totalThrust;
            return (rcsTranslationEfficiencyAvg.value * 100).ToString("F2") + "%";
        }

        [ValueInfoItem("#MechJeb_RCSdV", InfoItem.Category.Vessel, format = "F1", units = "m/s", showInEditor = true)] //RCS ΔV
        public double RCSDeltaVVacuum()
        {
            // Use the average specific impulse of all RCS parts.
            double totalIsp = 0;
            int numThrusters = 0;
            double gForRCS = 9.81;

            double monopropMass = Vessel.TotalResourceMass("MonoPropellant");

            foreach (ModuleRCS pm in Vessel.GetModules<ModuleRCS>())
            {
                totalIsp += pm.atmosphereCurve.Evaluate(0);
                numThrusters++;
                gForRCS = pm.G;
            }

            double m0 = VesselMass();
            double m1 = m0 - monopropMass;
            if (numThrusters == 0 || m1 <= 0) return 0;
            double isp = totalIsp / numThrusters;
            return isp * gForRCS * Math.Log(m0 / m1);
        }

        [ValueInfoItem("#MechJeb_AngularVelocity", InfoItem.Category.Vessel, showInEditor = false, showInFlight = true)] //Angular Velocity
        public string angularVelocity()
        {
            return MuUtils.PrettyPrint(VesselState.angularVelocityAvg.value.xzy * UtilMath.Rad2Deg) + "°/s";
        }

        [ValueInfoItem("#MechJeb_CurrentAcceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")] //Current acceleration
        public double CurrentAcceleration()
        {
            return CurrentThrust() / (1000 * VesselMass());
        }

        [ValueInfoItem("#MechJeb_CurrentThrust", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "N")] //Current thrust
        public double CurrentThrust()
        {
            return VesselState.thrustCurrent * 1000;
        }

        [ValueInfoItem("#MechJeb_TimeToSoIWwitch", InfoItem.Category.Orbit)] //Time to SoI switch
        public string TimeToSOITransition()
        {
            if (Orbit.patchEndTransition == Orbit.PatchTransitionType.FINAL) return "N/A";

            return GuiUtils.TimeToDHMS(Orbit.EndUT - VesselState.time);
        }

        [ValueInfoItem("#MechJeb_SurfaceGravity", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s²")] //Surface gravity
        public double SurfaceGravity()
        {
            return MainBody.GeeASL * 9.81;
        }

        [ValueInfoItem("#MechJeb_EscapeVelocity", InfoItem.Category.Orbit, format = ValueInfoItem.SI, siSigFigs = 3, units = "m/s")] //Escape velocity
        public double EscapeVelocity()
        {
            return Math.Sqrt(2 * MainBody.gravParameter / VesselState.radius);
        }

        [ValueInfoItem("#MechJeb_VesselName", InfoItem.Category.Vessel, showInEditor = false)] //Vessel name
        public string VesselName()
        {
            return Vessel.vesselName;
        }

        [ValueInfoItem("#MechJeb_VesselType", InfoItem.Category.Vessel, showInEditor = false)] //Vessel type
        public string VesselType()
        {
            return Vessel != null ? Vessel.vesselType.displayDescription() : "-";
        }

        [ValueInfoItem("#MechJeb_VesselMass", InfoItem.Category.Vessel, format = "F3", units = "t", showInEditor = true)] //Vessel mass
        public double VesselMass()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.fetch.ship.parts.Sum(p => p.mass + p.GetResourceMass());
            return VesselState.mass;
        }

        [ValueInfoItem("#MechJeb_MaxVesselMass", InfoItem.Category.Vessel, showInEditor = true, showInFlight = false)] //Max vessel mass
        public string MaximumVesselMass()
        {
            SpaceCenterFacility rolloutFacility =
                EditorDriver.editorFacility == EditorFacility.VAB ? SpaceCenterFacility.LaunchPad : SpaceCenterFacility.Runway;
            float maximumVesselMass = GameVariables.Instance.GetCraftMassLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(rolloutFacility),
                EditorDriver.editorFacility == EditorFacility.VAB);
            return maximumVesselMass < float.MaxValue
                ? $"{maximumVesselMass:F3} t"
                : CachedLocalizer.Instance.MechJeb_InfoItems_UnlimitedText; //"Unlimited"
        }

        [ValueInfoItem("#MechJeb_DryMass", InfoItem.Category.Vessel, showInEditor = true, format = "F3", units = "t")] //Dry mass
        public double DryMass()
        {
            return parts.Where(p => p.IsPhysicallySignificant()).Sum(p => p.mass + p.GetPhysicslessChildMass());
        }

        [ValueInfoItem("#MechJeb_LiquidFuelandOxidizerMass", InfoItem.Category.Vessel, showInEditor = true, format = "F2",
            units = "t")] //Liquid fuel & oxidizer mass
        public double LiquidFuelAndOxidizerMass()
        {
            return Vessel.TotalResourceMass("LiquidFuel") + Vessel.TotalResourceMass("Oxidizer");
        }

        [ValueInfoItem("#MechJeb_MonopropellantMass", InfoItem.Category.Vessel, showInEditor = true, format = "F2",
            units = "kg")] //Monopropellant mass
        public double MonoPropellantMass()
        {
            return Vessel.TotalResourceMass("MonoPropellant");
        }

        [ValueInfoItem("#MechJeb_TotalElectricCharge", InfoItem.Category.Vessel, showInEditor = true, format = ValueInfoItem.SI,
            units = "Ah")] //Total electric charge
        public double TotalElectricCharge()
        {
            return Vessel.TotalResourceAmount(PartResourceLibrary.ElectricityHashcode);
        }

        [ValueInfoItem("#MechJeb_MaxThrust", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "N", showInEditor = true)] //Max thrust
        public double MaxThrust()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                IEnumerable<ModuleEngines> engines = from part in EditorLogic.fetch.ship.parts
                    where part.inverseStage == StageManager.LastStage
                    from engine in part.Modules.OfType<ModuleEngines>()
                    select engine;
                return 1000 * engines.Sum(e => e.minThrust + e.thrustPercentage / 100f * (e.maxThrust - e.minThrust));
            }

            return 1000 * VesselState.thrustAvailable;
        }

        [ValueInfoItem("#MechJeb_MinThrust", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "N", showInEditor = true)] //Min thrust
        public double MinThrust()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                IEnumerable<ModuleEngines> engines = from part in EditorLogic.fetch.ship.parts
                    where part.inverseStage == StageManager.LastStage
                    from engine in part.Modules.OfType<ModuleEngines>()
                    select engine;
                return 1000 * engines.Sum(e =>
                    e.throttleLocked ? e.minThrust + e.thrustPercentage / 100f * (e.maxThrust - e.minThrust) : e.minThrust);
            }

            return VesselState.thrustMinimum;
        }

        [ValueInfoItem("#MechJeb_MaxAcceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²",
            showInEditor = true)] //Max acceleration
        public double MaxAcceleration()
        {
            return MaxThrust() / (1000 * VesselMass());
        }

        [ValueInfoItem("#MechJeb_MinAcceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²",
            showInEditor = true)] //Min acceleration
        public double MinAcceleration()
        {
            return MinThrust() / (1000 * VesselMass());
        }

        [ValueInfoItem("#MechJeb_Gforce", InfoItem.Category.Vessel, format = "F4", units = "g", showInEditor = true)] //G force
        public double Acceleration()
        {
            return Vessel != null ? Vessel.geeForce : 0;
        }

        [ValueInfoItem("#MechJeb_DragCoefficient", InfoItem.Category.Vessel, format = "F3", showInEditor = true)] //Drag Coefficient
        public double DragCoefficient()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                // Still not working...

                //double dragCoef = 0;
                //for (int i = 0; i < EditorLogic.fetch.ship.parts.Count; i++)
                //{
                //    Part p = EditorLogic.fetch.ship.parts[i];
                //    if (p.ShieldedFromAirstream)
                //    {
                //        continue;
                //    }
                //
                //    Vector3d dragDir = -p.partTransform.InverseTransformDirection(vessel.GetTransform().up);
                //
                //    DragCubeList.CubeData data = p.DragCubes.AddSurfaceDragDirection(dragDir, 0.1f);
                //
                //    dragCoef += data.dragCoeff;
                //}
                //return dragCoef;

                return 0;
            }

            return VesselState.dragCoef;
        }

        [ValueInfoItem("#MechJeb_PartCount", InfoItem.Category.Vessel, showInEditor = true)] //Part count
        public int PartCount()
        {
            return parts.Count;
        }

        [ValueInfoItem("#MechJeb_MaxPartCount", InfoItem.Category.Vessel, showInEditor = true)] //Max part count
        public string MaxPartCount()
        {
            float editorFacilityLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(EditorDriver.editorFacility.ToFacility());
            int maxPartCount = GameVariables.Instance.GetPartCountLimit(editorFacilityLevel, EditorDriver.editorFacility == EditorFacility.VAB);
            if (maxPartCount < int.MaxValue)
                return maxPartCount.ToString();
            return Localizer.Format("#MechJeb_InfoItems_UnlimitedText"); //"Unlimited"
        }

        [ValueInfoItem("#MechJeb_PartCountDivideMaxParts", InfoItem.Category.Vessel, showInEditor = true)] //Part count / Max parts
        public string PartCountAndMaxPartCount()
        {
            return string.Format("{0} / {1}", PartCount().ToString(), MaxPartCount());
        }

        [ValueInfoItem("#MechJeb_StrutCount", InfoItem.Category.Vessel, showInEditor = true)] //Strut count
        public int StrutCount()
        {
            return parts.Count(p => p is CompoundPart && p.Modules.GetModule<CModuleStrut>());
        }

        [ValueInfoItem("#MechJeb_FuelLinesCount", InfoItem.Category.Vessel, showInEditor = true)] //Fuel Lines count
        public int FuelLinesCount()
        {
            return parts.Count(p => p is CompoundPart && p.Modules.GetModule<CModuleFuelLine>());
        }

        [ValueInfoItem("#MechJeb_VesselCost", InfoItem.Category.Vessel, showInEditor = true, format = ValueInfoItem.SI, units = "$")] //Vessel cost
        public double VesselCost()
        {
            return parts.Sum(p => p.partInfo.cost) * 1000;
        }

        [ValueInfoItem("#MechJeb_CrewCount", InfoItem.Category.Vessel)] //Crew count
        public int CrewCount()
        {
            return Vessel.GetCrewCount();
        }

        [ValueInfoItem("#MechJeb_CrewCapacity", InfoItem.Category.Vessel, showInEditor = true)] //Crew capacity
        public int CrewCapacity()
        {
            return parts.Sum(p => p.CrewCapacity);
        }

        [ValueInfoItem("#MechJeb_DistanceToTarget", InfoItem.Category.Target)] //Distance to target
        public string TargetDistance()
        {
            if (Core.Target.Target == null) return "N/A";
            return Core.Target.Distance.ToSI() + "m";
        }

        [ValueInfoItem("#MechJeb_HeadingToTarget", InfoItem.Category.Target)] //Heading to target
        public string HeadingToTarget()
        {
            if (Core.Target.Target == null) return "N/A";
            return VesselState.HeadingFromDirection(-Core.Target.RelativePosition).ToString("F1") + "º";
        }

        [ValueInfoItem("#MechJeb_RelativeVelocity", InfoItem.Category.Target)] //Relative velocity
        public string TargetRelativeVelocity()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return Core.Target.RelativeVelocity.magnitude.ToSI() + "m/s";
        }

        [ValueInfoItem("#MechJeb_TimeToClosestApproach", InfoItem.Category.Target)] //Time to closest approach
        public string TargetTimeToClosestApproach()
        {
            if (Core.Target.Target != null && VesselState.altitudeTrue < 1000.0)
            {
                return GuiUtils.TimeToDHMS(GuiUtils.FromToETA(Vessel.CoM, Core.Target.Transform.position));
            }

            if (!Core.Target.NormalTargetExists) return "N/A";
            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody) return "N/A";
            if (double.IsNaN(Core.Target.TargetOrbit.semiMajorAxis)) { return "N/A"; }

            if (VesselState.altitudeTrue < 1000.0)
            {
                double a = (Vessel.mainBody.transform.position - Vessel.transform.position).magnitude;
                double b = (Vessel.mainBody.transform.position - Core.Target.Transform.position).magnitude;
                double c = Vector3d.Distance(Vessel.transform.position, Core.Target.Position);
                double ang = Math.Acos((a * a + b * b - c * c) / (2f * a * b));
                return GuiUtils.TimeToDHMS(ang * Vessel.mainBody.Radius / VesselState.speedSurfaceHorizontal);
            }

            return GuiUtils.TimeToDHMS(Orbit.NextClosestApproachTime(Core.Target.TargetOrbit, VesselState.time) - VesselState.time);
        }

        [ValueInfoItem("#MechJeb_ClosestApproachDistance", InfoItem.Category.Target)] //Closest approach distance
        public string TargetClosestApproachDistance()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody) return "N/A";
            if (VesselState.altitudeTrue < 1000.0) { return "N/A"; }

            if (double.IsNaN(Core.Target.TargetOrbit.semiMajorAxis)) { return "N/A"; }

            return Orbit.NextClosestApproachDistance(Core.Target.TargetOrbit, VesselState.time).ToSI() + "m";
        }

        [ValueInfoItem("#MechJeb_RelativeVelocityAtClosestApproach", InfoItem.Category.Target)] //Rel. vel. at closest approach
        public string TargetClosestApproachRelativeVelocity()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody) return "N/A";
            if (VesselState.altitudeTrue < 1000.0) { return "N/A"; }

            if (double.IsNaN(Core.Target.TargetOrbit.semiMajorAxis)) { return "N/A"; }

            try
            {
                double UT = Orbit.NextClosestApproachTime(Core.Target.TargetOrbit, VesselState.time);

                if (double.IsNaN(UT))
                {
                    return "N/A";
                }

                double relVel =
                    (Orbit.WorldOrbitalVelocityAtUT(UT) - Core.Target.TargetOrbit.WorldOrbitalVelocityAtUT(UT))
                    .magnitude;
                return relVel.ToSI() + "m/s";
            }
            catch
            {
                return "N/A";
            }
        }

        [ValueInfoItem("#MechJeb_PeriapsisInTargetSoI", InfoItem.Category.Misc)] //Periapsis in target SoI
        public string PeriapsisInTargetSOI()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";

            Orbit o;
            if (Vessel.patchedConicsUnlocked() && Vessel.patchedConicSolver.maneuverNodes.Any())
            {
                ManeuverNode node = Vessel.patchedConicSolver.maneuverNodes.Last();
                o = node.nextPatch;
            }
            else
            {
                o = Vessel.orbit;
            }

            while (o != null && o.referenceBody != (CelestialBody)Vessel.targetObject)
                o = o.nextPatch;

            if (o == null) return "N/A";

            return o.PeA.ToSI() + "m";
        }

        [ValueInfoItem("#MechJeb_TargetCaptureDV", InfoItem.Category.Misc)] //ΔV for capture by target
        public string TargetCaptureDV()
        {
            if (!Core.Target.NormalTargetExists || !(Vessel.targetObject is CelestialBody)) return "N/A";

            Orbit o = Vessel.orbit;
            while (o != null && o.referenceBody != (CelestialBody)Vessel.targetObject)
                o = o.nextPatch;

            if (o == null) return "N/A";

            double smaCapture = (o.PeR + o.referenceBody.sphereOfInfluence) / 2;
            double velAtPeriapsis = Math.Sqrt(o.referenceBody.gravParameter * (2 / o.PeR - 1 / o.semiMajorAxis));
            double velCapture = Math.Sqrt(o.referenceBody.gravParameter * (2 / o.PeR - 1 / smaCapture));

            return (velAtPeriapsis - velCapture).ToSI() + "m/s";
        }

        [ValueInfoItem("#MechJeb_TargetApoapsis", InfoItem.Category.Target)] //Target apoapsis
        public string TargetApoapsis()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return Core.Target.TargetOrbit.ApA.ToSI() + "m";
        }

        [ValueInfoItem("#MechJeb_TargetPeriapsis", InfoItem.Category.Target)] //Target periapsis
        public string TargetPeriapsis()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return Core.Target.TargetOrbit.PeA.ToSI() + "m";
        }

        [ValueInfoItem("#MechJeb_TargetInclination", InfoItem.Category.Target)] //Target inclination
        public string TargetInclination()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return Core.Target.TargetOrbit.inclination.ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TargetOrbitPeriod", InfoItem.Category.Target)] //Target orbit period
        public string TargetOrbitPeriod()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(Core.Target.TargetOrbit.period);
        }

        [ValueInfoItem("#MechJeb_TargetOrbitSpeed", InfoItem.Category.Target)] //Target orbit speed
        public string TargetOrbitSpeed()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return Core.Target.TargetOrbit.GetVel().magnitude.ToSI() + "m/s";
        }

        [ValueInfoItem("#MechJeb_TargetTimeToAp", InfoItem.Category.Target)] //Target time to Ap
        public string TargetOrbitTimeToAp()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(Core.Target.TargetOrbit.timeToAp);
        }

        [ValueInfoItem("#MechJeb_TargetTimeToPe", InfoItem.Category.Target)] //Target time to Pe
        public string TargetOrbitTimeToPe()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(Core.Target.TargetOrbit.timeToPe);
        }

        [ValueInfoItem("#MechJeb_TargetLAN", InfoItem.Category.Target)] //Target LAN
        public string TargetLAN()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return Core.Target.TargetOrbit.LAN.ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TargetLDN", InfoItem.Category.Target)] //Target LDN
        public string TargetLDN()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return MuUtils.ClampDegrees360(Core.Target.TargetOrbit.LAN + 180).ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TargetTimeToAN", InfoItem.Category.Target)] //Target Time to AN
        public string TargetTimeToAscendingNode()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            if (!Core.Target.TargetOrbit.AscendingNodeEquatorialExists()) return "N/A";

            return GuiUtils.TimeToDHMS(Core.Target.TargetOrbit.TimeOfAscendingNodeEquatorial(VesselState.time) - VesselState.time);
        }

        [ValueInfoItem("#MechJeb_TargetTimeToDN", InfoItem.Category.Target)] //Target Time to DN
        public string TargetTimeToDescendingNode()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            if (!Core.Target.TargetOrbit.DescendingNodeEquatorialExists()) return "N/A";

            return GuiUtils.TimeToDHMS(Core.Target.TargetOrbit.TimeOfDescendingNodeEquatorial(VesselState.time) - VesselState.time);
        }

        [ValueInfoItem("#MechJeb_TargetAoP", InfoItem.Category.Target)] //Target AoP
        public string TargetAoP()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return Core.Target.TargetOrbit.argumentOfPeriapsis.ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TargetEccentricity", InfoItem.Category.Target)] //Target eccentricity
        public string TargetEccentricity()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(Core.Target.TargetOrbit.eccentricity);
        }

        [ValueInfoItem("#MechJeb_TargetSMA", InfoItem.Category.Target)] //Target SMA
        public string TargetSMA()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return Core.Target.TargetOrbit.semiMajorAxis.ToSI() + "m";
        }

        [ValueInfoItem("#MechJeb_TargetMeanAnomaly", InfoItem.Category.Target, format = ValueInfoItem.ANGLE)] //Target Mean Anomaly
        public string TargetMeanAnomaly()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            return MuUtils.ClampDegrees360(Core.Target.TargetOrbit.meanAnomaly * UtilMath.Rad2Deg).ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TargetTrueLongitude", InfoItem.Category.Target)] //Target Mean Anomaly
        public string TargetTrueLongitude()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            double longitudeOfPeriapsis = Core.Target.TargetOrbit.LAN + Core.Target.TargetOrbit.argumentOfPeriapsis;
            return MuUtils.ClampDegrees360(Core.Target.TargetOrbit.trueAnomaly * UtilMath.Rad2Deg + longitudeOfPeriapsis).ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_AtmosphericDrag", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")] //Atmospheric drag
        public double AtmosphericDrag()
        {
            return VesselState.drag;
        }

        [ValueInfoItem("#MechJeb_SynodicPeriod", InfoItem.Category.Target)] //Synodic period
        public string SynodicPeriod()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody) return "N/A";
            return GuiUtils.TimeToDHMS(Orbit.SynodicPeriod(Core.Target.TargetOrbit));
        }

        [ValueInfoItem("#MechJeb_PhaseAngleToTarget", InfoItem.Category.Target)] //Phase angle to target
        public string PhaseAngle()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody) return "N/A";
            if (double.IsNaN(Core.Target.TargetOrbit.semiMajorAxis)) { return "N/A"; }

            return Orbit.PhaseAngle(Core.Target.TargetOrbit, VesselState.time).ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TargetPlanetPhaseAngle", InfoItem.Category.Target)] //Target planet phase angle
        public string TargetPlanetPhaseAngle()
        {
            if (!(Core.Target.Target is CelestialBody)) return "N/A";
            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody.referenceBody) return "N/A";

            return MainBody.orbit.PhaseAngle(Core.Target.TargetOrbit, VesselState.time).ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_RelativeInclination", InfoItem.Category.Target)] //Relative inclination
        public string RelativeInclinationToTarget()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody) return "N/A";

            return Orbit.RelativeInclination(Core.Target.TargetOrbit).ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TimeToAN", InfoItem.Category.Target)] //Time to AN
        public string TimeToAscendingNodeWithTarget()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody) return "N/A";
            if (!Orbit.AscendingNodeExists(Core.Target.TargetOrbit)) return "N/A";

            return GuiUtils.TimeToDHMS(Orbit.TimeOfAscendingNode(Core.Target.TargetOrbit, VesselState.time) - VesselState.time);
        }

        [ValueInfoItem("#MechJeb_TimeToDN", InfoItem.Category.Target)] //Time to DN
        public string TimeToDescendingNodeWithTarget()
        {
            if (!Core.Target.NormalTargetExists) return "N/A";
            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody) return "N/A";
            if (!Orbit.DescendingNodeExists(Core.Target.TargetOrbit)) return "N/A";

            return GuiUtils.TimeToDHMS(Orbit.TimeOfDescendingNode(Core.Target.TargetOrbit, VesselState.time) - VesselState.time);
        }

        [ValueInfoItem("#MechJeb_TimeToEquatorialAN", InfoItem.Category.Orbit)] //Time to equatorial AN
        public string TimeToEquatorialAscendingNode()
        {
            if (!Orbit.AscendingNodeEquatorialExists()) return "N/A";

            return GuiUtils.TimeToDHMS(Orbit.TimeOfAscendingNodeEquatorial(VesselState.time) - VesselState.time);
        }

        [ValueInfoItem("#MechJeb_TimeToEquatorialDN", InfoItem.Category.Orbit)] //Time to equatorial DN
        public string TimeToEquatorialDescendingNode()
        {
            if (!Orbit.DescendingNodeEquatorialExists()) return "N/A";

            return GuiUtils.TimeToDHMS(Orbit.TimeOfDescendingNodeEquatorial(VesselState.time) - VesselState.time);
        }

        [ValueInfoItem("#MechJeb_CircularOrbitSpeed", InfoItem.Category.Orbit, format = ValueInfoItem.SI, units = "m/s")] //Circular orbit speed
        public double CircularOrbitSpeed()
        {
            return OrbitalManeuverCalculator.CircularOrbitSpeed(MainBody, VesselState.radius);
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showStagedMass = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showBurnedMass = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showInitialMass = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showFinalMass = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showThrust = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showVacInitialTWR = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showAtmoInitialTWR = false; // NK

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showAtmoMaxTWR = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showVacMaxTWR = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showVacDeltaV = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showTime = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showAtmoDeltaV = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showISP = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool liveSLT = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public float altSLTScale = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public float machScale = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int TWRBody = 1;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int StageDisplayState = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showEmpty = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool timeSeconds = false;

        private MechJebStageStatsHelper stageStatsHelper;

        // Leave this stub here until I figure out how to properly target in the new class
        [GeneralInfoItem("#MechJeb_StageStatsAll", InfoItem.Category.Vessel, showInEditor = true)] //Stage stats (all)
        public void AllStageStats()
        {
            if (stageStatsHelper == null)
                stageStatsHelper = new MechJebStageStatsHelper(this);
            stageStatsHelper.AllStageStats();
        }

        public void UpdateItems()
        {
            stageStatsHelper?.UpdateStageStats();
        }

        [ValueInfoItem("#MechJeb_StageDv_vac", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)] //Stage ΔV (vac)
        public double StageDeltaVVacuum()
        {
            MechJebModuleStageStats stats = Core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            if (stats.VacStats.Count == 0) return 0;

            return stats.VacStats[stats.VacStats.Count - 1].DeltaV;
        }

        [ValueInfoItem("#MechJeb_StageDV_atmo", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)] //Stage ΔV (atmo)
        public double StageDeltaVAtmosphere()
        {
            MechJebModuleStageStats stats = Core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            if (stats.AtmoStats.Count == 0) return 0;

            return stats.AtmoStats[stats.AtmoStats.Count - 1].DeltaV;
        }

        [ValueInfoItem("#MechJeb_StageDV_atmo_vac", InfoItem.Category.Vessel, units = "m/s", showInEditor = true)] //Stage ΔV (atmo, vac)
        public string StageDeltaVAtmosphereAndVac()
        {
            MechJebModuleStageStats stats = Core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            double atmDv = stats.AtmoStats.Count == 0 ? 0 : stats.AtmoStats[stats.AtmoStats.Count - 1].DeltaV;
            double vacDv = stats.VacStats.Count == 0 ? 0 : stats.VacStats[stats.VacStats.Count - 1].DeltaV;

            return string.Format("{0:F0}, {1:F0}", atmDv, vacDv);
        }

        [ValueInfoItem("#MechJeb_StageTimeFullThrottle", InfoItem.Category.Vessel, format = ValueInfoItem.TIME,
            showInEditor = true)] //Stage time (full throttle)
        public float StageTimeLeftFullThrottle()
        {
            MechJebModuleStageStats stats = Core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            if (stats.VacStats.Count == 0 || stats.AtmoStats.Count == 0) return 0;

            float vacTimeLeft = (float)stats.VacStats[stats.VacStats.Count - 1].DeltaTime;
            float atmoTimeLeft = (float)stats.AtmoStats[stats.AtmoStats.Count - 1].DeltaTime;
            float timeLeft = Mathf.Lerp(vacTimeLeft, atmoTimeLeft, Mathf.Clamp01((float)FlightGlobals.getStaticPressure()));

            return timeLeft;
        }

        [ValueInfoItem("#MechJeb_StageTimeCurrentThrottle", InfoItem.Category.Vessel, format = ValueInfoItem.TIME)] //Stage time (current throttle)
        public float StageTimeLeftCurrentThrottle()
        {
            float fullThrottleTime = StageTimeLeftFullThrottle();
            if (fullThrottleTime == 0) return 0;

            return fullThrottleTime / Vessel.ctrlState.mainThrottle;
        }

        [ValueInfoItem("#MechJeb_StageTimeHover", InfoItem.Category.Vessel, format = ValueInfoItem.TIME)] //Stage time (hover)
        public float StageTimeLeftHover()
        {
            float fullThrottleTime = StageTimeLeftFullThrottle();
            if (fullThrottleTime == 0) return 0;

            double hoverThrottle = VesselState.localg / VesselState.maxThrustAccel;
            return fullThrottleTime / (float)hoverThrottle;
        }

        [ValueInfoItem("#MechJeb_TotalDV_vacuum", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)] //Total ΔV (vacuum)
        public double TotalDeltaVVaccum()
        {
            MechJebModuleStageStats stats = Core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();
            return stats.VacStats.Sum(s => s.DeltaV);
        }

        [ValueInfoItem("#MechJeb_TotalDV_atmo", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)] //Total ΔV (atmo)
        public double TotalDeltaVAtmosphere()
        {
            MechJebModuleStageStats stats = Core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();
            return stats.AtmoStats.Sum(s => s.DeltaV);
        }

        [ValueInfoItem("#MechJeb_TotalDV_atmo_vac", InfoItem.Category.Vessel, units = "m/s", showInEditor = true)] //Total ΔV (atmo, vac)
        public string TotalDeltaVAtmosphereAndVac()
        {
            MechJebModuleStageStats stats = Core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            double atmDv = stats.AtmoStats.Sum(s => s.DeltaV);
            double vacDv = stats.VacStats.Sum(s => s.DeltaV);

            return string.Format("{0:F0}, {1:F0}", atmDv, vacDv);
        }

        [GeneralInfoItem("#MechJeb_DockingGuidance_velocity", InfoItem.Category.Target)] //Docking guidance: velocity
        public void DockingGuidanceVelocity()
        {
            if (!Core.Target.NormalTargetExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_velocityNA")); //"Target-relative velocity: (N/A)"
                return;
            }

            Vector3d relVel = Core.Target.RelativeVelocity;
            double relVelX = Vector3d.Dot(relVel, Vessel.GetTransform().right);
            double relVelY = Vector3d.Dot(relVel, Vessel.GetTransform().forward);
            double relVelZ = Vector3d.Dot(relVel, Vessel.GetTransform().up);
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_velocity")); //"Target-relative velocity:"
            GUILayout.Label("X: " + MuUtils.PadPositive(relVelX, "F2") + " m/s  [L/J]");
            GUILayout.Label("Y: " + MuUtils.PadPositive(relVelY, "F2") + " m/s  [I/K]");
            GUILayout.Label("Z: " + MuUtils.PadPositive(relVelZ, "F2") + " m/s  [H/N]");
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_DockingGuidanceAngularVelocity", InfoItem.Category.Target)] //Docking guidance: Angular velocity
        public void DockingGuidanceAngularVelocity()
        {
            if (!(Core.Target.Target is Vessel))
            {
                GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label2")); //"Target-relative Angular velocity: (N/A)"
                return;
            }

            var target = (Vessel)Core.Target.Target;
            Vector3d relw = Quaternion.Inverse(Vessel.ReferenceTransform.rotation) * (target.angularVelocity - Vessel.angularVelocity) *
                            Mathf.Rad2Deg;

            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label3")); //"Target-relative angular velocity:"
            GUILayout.Label("P: " + MuUtils.PadPositive(relw.x, "F2") + " °/s");
            GUILayout.Label("Y: " + MuUtils.PadPositive(relw.z, "F2") + " °/s");
            GUILayout.Label("R: " + MuUtils.PadPositive(relw.y, "F2") + " °/s");
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_DockingGuidancePosition", InfoItem.Category.Target)] //Docking guidance: position
        public void DockingGuidancePosition()
        {
            if (!Core.Target.NormalTargetExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label4")); //"Separation from target: (N/A)"
                return;
            }

            Vector3d sep = Core.Target.RelativePosition;
            double sepX = Vector3d.Dot(sep, Vessel.GetTransform().right);
            double sepY = Vector3d.Dot(sep, Vessel.GetTransform().forward);
            double sepZ = Vector3d.Dot(sep, Vessel.GetTransform().up);
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label5")); //"Separation from target:"
            GUILayout.Label("X: " + MuUtils.PadPositive(sepX, "F2") + " m  [L/J]");
            GUILayout.Label("Y: " + MuUtils.PadPositive(sepY, "F2") + " m  [I/K]");
            GUILayout.Label("Z: " + MuUtils.PadPositive(sepZ, "F2") + " m  [H/N]");
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_AllPlanetPhaseAngles", InfoItem.Category.Orbit)] //All planet phase angles
        public void AllPlanetPhaseAngles()
        {
            Orbit o = Orbit;
            while (o.referenceBody != Planetarium.fetch.Sun) o = o.referenceBody.orbit;

            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label6"), GuiUtils.middleCenterLabel); //"Planet phase angles"

            for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
            {
                CelestialBody body = FlightGlobals.Bodies[i];
                if (body == Planetarium.fetch.Sun)
                {
                    continue;
                }

                if (body.referenceBody != Planetarium.fetch.Sun)
                {
                    continue;
                }

                if (body.orbit == o)
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(body.bodyName, GUILayout.ExpandWidth(true));
                GUILayout.Label(o.PhaseAngle(body.orbit, VesselState.time).ToString("F2") + "º", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_AllMoonPhaseAngles", InfoItem.Category.Orbit)] //All moon phase angles
        public void AllMoonPhaseAngles()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label7"), GuiUtils.middleCenterLabel); //"Moon phase angles"

            if (Orbit.referenceBody != Planetarium.fetch.Sun)
            {
                Orbit o = Orbit;
                while (o.referenceBody.referenceBody != Planetarium.fetch.Sun) o = o.referenceBody.orbit;

                for (int i = 0; i < o.referenceBody.orbitingBodies.Count; i++)
                {
                    CelestialBody body = o.referenceBody.orbitingBodies[i];
                    if (body.orbit == o)
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(body.bodyName, GUILayout.ExpandWidth(true));
                    GUILayout.Label(o.PhaseAngle(body.orbit, VesselState.time).ToString("F2") + "º", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }

        [ValueInfoItem("#MechJeb_SurfaceBiome", InfoItem.Category.Misc, showInEditor = false)] //Surface Biome
        public string CurrentRawBiome()
        {
            if (Vessel.landedAt != string.Empty)
                return Vessel.landedAt;
            return MainBody.GetExperimentBiomeSafe(Vessel.latitude, Vessel.longitude);
        }

        [ValueInfoItem("#MechJeb_CurrentBiome", InfoItem.Category.Misc, showInEditor = false)] //Current Biome
        public string CurrentBiome()
        {
            if (Vessel.landedAt != string.Empty)
                return Vessel.landedAt;
            if (MainBody.BiomeMap == null)
                return "N/A";
            string biome = MainBody.BiomeMap.GetAtt(Vessel.latitude * UtilMath.Deg2Rad, Vessel.longitude * UtilMath.Deg2Rad).displayname;
            if (biome != "")
                biome = "'s " + biome;

            switch (Vessel.situation)
            {
                //ExperimentSituations.SrfLanded
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                    return MainBody.displayName.LocalizeRemoveGender() +
                           (biome == "" ? Localizer.Format("#MechJeb_InfoItems_VesselSituation5") : biome); //"'s surface"
                //ExperimentSituations.SrfSplashed
                case Vessel.Situations.SPLASHED:
                    return MainBody.displayName.LocalizeRemoveGender() +
                           (biome == "" ? Localizer.Format("#MechJeb_InfoItems_VesselSituation6") : biome); //"'s oceans"
                case Vessel.Situations.FLYING:
                    if (Vessel.altitude < MainBody.scienceValues.flyingAltitudeThreshold)
                        //ExperimentSituations.FlyingLow
                        return Localizer.Format("#MechJeb_InfoItems_VesselSituation1",
                            MainBody.displayName.LocalizeRemoveGender() + biome); //"Flying over <<1>>"
                    //ExperimentSituations.FlyingHigh
                    return Localizer.Format("#MechJeb_InfoItems_VesselSituation2",
                        MainBody.displayName.LocalizeRemoveGender() + biome); //"Upper atmosphere of <<1>>"
                default:
                    if (Vessel.altitude < MainBody.scienceValues.spaceAltitudeThreshold)
                        //ExperimentSituations.InSpaceLow
                        return Localizer.Format("#MechJeb_InfoItems_VesselSituation3",
                            MainBody.displayName.LocalizeRemoveGender() + biome); //"Space just above <<1>>"
                    // ExperimentSituations.InSpaceHigh
                    return Localizer.Format("#MechJeb_InfoItems_VesselSituation4",
                        MainBody.displayName.LocalizeRemoveGender() + biome); //"Space high over <<1>>"
            }
        }

        [GeneralInfoItem("#MechJeb_LatLonClipbardCopy", InfoItem.Category.Misc, showInEditor = false)] //Lat/Lon/Alt Copy to Clipboard
        public void LatLonClipbardCopy()
        {
            if (GUILayout.Button(Localizer.Format("#MechJeb_InfoItems_CopytoClipboard"))) //"Copy Lat/Lon/Alt to Clipboard"
            {
                var te = new TextEditor();
                string result = "latitude =  " + VesselState.latitude.ToString("F6") + "\nlongitude = " + VesselState.longitude.ToString("F6") +
                                "\naltitude = " + Vessel.altitude.ToString("F2") + "\n";
                te.text = result;
                te.SelectAll();
                te.Copy();
            }
        }

        [GeneralInfoItem("#MechJeb_PoolsStatus", InfoItem.Category.Misc, showInEditor = true)] //Pools Status
        public void DebugString()
        {
            GUILayout.BeginVertical();
            foreach (KeyValuePair<Type, PoolsStatus> pair in PoolsStatus.poolsInfo)
            {
                Type type = pair.Key;
                //string name = type.ToString();
                if (typeof(IDisposable).IsAssignableFrom(type))
                    type = type.GetGenericArguments()[0];
                StringBuilder name = StringBuilderCache.Acquire();
                name.Append(type.Name);
                Type[] generics = type.GetGenericArguments();
                for (int i = 0; i < generics.Length; i++)
                {
                    if (i == 0) name.Append("<");
                    if (i > 0) name.Append(",");
                    name.Append(type.GetGenericArguments()[i].Name);
                    if (i == generics.Length - 1) name.Append(">");
                }

                GuiUtils.SimpleLabel(name.ToStringAndRelease(), pair.Value.allocated + "/" + pair.Value.maxSize);
            }

            GUILayout.EndHorizontal();
        }

        private static GUIStyle _separatorStyle;

        private static GUIStyle separatorStyle
        {
            get
            {
                if (_separatorStyle == null || _separatorStyle.normal.background == null)
                {
                    var texture = new Texture2D(1, 1);
                    texture.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f));
                    texture.Apply();
                    _separatorStyle = new GUIStyle { normal = { background = texture }, padding = { left = 50 } };
                }

                return _separatorStyle;
            }
        }

        [GeneralInfoItem("#MechJeb_Separator", InfoItem.Category.Misc, showInEditor = true)] //Separator
        public void HorizontalSeparator()
        {
            GUILayout.Label("", separatorStyle, GUILayout.ExpandWidth(true), GUILayout.Height(2));
        }
    }
}
