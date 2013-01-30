using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MuMech
{
    //LandingPredctions should be enabled/disabled through .users, not .enabled.
    public class MechJebModuleLandingPredictions : ComputerModule
    {
        //publicly available output:
        //Call this function and use the returned object in case this.result changes while you
        //are doing your calculations:
        public ReentrySimulation.Result GetResult() { return result; }

        //inputs:
        public bool makeAerobrakeNodes = false;

        //simulation inputs:
        public double endAltitudeASL = 0; //end simulations when they reach this altitude above sea level
        public IDescentSpeedPolicy descentSpeedPolicy = null; //simulate this descent speed policy


        //internal data:
        protected bool simulationRunning = false;
        protected System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        protected long millisecondsBetweenSimulations;

        protected ReentrySimulation.Result result;

        protected ManeuverNode aerobrakeNode = null;


        public override void OnModuleEnabled()
        {
            StartSimulation();
        }

        public override void OnModuleDisabled()
        {
            stopwatch.Stop();
            stopwatch.Reset();
        }

        public override void OnFixedUpdate()
        {
            if (vessel.isActiveVessel)
            {
                //We should be running simulations periodically. If one is not running right now,
                //check if enough time has passed since the last one to start a new one:
                if (!simulationRunning && stopwatch.ElapsedMilliseconds > millisecondsBetweenSimulations)
                {
                    stopwatch.Stop();
                    stopwatch.Reset();

                    StartSimulation();
                }
            }
        }

        public override void OnUpdate()
        {
            if (vessel.isActiveVessel)
            {
                MaintainAerobrakeNode();
            }
        }

        protected void StartSimulation()
        {
            simulationRunning = true;

            stopwatch.Start(); //starts a timer that times how long the simulation takes

            Orbit patch = GetReenteringPatch() ?? orbit;

            ReentrySimulation sim = new ReentrySimulation(patch, patch.StartUT, vesselState.massDrag / vesselState.mass, descentSpeedPolicy, endAltitudeASL, vesselState.maxThrustAccel);

            //Run the simulation in a separate thread
            ThreadPool.QueueUserWorkItem(RunSimulation, sim);
        }

        protected void RunSimulation(object o)
        {
            ReentrySimulation sim = (ReentrySimulation)o;

            ReentrySimulation.Result newResult = sim.RunSimulation();

            result = newResult;

            //see how long the simulation took
            stopwatch.Stop();
            long millisecondsToCompletion = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            //set the delay before the next simulation
            millisecondsBetweenSimulations = 2 * millisecondsToCompletion;

            //start the stopwatch that will count off this delay
            stopwatch.Start();

            simulationRunning = false;
        }

        protected Orbit GetReenteringPatch()
        {
            Orbit patch = orbit;

            int i = 0;

            do
            {
                i++;
//                Debug.Log("looking at patch #" + i + "; start = " + patch.patchStartTransition + "; PeR = " + patch.PeR);
                double reentryRadius = patch.referenceBody.Radius + patch.referenceBody.maxAtmosphereAltitude;
                Orbit nextPatch = vessel.GetNextPatch(patch, aerobrakeNode);
                if (patch.PeR < reentryRadius)
                {
                    if (patch.Radius(patch.StartUT) < reentryRadius) return patch;

                    double reentryTime = patch.NextTimeOfRadius(patch.StartUT, reentryRadius);
//                    Debug.Log("reentryTime = " + reentryTime + (nextPatch == null ? "; nextpatch null" : "nextpatch start = " + nextPatch.StartUT));
                    if (patch.StartUT < reentryTime && (nextPatch == null || reentryTime < nextPatch.StartUT))
                    {
//                        Debug.Log("reentering patch is patch #" + i);
                        return patch;
                    }
                }
                
                patch = nextPatch;
            }
            while (patch != null);

//            Debug.Log("No reentering patch");

            return null;
        }

        protected void MaintainAerobrakeNode()
        {
            if (makeAerobrakeNodes)
            {
                //Remove node after finishing aerobraking:
                if (aerobrakeNode != null && vessel.patchedConicSolver.maneuverNodes.Contains(aerobrakeNode))
                {
                    if (aerobrakeNode.UT < vesselState.time && vesselState.altitudeASL > mainBody.maxAtmosphereAltitude)
                    {
                        vessel.patchedConicSolver.RemoveManeuverNode(aerobrakeNode);
                        aerobrakeNode = null;
                    }
                }

                //Update or create node if necessary:
                ReentrySimulation.Result r = GetResult();
                if (r != null && r.outcome == ReentrySimulation.Outcome.AEROBRAKED)
                {
                    
                    //Compute the node dV:
                    Orbit preAerobrakeOrbit = GetReenteringPatch();

                    //Put the node at periapsis, unless we're past periapsis. In that case put the node at the current time.
                    double UT;
                    if (preAerobrakeOrbit == orbit &&
                        vesselState.altitudeASL < mainBody.maxAtmosphereAltitude && vesselState.speedVertical > 0)
                    {
                        UT = vesselState.time;
                    }
                    else
                    {
                        UT = preAerobrakeOrbit.NextPeriapsisTime(preAerobrakeOrbit.StartUT);
                    }

                    Orbit postAerobrakeOrbit = MuUtils.OrbitFromStateVectors(r.WorldEndPosition(), r.WorldEndVelocity(), r.body, r.endUT);
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(preAerobrakeOrbit, UT, postAerobrakeOrbit.ApR);

                    if (aerobrakeNode != null && vessel.patchedConicSolver.maneuverNodes.Contains(aerobrakeNode))
                    {
                        //update the existing node
                        Vector3d nodeDV = preAerobrakeOrbit.DeltaVToManeuverNodeCoordinates(UT, dV);
                        aerobrakeNode.OnGizmoUpdated(nodeDV, UT);
                    }
                    else
                    {
                        //place a new node
                        aerobrakeNode = vessel.PlaceManeuverNode(preAerobrakeOrbit, dV, UT);
                    }
                }
                else
                {
//                    Debug.Log("No aerobraking");
                    //no aerobraking, remove the node:
                    if (aerobrakeNode != null && vessel.patchedConicSolver.maneuverNodes.Contains(aerobrakeNode))
                    {
//                        Debug.Log("removing node");
                        vessel.patchedConicSolver.RemoveManeuverNode(aerobrakeNode);
                    }
                    else
                    {
//                        Debug.Log("no node to remove");
                    }
                }
            }
            else
            {
                //Remove aerobrake node when it is turned off:
                if (aerobrakeNode != null && vessel.patchedConicSolver.maneuverNodes.Contains(aerobrakeNode))
                {
                    vessel.patchedConicSolver.RemoveManeuverNode(aerobrakeNode);
                }
            }
        }


        public MechJebModuleLandingPredictions(MechJebCore core) : base(core) { }


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

            return MuUtils.ToSI(impactTime - vesselState.time, -1) + "s";
        }
    }
}
