/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>The value model the input widgets share (<see cref="ValueModel{T}"/>), without KSP or Unity.</summary>
    public class ValueModelTests
    {
        private sealed class Box<T>
        {
            public          T                                   V;
            public readonly List<KeyValuePair<T, ChangeSource>> Calls = new List<KeyValuePair<T, ChangeSource>>();
            public          bool                                Keep; // the setter refuses: the value stays

            public Box(T v) => V = v;

            public void Set(T v, ChangeSource s)
            {
                Calls.Add(new KeyValuePair<T, ChangeSource>(v, s));
                if (!Keep) V = v;
            }
        }

        [Fact]
        public void SettingTheValueItHasStillCallsTheSetter()
        {
            var b = new Box<int>(3);
            var m = new ValueModel<int>(() => b.V, b.Set);
            Assert.True(m.Set(3, ChangeSource.Pointer));
            Assert.Single(b.Calls);
        }

        [Fact]
        public void ASetIsRefusedWithNothingCalledWhileReadOnly()
        {
            var  b        = new Box<int>(3);
            bool editable = false;
            var  m        = new ValueModel<int>(() => b.V, b.Set, () => editable);
            Assert.False(m.Set(4, ChangeSource.Pointer));
            Assert.Empty(b.Calls);
            editable = true;
            Assert.True(m.Set(4, ChangeSource.Pointer));
        }

        [Fact]
        public void AllowCanRefuseASetBySource()
        {
            var b = new Box<int>(3);
            var m = new ValueModel<int>(() => b.V, b.Set) { Allow = (v, s) => s != ChangeSource.Agent };
            Assert.False(m.Set(4, ChangeSource.Agent));
            Assert.True(m.Set(4, ChangeSource.Pointer));
            Assert.Single(b.Calls);
        }

        [Fact]
        public void TheSetterGetsTheSourceAndLastSourceAgrees()
        {
            var b = new Box<int>(3);
            var m = new ValueModel<int>(() => b.V, b.Set);
            m.Set(5, ChangeSource.Agent);
            Assert.Equal(ChangeSource.Agent, b.Calls[0].Value);
            Assert.Equal(ChangeSource.Agent, m.LastSource);
        }

        [Fact]
        public void ShownIsWhatTheSetterKeptRightAfterTheSet()
        {
            var b = new Box<int>(3) { Keep = true };
            var m = new ValueModel<int>(() => b.V, b.Set);
            m.Set(9, ChangeSource.Pointer);
            Assert.Equal(3, m.Shown);
        }

        [Fact]
        public void ShownChangedFiresAndVersionCountsOnEveryChangeWhoeverMadeIt()
        {
            var b     = new Box<int>(3);
            var m     = new ValueModel<int>(() => b.V, b.Set);
            int fired = 0;
            m.ShownChanged += () => fired++;
            m.Set(4, ChangeSource.Pointer);
            b.V = 7;
            Assert.True(m.Sync());
            Assert.False(m.Sync());
            Assert.Equal(2, fired);
            Assert.Equal(2, m.Version);
        }

        [Fact]
        public void ASetTheSetterKeepsTheOldValueForReturnsTrueWithNothingToRepaintSoTheHostDrawsAgain()
        {
            var b     = new Box<bool>(false) { Keep = true };
            var m     = new ValueModel<bool>(() => b.V, b.Set);
            int fired = 0;
            m.ShownChanged += () => fired++;
            Assert.True(m.Set(true, ChangeSource.Pointer));
            Assert.False(m.Shown);
            Assert.Equal(0, m.Version);
            Assert.Equal(0, fired);
        }
    }
}
