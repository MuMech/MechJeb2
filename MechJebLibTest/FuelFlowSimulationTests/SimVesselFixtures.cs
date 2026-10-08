/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Linq;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Primitives;
using static MechJebLibTest.FuelFlowSimulationTests.PartFixtures;
using static MechJebLibTest.FuelFlowSimulationTests.RP1PartFixtures;

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
        ///     An RP-1 Falcon 9 Block 5 with a lead ballast payload under a procedural fairing, sitting in eight launch clamps on
        ///     the pad.  The nine Merlin 1Ds are in stage 4 and the launch clamps in stage 3.  Stage 2 drops the first stage at
        ///     the interstage and lights the Merlin 1D Vacuum, whose ullage is settled by four cold gas RCS thrusters.  Stage 1
        ///     jettisons the payload fairing and stage 0 separates the payload.  This is
        ///     MechJebLibTest/Craft/RP-1/My Falcon 9 Block 5.craft, as captured in Captures/MyFalcon9Block5.out.
        /// </summary>
        public static SimVessel MyFalcon9Block5(bool kscPad = false)
        {
            SimVessel v = NewVessel(5);
            v.SetInitial(671824.00810271199,
                new V3(3328117.6811200185, -4495382.4039072869, 3050619.8108357675),
                new V3(327.80846173363119, 242.69017320559482, -1.1649856299288815E-07),
                new V3(0.52232058711411489, -0.70562870200873251, 0.47882080078125));

            SimPart payload     = ROTServiceModule(v, 716676487, -0.91552984714508057, 659.92299999999966);
            SimPart avionics2   = ProceduralAvionics(v, 3578329919, -1, -0.70491535891778767, 111778, 150);
            SimPart fairingBase = KzResizableFairingBase(v, 711105664, 0, 0.53969687223434448, avionics2);
            SimPart tank2       = ROTGenericTankSeparate(v, 2593943298, 0, 1.9433658123016357, 38750.520237705852, 61199.234562294143);
            SimPart mvac = ROEMerlin1DV(v, 248665222, 2,
                new V3(0.52153756579203336, -0.70620775913935407, 0.47882074117660522));
            SimPart interstage = KzFlatAdapter(v, 4238809487, 2, 0.42880785465240479, mvac);
            SimPart avionics1  = ProceduralAvionics(v, 1573872723, 2, -0.36856828560121357, 268317, 600);
            SimPart tank1 = ROTGenericTankSeparate(v, 1975695187, 2, 11.127128601074219, 149242.99723951373, 235701.53739390321);
            SimPart thrustPlate = KzThrustPlate(v, 3628547826, 2, 0.74751102924346924);

            SimPart[] merlins =
            {
                ROEMerlin1D(v, 3766919695, 4, new V3(0.52153756579203336, -0.70620775913935407, 0.47882074117660522)),
                ROEMerlin1D(v, 1520557324, 4, new V3(0.52153756579203336, -0.70620775913935407, 0.47882074117660522)),
                ROEMerlin1D(v, 2691955204, 4, new V3(0.52153769716744469, -0.70620778159085063, 0.47882071137428284)),
                ROEMerlin1D(v, 75275086, 4, new V3(0.52153757701778169, -0.70620769345164847, 0.47882065176963806)),
                ROEMerlin1D(v, 4036330361, 4, new V3(0.5215374568681187, -0.7062076053124462, 0.47882062196731567)),
                ROEMerlin1D(v, 3933714968, 4, new V3(0.52153730304121071, -0.70620771423636086, 0.47882068157196045)),
                ROEMerlin1D(v, 2052249669, 4, new V3(0.52153741712213852, -0.70620767964739362, 0.47882068157196045)),
                ROEMerlin1D(v, 4132307635, 4, new V3(0.52153757701778169, -0.70620769345164847, 0.47882065176963806)),
                ROEMerlin1D(v, 957131212, 4, new V3(0.52153736008167462, -0.70620769694187724, 0.47882059216499329))
            };

            SimPart[] clamps =
            {
                RP1LaunchClamp1(v, 1950391702, 3), RP1LaunchClamp1(v, 536336776, 3), RP1LaunchClamp1(v, 3329569423, 3),
                RP1LaunchClamp1(v, 3966630377, 3), RP1LaunchClamp1(v, 3977908051, 3), RP1LaunchClamp1(v, 219405182, 3),
                RP1LaunchClamp1(v, 3483319384, 3), RP1LaunchClamp1(v, 26648143, 3)
            };

            SimPart[] interstageFairing =
            {
                KzProcFairingSide1(v, 1110556197, 2, 0.44426935911178589), KzProcFairingSide1(v, 84964491, 2, 0.44426935911178589),
                KzProcFairingSide1(v, 3892549061, 2, 0.44426935911178589), KzProcFairingSide1(v, 613648638, 2, 0.44426935911178589)
            };

            SimPart[] nitrogenTanks =
            {
                RORFTankIntegral(v, 1519049209, 0, -0.99626743793487549, 44323.939999999995),
                RORFTankIntegral(v, 1893542743, 0, -0.99626743793487549, 44323.939999999995),
                RORFTankIntegral(v, 1556299446, 0, -0.98235255479812622, 44323.939999999995),
                RORFTankIntegral(v, 2210098343, 0, -0.99626743793487549, 44323.939999999995)
            };

            SimPart[] rcs =
            {
                TE219F9CGT(v, 1552971870, 2), TE219F9CGT(v, 3144618445, 2), TE219F9CGT(v, 2229477872, 2), TE219F9CGT(v, 2659758618, 2)
            };

            SimPart[] payloadFairing =
            {
                KzProcFairingSide1(v, 288594212, 1, 0.82329875230789185), KzProcFairingSide1(v, 3427567844, 1, 0.82329875230789185)
            };

            MakeRoot(payload, "My Falcon 9 Block 5");

            Link(payload, avionics2);
            Link(avionics2, fairingBase);
            Link(fairingBase, tank2);
            Link(tank2, mvac);
            Link(mvac, interstage);
            Link(interstage, avionics1);
            Link(avionics1, tank1);
            Link(tank1, thrustPlate);
            foreach (SimPart p in merlins)
                Link(thrustPlate, p);
            foreach (SimPart p in clamps)
                Link(tank1, p);
            foreach (SimPart p in interstageFairing)
                Link(interstage, p);
            foreach (SimPart p in nitrogenTanks)
                Link(tank2, p);
            foreach (SimPart p in rcs)
                Link(tank2, p);
            foreach (SimPart p in payloadFairing)
                Link(fairingBase, p);

            CrossFeedSet(new[] { avionics2, payload, fairingBase }, payload, avionics2);
            CrossFeedSet(Concat(rcs.Reverse().ToArray(), nitrogenTanks.Reverse().ToArray(), new[] { mvac, tank2, interstage, fairingBase }),
                Concat(new[] { tank2, mvac }, nitrogenTanks, rcs));
            CrossFeedSet(new[] { thrustPlate, tank1, avionics1, interstage }, avionics1, tank1, thrustPlate);
            // each Merlin's set has the parts it is fed through, but not the other Merlins
            foreach (SimPart p in merlins)
                CrossFeedSet(new[] { p, thrustPlate, tank1, avionics1, interstage }, p);
            // the decouplers, launch clamps and fairings each have a set of their own
            foreach (SimPart p in Concat(new[] { fairingBase, interstage }, clamps, interstageFairing, payloadFairing))
                CrossFeed(p);

            // the center Merlin has no symmetry counterparts, the other eight are in a ring around it
            Symmetry(merlins.Skip(1).ToArray());
            Symmetry(clamps);
            Symmetry(interstageFairing);
            Symmetry(nitrogenTanks);
            Symmetry(rcs);
            Symmetry(payloadFairing);

            if (kscPad)
                v.SetConditions(0.95937552309112017, 0.98644521069944324, 1.5314373912311351E-09);

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
