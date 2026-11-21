#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>Why a <see cref="MenuModel"/> closed.</summary>
    public enum MenuClose
    {
        /// <summary>An enabled entry was picked (its action runs after the menu closed).</summary>
        Picked,

        /// <summary>A press of any button outside the menu.</summary>
        OutsidePress,

        /// <summary>The pointer stayed outside the menu for <see cref="MenuModel.LeaveDelay"/>.</summary>
        PointerLeft,

        /// <summary>A key went down. The key is not taken: it does what it does with no menu open (Escape still pauses KSP).</summary>
        KeyPress,

        /// <summary>What it was opened on (a read-out, a window) closed, was hidden or was destroyed.</summary>
        OwnerClosed,

        /// <summary>The game is changing scene.</summary>
        SceneChange,

        /// <summary>Another menu opened in its place.</summary>
        Replaced,

        /// <summary>Its host closed it.</summary>
        Program
    }

    /// <summary>What an entry of a <see cref="MenuModel"/> is.</summary>
    public enum MenuEntryKind
    {
        /// <summary>An action: picked by a click.</summary>
        Action,

        /// <summary>A line of text that names the entries under it (or the menu); never picked.</summary>
        Caption,

        /// <summary>A rule between groups of entries; never picked.</summary>
        Separator
    }

    /// <summary>One entry of a menu: its label (a text or a '#key', localized by the host), whether it can be picked now, a check mark, its action.</summary>
    [NotAWidget("data: one entry of a menu; the menu describes its parts")]
    public sealed class MenuEntry
    {
        private readonly Func<bool> enabled, check;
        private readonly Action<ChangeSource> run;

        private MenuEntry(MenuEntryKind kind, string label, Action<ChangeSource> run, Func<bool> enabled, Func<bool> check)
        {
            Kind         = kind;
            Label        = label;
            this.run     = run;
            this.enabled = enabled;
            this.check   = check;
        }

        /// <summary>An action entry. <paramref name="enabled"/> (null: always) is asked when the menu is drawn and again on a pick.</summary>
        public static MenuEntry Item(string label, Action<ChangeSource> run, Func<bool> enabled = null, Func<bool> check = null) =>
            new MenuEntry(MenuEntryKind.Action, label, run, enabled, check);

        /// <summary>A caption: a line of text over the entries it names.</summary>
        public static MenuEntry Caption(string label) => new MenuEntry(MenuEntryKind.Caption, label, null, null, null);

        /// <summary>A separator.</summary>
        public static MenuEntry Separator() => new MenuEntry(MenuEntryKind.Separator, null, null, null, null);

        public MenuEntryKind Kind { get; }

        /// <summary>The label: a text or a '#key' (null for a separator).</summary>
        public string Label { get; }

        /// <summary>An optional name for the agent bridge and tests (null: the entry's index names it).</summary>
        public string Name { get; set; }

        /// <summary>Whether it can be picked now: an action whose <c>enabled</c> says so (captions and separators never).</summary>
        public bool IsEnabled() => Kind == MenuEntryKind.Action && run != null && (enabled == null || enabled());

        /// <summary>Whether it shows a check mark (an on/off option).</summary>
        public bool IsChecked() => check != null && check();

        /// <summary>Whether it has a check mark at all (on or off).</summary>
        public bool Checkable => check != null;

        internal void Run(ChangeSource source) => run(source);
    }

    /// <summary>
    /// A context menu, without Unity: the entries a right-click on a read-out or a window's title opens, and the rules that close it.
    /// The menu is pointer-only: it takes no keyboard lock and reads no key. Its host tells it what the pointer does (where it is, a
    /// press outside it, a key going down) and the time (seconds, any steady clock), and draws it.
    /// <list type="bullet">
    /// <item>Opening a menu while one is open closes the first (<see cref="MenuClose.Replaced"/>). An open with no entry, or with no
    /// action entry, opens nothing.</item>
    /// <item>Picking an enabled action closes the menu (<see cref="MenuClose.Picked"/>), then runs the action with the pick's source,
    /// so an action that opens a window or a dialog finds the menu gone. Picking a disabled action, a caption or a separator does
    /// nothing, and the menu stays.</item>
    /// <item>A press of any button outside the menu closes it (<see cref="MenuClose.OutsidePress"/>). The press is not taken: it
    /// still reaches what is under it (a button, the scene, the camera).</item>
    /// <item>The pointer outside the menu's box (widened by <see cref="Margin"/>) for <see cref="LeaveDelay"/> seconds closes it
    /// (<see cref="MenuClose.PointerLeft"/>); coming back before then keeps it. The menu opens at the pointer, so it starts inside.</item>
    /// <item>Any key going down closes it (<see cref="MenuClose.KeyPress"/>). The model never learns which key: the key is not taken,
    /// and does what it does with no menu (a flight key flies, Escape pauses).</item>
    /// <item>What it was opened on closing, and a scene change, close it. Each menu closes once, with its reason
    /// (<see cref="LastClose"/>, <see cref="Closed"/>).</item>
    /// <item>No entry takes focus: the menu has no highlight and no key, so its parts handle none.</item>
    /// </list>
    /// </summary>
    public sealed class MenuModel : IDescribeParts
    {
        private readonly List<MenuEntry> entries = new List<MenuEntry>();
        private double outsideSince;
        private bool outside;

        /// <summary>What the menu is about (the read-out's name, the window's title), as shown.</summary>
        public string Label { get; set; }

        /// <summary>Seconds the pointer may stay outside the menu before it closes.</summary>
        public double LeaveDelay { get; set; } = 1;

        /// <summary>Units around the menu's box that still count as inside (a pointer resting on its edge).</summary>
        public float Margin { get; set; } = 8;

        /// <summary>The entries of the open menu (empty when closed).</summary>
        public IReadOnlyList<MenuEntry> Entries => entries;

        public bool IsOpen { get; private set; }

        /// <summary>What it was opened on (null when closed).</summary>
        public object Owner { get; private set; }

        /// <summary>Its box on the canvas (canvas units, y up), set by the host after placing it.</summary>
        public FrameBox Box { get; private set; }

        public bool HasBox { get; private set; }

        /// <summary>Why the last menu closed (valid once one closed).</summary>
        public MenuClose LastClose { get; private set; }

        /// <summary>The entry the last pick ran (-1: none since the last open).</summary>
        public int LastPicked { get; private set; } = -1;

        /// <summary>Called when a menu opens.</summary>
        public event Action Opened;

        /// <summary>Called once when the open menu closes, with the reason; the host tears its popup down here.</summary>
        public event Action<MenuClose> Closed;

        /// <summary>
        /// Opens a menu of <paramref name="items"/> on <paramref name="owner"/> at time <paramref name="now"/>. False (and nothing opens)
        /// when it holds no action entry.
        /// </summary>
        public bool Open(object owner, IEnumerable<MenuEntry> items, double now)
        {
            if (IsOpen) Close(MenuClose.Replaced);
            entries.Clear();
            bool any = false;
            if (items != null)
                foreach (MenuEntry e in items)
                {
                    if (e == null) continue;
                    entries.Add(e);
                    if (e.Kind == MenuEntryKind.Action) any = true;
                }

            if (!any)
            {
                entries.Clear();
                return false;
            }

            IsOpen       = true;
            Owner        = owner;
            HasBox       = false;
            LastPicked   = -1;
            outside      = false;
            outsideSince = now;
            Opened?.Invoke();
            return true;
        }

        /// <summary>The host placed the menu at <paramref name="box"/>.</summary>
        public void Place(FrameBox box)
        {
            Box    = box;
            HasBox = true;
        }

        /// <summary>Whether a point (canvas units) is on the menu, its margin included. A menu not placed yet holds every point.</summary>
        public bool Contains(float x, float y) =>
            !HasBox || x >= Box.Left - Margin && x <= Box.Right + Margin && y >= Box.Bottom - Margin && y <= Box.Top + Margin;

        /// <summary>
        /// Picks entry <paramref name="index"/>: an enabled action closes the menu, then runs. True when it ran; a disabled entry, a
        /// caption, a separator or a closed menu does nothing.
        /// </summary>
        public bool Pick(int index, ChangeSource source)
        {
            if (!IsOpen || index < 0 || index >= entries.Count) return false;
            MenuEntry e = entries[index];
            if (!e.IsEnabled()) return false;
            Close(MenuClose.Picked);
            LastPicked = index;
            e.Run(source);
            return true;
        }

        /// <summary>The pointer is at (<paramref name="x"/>, <paramref name="y"/>) at <paramref name="now"/>; true when that closed the menu.</summary>
        public bool PointerAt(float x, float y, double now)
        {
            if (!IsOpen) return false;
            if (Contains(x, y))
            {
                outside = false;
                return false;
            }

            if (!outside)
            {
                outside      = true;
                outsideSince = now;
            }

            return Tick(now);
        }

        /// <summary>The time is <paramref name="now"/>: closes the menu once the pointer has been outside for the leave delay.</summary>
        public bool Tick(double now)
        {
            if (!IsOpen || !outside || now - outsideSince < LeaveDelay) return false;
            Close(MenuClose.PointerLeft);
            return true;
        }

        /// <summary>Whether the pointer is outside the menu now (its leave delay running).</summary>
        public bool PointerOutside => IsOpen && outside;

        /// <summary>A press at (<paramref name="x"/>, <paramref name="y"/>): outside the menu, it closes it. True when it closed it.</summary>
        public bool Press(float x, float y)
        {
            if (!IsOpen || HasBox && x >= Box.Left && x <= Box.Right && y >= Box.Bottom && y <= Box.Top) return false;
            Close(MenuClose.OutsidePress);
            return true;
        }

        /// <summary>A key went down (which one is not asked): the menu closes, and the key goes on to whatever reads it.</summary>
        public bool KeyDown()
        {
            if (!IsOpen) return false;
            Close(MenuClose.KeyPress);
            return true;
        }

        /// <summary>Closes the open menu with <paramref name="reason"/>; nothing happens when it is closed.</summary>
        public void Close(MenuClose reason)
        {
            if (!IsOpen) return;
            IsOpen    = false;
            LastClose = reason;
            Owner     = null;
            outside   = false;
            entries.Clear();
            Closed?.Invoke(reason);
        }

        /// <summary>The menu reason for a popup layer's dismissal (the popup stack closes a menu as it closes any popup).</summary>
        public static MenuClose FromDismiss(PopupDismiss d)
        {
            switch (d)
            {
                case PopupDismiss.OutsidePress: return MenuClose.OutsidePress;
                case PopupDismiss.OwnerClosed:
                case PopupDismiss.ParentClosed:
                    return MenuClose.OwnerClosed;
                case PopupDismiss.SceneChange: return MenuClose.SceneChange;
                case PopupDismiss.Escape: return MenuClose.KeyPress;
                default: return MenuClose.Program;
            }
        }

        /// <summary>
        /// The menu (role Menu, labelled with <see cref="Label"/>), then each entry: an action as a MenuItem (Disabled when it can't be
        /// picked, On when checked), a caption as a Group, a separator as a Separator. No part takes focus or handles a key.
        /// </summary>
        public void Describe(List<PartDescription> into)
        {
            into.Add(new PartDescription
            {
                Name = "menu", Role = PartRole.Menu, Label = Label, Flags = IsOpen ? PartFlags.Expanded : PartFlags.None
            });
            for (int i = 0; i < entries.Count; i++)
            {
                MenuEntry e = entries[i];
                string name = e.Name ?? "entry" + i;
                switch (e.Kind)
                {
                    case MenuEntryKind.Separator:
                        into.Add(new PartDescription { Name = name, Role = PartRole.Separator });
                        break;
                    case MenuEntryKind.Caption:
                        into.Add(new PartDescription { Name = name, Role = PartRole.Group, Label = e.Label });
                        break;
                    default:
                        PartFlags f = PartFlags.None;
                        if (!e.IsEnabled()) f |= PartFlags.Disabled;
                        if (e.IsChecked()) f |= PartFlags.On;
                        into.Add(new PartDescription { Name = name, Role = PartRole.MenuItem, Label = e.Label, Flags = f });
                        break;
                }
            }
        }
    }
}
