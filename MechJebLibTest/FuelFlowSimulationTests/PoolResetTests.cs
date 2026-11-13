/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.FuelFlowSimulation.PartModules;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    /// <summary>
    ///     The SimVesselManagers hand their parts and modules back to the pools on every rebuild (every 500 ms in the editor),
    ///     and the next vessel borrows them.  A field the builder or the updater sets only in some cases (Procedural Fairings'
    ///     IsDecoupled, say) would otherwise keep the last vessel's value, so every field goes back to its declared default.
    /// </summary>
    public class PoolResetTests
    {
        private const int COUNT = 8;

        [Fact]
        public void ReleasedPartsAndModulesComeBackCleared()
        {
            SimVessel v = SimVessel.Borrow();
            var parts = new List<SimPart>();

            for (int i = 0; i < COUNT; i++)
                parts.Add(DirtyPart(v));

            // a part hands its modules back to their pools too
            foreach (SimPart p in parts)
                p.Dispose();

            for (int i = 0; i < COUNT; i++)
            {
                SimPart p = SimPart.Borrow(v, "clean");
                AssertClean(p);
                p.Dispose();
            }

            for (int i = 0; i < COUNT; i++)
            {
                SimPart p = SimPart.Borrow(v, "clean");

                var fairing = SimProceduralFairingDecoupler.Borrow(p);
                Assert.False(fairing.IsDecoupled);

                var decoupler = SimModuleDecouple.Borrow(p);
                Assert.False(decoupler.IsDecoupled);
                Assert.False(decoupler.IsOmniDecoupler);
                Assert.False(decoupler.Staged);
                Assert.Null(decoupler.AttachedPart);

                var dockingNode = SimModuleDockingNode.Borrow(p);
                Assert.False(dockingNode.Staged);
                Assert.Null(dockingNode.AttachedPart);

                var engine = SimModuleEngines.Borrow(p);
                Assert.False(engine.IsOperational);
                Assert.False(engine.NoPropellants);
                Assert.False(engine.IsUnrestartableDeadEngine);
                Assert.False(engine.IsModuleEnginesRf);
                Assert.Equal(0, engine.MaxThrust);
                Assert.Equal(0, engine.MaxFuelFlow);
                Assert.Equal(0, engine.ThrottleLimiter);
                Assert.Equal(0.0, engine.ModuleResiduals);
                Assert.Equal(0.0, engine.MassFlowRate);
                Assert.Equal(V3.zero, engine.ThrustCurrent);

                var rcs = SimModuleRCS.Borrow(p);
                Assert.False(rcs.RcsEnabled);
                Assert.Equal(0.0, rcs.MaxFuelFlow);
                Assert.Equal(0.0, rcs.ThrustPercentage);
                Assert.Equal(0.0, rcs.MassFlowRate);

                foreach (SimPartModule m in new SimPartModule[] { fairing, decoupler, dockingNode, engine, rcs })
                {
                    Assert.False(m.IsEnabled);
                    Assert.True(m.ModuleIsEnabled);
                    Assert.True(m.StagingEnabled);
                    Assert.Equal(-1, m.Index);
                    p.Modules.Add(m);
                }

                p.Dispose();
            }

            v.Dispose();
        }

        private static SimPart DirtyPart(SimVessel v)
        {
            SimPart p = SimPart.Borrow(v, "dirty");
            p.PersistentId = 42;
            p.DecoupledInStage = 3;
            p.StagingOn = false;
            p.InverseStage = 5;
            p.ActivatesEvenIfDisconnected = false;
            p.IsThrottleLocked = true;
            p.ResourcePriority = 7;
            p.ResourceRequestRemainingThreshold = 1;
            p.IsEnabled = true;
            p.Mass = p.DryMass = p.CrewMass = 1;
            p.ModulesStagedMass = p.ModulesUnstagedMass = p.DisabledResourcesMass = p.EngineResiduals = 1;
            p.IsRoot = true;
            p.IsLaunchClamp = true;
            p.IsEngine = true;

            var fairing = SimProceduralFairingDecoupler.Borrow(p);
            fairing.IsDecoupled = true;

            var decoupler = SimModuleDecouple.Borrow(p);
            decoupler.IsDecoupled = decoupler.IsOmniDecoupler = decoupler.Staged = true;
            decoupler.AttachedPart = p;

            var dockingNode = SimModuleDockingNode.Borrow(p);
            dockingNode.Staged = true;
            dockingNode.AttachedPart = p;

            var engine = SimModuleEngines.Borrow(p);
            engine.IsOperational = engine.NoPropellants = engine.IsUnrestartableDeadEngine = engine.IsModuleEnginesRf = true;
            engine.MaxThrust = engine.MaxFuelFlow = engine.ThrottleLimiter = 1;
            engine.ModuleResiduals = engine.MassFlowRate = 1;
            engine.ThrustCurrent = new V3(1, 2, 3);

            var rcs = SimModuleRCS.Borrow(p);
            rcs.RcsEnabled = true;
            rcs.MaxFuelFlow = rcs.ThrustPercentage = rcs.MassFlowRate = 1;

            foreach (SimPartModule m in new SimPartModule[] { fairing, decoupler, dockingNode, engine, rcs })
            {
                m.IsEnabled = true;
                m.ModuleIsEnabled = m.StagingEnabled = false;
                m.Index = 9;
                p.Modules.Add(m);
            }

            return p;
        }

        private static void AssertClean(SimPart p)
        {
            Assert.Empty(p.Modules);
            Assert.Equal(0u, p.PersistentId);
            Assert.Equal(-1, p.DecoupledInStage);
            Assert.True(p.StagingOn);
            Assert.Equal(-1, p.InverseStage);
            Assert.True(p.ActivatesEvenIfDisconnected);
            Assert.False(p.IsThrottleLocked);
            Assert.Equal(0, p.ResourcePriority);
            Assert.Equal(1e-12, p.ResourceRequestRemainingThreshold);
            Assert.False(p.IsEnabled);
            Assert.Equal(0.0, p.Mass);
            Assert.Equal(0.0, p.DryMass);
            Assert.Equal(0.0, p.CrewMass);
            Assert.Equal(0.0, p.ModulesStagedMass);
            Assert.Equal(0.0, p.ModulesUnstagedMass);
            Assert.Equal(0.0, p.DisabledResourcesMass);
            Assert.Equal(0.0, p.EngineResiduals);
            Assert.False(p.IsRoot);
            Assert.False(p.IsLaunchClamp);
            Assert.False(p.IsEngine);
        }
    }
}
