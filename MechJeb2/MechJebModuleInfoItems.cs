using System;
using System.Collections.Generic;
using UniLinq;
using KSP.UI.Screens;
using Smooth.Pools;
using UnityEngine;
using UnityEngine.Profiling;
using KSP.Localization;

namespace MuMech
{
    //This class just exists to collect miscellaneous info item functions in one place.
    //If any of these functions are useful in another module, they should be moved there.
    public class MechJebModuleInfoItems : ComputerModule
    {
        public MechJebModuleInfoItems(MechJebCore core) : base(core) { }

        //Provides a unified interface for getting the parts list in the editor or in flight:
        List<Part> parts
        {
            get
            {
                return (HighLogic.LoadedSceneIsEditor) ? EditorLogic.fetch.ship.parts :
                    ((vessel == null) ? new List<Part>() : vessel.Parts);
            }
        }

        [ValueInfoItem("#MechJeb_NodeBurnTime", InfoItem.Category.Misc)]//Node burn time
        public string NextManeuverNodeBurnTime()
        {
            if (!vessel.patchedConicsUnlocked() || !vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
            double burnTime = node.GetBurnVector(node.patch).magnitude / vesselState.limitedMaxThrustAccel;
            return GuiUtils.TimeToDHMS(burnTime);
        }

        [ValueInfoItem("#MechJeb_TimeToNode", InfoItem.Category.Misc)]//Time to node
        public string TimeToManeuverNode()
        {
            if (!vessel.patchedConicsUnlocked() || !vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            return GuiUtils.TimeToDHMS(vessel.patchedConicSolver.maneuverNodes[0].UT - vesselState.time);
        }

        [ValueInfoItem("#MechJeb_NodedV", InfoItem.Category.Misc)]//Node dV
        public string NextManeuverNodeDeltaV()
        {
            if (!vessel.patchedConicsUnlocked() || !vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            return MuUtils.ToSI(vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(orbit).magnitude, -1) + "m/s";
        }

        [ValueInfoItem("#MechJeb_SurfaceTWR", InfoItem.Category.Vessel, format = "F2", showInEditor = true)]//Surface TWR
        public double SurfaceTWR()
        {
            if (HighLogic.LoadedSceneIsEditor) return MaxAcceleration() / 9.81;
            else return vesselState.thrustAvailable / (vesselState.mass * mainBody.GeeASL * 9.81);
        }

        [ValueInfoItem("#MechJeb_LocalTWR", InfoItem.Category.Vessel, format = "F2", showInEditor = false)]//Local TWR
        public double LocalTWR()
        {
            return vesselState.thrustAvailable / (vesselState.mass * vesselState.gravityForce.magnitude);
        }

        [ValueInfoItem("#MechJeb_ThrottleTWR", InfoItem.Category.Vessel, format = "F2", showInEditor = false)]//Throttle TWR
        public double ThrottleTWR()
        {
            return vesselState.thrustCurrent / (vesselState.mass * vesselState.gravityForce.magnitude);
        }

        [ValueInfoItem("#MechJeb_AtmosphericPressurePa", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "Pa")]//Atmospheric pressure (Pa)
        public double AtmosphericPressurekPA()
        {
            return FlightGlobals.getStaticPressure(vesselState.CoM) * 1000;
        }

        [ValueInfoItem("#MechJeb_AtmosphericPressure", InfoItem.Category.Misc, format = "F3", units = "atm")]//Atmospheric pressure
        public double AtmosphericPressure()
        {
            return FlightGlobals.getStaticPressure(vesselState.CoM) * PhysicsGlobals.KpaToAtmospheres;
        }

        [ValueInfoItem("#MechJeb_Coordinates", InfoItem.Category.Surface)]//Coordinates
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

        [ValueInfoItem("#MechJeb_MeanAnomaly", InfoItem.Category.Orbit, format = ValueInfoItem.ANGLE)]//Mean Anomaly
        public double MeanAnomaly()
        {
            return orbit.meanAnomaly * UtilMath.Rad2Deg;
        }

        [ValueInfoItem("#MechJeb_Orbit", InfoItem.Category.Orbit)]//Orbit
        public string CurrentOrbitSummary()
        {
            return OrbitSummary(orbit);
        }

        [ValueInfoItem("#MechJeb_TargetOrbit", InfoItem.Category.Target)]//Target orbit
        public string TargetOrbitSummary()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return OrbitSummary(core.target.TargetOrbit);
        }

        [ValueInfoItem("#MechJeb_OrbitWithInc", InfoItem.Category.Orbit, description = "#MechJeb_OrbitWithInc_desc")]//Orbit||Orbit shape w/ inc.
        public string CurrentOrbitSummaryWithInclination()
        {
            return OrbitSummaryWithInclination(orbit);
        }

        [ValueInfoItem("#MechJeb_TargetOrbitWithInc", InfoItem.Category.Target, description = "#MechJeb_TargetOrbitWithInc_desc")]//Target orbit|Target orbit shape w/ inc.
        public string TargetOrbitSummaryWithInclination()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return OrbitSummaryWithInclination(core.target.TargetOrbit);
        }

        [ValueInfoItem("#MechJeb_OrbitalEnergy", InfoItem.Category.Orbit, description = "#MechJeb_OrbitalEnergy_desc", format = ValueInfoItem.SI, units = "J/kg")]//Orbital energy||Specific orbital energy
        public double OrbitalEnergy()
        {
            return orbit.orbitalEnergy;
        }

        [ValueInfoItem("#MechJeb_PotentialEnergy", InfoItem.Category.Orbit, description = "#MechJeb_PotentialEnergy_desc", format = ValueInfoItem.SI, units = "J/kg")]//Potential energy||Specific potential energy
        public double PotentialEnergy()
        {
            return -orbit.referenceBody.gravParameter / orbit.radius;
        }

        [ValueInfoItem("#MechJeb_KineticEnergy", InfoItem.Category.Orbit, description = "#MechJeb_KineticEnergy_desc", format = ValueInfoItem.SI, units = "J/kg")]//Kinetic energy||Specific kinetic energy
        public double KineticEnergy()
        {
            return orbit.orbitalEnergy + orbit.referenceBody.gravParameter / orbit.radius;
        }

        //TODO: consider turning this into a binary search
        [ValueInfoItem("#MechJeb_TimeToImpact", InfoItem.Category.Misc)]//Time to impact
        public string TimeToImpact()
        {
            if (orbit.PeA > 0 || vessel.Landed) return "N/A";

            double impactTime = vesselState.time;
            try
            {
                for (int iter = 0; iter < 10; iter++)
                {
                    Vector3d impactPosition = orbit.SwappedAbsolutePositionAtUT(impactTime);
                    double terrainRadius = mainBody.Radius + mainBody.TerrainAltitude(impactPosition);
                    impactTime = orbit.NextTimeOfRadius(vesselState.time, terrainRadius);
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

            return GuiUtils.TimeToDHMS(impactTime - vesselState.time);
        }

        [ValueInfoItem("#MechJeb_SuicideBurnCountdown", InfoItem.Category.Misc)]//Suicide burn countdown
        public string SuicideBurnCountdown()
        {
            try
            {
                return GuiUtils.TimeToDHMS(OrbitExtensions.SuicideBurnCountdown(orbit, vesselState, vessel),1);
            }
            catch
            {
                return "N/A";
            }
        }

        private MovingAverage rcsThrustAvg = new MovingAverage(10);

        [ValueInfoItem("#MechJeb_RCSthrust", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "N")]//RCS thrust
        public double RCSThrust()
        {
            rcsThrustAvg.value = RCSThrustNow();
            return rcsThrustAvg.value * 1000; // kN to N
        }

        // Returns a value in kN.
        private double RCSThrustNow()
        {
            double rcsThrust = 0;

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
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

        private MovingAverage rcsTranslationEfficiencyAvg = new MovingAverage(10);

        [ValueInfoItem("#MechJeb_RCSTranslationEfficiency", InfoItem.Category.Misc)]//RCS translation efficiency
        public string RCSTranslationEfficiency()
        {
            double totalThrust = RCSThrustNow();
            double effectiveThrust = 0;
            FlightCtrlState s = FlightInputHandler.state;

            // FlightCtrlState and a vessel have different coordinate systems.
            // See MechJebModuleRCSController for a comment explaining this.
            Vector3 direction = new Vector3(-s.X, -s.Z, -s.Y);
            if (totalThrust == 0 || direction.magnitude == 0)
            {
                return "--";
            }

            direction.Normalize();

            for (int index = 0; index < vessel.parts.Count; index++)
            {
                Part p = vessel.parts[index];
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (p.Rigidbody == null || !pm.isEnabled || pm.isJustForShow)
                    {
                        continue;
                    }

                    // Find our distance from the vessel's center of mass, in
                    // world coordinates.
                    Vector3 pos = p.Rigidbody.worldCenterOfMass - vesselState.CoM;

                    // Translate to the vessel's reference frame.
                    pos = Quaternion.Inverse(vessel.GetTransform().rotation) * pos;

                    for (int i = 0; i < pm.thrustForces.Length; i++)
                    {
                        float force = pm.thrustForces[i];
                        Transform t = pm.thrusterTransforms[i];

                        Vector3 thrusterDir = Quaternion.Inverse(vessel.GetTransform().rotation) * -t.up;
                        double thrusterEfficiency = Vector3.Dot(direction, thrusterDir.normalized);

                        effectiveThrust += thrusterEfficiency * pm.thrusterPower * force;
                    }
                }
            }

            rcsTranslationEfficiencyAvg.value = effectiveThrust / totalThrust;
            return (rcsTranslationEfficiencyAvg.value * 100).ToString("F2") + "%";
        }

        [ValueInfoItem("#MechJeb_RCSdV", InfoItem.Category.Vessel, format = "F1", units = "m/s", showInEditor = true)]//RCS ΔV
        public double RCSDeltaVVacuum()
        {
            // Use the average specific impulse of all RCS parts.
            double totalIsp = 0;
            int numThrusters = 0;
            double gForRCS = 9.81;

            double monopropMass = vessel.TotalResourceMass("MonoPropellant");

            foreach (ModuleRCS pm in vessel.GetModules<ModuleRCS>())
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

        [ValueInfoItem("#MechJeb_AngularVelocity", InfoItem.Category.Vessel, showInEditor = false, showInFlight = true)]//Angular Velocity
        public string angularVelocity()
        {
            return MuUtils.PrettyPrint(vesselState.angularVelocityAvg.value.xzy * UtilMath.Rad2Deg, "F3") + "°/s" ;
        }

        [ValueInfoItem("#MechJeb_CurrentAcceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")]//Current acceleration
        public double CurrentAcceleration()
        {
            return CurrentThrust() / (1000 * VesselMass());
        }

        [ValueInfoItem("#MechJeb_CurrentThrust", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "N")]//Current thrust
        public double CurrentThrust()
        {
            return vesselState.thrustCurrent * 1000;
        }

        [ValueInfoItem("#MechJeb_TimeToSoIWwitch", InfoItem.Category.Orbit)]//Time to SoI switch
        public string TimeToSOITransition()
        {
            if (orbit.patchEndTransition == Orbit.PatchTransitionType.FINAL) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.EndUT - vesselState.time);
        }

        [ValueInfoItem("#MechJeb_SurfaceGravity", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s²")]//Surface gravity
        public double SurfaceGravity()
        {
            return mainBody.GeeASL * 9.81;
        }

        [ValueInfoItem("#MechJeb_EscapeVelocity", InfoItem.Category.Orbit, format = ValueInfoItem.SI, siSigFigs = 3, units = "m/s")]//Escape velocity
        public double EscapeVelocity()
        {
            return Math.Sqrt(2 * mainBody.gravParameter / vesselState.radius);
        }

        [ValueInfoItem("#MechJeb_VesselName", InfoItem.Category.Vessel, showInEditor = false)]//Vessel name
        public string VesselName()
        {
            return vessel.vesselName;
        }

        [ValueInfoItem("#MechJeb_VesselType", InfoItem.Category.Vessel, showInEditor = false)]//Vessel type
        public string VesselType()
        {
            return vessel != null ? vessel.vesselType.displayDescription() : "-";
        }

        [ValueInfoItem("#MechJeb_VesselMass", InfoItem.Category.Vessel, format = "F3", units = "t", showInEditor = true)]//Vessel mass
        public double VesselMass()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.fetch.ship.parts.Sum(p => p.mass + p.GetResourceMass());
            else return vesselState.mass;
        }

        [ValueInfoItem("#MechJeb_MaxVesselMass", InfoItem.Category.Vessel, showInEditor = true, showInFlight = false)]//Max vessel mass
        public string MaximumVesselMass()
        {
            SpaceCenterFacility rolloutFacility = (EditorDriver.editorFacility == EditorFacility.VAB) ? SpaceCenterFacility.LaunchPad : SpaceCenterFacility.Runway;
            float maximumVesselMass = GameVariables.Instance.GetCraftMassLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(rolloutFacility), EditorDriver.editorFacility == EditorFacility.VAB);

            if(maximumVesselMass < float.MaxValue)
                return string.Format("{0} t", maximumVesselMass.ToString("F3"));
            else
                return Localizer.Format("#MechJeb_InfoItems_UnlimitedText");//"Unlimited"
        }

        [ValueInfoItem("#MechJeb_DryMass", InfoItem.Category.Vessel, showInEditor = true, format = "F3", units = "t")]//Dry mass
        public double DryMass()
        {
            return parts.Where(p => p.IsPhysicallySignificant()).Sum(p => p.mass + p.GetPhysicslessChildMass());
        }

        [ValueInfoItem("#MechJeb_LiquidFuelandOxidizerMass", InfoItem.Category.Vessel, showInEditor = true, format = "F2", units = "t")]//Liquid fuel & oxidizer mass
        public double LiquidFuelAndOxidizerMass()
        {
            return vessel.TotalResourceMass("LiquidFuel") + vessel.TotalResourceMass("Oxidizer");
        }

        [ValueInfoItem("#MechJeb_MonopropellantMass", InfoItem.Category.Vessel, showInEditor = true, format = "F2", units = "kg")]//Monopropellant mass
        public double MonoPropellantMass()
        {
            return vessel.TotalResourceMass("MonoPropellant");
        }

        [ValueInfoItem("#MechJeb_TotalElectricCharge", InfoItem.Category.Vessel, showInEditor = true, format = ValueInfoItem.SI, siMaxPrecision = 1, units = "Ah")]//Total electric charge
        public double TotalElectricCharge()
        {
            return vessel.TotalResourceAmount(PartResourceLibrary.ElectricityHashcode);
        }

        [ValueInfoItem("#MechJeb_MaxThrust", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "N", showInEditor = true)]//Max thrust
        public double MaxThrust()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                var engines = (from part in EditorLogic.fetch.ship.parts
                               where part.inverseStage == StageManager.LastStage
                               from engine in part.Modules.OfType<ModuleEngines>()
                               select engine);
                return 1000 * engines.Sum(e => e.minThrust + e.thrustPercentage / 100f * (e.maxThrust - e.minThrust));
            }
            else
            {
                return 1000 * vesselState.thrustAvailable;
            }
        }

        [ValueInfoItem("#MechJeb_MinThrust", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "N", showInEditor = true)]//Min thrust
        public double MinThrust()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                var engines = (from part in EditorLogic.fetch.ship.parts
                               where part.inverseStage == StageManager.LastStage
                               from engine in part.Modules.OfType<ModuleEngines>()
                               select engine);
                return 1000 * engines.Sum(e => (e.throttleLocked ? e.minThrust + e.thrustPercentage / 100f * (e.maxThrust - e.minThrust) : e.minThrust));
            }
            else
            {
                return vesselState.thrustMinimum;
            }
        }

