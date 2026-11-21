/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's Stepper behaviour, without KSP or Unity.</summary>
    public class StepperModelTests
    {
        private sealed class Model
        {
            public          double       Value = 100;
            public          string       Text  = "100";
            public          double       Step  = 10;
            public          bool         Parsed = true;
            public readonly List<double> Adds  = new List<double>();

            public void Add(double d)
            {
                Value += d;
                Text  =  Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Adds.Add(d);
            }

            public void SetText(string s)
            {
                Text   = s;
                Parsed = double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v);
                if (Parsed) Value = v;
            }
        }

        private static (Model m, StepperModel s) Make()
        {
            var m = new Model();
            return (m, new StepperModel(() => m.Text, m.SetText, m.Add, () => m.Step, () => m.Parsed));
        }

        [Fact]
        public void PlusAndMinusAddOneStep()
        {
            (Model m, StepperModel s) = Make();
            Assert.True(s.Nudge(1, false, ChangeSource.Pointer));
            Assert.True(s.Nudge(-1, false, ChangeSource.Pointer));
            Assert.Equal(new[] { 10.0, -10.0 }, m.Adds);
            Assert.Equal(100, m.Value);
        }

        [Fact]
        public void BigIsTenSteps()
        {
            (Model m, StepperModel s) = Make();
            s.Nudge(1, true, ChangeSource.Pointer);
            Assert.Equal(200, m.Value);
        }

        [Fact]
        public void TheStepIsReadAtEachNudge()
        {
            // the node editor's step is a separate choice: a nudge uses the step chosen now
            (Model m, StepperModel s) = Make();
            s.Nudge(1, false, ChangeSource.Pointer);
            m.Step = 0.1;
            s.Nudge(1, false, ChangeSource.Pointer);
            Assert.Equal(new[] { 10.0, 0.1 }, m.Adds);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void AStepThatIsNoNumberDoesNothing(double step)
        {
            (Model m, StepperModel s) = Make();
            m.Step = step;
            Assert.False(s.Nudge(1, false, ChangeSource.Pointer));
            Assert.Empty(m.Adds);
        }

        [Fact]
        public void KeysNudgeAndPageKeysAreTenSteps()
        {
            (Model m, StepperModel s) = Make();
            Assert.True(s.Key(StepperKey.Up, false));
            Assert.True(s.Key(StepperKey.Down, true));
            Assert.True(s.Key(StepperKey.PageUp, false));
            Assert.True(s.Key(StepperKey.PageDown, false));
            Assert.Equal(new[] { 10.0, -100.0, 100.0, -100.0 }, m.Adds);
            Assert.Equal(ChangeSource.Keyboard, s.LastSource);
        }

        [Fact]
        public void TypedTextGoesToTheModelWhichParsesIt()
        {
            (Model m, StepperModel s) = Make();
            Assert.True(s.Type("2.5", ChangeSource.Keyboard));
            Assert.Equal(2.5, m.Value);
            Assert.True(s.Valid);
            s.Type("abc", ChangeSource.Keyboard);
            Assert.False(s.Valid);
            Assert.Equal(2.5, m.Value); // the model keeps its last good value
        }

        [Fact]
        public void ARefusedChangeLeavesTheModel()
        {
            (Model m, StepperModel s) = Make();
            s.Allow = (d, src) => src != ChangeSource.Agent;
            Assert.False(s.Nudge(1, false, ChangeSource.Agent));
            Assert.False(s.Type("5", ChangeSource.Agent));
            Assert.True(s.Nudge(1, false, ChangeSource.Pointer));
            Assert.Equal(110, m.Value);
        }

        [Fact]
        public void AllowSeesTheDelta()
        {
            (Model m, StepperModel s) = Make();
            double seen = 0;
            s.Allow = (d, src) =>
            {
                seen = d;
                return true;
            };
            s.Nudge(-1, true, ChangeSource.Pointer);
            Assert.Equal(-100, seen);
        }

        [Fact]
        public void LeftShiftAloneIsNoBigStepOnAButton()
        {
            Assert.False(StepperModel.BigOnButton(StepperModifiers.LeftShift));
            (Model m, StepperModel s) = Make();
            s.Press(1, StepperModifiers.LeftShift, ChangeSource.Pointer);
            Assert.Equal(new[] { 10.0 }, m.Adds);
        }

        [Fact]
        public void RightShiftIsABigStepOnAButton()
        {
            (Model m, StepperModel s) = Make();
            s.Press(1, StepperModifiers.RightShift, ChangeSource.Pointer);
            s.Press(-1, StepperModifiers.RightShift | StepperModifiers.LeftShift, ChangeSource.Pointer);
            Assert.Equal(new[] { 100.0, -100.0 }, m.Adds);
        }

        [Fact]
        public void AltIsABigStepOnAButton()
        {
            (Model m, StepperModel s) = Make();
            s.Press(-1, StepperModifiers.Alt, ChangeSource.Pointer);
            Assert.Equal(new[] { -100.0 }, m.Adds);
        }

        [Fact]
        public void NoModifierIsOneStepOnAButton()
        {
            (Model m, StepperModel s) = Make();
            s.Press(1, StepperModifiers.None, ChangeSource.Pointer);
            Assert.Equal(new[] { 10.0 }, m.Adds);
        }

        [Fact]
        public void EitherShiftOrAltIsABigStepOnAFieldKey()
        {
            (Model m, StepperModel s) = Make();
            s.Key(StepperKey.Up, StepperModifiers.LeftShift);
            s.Key(StepperKey.Up, StepperModifiers.RightShift);
            s.Key(StepperKey.Down, StepperModifiers.Alt);
            s.Key(StepperKey.Down, StepperModifiers.None);
            Assert.Equal(new[] { 100.0, 100.0, -100.0, -10.0 }, m.Adds);
            Assert.Equal(ChangeSource.Keyboard, s.LastSource);
        }

        [Fact]
        public void PageKeysAreTenStepsWhateverIsHeld()
        {
            (Model m, StepperModel s) = Make();
            s.Key(StepperKey.PageUp, StepperModifiers.None);
            s.Key(StepperKey.PageDown, StepperModifiers.Alt);
            Assert.Equal(new[] { 100.0, -100.0 }, m.Adds);
        }
    }
}
