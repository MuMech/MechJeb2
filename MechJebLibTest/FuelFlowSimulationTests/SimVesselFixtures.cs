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
    ///     SimVesselManager leaves it in, ready for the FuelFlowSimulation to run.
    /// </summary>
    public static class SimVesselFixtures
    {
        /// <summary>
        ///     A probe core, battery and SAS on two Rockomax64 tanks and a Mainsail, sitting in two launch clamps on the pad.
        ///     The Mainsail and the launch clamps are both in stage 0.  This is the stock
        ///     MechJebLibTest/Craft/Stock/Mainsail Tin Can.craft, as captured in Captures/MainsailTinCan.out.
        /// </summary>
        public static SimVessel MainsailTinCan()
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

            return Finish(v);
        }

        /// <summary>
        ///     The atmospheric conditions on the launchpad at KSC, as captured for the MainsailTinCan.
        /// </summary>
        public static void SetKSCPadConditions(SimVessel v) => v.SetConditions(0.915457633121104, 0.987155510143539, 9.52048848036624E-05);

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

        // KSP appends the vessel name to the name of the root part
        public static void MakeRoot(SimPart p, string vesselName)
        {
            p.IsRoot = true;
            p.Name   = $"{p.Name} ({vesselName})";
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
