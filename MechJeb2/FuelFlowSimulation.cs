﻿using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using Smooth.Dispose;
using Smooth.Pools;
using Smooth.Slinq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityToolbag;

namespace MuMech
{
    public partial class FuelFlowSimulation
    {
        private          int                        _simStage;                          //the simulated rocket's current stage
        private readonly List<FuelNode>             _nodes      = new List<FuelNode>(); //a list of FuelNodes representing all the parts of the ship
        private readonly Dictionary<Part, FuelNode> _nodeLookup = new Dictionary<Part, FuelNode>();
        private readonly Dictionary<FuelNode, Part> _partLookup = new Dictionary<FuelNode, Part>();

        private double _kpaToAtmospheres;

        //MJ uses 2 separate FuelFlowSimulation instances for vac and atmo calculations.
        //This method is used for copying data from one instance to the other for optimization purposes.
        public void CopyFrom(FuelFlowSimulation f)
        {
            Profiler.BeginSample("FuelFlowSimulation.CopyFrom");

            _nodes.Clear();
            _nodeLookup.Clear();
            _partLookup.Clear();

            _kpaToAtmospheres = f._kpaToAtmospheres;
            _simStage         = f._simStage;

            Profiler.BeginSample("BorrowAndCopyFrom");
            foreach (FuelNode n in f._nodes)
            {
                var n2 = FuelNode.BorrowAndCopyFrom(n);
                _nodes.Add(n2);
            }

            Profiler.EndSample();

            foreach (FuelNode node in _nodes)
            {
                Part part = node.part;
                _nodeLookup[part] = node;
                _partLookup[node] = part;
            }

            Profiler.BeginSample("AddCrossfeedSouces");
            foreach (FuelNode node in _nodes)
            {
                if (!node.isEngine) continue;
                Part p = node.part;
                node.AddCrossfeedSources(p.crossfeedPartSet.GetParts(), _nodeLookup);
            }

            Profiler.EndSample();

            Profiler.EndSample();
        }

        //Takes a list of parts so that the simulation can be run in the editor as well as the flight scene
        public void Init(List<Part> parts, bool dVLinearThrust)
        {
            //print("==================================================");
            //print("Init Start");
            _kpaToAtmospheres = PhysicsGlobals.KpaToAtmospheres;

            // Create FuelNodes corresponding to each Part
            _nodes.Clear();
            _nodeLookup.Clear();
            _partLookup.Clear();

            Part negOnePart = null;

            // initial parts scan, construct the nodes and lookup tables, and choose our root part
            for (int index = 0; index < parts.Count; index++)
            {
                Part part = parts[index];
                var node = FuelNode.Borrow(part, dVLinearThrust);
                _nodeLookup[part] = node;
                _partLookup[node] = part;
                _nodes.Add(node);
                if (part.inverseStage < 0 && negOnePart == null)
                    negOnePart = part;
            }

            if (negOnePart == null)
                negOnePart = parts[0];

            // Determine when each part will be decoupled
            _nodeLookup[negOnePart].AssignDecoupledInStage(negOnePart, null, _nodeLookup, -1);

            _simStage = StageManager.CurrentStage;

            // Set up the fuel flow graph
            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                FuelNode node = _nodeLookup[p];
                if (node.isEngine)
                {
                    HashSet<Part> set = p.crossfeedPartSet.GetParts();
                    node.AddCrossfeedSources(set, _nodeLookup);
                }

                if (node.decoupledInStage >= _simStage) _simStage = node.decoupledInStage + 1;
            }

            //print("Init End");
        }

