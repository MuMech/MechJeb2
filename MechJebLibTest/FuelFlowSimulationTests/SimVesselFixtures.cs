/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Linq;
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
            v.SetInitial(105865.0041307488,
                new V3(576839.96092781203, -165406.15003102238, -1017.9253790531925),
                new V3(48.231074597909611, 168.18874204753263, -1.392905019470582E-05),
                new V3(0.9610545499785107, -0.27635343056128087, -0.0017315298318862915));

            SimPart probe    = ProbeStackLarge(v, 2919945081);
            SimPart noseCone = RocketNoseConeV3(v, 2612439557);
            SimPart battery  = BatteryBankLarge(v, 1169654648);
            SimPart sas      = AsasModule1_2(v, 2588065212);
            SimPart tank1    = Rockomax64BW(v, 706255026);
            SimPart tank2    = Rockomax64BW(v, 4252378878);
            SimPart engine = LiquidEngineMainsailV2(v, 477612636, 0,
                new V3(0.95751801278484683, -0.28836976344549975, -0.0016915356973186135));
            SimPart clamp1 = LaunchClamp1(v, 2115970530, 0);
            SimPart clamp2 = LaunchClamp1(v, 2632810960, 0);
            SimPart panel1 = LargeSolarPanel(v, 1331256762);
            SimPart panel2 = LargeSolarPanel(v, 3249199616);

            MakeRoot(probe);

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
            {
                v.SetConditions(0.9151522794796263, 0.98715392892182707, 1.0179783179606657E-05);
                // this capture's KSC pad dump comes from a later simulation cycle than its vacuum dump
                v.SetInitial(105865.10413074882,
                    new V3(576844.78344503092, -165389.33098735014, -1017.9253804278293),
                    new V3(48.226163231435962, 168.19014738799311, -1.4421056742189789E-05),
                    new V3(0.96106251319437375, -0.27632550333824962, -0.00173167884349823));
            }

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
            v.SetInitial(106246.4041308265,
                new V3(591631.40469863731, -100368.74324576411, -1017.9225292618587),
                new V3(29.268101349073248, 172.5015596229764, -8.7618949349102626E-06),
                new V3(0.98573276206411742, -0.16830977108617634, -0.0016990453004837036));

            var thrustDirection = new V3(0.98481306835897642, -0.17361188227317309, -0.0016915356973186135);

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
            {
                v.SetConditions(0.91403608176848128, 0.98738998273456668, 1.0267795926164918E-05);
                // this capture's KSC pad dump comes from a later simulation cycle than its vacuum dump
                v.SetInitial(106246.52413082652,
                    new V3(591634.91607620171, -100348.04294868605, -1017.9225288217948),
                    new V3(29.262013680049563, 172.50225809072387, -1.6826553830632715E-06),
                    new V3(0.98573675156092799, -0.16828576092558789, -0.0016986876726150513));
            }

            return Finish(v);
        }

        /// <summary>
        ///     The MainsailTinCan core with two pairs of asparagus staged boosters, each a Rockomax64 pair and a Mainsail on a
        ///     radial decoupler, held by a launch clamp, with a fuel line feeding the stage inboard of it.  All five Mainsails
        ///     are in stage 3 and the launch clamps in stage 2, the outer pair of boosters is dropped in stage 1 and the inner
        ///     pair in stage 0.  This is the stock MechJebLibTest/Craft/Stock/Mainsail Tin Can Asparagus.craft, as captured in
        ///     Captures/MainsailTinCanAsparagus.out.
        /// </summary>
        public static SimVessel MainsailTinCanAsparagus(bool kscPad = false)
        {
            SimVessel v = NewVessel(4);
            v.SetInitial(105815.06413073862,
                new V3(574370.49008105334, -173787.77917027441, -1017.9253035871751),
                new V3(50.526540503692551, 167.50774574574771, -0.0015525435741062415),
                new V3(0.95663945646028403, -0.29127067404789669, -0.0016749948263168335));

            SimPart probe      = ProbeStackLarge(v, 3081765285);
            SimPart noseCone   = RocketNoseConeV3(v, 2058457420);
            SimPart battery    = BatteryBankLarge(v, 3464832802);
            SimPart sas        = AsasModule1_2(v, 1568787730);
            SimPart panel1     = LargeSolarPanel(v, 986793881);
            SimPart panel2     = LargeSolarPanel(v, 2039583872);
            SimPart coreTank1  = Rockomax64BW(v, 4249717286);
            SimPart coreTank2  = Rockomax64BW(v, 2897112194);
            SimPart coreEngine = LiquidEngineMainsailV2(v, 5718290, 3,
                new V3(0.95671721860040637, -0.29101534458903305, -0.0016915164887905121));
            SimPart strut1 = StrutConnector(v, 4035748844);
            SimPart strut2 = StrutConnector(v, 4236372261);
            SimPart strut3 = StrutConnector(v, 664156568);
            SimPart strut4 = StrutConnector(v, 3338245800);

            SimPart inADecoupler = RadialDecoupler2(v, 1342711734, 0, coreTank1);
            SimPart inATank1     = Rockomax64BW(v, 3436666188, 0);
            SimPart inATank2     = Rockomax64BW(v, 1491235714, 0);
            SimPart inAEngine = LiquidEngineMainsailV2(v, 1387000143, 3,
                new V3(0.95671721860040637, -0.29101534458903305, -0.0016915164887905121));
            SimPart inAFuelLine = FuelLine(v, 2825487276, 0);
            SimPart inANoseCone = RocketNoseConeV3(v, 2107898442, 0);
            SimPart inAClamp    = LaunchClamp1(v, 1834761637, 2);

            SimPart inBDecoupler = RadialDecoupler2(v, 338597663, 0, coreTank1);
            SimPart inBTank1     = Rockomax64BW(v, 2903300948, 0);
            SimPart inBTank2     = Rockomax64BW(v, 2835658593, 0);
            SimPart inBEngine = LiquidEngineMainsailV2(v, 904352827, 3,
                new V3(0.95671727538909312, -0.29101532648492462, -0.0016915581654757261));
            SimPart inBFuelLine = FuelLine(v, 2565553530, 0);
            SimPart inBNoseCone = RocketNoseConeV3(v, 1781570610, 0);
            SimPart inBClamp    = LaunchClamp1(v, 2147514922, 2);

            SimPart outADecoupler = RadialDecoupler2(v, 2520312754, 1, coreTank1);
            SimPart outATank1     = Rockomax64BW(v, 1315697509, 1);
            SimPart outATank2     = Rockomax64BW(v, 3207382999, 1);
            SimPart outAEngine = LiquidEngineMainsailV2(v, 4182532358, 3,
                new V3(0.95671731407367133, -0.29101525159212938, -0.0016914904117584229));
            SimPart outAFuelLine = FuelLine(v, 1428295298, 1);
            SimPart outANoseCone = RocketNoseConeV3(v, 3121470973, 1);
            SimPart outAClamp    = LaunchClamp1(v, 2660487534, 2);

            SimPart outBDecoupler = RadialDecoupler2(v, 853805331, 1, coreTank1);
            SimPart outBTank1     = Rockomax64BW(v, 655026546, 1);
            SimPart outBTank2     = Rockomax64BW(v, 3074366510, 1);
            SimPart outBEngine = LiquidEngineMainsailV2(v, 2186939596, 3,
                new V3(0.95671729349320156, -0.29101538327361126, -0.0016914308071136475));
            SimPart outBFuelLine = FuelLine(v, 3687288196, 1);
            SimPart outBNoseCone = RocketNoseConeV3(v, 2745859995, 1);
            SimPart outBClamp    = LaunchClamp1(v, 1622391962, 2);

            MakeRoot(probe, "Mainsail Tin Can Asparagus");

            Link(probe, noseCone);
            Link(probe, battery);
            Link(battery, sas);
            Link(sas, panel1);
            Link(sas, panel2);
            Link(sas, coreTank1);
            Link(coreTank1, coreTank2);
            Link(coreTank2, coreEngine);
            Link(coreTank2, strut1);
            Link(coreTank2, strut2);
            Link(coreTank2, strut3);
            Link(coreTank2, strut4);
            LinkBooster(coreTank1, inADecoupler, inATank1, inATank2, inAEngine, inAFuelLine, inANoseCone, inAClamp);
            LinkBooster(coreTank1, inBDecoupler, inBTank1, inBTank2, inBEngine, inBFuelLine, inBNoseCone, inBClamp);
            LinkBooster(coreTank1, outADecoupler, outATank1, outATank2, outAEngine, outAFuelLine, outANoseCone, outAClamp);
            LinkBooster(coreTank1, outBDecoupler, outBTank1, outBTank2, outBEngine, outBFuelLine, outBNoseCone, outBClamp);

            // the fuel lines make each set reach the booster outboard of it, so the core's set reaches every booster
            SimPart[] inA  = { inAFuelLine, inAEngine, inATank1, inATank2, inANoseCone };
            SimPart[] inB  = { inBFuelLine, inBEngine, inBTank1, inBTank2, inBNoseCone };
            SimPart[] outA = { outAFuelLine, outAEngine, outATank1, outATank2, outANoseCone };
            SimPart[] outB = { outBFuelLine, outBEngine, outBTank1, outBTank2, outBNoseCone };

            CrossFeedSet(Concat(new[] { panel2, panel1, coreEngine, coreTank2, coreTank1, sas, battery, probe }, inA, outA, inB, outB, new[] { noseCone }),
                probe, battery, sas, panel1, panel2, coreTank1, coreTank2, coreEngine);
            CrossFeedSet(Concat(inA, outA), inATank1, inATank2, inAEngine, inAFuelLine);
            CrossFeedSet(Concat(inB, outB), inBTank1, inBTank2, inBEngine, inBFuelLine);
            CrossFeedSet(outA, outATank1, outATank2, outAEngine, outAFuelLine);
            CrossFeedSet(outB, outBTank1, outBTank2, outBEngine, outBFuelLine);

            // the nose cones, struts, decouplers and launch clamps each have a set of their own
            foreach (SimPart p in new[]
                     {
                         noseCone, strut1, strut2, strut3, strut4, inADecoupler, inANoseCone, inAClamp, inBDecoupler, inBNoseCone, inBClamp,
                         outADecoupler, outANoseCone, outAClamp, outBDecoupler, outBNoseCone, outBClamp
                     })
                CrossFeed(p);

            Symmetry(panel1, panel2);
            Symmetry(strut1, strut2);
            Symmetry(strut3, strut4);
            Symmetry(inADecoupler, inBDecoupler);
            Symmetry(inATank1, inBTank1);
            Symmetry(inATank2, inBTank2);
            Symmetry(inAEngine, inBEngine);
            Symmetry(inAFuelLine, inBFuelLine);
            Symmetry(inANoseCone, inBNoseCone);
            Symmetry(inAClamp, inBClamp);
            Symmetry(outADecoupler, outBDecoupler);
            Symmetry(outATank1, outBTank1);
            Symmetry(outATank2, outBTank2);
            Symmetry(outAEngine, outBEngine);
            Symmetry(outAFuelLine, outBFuelLine);
            Symmetry(outANoseCone, outBNoseCone);
            Symmetry(outAClamp, outBClamp);

            if (kscPad)
                v.SetConditions(0.91531713469371678, 0.98713962263014587, 0.00044246903148607955);

            return Finish(v);
        }

        // links a radial booster below the core part its decoupler is attached to, in the order KSP lists the booster's parts
        private static void LinkBooster(SimPart core, SimPart decoupler, SimPart tank1, SimPart tank2, SimPart engine, SimPart fuelLine,
            SimPart noseCone, SimPart clamp)
        {
            Link(core, decoupler);
            Link(decoupler, tank1);
            Link(tank1, tank2);
            Link(tank2, engine);
            Link(tank2, fuelLine);
            Link(tank1, noseCone);
            Link(tank1, clamp);
        }

        private static SimPart[] Concat(params SimPart[][] sets) => sets.SelectMany(s => s).ToArray();

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

        // KSP appends the vessel name to the name of the root part in some captures but not others, even between captures of
        // the same vessel, so this follows whatever the capture has
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

        // gives each of the parts the crossfeed set made of the parts
        public static void CrossFeed(params SimPart[] parts) => CrossFeedSet(parts, parts);

        // gives each of the members the crossfeed set, in the order KSP lists it, which may include parts that have sets of
        // their own
        public static void CrossFeedSet(SimPart[] set, params SimPart[] members)
        {
            foreach (SimPart p in members)
            {
                p.CrossFeedPartSet.Clear();
                p.CrossFeedPartSet.AddRange(set);
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
