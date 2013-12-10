using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleLandingPredictions : ComputerModule
    {
        //publicly available output:
        //Call this function and use the returned object in case this.result changes while you
        //are doing your calculations:
        public ReentrySimulation.Result GetResult() { return result; }
        public ReentrySimulation.Result GetErrorResult() { return errorResult; }

        //inputs:
        [Persistent(pass = (int)Pass.Global)]
        public bool makeAerobrakeNodes = false;

        public bool deployChutes = false;        
        public int limitChutesStage = 0;

        //simulation inputs:
        public double endAltitudeASL = 0; //end simulations when they reach this altitude above sea level
        public IDescentSpeedPolicy descentSpeedPolicy = null; //simulate this descent speed policy
        public double parachuteSemiDeployMultiplier = 3; // this will get updated by the autopilot.
        public bool runErrorSimulations = false; // This will be set by the autopilot to turn error simulations on or off.

        //internal data:
      
        protected bool errorSimulationRunning = false; // the predictor can run two types of simulation - 1) simulations of the current situation. 2) simulations of the current situation with deliberate error introduced into the parachute multiplier to aid the statistical analysis of these results.
        protected System.Diagnostics.Stopwatch errorStopwatch = new System.Diagnostics.Stopwatch();
        protected long millisecondsBetweenErrorSimulations;
  
        protected bool simulationRunning = false;
        protected System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        protected long millisecondsBetweenSimulations;

        protected ReentrySimulation.Result result = null;
        protected ReentrySimulation.Result errorResult = null;

        public ManeuverNode aerobrakeNode = null;

        protected int interationsPerSecond = 5; // the number of times that we want to try to run the simulation each second.
        protected double dt = 0.2; // the suggested dt for each timestep in the simulations. This will be adjusted depending on how long the simulations take to run.

        public override void OnStart(PartModule.StartState state)
        {
            if (state != PartModule.StartState.None && state != PartModule.StartState.Editor)
            {
                RenderingManager.AddToPostDrawQueue(1, DoMapView);
            }
        }

        public override void OnModuleEnabled()
        {
            StartSimulation(false);
        }

        public override void OnModuleDisabled()
        {
            stopwatch.Stop();
            stopwatch.Reset();

            errorStopwatch.Stop();
            errorStopwatch.Reset();
        }

        public override void OnFixedUpdate()
        {
            if (vessel.isActiveVessel)
            {
                // We should be running simulations periodically. If one is not running right now,
                // check if enough time has passed since the last one to start a new one:
                if (!simulationRunning && stopwatch.ElapsedMilliseconds > millisecondsBetweenSimulations)
                {
                    stopwatch.Stop();
                    stopwatch.Reset();

                    StartSimulation(false);
                }

                // We also periodically run simulations containing deliberate errors if we have been asked to do so by the landing autopilot.
                if (this.runErrorSimulations && !errorSimulationRunning && errorStopwatch.ElapsedMilliseconds >= millisecondsBetweenErrorSimulations)
                {
                    errorStopwatch.Stop();
                    errorStopwatch.Reset();

                    StartSimulation(true);
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

        protected void StartSimulation(bool addParachuteError)
        {
            double altitudeOfPreviousPrediction = 0;
            double parachuteMultiplierForThisSimulation = this.parachuteSemiDeployMultiplier;

            if (addParachuteError)
            {
                errorSimulationRunning = true;
                errorStopwatch.Start(); //starts a timer that times how long the simulation takes
            }
            else
            {
                simulationRunning = true;
                stopwatch.Start(); //starts a timer that times how long the simulation takes
            }

            // Work out a mass for the total ship, a DragMass for everything except the parachutes that will be used (including the stowed parachutes that will not be used) and list of parchutes that will be used.
            double totalMass =0;
            double dragMassExcludingUsedParachutes = 0;
            List<SimulatedParachute> usableChutes = new List<SimulatedParachute>();
            
            foreach (Part p in vessel.parts)
            {
                if (p.physicalSignificance != Part.PhysicalSignificance.NONE)
                {
                    bool partIsParachute = false;
                    double partDrag =0;
                    double partMass = p.TotalMass();

                    totalMass += partMass;

                    // Is this part a parachute?
                    foreach (PartModule pm in p.Modules)
                    {
                        if (!pm.isEnabled) continue;

                        if (pm is ModuleParachute)
                        {
                            ModuleParachute chute = (ModuleParachute)pm;
                            partIsParachute = true;
                            // This is a parachute, but is it one that will be used in the landing / rentry simulation?
                            if (deployChutes && p.inverseStage >= limitChutesStage)
                            {
                                // This chute will be used in the simualtion. Add it to the list of useage parachutes.
                                usableChutes.Add(new SimulatedParachute(chute));
                            }
                            else
                            {
                                partDrag = p.maximum_drag;
                            }
                        }
                    }

                    if (false == partIsParachute)
                    {
                        // Part is not a parachute. Just use its drag value.
                        partDrag = p.maximum_drag;
                    }

                    dragMassExcludingUsedParachutes += partDrag * partMass;
                }
            }

            Orbit patch = GetReenteringPatch() ?? orbit;

            // Work out what the landing altitude was of the last prediction, and use that to pass into the next simulation
            if(null !=this.result)
            {
                if(result.outcome == ReentrySimulation.Outcome.LANDED &&  null != result.body)
                {
                    altitudeOfPreviousPrediction = this.result.body.TerrainAltitude(this.result.endPosition.latitude, this.result.endPosition.longitude);
                }
            }

            // Is this a simulation run with errors added? If so then add some error to the parachute multiple
            if (addParachuteError)
            {
                System.Random random = new System.Random();
                parachuteMultiplierForThisSimulation *= ((double)1 + ((double)(random.Next(500000) - 250000) / (double)10000000));
            }

            ReentrySimulation sim = new ReentrySimulation(patch, patch.StartUT, dragMassExcludingUsedParachutes, usableChutes, totalMass, descentSpeedPolicy, endAltitudeASL, vesselState.limitedMaxThrustAccel, parachuteMultiplierForThisSimulation, altitudeOfPreviousPrediction,addParachuteError, dt);

            //Run the simulation in a separate thread
            ThreadPool.QueueUserWorkItem(RunSimulation, sim);
        }

        protected void RunSimulation(object o)
        {
            ReentrySimulation sim = (ReentrySimulation)o;

            ReentrySimulation.Result newResult = sim.RunSimulation();

            if (newResult.multiplierHasError)
            {
                errorResult = newResult;

                //see how long the simulation took
                errorStopwatch.Stop();
                long millisecondsToCompletion = errorStopwatch.ElapsedMilliseconds;

                errorStopwatch.Reset();

                //set the delay before the next simulation
                millisecondsBetweenErrorSimulations = 4 * millisecondsToCompletion; // Note that we are going to run the simualtions with error in less often that the real simulations

                //start the stopwatch that will count off this delay
                errorStopwatch.Start();
                errorSimulationRunning = false;
            }
            else
            {
                result = newResult;
                //see how long the simulation took
                stopwatch.Stop();
                long millisecondsToCompletion = stopwatch.ElapsedMilliseconds;
                stopwatch.Reset();

                //set the delay before the next simulation
                millisecondsBetweenSimulations = 2 * millisecondsToCompletion;

                // How long should we set the max_dt to be in the future? Calculate for interationsPerSecond runs per second.
                dt = (newResult.maxdt * ((double)millisecondsToCompletion/(double)1000) ) /  ((double)1 / ((double)3 * (double)interationsPerSecond)) ;
                dt = Math.Max(dt, Time.fixedDeltaTime);
                // TODO remove debugging 
                //Debug.Log("Time to run: " + millisecondsToCompletion + " new dt: " + dt + " Time.fixedDeltaTime " + Time.fixedDeltaTime);

                //start the stopwatch that will count off this delay
                stopwatch.Start();
                simulationRunning = false;
            }
        }

        protected Orbit GetReenteringPatch()
        {
            Orbit patch = orbit;

            int i = 0;

            do
            {
                i++;
                double reentryRadius = patch.referenceBody.Radius + patch.referenceBody.RealMaxAtmosphereAltitude();
                Orbit nextPatch = vessel.GetNextPatch(patch, aerobrakeNode);
                if (patch.PeR < reentryRadius)
                {
                    if (patch.Radius(patch.StartUT) < reentryRadius) return patch;

                    double reentryTime = patch.NextTimeOfRadius(patch.StartUT, reentryRadius);
                    if (patch.StartUT < reentryTime && (nextPatch == null || reentryTime < nextPatch.StartUT))
                    {
                        return patch;
                    }
                }

                patch = nextPatch;
            }
            while (patch != null);

            return null;
        }

        protected void MaintainAerobrakeNode()
        {
            if (makeAerobrakeNodes)
            {
                //Remove node after finishing aerobraking:
                if (aerobrakeNode != null && vessel.patchedConicSolver.maneuverNodes.Contains(aerobrakeNode))
                {
                    if (aerobrakeNode.UT < vesselState.time && vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude())
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
                        vesselState.altitudeASL < mainBody.RealMaxAtmosphereAltitude() && vesselState.speedVertical > 0)
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
                    //no aerobraking, remove the node:
                    if (aerobrakeNode != null && vessel.patchedConicSolver.maneuverNodes.Contains(aerobrakeNode))
                    {
                        vessel.patchedConicSolver.RemoveManeuverNode(aerobrakeNode);
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

        void DoMapView()
        {
            ReentrySimulation.Result drawnResult = GetResult();
            if (MapView.MapIsEnabled && this.enabled && drawnResult != null)
            {
                if (drawnResult.outcome == ReentrySimulation.Outcome.LANDED)
                {
                    GLUtils.DrawMapViewGroundMarker(drawnResult.body, drawnResult.endPosition.latitude, drawnResult.endPosition.longitude, Color.blue, 60);
                }
            }
        }

        public MechJebModuleLandingPredictions(MechJebCore core) : base(core) { }
    }
}