        [ValueInfoItem("#MechJeb_MaxAcceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²", showInEditor = true)]//Max acceleration
        public double MaxAcceleration()
        {
            return MaxThrust() / (1000 * VesselMass());
        }

        [ValueInfoItem("#MechJeb_MinAcceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²", showInEditor = true)]//Min acceleration
        public double MinAcceleration()
        {
            return MinThrust() / (1000 * VesselMass());
        }

        [ValueInfoItem("#MechJeb_Gforce", InfoItem.Category.Vessel, format = "F4", units = "g", showInEditor = true)]//G force
        public double Acceleration()
        {
            return (vessel != null) ? vessel.geeForce : 0;
        }

        [ValueInfoItem("#MechJeb_DragCoefficient", InfoItem.Category.Vessel, format = "F3", showInEditor = true)]//Drag coefficient
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
            else
            {
                return vesselState.dragCoef;
            }
        }

        [ValueInfoItem("#MechJeb_PartCount", InfoItem.Category.Vessel, showInEditor = true)]//Part count
        public int PartCount()
        {
            return parts.Count;
        }

        [ValueInfoItem("#MechJeb_MaxPartCount", InfoItem.Category.Vessel, showInEditor = true)]//Max part count
        public string MaxPartCount()
        {
            float editorFacilityLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(EditorDriver.editorFacility.ToFacility());
            int maxPartCount = GameVariables.Instance.GetPartCountLimit(editorFacilityLevel, EditorDriver.editorFacility == EditorFacility.VAB);
            if(maxPartCount < int.MaxValue)
                return maxPartCount.ToString();
            else
                return Localizer.Format("#MechJeb_InfoItems_UnlimitedText");//"Unlimited"
        }

        [ValueInfoItem("#MechJeb_PartCountDivideMaxParts", InfoItem.Category.Vessel, showInEditor = true)]//Part count / Max parts
        public string PartCountAndMaxPartCount()
        {
            return string.Format("{0} / {1}", PartCount().ToString(), MaxPartCount());
        }

        [ValueInfoItem("#MechJeb_StrutCount", InfoItem.Category.Vessel, showInEditor = true)]//Strut count
        public int StrutCount()
        {
            return parts.Count(p => p is CompoundPart && p.Modules.GetModule<CompoundParts.CModuleStrut>());
        }

        [ValueInfoItem("#MechJeb_FuelLinesCount", InfoItem.Category.Vessel, showInEditor = true)]//Fuel Lines count
        public int FuelLinesCount()
        {
            return parts.Count(p => p is CompoundPart && p.Modules.GetModule<CompoundParts.CModuleFuelLine>());
        }

        [ValueInfoItem("#MechJeb_VesselCost", InfoItem.Category.Vessel, showInEditor = true, format = ValueInfoItem.SI, units = "$")]//Vessel cost
        public double VesselCost()
        {
            return parts.Sum(p => p.partInfo.cost) * 1000;
        }

        [ValueInfoItem("#MechJeb_CrewCount", InfoItem.Category.Vessel)]//Crew count
        public int CrewCount()
        {
            return vessel.GetCrewCount();
        }

        [ValueInfoItem("#MechJeb_CrewCapacity", InfoItem.Category.Vessel, showInEditor = true)]//Crew capacity
        public int CrewCapacity()
        {
            return parts.Sum(p => p.CrewCapacity);
        }

        [ValueInfoItem("#MechJeb_DistanceToTarget", InfoItem.Category.Target)]//Distance to target
        public string TargetDistance()
        {
            if (core.target.Target == null) return "N/A";
            return MuUtils.ToSI(core.target.Distance, -1) + "m";
        }

        [ValueInfoItem("#MechJeb_HeadingToTarget", InfoItem.Category.Target)]//Heading to target
        public string HeadingToTarget()
        {
            if (core.target.Target == null) return "N/A";
            return vesselState.HeadingFromDirection(-core.target.RelativePosition).ToString("F1") + "º";
        }

        [ValueInfoItem("#MechJeb_RelativeVelocity", InfoItem.Category.Target)]//Relative velocity
        public string TargetRelativeVelocity()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.RelativeVelocity.magnitude) + "m/s";
        }

        [ValueInfoItem("#MechJeb_TimeToClosestApproach", InfoItem.Category.Target)]//Time to closest approach
        public string TargetTimeToClosestApproach()
        {
            if (core.target.Target != null && vesselState.altitudeTrue < 1000.0) { return GuiUtils.TimeToDHMS(GuiUtils.FromToETA(vessel.CoM, core.target.Transform.position)); }
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return "N/A"; }
            if (vesselState.altitudeTrue < 1000.0) {
                double a = (vessel.mainBody.transform.position - vessel.transform.position).magnitude;
                double b = (vessel.mainBody.transform.position - core.target.Transform.position).magnitude;
                double c = Vector3d.Distance(vessel.transform.position, core.target.Position);
                double ang = Math.Acos(((a * a + b * b) - c * c) / (double)(2f * a * b));
                return GuiUtils.TimeToDHMS(ang * vessel.mainBody.Radius / vesselState.speedSurfaceHorizontal);
            }
            return GuiUtils.TimeToDHMS(orbit.NextClosestApproachTime(core.target.TargetOrbit, vesselState.time) - vesselState.time);
        }

        [ValueInfoItem("#MechJeb_ClosestApproachDistance", InfoItem.Category.Target)]//Closest approach distance
        public string TargetClosestApproachDistance()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (vesselState.altitudeTrue < 1000.0) { return "N/A"; }
            if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return "N/A"; }
            return MuUtils.ToSI(orbit.NextClosestApproachDistance(core.target.TargetOrbit, vesselState.time), -1) + "m";
        }

        [ValueInfoItem("#MechJeb_RelativeVelocityAtClosestApproach", InfoItem.Category.Target)]//Rel. vel. at closest approach
        public string TargetClosestApproachRelativeVelocity()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (vesselState.altitudeTrue < 1000.0) { return "N/A"; }
            if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return "N/A"; }

            try
            {
                double UT = orbit.NextClosestApproachTime(core.target.TargetOrbit, vesselState.time);

                if (double.IsNaN(UT))
                {
                    return "N/A";
                }

                double relVel =
                    (orbit.SwappedOrbitalVelocityAtUT(UT) - core.target.TargetOrbit.SwappedOrbitalVelocityAtUT(UT))
                        .magnitude;
                return MuUtils.ToSI(relVel, -1) + "m/s";
            }
            catch
            {
                return "N/A";
            }
        }

        [ValueInfoItem("#MechJeb_PeriapsisInTargetSoI", InfoItem.Category.Misc)]//Periapsis in target SoI
        public string PeriapsisInTargetSOI()
        {
            if (!core.target.NormalTargetExists) return "N/A";

            Orbit o;
            if (vessel.patchedConicsUnlocked() && vessel.patchedConicSolver.maneuverNodes.Any())
            {
                ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.Last();
                o = node.nextPatch;
            }
            else
            {
                o = vessel.orbit;
            }

            while (o != null && o.referenceBody != (CelestialBody) vessel.targetObject)
                o = o.nextPatch;

            if (o == null) return "N/A";

            return MuUtils.ToSI(o.PeA, -1) + "m";
        }

        [ValueInfoItem("#MechJeb_TargetCaptureDV", InfoItem.Category.Misc)]//ΔV for capture by target
        public string TargetCaptureDV()
        {
            if (!core.target.NormalTargetExists || !(vessel.targetObject is CelestialBody)) return "N/A";

            Orbit o = vessel.orbit;
            while (o != null && o.referenceBody != (CelestialBody) vessel.targetObject)
                o = o.nextPatch;

            if (o == null) return "N/A";

            double smaCapture = (o.PeR + o.referenceBody.sphereOfInfluence) / 2;
            double velAtPeriapsis = Math.Sqrt(o.referenceBody.gravParameter * (2 / o.PeR - 1 / o.semiMajorAxis));
            double velCapture = Math.Sqrt(o.referenceBody.gravParameter * (2 / o.PeR - 1 / smaCapture));

            return MuUtils.ToSI(velAtPeriapsis - velCapture, -1) + "m/s";
        }


        [ValueInfoItem("#MechJeb_TargetApoapsis", InfoItem.Category.Target)]//Target apoapsis
        public string TargetApoapsis()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.TargetOrbit.ApA, 2) + "m";
        }

        [ValueInfoItem("#MechJeb_TargetPeriapsis", InfoItem.Category.Target)]//Target periapsis
        public string TargetPeriapsis()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.TargetOrbit.PeA, 2) + "m";
        }

        [ValueInfoItem("#MechJeb_TargetInclination", InfoItem.Category.Target)]//Target inclination
        public string TargetInclination()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return core.target.TargetOrbit.inclination.ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TargetOrbitPeriod", InfoItem.Category.Target)]//Target orbit period
        public string TargetOrbitPeriod()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.TargetOrbit.period);
        }

        [ValueInfoItem("#MechJeb_TargetOrbitSpeed", InfoItem.Category.Target)]//Target orbit speed
        public string TargetOrbitSpeed()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.TargetOrbit.GetVel().magnitude) + "m/s";
        }

        [ValueInfoItem("#MechJeb_TargetTimeToAp", InfoItem.Category.Target)]//Target time to Ap
        public string TargetOrbitTimeToAp()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.TargetOrbit.timeToAp);
        }

        [ValueInfoItem("#MechJeb_TargetTimeToPe", InfoItem.Category.Target)]//Target time to Pe
        public string TargetOrbitTimeToPe()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.TargetOrbit.timeToPe);
        }

        [ValueInfoItem("#MechJeb_TargetLAN", InfoItem.Category.Target)]//Target LAN
        public string TargetLAN()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return core.target.TargetOrbit.LAN.ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TargetAoP", InfoItem.Category.Target)]//Target AoP
        public string TargetAoP()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return core.target.TargetOrbit.argumentOfPeriapsis.ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TargetEccentricity", InfoItem.Category.Target)]//Target eccentricity
        public string TargetEccentricity()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.TargetOrbit.eccentricity);
        }

        [ValueInfoItem("#MechJeb_TargetSMA", InfoItem.Category.Target)]//Target SMA
        public string TargetSMA()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.TargetOrbit.semiMajorAxis, 2) + "m";
        }

        [ValueInfoItem("#MechJeb_AtmosphericDrag", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")]//Atmospheric drag
        public double AtmosphericDrag()
        {
            return vesselState.drag;
        }

        [ValueInfoItem("#MechJeb_SynodicPeriod", InfoItem.Category.Target)]//Synodic period
        public string SynodicPeriod()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            return GuiUtils.TimeToDHMS(orbit.SynodicPeriod(core.target.TargetOrbit));
        }

        [ValueInfoItem("#MechJeb_PhaseAngleToTarget", InfoItem.Category.Target)]//Phase angle to target
        public string PhaseAngle()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return "N/A"; }

            return orbit.PhaseAngle(core.target.TargetOrbit, vesselState.time).ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TargetPlanetPhaseAngle", InfoItem.Category.Target)]//Target planet phase angle
        public string TargetPlanetPhaseAngle()
        {
            if (!(core.target.Target is CelestialBody)) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody.referenceBody) return "N/A";

            return mainBody.orbit.PhaseAngle(core.target.TargetOrbit, vesselState.time).ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_RelativeInclination", InfoItem.Category.Target)]//Relative inclination
        public string RelativeInclinationToTarget()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";

            return orbit.RelativeInclination(core.target.TargetOrbit).ToString("F2") + "º";
        }

        [ValueInfoItem("#MechJeb_TimeToAN", InfoItem.Category.Target)]//Time to AN
        public string TimeToAscendingNodeWithTarget()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (!orbit.AscendingNodeExists(core.target.TargetOrbit)) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.TimeOfAscendingNode(core.target.TargetOrbit, vesselState.time) - vesselState.time);
        }

        [ValueInfoItem("#MechJeb_TimeToDN", InfoItem.Category.Target)]//Time to DN
        public string TimeToDescendingNodeWithTarget()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (!orbit.DescendingNodeExists(core.target.TargetOrbit)) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.TimeOfDescendingNode(core.target.TargetOrbit, vesselState.time) - vesselState.time);
        }

        [ValueInfoItem("#MechJeb_TimeToEquatorialAN", InfoItem.Category.Orbit)]//Time to equatorial AN
        public string TimeToEquatorialAscendingNode()
        {
            if (!orbit.AscendingNodeEquatorialExists()) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.TimeOfAscendingNodeEquatorial(vesselState.time) - vesselState.time);
        }

        [ValueInfoItem("#MechJeb_TimeToEquatorialDN", InfoItem.Category.Orbit)]//Time to equatorial DN
        public string TimeToEquatorialDescendingNode()
        {
            if (!orbit.DescendingNodeEquatorialExists()) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.TimeOfDescendingNodeEquatorial(vesselState.time) - vesselState.time);
        }

        [ValueInfoItem("#MechJeb_CircularOrbitSpeed", InfoItem.Category.Orbit, format = ValueInfoItem.SI, units = "m/s")]//Circular orbit speed
        public double CircularOrbitSpeed()
        {
            return OrbitalManeuverCalculator.CircularOrbitSpeed(mainBody, vesselState.radius);
        }

        [Persistent(pass = (int)Pass.Global)]
        public bool showStagedMass = false;
        [Persistent(pass = (int)Pass.Global)]
        public bool showBurnedMass = false;
        [Persistent(pass = (int)Pass.Global)]
        public bool showInitialMass = false;
        [Persistent(pass = (int)Pass.Global)]
        public bool showFinalMass = false;
        [Persistent(pass = (int)Pass.Global)]
        public bool showVacInitialTWR = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showAtmoInitialTWR = false; // NK
        [Persistent(pass = (int)Pass.Global)]
        public bool showAtmoMaxTWR = false;
        [Persistent(pass = (int)Pass.Global)]
        public bool showVacMaxTWR = false;
        [Persistent(pass = (int)Pass.Global)]
        public bool showVacDeltaV = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showTime = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showAtmoDeltaV = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool showISP = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool liveSLT = true;
        [Persistent(pass = (int)Pass.Global)]
        public float altSLTScale = 0;
        [Persistent(pass = (int)Pass.Global)]
        public float machScale = 0;
        [Persistent(pass = (int)Pass.Global)]
        public int TWRBody = 1;
        [Persistent(pass = (int)Pass.Global)]
        public int StageDisplayState = 0;
        [Persistent(pass = (int)Pass.Global)]
        public bool showEmpty = true;
        [Persistent(pass = (int)Pass.Global)]
        public bool timeSeconds = false;


        private static readonly string[] StageDisplayStates = {Localizer.Format("#MechJeb_InfoItems_button1"), Localizer.Format("#MechJeb_InfoItems_button2"), Localizer.Format("#MechJeb_InfoItems_button3"), Localizer.Format("#MechJeb_InfoItems_button4") };//"Short stats""Long stats""Full stats""Custom"

        private FuelFlowSimulation.Stats[] vacStats;
        private FuelFlowSimulation.Stats[] atmoStats;
        private string[] bodies;

        [GeneralInfoItem("#MechJeb_StageStatsAll", InfoItem.Category.Vessel, showInEditor = true)]//Stage stats (all)
        public void AllStageStats()
        {
            Profiler.BeginSample("AllStageStats.init");
            // Unity throws an exception if we change our layout between the Layout event and
            // the Repaint event, so only get new data right before the Layout event.
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            if (Event.current.type == EventType.Layout)
            {
                stats.RequestUpdate(this);
            }

            vacStats = stats.vacStats;
            atmoStats = stats.atmoStats;

            Profiler.EndSample();

            Profiler.BeginSample("AllStageStats.UI1");

            int numStages = atmoStats.Length;
            var stages = Enumerable.Range(0, numStages).Where(s => showEmpty || atmoStats[s].deltaV > 0).ToArray();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label1"), GUILayout.ExpandWidth(true));//"Stage stats"

            if (GUILayout.Button(timeSeconds ? "s" : "dhms", GUILayout.ExpandWidth(false)))
            {
                timeSeconds = !timeSeconds;
            }

            if (GUILayout.Button(showEmpty ?  Localizer.Format("#MechJeb_InfoItems_showEmpty") :Localizer.Format("#MechJeb_InfoItems_hideEmpty"), GUILayout.ExpandWidth(false)))
            {
                showEmpty = !showEmpty;
            }

            if (GUILayout.Button(StageDisplayStates[StageDisplayState], GUILayout.ExpandWidth(false)))
            {
                StageDisplayState = (StageDisplayState + 1) % StageDisplayStates.Length;
            }

            if (!HighLogic.LoadedSceneIsEditor)
            {
                if (GUILayout.Button(liveSLT ?  Localizer.Format("#MechJeb_InfoItems_button5") :Localizer.Format("#MechJeb_InfoItems_button6"), GUILayout.ExpandWidth(false)))//"Live SLT" "0Alt SLT"
                {
                    liveSLT = !liveSLT;
                }
                stats.liveSLT = liveSLT;
            }
            GUILayout.EndHorizontal();

            double geeASL;
            if (HighLogic.LoadedSceneIsEditor)
            {
                GUILayout.BeginHorizontal();
                if (bodies == null)
                    bodies = FlightGlobals.Bodies.ConvertAll(b => b.GetName()).ToArray();

                // We're in the VAB/SPH
                TWRBody = GuiUtils.ComboBox.Box(TWRBody, bodies, this, false);
                stats.editorBody = FlightGlobals.Bodies[TWRBody];
                geeASL = FlightGlobals.Bodies[TWRBody].GeeASL;

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                altSLTScale = GUILayout.HorizontalSlider(altSLTScale, 0, 1, GUILayout.ExpandWidth(true));
                stats.altSLT = Math.Pow(altSLTScale, 2) * stats.editorBody.atmosphereDepth;
                GUILayout.Label(MuUtils.ToSI(stats.altSLT, 2) + "m", GUILayout.Width(80));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                machScale = GUILayout.HorizontalSlider(machScale, 0, 1, GUILayout.ExpandWidth(true));
                stats.mach = Math.Pow(machScale * 2, 3);
                GUILayout.Label(stats.mach.ToString("F1") + " M", GUILayout.Width(80));
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
            else
            {
                // We're in flight
                stats.editorBody = mainBody;
                geeASL = mainBody.GeeASL;
            }

            switch (StageDisplayState)
            {
                case 0:
                    showVacInitialTWR = showAtmoInitialTWR = showVacDeltaV = showAtmoDeltaV = showTime = true;
                    showInitialMass = showFinalMass = showStagedMass = showBurnedMass = showVacMaxTWR = showAtmoMaxTWR = showISP = false;
                    break;
                case 1:
                    showInitialMass = showFinalMass = showVacInitialTWR = showAtmoInitialTWR = showVacMaxTWR = showAtmoMaxTWR = showVacDeltaV = showTime = showAtmoDeltaV = true;
                    showStagedMass = showBurnedMass = showISP = false;
                    break;
                case 2:
                    showInitialMass = showFinalMass = showStagedMass = showBurnedMass = showVacInitialTWR = showAtmoInitialTWR = showAtmoMaxTWR = showVacMaxTWR = showVacDeltaV = showTime = showAtmoDeltaV = showISP = true;
                    break;
            }

            Profiler.EndSample();

            Profiler.BeginSample("AllStageStats.UI2");

            GUILayout.BeginHorizontal();
            DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn0"), stages.Select(s => s.ToString()));

            Profiler.EndSample();

            Profiler.BeginSample("AllStageStats.UI3");

            bool noChange = true;
            if (showInitialMass) noChange &= showInitialMass = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn1"), stages.Select(s => atmoStats[s].startMass.ToString("F3") + " t"));//"Start Mass"

            Profiler.EndSample();

            Profiler.BeginSample("AllStageStats.UI4");

            if (showFinalMass) noChange &= showFinalMass = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn2"), stages.Select(s => atmoStats[s].endMass.ToString("F3") + " t"));//"End mass"
            if (showStagedMass) noChange &= showStagedMass = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn3"), stages.Select(s => atmoStats[s].stagedMass.ToString("F3") + " t"));//"Staged Mass"
            if (showBurnedMass) noChange &= showBurnedMass = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn4"), stages.Select(s => atmoStats[s].resourceMass.ToString("F3") + " t"));//"Burned Mass"
            if (showVacInitialTWR) noChange &= showVacInitialTWR = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn5"), stages.Select(s => vacStats[s].StartTWR(geeASL).ToString("F2")));//"TWR"
            if (showVacMaxTWR) noChange &= showVacMaxTWR = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn6"), stages.Select(s => vacStats[s].MaxTWR(geeASL).ToString("F2")));//"Max TWR"
            if (showAtmoInitialTWR) noChange &= showAtmoInitialTWR = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn7"), stages.Select(s => atmoStats[s].StartTWR(geeASL).ToString("F2")));//"SLT"
            if (showAtmoMaxTWR) noChange &= showAtmoMaxTWR = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn8"), stages.Select(s => atmoStats[s].MaxTWR(geeASL).ToString("F2")));//"Max SLT"
            if (showISP) noChange &= showISP = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn9"), stages.Select(s => atmoStats[s].isp.ToString("F2")));//"ISP"
            if (showAtmoDeltaV) noChange &= showAtmoDeltaV = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn10"), stages.Select(s => atmoStats[s].deltaV.ToString("F0") + " m/s"));//"Atmo ΔV"
            if (showVacDeltaV) noChange &= showVacDeltaV = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn11"), stages.Select(s => vacStats[s].deltaV.ToString("F0") + " m/s"));//"Vac ΔV"
            if (showTime) noChange &= showTime = !DrawStageStatsColumn(Localizer.Format("#MechJeb_InfoItems_StatsColumn12"), stages.Select(s => timeSeconds ? MuUtils.ToSI(atmoStats[s].deltaTime, 2) + " s": GuiUtils.TimeToDHMS(atmoStats[s].deltaTime, 1)));//"Time"

            if (!noChange)
                StageDisplayState = 3;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        static GUIStyle _columnStyle;
        public static GUIStyle ColumnStyle
        {
            get
            {
                if (_columnStyle == null)
                {
                    _columnStyle = new GUIStyle(GuiUtils.yellowOnHover)
                    {
                        alignment = TextAnchor.MiddleRight,
                        wordWrap = false,
                        padding = new RectOffset(2, 2, 0, 0)
                    };
                }
                return _columnStyle;
            }
        }

        bool DrawStageStatsColumn(string header, IEnumerable<string> data)
        {
            GUILayout.BeginVertical();
            bool ret = GUILayout.Button(header + "   ", ColumnStyle);

            foreach (string datum in data) GUILayout.Label(datum + "   ", ColumnStyle);

            GUILayout.EndVertical();

            return ret;
        }

        /*[ActionInfoItem("Update stage stats", InfoItem.Category.Vessel, showInEditor = true)]
        public void UpdateStageStats()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();

            stats.RequestUpdate(this);
        }*/

        [ValueInfoItem("#MechJeb_StageDv_vac", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]//Stage ΔV (vac)
        public double StageDeltaVVacuum()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);

            if (stats.vacStats.Length == 0) return 0;

            return stats.vacStats[stats.vacStats.Length - 1].deltaV;
        }

        [ValueInfoItem("#MechJeb_StageDV_atmo", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]//Stage ΔV (atmo)
        public double StageDeltaVAtmosphere()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);

            if (stats.atmoStats.Length == 0) return 0;

            return stats.atmoStats[stats.atmoStats.Length - 1].deltaV;
        }

        [ValueInfoItem("#MechJeb_StageDV_atmo_vac", InfoItem.Category.Vessel, units = "m/s", showInEditor = true)]//Stage ΔV (atmo, vac)
        public string StageDeltaVAtmosphereAndVac()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);

            double atmDv = (stats.atmoStats.Length == 0) ? 0 : stats.atmoStats[stats.atmoStats.Length - 1].deltaV;
            double vacDv = (stats.vacStats.Length == 0) ? 0 : stats.vacStats[stats.vacStats.Length - 1].deltaV;

            return String.Format("{0:F0}, {1:F0}", atmDv, vacDv);
        }

        [ValueInfoItem("#MechJeb_StageTimeFullThrottle", InfoItem.Category.Vessel, format = ValueInfoItem.TIME, showInEditor = true)]//Stage time (full throttle)
        public float StageTimeLeftFullThrottle()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);

            if (stats.vacStats.Length == 0 || stats.atmoStats.Length == 0) return 0;

            float vacTimeLeft = (float)stats.vacStats[stats.vacStats.Length - 1].deltaTime;
            float atmoTimeLeft = (float)stats.atmoStats[stats.atmoStats.Length - 1].deltaTime;
            float timeLeft = Mathf.Lerp(vacTimeLeft, atmoTimeLeft, Mathf.Clamp01((float)FlightGlobals.getStaticPressure()));

            return timeLeft;
        }

        [ValueInfoItem("#MechJeb_StageTimeCurrentThrottle", InfoItem.Category.Vessel, format = ValueInfoItem.TIME)]//Stage time (current throttle)
        public float StageTimeLeftCurrentThrottle()
        {
            float fullThrottleTime = StageTimeLeftFullThrottle();
            if (fullThrottleTime == 0) return 0;

            return fullThrottleTime / vessel.ctrlState.mainThrottle;
        }

        [ValueInfoItem("#MechJeb_StageTimeHover", InfoItem.Category.Vessel, format = ValueInfoItem.TIME)]//Stage time (hover)
        public float StageTimeLeftHover()
        {
            float fullThrottleTime = StageTimeLeftFullThrottle();
            if (fullThrottleTime == 0) return 0;

            double hoverThrottle = vesselState.localg / vesselState.maxThrustAccel;
            return fullThrottleTime / (float)hoverThrottle;
        }

        [ValueInfoItem("#MechJeb_TotalDV_vacuum", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]//Total ΔV (vacuum)
        public double TotalDeltaVVaccum()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);
            return stats.vacStats.Sum(s => s.deltaV);
        }

        [ValueInfoItem("#MechJeb_TotalDV_atmo", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]//Total ΔV (atmo)
        public double TotalDeltaVAtmosphere()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);
            return stats.atmoStats.Sum(s => s.deltaV);
        }

        [ValueInfoItem("#MechJeb_TotalDV_atmo_vac", InfoItem.Category.Vessel, units = "m/s", showInEditor = true)]//Total ΔV (atmo, vac)
        public string TotalDeltaVAtmosphereAndVac()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);

            double atmDv = stats.atmoStats.Sum(s => s.deltaV);
            double vacDv = stats.vacStats.Sum(s => s.deltaV);

            return String.Format("{0:F0}, {1:F0}", atmDv, vacDv);
        }

        [GeneralInfoItem("#MechJeb_DockingGuidance_velocity", InfoItem.Category.Target)]//Docking guidance: velocity
        public void DockingGuidanceVelocity()
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_velocityNA"));//"Target-relative velocity: (N/A)"
                return;
            }

            Vector3d relVel = core.target.RelativeVelocity;
            double relVelX = Vector3d.Dot(relVel, vessel.GetTransform().right);
            double relVelY = Vector3d.Dot(relVel, vessel.GetTransform().forward);
            double relVelZ = Vector3d.Dot(relVel, vessel.GetTransform().up);
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_velocity"));//"Target-relative velocity:"
            GUILayout.Label("X: " + MuUtils.PadPositive(relVelX, "F2") + " m/s  [L/J]");
            GUILayout.Label("Y: " + MuUtils.PadPositive(relVelY, "F2") + " m/s  [I/K]");
            GUILayout.Label("Z: " + MuUtils.PadPositive(relVelZ, "F2") + " m/s  [H/N]");
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_DockingGuidanceAngularVelocity", InfoItem.Category.Target)]//Docking guidance: Angular velocity
        public void DockingGuidanceAngularVelocity()
        {
            if (!(core.target.Target is Vessel))
            {
                GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label2"));//"Target-relative Angular velocity: (N/A)"
                return;
            }

            Vessel target = (Vessel)core.target.Target;
            Vector3d relw = Quaternion.Inverse(vessel.ReferenceTransform.rotation) * (target.angularVelocity - vessel.angularVelocity) * Mathf.Rad2Deg;

            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label3"));//"Target-relative angular velocity:"
            GUILayout.Label("P: " + MuUtils.PadPositive(relw.x, "F2") + " °/s");
            GUILayout.Label("Y: " + MuUtils.PadPositive(relw.z, "F2") + " °/s");
            GUILayout.Label("R: " + MuUtils.PadPositive(relw.y, "F2") + " °/s");
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_DockingGuidancePosition", InfoItem.Category.Target)]//Docking guidance: position
        public void DockingGuidancePosition()
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label4"));//"Separation from target: (N/A)"
                return;
            }

            Vector3d sep = core.target.RelativePosition;
            double sepX = Vector3d.Dot(sep, vessel.GetTransform().right);
            double sepY = Vector3d.Dot(sep, vessel.GetTransform().forward);
            double sepZ = Vector3d.Dot(sep, vessel.GetTransform().up);
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label5"));//"Separation from target:"
            GUILayout.Label("X: " + MuUtils.PadPositive(sepX, "F2") + " m  [L/J]");
            GUILayout.Label("Y: " + MuUtils.PadPositive(sepY, "F2") + " m  [I/K]");
            GUILayout.Label("Z: " + MuUtils.PadPositive(sepZ, "F2") + " m  [H/N]");
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_AllPlanetPhaseAngles", InfoItem.Category.Orbit)]//All planet phase angles
        public void AllPlanetPhaseAngles()
        {
            Orbit o = orbit;
            while (o.referenceBody != Planetarium.fetch.Sun) o = o.referenceBody.orbit;

            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label6"), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });//"Planet phase angles"

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
                GUILayout.Label(o.PhaseAngle(body.orbit, vesselState.time).ToString("F2") + "º", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_AllMoonPhaseAngles", InfoItem.Category.Orbit)]//All moon phase angles
        public void AllMoonPhaseAngles()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_InfoItems_label7"), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });//"Moon phase angles"

            if (orbit.referenceBody != Planetarium.fetch.Sun)
            {
                Orbit o = orbit;
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
                    GUILayout.Label(o.PhaseAngle(body.orbit, vesselState.time).ToString("F2") + "º", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }


        [ValueInfoItem("#MechJeb_SurfaceBiome", InfoItem.Category.Misc, showInEditor = false)]//Surface Biome
        public string CurrentRawBiome()
        {
            if (vessel.landedAt != string.Empty)
                return vessel.landedAt;
            return mainBody.GetExperimentBiomeSafe(vessel.latitude, vessel.longitude);
        }

        [ValueInfoItem("#MechJeb_CurrentBiome", InfoItem.Category.Misc, showInEditor=false)]//Current Biome
        public string CurrentBiome()
        {
            if (vessel.landedAt != string.Empty)
                return vessel.landedAt;
            if (mainBody.BiomeMap == null)
                return "N/A";
            string biome = mainBody.BiomeMap.GetAtt (vessel.latitude * UtilMath.Deg2Rad, vessel.longitude * UtilMath.Deg2Rad).name;
            if (biome != "")
                biome = "'s " + biome;

            switch (vessel.situation)
            {
                //ExperimentSituations.SrfLanded
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                    return mainBody.displayName + (biome == "" ? Localizer.Format("#MechJeb_InfoItems_VesselSituation5") : biome);//"'s surface"
                //ExperimentSituations.SrfSplashed
                case Vessel.Situations.SPLASHED:
                    return mainBody.displayName + (biome == "" ? Localizer.Format("#MechJeb_InfoItems_VesselSituation6") : biome);//"'s oceans"
                case Vessel.Situations.FLYING:
                    if (vessel.altitude < mainBody.scienceValues.flyingAltitudeThreshold)
                        //ExperimentSituations.FlyingLow
                        return Localizer.Format("#MechJeb_InfoItems_VesselSituation1", mainBody.displayName + biome);//"Flying over <<1>>"
                    else
                        //ExperimentSituations.FlyingHigh
                        return Localizer.Format("#MechJeb_InfoItems_VesselSituation2", mainBody.displayName + biome);//"Upper atmosphere of <<1>>"
                default:
                    if (vessel.altitude < mainBody.scienceValues.spaceAltitudeThreshold)
                        //ExperimentSituations.InSpaceLow
                        return Localizer.Format("#MechJeb_InfoItems_VesselSituation3", mainBody.displayName + biome);//"Space just above <<1>>"
                    else
                        // ExperimentSituations.InSpaceHigh
                        return Localizer.Format("#MechJeb_InfoItems_VesselSituation4", mainBody.displayName + biome);//"Space high over <<1>>"
            }
        }

        [GeneralInfoItem("#MechJeb_LatLonClipbardCopy", InfoItem.Category.Misc, showInEditor = false)]//Lat/Lon/Alt Copy to Clipboard
        public void LatLonClipbardCopy()
        {
            if (GUILayout.Button(Localizer.Format("#MechJeb_InfoItems_CopytoClipboard")))//"Copy Lat/Lon/Alt to Clipboard"
            {
                TextEditor te = new TextEditor();
                string result = "latitude =  " + vesselState.latitude.ToString("F6") + "\nlongitude = " + vesselState.longitude.ToString("F6") +
                                "\naltitude = " + vessel.altitude.ToString("F2") + "\n";
                te.text = result;
                te.SelectAll();
                te.Copy();
            }
        }

        [GeneralInfoItem("#MechJeb_PoolsStatus", InfoItem.Category.Misc, showInEditor = true)]//Pools Status
        public void DebugString()
        {
            GUILayout.BeginVertical();
            foreach (var pair in PoolsStatus.poolsInfo)
            {
                Type type = pair.Key;
                //string name = type.ToString();
                if (typeof(IDisposable).IsAssignableFrom(type))
                    type = type.GetGenericArguments()[0];
                string name = type.Name;
                var generics = type.GetGenericArguments();
                for (int i = 0; i < generics.Length; i++)
                {
                    if (i == 0) name += "<";
                    if (i > 0) name += ",";
                    name += type.GetGenericArguments()[i].Name;
                    if (i == generics.Length - 1) name += ">";
                }
                GuiUtils.SimpleLabel(name, pair.Value.allocated + "/" + pair.Value.maxSize);
            }
            GUILayout.EndHorizontal();
        }

        static GUIStyle _separatorStyle;
        static GUIStyle separatorStyle
        {
            get
            {
                if (_separatorStyle == null || _separatorStyle.normal.background == null)
                {
                    Texture2D texture = new Texture2D(1, 1);
                    texture.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f));
                    texture.Apply();
                    _separatorStyle = new GUIStyle
                    {
                        normal = {background = texture},
                        padding = {left = 50}
                    };
                }
                return _separatorStyle;
            }
        }

        [GeneralInfoItem("#MechJeb_Separator", InfoItem.Category.Misc, showInEditor = true)]//Separator
        public void HorizontalSeparator()
        {
            GUILayout.Label("", separatorStyle, GUILayout.ExpandWidth(true), GUILayout.Height(2));
        }
    }
}
