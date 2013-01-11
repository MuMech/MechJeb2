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
        //publicly available output
        public ReentrySimulation.Result result;

        //input parameters:
        public double endAltitudeASL = 0; //end simulations when they reach this altitude above sea level
        public IDescentSpeedPolicy descentSpeedPolicy = null; //simulate this descent speed policy

        //data for deciding when to run simulations:
        protected bool simulationRunning = false;
        protected System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        protected long millisecondsBetweenSimulations;


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

            Orbit o = vessel.orbit;
            if (vessel.patchedConicSolver.maneuverNodes.Count() > 0) o = vessel.patchedConicSolver.maneuverNodes.Last().nextPatch;
            ReentrySimulation sim = new ReentrySimulation(o, vesselState.time, vesselState.massDrag / vesselState.mass, descentSpeedPolicy, endAltitudeASL);

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

        public MechJebModuleLandingPredictions(MechJebCore core) : base(core) { }
    }
}
