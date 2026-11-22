/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>The switch and check box (<see cref="ToggleModel"/>) and the three-position switch (<see cref="Switch3Model"/>).</summary>
    public class ToggleModelTests
    {
        [Fact]
        public void AClickAndAnAgentsFlipBothCallTheSetterOnceWithTheirSource()
        {
            bool on      = false;
            var  sources = new List<ChangeSource>();
            var  m = new ToggleModel(ToggleKind.Switch, () => on, (v, s) =>
            {
                on = v;
                sources.Add(s);
            });
            m.Set(true, ChangeSource.Pointer);
            m.Flip(ChangeSource.Agent);
            Assert.False(on);
            AssertionExtensions.EqualSequence(new[] { ChangeSource.Pointer, ChangeSource.Agent }, sources);
        }

        [Fact]
        public void FlipTurnsTheValueAsItIsNowNotAsLastDrawn()
        {
            bool on = false;
            var  m  = new ToggleModel(ToggleKind.CheckBox, () => on, (v, s) => on = v);
            on = true; // changed since the last sync
            m.Flip(ChangeSource.Agent);
            Assert.False(on);
        }

        [Fact]
        public void ARefusedChangeCallsNothingAndTheDrawnStateStays()
        {
            bool on    = false;
            int  calls = 0;
            var m = new ToggleModel(ToggleKind.Switch, () => on, (v, s) =>
            {
                calls++;
                on = v;
            }, () => false);
            Assert.False(m.Set(true, ChangeSource.Pointer));
            Assert.Equal(0, calls);
            Assert.False(m.On);
        }

        [Fact]
        public void ASetterThatKeepsTheOldStateShowsItAtOnce()
        {
            bool on = false;
            var  m  = new ToggleModel(ToggleKind.Switch, () => on, (v, s) => { });
            m.Set(true, ChangeSource.Pointer);
            Assert.False(m.On);
        }

        [Fact]
        public void SpaceAndEnterFlipAndOtherKeysAreNotHandled()
        {
            bool on = false;
            var  m  = new ToggleModel(ToggleKind.Switch, () => on, (v, s) => on = v);
            Assert.True(m.Key(PartKey.Space));
            Assert.True(on);
            Assert.True(m.Key(PartKey.Enter));
            Assert.False(on);
            Assert.False(m.Key(PartKey.Left));
            Assert.Equal(ChangeSource.Keyboard, m.Value.LastSource);
        }

        [Fact]
        public void ASwitchDescribesASwitchAndACheckBoxACheckBoxOnWhenOn()
        {
            var parts = new List<PartDescription>();
            new ToggleModel(ToggleKind.Switch, () => true, (v, s) => { }) { Label = "a" }.Describe(parts);
            new ToggleModel(ToggleKind.CheckBox, () => false, (v, s) => { }) { Label = "b" }.Describe(parts);
            Assert.Equal(PartRole.Switch, parts[0].Role);
            Assert.True(parts[0].Has(PartFlags.On));
            Assert.Equal(PartRole.CheckBox, parts[1].Role);
            Assert.False(parts[1].Has(PartFlags.On));
        }

        private static readonly string[] Names = { "Off", "Auto", "On" };

        [Fact]
        public void PickingAnotherPositionCallsTheSetterOnce()
        {
            int at    = 0;
            int calls = 0;
            var m = new Switch3Model(Names, () => at, (v, s) =>
            {
                calls++;
                at = v;
            });
            Assert.True(m.Pick(2, ChangeSource.Pointer));
            Assert.Equal(2, at);
            Assert.Equal(1, calls);
            Assert.Equal("On", m.ShownName);
        }

        [Fact]
        public void PickingThePositionShownCallsNothing()
        {
            int calls = 0;
            var m     = new Switch3Model(Names, () => 1, (v, s) => calls++);
            Assert.False(m.Pick(1, ChangeSource.Pointer));
            Assert.Equal(0, calls);
        }

        [Fact]
        public void APositionOutsideTheSwitchIsRefused()
        {
            int calls = 0;
            var m     = new Switch3Model(Names, () => 0, (v, s) => calls++);
            Assert.False(m.Pick(3, ChangeSource.Agent));
            Assert.False(m.Pick(-1, ChangeSource.Agent));
            Assert.Equal(0, calls);
        }

        [Fact]
        public void LeftAndRightMoveByOneWithoutWrapping()
        {
            int at = 2;
            var m  = new Switch3Model(Names, () => at, (v, s) => at = v);
            Assert.True(m.Key(PartKey.Right));
            Assert.Equal(2, at);
            m.Key(PartKey.Left);
            Assert.Equal(1, at);
            m.Key(PartKey.Home);
            Assert.Equal(0, at);
        }

        [Fact]
        public void TheSwitch3DescribesOneRadioOnForThePositionShown()
        {
            var m = new Switch3Model(Names, () => 1, (v, s) => { }) { Label = "Mode" };
            Assert.Empty(Patterns.Check(m));
            var parts = new List<PartDescription>();
            m.Describe(parts);
            Assert.True(parts[2].Has(PartFlags.On));
        }
    }
}
