using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleLandingPredictions : ComputerModule
    {    
        public MechJebModuleLandingPredictions(MechJebCore core) : base(core) { }

        public ReentrySimulation.Result result;

        public double perturbationSize = 10;
        public IDescentSpeedPolicy descentSpeedPolicy = null;

        protected bool simulationRunning = false;
        protected System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        long millisecondsBetweenSimulations;

        public override void OnModuleEnabled()
        {
            millisecondsBetweenSimulations = 0;
            stopwatch.Start();
        }

        public override void OnModuleDisabled()
        {
            stopwatch.Stop();
            stopwatch.Reset();
        }

        public override void OnFixedUpdate()
        {
            if (enabled && vessel.isActiveVessel && !simulationRunning)
            {
                //We should be running simulations periodically, but one is not running right now. 
                //Check if enough time has passed since the last one to start a new one:
                if (stopwatch.ElapsedMilliseconds > millisecondsBetweenSimulations)
                {
                    stopwatch.Stop();
                    stopwatch.Reset();

                    StartSimulation();
                }
            }
        }

        protected void StartSimulation()
        {
            simulationRunning = true;

            stopwatch.Start(); //starts a timer that times how long the simulation takes

            double endAltitudeASL = 0;
            if (result != null && result.outcome == ReentrySimulation.Outcome.LANDED)
            {
                endAltitudeASL = MuUtils.TerrainAltitude(mainBody, result.endPosition.latitude, result.endPosition.longitude);
            }

            Orbit o = vessel.orbit;
            if (vessel.patchedConicSolver.maneuverNodes.Count() > 0) o = vessel.patchedConicSolver.maneuverNodes.Last().nextPatch;
            ReentrySimulation sim = new ReentrySimulation(o, vesselState.time, vesselState.massDrag / vesselState.mass, descentSpeedPolicy, endAltitudeASL, 1);

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
    }
}
