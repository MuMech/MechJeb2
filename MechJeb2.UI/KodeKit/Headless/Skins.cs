#if !CSHARP_7_3
#nullable disable
#endif
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>
    /// The style parts KodeKit's widgets paint through a skin's <see cref="StyleSheet"/>. A '.' refines a part ("button.primary" takes
    /// "button"'s rules first); a '-' names a part of its own ("switch-knob" takes none of "switch"'s).
    /// </summary>
    public static class StyleParts
    {
        /// <summary>The plain button (a role button whose role is Normal now, <c>BindRole</c>).</summary>
        public const string ButtonNormal = "button.normal";

        /// <summary>The main action (<c>ButtonRole.Primary</c>).</summary>
        public const string ButtonPrimary = "button.primary";

        /// <summary>A destructive action (<c>ButtonRole.Danger</c>).</summary>
        public const string ButtonDanger = "button.danger";

        /// <summary>A selected choice (<c>ButtonRole.Selected</c>: a state button whose mode is on).</summary>
        public const string ButtonSelected = "button.selected";

        /// <summary>A switch's pill track (On: on), also <c>Switch3</c>'s (On: the right position).</summary>
        public const string Switch = "switch";

        /// <summary>A switch's knob (On as its track).</summary>
        public const string SwitchKnob = "switch-knob";

        /// <summary>A cell of a joined segmented bar (On: chosen).</summary>
        public const string Segment = "segmented";

        /// <summary>A tab of a segmented choice drawn as tabs (On: chosen).</summary>
        public const string Tab = "segmented-tab";

        /// <summary>The chosen tab's underline (shown when On).</summary>
        public const string TabLine = "segmented-tab-line";

        /// <summary>A pill of a segmented choice drawn as pills (On: chosen).</summary>
        public const string Pill = "segmented-pill";

        /// <summary>A stepper's field, drawn on the stepper's background.</summary>
        public const string StepperField = "stepper-field";

        /// <summary>A stepper's − and + buttons.</summary>
        public const string StepperButton = "stepper-button";

        /// <summary>An on/off chip (On: on).</summary>
        public const string Chip = "chip";

        /// <summary>A borderless title-bar button.</summary>
        public const string TitleIcon = "title-icon";

        /// <summary>The title bar's close button (red under the pointer).</summary>
        public const string TitleClose = "title-icon.close";

        /// <summary>A row of a plain list (On: selected).</summary>
        public const string ListRow = "list-row";

        /// <summary>A flat clickable column header (a list row with dim text).</summary>
        public const string HeaderButton = "list-row.header";

        /// <summary>The keyboard-focus frame of a field or a dialog's button (shown with Focus).</summary>
        public const string FocusFrame = "focus-frame";

        /// <summary>Every part a KodeKit widget paints through a sheet.</summary>
        public static readonly string[] All =
        {
            ButtonNormal, ButtonPrimary, ButtonDanger, ButtonSelected, Switch, SwitchKnob, Segment, Tab, TabLine, Pill, StepperField,
            StepperButton, Chip, TitleIcon, TitleClose, ListRow, HeaderButton, FocusFrame
        };
    }

    /// <summary>The sprite ids a sheet names; KodeKit maps each to a sprite of the theme the sheet belongs to.</summary>
    public static class SpriteIds
    {
        /// <summary>A pill (a switch's track, a pill or chip that is on).</summary>
        public const string Pill = "pill";

        /// <summary>A pill's outline (a pill or chip that is off).</summary>
        public const string PillOutline = "pill-outline";

        /// <summary>A small rounded box (a title-bar button's hover).</summary>
        public const string Rounded = "rounded";

        /// <summary>A 2 px frame, rounded like a field (the focus frame).</summary>
        public const string Frame = "frame";

        /// <summary>The skin's button by role: its own sprite, or the plain button's (KodeKit's <c>Theme.Role</c>).</summary>
        public const string RoleNormal = "role.normal", RolePrimary = "role.primary", RoleDanger = "role.danger", RoleSelected = "role.selected";

        public static readonly string[] All = { Pill, PillOutline, Rounded, Frame, RoleNormal, RolePrimary, RoleDanger, RoleSelected };
    }

    /// <summary>
    /// The colours a skin's rules take from its theme: text, accent and the buttons by role (fill and text). KodeKit reads them from
    /// the live theme (<c>Styling.PaletteOf</c>); the static ones here are what MechJeb's skins give (<c>KodeHost.ThemeFor</c>), for
    /// the tests, and Spike 159 checks that they match the game's.
    /// </summary>
    [NotAWidget("data: the colours a skin's rules are built from")]
    public sealed class SkinPalette
    {
        public string Name;
        public StyleColor Text, Dim, Accent, AccentText;

        /// <summary>The fill (its Image colour) and text of a button by role.</summary>
        public StyleColor NormalFill, NormalText, PrimaryFill, PrimaryText, DangerFill, DangerText, SelectedFill, SelectedText;

        public SkinPalette Copy(string name)
        {
            var p = (SkinPalette)MemberwiseClone();
            p.Name = name;
            return p;
        }

        /// <summary>The plain button's fill and text by role, for a skin whose roles are its plain button tinted (Classic).</summary>
        static SkinPalette Tinted(string name, StyleColor text, StyleColor dim, StyleColor accent, StyleColor accentText, StyleColor buttonText)
        {
            StyleColor white = StyleColor.White;
            return new SkinPalette
            {
                Name = name, Text = text, Dim = dim, Accent = accent, AccentText = accentText,
                NormalFill = white, NormalText = buttonText,
                PrimaryFill = white * new StyleColor(0.62f, 0.8f, 1f), PrimaryText = buttonText,
                DangerFill = white * new StyleColor(1f, 0.6f, 0.55f), DangerText = buttonText,
                SelectedFill = white * new StyleColor(0.6f, 1f, 0.65f), SelectedText = buttonText
            };
        }

        /// <summary>The roles of a drawn skin: their own sprites carry the colour, so the fill is white.</summary>
        static SkinPalette Drawn(string name, StyleColor text, StyleColor dim, StyleColor accent, StyleColor accentText, StyleColor buttonText,
            StyleColor dangerText)
        {
            StyleColor white = StyleColor.White;
            return new SkinPalette
            {
                Name = name, Text = text, Dim = dim, Accent = accent, AccentText = accentText,
                NormalFill = white, NormalText = buttonText,
                PrimaryFill = white, PrimaryText = accentText,
                DangerFill = white, DangerText = dangerText,
                SelectedFill = white, SelectedText = accentText
            };
        }

        // KSP's stock button text (UISkinManager.defaultSkin.button.normal.textColor, as the game reads it), which Classic's buttons keep
        static readonly StyleColor KspButtonText = new StyleColor(0.8295834f, 0.9583512f, 0.9925373f);

        /// <summary>Skin A: KSP's stock sprites, MechJeb's text, the plain button tinted by role.</summary>
        public static SkinPalette Classic() =>
            Tinted("Classic", StyleColor.Grey(0.9f), StyleColor.Grey(0.75f), new StyleColor(0.31f, 0.66f, 0.39f), StyleColor.White, KspButtonText);

        /// <summary>Skin B: solid dark panels, one green accent.</summary>
        public static SkinPalette Flat()
        {
            var text = new StyleColor(0.87f, 0.9f, 0.93f);
            return Drawn("Flat", text, new StyleColor(0.6f, 0.66f, 0.72f), new StyleColor(0.18f, 0.75f, 0.48f), new StyleColor(0.02f, 0.08f, 0.05f),
                text, new StyleColor(1f, 0.54f, 0.48f));
        }

        /// <summary>Skin C: translucent panels, cyan.</summary>
        public static SkinPalette Glass() =>
            Drawn("Glass", new StyleColor(0.86f, 0.96f, 0.99f), new StyleColor(0.55f, 0.72f, 0.79f), new StyleColor(0.333f, 0.816f, 0.949f),
                new StyleColor(0.012f, 0.082f, 0.11f), new StyleColor(0.8f, 0.95f, 0.98f), new StyleColor(1f, 0.76f, 0.72f));

        /// <summary>Skin D: ZTheme's greys and green.</summary>
        public static SkinPalette ZTheme()
        {
            StyleColor text = StyleColor.Rgb(0xDCDCE0);
            return Drawn("ZTheme", text, StyleColor.Rgb(0x9C9C9C), StyleColor.Rgb(0x4CAC50), StyleColor.Rgb(0x141418), text, new StyleColor(1f, 0.54f, 0.48f));
        }
    }

    /// <summary>
    /// MechJeb's skins as rule sets. Every skin has the same rules over its own palette, so a state looks alike in all of them (the
    /// accent where a choice is on, the same hover and press tints); a skin that needs another look adds rules after
    /// <see cref="Common"/>. Compact is a spacing, not a look: its sheet is its skin's (<see cref="Compact"/>).
    /// <list type="bullet">
    /// <item>The state tints follow uGUI's order, as <c>InstantTint</c> did: disabled over pressed over focused over hovered. A focused
    /// part (uGUI's selected: a button just clicked) shows its normal tint, not the hover one.</item>
    /// <item>A part whose tints change when it is on has its "on" tints in rules that need On, one flag more than the "off" ones, so they
    /// win in every state.</item>
    /// </list>
    /// </summary>
    public static class Skins
    {
        /// <summary>The skins' names, as MechJeb's setting lists them.</summary>
        public static readonly string[] Names = { "Classic", "Flat", "Glass", "ZTheme" };

        /// <summary>The palette MechJeb's skin <paramref name="name"/> gives (null: no such skin).</summary>
        public static SkinPalette PaletteFor(string name)
        {
            switch (name)
            {
                case "Classic": return SkinPalette.Classic();
                case "Flat": return SkinPalette.Flat();
                case "Glass": return SkinPalette.Glass();
                case "ZTheme": return SkinPalette.ZTheme();
                default: return null;
            }
        }

        /// <summary>The sheet of MechJeb's skin <paramref name="name"/> with its own palette.</summary>
        public static StyleSheet For(string name) => Build(PaletteFor(name) ?? SkinPalette.Classic().Copy(name));

        /// <summary>The sheet of a skin at MechJeb's COMPACT spacing: its rules, named after it.</summary>
        public static StyleSheet Compact(string name) => Build((PaletteFor(name) ?? SkinPalette.Classic()).Copy("Compact (" + name + ")"));

        /// <summary>A skin's sheet over <paramref name="p"/>.</summary>
        public static StyleSheet Build(SkinPalette p)
        {
            var s = new StyleSheet(p.Name);
            Common(s, p);
            return s;
        }

        // the tints of uGUI's transitions that KodeKit used: a grey at rest so hover can brighten to white, a white wash over a clear cell
        static readonly StyleColor Dimmed = StyleColor.Grey(0.8f), Pressed = StyleColor.Grey(0.7f), Grey92 = StyleColor.Grey(0.92f),
            Grey80 = StyleColor.Grey(0.8f), Grey78 = StyleColor.Grey(0.78f),
            HoverWash = new StyleColor(1, 1, 1, 0.1f), PressWash = new StyleColor(1, 1, 1, 0.18f),
            RowHover = new StyleColor(1, 1, 1, 0.12f), RowPress = new StyleColor(1, 1, 1, 0.2f),
            CloseRed = new StyleColor(0.85f, 0.25f, 0.2f, 0.85f), RowSelected = new StyleColor(0.95f, 0.82f, 0.35f);

        /// <summary>
        /// The disabled tint of a part whose tint at rest is <paramref name="normal"/>: uGUI's grey (200, 200, 200, 128), as see-through
        /// as <paramref name="normal"/>, so a cell clear at rest stays clear when disabled.
        /// </summary>
        public static StyleColor DisabledTint(StyleColor normal)
        {
            StyleColor d = StyleColor.FromBytes(200, 200, 200, 128);
            return d.WithAlpha(d.A * normal.A);
        }

        /// <summary>The state tints of <paramref name="part"/> when the flags of <paramref name="need"/> are set (None: every state).</summary>
        public static void Tints(StyleSheet s, string part, StyleFlags need, StyleColor normal, StyleColor hover, StyleColor press)
        {
            s.Rule(part, need).Tint(normal);
            s.Rule(part, need | StyleFlags.Hover).Tint(hover);
            s.Rule(part, need | StyleFlags.Focus).Tint(normal);
            s.Rule(part, need | StyleFlags.Press).Tint(press);
            s.Rule(part, need | StyleFlags.Disabled).Tint(DisabledTint(normal));
        }

        /// <summary>The rules every skin has.</summary>
        public static void Common(StyleSheet s, SkinPalette p)
        {
            StyleColor white = StyleColor.White, clear = StyleColor.Clear;

            // buttons with a role: the skin's sprite by role, its fill and text (icons too); one set of tints for every role
            Tints(s, "button", StyleFlags.None, white, Grey92, Grey78);
            s.Rule(StyleParts.ButtonNormal).Sprite(SpriteIds.RoleNormal).Fill(p.NormalFill).Text(p.NormalText);
            s.Rule(StyleParts.ButtonPrimary).Sprite(SpriteIds.RolePrimary).Fill(p.PrimaryFill).Text(p.PrimaryText);
            s.Rule(StyleParts.ButtonDanger).Sprite(SpriteIds.RoleDanger).Fill(p.DangerFill).Text(p.DangerText);
            s.Rule(StyleParts.ButtonSelected).Sprite(SpriteIds.RoleSelected).Fill(p.SelectedFill).Text(p.SelectedText);

            // switch: a pill track, dim when off, the accent when on; its knob white when on
            s.Rule(StyleParts.Switch).Sprite(SpriteIds.Pill).Fill(p.Dim.WithAlpha(0.35f));
            s.Rule(StyleParts.Switch, StyleFlags.On).Fill(p.Accent);
            Tints(s, StyleParts.Switch, StyleFlags.None, Dimmed, white, Pressed);
            s.Rule(StyleParts.SwitchKnob).Fill(p.Text.WithAlpha(0.9f));
            s.Rule(StyleParts.SwitchKnob, StyleFlags.On).Fill(white);

            // a joined segmented bar: clear cells with a white wash under the pointer; the chosen one in the selected role's look
            s.Rule(StyleParts.Segment).Sprite(null).Fill(white).Text(p.Text);
            Tints(s, StyleParts.Segment, StyleFlags.None, clear, HoverWash, PressWash);
            s.Rule(StyleParts.Segment, StyleFlags.On).Sprite(SpriteIds.RoleSelected).Fill(p.SelectedFill).Text(p.SelectedText);
            Tints(s, StyleParts.Segment, StyleFlags.On, white, Grey92, Grey80);

            // tabs: no cell at all, the chosen one's text bold and bright over an accent underline
            s.Rule(StyleParts.Tab).Sprite(null).Fill(white).Text(p.Dim).Bold(false);
            s.Rule(StyleParts.Tab, StyleFlags.On).Text(p.Text).Bold(true);
            Tints(s, StyleParts.Tab, StyleFlags.None, clear, clear, clear);
            s.Rule(StyleParts.TabLine).Fill(p.Accent).Hidden(true);
            s.Rule(StyleParts.TabLine, StyleFlags.On).Hidden(false);

            // pills: an outline when off, the accent when chosen
            s.Rule(StyleParts.Pill).Sprite(SpriteIds.PillOutline).Fill(p.Dim.WithAlpha(0.8f)).Text(p.Text);
            Tints(s, StyleParts.Pill, StyleFlags.None, Dimmed, white, white);
            s.Rule(StyleParts.Pill, StyleFlags.On).Sprite(SpriteIds.Pill).Fill(p.Accent).Text(p.AccentText);
            Tints(s, StyleParts.Pill, StyleFlags.On, white, Grey92, Grey80);

            // a stepper: its field and buttons drawn on its background, washed under the pointer
            s.Rule(StyleParts.StepperField).Sprite(null).Fill(white);
            Tints(s, StyleParts.StepperField, StyleFlags.None, clear, HoverWash, clear);
            s.Rule(StyleParts.StepperButton).Sprite(null).Fill(white).Text(p.Text).Bold(true);
            Tints(s, StyleParts.StepperButton, StyleFlags.None, clear, HoverWash, PressWash);

            // chips: an outline and dim text when off, a light pill when on
            s.Rule(StyleParts.Chip).Sprite(SpriteIds.PillOutline).Fill(p.Dim.WithAlpha(0.55f)).Text(p.Dim);
            s.Rule(StyleParts.Chip, StyleFlags.On).Sprite(SpriteIds.Pill).Fill(p.Text.WithAlpha(0.2f)).Text(p.Text);
            Tints(s, StyleParts.Chip, StyleFlags.None, Dimmed, white, white);

            // title-bar buttons: nothing at rest, a light box under the pointer; the close button's is red
            StyleColor iconHover = p.Text.WithAlpha(0.15f);
            s.Rule(StyleParts.TitleIcon).Sprite(SpriteIds.Rounded).Fill(white).Text(p.Text.WithAlpha(0.85f)).Bold(true);
            Tints(s, StyleParts.TitleIcon, StyleFlags.None, clear, iconHover, iconHover);
            Tints(s, StyleParts.TitleClose, StyleFlags.None, clear, CloseRed, CloseRed);

            // list rows: a wash under the pointer, the selected row's text in its bar's colour; column headers dim
            s.Rule(StyleParts.ListRow).Sprite(null).Fill(white).Text(p.Text);
            s.Rule(StyleParts.ListRow, StyleFlags.On).Text(RowSelected);
            Tints(s, StyleParts.ListRow, StyleFlags.None, clear, RowHover, RowPress);
            s.Rule(StyleParts.HeaderButton).Text(p.Dim);
            s.Rule(StyleParts.HeaderButton, StyleFlags.On).Text(RowSelected);

            // the keyboard focus: one frame in the accent, for fields and dialog buttons alike
            s.Rule(StyleParts.FocusFrame).Sprite(SpriteIds.Frame).Fill(p.Accent).Hidden(true);
            s.Rule(StyleParts.FocusFrame, StyleFlags.Focus).Hidden(false);
        }

        /// <summary>Every skin's sheet, normal and COMPACT, with the default palettes (for tests).</summary>
        public static List<StyleSheet> All()
        {
            var all = new List<StyleSheet>();
            foreach (string n in Names)
            {
                all.Add(For(n));
                all.Add(Compact(n));
            }
            return all;
        }
    }
}
