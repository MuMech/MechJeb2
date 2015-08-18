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

        //Provides a unified interface for getting the parts list in the editor or in flight:
        List<Part> parts
        {
            get
            {
                return (HighLogic.LoadedSceneIsEditor) ? EditorLogic.fetch.ship.parts :
                    ((vessel == null) ? new List<Part>() : vessel.Parts);
            }
        }


        [ValueInfoItem("Node burn time", InfoItem.Category.Misc)]
        public string NextManeuverNodeBurnTime()
        {
            if (!vessel.patchedConicsUnlocked() || !vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
            double burnTime = node.GetBurnVector(node.patch).magnitude / vesselState.limitedMaxThrustAccel;
            return GuiUtils.TimeToDHMS(burnTime);
        }

        [ValueInfoItem("Time to node", InfoItem.Category.Misc)]
        public string TimeToManeuverNode()
        {
            if (!vessel.patchedConicsUnlocked() || !vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            return GuiUtils.TimeToDHMS(vessel.patchedConicSolver.maneuverNodes[0].UT - vesselState.time);
        }

        [ValueInfoItem("Node dV", InfoItem.Category.Misc)]
        public string NextManeuverNodeDeltaV()
        {
            if (!vessel.patchedConicsUnlocked() || !vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";

            return MuUtils.ToSI(vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(orbit).magnitude, -1) + "m/s";
        }

        [ValueInfoItem("Surface TWR", InfoItem.Category.Vessel, format = "F2", showInEditor = true)]
        public double SurfaceTWR()
        {
            if (HighLogic.LoadedSceneIsEditor) return MaxAcceleration() / 9.81;
            else return vesselState.thrustAvailable / (vesselState.mass * mainBody.GeeASL * 9.81);
        }

        [ValueInfoItem("Local TWR", InfoItem.Category.Vessel, format = "F2", showInEditor = false)]
        public double LocalTWR()
        {
            return vesselState.thrustAvailable / (vesselState.mass * vesselState.gravityForce.magnitude);
        }

        [ValueInfoItem("Throttle TWR", InfoItem.Category.Vessel, format = "F2", showInEditor = false)]
        public double ThrottleTWR()
        {
            return vesselState.thrustCurrent / (vesselState.mass * vesselState.gravityForce.magnitude);
        }

        [ValueInfoItem("Atmospheric pressure (Pa)", InfoItem.Category.Misc, format = "F3", units = "Pa")]
        public double AtmosphericPressurekPA()
        {
            return FlightGlobals.getStaticPressure(vesselState.CoM) * 1000;
        }

        [ValueInfoItem("Atmospheric pressure", InfoItem.Category.Misc, format = "F3", units = "atm")]
        public double AtmosphericPressure()
        {
            return FlightGlobals.getStaticPressure(vesselState.CoM) * PhysicsGlobals.KpaToAtmospheres;
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
            return OrbitSummary(core.target.TargetOrbit);
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
            return OrbitSummaryWithInclination(core.target.TargetOrbit);
        }

        //TODO: consider turning this into a binary search
        [ValueInfoItem("Time to impact", InfoItem.Category.Misc)]
        public string TimeToImpact()
        {
            if (orbit.PeA > 0) return "N/A";

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


            return GuiUtils.TimeToDHMS(impactTime - vesselState.time);
        }

        [ValueInfoItem("Suicide burn countdown", InfoItem.Category.Misc)]
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

        [ValueInfoItem("RCS thrust", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "N")]
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

                    for (int j = 0; j < pm.thrustForces.Count; j++)
                    {
                        rcsThrust += pm.thrustForces[j] * pm.thrusterPower;
                    }
                }
            }

            return rcsThrust;
        }

        private MovingAverage rcsTranslationEfficiencyAvg = new MovingAverage(10);

        [ValueInfoItem("RCS translation efficiency", InfoItem.Category.Misc)]
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

                    for (int i = 0; i < pm.thrustForces.Count; i++)
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

        [ValueInfoItem("RCS ΔV", InfoItem.Category.Vessel, format = "F1", units = "m/s", showInEditor = true)]
        public double RCSDeltaVVacuum()
        {
            // Use the average specific impulse of all RCS parts.
            double totalIsp = 0;
            int numThrusters = 0;
            float gForRCS;

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
            return isp * 9.81 * Math.Log(m0 / m1);
        }

        [ValueInfoItem("Current acceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")]
        public double CurrentAcceleration()
        {
            return CurrentThrust() / (1000 * VesselMass());
        }

        [ValueInfoItem("Current thrust", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "N")]
        public double CurrentThrust()
        {
            return vesselState.thrustCurrent * 1000;
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

        [ValueInfoItem("Escape velocity", InfoItem.Category.Orbit, format = ValueInfoItem.SI, siSigFigs = 3, units = "m/s")]
        public double EscapeVelocity()
        {
            return Math.Sqrt(2 * mainBody.gravParameter / vesselState.radius);
        }

        [ValueInfoItem("Vessel mass", InfoItem.Category.Vessel, format = "F3", units = "t", showInEditor = true)]
        public double VesselMass()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.fetch.ship.parts.Sum(p => p.mass + p.GetResourceMass());
            else return vesselState.mass;
        }

        [ValueInfoItem("Max vessel mass", InfoItem.Category.Vessel, showInEditor = true, showInFlight = false)]
        public string MaximumVesselMass()
        {
            SpaceCenterFacility rolloutFacility = (EditorDriver.editorFacility == EditorFacility.VAB) ? SpaceCenterFacility.LaunchPad : SpaceCenterFacility.Runway;
            float maximumVesselMass = GameVariables.Instance.GetCraftMassLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(rolloutFacility));

            if(maximumVesselMass < float.MaxValue)
                return string.Format("{0} t", maximumVesselMass.ToString("F3"));
            else
                return "Unlimited";
        }

        [ValueInfoItem("Dry mass", InfoItem.Category.Vessel, showInEditor = true, format = "F3", units = "t")]
        public double DryMass()
        {
            return parts.Where(p => p.IsPhysicallySignificant()).Sum(p => p.mass + p.GetPhysicslessChildMass());
        }

        [ValueInfoItem("Liquid fuel & oxidizer mass", InfoItem.Category.Vessel, showInEditor = true, format = "F2", units = "t")]
        public double LiquidFuelAndOxidizerMass()
        {
            return vessel.TotalResourceMass("LiquidFuel") + vessel.TotalResourceMass("Oxidizer");
        }

        [ValueInfoItem("Monopropellant mass", InfoItem.Category.Vessel, showInEditor = true, format = "F2", units = "kg")]
        public double MonoPropellantMass()
        {
            return vessel.TotalResourceMass("MonoPropellant");
        }

        [ValueInfoItem("Total electric charge", InfoItem.Category.Vessel, showInEditor = true, format = ValueInfoItem.SI, siMaxPrecision = 1, units = "Ah")]
        public double TotalElectricCharge()
        {
            return vessel.TotalResourceAmount("ElectricCharge");
        }



        [ValueInfoItem("Max thrust", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "N", showInEditor = true)]
        public double MaxThrust()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                var engines = (from part in EditorLogic.fetch.ship.parts
                               where part.inverseStage == Staging.lastStage
                               from engine in part.Modules.OfType<ModuleEngines>()
                               select engine);
                return 1000 * engines.Sum(e => e.minThrust + e.thrustPercentage / 100f * (e.maxThrust - e.minThrust));
            }
            else
            {
                return 1000 * vesselState.thrustAvailable;
            }
        }

        [ValueInfoItem("Min thrust", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "N", showInEditor = true)]
        public double MinThrust()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                var engines = (from part in EditorLogic.fetch.ship.parts
                               where part.inverseStage == Staging.lastStage
                               from engine in part.Modules.OfType<ModuleEngines>()
                               select engine);
                return 1000 * engines.Sum(e => (e.throttleLocked ? e.minThrust + e.thrustPercentage / 100f * (e.maxThrust - e.minThrust) : e.minThrust));
            }
            else
            {
                return vesselState.thrustMinimum;
            }
        }

        [ValueInfoItem("Max acceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²", showInEditor = true)]
        public double MaxAcceleration()
        {
            return MaxThrust() / (1000 * VesselMass());
        }

        [ValueInfoItem("Min acceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²", showInEditor = true)]
        public double MinAcceleration()
        {
            return MinThrust() / (1000 * VesselMass());
        }

        [ValueInfoItem("G force", InfoItem.Category.Vessel, format = "F4", units = "g", showInEditor = true)]
        public double Acceleration()
        {
            return (vessel != null) ? vessel.geeForce : 0;
        }

        [ValueInfoItem("Drag coefficient", InfoItem.Category.Vessel, format = "F3", showInEditor = true)]
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
#warning Check that ....
                return vesselState.dragCoef;
            }
        }

        [ValueInfoItem("Part count", InfoItem.Category.Vessel, showInEditor = true)]
        public int PartCount()
        {
            return parts.Count;
        }

        [ValueInfoItem("Max part count", InfoItem.Category.Vessel, showInEditor = true)]
        public string MaxPartCount()
        {
            float editorFacilityLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(EditorEnumExtensions.ToFacility(EditorDriver.editorFacility));
            int maxPartCount = GameVariables.Instance.GetPartCountLimit(editorFacilityLevel);
            if(maxPartCount < int.MaxValue)
                return maxPartCount.ToString();
            else
                return "Unlimited";
        }

        [ValueInfoItem("Part count / Max parts", InfoItem.Category.Vessel, showInEditor = true)]
        public string PartCountAndMaxPartCount()
        {
            return string.Format("{0} / {1}", PartCount().ToString(), MaxPartCount());
        }

        [ValueInfoItem("Strut count", InfoItem.Category.Vessel, showInEditor = true)]
        public int StrutCount()
        {
            return parts.Count(p => p is StrutConnector);
        }

        [ValueInfoItem("Vessel cost", InfoItem.Category.Vessel, showInEditor = true, format = ValueInfoItem.SI, units = "$")]
        public double VesselCost()
        {
            return parts.Sum(p => p.partInfo.cost) * 1000;
        }

        [ValueInfoItem("Crew count", InfoItem.Category.Vessel)]
        public int CrewCount()
        {
            return vessel.GetCrewCount();
        }

        [ValueInfoItem("Crew capacity", InfoItem.Category.Vessel, showInEditor = true)]
        public int CrewCapacity()
        {
            return parts.Sum(p => p.CrewCapacity);
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
            return vesselState.HeadingFromDirection(-core.target.RelativePosition).ToString("F1") + "º";
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

        [ValueInfoItem("Closest approach distance", InfoItem.Category.Target)]
        public string TargetClosestApproachDistance()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (vesselState.altitudeTrue < 1000.0) { return "N/A"; }
            if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return "N/A"; }
            return MuUtils.ToSI(orbit.NextClosestApproachDistance(core.target.TargetOrbit, vesselState.time), -1) + "m";
        }

        [ValueInfoItem("Rel. vel. at closest approach", InfoItem.Category.Target)]
        public string TargetClosestApproachRelativeVelocity()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (vesselState.altitudeTrue < 1000.0) { return "N/A"; }
            if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return "N/A"; }

            double UT = orbit.NextClosestApproachTime(core.target.TargetOrbit, vesselState.time);

            if (double.IsNaN(UT)) { return "N/A"; }

            double relVel = (orbit.SwappedOrbitalVelocityAtUT(UT) - core.target.TargetOrbit.SwappedOrbitalVelocityAtUT(UT)).magnitude;
            return MuUtils.ToSI(relVel, -1) + "m/s";
        }

        [ValueInfoItem("Target apoapsis", InfoItem.Category.Target)]
        public string TargetApoapsis()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.TargetOrbit.ApA, 2) + "m";
        }

        [ValueInfoItem("Target periapsis", InfoItem.Category.Target)]
        public string TargetPeriapsis()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.TargetOrbit.PeA, 2) + "m";
        }

        [ValueInfoItem("Target inclination", InfoItem.Category.Target)]
        public string TargetInclination()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return core.target.TargetOrbit.inclination.ToString("F2") + "º";
        }

        [ValueInfoItem("Target orbit period", InfoItem.Category.Target)]
        public string TargetOrbitPeriod()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.TargetOrbit.period);
        }

        [ValueInfoItem("Target orbit speed", InfoItem.Category.Target)]
        public string TargetOrbitSpeed()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.TargetOrbit.GetVel().magnitude) + "m/s";
        }

        [ValueInfoItem("Target time to Ap", InfoItem.Category.Target)]
        public string TargetOrbitTimeToAp()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.TargetOrbit.timeToAp);
        }

        [ValueInfoItem("Target time to Pe", InfoItem.Category.Target)]
        public string TargetOrbitTimeToPe()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.TargetOrbit.timeToPe);
        }

        [ValueInfoItem("Target LAN", InfoItem.Category.Target)]
        public string TargetLAN()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return core.target.TargetOrbit.LAN.ToString("F2") + "º";
        }

        [ValueInfoItem("Target AoP", InfoItem.Category.Target)]
        public string TargetAoP()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return core.target.TargetOrbit.argumentOfPeriapsis.ToString("F2") + "º";
        }

        [ValueInfoItem("Target eccentricity", InfoItem.Category.Target)]
        public string TargetEccentricity()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return GuiUtils.TimeToDHMS(core.target.TargetOrbit.eccentricity);
        }

        [ValueInfoItem("Target SMA", InfoItem.Category.Target)]
        public string TargetSMA()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            return MuUtils.ToSI(core.target.TargetOrbit.semiMajorAxis, 2) + "m";
        }

        [ValueInfoItem("Atmospheric drag", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")]
        public double AtmosphericDrag()
        {
            return vesselState.drag;
        }

        [ValueInfoItem("Synodic period", InfoItem.Category.Target)]
        public string SynodicPeriod()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            return GuiUtils.TimeToDHMS(orbit.SynodicPeriod(core.target.TargetOrbit));
        }

        [ValueInfoItem("Phase angle to target", InfoItem.Category.Target)]
        public string PhaseAngle()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return "N/A"; }

            return orbit.PhaseAngle(core.target.TargetOrbit, vesselState.time).ToString("F2") + "º";
        }

        [ValueInfoItem("Target planet phase angle", InfoItem.Category.Target)]
        public string TargetPlanetPhaseAngle()
        {
            if (!(core.target.Target is CelestialBody)) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody.referenceBody) return "N/A";

            return mainBody.orbit.PhaseAngle(core.target.TargetOrbit, vesselState.time).ToString("F2") + "º";
        }

        [ValueInfoItem("Relative inclination", InfoItem.Category.Target)]
        public string RelativeInclinationToTarget()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";

            return orbit.RelativeInclination(core.target.TargetOrbit).ToString("F2") + "º";
        }

        [ValueInfoItem("Time to AN", InfoItem.Category.Target)]
        public string TimeToAscendingNodeWithTarget()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (!orbit.AscendingNodeExists(core.target.TargetOrbit)) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.TimeOfAscendingNode(core.target.TargetOrbit, vesselState.time) - vesselState.time);
        }

        [ValueInfoItem("Time to DN", InfoItem.Category.Target)]
        public string TimeToDescendingNodeWithTarget()
        {
            if (!core.target.NormalTargetExists) return "N/A";
            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody) return "N/A";
            if (!orbit.DescendingNodeExists(core.target.TargetOrbit)) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.TimeOfDescendingNode(core.target.TargetOrbit, vesselState.time) - vesselState.time);
        }

        [ValueInfoItem("Time to equatorial AN", InfoItem.Category.Orbit)]
        public string TimeToEquatorialAscendingNode()
        {
            if (!orbit.AscendingNodeEquatorialExists()) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.TimeOfAscendingNodeEquatorial(vesselState.time) - vesselState.time);
        }

        [ValueInfoItem("Time to equatorial DN", InfoItem.Category.Orbit)]
        public string TimeToEquatorialDescendingNode()
        {
            if (!orbit.DescendingNodeEquatorialExists()) return "N/A";

            return GuiUtils.TimeToDHMS(orbit.TimeOfDescendingNodeEquatorial(vesselState.time) - vesselState.time);
        }

        [ValueInfoItem("Circular orbit speed", InfoItem.Category.Orbit, format = ValueInfoItem.SI, units = "m/s")]
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
        public int TWRBody = 1;
        [Persistent(pass = (int)Pass.Global)]
        public int StageDisplayState = 0;

        private static readonly string[] StageDisplayStates = {"Short stats", "Long stats", "Full stats", "Custom"};

        private FuelFlowSimulation.Stats[] vacStats;
        private FuelFlowSimulation.Stats[] atmoStats;

        [GeneralInfoItem("Stage stats (all)", InfoItem.Category.Vessel, showInEditor = true)]
        public void AllStageStats()
        {
            // Unity throws an exception if we change our layout between the Layout event and
            // the Repaint event, so only get new data right before the Layout event.
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            if (Event.current.type == EventType.Layout)
            {
                vacStats = stats.vacStats;
                atmoStats = stats.atmoStats;
                stats.RequestUpdate(this);
            }

            int numStages = atmoStats.Length;
            var stages = Enumerable.Range(0, numStages);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Stage stats", GUILayout.ExpandWidth(true));

            double geeASL;
            if (HighLogic.LoadedSceneIsEditor)
            {
                // We're in the VAB/SPH
                TWRBody = GuiUtils.ComboBox.Box(TWRBody, FlightGlobals.Bodies.ConvertAll(b => b.GetName()).ToArray(), this);
                stats.editorBody = FlightGlobals.Bodies[TWRBody];
                geeASL = FlightGlobals.Bodies[TWRBody].GeeASL;
            }
            else
            {
                // We're in flight
                stats.editorBody = mainBody;
                geeASL = mainBody.GeeASL;
            }

            if (GUILayout.Button(StageDisplayStates[StageDisplayState], GUILayout.ExpandWidth(false)))
            {
                StageDisplayState = (StageDisplayState + 1) % StageDisplayStates.Length;
            }

            if (!HighLogic.LoadedSceneIsEditor)
            {
                if (GUILayout.Button(liveSLT ? "Live SLT" : "0Alt SLT", GUILayout.ExpandWidth(false)))
                {
                    liveSLT = !liveSLT;
                }
                stats.liveSLT = liveSLT;
            }

            GUILayout.EndHorizontal();

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

            GUILayout.BeginHorizontal();
            DrawStageStatsColumn("Stage", stages.Select(s => s.ToString()));
            bool noChange = true;
            if (showInitialMass) noChange &= showInitialMass = !DrawStageStatsColumn("Start Mass", stages.Select(s => atmoStats[s].startMass.ToString("F3") + " t"));
            if (showFinalMass) noChange &= showFinalMass = !DrawStageStatsColumn("End mass", stages.Select(s => atmoStats[s].endMass.ToString("F3") + " t"));
            if (showStagedMass) noChange &= showStagedMass = !DrawStageStatsColumn("Staged Mass", stages.Select(s => atmoStats[s].stagedMass.ToString("F3") + " t"));
            if (showBurnedMass) noChange &= showBurnedMass = !DrawStageStatsColumn("Burned Mass", stages.Select(s => atmoStats[s].resourceMass.ToString("F3") + " t"));
            if (showVacInitialTWR) noChange &= showVacInitialTWR = !DrawStageStatsColumn("TWR", stages.Select(s => vacStats[s].StartTWR(geeASL).ToString("F2")));
            if (showVacMaxTWR) noChange &= showVacMaxTWR = !DrawStageStatsColumn("Max TWR", stages.Select(s => vacStats[s].MaxTWR(geeASL).ToString("F2")));
            if (showAtmoInitialTWR) noChange &= showAtmoInitialTWR = !DrawStageStatsColumn("SLT", stages.Select(s => atmoStats[s].StartTWR(geeASL).ToString("F2")));
            if (showAtmoMaxTWR) noChange &= showAtmoMaxTWR = !DrawStageStatsColumn("Max SLT", stages.Select(s => atmoStats[s].MaxTWR(geeASL).ToString("F2")));
            if (showISP) noChange &= showISP = !DrawStageStatsColumn("ISP", stages.Select(s => atmoStats[s].isp.ToString("F2")));
            if (showAtmoDeltaV) noChange &= showAtmoDeltaV = !DrawStageStatsColumn("Atmo ΔV", stages.Select(s => atmoStats[s].deltaV.ToString("F0") + " m/s"));
            if (showVacDeltaV) noChange &= showVacDeltaV = !DrawStageStatsColumn("Vac ΔV", stages.Select(s => vacStats[s].deltaV.ToString("F0") + " m/s"));
            if (showTime) noChange &= showTime = !DrawStageStatsColumn("Time", stages.Select(s => GuiUtils.TimeToDHMS(atmoStats[s].deltaTime)));

            if (!noChange)
                StageDisplayState = 3;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        bool DrawStageStatsColumn(string header, IEnumerable<string> data)
        {
            GUILayout.BeginVertical();
            GUIStyle s = new GUIStyle(GuiUtils.yellowOnHover) { alignment = TextAnchor.MiddleRight, wordWrap = false, padding = new RectOffset(2, 2, 0, 0) };
            bool ret = GUILayout.Button(header + "   ", s);

            foreach (string datum in data) GUILayout.Label(datum + "   ", s);

            GUILayout.EndVertical();

            return ret;
        }

        /*[ActionInfoItem("Update stage stats", InfoItem.Category.Vessel, showInEditor = true)]
        public void UpdateStageStats()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();

            stats.RequestUpdate(this);
        }*/

        [ValueInfoItem("Stage ΔV (vac)", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]
        public double StageDeltaVVacuum()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);

            if (stats.vacStats.Length == 0) return 0;

            return stats.vacStats[stats.vacStats.Length - 1].deltaV;
        }

        [ValueInfoItem("Stage ΔV (atmo)", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]
        public double StageDeltaVAtmosphere()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);

            if (stats.atmoStats.Length == 0) return 0;

            return stats.atmoStats[stats.atmoStats.Length - 1].deltaV;
        }

        [ValueInfoItem("Stage ΔV (atmo, vac)", InfoItem.Category.Vessel, units = "m/s", showInEditor = true)]
        public string StageDeltaVAtmosphereAndVac()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);

            double atmDv = (stats.atmoStats.Length == 0) ? 0 : stats.atmoStats[stats.atmoStats.Length - 1].deltaV;
            double vacDv = (stats.vacStats.Length == 0) ? 0 : stats.vacStats[stats.vacStats.Length - 1].deltaV;

            return String.Format("{0:F0}, {1:F0}", atmDv, vacDv);
        }

        [ValueInfoItem("Stage time (full throttle)", InfoItem.Category.Vessel, format = ValueInfoItem.TIME, showInEditor = true)]
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

        [ValueInfoItem("Stage time (current throttle)", InfoItem.Category.Vessel, format = ValueInfoItem.TIME)]
        public float StageTimeLeftCurrentThrottle()
        {
            float fullThrottleTime = StageTimeLeftFullThrottle();
            if (fullThrottleTime == 0) return 0;

            return fullThrottleTime / vessel.ctrlState.mainThrottle;
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
        public double TotalDeltaVVaccum()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);
            return stats.vacStats.Sum(s => s.deltaV);
        }

        [ValueInfoItem("Total ΔV (atmo)", InfoItem.Category.Vessel, format = "F0", units = "m/s", showInEditor = true)]
        public double TotalDeltaVAtmosphere()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);
            return stats.atmoStats.Sum(s => s.deltaV);
        }

        [ValueInfoItem("Total ΔV (atmo, vac)", InfoItem.Category.Vessel, units = "m/s", showInEditor = true)]
        public string TotalDeltaVAtmosphereAndVac()
        {
            MechJebModuleStageStats stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);

            double atmDv = stats.atmoStats.Sum(s => s.deltaV);
            double vacDv = stats.vacStats.Sum(s => s.deltaV);

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


        [GeneralInfoItem("All planet phase angles", InfoItem.Category.Orbit)]
        public void AllPlanetPhaseAngles()
        {
            Orbit o = orbit;
            while (o.referenceBody != Planetarium.fetch.Sun) o = o.referenceBody.orbit;

            GUILayout.BeginVertical();
            GUILayout.Label("Planet phase angles", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });

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

        [GeneralInfoItem("All moon phase angles", InfoItem.Category.Orbit)]
        public void AllMoonPhaseAngles()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Moon phase angles", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });

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


        [ValueInfoItem("Surface Biome", InfoItem.Category.Misc, showInEditor = false)]
        public string CurrentRawBiome()
        {
            if (vessel.landedAt != string.Empty)
                return vessel.landedAt;
            string biome = ScienceUtil.GetExperimentBiome(mainBody, vessel.latitude, vessel.longitude);
            return "" + biome;
        }

        [ValueInfoItem("Current Biome", InfoItem.Category.Misc, showInEditor=false)]
        public string CurrentBiome()
        {
            if (vessel.landedAt != string.Empty)
                return vessel.landedAt;
            if (mainBody.BiomeMap == null)
                return "N/A";
            string biome = mainBody.BiomeMap.GetAtt (vessel.latitude * Math.PI / 180d, vessel.longitude * Math.PI / 180d).name;
            if (biome != "")
                biome = "'s " + biome;

            switch (vessel.situation)
            {
                //ExperimentSituations.SrfLanded
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                    return mainBody.theName + (biome == "" ? "'s surface" : biome);
                //ExperimentSituations.SrfSplashed
                case Vessel.Situations.SPLASHED:
                    return mainBody.theName + (biome == "" ? "'s oceans" : biome);
                case Vessel.Situations.FLYING:
                    if (vessel.altitude < mainBody.scienceValues.flyingAltitudeThreshold)
                        //ExperimentSituations.FlyingLow
                        return "Flying over " + mainBody.theName + biome;
                    else
                        //ExperimentSituations.FlyingHigh
                        return "Upper atmosphere of " + mainBody.theName + biome;
                default:
                    if (vessel.altitude < mainBody.scienceValues.spaceAltitudeThreshold)
                        //ExperimentSituations.InSpaceLow
                        return "Space just above " + mainBody.theName + biome;
                    else
                        // ExperimentSituations.InSpaceHigh
                        return "Space high over " + mainBody.theName;
            }
        }

        [GeneralInfoItem("Lat/Lon/Alt Copy to Clipboard", InfoItem.Category.Misc, showInEditor = false)]
        public void LatLonClipbardCopy()
        {
            if (GUILayout.Button("Copy Lat/Lon/Alt to Clipboard"))
            {
                TextEditor te = new TextEditor();
                string result = "latitude =  " + vesselState.latitude.ToString("F6") + "\nlongitude = " + vesselState.longitude.ToString("F6") +
                                "\naltitude = " + vessel.altitude.ToString("F2") + "\n";
                te.content = new GUIContent(result);
                te.SelectAll();
                te.Copy();
            }
        }


        static GUIStyle _separatorStyle;
        static GUIStyle separatorStyle
        {
            get
            {
                if (_separatorStyle == null)
                {
                    Texture2D texture = new Texture2D(1, 1);
                    texture.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f));
                    texture.Apply();
                    _separatorStyle = new GUIStyle();
                    _separatorStyle.normal.background = texture;
                    _separatorStyle.padding.left = 50;
                }
                return _separatorStyle;
            }
        }

        [GeneralInfoItem("Separator", InfoItem.Category.Misc)]
        public void HorizontalSeparator()
        {
            GUILayout.Label("", separatorStyle, GUILayout.ExpandWidth(true), GUILayout.Height(2));
        }
    }
}
