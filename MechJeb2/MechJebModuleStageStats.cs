﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Threading;

namespace MuMech
{
    //Other modules can request that the stage stats be computed by calling RequestUpdate
    //This module will then run the stage stats computation in a separate thread, update
    //the publicly available atmoStats and vacStats. Then it will disable itself unless
    //it got another RequestUpdate in the meantime.
    public class MechJebModuleStageStats : ComputerModule
    {
        public MechJebModuleStageStats(MechJebCore core) : base(core) { }

        [ToggleInfoItem("ΔV include cosine losses", InfoItem.Category.Thrust, showInEditor = true)]
        public bool dVLinearThrust = true;

        public FuelFlowSimulation.Stats[] atmoStats = { };
        public FuelFlowSimulation.Stats[] vacStats = { };

        public void RequestUpdate(object controller)
        {
            users.Add(controller);
            updateRequested = true;

            if (HighLogic.LoadedSceneIsEditor) TryStartSimulation();
        }

        protected bool updateRequested = false;
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
            TryStartSimulation();
        }

        public void TryStartSimulation()
        {
            if ((HighLogic.LoadedSceneIsEditor || vessel.isActiveVessel) && !simulationRunning)
            {
                //We should be running simulations periodically, but one is not running right now. 
                //Check if enough time has passed since the last one to start a new one:
                if (stopwatch.ElapsedMilliseconds > millisecondsBetweenSimulations)
                {
                    if (updateRequested)
                    {
                        updateRequested = false;

                        stopwatch.Stop();
                        stopwatch.Reset();

                        StartSimulation();
                    }
                    else
                    {
                        users.Clear();
                    }
                }
            }
        }

        protected void StartSimulation()
        {
            try
            {
                simulationRunning = true;
                stopwatch.Start(); //starts a timer that times how long the simulation takes

                //Create two FuelFlowSimulations, one for vacuum and one for atmosphere
                List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);
                FuelFlowSimulation[] sims = { new FuelFlowSimulation(parts, dVLinearThrust), new FuelFlowSimulation(parts, dVLinearThrust) };

                //Run the simulation in a separate thread
                ThreadPool.QueueUserWorkItem(RunSimulation, sims);
                //RunSimulation(sims);
            }
            catch (Exception e)
            {
                print("Exception in MechJebModuleStageStats.StartSimulation(): " + e.StackTrace);
                
                // Stop timing the simulation
                stopwatch.Stop();
                millisecondsBetweenSimulations = 500;
                stopwatch.Reset();

                // Start counting down the time to the next simulation
                stopwatch.Start();
                simulationRunning = false;
            }
        }

        protected void RunSimulation(object o)
        {
            try
            {
                //Run the simulation
                FuelFlowSimulation[] sims = (FuelFlowSimulation[])o;
                FuelFlowSimulation.Stats[] newAtmoStats = sims[0].SimulateAllStages(1.0f, 1.0f);
                FuelFlowSimulation.Stats[] newVacStats = sims[1].SimulateAllStages(1.0f, 0.0f);
                atmoStats = newAtmoStats;
                vacStats = newVacStats;
            }
            catch (Exception e)
            {
                print("Exception in MechJebModuleStageStats.RunSimulation(): " + e.StackTrace);
            }

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
