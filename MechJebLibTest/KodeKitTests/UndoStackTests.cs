/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's undo stack and history (undo and redo of commits, filtered by source), without KSP or Unity.</summary>
    public class UndoStackTests
    {
        private sealed class Window
        {
            public readonly CommitPath Path;
            public readonly UndoStack  Undo;

            public Window(UndoHistory? history = null, int capacity = 50, string name = "W")
            {
                Path = new CommitPath(name);
                Undo = new UndoStack(Path, capacity, history) { Recording = true };
            }

            public CommitTarget<int> Int(Func<int> get, Action<int> set, bool persist = true) =>
                Path.Target("Segmented", get, set, null, null, persist);
        }

        private sealed class Box
        {
            public int V;
        }

        private static (Window w, CommitTarget<int> t, Box b) One(int start = 0)
        {
            var w = new Window();
            var b = new Box { V = start };
            return (w, w.Int(() => b.V, x => b.V = x), b);
        }

        [Fact]
        public void AnUndoWritesTheOldValueBackThroughTheTarget()
        {
            (Window w, CommitTarget<int> t, Box b) = One(1);
            t.Set(5, ChangeSource.Pointer);

            Assert.Equal(UndoResult.Done, w.Undo.Undo());
            Assert.Equal(1, b.V);
        }

        [Fact]
        public void AnUndoIsACommitWithTheUndoSourceThatObserversSee()
        {
            (Window w, CommitTarget<int> t, _) = One(1);
            var seen = new List<Commit>();
            w.Path.Committed += seen.Add;
            t.Set(5, ChangeSource.Pointer);
            w.Undo.Undo();

            Assert.Equal(2, seen.Count);
            Assert.Equal(ChangeSource.Undo, seen[1].Source);
            Assert.Equal(5, seen[1].Old);
            Assert.Equal(1, seen[1].New);
        }

        [Fact]
        public void TheStackRecordsNoEntryForItsOwnWrites()
        {
            (Window w, CommitTarget<int> t, _) = One();
            t.Set(5, ChangeSource.Pointer);
            w.Undo.Undo();

            Assert.Empty(w.Undo.Done);
            Assert.Single(w.Undo.Undone);
        }

        [Fact]
        public void ARedoWritesTheNewValueAgain()
        {
            (Window w, CommitTarget<int> t, Box b) = One(1);
            t.Set(5, ChangeSource.Pointer);
            w.Undo.Undo();

            Assert.Equal(UndoResult.Done, w.Undo.Redo());
            Assert.Equal(5, b.V);
            Assert.Single(w.Undo.Done);
            Assert.Empty(w.Undo.Undone);
        }

        [Fact]
        public void UndoAndRedoWalkBackAndForthInOrder()
        {
            (Window w, CommitTarget<int> t, Box b) = One();
            t.Set(1, ChangeSource.Pointer);
            t.Set(2, ChangeSource.Pointer);
            t.Set(3, ChangeSource.Pointer);

            w.Undo.Undo();
            w.Undo.Undo();
            Assert.Equal(1, b.V);
            w.Undo.Redo();
            Assert.Equal(2, b.V);
            w.Undo.Undo();
            w.Undo.Undo();
            Assert.Equal(0, b.V);
            Assert.Equal(UndoResult.Nothing, w.Undo.Undo());
        }

        [Fact]
        public void AGroupedGestureIsOneEntry()
        {
            (Window w, CommitTarget<int> t, Box b) = One();
            t.BeginGroup();
            t.Set(1, ChangeSource.Pointer);
            t.Set(2, ChangeSource.Pointer);
            t.Set(3, ChangeSource.Pointer);
            t.EndGroup();

            Assert.Single(w.Undo.Done);
            w.Undo.Undo();
            Assert.Equal(0, b.V);
        }

        [Fact]
        public void ACommitThatKeepsTheValueIsNotAnEntry()
        {
            (Window w, CommitTarget<int> t, _) = One(2);
            t.Set(2, ChangeSource.Pointer);

            Assert.Empty(w.Undo.Done);
            Assert.False(w.Undo.CanUndo());
        }

        [Fact]
        public void AFieldEditedBackToItsValueIsNotAnUndoStep()
        {
            (Window w, CommitTarget<int> t, Box b) = One(2);
            t.Set(9, ChangeSource.Pointer);
            t.BeginGroup();
            t.Set(7, ChangeSource.Keyboard);
            t.Set(9, ChangeSource.Keyboard); // Escape: the field puts its text back
            t.EndGroup();

            // the edit that ended where it began is skipped: the undo takes the click before it
            w.Undo.Undo();
            Assert.Equal(2, b.V);
        }

        [Fact]
        public void ANewEditDropsTheRedoEntriesOfItsSource()
        {
            var w = new Window();
            var a = new Box();
            var c = new Box();
            CommitTarget<int> ta = w.Int(() => a.V, x => a.V = x);
            CommitTarget<int> tc = w.Int(() => c.V, x => c.V = x);
            ta.Set(1, ChangeSource.Pointer);
            w.Undo.Undo();

            tc.Set(4, ChangeSource.Pointer);
            Assert.Equal(UndoResult.Nothing, w.Undo.Redo());
            Assert.Equal(0, a.V);
        }

        [Fact]
        public void AnotherSourcesEditOfAnotherControlKeepsTheRedo()
        {
            var w = new Window();
            var a = new Box();
            var c = new Box();
            CommitTarget<int> ta = w.Int(() => a.V, x => a.V = x);
            CommitTarget<int> tc = w.Int(() => c.V, x => c.V = x);
            ta.Set(1, ChangeSource.Agent);
            w.Undo.Undo(ChangeSource.Agent);

            tc.Set(4, ChangeSource.Pointer);
            Assert.Equal(UndoResult.Done, w.Undo.Redo(ChangeSource.Agent));
            Assert.Equal(1, a.V);
            Assert.Equal(4, c.V);
        }

        [Fact]
        public void ANewEditOfTheSameControlDropsItsRedoWhateverTheSource()
        {
            (Window w, CommitTarget<int> t, Box b) = One();
            t.Set(1, ChangeSource.Agent);
            w.Undo.Undo(ChangeSource.Agent);

            t.Set(6, ChangeSource.Pointer);
            Assert.Equal(UndoResult.Nothing, w.Undo.Redo(ChangeSource.Agent));
            Assert.Equal(6, b.V);
        }

        [Fact]
        public void UndoBySourceRevertsOnlyThatSourcesChange()
        {
            var w = new Window();
            var a = new Box();
            var c = new Box();
            CommitTarget<int> ta = w.Int(() => a.V, x => a.V = x);
            CommitTarget<int> tc = w.Int(() => c.V, x => c.V = x);
            ta.Set(1, ChangeSource.Agent);
            tc.Set(2, ChangeSource.Pointer);

            Assert.Equal(UndoResult.Done, w.Undo.Undo(ChangeSource.Agent));
            Assert.Equal(0, a.V);
            Assert.Equal(2, c.V);
            Assert.Equal(UndoResult.Nothing, w.Undo.Undo(ChangeSource.Agent));
        }

        [Fact]
        public void UndoBySourceRefusesWhenAnotherSourceChangedTheSameControlSince()
        {
            (Window w, CommitTarget<int> t, Box b) = One();
            t.Set(1, ChangeSource.Agent);
            t.Set(2, ChangeSource.Pointer);

            Assert.Equal(UndoResult.Conflict, w.Undo.Undo(ChangeSource.Agent));
            Assert.Equal(2, b.V);
            Assert.Equal(2, w.Undo.Done.Count);
        }

        [Fact]
        public void AValueChangedOutsideThePathMakesTheEntryStale()
        {
            (Window w, CommitTarget<int> t, Box b) = One();
            t.Set(1, ChangeSource.Pointer);
            b.V = 8; // the module changed it itself

            Assert.Equal(UndoResult.Stale, w.Undo.Undo());
            Assert.Equal(8, b.V);
            Assert.Empty(w.Undo.Done);
            Assert.Empty(w.Undo.Undone);
        }

        [Fact]
        public void ARedoOfAValueChangedSinceTheUndoIsStale()
        {
            (Window w, CommitTarget<int> t, Box b) = One();
            t.Set(1, ChangeSource.Pointer);
            w.Undo.Undo();
            b.V = 8;

            Assert.Equal(UndoResult.Stale, w.Undo.Redo());
            Assert.Equal(8, b.V);
        }

        [Fact]
        public void TheGuardHoldsTheUndoAndKeepsTheEntry()
        {
            (Window w, CommitTarget<int> t, Box b) = One();
            bool engaged = true;
            w.Undo.Guard = _ => !engaged;
            t.Set(1, ChangeSource.Pointer);

            Assert.False(w.Undo.CanUndo());
            Assert.Equal(UndoResult.Blocked, w.Undo.Undo());
            Assert.Equal(1, b.V);

            engaged = false;
            Assert.True(w.Undo.CanUndo());
            Assert.Equal(UndoResult.Done, w.Undo.Undo());
            Assert.Equal(0, b.V);
        }

        [Fact]
        public void TheGuardHoldsTheRedoToo()
        {
            (Window w, CommitTarget<int> t, Box b) = One();
            bool engaged = false;
            w.Undo.Guard = _ => !engaged;
            t.Set(1, ChangeSource.Pointer);
            w.Undo.Undo();
            engaged = true;

            Assert.False(w.Undo.CanRedo());
            Assert.Equal(UndoResult.Blocked, w.Undo.Redo());
            Assert.Equal(0, b.V);
        }

        [Fact]
        public void TheStackKeepsItsCapacityDroppingTheOldest()
        {
            var w = new Window(null, 3);
            var b = new Box();
            CommitTarget<int> t = w.Int(() => b.V, x => b.V = x);
            for (int i = 1; i <= 5; i++) t.Set(i, ChangeSource.Pointer);

            Assert.Equal(3, w.Undo.Done.Count);
            while (w.Undo.Undo() == UndoResult.Done) { }
            Assert.Equal(2, b.V);
        }

        [Fact]
        public void ATargetThatIsNotUndoableIsNotRecorded()
        {
            var w = new Window();
            var b = new Box();
            CommitTarget<int> t = w.Int(() => b.V, x => b.V = x, false); // a selected row: navigation, not a setting
            t.Set(3, ChangeSource.Pointer);

            Assert.False(t.Undoable);
            Assert.Empty(w.Undo.Done);
        }

        private sealed class Node
        {
            public double Ut;
            public bool   Gone;
        }

        // a node's time behind a field that only holds a shift
        private sealed class NodeState : IUndoState
        {
            private readonly Node _node;
            public NodeState(Node node) => _node = node;
            public object Take() => _node.Ut;

            public bool Restore(object state)
            {
                if (_node.Gone) return false;
                _node.Ut = (double)state;
                return true;
            }
        }

        [Fact]
        public void AStateTargetRestoresItsStateNotItsValue()
        {
            var w = new Window();
            var node = new Node { Ut = 100 };
            string shift = "10";
            CommitTarget<string> t = w.Path.Target("Stepper", () => shift, s => shift = s);
            t.State = new NodeState(node);
            t.Apply(1, d => node.Ut += 10 * d, ChangeSource.Pointer); // + moves the node by the shift

            Assert.Equal(110, node.Ut);
            Assert.Equal(UndoResult.Done, w.Undo.Undo());
            Assert.Equal(100, node.Ut);
            Assert.Equal("10", shift);
            Assert.Equal(UndoResult.Done, w.Undo.Redo());
            Assert.Equal(110, node.Ut);
        }

        [Fact]
        public void AChangeOfOnlyTheStateIsAnEntry()
        {
            var w = new Window();
            var node = new Node { Ut = 100 };
            CommitTarget<string> t = w.Path.Target("Stepper", () => "10", _ => { });
            t.State = new NodeState(node);
            t.Apply(1, d => node.Ut += 10, ChangeSource.Pointer);

            Assert.True(w.Path.Last.Changed);
            Assert.Single(w.Undo.Done);
        }

        [Fact]
        public void AStateThatRefusesItsRestoreIsStaleAndDropped()
        {
            var w = new Window();
            var node = new Node { Ut = 100 };
            CommitTarget<string> t = w.Path.Target("Stepper", () => "10", _ => { });
            t.State = new NodeState(node);
            t.Apply(1, d => node.Ut += 10, ChangeSource.Pointer);
            node.Gone = true; // executed

            Assert.Equal(UndoResult.Stale, w.Undo.Undo());
            Assert.Equal(110, node.Ut);
            Assert.Empty(w.Undo.Done);
        }

        [Fact]
        public void TypingThatHasNotReachedTheStateIsNoUndoStep()
        {
            // the node editor's time shift: its text is a setting, the node moves only on − and +
            var w = new Window();
            var node = new Node { Ut = 100 };
            string shift = "10";
            CommitTarget<string> t = w.Path.Target("Stepper", () => shift, s => shift = s);
            t.State = new NodeState(node);
            t.BeginGroup();
            t.Set("60", ChangeSource.Keyboard);
            t.EndGroup();

            Assert.True(w.Path.Last.Changed); // the commit changed the text
            Assert.Empty(w.Undo.Done);        // but no undo step: the node is as it was
            Assert.Equal(UndoResult.Nothing, w.Undo.Undo());
        }

        [Fact]
        public void ATypedValueAppliedWhenTheEditEndsIsOneStepWithTheNodesRealStates()
        {
            // the node editor's Δv: typing writes the text, Enter writes the node through the same target
            var w = new Window();
            var node = new Node { Ut = 100 };
            string text = "100";
            CommitTarget<string> t = w.Path.Target("Stepper", () => text, s => text = s);
            t.State = new NodeState(node);
            t.BeginGroup();
            t.Set("1", ChangeSource.Keyboard);
            t.Set("125", ChangeSource.Keyboard);
            t.EndGroup();
            Assert.Empty(w.Undo.Done);
            t.Apply(text, s => node.Ut = double.Parse(s, System.Globalization.CultureInfo.InvariantCulture), ChangeSource.Keyboard);

            Assert.Single(w.Undo.Done);
            Assert.Equal(100.0, w.Undo.Done[0].Commit.Before);
            Assert.Equal(125.0, w.Undo.Done[0].Commit.After);
            Assert.Equal(UndoResult.Done, w.Undo.Undo());
            Assert.Equal(100, node.Ut);
            Assert.Equal(UndoResult.Done, w.Undo.Redo());
            Assert.Equal(125, node.Ut);
        }

        [Fact]
        public void AStateChangedOutsideThePathMakesTheEntryStale()
        {
            // a gizmo drag since the + : the undo would throw it away
            var w = new Window();
            var node = new Node { Ut = 100 };
            CommitTarget<string> t = w.Path.Target("Stepper", () => "10", _ => { });
            t.State = new NodeState(node);
            t.Apply(1, d => node.Ut += 10, ChangeSource.Pointer);
            node.Ut = 300;

            Assert.Equal(UndoResult.Stale, w.Undo.Undo());
            Assert.Equal(300, node.Ut);
            Assert.Empty(w.Undo.Done);
        }

        [Fact]
        public void ARedoOfAStateChangedSinceTheUndoIsStale()
        {
            var w = new Window();
            var node = new Node { Ut = 100 };
            CommitTarget<string> t = w.Path.Target("Stepper", () => "10", _ => { });
            t.State = new NodeState(node);
            t.Apply(1, d => node.Ut += 10, ChangeSource.Pointer);
            w.Undo.Undo();
            node.Ut = 250;

            Assert.Equal(UndoResult.Stale, w.Undo.Redo());
            Assert.Equal(250, node.Ut);
            Assert.Empty(w.Undo.Undone);
        }

        [Fact]
        public void TargetsSharingAStateAreOneControlForConflicts()
        {
            var w = new Window();
            var node = new Node { Ut = 100 };
            var state = new NodeState(node);
            CommitTarget<string> a = w.Path.Target("Stepper", () => "", _ => { });
            CommitTarget<string> b = w.Path.Target("Stepper", () => "", _ => { });
            a.State = b.State = state;
            a.Apply(1, d => node.Ut += 10, ChangeSource.Agent);
            b.Apply(1, d => node.Ut += 5, ChangeSource.Pointer);

            Assert.Equal(UndoResult.Conflict, w.Undo.Undo(ChangeSource.Agent));
            Assert.Equal(115, node.Ut);
        }

        [Fact]
        public void UndoingAnOpenGroupClosesItAndTheNextChangeIsANewEntry()
        {
            (Window w, CommitTarget<int> t, Box b) = One();
            t.BeginGroup(); // a field has the keyboard
            t.Set(4, ChangeSource.Keyboard);
            Assert.Equal(UndoResult.Done, w.Undo.Undo()); // Ctrl+Z while typing
            Assert.Equal(0, b.V);

            t.Set(7, ChangeSource.Keyboard);
            t.EndGroup();
            Assert.Single(w.Undo.Done);
            Assert.Equal(0, w.Undo.Done[0].Commit.Old);
            w.Undo.Undo();
            Assert.Equal(0, b.V);
        }

        [Fact]
        public void AStackRecordsNothingUntilItsOwnerOptsIn()
        {
            // a window whose setters engage a flight mode (the Translatron's): the history must never write them back
            var history = new UndoHistory();
            var path = new CommitPath("Translatron");
            var stack = new UndoStack(path, 50, history);
            var b = new Box();
            CommitTarget<int> mode = path.Target("Segmented", () => b.V, x => b.V = x);
            mode.Set(2, ChangeSource.Pointer);

            Assert.False(stack.Recording);
            Assert.Empty(stack.Done);
            Assert.Equal(UndoResult.Nothing, history.Undo());
            Assert.Equal(UndoResult.Nothing, history.Undo(ChangeSource.Pointer));
            Assert.Equal(2, b.V);

            stack.Recording = true;
            mode.Set(3, ChangeSource.Pointer);
            Assert.Single(stack.Done);
            stack.Recording = false;
            Assert.Empty(stack.Done);
        }

        [Fact]
        public void APlayersClickDropsTheRedoOfTheSamePlayersTyping()
        {
            // typed, undone, then a click on another control: redoing the typing on top of the click would surprise the player
            var w = new Window();
            var a = new Box();
            var b = new Box();
            CommitTarget<int> field = w.Int(() => a.V, x => a.V = x);
            CommitTarget<int> toggle = w.Int(() => b.V, x => b.V = x);
            field.Set(5, ChangeSource.Keyboard);
            w.Undo.Undo();
            toggle.Set(1, ChangeSource.Pointer);
            Assert.Empty(w.Undo.Undone);
            Assert.Equal(UndoResult.Nothing, w.Undo.Redo());
        }

        [Fact]
        public void UndoByPointerAlsoTakesThePlayersTypedEdits()
        {
            var w = new Window();
            var a = new Box();
            var b = new Box();
            CommitTarget<int> field = w.Int(() => a.V, x => a.V = x);
            CommitTarget<int> agentOnly = w.Int(() => b.V, x => b.V = x);
            field.Set(5, ChangeSource.Keyboard);
            agentOnly.Set(9, ChangeSource.Agent);
            Assert.Same(w.Undo.Done[0], w.Undo.Next(ChangeSource.Pointer));
            Assert.Equal(UndoResult.Done, w.Undo.Undo(ChangeSource.Pointer));
            Assert.Equal(0, a.V);
            Assert.Equal(9, b.V);
        }

        [Fact]
        public void TheSamePlayersClickAfterTypingOnOneControlIsNoConflict()
        {
            // a stepper: typed 5 (Keyboard), then + (Pointer): the player's undo takes the + and then the typing
            var w = new Window();
            var a = new Box();
            CommitTarget<int> stepper = w.Int(() => a.V, x => a.V = x);
            stepper.Set(5, ChangeSource.Keyboard);
            stepper.Set(6, ChangeSource.Pointer);
            Assert.Equal(UndoResult.Done, w.Undo.Undo(ChangeSource.Keyboard));
            Assert.Equal(5, a.V);
            Assert.Equal(UndoResult.Done, w.Undo.Undo(ChangeSource.Keyboard));
            Assert.Equal(0, a.V);
        }

        [Fact]
        public void ASetterThatThrowsDuringAnUndoKeepsTheEntryAndReportsIt()
        {
            var w = new Window();
            var b = new Box();
            bool fail = false;
            CommitTarget<int> t = w.Int(() => b.V, x =>
            {
                if (fail) throw new InvalidOperationException("setter");
                b.V = x;
            });
            Exception? seen = null;
            w.Undo.Failed += (e, x) => seen = x;
            t.Set(5, ChangeSource.Pointer);
            fail = true;
            Assert.Equal(UndoResult.Failed, w.Undo.Undo());
            Assert.IsType<InvalidOperationException>(seen);
            Assert.Single(w.Undo.Done);
            fail = false;
            Assert.Equal(UndoResult.Done, w.Undo.Undo());
            Assert.Equal(0, b.V);
        }

        [Fact]
        public void AnUndoAskedWhileAnotherWritesIsRefused()
        {
            var w = new Window();
            var a = new Box();
            var b = new Box();
            CommitTarget<int> ta = w.Int(() => a.V, x => a.V = x);
            CommitTarget<int> tb = w.Int(() => b.V, x => b.V = x);
            tb.Set(1, ChangeSource.Pointer);
            ta.Set(1, ChangeSource.Pointer);
            var inner = new List<UndoResult>();
            w.Path.Committed += c =>
            {
                if (c.Source == ChangeSource.Undo) inner.Add(w.Undo.Undo());
            };
            Assert.Equal(UndoResult.Done, w.Undo.Undo());
            AssertionExtensions.EqualSequence(new[] { UndoResult.Busy }, inner);
            Assert.Equal(1, b.V);
            Assert.Single(w.Undo.Done);
        }

        [Fact]
        public void AnObserversCommitIsRecordedAfterTheOneThatCausedIt()
        {
            // the stack subscribes after an observer that commits while called: it still gets both in order
            var path = new CommitPath("W");
            int a = 0, b = 0;
            CommitTarget<int> ta = path.Target("Toggle", () => a, x => a = x);
            CommitTarget<int> tb = path.Target("Toggle", () => b, x => b = x);
            path.Committed += c =>
            {
                if (c.Target == ta && c.Source == ChangeSource.Pointer) tb.Set(a * 10, ChangeSource.Program);
            };
            var stack = new UndoStack(path) { Recording = true };
            ta.Set(1, ChangeSource.Pointer);
            Assert.Equal(2, stack.Done.Count);
            Assert.Same(ta, stack.Done[0].Target);
            Assert.Same(tb, stack.Done[1].Target);
        }

        [Fact]
        public void AWriteThatRebuildsTheWindowLeavesNoRedoInTheDetachedStack()
        {
            var w = new Window();
            var b = new Box();
            bool rebuild = false;
            CommitTarget<int> t = w.Int(() => b.V, x =>
            {
                b.V = x;
                if (rebuild) w.Undo.Detach(); // a language switch inside the setter
            });
            t.Set(5, ChangeSource.Pointer);
            rebuild = true;
            Assert.Equal(UndoResult.Done, w.Undo.Undo());
            Assert.Empty(w.Undo.Undone);
            Assert.Empty(w.Undo.Done);
        }

        [Fact]
        public void AnUndosCommitNamesWhoUndid()
        {
            var w = new Window();
            var b = new Box();
            CommitTarget<int> t = w.Int(() => b.V, x => b.V = x);
            var undos = new List<Commit>();
            w.Path.Committed += c =>
            {
                if (c.Source == ChangeSource.Undo) undos.Add(c);
            };
            t.Set(1, ChangeSource.Agent);
            w.Undo.Undo(ChangeSource.Agent); // an agent undoing its own change
            w.Undo.Redo();                   // the program
            using (CommitPath.As(ChangeSource.Pointer))
                w.Undo.Undo();               // the player's click on the icon
            AssertionExtensions.EqualSequence(new[] { ChangeActor.Agent, ChangeActor.Program, ChangeActor.User }, undos.ConvertAll(c => c.Actor));
        }

        [Fact]
        public void ASlidersPressAndItsDragAreOneEntry()
        {
            var w = new Window();
            var b = new Box();
            CommitTarget<int> slider = w.Int(() => b.V, x => b.V = x);
            slider.Set(4, ChangeSource.Pointer); // the track click moves the value before the drag's group opens
            slider.BeginGroup(true);
            slider.Set(6, ChangeSource.Pointer);
            slider.EndGroup();
            Assert.Single(w.Undo.Done);
            Assert.Equal(UndoResult.Done, w.Undo.Undo());
            Assert.Equal(0, b.V);
        }

        [Fact]
        public void ACommitInsideAnotherTargetsSetterIsNoEntryOfItsOwn()
        {
            var w = new Window();
            var a = new Box();
            var b = new Box();
            CommitTarget<int> tb = w.Int(() => b.V, x => b.V = x);
            CommitTarget<int> ta = w.Int(() => a.V, x =>
            {
                a.V = x;
                tb.Set(x * 10, ChangeSource.Program);
            });
            ta.Set(1, ChangeSource.Pointer);
            Assert.Single(w.Undo.Done);
            Assert.Same(ta, w.Undo.Done[0].Target);
            Assert.Equal(UndoResult.Done, w.Undo.Undo());
            Assert.Equal(0, a.V);
            Assert.Equal(0, b.V); // the outer setter ran again and wrote its side effect
            Assert.Empty(w.Undo.Done);
        }

        [Fact]
        public void ARemovedTargetTakesItsEntriesWithIt()
        {
            var w = new Window();
            var a = new Box();
            var b = new Box();
            CommitTarget<int> row = w.Int(() => a.V, x => a.V = x);
            CommitTarget<int> keep = w.Int(() => b.V, x => b.V = x);
            row.Set(1, ChangeSource.Pointer);
            keep.Set(1, ChangeSource.Pointer);
            row.Set(2, ChangeSource.Pointer);
            w.Undo.Undo();
            w.Path.Remove(row); // the list's row destroyed
            Assert.Single(w.Undo.Done);
            Assert.Same(keep, w.Undo.Done[0].Target);
            Assert.Empty(w.Undo.Undone);
            Assert.Equal(UndoResult.Done, w.Undo.Undo());
            Assert.Equal(0, b.V);
            Assert.Equal(1, a.V);
        }

        [Fact]
        public void ADetachedStackRecordsNothingAndLeavesTheHistory()
        {
            var history = new UndoHistory();
            var w = new Window(history);
            var b = new Box();
            CommitTarget<int> t = w.Int(() => b.V, x => b.V = x);
            t.Set(1, ChangeSource.Pointer);
            w.Undo.Detach();
            t.Set(2, ChangeSource.Pointer);

            Assert.Empty(w.Undo.Done);
            Assert.Empty(history.Stacks);
            Assert.Equal(UndoResult.Nothing, history.Undo());
        }

        [Fact]
        public void ARebuiltWindowStartsWithAnEmptyStackAndTheOldControlsAreNotWritten()
        {
            // a language switch destroys the window and builds another over the same module value
            var history = new UndoHistory();
            var b = new Box();
            var old = new Window(history);
            CommitTarget<int> oldTarget = old.Int(() => b.V, x => b.V = x);
            oldTarget.Set(3, ChangeSource.Pointer);
            old.Undo.Detach(); // the old window's root was destroyed

            var rebuilt = new Window(history);
            rebuilt.Int(() => b.V, x => b.V = x);

            Assert.Empty(rebuilt.Undo.Done);
            Assert.Equal(UndoResult.Nothing, history.Undo());
            Assert.Equal(3, b.V);
        }

        [Fact]
        public void TheHistoryUndoesTheMostRecentEntryOfASourceAcrossWindows()
        {
            var history = new UndoHistory();
            var w1 = new Window(history, 50, "Planner");
            var w2 = new Window(history, 50, "Node editor");
            var a = new Box();
            var c = new Box();
            CommitTarget<int> ta = w1.Int(() => a.V, x => a.V = x);
            CommitTarget<int> tc = w2.Int(() => c.V, x => c.V = x);
            ta.Set(1, ChangeSource.Agent);
            tc.Set(2, ChangeSource.Agent);
            ta.Set(5, ChangeSource.Pointer);

            Assert.Equal(2, history.Count(ChangeSource.Agent));
            Assert.Same(w2.Undo, history.Latest(ChangeSource.Agent));
            Assert.Equal(UndoResult.Done, history.Undo(ChangeSource.Agent));
            Assert.Equal(0, c.V);
            // the agent's edit of the first window: the player changed that control since, so it stays
            Assert.Equal(UndoResult.Conflict, history.Undo(ChangeSource.Agent));
            Assert.Equal(5, a.V);
        }

        [Fact]
        public void TheHistoryRedoesTheMostRecentlyUndoneEntry()
        {
            var history = new UndoHistory();
            var w1 = new Window(history);
            var w2 = new Window(history);
            var a = new Box();
            var c = new Box();
            CommitTarget<int> ta = w1.Int(() => a.V, x => a.V = x);
            CommitTarget<int> tc = w2.Int(() => c.V, x => c.V = x);
            tc.Set(2, ChangeSource.Agent);
            ta.Set(1, ChangeSource.Agent);
            history.Undo(ChangeSource.Agent);
            history.Undo(ChangeSource.Agent);

            Assert.Same(w2.Undo, history.LatestUndone(ChangeSource.Agent));
            history.Redo(ChangeSource.Agent);
            Assert.Equal(2, c.V);
            Assert.Equal(0, a.V);
        }

        [Fact]
        public void ClearForgetsEveryEntry()
        {
            (Window w, CommitTarget<int> t, _) = One();
            t.Set(1, ChangeSource.Pointer);
            t.Set(2, ChangeSource.Pointer);
            w.Undo.Undo();
            int v = w.Undo.Version;
            w.Undo.Clear();

            Assert.Empty(w.Undo.Done);
            Assert.Empty(w.Undo.Undone);
            Assert.NotEqual(v, w.Undo.Version);
        }

        [Fact]
        public void AnObserverThatThrowsDoesNotStopTheOthersAndIsReported()
        {
            var path = new CommitPath("W");
            var failures = new List<Exception>();
            path.ObserverFailed += (_, e) => failures.Add(e);
            int after = 0;
            path.Committed += _ => throw new InvalidOperationException("observer");
            path.Committed += _ => after++;
            int v = 0;
            CommitTarget<int> t = path.Target("Toggle", () => v, x => v = x);

            t.Set(1, ChangeSource.Pointer); // doesn't throw: the widget's handler goes on

            Assert.Equal(1, v);
            Assert.Equal(1, after);
            Assert.IsType<InvalidOperationException>(Assert.Single(failures));
        }

        [Fact]
        public void AnObserverCanUnsubscribeWhileBeingCalled()
        {
            var path = new CommitPath("W");
            int calls = 0, others = 0;
            Action<Commit>? once = null;
            once = _ =>
            {
                calls++;
                path.Committed -= once!;
            };
            path.Committed += once;
            path.Committed += _ => others++;
            int v = 0;
            CommitTarget<int> t = path.Target("Toggle", () => v, x => v = x);

            t.Set(1, ChangeSource.Pointer);
            t.Set(2, ChangeSource.Pointer);

            Assert.Equal(1, calls);
            Assert.Equal(2, others);
        }
    }
}
