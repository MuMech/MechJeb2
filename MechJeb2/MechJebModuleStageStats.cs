﻿using System;
using System.Collections.Generic;
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

        public void RequestUpdate(object controller, bool wait = false)
        {
            users.Add(controller);
            updateRequested = true;

            if (HighLogic.LoadedSceneIsEditor && editorBody != null)
            {
                if (TryStartSimulation() && wait)
                {
                    while (simulationRunning)
                    {
                        // wait for a sim to be ready. Risked ?
                        Thread.Sleep(1);
                    }
                }
            }
        }

        public CelestialBody editorBody;
        public bool liveSLT = true;
        public double altSLT = 0;
        public double mach = 0;

        protected bool updateRequested = false;
        protected bool simulationRunning = false;
        protected System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        private FuelFlowSimulation[] sims = { new FuelFlowSimulation(), new FuelFlowSimulation() };

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

        public bool TryStartSimulation()
        {
            if ((HighLogic.LoadedSceneIsEditor || (vessel != null && vessel.isActiveVessel)) && !simulationRunning)
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
                        return true;
                    }
                    else
                    {
                        users.Clear();
                    }
                }
            }
            return false;
        }

        protected void StartSimulation()
        {
            try
            {
                simulationRunning = true;
                stopwatch.Start(); //starts a timer that times how long the simulation takes

                //Create two FuelFlowSimulations, one for vacuum and one for atmosphere
                List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);
                
                sims[0].Init(parts, dVLinearThrust);
                sims[1].Init(parts, dVLinearThrust);

                //Run the simulation in a separate thread
                ThreadPool.QueueUserWorkItem(RunSimulation, sims);
                //RunSimulation(sims);
            }
            catch (Exception e)
            {
                print("Exception in MechJebModuleStageStats.StartSimulation(): " + e + "\n" + e.StackTrace);

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
                CelestialBody simBody = HighLogic.LoadedSceneIsEditor ? editorBody : vessel.mainBody;

                double staticPressureKpa = (HighLogic.LoadedSceneIsEditor || !liveSLT ? (simBody.atmosphere ? simBody.GetPressure(altSLT) : 0) : vessel.staticPressurekPa);
                double atmDensity = (HighLogic.LoadedSceneIsEditor || !liveSLT ? simBody.GetDensity(simBody.GetPressure(altSLT), simBody.GetTemperature(0)) : vessel.atmDensity) / 1.225;
                double mach = HighLogic.LoadedSceneIsEditor ? this.mach : vessel.mach;

                //Run the simulation
                FuelFlowSimulation.Stats[] newAtmoStats = sims[0].SimulateAllStages(1.0f, staticPressureKpa, atmDensity, mach);
                FuelFlowSimulation.Stats[] newVacStats = sims[1].SimulateAllStages(1.0f, 0.0, 0.0 , mach);
                atmoStats = newAtmoStats;
                vacStats = newVacStats;
            }
            catch (Exception e)
            {
                print("Exception in MechJebModuleStageStats.RunSimulation(): " + e);
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
