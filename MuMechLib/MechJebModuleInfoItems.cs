using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
