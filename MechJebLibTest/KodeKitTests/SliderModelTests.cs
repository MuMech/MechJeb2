/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>The slider (<see cref="SliderModel"/>).</summary>
    public class SliderModelTests
    {
        private static (List<float> sets, List<string> group, SliderModel m) Make(float step = 0, float min = 0, float max = 1)
        {
            var   sets  = new List<float>();
            var   group = new List<string>();
            float v     = 0.5f;
            var m = new SliderModel(() => v, (x, s) =>
            {
                v = x;
                sets.Add(x);
            }, min, max, step, () => group.Add("begin"), () => group.Add("end"));
            return (sets, group, m);
        }

        [Fact]
        public void AMoveIsClampedToTheRange()
        {
            (List<float> sets, _, SliderModel m) = Make();
            m.Set(3, ChangeSource.Pointer);
            m.Set(-2, ChangeSource.Pointer);
            Assert.Equal(new[] { 1f, 0f }, sets);
        }

        [Fact]
        public void WithAStepAMoveSnapsToTheGridFromMin()
        {
            (List<float> sets, _, SliderModel m) = Make(0.25f, 0.1f, 1.1f);
            m.Set(0.4f, ChangeSource.Pointer);
            Assert.Equal(0.35f, sets[0], 4);
        }

        [Fact]
        public void ANaNIsRefused()
        {
            (List<float> sets, _, SliderModel m) = Make();
            Assert.False(m.Set(float.NaN, ChangeSource.Agent));
            Assert.Empty(sets);
        }

        [Fact]
        public void ADragOpensTheGroupOnceAndClosesItOnce()
        {
            (List<float> sets, List<string> group, SliderModel m) = Make();
            Assert.True(m.BeginDrag());
            Assert.False(m.BeginDrag());
            m.Set(0.6f, ChangeSource.Pointer);
            m.Set(0.7f, ChangeSource.Pointer);
            Assert.True(m.EndDrag());
            Assert.False(m.EndDrag());
            Assert.Equal(new[] { "begin", "end" }, group);
            Assert.Equal(2, sets.Count);
        }

        [Fact]
        public void ADragOnACommitPathIsOneCommitTakingInThePress()
        {
            var   path = new CommitPath("w", () => 1);
            float v    = 0.5f;
            CommitTarget<float> t = path.Target("Slider", () => v, x => v = x);
            var m = new SliderModel(() => v, t.Set, 0, 1, 0, () => t.BeginGroup(true), t.EndGroup);
            var settled = new List<Commit>();
            path.Settled += settled.Add;
            m.Set(0.2f, ChangeSource.Pointer); // the press moves the slider before the drag begins
            m.BeginDrag();
            m.Set(0.3f, ChangeSource.Pointer);
            m.Set(0.4f, ChangeSource.Pointer);
            m.EndDrag();
            Commit last = settled[settled.Count - 1];
            Assert.Equal(0.4f, last.New);
            Assert.Same(settled[0], last.Replaces);
            Assert.Equal(0.5f, last.Old);
        }

        [Fact]
        public void KeysMoveByAStepOrAHundredthOfTheRangeAndPageKeysByTen()
        {
            (List<float> sets, _, SliderModel m) = Make();
            m.Key(PartKey.Right);
            Assert.Equal(0.51f, sets[0], 4);
            m.Key(PartKey.PageDown);
            Assert.Equal(0.41f, sets[1], 4);
            m.Key(PartKey.End);
            Assert.Equal(1f, sets[2]);
            Assert.Equal(ChangeSource.Keyboard, m.Value.LastSource);
            Assert.False(m.Key(PartKey.Enter));
        }

        [Fact]
        public void TheSliderIsGrabbedWhileDragged()
        {
            (_, _, SliderModel m) = Make();
            m.Label = "Opacity";
            m.BeginDrag();
            var parts = new List<PartDescription>();
            m.Describe(parts);
            Assert.Equal(PartRole.Slider, parts[0].Role);
            Assert.True(parts[0].Has(PartFlags.Grabbed));
        }
    }
}
