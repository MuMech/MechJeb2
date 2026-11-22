/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.PSG;
using Xunit;
using Member = MechJebLib.PSG.PadStructure.Member;
using Section = MechJebLib.PSG.PadStructure.Section;

namespace MechJebLibTest.PSGTests
{
    public class PadStructureTests
    {
        private static Member Clamp(int stage, int dropped) => new Member(true, false, stage, dropped);
        private static Member Engine(int dropped) => new Member(false, true, 6, dropped);
        private static Member Other(int dropped) => new Member(false, false, 0, dropped);

        private static bool[] Find(params Member[] members)
        {
            bool[] left = new bool[members.Length];
            PadStructure.Find(members, left);
            return left;
        }

        [Fact]
        public void TowerOnAClampStaysOnThePad()
        {
            // the payload, the booster, the clamp and the tower on it
            bool[] left = Find(Other(-1), Engine(4), Clamp(5, 5), Other(5));

            Assert.Equal(new[] { false, false, true, true }, left);
        }

        [Fact]
        public void StandBetweenTheRocketAndTheClampStaysOnThePad()
        {
            // the stand is the clamp's parent, decoupled from the rocket as the clamp releases, with a hold-down arm on it
            bool[] left = Find(Other(-1), Engine(4), Other(5), Clamp(5, 5), Other(5));

            Assert.Equal(new[] { false, false, true, true, true }, left);
        }

        [Fact]
        public void StructureDroppedBeforeTheClampsReleaseStaysOnThePad()
        {
            bool[] left = Find(Engine(4), Other(7), Clamp(5, 5));

            Assert.Equal(new[] { false, true, true }, left);
        }

        [Fact]
        public void StructureDroppedWithTheFirstOfTwoClampReleasesStaysOnThePad()
        {
            bool[] left = Find(Engine(3), Clamp(6, 6), Other(6), Clamp(5, 5), Other(5));

            Assert.Equal(new[] { false, true, true, true, true }, left);
        }

        [Fact]
        public void StagesDroppedInFlightStayOnTheRocket()
        {
            bool[] left = Find(Other(-1), Engine(2), Engine(4), Other(3), Clamp(5, 5));

            Assert.Equal(new[] { false, false, false, false, true }, left);
        }

        [Fact]
        public void WithoutAClampNothingStaysOnThePad()
        {
            bool[] left = Find(Other(-1), Engine(4), Other(6));

            Assert.Equal(new[] { false, false, false }, left);
        }

        [Fact]
        public void WhenNoEngineWouldLeaveThePadOnlyTheClampsStay()
        {
            // the vessel's root is on the pad's side, so the whole rocket reads as dropped with the clamps
            bool[] left = Find(Other(5), Engine(5), Clamp(5, 5));

            Assert.Equal(new[] { false, false, true }, left);
        }

        [Fact]
        public void OnlyPartsBesidesTheClampsCountAsStructure()
        {
            Assert.False(PadStructure.Find(new[] { Engine(4), Clamp(5, 5) }, new bool[2]));
            Assert.True(PadStructure.Find(new[] { Engine(4), Clamp(5, 5), Other(5) }, new bool[3]));
        }

        [Fact]
        public void FrontalAreaOfAStackIsItsWidestPart()
        {
            double area = PadStructure.FrontalArea(new[] { new Section(0, 0, 7), new Section(0, 0, 25), new Section(0, 0, 9.7) });

            area.ShouldEqual(25);
        }

        [Fact]
        public void FrontalAreaLeavesOutWhatIsWithinAWiderPart()
        {
            // engines and fairing halves, centred off the axis but behind the tank
            double area = PadStructure.FrontalArea(new[]
            {
                new Section(1.3, 0, 5.3), new Section(-1.3, 0, 5.3), new Section(0, 0, 10.7), new Section(0, 1.5, 1.2)
            });

            area.ShouldEqual(10.7);
        }

        [Fact]
        public void FrontalAreaAddsBoostersBesideTheCore()
        {
            double area = PadStructure.FrontalArea(new[] { new Section(2, 0, 1.25), new Section(0, 0, 5), new Section(-2, 0, 1.25) });

            area.ShouldEqual(7.5);
        }

        [Fact]
        public void FrontalAreaOfNothingIsZero() => PadStructure.FrontalArea(new Section[0]).ShouldEqual(0);

        [Fact]
        public void AeroIsNotReadBeforeItHasDroppedTheStructure()
        {
            var release = new PadRelease();
            release.Attached(292.4);
            release.Released(100);

            Assert.False(release.CanRead(100, 292.4));
            // a rebuild of the same shape moves the area a little
            Assert.False(release.CanRead(102, 292.0));
        }

        [Fact]
        public void AeroIsReadOnceItHasDroppedTheStructure()
        {
            var release = new PadRelease();
            release.Attached(292.4);
            release.Released(100);

            Assert.True(release.CanRead(101, 24.3));
        }

        [Fact]
        public void AeroIsReadAfterAWhileWhenTheAreaCannotShowTheRelease()
        {
            // pad parts narrower than the rocket leave the area as it was
            var release = new PadRelease();
            release.Attached(24.3);
            release.Released(100);

            Assert.False(release.CanRead(102, 24.3));
            Assert.True(release.CanRead(105, 24.3));
        }

        [Fact]
        public void AeroWithoutAnAreaYetIsNotRead()
        {
            var release = new PadRelease();
            release.Attached(292.4);
            release.Released(100);

            Assert.False(release.CanRead(101, 0));
        }

        [Fact]
        public void AeroIsNotReadWhileTheStructureIsAttached()
        {
            var release = new PadRelease();
            release.Attached(292.4);

            Assert.False(release.CanRead(1000, 24.3));
        }

        [Fact]
        public void AeroIsAlwaysReadableWithoutPadStructure() => Assert.True(new PadRelease().CanRead(0, 24.3));

        [Fact]
        public void ResetForgetsTheStructure()
        {
            var release = new PadRelease();
            release.Attached(292.4);
            release.Reset();

            Assert.True(release.CanRead(0, 292.4));
            Assert.False(release.Pending);
        }
    }
}
