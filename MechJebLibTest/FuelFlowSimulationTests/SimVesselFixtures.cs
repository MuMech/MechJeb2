/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.FuelFlowSimulation;
using MechJebLib.Primitives;
using static MechJebLibTest.FuelFlowSimulationTests.PartFixtures;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    /// <summary>
    ///     Whole SimVessels, reproducing SimVessel dumps captured in KSP.  These return the vessel in the state the KSP
    ///     SimVesselManager leaves it in, ready for the FuelFlowSimulation to run.  The captures hold a dump in vacuum
    ///     followed by a dump in the atmospheric conditions on the launchpad at KSC, which kscPad selects.
    /// </summary>
    public static class SimVesselFixtures
    {
        /// <summary>
        ///     A probe core, battery and SAS on two Rockomax64 tanks and a Mainsail, sitting in two launch clamps on the pad.
        ///     The Mainsail and the launch clamps are both in stage 0.  This is the stock
        ///     MechJebLibTest/Craft/Stock/Mainsail Tin Can.craft, as captured in Captures/MainsailTinCan.out.
        /// </summary>
        public static SimVessel MainsailTinCan(bool kscPad = false)
        {
            SimVessel v = NewVessel(1);
            v.SetInitial(105778.724130731,
                new V3(572496.79306991608, -179863.68143572571, -1017.9262911602394),
                new V3(52.443092987494708, 166.92443898866966, -0.00043090463143150536),
                new V3(0.95377818018984117, -0.30050392564635986, -0.0022021681070327759));

            SimPart probe    = ProbeStackLarge(v, 1034108735);
            SimPart noseCone = RocketNoseConeV3(v, 1642962949);
            SimPart battery  = BatteryBankLarge(v, 215457040);
            SimPart sas      = AsasModule1_2(v, 1619319857);
            SimPart tank1    = Rockomax64BW(v, 367462471);
            SimPart tank2    = Rockomax64BW(v, 2907379357);
            SimPart engine = LiquidEngineMainsailV2(v, 3382441717, 0,
                new V3(0.95359810031390801, -0.30107585837918505, -0.0021386512089520693));
            SimPart clamp1 = LaunchClamp1(v, 2115970530, 0);
            SimPart clamp2 = LaunchClamp1(v, 2632810960, 0);
            SimPart panel1 = LargeSolarPanel(v, 1848261757);
            SimPart panel2 = LargeSolarPanel(v, 246217793);

            MakeRoot(probe, "Mainsail Tin Can");

            Link(probe, noseCone);
            Link(probe, battery);
            Link(battery, sas);
            Link(sas, tank1);
            Link(sas, panel1);
            Link(sas, panel2);
            Link(tank1, tank2);
            Link(tank2, engine);
            Link(tank2, clamp1);
            Link(tank2, clamp2);

            CrossFeed(panel2, panel1, engine, tank2, tank1, sas, battery, probe, noseCone);
            // KSP puts the nose cone in the set of the parts below it, but gives the nose cone a set of its own
            CrossFeed(noseCone);
            CrossFeed(clamp1);
            CrossFeed(clamp2);

            Symmetry(clamp1, clamp2);
            Symmetry(panel1, panel2);

            if (kscPad)
                v.SetConditions(0.915457633121104, 0.987155510143539, 9.52048848036624E-05);

            return Finish(v);
        }

        /// <summary>
        ///     The MainsailTinCan with a second Rockomax64 and Mainsail below it as a lower stage, separated by a decoupler and
        ///     sitting in two launch clamps on the pad.  The lower Mainsail is in stage 3, the launch clamps in stage 2, the
        ///     decoupler in stage 1 and the upper Mainsail in stage 0.  This is the stock
        ///     MechJebLibTest/Craft/Stock/Mainsail Tin Can 2 Stage.craft, as captured in Captures/MainsailTinCan2Stage.out.
        /// </summary>
        public static SimVessel MainsailTinCan2Stage(bool kscPad = false)
        {
            SimVessel v = NewVessel(4);
            v.SetInitial(105794.044130734,
                new V3(573292.96945599711, -177304.16708120424, -1017.9209188676783),
                new V3(51.699900549319686, 167.15360977544978, 0.00096893554437770674),
                new V3(0.95495424609258295, -0.29675148235150173, -0.0010958015918731689));

            var thrustDirection = new V3(0.95435312712642562, -0.29867703966168524, -0.0016915356973186135);

            SimPart probe       = ProbeStackLarge(v, 363062213);
            SimPart noseCone    = RocketNoseConeV3(v, 680279770);
            SimPart battery     = BatteryBankLarge(v, 2696621395);
            SimPart sas         = AsasModule1_2(v, 3652483317);
            SimPart upperTank   = Rockomax64BW(v, 1086072213);
            SimPart upperEngine = LiquidEngineMainsailV2(v, 1899096253, 0, thrustDirection);
            SimPart decoupler   = Decoupler2(v, 3302637856, 1, upperEngine);
            SimPart lowerTank   = Rockomax64BW(v, 186459485, 1);
            SimPart lowerEngine = LiquidEngineMainsailV2(v, 296107038, 3, thrustDirection);
            SimPart clamp1      = LaunchClamp1(v, 3767408006, 2);
            SimPart clamp2      = LaunchClamp1(v, 3106617278, 2);
            SimPart panel1      = LargeSolarPanel(v, 1281754298);
            SimPart panel2      = LargeSolarPanel(v, 3802754131);

            MakeRoot(probe);

            Link(probe, noseCone);
            Link(probe, battery);
            Link(battery, sas);
            Link(sas, upperTank);
            Link(sas, panel1);
            Link(sas, panel2);
            Link(upperTank, upperEngine);
            Link(upperEngine, decoupler);
            Link(decoupler, lowerTank);
            Link(lowerTank, lowerEngine);
            Link(lowerTank, clamp1);
            Link(lowerTank, clamp2);

            CrossFeed(panel2, panel1, upperEngine, upperTank, sas, battery, probe, decoupler, noseCone);
            CrossFeed(lowerEngine, lowerTank, decoupler);
            // KSP puts the nose cone and the decoupler in the sets of the parts next to them, but gives each a set of its own
            CrossFeed(noseCone);
            CrossFeed(decoupler);
            CrossFeed(clamp1);
            CrossFeed(clamp2);

            Symmetry(clamp1, clamp2);
            Symmetry(panel1, panel2);

            if (kscPad)
                v.SetConditions(0.915582911665934, 0.987390810993559, 1.11535089238807E-05);

            return Finish(v);
        }

        /// <summary>
        ///     Borrows a SimVessel and sets every field that the KSP SimVesselManager would set, since pooled SimVessels
        ///     are not reset when they are released.  The conditions are vacuum and the initial state is zeros.
        /// </summary>
        public static SimVessel NewVessel(int currentStage)
        {
            SimVessel v = SimVessel.Borrow();
            v.HasLaunchClamp = false;
            v.MainThrottle   = 1.0;
            v.SetCurrentStage(currentStage);
            v.SetConditions(0, 0, 0);
            v.SetInitial(0, V3.zero, V3.zero, V3.zero);
            return v;
        }

        /// <summary>
        ///     Does what the KSP SimVesselManager does after it has built the parts and copied the part topology.
        /// </summary>
        public static SimVessel Finish(SimVessel v)
        {
            DecouplingAnalyzer.Analyze(v);
            v.UpdateEngineSet();
            return v;
        }

        // the name of the root part has the vessel name appended in some captures (MainsailTinCan) but not in others
        // (MainsailTinCan2Stage)
        public static void MakeRoot(SimPart p, string? vesselName = null)
        {
            p.IsRoot = true;
            if (vesselName != null)
                p.Name = $"{p.Name} ({vesselName})";
        }

        // the SimPart.Links are the KSP parent followed by the KSP children, so link the tree from the root downwards
        public static void Link(SimPart parent, SimPart child)
        {
            parent.Links.Add(child);
            child.Links.Add(parent);
        }

        public static void CrossFeed(params SimPart[] parts)
        {
            foreach (SimPart p in parts)
            {
                p.CrossFeedPartSet.Clear();
                p.CrossFeedPartSet.AddRange(parts);
            }
        }

        public static void Symmetry(params SimPart[] parts)
        {
            foreach (SimPart p in parts)
            {
                p.SymmetryCounterParts.Clear();
                foreach (SimPart q in parts)
                    if (q != p)
                        p.SymmetryCounterParts.Add(q);
            }
        }
    }
}
