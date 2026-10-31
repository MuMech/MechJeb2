using System;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>What a button does, which sets its look: plain, the main action, a destructive one, a selected choice.</summary>
    public enum ButtonRole { Normal, Primary, Danger, Selected }

    /// <summary>One element's look: background sprite per state (sprite swap) or one sprite tinted (colour tint).</summary>
    public class ElementStyle
    {
        public Sprite Normal, Highlight, Active, Disabled;
        public Color Tint = Color.white;
        public Image.Type Type = Image.Type.Sliced;
        public Color TextColor = Color.white;

        public void ApplyImage(Image img)
        {
            if (img == null) return;
            img.sprite = Normal;
            img.type = Normal != null && Normal.border != Vector4.zero ? Type : Image.Type.Simple;
            img.color = Tint;
        }

        public void ApplySelectable(Selectable s, Image img)
        {
            ApplyImage(img);
            if (s == null) return;
            s.targetGraphic = img;
            if (Highlight != null || Active != null)
            {
                s.transition = Selectable.Transition.SpriteSwap;
                s.spriteState = new SpriteState { highlightedSprite = Highlight, pressedSprite = Active, disabledSprite = Disabled };
            }
            else
            {
                s.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = ColorBlock.defaultColorBlock;
                cb.highlightedColor = new Color(1f, 1f, 0.75f);
                cb.pressedColor = new Color(0.7f, 0.9f, 0.7f);
                s.colors = cb;
            }
        }
    }

    /// <summary>
    /// The look of every KodeKit widget, switchable at runtime (MechJeb's skin setting: Default, KSP, Compact).
    /// Widgets register a restyle action with their window; Theme.Set restyles all live windows in place.
    /// </summary>
    public class Theme
    {
        public string Name;
        public TMP_FontAsset Font;      // null: TMP default (LiberationSans)
        public float FontSize = 14;
        public float RowHeight = 22;    // fields, live cells
        public float LineHeight = 0;    // minimum height of a text row (labels, live cells, buttons); 0: the text's own height
        public int Padding = 6, Spacing = 2;
        public Color Text = Color.white, Dim = new Color(0.75f, 0.75f, 0.75f), Good = Color.green, Warn = Color.yellow, Bad = new Color(1f, 0.35f, 0.35f);
        public Color Caution = new Color(0.976f, 0.451f, 0.024f); // KSP's XKCDColors.Orange, MechJeb's OrangeLabel
        public ElementStyle Window = new ElementStyle(), Button = new ElementStyle(), Toggle = new ElementStyle(), Check = new ElementStyle(),
            Field = new ElementStyle(), Box = new ElementStyle(), Tooltip = new ElementStyle();

        // ---- the modern layout's widgets (segmented choices, switches, chips, action bars, headers) ----

        /// <summary>The accent: selected choices, switches that are on, primary actions. AccentText is text drawn on it.</summary>
        public Color Accent = new Color(0.18f, 0.75f, 0.48f), AccentText = new Color(0.02f, 0.08f, 0.05f);

        /// <summary>Button looks by role. Null: the plain button tinted (see <see cref="Role"/>).</summary>
        public ElementStyle Primary, Danger, Selected;

        /// <summary>Section headers: colour, upper case, size relative to FontSize.</summary>
        public Color Header = new Color(0.62f, 0.69f, 0.75f);
        public bool HeaderUpper = true;
        public float HeaderScale = 0.85f;

        /// <summary>Hairlines: the tabs' base line, separators.</summary>
        public Color Rule = new Color(1, 1, 1, 0.14f);

        // the tinted role looks, made once per role and Button (Role is called on every restyle of every role button)
        ElementStyle[] tinted;
        ElementStyle tintedFrom;
        Color tintedTint;

        /// <summary>The look of a button by role: its own style if the theme has one, else the plain button tinted.</summary>
        public ElementStyle Role(ButtonRole role)
        {
            ElementStyle own = role == ButtonRole.Primary ? Primary : role == ButtonRole.Danger ? Danger : role == ButtonRole.Selected ? Selected : null;
            if (own != null || role == ButtonRole.Normal) return own ?? Button;
            // a new array, not a cleared one: Copy() shares this one with the theme it was copied from
            if (tinted == null || tintedFrom != Button || tintedTint != Button.Tint)
            {
                tinted = new ElementStyle[4];
                tintedFrom = Button;
                tintedTint = Button.Tint;
            }

            int i = (int)role;
            if (tinted[i] != null) return tinted[i];
            Color tint = role == ButtonRole.Primary ? new Color(0.62f, 0.8f, 1f) : role == ButtonRole.Danger ? new Color(1f, 0.6f, 0.55f) : new Color(0.6f, 1f, 0.65f);
            return tinted[i] = new ElementStyle
            {
                Normal = Button.Normal, Highlight = Button.Highlight, Active = Button.Active, Disabled = Button.Disabled, Type = Button.Type,
                Tint = Button.Tint * tint, TextColor = Button.TextColor
            };
        }

        /// <summary>The colour of a state (green, yellow, red, dim, orange); <paramref name="normal"/> for Tone.Normal: the text, a button's text or the accent.</summary>
        public Color ToneColor(KodeWindow.Tone tone, Color normal) =>
            tone == KodeWindow.Tone.Good ? Good : tone == KodeWindow.Tone.Warn ? Warn : tone == KodeWindow.Tone.Bad ? Bad :
            tone == KodeWindow.Tone.Dim ? Dim : tone == KodeWindow.Tone.Caution ? Caution : normal;

        public static Theme Current { get; private set; }
        public static int Version { get; private set; }

        /// <summary>Raised after Set; live windows restyle themselves.</summary>
        public static event Action Changed;

        public static void Set(Theme t)
        {
            Current = t;
            Version++;
            Changed?.Invoke();
        }

        static Sprite KodeSprite(string name)
        {
            try { return SpriteLoader.GetSprite(name); }
            catch { return null; }
        }

        /// <summary>KodeUI's own sprites (GameData/KodeUI/UI), white text, 14 pt LiberationSans: the look used in the tests.</summary>
        public static Theme KodeDefault()
        {
            var t = new Theme { Name = "Default" };
            t.Window.Normal = KodeSprite("KodeUI/Default/window");
            t.Button.Normal = KodeSprite("KodeUI/Default/button_on");
            t.Toggle.Normal = KodeSprite("KodeUI/Default/toggle");
            t.Check.Normal = KodeSprite("KodeUI/Default/toggle_on");
            t.Field.Normal = KodeSprite("KodeUI/Default/background");
            t.Field.Tint = new Color(0.15f, 0.15f, 0.15f, 1);
            t.Box.Normal = KodeSprite("KodeUI/Default/background");
            t.Box.Tint = new Color(1, 1, 1, 0.5f);
            t.Tooltip.Normal = KodeSprite("KodeUI/Default/background");
            t.Tooltip.Tint = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            return t;
        }

        static ElementStyle From(UIStyle s, Color? tint = null) => new ElementStyle
        {
            Normal = s.normal.background,
            Highlight = s.highlight.background,
            Active = s.active.background,
            Disabled = s.disabled.background,
            TextColor = s.normal.textColor.a > 0 ? s.normal.textColor : Color.white,
            Tint = tint ?? Color.white
        };

        /// <summary>KSP's stock uGUI skin (UISkinManager.defaultSkin) and font (NotoSans): MechJeb's "KSP window" skin.</summary>
        public static Theme KspStock()
        {
            UISkinDef s = UISkinManager.defaultSkin;
            var t = new Theme
            {
                Name = "KSP",
                Font = UISkinManager.TMPFont,
                FontSize = 13,
                Window = From(s.window),
                Button = From(s.button),
                Toggle = From(s.toggle),
                Field = From(s.textField),
                Box = From(s.box),
                Tooltip = From(s.box, new Color(1, 1, 1, 0.95f)),
            };
            // stock toggles draw on/off as two sprites of one style; the "on" look is the toggle's active state
            t.Check = new ElementStyle { Normal = s.toggle.active.background ?? s.toggle.normal.background, Tint = new Color(0.6f, 1f, 0.6f) };
            t.Text = t.Window.TextColor;
            return t;
        }

        /// <summary>
        /// MechJeb's COMPACT skin: same sprites and font size, tighter margins and padding
        /// (GuiUtils.CopyCompactSkin only changes RectOffsets).
        /// </summary>
        public static Theme Compact(Theme from)
        {
            var t = (Theme)from.MemberwiseClone();
            t.Name = "Compact (" + from.Name + ")";
            t.RowHeight = from.RowHeight - 2;
            if (from.LineHeight > 0) t.LineHeight = from.LineHeight - 2;
            t.Padding = 3;
            t.Spacing = 0;
            return t;
        }

        // ---- the new UI's skins, built on a base theme (font, sizes) ----

        static ElementStyle Rounded(int radius, Color fill, Color edge, Color text, Color? hoverFill = null, Color? pressFill = null, Color? hoverEdge = null) =>
            new ElementStyle
            {
                Normal = Sprites.RoundedBox(radius, 1, fill, edge),
                Highlight = hoverFill.HasValue || hoverEdge.HasValue ? Sprites.RoundedBox(radius, 1, hoverFill ?? fill, hoverEdge ?? edge) : null,
                Active = pressFill.HasValue ? Sprites.RoundedBox(radius, 1, pressFill.Value, hoverEdge ?? edge) : null,
                TextColor = text
            };

        Theme Copy(string name)
        {
            var t = (Theme)MemberwiseClone();
            t.Name = name;
            return t;
        }

        /// <summary>Skin A, "Classic": KSP's stock sprites, with an accent and button roles (tinted).</summary>
        public static Theme Classic(Theme from)
        {
            Theme t = from.Copy("Classic");
            t.Accent = new Color(0.31f, 0.66f, 0.39f);
            t.AccentText = Color.white;
            t.Header = new Color(0.62f, 0.69f, 0.75f);
            return t;
        }

        /// <summary>Skin B, "Flat": solid dark panels, 4-7 px corners, one green accent.</summary>
        public static Theme Flat(Theme from)
        {
            Theme t = from.Copy("Flat");
            var text = new Color(0.87f, 0.9f, 0.93f);
            t.Text = text;
            t.Dim = new Color(0.6f, 0.66f, 0.72f);
            t.Accent = new Color(0.18f, 0.75f, 0.48f);
            t.AccentText = new Color(0.02f, 0.08f, 0.05f);
            t.Header = new Color(0.44f, 0.49f, 0.55f);
            t.Rule = new Color(0.17f, 0.2f, 0.235f);
            t.Window = Rounded(7, new Color(0.086f, 0.102f, 0.122f), new Color(0.17f, 0.2f, 0.23f), text);
            t.Button = Rounded(4, new Color(0.15f, 0.18f, 0.216f), new Color(0.2f, 0.243f, 0.286f), text,
                new Color(0.19f, 0.23f, 0.27f), new Color(0.12f, 0.15f, 0.18f), new Color(0.3f, 0.36f, 0.42f));
            t.Field = Rounded(3, new Color(0.059f, 0.075f, 0.09f), new Color(0.176f, 0.208f, 0.243f), text);
            t.Toggle = Rounded(3, new Color(0.059f, 0.075f, 0.09f), new Color(0.33f, 0.38f, 0.43f), text, null, null, new Color(0.5f, 0.56f, 0.62f));
            t.Check = new ElementStyle { Normal = Sprites.Tick, Tint = t.Accent };
            t.Box = Rounded(5, new Color(1, 1, 1, 0.03f), new Color(0.17f, 0.2f, 0.235f), text);
            t.Tooltip = Rounded(4, new Color(0.07f, 0.085f, 0.1f, 0.97f), new Color(0.2f, 0.24f, 0.28f), text);
            t.Primary = Rounded(4, t.Accent, t.Accent, t.AccentText, t.Accent * 1.12f, t.Accent * 0.85f);
            t.Selected = t.Primary;
            t.Danger = Rounded(4, new Color(0.71f, 0.28f, 0.23f, 0.12f), new Color(0.71f, 0.28f, 0.23f), new Color(1f, 0.54f, 0.48f),
                new Color(0.71f, 0.28f, 0.23f, 0.25f));
            return t;
        }

        /// <summary>Skin C, "Glass": translucent panels with cyan outlines, close to KSP's navball and map HUD.</summary>
        public static Theme Glass(Theme from)
        {
            Theme t = from.Copy("Glass");
            var text = new Color(0.86f, 0.96f, 0.99f);
            var cyan = new Color(0.333f, 0.816f, 0.949f);
            t.Text = text;
            t.Dim = new Color(0.55f, 0.72f, 0.79f);
            t.Accent = cyan;
            t.AccentText = new Color(0.012f, 0.082f, 0.11f);
            t.Header = new Color(0.44f, 0.74f, 0.83f);
            t.Rule = new Color(0.59f, 0.84f, 1f, 0.2f);
            t.Window = Rounded(9, new Color(0.055f, 0.086f, 0.118f, 0.78f), new Color(0.59f, 0.84f, 1f, 0.32f), text);
            t.Button = Rounded(3, new Color(cyan.r, cyan.g, cyan.b, 0.09f), new Color(cyan.r, cyan.g, cyan.b, 0.42f), new Color(0.8f, 0.95f, 0.98f),
                new Color(cyan.r, cyan.g, cyan.b, 0.2f), new Color(cyan.r, cyan.g, cyan.b, 0.3f));
            t.Field = Rounded(3, new Color(0, 0, 0, 0.35f), new Color(0.59f, 0.84f, 1f, 0.3f), text);
            t.Toggle = Rounded(3, new Color(0, 0, 0, 0.2f), new Color(0.59f, 0.84f, 1f, 0.6f), text, null, null, cyan);
            t.Check = new ElementStyle { Normal = Sprites.Tick, Tint = cyan };
            t.Box = Rounded(6, new Color(cyan.r, cyan.g, cyan.b, 0.04f), new Color(0.59f, 0.84f, 1f, 0.18f), text);
            t.Tooltip = Rounded(5, new Color(0.04f, 0.07f, 0.1f, 0.95f), new Color(0.59f, 0.84f, 1f, 0.4f), text);
            t.Primary = Rounded(3, new Color(cyan.r, cyan.g, cyan.b, 0.85f), cyan, t.AccentText, cyan, cyan * 0.85f);
            t.Selected = t.Primary;
            t.Danger = Rounded(3, new Color(1f, 0.43f, 0.35f, 0.1f), new Color(1f, 0.43f, 0.35f, 0.6f), new Color(1f, 0.76f, 0.72f),
                new Color(1f, 0.43f, 0.35f, 0.22f));
            return t;
        }

        static Color Rgb(int rgb, float alpha = 1) => new Color((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f, alpha);

        /// <summary>Skin D, "ZTheme": the purple-tinted dark greys and the green of the ZTheme mod (github.com/zapSNH/ZTheme), read from its
        /// textures: gen_panel_bg_meui, gen_panel_bg3, rect_round, TrackingStation_Button_Over and _Down, Green_Progress_full.</summary>
        public static Theme ZTheme(Theme from)
        {
            Theme t = from.Copy("ZTheme");
            var text = Rgb(0xDCDCE0);
            Color edge = Rgb(0x38343C), panel = Rgb(0x201C24), deep = Rgb(0x18181C);
            t.Text = text;
            t.Dim = Rgb(0x9C9C9C);
            t.Accent = Rgb(0x4CAC50);
            t.AccentText = Rgb(0x141418);
            t.Header = Rgb(0x808084);
            t.Rule = Rgb(0x302C34);
            t.Window = Rounded(5, panel, edge, text);
            t.Button = Rounded(4, Rgb(0x4C4C50), edge, text, Rgb(0x686870), Rgb(0x2C2C34), Rgb(0x545458));
            t.Field = Rounded(3, deep, Rgb(0x302C34), text);
            t.Toggle = Rounded(3, deep, Rgb(0x5C5C64), text, null, null, Rgb(0x9C9C9C));
            t.Check = new ElementStyle { Normal = Sprites.Tick, Tint = t.Accent };
            t.Box = Rounded(4, Rgb(0x28242C), Rgb(0x302C34), text);
            t.Tooltip = Rounded(4, Rgb(0x18181C, 0.97f), edge, text);
            t.Primary = Rounded(4, t.Accent, t.Accent, t.AccentText, t.Accent * 1.12f, t.Accent * 0.85f);
            t.Selected = t.Primary;
            t.Danger = Rounded(4, new Color(0.71f, 0.28f, 0.23f, 0.15f), new Color(0.71f, 0.28f, 0.23f), new Color(1f, 0.54f, 0.48f),
                new Color(0.71f, 0.28f, 0.23f, 0.28f));
            return t;
        }

        // ---- helpers for widgets ----

        public void ApplyText(TMP_Text text, Color? color = null)
        {
            if (Font != null && text.font != Font) text.font = Font;
            text.fontSize = FontSize;
            text.color = color ?? Text;
        }
    }
}
