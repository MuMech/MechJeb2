/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Linq;
using KodeKit.Headless;
using Xunit;
using K = KodeKit.Headless.ShortcutKeys;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>The keyboard shortcuts (<see cref="ShortcutMap"/>, <see cref="ActionRegistry"/>): opt-in window toggles kept off KSP's keys.</summary>
    public class ShortcutMapTests
    {
        private const int Q = 113, E = 101, G = 103, Alpha1 = 49, Semicolon = 59, Mouse1 = 324, Joystick1 = 330;

        // KSP's default flight bindings that matter here, as the host reads them from GameSettings
        private static List<ReservedKeys> Defaults() => new List<ReservedKeys>
        {
            new ReservedKeys("PITCH_DOWN", K.W), new ReservedKeys("YAW_LEFT", K.A), new ReservedKeys("PITCH_UP", K.S),
            new ReservedKeys("YAW_RIGHT", K.D), new ReservedKeys("ROLL_LEFT", Q), new ReservedKeys("ROLL_RIGHT", E),
            new ReservedKeys("THROTTLE_UP", K.LeftShift), new ReservedKeys("THROTTLE_DOWN", K.LeftControl),
            new ReservedKeys("THROTTLE_CUTOFF", K.X), new ReservedKeys("THROTTLE_FULL", K.Z), new ReservedKeys("LANDING_GEAR", G),
            new ReservedKeys("CustomActionGroup1", Alpha1), new ReservedKeys("MODIFIER_KEY", K.LeftAlt),
            new ReservedKeys("MODIFIER_KEY", K.RightAlt), new ReservedKeys("PAUSE", K.Escape), new ReservedKeys("LAUNCH_STAGES", K.Space)
        };

        private sealed class Keys : IShortcutKeys
        {
            public readonly HashSet<int> DownNow = new HashSet<int>();
            public readonly HashSet<int> HeldNow = new HashSet<int>();
            public readonly List<int>    Read    = new List<int>();

            public bool Down(int key)
            {
                Read.Add(key);
                return DownNow.Contains(key);
            }

            public bool Held(int key)
            {
                Read.Add(key);
                return HeldNow.Contains(key) || DownNow.Contains(key);
            }

            public void Press(Chord c)
            {
                DownNow.Clear();
                HeldNow.Clear();
                DownNow.Add(c.Key);
                foreach (int k in c.Keys()) if (k != c.Key) HeldNow.Add(k);
            }
        }

        private sealed class Rig
        {
            public readonly ActionRegistry Actions = new ActionRegistry();
            public readonly ShortcutMap    Map;
            public readonly Keys           Keys = new Keys();
            public bool Busy, Layer, AscentShown, DeltaVShown, AscentAvailable = true;
            public int AscentRuns;

            public Rig(bool defaults = true)
            {
                Actions.Add(new UiAction("window:Ascent", UiActionKind.ToggleWindow, () => "Ascent Guidance", () =>
                {
                    AscentShown = !AscentShown;
                    AscentRuns++;
                }, () => AscentAvailable));
                Actions.Add(new UiAction("window:DeltaV", UiActionKind.ToggleWindow, () => "Delta-V Stats", () => DeltaVShown = !DeltaVShown));
                Map = new ShortcutMap(Actions, () => Busy, () => Layer);
                if (defaults) Map.SetReserved(Defaults().Concat(ShortcutMap.KspHardCoded(new[] { K.LeftAlt, K.RightAlt })));
            }

            public ShortcutResult Press(Chord c)
            {
                Keys.Press(c);
                return Map.Poll(Keys);
            }
        }

        private static readonly Chord RCtrlY = new Chord(K.Y, ShortcutMods.RightControl);

        // ---- the registry ----

        [Fact]
        public void AnActionAddedWithAnIdAlreadyThereReplacesItInItsPlace()
        {
            var r = new ActionRegistry();
            r.Add(new UiAction("a", UiActionKind.ToggleWindow, () => "A1", null));
            r.Add(new UiAction("b", UiActionKind.ToggleWindow, () => "B", null));
            r.Add(new UiAction("a", UiActionKind.ToggleWindow, () => "A2", null));
            Assert.Equal(new[] { "A2", "B" }, r.All.Select(a => a.Label));
        }

        [Fact]
        public void AnActionThatCantRunNowDoesNotRun()
        {
            int runs = 0;
            var a = new UiAction("a", UiActionKind.ToggleWindow, null, () => runs++, () => false);
            Assert.False(a.Run());
            Assert.Equal(0, runs);
        }

        [Fact]
        public void AnActionWithoutALabelShowsItsId() =>
            Assert.Equal("window:X", new UiAction("window:X", UiActionKind.ToggleWindow, () => null, null).Label);

        [Fact]
        public void EveryChangeOfTheRegistryRaisesChangedAndMovesTheVersion()
        {
            var r = new ActionRegistry();
            int changed = 0;
            r.Changed += () => changed++;
            r.Add(new UiAction("a", UiActionKind.ToggleWindow, null, null));
            r.Remove("a");
            Assert.False(r.Remove("a"));
            Assert.Equal(2, changed);
            Assert.Equal(2, r.Version);
        }

        [Fact]
        public void RemovingAKindRemovesOnlyItsActions()
        {
            var r = new ActionRegistry();
            r.Add(new UiAction("a", UiActionKind.ToggleWindow, null, null));
            r.RemoveAll(UiActionKind.ToggleWindow);
            Assert.Equal(0, r.Count);
            Assert.Null(r.Find("a"));
        }

        // ---- binding rules ----

        [Fact]
        public void NothingIsBoundByDefault() => Assert.Equal(0, new Rig().Map.Count);

        [Fact]
        public void AFreeChordWithTheRightControlBinds()
        {
            var t = new Rig();
            Assert.True(t.Map.Bind("window:Ascent", RCtrlY).Ok);
            Assert.Equal(RCtrlY, t.Map.ChordOf("window:Ascent"));
        }

        [Fact]
        public void AKeyKspBindsIsRefusedWithTheBindingsName()
        {
            ChordCheck c = new Rig().Map.Bind("window:Ascent", new Chord(K.W));
            Assert.Equal(ChordProblem.Reserved, c.Problem);
            Assert.Equal("PITCH_DOWN", c.Source);
            Assert.Equal(K.W, c.Key);
        }

        [Fact]
        public void AChordWithAFreeModifierAndABoundKeyIsRefused() =>
            Assert.Equal(ChordProblem.Reserved, new Rig().Map.Check(new Chord(K.W, ShortcutMods.RightShift), "window:Ascent").Problem);

        [Fact]
        public void LeftShiftAndLeftControlAreRefusedAsKspsThrottleKeys()
        {
            var t = new Rig();
            ChordCheck shift = t.Map.Check(new Chord(K.Y, ShortcutMods.LeftShift), "window:Ascent");
            ChordCheck ctrl = t.Map.Check(new Chord(K.Y, ShortcutMods.LeftControl), "window:Ascent");
            Assert.Equal("THROTTLE_UP", shift.Source);
            Assert.Equal("THROTTLE_DOWN", ctrl.Source);
        }

        [Fact]
        public void RightShiftAndRightControlAreAllowedWhileKspLeavesThemFree()
        {
            var t = new Rig();
            Assert.True(t.Map.Check(new Chord(K.Y, ShortcutMods.RightShift), "window:Ascent").Ok);
            Assert.True(t.Map.Check(new Chord(K.Y, ShortcutMods.RightControl | ShortcutMods.RightShift), "window:Ascent").Ok);
        }

        [Fact]
        public void AModifierThePlayerBoundInKspIsRefused()
        {
            var t = new Rig();
            t.Map.SetReserved(Defaults().Append(new ReservedKeys("PRECISION_CTRL", K.RightControl)));
            Assert.Equal("PRECISION_CTRL", t.Map.Check(RCtrlY, "window:Ascent").Source);
        }

        [Fact]
        public void AltChordsAreRefusedWhateverKspBinds()
        {
            var t = new Rig(false);
            Assert.Equal(ChordProblem.AltChord, t.Map.Check(new Chord(K.Y, ShortcutMods.RightAlt), "window:Ascent").Problem);
            Assert.Equal(ChordProblem.AltChord, t.Map.Check(new Chord(K.Y, ShortcutMods.AltGr), "window:Ascent").Problem);
        }

        [Fact]
        public void CommandAndWindowsChordsAreRefusedAsTheSystems()
        {
            var t = new Rig(false);
            Assert.Equal(ChordProblem.SystemChord, t.Map.Check(new Chord(K.Y, ShortcutMods.LeftCommand), "window:Ascent").Problem);
            Assert.Equal(ChordProblem.SystemChord, t.Map.Check(new Chord(K.Y, ShortcutMods.RightWindows), "window:Ascent").Problem);
        }

        [Fact]
        public void TabEscapeAndEnterAreRefusedAsKeysTheWindowsRead()
        {
            var t = new Rig(false);
            foreach (int k in new[] { K.Tab, K.Escape, K.Return, K.KeypadEnter })
                Assert.Equal(ChordProblem.UiKey, t.Map.Check(new Chord(k, ShortcutMods.RightControl), "window:Ascent").Problem);
        }

        [Fact]
        public void AModifierAloneAMouseOrJoystickButtonOrALockKeyIsNoKey()
        {
            var t = new Rig(false);
            foreach (int k in new[] { K.RightControl, Mouse1, Joystick1, K.CapsLock, K.ScrollLock })
                Assert.Equal(ChordProblem.NotAKey, t.Map.Check(new Chord(k), "window:Ascent").Problem);
            Assert.Equal(ChordProblem.NoKey, t.Map.Check(Chord.Empty, "window:Ascent").Problem);
        }

        [Fact]
        public void KspsHardCodedKeysAreRefusedWithTheirFileAndLine()
        {
            var t = new Rig(false);
            t.Map.SetReserved(ShortcutMap.KspHardCoded(new[] { K.LeftAlt }));
            Assert.StartsWith("EditorLogic.cs", t.Map.Check(new Chord(K.Z, ShortcutMods.LeftControl), "a").Source);
            Assert.StartsWith("CraftBrowserDialog.cs:430", t.Map.Check(new Chord(K.BackQuote), "a").Source);
        }

        [Fact]
        public void AHardCodedChordIsRefusedOnlyWhenAllItsKeysArePressed()
        {
            var t = new Rig(false);
            t.Map.SetReserved(ShortcutMap.KspHardCoded(null));
            Assert.True(t.Map.Check(new Chord(K.Z, ShortcutMods.RightControl), "a").Ok);
        }

        [Fact]
        public void TheSpaceCentreAndMissionBuilderKeysAreRefused()
        {
            var t = new Rig(false);
            t.Map.SetReserved(ShortcutMap.KspHardCoded(null));
            Assert.StartsWith("SpaceCenterCamera2.cs:199", t.Map.Check(new Chord(K.Alpha0), "a").Source);
            Assert.Equal(ChordProblem.Reserved, t.Map.Check(new Chord(K.C, ShortcutMods.RightControl), "a").Problem); // C alone is the space centre camera's
            Assert.StartsWith("MissionEditorLogic.cs:2119", t.Map.Check(new Chord(K.V, ShortcutMods.RightControl), "a").Source);
            Assert.True(t.Map.Check(new Chord(K.Y, ShortcutMods.RightControl), "a").Ok);
        }

        [Fact]
        public void AnOrphanBindingStillHoldsItsChordAndClearsLikeAnyOther()
        {
            var t = new Rig();
            // a custom window renamed or deleted: its saved binding loads with no action behind it
            t.Map.Load("window:custom:Foo=RightControl+Y", Code);
            ShortcutBinding b = t.Map.Find("window:custom:Foo");
            Assert.True(t.Map.IsOrphan(b));
            Assert.False(t.Map.IsOrphan(t.Map.Find("nothing")));
            Assert.Equal(ChordProblem.Taken, t.Map.Bind("window:Ascent", RCtrlY).Problem);
            Assert.Equal(ShortcutOutcome.Unavailable, t.Press(RCtrlY).Outcome);
            Assert.True(t.Map.Clear("window:custom:Foo"));
            Assert.True(t.Map.Bind("window:Ascent", RCtrlY).Ok);
        }

        [Fact]
        public void TheStageLockFollowsWhateverKeyIsKspsModifier()
        {
            var t = new Rig(false);
            t.Map.SetReserved(ShortcutMap.KspHardCoded(new[] { K.RightControl }));
            Assert.StartsWith("FlightInputHandler.cs:196", t.Map.Check(new Chord(K.L, ShortcutMods.RightControl), "a").Source);
        }

        [Fact]
        public void TheDebugToolbarsF12IsReservedWithKspsModifier()
        {
            // MODIFIER_KEY is Right Shift on Linux: RightShift+F12 opens KSP's debug toolbar there
            var t = new Rig(false);
            t.Map.SetReserved(ShortcutMap.KspHardCoded(new[] { K.RightShift }));
            Assert.StartsWith("DebugToolbar.cs:276", t.Map.Check(new Chord(K.F12, ShortcutMods.RightShift), "a").Source);
        }

        [Fact]
        public void TwoActionsOnOneChordIsAnErrorAtBindTime()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            ChordCheck c = t.Map.Bind("window:DeltaV", RCtrlY);
            Assert.Equal(ChordProblem.Taken, c.Problem);
            Assert.Equal("window:Ascent", c.Other);
            Assert.True(t.Map.ChordOf("window:DeltaV").IsEmpty);
        }

        [Fact]
        public void BindingAnotherChordReplacesTheActionsChord()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            t.Map.Bind("window:Ascent", new Chord(K.F8));
            Assert.Equal(1, t.Map.Count);
            Assert.Equal(new Chord(K.F8), t.Map.ChordOf("window:Ascent"));
        }

        [Fact]
        public void ARefusedBindChangesNothing()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            t.Map.Bind("window:Ascent", new Chord(K.W));
            Assert.Equal(RCtrlY, t.Map.ChordOf("window:Ascent"));
        }

        [Fact]
        public void ClearingRemovesTheChord()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            Assert.True(t.Map.Clear("window:Ascent"));
            Assert.False(t.Map.Clear("window:Ascent"));
            Assert.Equal(ShortcutOutcome.None, t.Press(RCtrlY).Outcome);
        }

        // ---- firing ----

        [Fact]
        public void ABoundChordRunsItsAction()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            ShortcutResult r = t.Press(RCtrlY);
            Assert.Equal(ShortcutOutcome.Fired, r.Outcome);
            Assert.Equal("window:Ascent", r.ActionId);
            Assert.True(t.AscentShown);
        }

        [Fact]
        public void AChordFiresOnlyWithExactlyItsModifiers()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            Assert.Equal(ShortcutOutcome.None, t.Press(new Chord(K.Y)).Outcome);
            Assert.Equal(ShortcutOutcome.None, t.Press(new Chord(K.Y, ShortcutMods.RightControl | ShortcutMods.RightShift)).Outcome);
            // Left Shift held for the throttle doesn't turn a plain key into a shortcut
            t.Map.Bind("window:DeltaV", new Chord(K.F8));
            Assert.Equal(ShortcutOutcome.None, t.Press(new Chord(K.F8, ShortcutMods.LeftShift)).Outcome);
            Assert.Equal(0, t.AscentRuns);
        }

        [Fact]
        public void NothingFiresWhileAKeyboardScopeHasTheKeys()
        {
            var t = new Rig { Busy = true };
            t.Map.Bind("window:Ascent", RCtrlY);
            Assert.Equal(ShortcutOutcome.KeyboardBusy, t.Press(RCtrlY).Outcome);
            Assert.Equal(0, t.AscentRuns);
        }

        [Fact]
        public void ABlockingLayerPausesTheShortcuts()
        {
            var t = new Rig { Layer = true };
            t.Map.Bind("window:Ascent", RCtrlY);
            Assert.Equal(ShortcutOutcome.Layer, t.Press(RCtrlY).Outcome);
            t.Layer = false;
            Assert.Equal(ShortcutOutcome.Fired, t.Press(RCtrlY).Outcome);
        }

        [Fact]
        public void AnActionThatCantRunNowIsUnavailable()
        {
            var t = new Rig { AscentAvailable = false };
            t.Map.Bind("window:Ascent", RCtrlY);
            Assert.Equal(ShortcutOutcome.Unavailable, t.Press(RCtrlY).Outcome);
            Assert.Equal(0, t.AscentRuns);
        }

        [Fact]
        public void ABindingWhoseActionIsGoneIsUnavailable()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            t.Actions.Remove("window:Ascent");
            Assert.Equal(ShortcutOutcome.Unavailable, t.Press(RCtrlY).Outcome);
        }

        [Fact]
        public void AKspKeyReboundOntoABoundChordShowsTheConflictAndStopsItFiring()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            t.Map.SetReserved(Defaults().Append(new ReservedKeys("SAS_TOGGLE", K.Y)));
            ShortcutBinding b = t.Map.Find("window:Ascent");
            Assert.Equal(ChordProblem.Reserved, b.Live.Problem);
            Assert.Equal("SAS_TOGGLE", b.Live.Source);
            Assert.Equal(ShortcutOutcome.Conflict, t.Press(RCtrlY).Outcome);
            Assert.Equal(0, t.AscentRuns);
            // the player moves KSP's key away again: it fires again
            t.Map.SetReserved(Defaults());
            Assert.Equal(ShortcutOutcome.Fired, t.Press(RCtrlY).Outcome);
        }

        [Fact]
        public void AtMostOneActionRunsPerFrame()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            t.Map.Bind("window:DeltaV", new Chord(Semicolon, ShortcutMods.RightControl));
            t.Keys.DownNow.Add(K.Y);
            t.Keys.DownNow.Add(Semicolon);
            t.Keys.HeldNow.Add(K.RightControl);
            t.Map.Poll(t.Keys);
            Assert.True(t.AscentShown ^ t.DeltaVShown);
        }

        [Fact]
        public void APollWithNoBindingReadsNoKey()
        {
            var t = new Rig();
            t.Map.Poll(t.Keys);
            Assert.Empty(t.Keys.Read);
        }

        [Fact]
        public void APollReadsOnlyTheBoundKeysUntilOneGoesDown()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            t.Map.Bind("window:DeltaV", new Chord(K.F8));
            t.Map.Poll(t.Keys);
            Assert.Equal(new[] { K.Y, K.F8 }, t.Keys.Read);
        }

        // ---- capture ----

        [Fact]
        public void ACaptureWaitsThroughModifiersAndBindsTheChord()
        {
            var t = new Rig();
            t.Map.BeginCapture("window:Ascent");
            Assert.False(t.Map.CaptureKey(K.RightControl, ShortcutMods.RightControl));
            Assert.True(t.Map.CaptureKey(K.Y, ShortcutMods.RightControl));
            Assert.Null(t.Map.Capturing);
            Assert.Equal(RCtrlY, t.Map.ChordOf("window:Ascent"));
        }

        [Fact]
        public void ACapturedKspKeyIsRefusedAndSaysWhy()
        {
            var t = new Rig();
            t.Map.BeginCapture("window:Ascent");
            t.Map.CaptureKey(K.W, ShortcutMods.None);
            Assert.Equal("PITCH_DOWN", t.Map.LastCapture.Source);
            Assert.Equal(new Chord(K.W), t.Map.LastCaptured);
            Assert.Equal(0, t.Map.Count);
        }

        [Fact]
        public void EscapeCancelsACapture()
        {
            var t = new Rig();
            t.Map.BeginCapture("window:Ascent");
            Assert.True(t.Map.CaptureKey(K.Escape, ShortcutMods.None));
            Assert.Null(t.Map.Capturing);
            Assert.True(t.Map.LastCapture.Ok);
            Assert.Equal(0, t.Map.Count);
        }

        [Fact]
        public void NothingFiresWhileACaptureIsOn()
        {
            var t = new Rig();
            t.Map.Bind("window:Ascent", RCtrlY);
            t.Map.BeginCapture("window:DeltaV");
            Assert.Equal(ShortcutOutcome.None, t.Press(RCtrlY).Outcome);
            Assert.Empty(t.Keys.Read);
        }

        // ---- text ----

        private static string Name(int k) => k == K.RightControl ? "RightControl" : k == K.Y ? "Y" : k == K.F8 ? "F8" : k == K.W ? "W" : k.ToString();

        private static int Code(string s) =>
            s == "RightControl" ? K.RightControl : s == "Y" ? K.Y : s == "F8" ? K.F8 : s == "W" ? K.W : s == "LeftAlt" ? K.LeftAlt : -1;

        [Fact]
        public void AChordReadsAsItsKeyNamesModifiersFirst() => Assert.Equal("RightControl+Y", RCtrlY.Format(Name));

        [Fact]
        public void SavedBindingsLoadBackWithIdsThatHoldTheSeparators()
        {
            var t = new Rig();
            t.Map.Bind("window:custom:a;b=c%", RCtrlY);
            t.Map.Bind("window:DeltaV", new Chord(K.F8));
            string saved = t.Map.Save(Name);
            var u = new Rig();
            u.Map.Load(saved, Code);
            Assert.Equal(RCtrlY, u.Map.ChordOf("window:custom:a;b=c%"));
            Assert.Equal(new Chord(K.F8), u.Map.ChordOf("window:DeltaV"));
        }

        [Fact]
        public void ALoadedChordThatKspReadsNowStaysBoundAndShowsTheConflict()
        {
            var t = new Rig();
            t.Map.Load("window:Ascent=W;window:DeltaV=RightControl+Y", Code);
            Assert.Equal(ChordProblem.Reserved, t.Map.Find("window:Ascent").Live.Problem);
            Assert.True(t.Map.Find("window:DeltaV").Live.Ok);
        }

        [Fact]
        public void ALoadedEntryTheFixedRulesRefuseOrThatDoesntParseIsDropped()
        {
            var t = new Rig();
            t.Map.Load("window:Ascent=LeftAlt+Y;window:DeltaV=Nope+Y;x=;=Y", Code);
            Assert.Equal(0, t.Map.Count);
        }

        [Fact]
        public void EveryChangeOfTheBindingsRaisesChanged()
        {
            var t = new Rig();
            int changed = 0;
            t.Map.Changed += () => changed++;
            t.Map.Bind("window:Ascent", RCtrlY);
            t.Map.Bind("window:Ascent", new Chord(K.W)); // refused: no change
            t.Map.Clear("window:Ascent");
            t.Map.SetReserved(Defaults());
            Assert.Equal(3, changed);
        }
    }
}
