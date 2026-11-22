#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>Why a popup closed.</summary>
    public enum PopupDismiss
    {
        /// <summary>A press outside it (on its blocker).</summary>
        OutsidePress,

        /// <summary>Escape, routed to it by its keyboard scope (the scope decides who gets the key: <see cref="KeyboardScopes"/>).</summary>
        Escape,

        /// <summary>Its owner (the window or control that opened it) closed, was hidden or was destroyed.</summary>
        OwnerClosed,

        /// <summary>The game is changing scene.</summary>
        SceneChange,

        /// <summary>Its host closed it (a pick, a button, another drop-down opening).</summary>
        Program,

        /// <summary>The layer under it closed: a popup opened from another one closes with it.</summary>
        ParentClosed
    }

    /// <summary>One open popup on a <see cref="PopupLayers"/> stack: its owner, the layer it was opened from, its box and how it closes.</summary>
    [NotAWidget("data: one open popup on the popup stack; the drop-down or dialog it stands for describes its parts")]
    public sealed class PopupLayer
    {
        private readonly PopupLayers stack;

        internal PopupLayer(PopupLayers stack, object owner, PopupLayer parent, bool modal)
        {
            this.stack = stack;
            Owner      = owner;
            Parent     = parent;
            Modal      = modal;
        }

        /// <summary>The window or control that opened it (its closing closes the layer).</summary>
        public object Owner { get; }

        /// <summary>The layer it was opened from (null: none). It closes with its parent.</summary>
        public PopupLayer Parent { get; }

        /// <summary>A modal layer (a dialog) is not closed by a press outside it: the press is taken and does nothing.</summary>
        public bool Modal { get; }

        /// <summary>Its box on the canvas (canvas units, y up), set by its host after each placement: a press inside it is no outside press.</summary>
        public FrameBox Box { get; set; }

        /// <summary>Whether <see cref="Box"/> was set (a layer without a box contains no point).</summary>
        public bool HasBox { get; private set; }

        public bool IsOpen { get; internal set; }

        /// <summary>How it closed (valid once closed).</summary>
        public PopupDismiss Reason { get; internal set; }

        /// <summary>Called once, when it closes, with the reason. The host tears its popup down here.</summary>
        public event Action<PopupDismiss> Dismissed;

        /// <summary>Puts the layer at <paramref name="box"/>.</summary>
        public void Place(FrameBox box)
        {
            Box    = box;
            HasBox = true;
        }

        /// <summary>Whether the point is inside its box.</summary>
        public bool Contains(float x, float y) => HasBox && x >= Box.Left && x <= Box.Right && y >= Box.Bottom && y <= Box.Top;

        /// <summary>Closes the layer, and every layer opened from it first. Nothing happens when it is closed already.</summary>
        public void Dismiss(PopupDismiss reason) => stack.Close(this, reason);

        internal void RaiseDismissed() => Dismissed?.Invoke(Reason);
    }

    /// <summary>
    /// The stack of open popups (drop-down lists, dialogs, and later menus and hover cards) and the rules that close them, without
    /// Unity. KodeKit keeps one shared stack (<c>KodeKit.Popups.Layers</c>); each popup opens a layer and tears itself down when the
    /// layer's <see cref="PopupLayer.Dismissed"/> is called. Where a popup goes is <see cref="PopupPlacer"/>'s.
    /// <list type="bullet">
    /// <item>A popup opened goes on top. A layer opened from another (<c>parent</c>) closes when its parent closes, and the layers
    /// opened from a layer always close before it (top first), each told <see cref="PopupDismiss.ParentClosed"/>.</item>
    /// <item>A press outside every popup closes the top layer only (the press is taken: it reaches nothing under the popup). A press
    /// inside a layer under the top closes the layers above that one. A modal layer on top takes the press and nothing closes, and a
    /// press never closes a layer under a modal one.</item>
    /// <item>Escape is the keyboard scope's (<see cref="KeyboardScopes"/>): the scope on top routes it to its popup, which closes its
    /// layer with <see cref="PopupDismiss.Escape"/>. The stack reads no key.</item>
    /// <item>An owner closing, hidden or destroyed closes every layer it opened (<see cref="OwnerClosed"/>), with the layers opened
    /// from them.</item>
    /// <item>A scene change closes every layer, top first (<see cref="SceneChange"/>).</item>
    /// <item>Each layer is closed once: a second close of any kind does nothing, and a layer opened or closed by a host during another
    /// layer's <see cref="PopupLayer.Dismissed"/> keeps the stack in order.</item>
    /// </list>
    /// </summary>
    [NotAWidget("the stack of open popups and the rules that close them, not a widget; each popup's widget describes its parts")]
    public sealed class PopupLayers
    {
        private readonly List<PopupLayer> open = new List<PopupLayer>();

        /// <summary>Called after a layer opens or closes.</summary>
        public event Action Changed;

        /// <summary>The open layers, bottom first.</summary>
        public IReadOnlyList<PopupLayer> Open => open;

        /// <summary>The layer on top (null: none).</summary>
        public PopupLayer Top => open.Count > 0 ? open[open.Count - 1] : null;

        public int Count => open.Count;

        /// <summary>Opens a layer for <paramref name="owner"/> on top, opened from <paramref name="parent"/> (null: none).</summary>
        public PopupLayer Push(object owner, PopupLayer parent = null, bool modal = false)
        {
            if (parent != null && !parent.IsOpen) parent = null;
            var l = new PopupLayer(this, owner, parent, modal) { IsOpen = true };
            open.Add(l);
            Changed?.Invoke();
            return l;
        }

        /// <summary>
        /// A press at (<paramref name="x"/>, <paramref name="y"/>) in canvas units. Closes the layers above the topmost layer that holds
        /// the point, looking no lower than a modal layer; when none holds it, the top layer only, unless it is modal. True when a popup
        /// is open: the press is the popups', not the window's or the game's under them.
        /// </summary>
        public bool OutsidePress(float x, float y)
        {
            if (open.Count == 0) return false;
            PopupLayer top = Top;
            if (top.Contains(x, y)) return true;
            // the topmost layer holding the point, looking no lower than a modal layer
            int keep = -1;
            for (int i = open.Count - 1; i >= 0; i--)
            {
                if (open[i].Contains(x, y))
                {
                    keep = i;
                    break;
                }

                if (open[i].Modal) break;
            }

            if (keep < 0)
            {
                if (!top.Modal) Close(top, PopupDismiss.OutsidePress);
                return true;
            }

            // close the layers above it, top first
            while (open.Count - 1 > keep) Close(Top, PopupDismiss.OutsidePress);
            return true;
        }

        /// <summary>Closes every layer <paramref name="owner"/> opened, and the layers opened from them.</summary>
        public void OwnerClosed(object owner)
        {
            if (owner == null) return;
            for (int i = open.Count - 1; i >= 0; i--)
            {
                if (i >= open.Count) continue;
                PopupLayer l = open[i];
                if (Equals(l.Owner, owner)) Close(l, PopupDismiss.OwnerClosed);
            }
        }

        /// <summary>Closes every layer, top first.</summary>
        public void SceneChange()
        {
            while (open.Count > 0) Close(Top, PopupDismiss.SceneChange);
        }

        internal void Close(PopupLayer layer, PopupDismiss reason)
        {
            if (layer == null || !layer.IsOpen) return;
            // the layers opened from it first, top first
            for (int i = open.Count - 1; i >= 0; i--)
            {
                if (i >= open.Count) continue;
                PopupLayer l = open[i];
                if (l != layer && l.IsOpen && DescendsFrom(l, layer)) Close(l, PopupDismiss.ParentClosed);
            }

            if (!layer.IsOpen) return;
            layer.IsOpen = false;
            layer.Reason = reason;
            open.Remove(layer);
            layer.RaiseDismissed();
            Changed?.Invoke();
        }

        private static bool DescendsFrom(PopupLayer l, PopupLayer ancestor)
        {
            for (PopupLayer p = l.Parent; p != null; p = p.Parent)
                if (p == ancestor) return true;
            return false;
        }
    }
}
