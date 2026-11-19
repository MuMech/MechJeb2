/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Linq;
using MechJebLib.PSG;
using Xunit;
using static MechJebLibTest.PSGTests.SolutionTests;

namespace MechJebLibTest.PSGTests
{
    /// <summary>
    ///     SolutionBuilder.AnalyzeStages flags the phases for guidance from the optimizer's burn times and end masses.  The
    ///     phases are SolutionTests' normalized ones, and the burn times (s) and end masses (kg) are given in SI units.
    /// </summary>
    public class SolutionBuilderTests
    {
        // the burn time and the end mass the optimizer gave a phase
        private readonly struct Solved
        {
            public readonly double Bt;
            public readonly double Mf;

            public Solved(double bt, double mf)
            {
                Bt = bt;
                Mf = mf;
            }
        }

        private static Solved S(double bt, double mf) => new Solved(bt, mf);

        private static PhaseCollection Analyze(Phase[] phases, params Solved[] solved)
        {
            Assert.Equal(phases.Length, solved.Length);

            var collection = new PhaseCollection();
            collection.AddRange(phases);

            SolutionBuilder.AnalyzeStages(collection, solved.Select(s => s.Bt / TIME_SCALE).ToArray(),
                solved.Select(s => s.Mf / MASS_SCALE).ToArray());

            return collection;
        }

        private static string Flags(PhaseCollection phases) =>
            string.Join(" ", phases.Select(p => (p.Skip ? "S" : "-") + (p.TerminalStage ? "T" : "-") + (p.PreciseShutdown ? "P" : "-")));

        private static Phase DuringCoast(int kspStage, double m)
        {
            Phase coast = NewCoast(kspStage, kspStage, m);
            coast.MassContinuity = true;
            return coast;
        }

        private static Phase AfterCoast(Phase burn)
        {
            burn.MassContinuity = true;
            return burn;
        }

        // The zero coast constraint shrinks the burn before a coast-during to nothing with it: both are skipped, and guidance
        // flies the burn after the coast, which ends the ascent with a precise shutdown.
        [Fact]
        public void ZeroCoastDuringSkipsTheBurnBeforeIt()
        {
            PhaseCollection phases = Analyze(new[]
                {
                    NewBurn(2, 2, 4096, 2048, 300, 64, false),
                    NewBurn(1, 1, 1024, 256, 300, 64),
                    DuringCoast(1, 1024),
                    AfterCoast(NewBurn(1, 1, 1024, 256, 300, 64))
                },
                S(64, 2048), S(0, 1024), S(0, 1024), S(48, 400));

            Assert.Equal("--- S-- S-- -TP", Flags(phases));
        }

        // A coast-during whose burn before it isn't shrunk with it, which the zero coast constraint keeps from happening: the
        // coast is skipped all the same, and AnalyzeStages only warns.
        [Fact]
        public void SkippedCoastDuringAfterALongBurn()
        {
            PhaseCollection phases = Analyze(new[]
                {
                    NewBurn(1, 1, 1024, 256, 300, 64),
                    DuringCoast(1, 768),
                    AfterCoast(NewBurn(1, 1, 768, 256, 300, 64))
                },
                S(30, 768), S(0, 768), S(40, 400));

            Assert.Equal("--- S-- -TP", Flags(phases));
        }

        // The burn before a coast-during shrunk to nothing, with a coast that isn't: only the burn is skipped.
        [Fact]
        public void SkippedBurnBeforeACoast()
        {
            PhaseCollection phases = Analyze(new[]
                {
                    NewBurn(2, 2, 4096, 2048, 300, 64, false),
                    NewBurn(1, 1, 1024, 256, 300, 64),
                    DuringCoast(1, 1024),
                    AfterCoast(NewBurn(1, 1, 1024, 256, 300, 64))
                },
                S(64, 2048), S(0, 1024), S(20, 1024), S(40, 400));

            Assert.Equal("--- S-- --- -TP", Flags(phases));
        }

        // A coast-before shrunk to nothing, between a burn cut with propellant left and a fixed last stage
        [Fact]
        public void ZeroCoastBefore()
        {
            PhaseCollection phases = Analyze(new[]
                {
                    NewBurn(2, 2, 4096, 1024, 300, 64),
                    NewCoast(1, 1, 768),
                    NewBurn(1, 1, 768, 512, 300, 64, false)
                },
                S(32, 2048), S(0, 768), S(64, 512));

            Assert.Equal("--P S-- -T-", Flags(phases));
            Assert.True(phases[0].PropellantLeft);
        }

        // the stages after the last stage are pruned, coasts included, and never flown
        [Fact]
        public void PrunedStagesAreSkipped()
        {
            PhaseCollection phases = Analyze(new[]
                {
                    NewBurn(2, 2, 4096, 1024, 300, 64),
                    NewBurn(1, 1, 768, 512, 300, 64),
                    NewCoast(0, 0, 256),
                    NewBurn(0, 0, 256, 128, 300, 64)
                },
                S(32, 2048), S(0, 768), S(0, 256), S(0, 256));

            Assert.Equal("-TP S-- S-- S--", Flags(phases));
        }

        // The last stage is never skipped, however short: it is the end of the ascent, which terminal guidance trims.  Here the
        // burn before it burns out right at the target.
        [Fact]
        public void TheLastStageIsNeverSkipped()
        {
            PhaseCollection phases = Analyze(new[]
                {
                    NewBurn(1, 1, 1024, 256, 300, 64),
                    NewBurn(0, 0, 128, 64, 300, 64)
                },
                S(64, 256), S(0, 128));

            Assert.Equal("--- -TP", Flags(phases));
        }

        // a fixed stage too short to fly is skipped too
        [Fact]
        public void ShortFixedStageIsSkipped()
        {
            PhaseCollection phases = Analyze(new[]
                {
                    NewBurn(2, 2, 4096, 2048, 300, 64, false),
                    NewBurn(1, 1, 1024, 1000, 300, 0.05, false),
                    NewBurn(0, 0, 768, 256, 300, 64)
                },
                S(64, 2048), S(0.05, 1000), S(48, 400));

            Assert.Equal("--- S-- -TP", Flags(phases));
        }
    }
}
