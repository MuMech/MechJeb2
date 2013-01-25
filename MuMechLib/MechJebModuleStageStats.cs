using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Threading;

namespace MuMech
{
    //When enabled, this ComputerModule periodically runs a FuelFlowSimulation in a separate thread.
    //It stores the results of the most recent simulation for use by other modules
    class MechJebModuleStageStats : ComputerModule
    {
        public MechJebModuleStageStats(MechJebCore core) : base(core) { }

        public FuelFlowSimulation.Stats[] atmoStats = {};
        public FuelFlowSimulation.Stats[] vacStats = {};

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

            //Create two FuelFlowSimulations, one for vacuum and one for atmosphere
            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.SortedShipList : vessel.parts);
            FuelFlowSimulation[] sims = {new FuelFlowSimulation(parts), new FuelFlowSimulation(parts)};

            //Run the simulation in a separate thread
            ThreadPool.QueueUserWorkItem(RunSimulation, sims);
        }

        protected void RunSimulation(object o)
        {
            //Run the simulation
            FuelFlowSimulation[] sims = (FuelFlowSimulation[])o;
            FuelFlowSimulation.Stats[] newAtmoStats = sims[0].SimulateAllStages(1.0f, 1.0f);
            FuelFlowSimulation.Stats[] newVacStats = sims[1].SimulateAllStages(1.0f, 0.0f);
            atmoStats = newAtmoStats;
            vacStats = newVacStats;

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


        [ValueInfoItem(name="Part count")]
        public int PartCount()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.Count;
            else return vessel.parts.Count;
        }

        [ValueInfoItem(name="Strut count")]
        public int StrutCount() 
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.Count(p => p is StrutConnector);
            else return vessel.parts.Count(p => p is StrutConnector);
        }

        [ValueInfoItem(name="Vessel cost", units="k$")]
        public int VesselCost()
        {
            if (HighLogic.LoadedSceneIsEditor) return EditorLogic.SortedShipList.Sum(p => p.partInfo.cost);
            else return vessel.parts.Sum(p => p.partInfo.cost);
        }
    }
}