        //Simulate the activation and execution of each stage of the rocket,
        //and return stats for each stage
        public FuelStats[] SimulateAllStages(float throttle, double staticPressureKpa, double atmDensity, double machNumber)
        {
            var stages = new FuelStats[_simStage + 1];

            double staticPressure = staticPressureKpa * _kpaToAtmospheres;

            //print("**************************************************");
            //print("SimulateAllStages starting from stage " + _simStage + " throttle=" + throttle + " staticPressureKpa=" + staticPressureKpa + " atmDensity=" + atmDensity + " machNumber=" + machNumber);

            while (_simStage >= 0)
            {
                //print("Simulating stage " + _simStage + "(vessel mass = " + VesselMass(_simStage) + ")");
                stages[_simStage] = SimulateStage(throttle, staticPressure, atmDensity, machNumber);
                if (_simStage + 1 < stages.Length)
                    stages[_simStage].StagedMass = stages[_simStage + 1].EndMass - stages[_simStage].StartMass;
                SimulateStageActivation();
            }

            //print("SimulateAllStages ended");

            for (int i = 0; i < _nodes.Count; i++) _nodes[i].Release();

            return stages;
        }

        public static void print(object message)
        {
            Dispatcher.InvokeAsync(() => MonoBehaviour.print("[MechJeb2] " + message));
        }

        //Simulate (the rest of) the current stage of the simulated rocket,
        //and return stats for the stage
        private FuelStats SimulateStage(float throttle, double staticPressure, double atmDensity, double machNumber)
        {
            //need to set initial consumption rates for VesselThrust and AllowedToStage to work right
            for (int i = 0; i < _nodes.Count; i++) _nodes[i].SetConsumptionRates(throttle, staticPressure, atmDensity, machNumber);

            var fuelStats = new FuelStats();
            double thrust, spoolup;
            thrust = VesselThrustAndSpoolup(out spoolup);
            //FuelFlowSimulation.print("Found spoolup time " + spoolup + " on fuel node in stage " + _simStage);
            fuelStats.StartMass    = VesselMass(_simStage);
            fuelStats.StartThrust  = thrust;
            fuelStats.EndThrust    = thrust;
            fuelStats.SpoolUpTime  = spoolup;
            fuelStats.EndMass      = fuelStats.StartMass;
            fuelStats.ResourceMass = 0;
            fuelStats.MaxAccel     = fuelStats.EndMass > 0 ? fuelStats.EndThrust / fuelStats.EndMass : 0;
            fuelStats.DeltaTime    = 0;
            fuelStats.DeltaV       = 0;
            fuelStats.MaxThrust    = MaxThrust();

            const int MAX_STEPS = 100;

            int stepsLeft = MAX_STEPS;
            while (true)
            {
                //print("Stage " + simStage + " step " + step + " endMass " + fuelStats.endMass.ToString("F3"));
                if (AllowedToStage())
                {
                    //print("allowed to stage");
                    break;
                }

                //print("not allowed to stage");

                fuelStats = fuelStats.Append(SimulateTimeStep(double.MaxValue, throttle, staticPressure, atmDensity, machNumber, out double dt));
                //print("Stage " + _simStage + " dt " + dt);
                // BS engine detected. Bail out.
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (dt == double.MaxValue || double.IsInfinity(dt))
                {
                    //print("BS engine detected. Bail out.");
                    break;
                }

                if (--stepsLeft == 0)
                    throw new Exception("FuelFlowSimulation.SimulateStage reached max step count of " + MAX_STEPS);
            }

            //print("thrust = " + fuelStats.StartThrust + " ISP = " + fuelStats.Isp + " FuelFlow = " + ( fuelStats.StartMass - fuelStats.EndMass ) / fuelStats.DeltaTime * 1000 + " num = " + FindActiveEngines().value.Count );

            return fuelStats;
        }

