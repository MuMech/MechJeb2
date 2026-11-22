/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>The popup stack's dismissal rules (outside press, owner closing, scene change, nesting), without KSP or Unity.</summary>
    public class PopupLayersTests
    {
        private static PopupLayer Layer(PopupLayers s, object owner, List<string> log, string name, FrameBox? box = null,
            PopupLayer parent = null, bool modal = false)
        {
            PopupLayer l = s.Push(owner, parent, modal);
            if (box.HasValue) l.Place(box.Value);
            l.Dismissed += r => log.Add(name + ":" + r);
            return l;
        }

        private static readonly FrameBox _list   = new FrameBox(0, 0, 100, -200);
        private static readonly FrameBox _dialog = new FrameBox(-200, 100, 200, -100);

        [Fact]
        public void APopupOpenedGoesOnTop()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            PopupLayer a = Layer(s, "w1", log, "a");
            PopupLayer b = Layer(s, "w2", log, "b");
            Assert.Same(b, s.Top);
            Assert.Equal(2, s.Count);
            Assert.True(a.IsOpen && b.IsOpen);
        }

        [Fact]
        public void APressOutsideEveryPopupClosesTheTopLayerOnlyAndIsTaken()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            Layer(s, "w1", log, "a", _dialog);
            PopupLayer b = Layer(s, "w2", log, "b", _list);
            Assert.True(s.OutsidePress(500, 500));
            Assert.False(b.IsOpen);
            Assert.Equal(new[] { "b:OutsidePress" }, log);
            Assert.Equal(1, s.Count);
        }

        [Fact]
        public void APressInsideTheTopLayerClosesNothing()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            Layer(s, "w", log, "a", _list);
            Assert.True(s.OutsidePress(50, -100));
            Assert.Empty(log);
        }

        [Fact]
        public void APressWithNoPopupOpenIsNotTaken()
        {
            Assert.False(new PopupLayers().OutsidePress(0, 0));
        }

        [Fact]
        public void APressInsideALowerLayerClosesTheLayersAboveIt()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            PopupLayer menu = Layer(s, "w", log, "menu", new FrameBox(-500, 500, -300, 0));
            Layer(s, "w", log, "sub", new FrameBox(-300, 500, -100, 0), menu);
            Layer(s, "w", log, "subsub", new FrameBox(-100, 500, 0, 0));
            s.OutsidePress(-400, 100);
            Assert.Equal(new[] { "subsub:OutsidePress", "sub:OutsidePress" }, log);
            Assert.Same(menu, s.Top);
        }

        [Fact]
        public void AModalLayerOnTopTakesAnOutsidePressAndNothingCloses()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            Layer(s, "w1", log, "list", _list);
            PopupLayer d = Layer(s, "w2", log, "dialog", _dialog, null, true);
            Assert.True(s.OutsidePress(800, 800));
            Assert.True(d.IsOpen);
            Assert.Empty(log);
            Assert.Equal(2, s.Count);
        }

        [Fact]
        public void APressNeverClosesALayerUnderAModalOne()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            // a list open, a dialog over it, a drop-down in the dialog: a press on the list's box closes the drop-down only
            Layer(s, "w1", log, "list", _list);
            Layer(s, "w2", log, "dialog", _dialog, null, true);
            Layer(s, "w2", log, "combo", new FrameBox(300, 0, 400, -100));
            s.OutsidePress(50, -150);
            Assert.Equal(new[] { "combo:OutsidePress" }, log);
            Assert.Equal(2, s.Count);
        }

        [Fact]
        public void APressInsideAModalDialogClosesTheDropDownOpenOverIt()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            PopupLayer d = Layer(s, "w", log, "dialog", _dialog, null, true);
            Layer(s, "w", log, "combo", new FrameBox(300, 0, 400, -100));
            s.OutsidePress(0, 0);
            Assert.Equal(new[] { "combo:OutsidePress" }, log);
            Assert.Same(d, s.Top);
        }

        [Fact]
        public void ALayerWithoutABoxContainsNoPoint()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            PopupLayer a = Layer(s, "w", log, "a");
            Assert.False(a.Contains(0, 0));
            s.OutsidePress(0, 0);
            Assert.False(a.IsOpen);
        }

        [Fact]
        public void TheOwnerClosingClosesEveryLayerItOpenedAndTheLayersOpenedFromThem()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            PopupLayer keep = Layer(s, "other", log, "keep");
            PopupLayer a = Layer(s, "w", log, "a");
            Layer(s, "other", log, "child", null, a);
            Layer(s, "w", log, "b");
            s.OwnerClosed("w");
            Assert.Equal(new[] { "b:OwnerClosed", "child:ParentClosed", "a:OwnerClosed" }, log);
            Assert.Same(keep, s.Top);
            Assert.Equal(1, s.Count);
        }

        [Fact]
        public void ANullOwnerClosesNothing()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            Layer(s, null, log, "a");
            s.OwnerClosed(null);
            Assert.Empty(log);
        }

        [Fact]
        public void ASceneChangeClosesEveryLayerTopFirst()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            PopupLayer a = Layer(s, "w1", log, "a");
            Layer(s, "w1", log, "b", null, a);
            Layer(s, "w2", log, "c", null, null, true);
            s.SceneChange();
            Assert.Equal(new[] { "c:SceneChange", "b:SceneChange", "a:SceneChange" }, log);
            Assert.Equal(0, s.Count);
        }

        [Fact]
        public void ClosingALayerClosesTheLayersOpenedFromItFirstTopFirst()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            PopupLayer a = Layer(s, "w", log, "a");
            PopupLayer b = Layer(s, "w", log, "b", null, a);
            Layer(s, "w", log, "c", null, b);
            Layer(s, "w", log, "d", null, a);
            a.Dismiss(PopupDismiss.Escape);
            Assert.Equal(new[] { "d:ParentClosed", "c:ParentClosed", "b:ParentClosed", "a:Escape" }, log);
            Assert.Equal(PopupDismiss.Escape, a.Reason);
        }

        [Fact]
        public void EachLayerClosesOnce()
        {
            var s = new PopupLayers();
            var log = new List<string>();
            PopupLayer a = Layer(s, "w", log, "a");
            a.Dismiss(PopupDismiss.Program);
            a.Dismiss(PopupDismiss.Escape);
            s.OwnerClosed("w");
            s.SceneChange();
            Assert.Equal(new[] { "a:Program" }, log);
            Assert.Equal(PopupDismiss.Program, a.Reason);
        }

        [Fact]
        public void AHostThatClosesItsLayerAgainInItsHandlerDoesNotCloseItTwice()
        {
            var s = new PopupLayers();
            int calls = 0;
            PopupLayer a = s.Push("w");
            a.Dismissed += r =>
            {
                calls++;
                a.Dismiss(PopupDismiss.Program);
            };
            a.Dismiss(PopupDismiss.Escape);
            Assert.Equal(1, calls);
        }

        [Fact]
        public void APopupOpenedInAnotherLayersHandlerStaysOpen()
        {
            var s = new PopupLayers();
            PopupLayer next = null;
            PopupLayer a = s.Push("w");
            a.Dismissed += r => next = s.Push("w2");
            a.Dismiss(PopupDismiss.Program);
            Assert.True(next.IsOpen);
            Assert.Same(next, s.Top);
        }

        [Fact]
        public void APopupOpenedFromAClosedLayerHasNoParent()
        {
            var s = new PopupLayers();
            PopupLayer a = s.Push("w");
            a.Dismiss(PopupDismiss.Program);
            Assert.Null(s.Push("w", a).Parent);
        }

        [Fact]
        public void EveryOpenAndCloseIsAChange()
        {
            var s = new PopupLayers();
            int changes = 0;
            s.Changed += () => changes++;
            PopupLayer a = s.Push("w");
            s.Push("w", a);
            a.Dismiss(PopupDismiss.Program);
            Assert.Equal(4, changes);
        }
    }
}
