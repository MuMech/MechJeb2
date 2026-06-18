/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:INFOITEMS - read-only access to MechJebModuleInfoItems's ValueInfoItems.
    //
    // These are the same single-value readouts MechJeb shows in its custom info windows. Numeric
    // items are exposed as scalars and the rest (formatted/summary text) as strings. The module's
    // other public surface (stage-stat state, and the GeneralInfoItems that render GUILayout) is
    // intentionally not exposed.
    [KOSNomenclature("MechJebInfoItems")]
    public class InfoItemsBinding : ComputerModuleBinding<MechJebModuleInfoItems>
    {
        public InfoItemsBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            AddSuffix("NEXTMANEUVERNODEBURNTIME", new Suffix<StringValue>(() => Module.NextManeuverNodeBurnTime(),
                "Node burn time."));
            AddSuffix("TIMETOMANEUVERNODE", new Suffix<StringValue>(() => Module.TimeToManeuverNode(),
                "Time to node."));
            AddSuffix("NEXTMANEUVERNODEDELTAV", new Suffix<StringValue>(() => Module.NextManeuverNodeDeltaV(),
                "Node dV."));
            AddSuffix("SURFACETWR", new Suffix<ScalarValue>(() => Module.SurfaceTWR(),
                "Surface TWR."));
            AddSuffix("LOCALTWR", new Suffix<ScalarValue>(() => Module.LocalTWR(),
                "Local TWR."));
            AddSuffix("THROTTLETWR", new Suffix<ScalarValue>(() => Module.ThrottleTWR(),
                "Throttle TWR."));
            AddSuffix("ATMOSPHERICPRESSUREKPA", new Suffix<ScalarValue>(() => Module.AtmosphericPressurekPa(),
                "Atmospheric pressure (Pa)"));
            AddSuffix("ATMOSPHERICPRESSURE", new Suffix<ScalarValue>(() => Module.AtmosphericPressure(),
                "Atmospheric pressure."));
            AddSuffix("GETCOORDINATESTRING", new Suffix<StringValue>(() => Module.GetCoordinateString(),
                "Coordinates."));
            AddSuffix("MEANANOMALY", new Suffix<ScalarValue>(() => Module.MeanAnomaly(),
                "Mean Anomaly."));
            AddSuffix("CURRENTORBITSUMMARY", new Suffix<StringValue>(() => Module.CurrentOrbitSummary(),
                "Orbit."));
            AddSuffix("TARGETORBITSUMMARY", new Suffix<StringValue>(() => Module.TargetOrbitSummary(),
                "Target orbit."));
            AddSuffix("CURRENTORBITSUMMARYWITHINCLINATION", new Suffix<StringValue>(() => Module.CurrentOrbitSummaryWithInclination(),
                "Orbit||Orbit shape w/ inc."));
            AddSuffix("TARGETORBITSUMMARYWITHINCLINATION", new Suffix<StringValue>(() => Module.TargetOrbitSummaryWithInclination(),
                "Target orbit|Target orbit shape w/ inc."));
            AddSuffix("ORBITALENERGY", new Suffix<ScalarValue>(() => Module.OrbitalEnergy(),
                "Orbital energy||Specific orbital energy."));
            AddSuffix("POTENTIALENERGY", new Suffix<ScalarValue>(() => Module.PotentialEnergy(),
                "Potential energy||Specific potential energy."));
            AddSuffix("KINETICENERGY", new Suffix<ScalarValue>(() => Module.KineticEnergy(),
                "Kinetic energy||Specific kinetic energy."));
            AddSuffix("RCSTHRUST", new Suffix<ScalarValue>(() => Module.RCSThrust(),
                "RCS thrust."));
            AddSuffix("RCSTRANSLATIONEFFICIENCY", new Suffix<StringValue>(() => Module.RCSTranslationEfficiency(),
                "RCS translation efficiency."));
            AddSuffix("RCSDELTAVVACUUM", new Suffix<ScalarValue>(() => Module.RCSDeltaVVacuum(),
                "RCS ΔV."));
            AddSuffix("ANGULARVELOCITY", new Suffix<StringValue>(() => Module.AngularVelocity(),
                "Angular Velocity."));
            AddSuffix("CURRENTACCELERATION", new Suffix<ScalarValue>(() => Module.CurrentAcceleration(),
                "Current acceleration."));
            AddSuffix("CURRENTTHRUST", new Suffix<ScalarValue>(() => Module.CurrentThrust(),
                "Current thrust."));
            AddSuffix("TIMETOSOITRANSITION", new Suffix<StringValue>(() => Module.TimeToSOITransition(),
                "Time to SoI switch."));
            AddSuffix("SURFACEGRAVITY", new Suffix<ScalarValue>(() => Module.SurfaceGravity(),
                "Surface gravity."));
            AddSuffix("ESCAPEVELOCITY", new Suffix<ScalarValue>(() => Module.EscapeVelocity(),
                "Escape velocity."));
            AddSuffix("VESSELNAME", new Suffix<StringValue>(() => Module.VesselName(),
                "Vessel name."));
            AddSuffix("VESSELTYPE", new Suffix<StringValue>(() => Module.VesselType(),
                "Vessel type."));
            AddSuffix("VESSELMASS", new Suffix<ScalarValue>(() => Module.VesselMass(),
                "Vessel mass."));
            AddSuffix("MAXIMUMVESSELMASS", new Suffix<StringValue>(() => Module.MaximumVesselMass(),
                "Max vessel mass."));
            AddSuffix("DRYMASS", new Suffix<ScalarValue>(() => Module.DryMass(),
                "Dry mass."));
            AddSuffix("LIQUIDFUELANDOXIDIZERMASS", new Suffix<ScalarValue>(() => Module.LiquidFuelAndOxidizerMass(),
                "Liquid fuel & oxidizer mass."));
            AddSuffix("MONOPROPELLANTMASS", new Suffix<ScalarValue>(() => Module.MonoPropellantMass(),
                "Monopropellant mass."));
            AddSuffix("TOTALELECTRICCHARGE", new Suffix<ScalarValue>(() => Module.TotalElectricCharge(),
                "Total electric charge."));
            AddSuffix("MAXTHRUST", new Suffix<ScalarValue>(() => Module.MaxThrust(),
                "Max thrust."));
            AddSuffix("MINTHRUST", new Suffix<ScalarValue>(() => Module.MinThrust(),
                "Min thrust."));
            AddSuffix("MAXACCELERATION", new Suffix<ScalarValue>(() => Module.MaxAcceleration(),
                "Max acceleration."));
            AddSuffix("MINACCELERATION", new Suffix<ScalarValue>(() => Module.MinAcceleration(),
                "Min acceleration."));
            AddSuffix("ACCELERATION", new Suffix<ScalarValue>(() => Module.Acceleration(),
                "G force."));
            AddSuffix("PARTCOUNT", new Suffix<ScalarValue>(() => Module.PartCount(),
                "Part count."));
            AddSuffix("MAXPARTCOUNT", new Suffix<StringValue>(() => Module.MaxPartCount(),
                "Max part count."));
            AddSuffix("PARTCOUNTANDMAXPARTCOUNT", new Suffix<StringValue>(() => Module.PartCountAndMaxPartCount(),
                "Part count / Max parts."));
            AddSuffix("STRUTCOUNT", new Suffix<ScalarValue>(() => Module.StrutCount(),
                "Strut count."));
            AddSuffix("FUELLINESCOUNT", new Suffix<ScalarValue>(() => Module.FuelLinesCount(),
                "Fuel Lines count."));
            AddSuffix("VESSELCOST", new Suffix<ScalarValue>(() => Module.VesselCost(),
                "Vessel cost."));
            AddSuffix("CREWCOUNT", new Suffix<ScalarValue>(() => Module.CrewCount(),
                "Crew count."));
            AddSuffix("CREWCAPACITY", new Suffix<ScalarValue>(() => Module.CrewCapacity(),
                "Crew capacity."));
            AddSuffix("TARGETDISTANCE", new Suffix<StringValue>(() => Module.TargetDistance(),
                "Distance to target."));
            AddSuffix("HEADINGTOTARGET", new Suffix<StringValue>(() => Module.HeadingToTarget(),
                "Heading to target."));
            AddSuffix("TARGETRELATIVEVELOCITY", new Suffix<StringValue>(() => Module.TargetRelativeVelocity(),
                "Relative velocity."));
            AddSuffix("TARGETTIMETOCLOSESTAPPROACH", new Suffix<StringValue>(() => Module.TargetTimeToClosestApproach(),
                "Time to closest approach."));
            AddSuffix("TARGETCLOSESTAPPROACHDISTANCE", new Suffix<StringValue>(() => Module.TargetClosestApproachDistance(),
                "Closest approach distance."));
            AddSuffix("TARGETCLOSESTAPPROACHRELATIVEVELOCITY", new Suffix<StringValue>(() => Module.TargetClosestApproachRelativeVelocity(),
                "Rel. vel. at closest approach."));
            AddSuffix("PERIAPSISINTARGETSOI", new Suffix<StringValue>(() => Module.PeriapsisInTargetSOI(),
                "Periapsis in target SoI."));
            AddSuffix("TARGETCAPTUREDV", new Suffix<StringValue>(() => Module.TargetCaptureDV(),
                "ΔV for capture by target."));
            AddSuffix("TARGETAPOAPSIS", new Suffix<StringValue>(() => Module.TargetApoapsis(),
                "Target apoapsis."));
            AddSuffix("TARGETPERIAPSIS", new Suffix<StringValue>(() => Module.TargetPeriapsis(),
                "Target periapsis."));
            AddSuffix("TARGETINCLINATION", new Suffix<StringValue>(() => Module.TargetInclination(),
                "Target inclination."));
            AddSuffix("TARGETORBITPERIOD", new Suffix<StringValue>(() => Module.TargetOrbitPeriod(),
                "Target orbit period."));
            AddSuffix("TARGETORBITSPEED", new Suffix<StringValue>(() => Module.TargetOrbitSpeed(),
                "Target orbit speed."));
            AddSuffix("TARGETORBITTIMETOAP", new Suffix<StringValue>(() => Module.TargetOrbitTimeToAp(),
                "Target time to Ap."));
            AddSuffix("TARGETORBITTIMETOPE", new Suffix<StringValue>(() => Module.TargetOrbitTimeToPe(),
                "Target time to Pe."));
            AddSuffix("TARGETLAN", new Suffix<StringValue>(() => Module.TargetLAN(),
                "Target LAN."));
            AddSuffix("TARGETLDN", new Suffix<StringValue>(() => Module.TargetLDN(),
                "Target LDN."));
            AddSuffix("TARGETTIMETOASCENDINGNODE", new Suffix<StringValue>(() => Module.TargetTimeToAscendingNode(),
                "Target Time to AN."));
            AddSuffix("TARGETTIMETODESCENDINGNODE", new Suffix<StringValue>(() => Module.TargetTimeToDescendingNode(),
                "Target Time to DN."));
            AddSuffix("TARGETAOP", new Suffix<StringValue>(() => Module.TargetAoP(),
                "Target AoP."));
            AddSuffix("TARGETECCENTRICITY", new Suffix<StringValue>(() => Module.TargetEccentricity(),
                "Target eccentricity."));
            AddSuffix("TARGETSMA", new Suffix<StringValue>(() => Module.TargetSMA(),
                "Target SMA."));
            AddSuffix("TARGETMEANANOMALY", new Suffix<StringValue>(() => Module.TargetMeanAnomaly(),
                "Target Mean Anomaly."));
            AddSuffix("TARGETTRUELONGITUDE", new Suffix<StringValue>(() => Module.TargetTrueLongitude(),
                "Target Mean Anomaly."));
            AddSuffix("SYNODICPERIOD", new Suffix<StringValue>(() => Module.SynodicPeriod(),
                "Synodic period."));
            AddSuffix("PHASEANGLE", new Suffix<StringValue>(() => Module.PhaseAngle(),
                "Phase angle to target."));
            AddSuffix("TARGETPLANETPHASEANGLE", new Suffix<StringValue>(() => Module.TargetPlanetPhaseAngle(),
                "Target planet phase angle."));
            AddSuffix("RELATIVEINCLINATIONTOTARGET", new Suffix<StringValue>(() => Module.RelativeInclinationToTarget(),
                "Relative inclination."));
            AddSuffix("TIMETOASCENDINGNODEWITHTARGET", new Suffix<StringValue>(() => Module.TimeToAscendingNodeWithTarget(),
                "Time to AN."));
            AddSuffix("TIMETODESCENDINGNODEWITHTARGET", new Suffix<StringValue>(() => Module.TimeToDescendingNodeWithTarget(),
                "Time to DN."));
            AddSuffix("TIMETOEQUATORIALASCENDINGNODE", new Suffix<StringValue>(() => Module.TimeToEquatorialAscendingNode(),
                "Time to equatorial AN."));
            AddSuffix("TIMETOEQUATORIALDESCENDINGNODE", new Suffix<StringValue>(() => Module.TimeToEquatorialDescendingNode(),
                "Time to equatorial DN."));
            AddSuffix("CIRCULARORBITSPEED", new Suffix<ScalarValue>(() => Module.CircularOrbitSpeed(),
                "Circular orbit speed."));
            AddSuffix("STAGEDELTAVVACUUM", new Suffix<ScalarValue>(() => Module.StageDeltaVVacuum(),
                "Stage ΔV (vac)"));
            AddSuffix("STAGEDELTAVATMOSPHERE", new Suffix<ScalarValue>(() => Module.StageDeltaVAtmosphere(),
                "Stage ΔV (atmo)"));
            AddSuffix("STAGEDELTAVATMOSPHEREANDVAC", new Suffix<StringValue>(() => Module.StageDeltaVAtmosphereAndVac(),
                "Stage ΔV (atmo, vac)"));
            AddSuffix("STAGETIMELEFTFULLTHROTTLE", new Suffix<ScalarValue>(() => Module.StageTimeLeftFullThrottle(),
                "Stage time (full throttle)"));
            AddSuffix("STAGETIMELEFTCURRENTTHROTTLE", new Suffix<ScalarValue>(() => Module.StageTimeLeftCurrentThrottle(),
                "Stage time (current throttle)"));
            AddSuffix("STAGETIMELEFTHOVER", new Suffix<ScalarValue>(() => Module.StageTimeLeftHover(),
                "Stage time (hover)"));
            AddSuffix("TOTALDELTAVVACUUM", new Suffix<ScalarValue>(() => Module.TotalDeltaVVacuum(),
                "Total ΔV (vacuum)"));
            AddSuffix("TOTALDELTAVATMOSPHERE", new Suffix<ScalarValue>(() => Module.TotalDeltaVAtmosphere(),
                "Total ΔV (atmo)"));
            AddSuffix("TOTALDELTAVATMOSPHEREANDVAC", new Suffix<StringValue>(() => Module.TotalDeltaVAtmosphereAndVac(),
                "Total ΔV (atmo, vac)"));
            AddSuffix("CURRENTRAWBIOME", new Suffix<StringValue>(() => Module.CurrentRawBiome(),
                "Surface Biome."));
            AddSuffix("CURRENTBIOME", new Suffix<StringValue>(() => Module.CurrentBiome(),
                "Current Biome."));
        }
    }
}