        //Simulate a single time step, and return stats for the time step.
        // - desiredDt is the requested time step size. Often the actual time step size
        //   with be less than this. The actual step size is reported in dt.
        private FuelStats SimulateTimeStep(double desiredDt, float throttle, double staticPressure, double atmDensity, double machNumber,
            out double dt)
        {
            var fuelStats = new FuelStats();

            for (int i = 0; i < _nodes.Count; i++) _nodes[i].ResetDrainRates();

            for (int i = 0; i < _nodes.Count; i++) _nodes[i].SetConsumptionRates(throttle, staticPressure, atmDensity, machNumber);

            fuelStats.StartMass = VesselMass(_simStage);
            // over a single timestep the thrust is considered constant, we don't support thrust curves.
            double spoolup;
            double thrust = VesselThrustAndSpoolup(out spoolup);
            fuelStats.StartThrust = fuelStats.EndThrust = thrust;
            fuelStats.SpoolUpTime = spoolup;
            fuelStats.MaxThrust   = MaxThrust();

            using (Disposable<List<FuelNode>> engines = FindActiveEngines())
            {
                //print("active engines: " + engines.value.Count);
                if (engines.value.Count > 0)
                {
                    for (int i = 0; i < engines.value.Count; i++)
                        engines.value[i].AssignResourceDrainRates(_nodes);

                    //foreach (FuelNode n in _nodes) n.DebugDrainRates();

                    double maxDt = _nodes.Slinq().Select(n => n.MaxTimeStep()).Min();
                    dt = Math.Min(desiredDt, maxDt);

                    //print("Simulating time step of " + dt);

                    for (int i = 0; i < _nodes.Count; i++)
                    {
                        _nodes[i].DrainResources(dt);
                        //_nodes[i].DebugResources();
                    }
                }
                else
                {
                    dt = 0;
                }
            }

            fuelStats.DeltaTime    = dt;
            fuelStats.EndMass      = VesselMass(_simStage);
            fuelStats.ResourceMass = fuelStats.StartMass - fuelStats.EndMass;
            fuelStats.MaxAccel     = fuelStats.EndMass > 0 ? fuelStats.EndThrust / fuelStats.EndMass : 0;
            fuelStats.ComputeTimeStepDeltaV();
            fuelStats.Isp = fuelStats.StartMass > fuelStats.EndMass
                ? fuelStats.DeltaV / (9.80665f * Math.Log(fuelStats.StartMass / fuelStats.EndMass))
                : 0;

            //print("timestep: " + dt + " start thrust: " + fuelStats.StartThrust + " end thrust: " + fuelStats.EndThrust);

            return fuelStats;
        }

        //Active the next stage of the simulated rocket and remove all nodes that get decoupled by the new stage
        private void SimulateStageActivation()
        {
            _simStage--;

            using Disposable<List<FuelNode>> decoupledNodes = ListPool<FuelNode>.Instance.BorrowDisposable();

            _nodes.Slinq().Where((n, stage) => n.decoupledInStage >= stage, _simStage).AddTo(decoupledNodes);

            for (int i = 0; i < decoupledNodes.value.Count; i++)
            {
                FuelNode decoupledNode = decoupledNodes.value[i];
                _nodes.Remove(decoupledNode); //remove the decoupled nodes from the simulated ship
                //print("Decoupling: " + decoupledNode.partName + " decoupledInStage=" + decoupledNode.decoupledInStage);
            }

            for (int i = 0; i < _nodes.Count; i++)
            for (int j = 0; j < decoupledNodes.value.Count; j++)
                _nodes[i].RemoveSourceNode(decoupledNodes.value[j]); //remove the decoupled nodes from the remaining nodes' source lists

            for (int i = 0; i < decoupledNodes.value.Count; i++) decoupledNodes.value[i].Release(); // We can now return them to the pool
        }

