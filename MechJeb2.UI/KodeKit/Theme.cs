using System;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
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
        public ElementStyle Window = new ElementStyle(), Button = new ElementStyle(), Toggle = new ElementStyle(), Check = new ElementStyle(),
            Field = new ElementStyle(), Box = new ElementStyle(), Tooltip = new ElementStyle();

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

        // ---- helpers for widgets ----

        public void ApplyText(TMP_Text text, Color? color = null)
        {
            if (Font != null && text.font != Font) text.font = Font;
            text.fontSize = FontSize;
            text.color = color ?? Text;
        }
    }
}
