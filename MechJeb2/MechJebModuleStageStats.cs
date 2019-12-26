using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    //Other modules can request that the stage stats be computed by calling RequestUpdate
    //This module will then run the stage stats computation in a separate thread, update
    //the publicly available atmoStats and vacStats. Then it will disable itself unless
    //it got another RequestUpdate in the meantime.
    public class MechJebModuleStageStats : ComputerModule
    {
        public MechJebModuleStageStats(MechJebCore core) : base(core) { }

        [ToggleInfoItem("#MechJeb_DVincludecosinelosses", InfoItem.Category.Thrust, showInEditor = true)]//ΔV include cosine losses
        public bool dVLinearThrust = true;

        public FuelFlowSimulation.Stats[] atmoStats = { };
        public FuelFlowSimulation.Stats[] vacStats = { };


        // Those are used to store the next result from the thread since we must move result 
        // to atmoStats/vacStats only in the main thread.
        private FuelFlowSimulation.Stats[] newAtmoStats;
        private FuelFlowSimulation.Stats[] newVacStats;
        private bool resultReady = false;

        public void RequestUpdate(object controller, bool wait = false)
        {
            users.Add(controller);
            updateRequested = true;

            IsResultReady();

            // In the editor this is our only entry point
            if (HighLogic.LoadedSceneIsEditor)
            {
                TryStartSimulation();
            }
            
            // wait means the code needs some result to run so we wait if we do not have any result yet
            if (wait && atmoStats.Length == 0 && (simulationRunning || TryStartSimulation()))
            {
                while (simulationRunning)
                {
                    // wait for a sim to be ready. Risked ?
                    Thread.Sleep(1);
                }
                IsResultReady();
            }
        }

        public CelestialBody editorBody;
        public bool liveSLT = true;
        public double altSLT = 0;
        public double mach = 0;

        protected bool updateRequested = false;
        protected bool simulationRunning = false;
        protected System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        private int needRebuild = 1;

        private FuelFlowSimulation[] sims = { new FuelFlowSimulation(), new FuelFlowSimulation() };

        long millisecondsBetweenSimulations;

        public override void OnStart(PartModule.StartState state)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(onEditorShipModified);
                GameEvents.onPartCrossfeedStateChange.Add(onPartCrossfeedStateChange);
            }
        }

        public override void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            GameEvents.onPartCrossfeedStateChange.Remove(onPartCrossfeedStateChange);
        }

        private void onPartCrossfeedStateChange(Part data)
        {
            setDirty();
        }

        void onEditorShipModified(ShipConstruct data)
        {
            setDirty();
        }

        void setDirty()
        {
            // The ship is not really ready in the first frame following the event so we wait 2
            needRebuild = 2;
        }

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
            // Check if we have a result ready from the previous physic frame
            IsResultReady();

            TryStartSimulation();
        }

        public override void OnWaitForFixedUpdate()
        {
            // Check if we managed to get a result while the physic frame was running
            IsResultReady();
        }

        public override void OnUpdate()
        {
            IsResultReady();
        }

        private void IsResultReady()
        {
            if (resultReady)
            {
                atmoStats = newAtmoStats;
                vacStats = newVacStats;
                resultReady = false;
            }
        }

        private bool TryStartSimulation()
        {
            if (!simulationRunning && ((HighLogic.LoadedSceneIsEditor && editorBody != null) || (vessel != null)))
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
            Profiler.BeginSample("StartSimulation");
            try
            {
                simulationRunning = true;
                resultReady = false;
                stopwatch.Start(); //starts a timer that times how long the simulation takes
                
                //Create two FuelFlowSimulations, one for vacuum and one for atmosphere
                List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);

                if (HighLogic.LoadedSceneIsEditor)
                {
                    if (needRebuild > 0)
                    {
                        PartSet.BuildPartSets(parts, null);
                        needRebuild--;
                    }
                }
                else
                {
                    vessel.UpdateResourceSetsIfDirty();
                }

                Profiler.BeginSample("StartSimulation_Init");

                sims[0].Init(parts, dVLinearThrust);
                sims[1].Init(parts, dVLinearThrust);

                Profiler.EndSample();

                //Run the simulation in a separate thread
                ThreadPool.QueueUserWorkItem(RunSimulation, sims);
                //Profiler.BeginSample("StartSimulation_Run");
                //RunSimulation(sims);
                //Profiler.EndSample();
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
            Profiler.EndSample();
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
                newAtmoStats = sims[0].SimulateAllStages(1.0f, staticPressureKpa, atmDensity, mach);
                newVacStats = sims[1].SimulateAllStages(1.0f, 0.0, 0.0, mach);
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
            resultReady = true;
            simulationRunning = false;
        }
    }
}