        //Whether we've used up the current stage
        private bool AllowedToStage()
        {
            //print("Checking whether allowed to stage");

            using (Disposable<List<FuelNode>> activeEngines = FindActiveEngines())
            {
                //print("  activeEngines.Count = " + activeEngines.value.Count);

                //if no engines are active, we can always stage
                if (activeEngines.value.Count == 0)
                {
                    //print("Allowed to stage because no active engines");
                    return true;
                }

                using (Disposable<List<int>> burnedResources = ListPool<int>.Instance.BorrowDisposable())
                {
                    activeEngines.value.Slinq().SelectMany(eng => eng.BurnedResources().Slinq()).Distinct().AddTo(burnedResources);

                    //if staging would decouple an active engine or non-empty fuel tank, we're not allowed to stage
                    for (int i = 0; i < _nodes.Count; i++)
                    {
                        FuelNode n = _nodes[i];
                        //print(n.partName + " is sepratron? " + n.isSepratron);

                        // filter only the parts that are going to get dropped
                        if (n.decoupledInStage != _simStage - 1 || n.isSepratron) continue;

                        if (activeEngines.value.Contains(n))
                        {
                            //print("Not allowed to stage because " + n.partName + " is an active engine (" + activeEngines.value.Contains(n) +")");
                            //n.DebugResources();
                            return false;
                        }

                        foreach (int id in burnedResources.value)
                        {
                            if (!n.ContainsResource(id))
                                continue;

                            for (int j = 0; j < activeEngines.value.Count; j++)
                            {
                                FuelNode engine = activeEngines.value[j];

                                if (engine.CanDrawResourceFrom(id, n))
                                {
                                    //print("Not allowed to stage because " + n.partName + " contains resources (" +
                                    //      n.ContainsResources(burnedResources.value) + ") reachable by an active engine");
                                    //n.DebugResources();
                                    return false;
                                }
                            }
                        }
                    }
                }

                // We are not allowed to stage if the stage does not decouple anything, and there is an active engine that still has access to resources
                {
                    bool activeEnginesWorking = false;
                    bool partDecoupledInNextStage = false;

                    for (int i = 0; i < _nodes.Count; i++)
                    {
                        FuelNode n = _nodes[i];
                        if (activeEngines.value.Contains(n))
                            if (n.CanDrawNeededResources(_nodes))
                            {
                                //print("Part " + n.partName + " is an active engine that still has resources to draw on.");
                                activeEnginesWorking = true;
                            }

                        if (n.decoupledInStage == _simStage - 1)
                        {
                            //print("Part " + n.partName + " is decoupled in the next stage.");
                            partDecoupledInNextStage = true;
                        }
                    }

                    if (!partDecoupledInNextStage && activeEnginesWorking)
                    {
                        //print("Not allowed to stage because nothing is decoupled in the next stage, and there are already other engines active.");
                        return false;
                    }
                }
            }

            //if this isn't the last stage, we're allowed to stage because doing so wouldn't drop anything important
            if (_simStage > 0)
            {
                //print("Allowed to stage because this isn't the last stage");
                return true;
            }

            //print("Not allowed to stage because there are active engines and this is the last stage");

            //if this is the last stage, we're not allowed to stage while there are still active engines
            return false;
        }

        private double VesselMass(int stage)
        {
            double sum = 0;
            for (int i = 0; i < _nodes.Count; i++) sum += _nodes[i].Mass(stage);

            return sum;
        }

        private double MaxThrust()
        {
            double maxThrust = 0;

            using Disposable<List<FuelNode>> activeEngines = FindActiveEngines();

            for (int i = 0; i < activeEngines.value.Count; i++)
            {
                double mt = activeEngines.value[i].maxThrust;
                if (mt.IsFinite())
                    maxThrust += mt;
            }

            return maxThrust;
        }

        private double VesselThrustAndSpoolup(out double sumSpoolup)
        {
            double sumThrust = 0;
            double sumSpoolupThrust = 0;
            sumSpoolup = 0;

            using Disposable<List<FuelNode>> activeEngines = FindActiveEngines();

            for (int i = 0; i < activeEngines.value.Count; i++)
            {
                double thrust = activeEngines.value[i].partThrust;
                sumThrust += thrust;
                if (_simStage == activeEngines.value[i].inverseStage)
                {
                    sumSpoolupThrust += thrust;
                    sumSpoolup       += activeEngines.value[i].partSpoolupTime * thrust;
                }
            }

            if (sumSpoolupThrust > 0)
                sumSpoolup /= sumSpoolupThrust;

            return sumThrust;
        }

        //Returns a list of engines that fire during the current simulated stage.
        private Disposable<List<FuelNode>> FindActiveEngines()
        {
            Disposable<List<FuelNode>> activeEngines = ListPool<FuelNode>.Instance.BorrowDisposable();

            foreach (FuelNode n in _nodes)
            {
                if (n.isEngine && n.inverseStage >= _simStage && n.isDrawingResources && n.CanDrawNeededResources(_nodes))
                    activeEngines.value.Add(n);
            }

            return activeEngines;
        }
    }
}
