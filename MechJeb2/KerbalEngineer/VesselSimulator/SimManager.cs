// 
//     Kerbal Engineer Redux
// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

namespace KerbalEngineer.VesselSimulator
{
    #region Using Directives

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using UnityEngine;

    #endregion

    public class SimManager
    {
        #region Constants

        public const double RESOURCE_MIN = 0.0001;
        public const double RESOURCE_PART_EMPTY_THRESH = 0.01;

        #endregion

        #region Fields

        public static bool dumpTree = false;
        public static bool logOutput = false;
        public static TimeSpan minSimTime = new TimeSpan(0, 0, 0, 0, 150);
        public static bool vectoredThrust = false;
        private static readonly object locker = new object();
        private static readonly Stopwatch timer = new Stopwatch();

        private static bool bRequested;
        private static bool bRunning;
        private static TimeSpan delayBetweenSims;

        // Support for RealFuels using reflection to check localCorrectThrust without dependency

        private static bool hasCheckedForMods;
        private static bool hasInstalledRealFuels;
        private static FieldInfo RF_ModuleEngineConfigs_localCorrectThrust;
        private static FieldInfo RF_ModuleHybridEngine_localCorrectThrust;
        private static FieldInfo RF_ModuleHybridEngines_localCorrectThrust;
        private static bool hasInstalledKIDS;
        private static MethodInfo KIDS_Utils_GetIspMultiplier;
        private static bool bKIDSThrustISP = false;
        private static List<Part> parts = new List<Part>(); 

        private static Simulation[] simulations = { new Simulation(), new Simulation() };
        #endregion

        #region Delegates

        public delegate void ReadyEvent();

        #endregion

        #region Events

        public static event ReadyEvent OnReady;

        #endregion

        #region Properties

        public static double Atmosphere { get; set; }

        public static double Gravity { get; set; }

        public static CelestialBody Body { get; set; }

        public static Stage LastVacStage { get; private set; }
        public static Stage LastAtmStage { get; private set; }

        public static Stage[] VacStages { get; private set; }
        public static Stage[] AtmStages { get; private set; }

        public static double Mach { get; set; }

        public static String failMessage { get; private set; }

        #endregion

        #region Methods

        private static void CheckForMods()
        {
            hasCheckedForMods = true;

            foreach (var assembly in AssemblyLoader.loadedAssemblies)
            {
                MonoBehaviour.print("Assembly:" + assembly.assembly);

                var name = assembly.assembly.ToString().Split(',')[0];

                if (name == "RealFuels")
                {
                    MonoBehaviour.print("Found RealFuels mod");

                    var RF_ModuleEngineConfigs_Type = assembly.assembly.GetType("RealFuels.ModuleEngineConfigs");
                    if (RF_ModuleEngineConfigs_Type != null)
                    {
                        RF_ModuleEngineConfigs_localCorrectThrust = RF_ModuleEngineConfigs_Type.GetField("localCorrectThrust");
                    }

                    var RF_ModuleHybridEngine_Type = assembly.assembly.GetType("RealFuels.ModuleHybridEngine");
                    if (RF_ModuleHybridEngine_Type != null)
                    {
                        RF_ModuleHybridEngine_localCorrectThrust = RF_ModuleHybridEngine_Type.GetField("localCorrectThrust");
                    }

                    var RF_ModuleHybridEngines_Type = assembly.assembly.GetType("RealFuels.ModuleHybridEngines");
                    if (RF_ModuleHybridEngines_Type != null)
                    {
                        RF_ModuleHybridEngines_localCorrectThrust = RF_ModuleHybridEngines_Type.GetField("localCorrectThrust");
                    }

                    hasInstalledRealFuels = true;
                    break;
                }
                else if (name == "KerbalIspDifficultyScaler")
                {
                    var KIDS_Utils_Type = assembly.assembly.GetType("KerbalIspDifficultyScaler.KerbalIspDifficultyScalerUtils");
                    if (KIDS_Utils_Type != null)
                    {
                        KIDS_Utils_GetIspMultiplier = KIDS_Utils_Type.GetMethod("GetIspMultiplier");
                    }

                    hasInstalledKIDS = true;
                }
            }
        }

        public static bool DoesEngineUseCorrectedThrust(Part theEngine)
        {
            if (hasInstalledRealFuels)
            {
                // Look for any of the Real Fuels engine modules and call the relevant method to find out
                if (RF_ModuleEngineConfigs_localCorrectThrust != null && theEngine.Modules.Contains("ModuleEngineConfigs"))
                {
                    var modEngineConfigs = theEngine.Modules["ModuleEngineConfigs"];
                    if (modEngineConfigs != null)
                    {
                        // Return the localCorrectThrust
                        return (bool)RF_ModuleEngineConfigs_localCorrectThrust.GetValue(modEngineConfigs);
                    }
                }

                if (RF_ModuleHybridEngine_localCorrectThrust != null && theEngine.Modules.Contains("ModuleHybridEngine"))
                {
                    var modHybridEngine = theEngine.Modules["ModuleHybridEngine"];
                    if (modHybridEngine != null)
                    {
                        // Return the localCorrectThrust
                        return (bool)RF_ModuleHybridEngine_localCorrectThrust.GetValue(modHybridEngine);
                    }
                }

                if (RF_ModuleHybridEngines_localCorrectThrust != null && theEngine.Modules.Contains("ModuleHybridEngines"))
                {
                    var modHybridEngines = theEngine.Modules["ModuleHybridEngines"];
                    if (modHybridEngines != null)
                    {
                        // Return the localCorrectThrust
                        return (bool)RF_ModuleHybridEngines_localCorrectThrust.GetValue(modHybridEngines);
                    }
                }
            }

            if (hasInstalledKIDS && HighLogic.LoadedSceneIsEditor)
            {
                return bKIDSThrustISP;
            }

            return false;
        }

        public static void UpdateModSettings()
        {
            if (!hasCheckedForMods)
            {
                CheckForMods();
            }

            if (hasInstalledKIDS)
            {
                // (out ispMultiplierVac, out ispMultiplierAtm, out extendToZeroIsp, out thrustCorrection, out ispCutoff, out thrustCutoff);
                object[] parameters = new object[6];
                KIDS_Utils_GetIspMultiplier.Invoke(null, parameters);
                bKIDSThrustISP = (bool)parameters[3];
            }
        }

        public static String GetVesselTypeString(VesselType vesselType)
        {
            switch (vesselType)
            {
                case VesselType.Debris:
                    return "Debris";
                case VesselType.SpaceObject:
                    return "SpaceObject";
                case VesselType.Unknown:
                    return "Unknown";
                case VesselType.Probe:
                    return "Probe";
                case VesselType.Rover:
                    return "Rover";
                case VesselType.Lander:
                    return "Lander";
                case VesselType.Ship:
                    return "Ship";
                case VesselType.Station:
                    return "Station";
                case VesselType.Base:
                    return "Base";
                case VesselType.EVA:
                    return "EVA";
                case VesselType.Flag:
                    return "Flag";
            }
            return "Undefined";
        }

        public static void RequestSimulation()
        {
            if (!hasCheckedForMods)
            {
                CheckForMods();
            }

            lock (locker)
            {
                bRequested = true;
                if (!timer.IsRunning)
                {
                    timer.Start();
                }
            }
        }

        public static bool ResultsReady()
        {
            lock (locker)
            {
                return !bRunning;
            }
        }

        public static void TryStartSimulation()
        {
            lock (locker)
            {
                if (!bRequested || bRunning || (timer.Elapsed < delayBetweenSims && timer.Elapsed >= TimeSpan.Zero) || (!HighLogic.LoadedSceneIsEditor && FlightGlobals.ActiveVessel == null))
                {
                    return;
                }

                bRequested = false;
                timer.Reset();
            }

            StartSimulation();
        }

        private static void ClearResults()
        {
            failMessage = "";
            VacStages = null;
            LastVacStage = null;
        }

        private static void RunSimulation(object simObject)
        {
            Simulation[] sims = (Simulation[])simObject;
            try
            {
                
                VacStages = sims[0].RunSimulation();
                //Profiler.EndSample();
                if (VacStages != null && VacStages.Length > 0)
                {
                    if (logOutput)
                    {
                        foreach (var stage in VacStages)
                        {
                            MonoBehaviour.print("VacStages");
                            stage.Dump();
                        }
                    }
                    LastVacStage = VacStages[VacStages.Length - 1];
                }


                AtmStages = sims[1].RunSimulation();
                if (AtmStages != null && AtmStages.Length > 0)
                {
                    if (logOutput)
                    {
                        MonoBehaviour.print("AtmStages");
                        foreach (var stage in AtmStages)
                        {
                            stage.Dump();
                        }
                    }
                    LastAtmStage = AtmStages[AtmStages.Length - 1];
                }

            }
            catch (Exception e)
            {
                Logger.Exception(e, "SimManager.RunSimulation()");
                VacStages = null;
                LastVacStage = null;
                failMessage = e.ToString();
            }
            lock (locker)
            {
                timer.Stop();
#if TIMERS
            MonoBehaviour.print("Total simulation time: " + timer.ElapsedMilliseconds + "ms");
#else
                if (logOutput)
                {
                    MonoBehaviour.print("Total simulation time: " + timer.ElapsedMilliseconds + "ms");
                }
#endif

                delayBetweenSims = minSimTime - timer.Elapsed;
                if (delayBetweenSims < TimeSpan.Zero)
                {
                    delayBetweenSims = TimeSpan.Zero;
                }

                timer.Reset();
                timer.Start();

                bRunning = false;
                if (OnReady != null)
                {
                    OnReady();
                }
            }

            logOutput = false;
        }

        private static void StartSimulation()
        {
            try
            {
                lock (locker)
                {
                    bRunning = true;
                }

                ClearResults();

                lock (locker)
                {
                    timer.Start();
                }

                parts = HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : FlightGlobals.ActiveVessel.Parts;

                //Profiler.BeginSample("SimManager.StartSimulation().vacSim");
                bool vacSim = simulations[0].PrepareSimulation(parts, Gravity, 0d, Mach, dumpTree, vectoredThrust, true);
                //Profiler.EndSample();
                //Profiler.BeginSample("SimManager.StartSimulation().atmSim");
                bool atmSim = simulations[1].PrepareSimulation(parts, Gravity, Atmosphere, Mach, dumpTree, vectoredThrust, true);
                //Profiler.EndSample();

                // This call doesn't ever fail at the moment but we'll check and return a sensible error for display
                if (vacSim && atmSim)
                {
                    ThreadPool.QueueUserWorkItem(RunSimulation, simulations);
                }
                else
                {
                    failMessage = "PrepareSimulation failed";
                    lock (locker)
                    {
                        bRunning = false;
                    }
                    logOutput = false;
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e, "SimManager.StartSimulation()");
                failMessage = e.ToString();
                lock (locker)
                {
                    bRunning = false;
                }
                logOutput = false;
            }
            dumpTree = false;
        }

        #endregion
    }
}