using System.Collections.Generic;
using KodeKit.Headless;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// The skin's rule sheets (<see cref="StyleSheet"/>, <see cref="Skins"/>) on a live theme: its palette, its compiled sheet and the
    /// sprites the sheet names, made once per theme at its first use.
    /// </summary>
    public static class Styling
    {
        /// <summary>The colours a theme gives its skin's rules.</summary>
        public static SkinPalette PaletteOf(Theme th)
        {
            ElementStyle normal = th.Role(ButtonRole.Normal), primary = th.Role(ButtonRole.Primary), danger = th.Role(ButtonRole.Danger),
                selected = th.Role(ButtonRole.Selected);
            return new SkinPalette
            {
                Name = th.Name,
                Text = C(th.Text), Dim = C(th.Dim), Accent = C(th.Accent), AccentText = C(th.AccentText),
                NormalFill = C(normal.Tint), NormalText = C(normal.TextColor),
                PrimaryFill = C(primary.Tint), PrimaryText = C(primary.TextColor),
                DangerFill = C(danger.Tint), DangerText = C(danger.TextColor),
                SelectedFill = C(selected.Tint), SelectedText = C(selected.TextColor)
            };
        }

        /// <summary>The theme's sheet: its skin's rules over its palette (<see cref="Skins.Build"/>).</summary>
        public static StyleSheet SheetOf(Theme th) => th.styles ?? (th.styles = Skins.Build(PaletteOf(th)));

        /// <summary>The theme's sprite for its sheet's sprite <paramref name="index"/> (-1: none).</summary>
        public static Sprite SpriteOf(Theme th, int index)
        {
            if (index < 0) return null;
            StyleSheet sheet = SheetOf(th);
            if (th.styleSprites == null || th.styleSprites.Length != sheet.Sprites.Count)
            {
                var sprites = new Sprite[sheet.Sprites.Count];
                for (int i = 0; i < sprites.Length; i++) sprites[i] = Resolve(th, sheet.Sprites[i]);
                th.styleSprites = sprites;
            }
            return th.styleSprites[index];
        }

        static Sprite Resolve(Theme th, string id)
        {
            switch (id)
            {
                case SpriteIds.Pill: return Sprites.Pill;
                case SpriteIds.PillOutline: return Sprites.PillOutline;
                case SpriteIds.Rounded: return Sprites.Rounded;
                case SpriteIds.Frame: return Sprites.RoundedBox(3, 2, new Color(1, 1, 1, 0), Color.white);
                case SpriteIds.RoleNormal: return th.Role(ButtonRole.Normal).Normal;
                case SpriteIds.RolePrimary: return th.Role(ButtonRole.Primary).Normal;
                case SpriteIds.RoleDanger: return th.Role(ButtonRole.Danger).Normal;
                case SpriteIds.RoleSelected: return th.Role(ButtonRole.Selected).Normal;
                default:
                    Debug.LogWarning("[KodeKit] style sheet " + th.Name + ": no sprite " + id);
                    return null;
            }
        }

        public static StyleColor C(Color c) => new StyleColor(c.r, c.g, c.b, c.a);
        public static Color C(StyleColor c) => new Color(c.R, c.G, c.B, c.A);
    }

    /// <summary>
    /// A graphic painted from its skin's rule table (<see cref="StyleTable"/>) by part name and state flags: KodeKit's widget looks
    /// beyond <see cref="InstantTint"/>.
    /// <list type="bullet">
    /// <item>The widget sets On (and Invalid) from its model; the pointer sets Hover and Press, uGUI's selection Focus, and the
    /// Selectable's <c>interactable</c> (with its CanvasGroups) Disabled, caught by one shared check a frame, as InstantTint did.</item>
    /// <item>Every change paints from the table: an array index, no rule matching, no allocation. The state tint goes to the canvas
    /// renderer, so hover and press rebuild no mesh; the fill, sprite, text colour and weight are written only when they change.</item>
    /// <item>A part with a Selectable takes over its transition (None) and turns its InstantTint off: the rules have its states.</item>
    /// <item>Followers (a switch's knob, a tab's underline) take their owner's flags with their own part's rules.</item>
    /// <item>The window repaints its parts after its restyles (a skin switch): the new theme's table, the whole look written again.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StyledPart : UIBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        string part;
        Image image;
        Selectable selectable;
        TMP_Text[] labels;
        Image[] icons;
        List<StyledPart> followers;
        Theme theme;
        StyleTable table;
        WindowBehaviour window;

        // the widget's flags (On, Invalid; Focus for a part a widget focuses itself) and the pointer's and selection's
        StyleFlags flags;
        bool inside, down, selected, paintedInteractable, tracked;
        int painted = -1;
        StyleLook look;

        static readonly List<StyledPart> live = new List<StyledPart>();
        static Watcher watcher;
        static readonly TMP_Text[] NoLabels = new TMP_Text[0];
        static readonly Image[] NoIcons = new Image[0];

        /// <summary>The part's name in its skin's sheet (<see cref="StyleParts"/>); a new name repaints it at once.</summary>
        public string Part
        {
            get => part;
            set
            {
                if (value == part) return;
                part = value;
                if (theme != null) Use(theme);
            }
        }

        /// <summary>The flags the part is painted with now, the pointer's and Disabled included.</summary>
        public StyleFlags Flags => Current();

        /// <summary>The look painted now (for tests).</summary>
        public StyleLook Look => look;

        /// <summary>The graphic it paints.</summary>
        public Image Image => image;

        /// <summary>
        /// Paints <paramref name="img"/> (on <paramref name="go"/>, which gets the part) as <paramref name="partName"/>, with
        /// <paramref name="s"/>'s states when given, and the texts and icons whose colour the part's rules set. Registers with the
        /// window, which repaints it after a skin switch.
        /// </summary>
        public static StyledPart Add(GameObject go, string partName, Image img, Selectable s = null, TMP_Text[] texts = null, Image[] iconImages = null)
        {
            StyledPart p = go.GetComponent<StyledPart>() ?? go.AddComponent<StyledPart>();
            p.part = partName;
            p.image = img;
            p.selectable = s;
            p.labels = texts ?? NoLabels;
            p.icons = iconImages ?? NoIcons;
            if (s != null)
            {
                s.targetGraphic = img;
                s.transition = Selectable.Transition.None;
                InstantTint.Off(s);
                if (p.isActiveAndEnabled) p.Track(true);
            }
            p.Register();
            return p;
        }

        /// <summary>
        /// The keyboard-focus frame of a field or a dialog's button: one sprite and the accent in every skin
        /// (<see cref="StyleParts.FocusFrame"/>), over <paramref name="owner"/>'s rect grown by <paramref name="outset"/> px, outside
        /// its layout. Shown while its Focus flag is set.
        /// </summary>
        public static StyledPart FocusFrame(Transform owner, float outset)
        {
            var go = new GameObject("Focus", typeof(RectTransform), typeof(Image), typeof(LayoutElement)) { layer = owner.gameObject.layer };
            go.transform.SetParent(owner, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-outset, -outset);
            rt.offsetMax = new Vector2(outset, outset);
            var frame = go.GetComponent<Image>();
            frame.raycastTarget = false;
            return Add(go, StyleParts.FocusFrame, frame);
        }

        /// <summary>A part on <paramref name="go"/> that takes this part's flags (its own rules).</summary>
        public StyledPart Follower(GameObject go, string partName, Image img)
        {
            StyledPart f = go.GetComponent<StyledPart>() ?? go.AddComponent<StyledPart>();
            f.part = partName;
            f.image = img;
            f.labels = NoLabels;
            f.icons = NoIcons;
            f.flags = Current() & ~(StyleFlags.Hover | StyleFlags.Press);
            if (followers == null) followers = new List<StyledPart>();
            followers.Add(f);
            f.Register();
            return f;
        }

        // with the window, which repaints the part on a skin switch; painted now with the window's theme
        void Register()
        {
            if (window == null)
            {
                window = GetComponentInParent<WindowBehaviour>();
                if (window != null) window.AddPart(this);
            }
            Restyle();
        }

        /// <summary>Paints the part with its window's theme (Theme.Current outside a window), every value written.</summary>
        public void Restyle()
        {
            if (this == null) return; // destroyed with its widget; its window's restyle list still holds it
            Use(window != null ? window.Theme ?? KodeWindow.ThemeNow : KodeWindow.ThemeNow);
        }

        /// <summary>Sets or clears a widget flag (On, Invalid, Focus) and repaints when the look changes.</summary>
        public void Set(StyleFlags flag, bool on)
        {
            StyleFlags f = on ? flags | flag : flags & ~flag;
            if (f == flags) return;
            flags = f;
            Paint(false);
        }

        /// <summary>Paints the part with <paramref name="th"/>'s sheet, every value written (a skin switch, a new part name).</summary>
        public void Use(Theme th)
        {
            if (th == null) return;
            theme = th;
            table = Styling.SheetOf(th).Table(part);
            if (selectable != null)
            {
                // a restyle of the widget under the part (its skin look: sprite swap or InstantTint) gave the Selectable a transition again
                selectable.transition = Selectable.Transition.None;
                InstantTint.Off(selectable);
            }
            Paint(true);
        }

        StyleFlags Current()
        {
            StyleFlags f = flags;
            if (inside) f |= StyleFlags.Hover;
            if (down) f |= StyleFlags.Press;
            if (selected) f |= StyleFlags.Focus;
            if (selectable != null && !paintedInteractable) f |= StyleFlags.Disabled;
            return f;
        }

        void Paint(bool force)
        {
            if (table == null) return;
            if (selectable != null) paintedInteractable = selectable.IsInteractable();
            StyleFlags f = Current();
            int i = (int)f;
            if (!force && i == painted) return;
            StyleLook l = table[f];
            bool all = force || painted < 0;
            StyleLook old = look;
            painted = i;
            look = l;
            if (image != null)
            {
                if (l.SetsSprite && (all || l.SpriteIndex != old.SpriteIndex || !old.SetsSprite))
                {
                    Sprite sp = Styling.SpriteOf(theme, l.SpriteIndex);
                    image.sprite = sp;
                    image.type = sp != null && sp.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                }
                // the fill only when it changes: a click's flash (ClickFlash) fades the fill and must not be cut by a hover
                if (all || l.Fill != old.Fill) image.color = Styling.C(l.Fill);
                if (all || l.Tint != old.Tint) image.canvasRenderer.SetColor(Styling.C(l.Tint));
                if (l.SetsHidden && (all || l.Hidden != old.Hidden)) image.enabled = !l.Hidden;
            }
            if (l.SetsText && (all || l.Text != old.Text))
            {
                Color c = Styling.C(l.Text);
                for (int k = 0; k < labels.Length; k++) labels[k].color = c;
                for (int k = 0; k < icons.Length; k++) icons[k].color = c;
            }
            if (l.SetsBold && (all || l.Bold != old.Bold))
                for (int k = 0; k < labels.Length; k++)
                    labels[k].fontStyle = l.Bold ? labels[k].fontStyle | FontStyles.Bold : labels[k].fontStyle & ~FontStyles.Bold;
            if (followers == null) return;
            StyleFlags owned = f & ~(StyleFlags.Hover | StyleFlags.Press);
            for (int k = 0; k < followers.Count; k++)
            {
                StyledPart fo = followers[k];
                if (fo == null || fo.flags == owned) continue;
                fo.flags = owned;
                fo.Paint(false);
            }
        }

        // in the shared check of interactable changes while enabled, for a part with a Selectable
        void Track(bool on)
        {
            if (on == tracked) return;
            tracked = on;
            if (!on)
            {
                live.Remove(this);
                return;
            }
            live.Add(this);
            if (watcher != null) return;
            var go = new GameObject("KodeKit StyledPart");
            DontDestroyOnLoad(go);
            watcher = go.AddComponent<Watcher>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            inside = down = false;
            if (selectable != null) Track(true);
            // the whole look again: a graphic shown again may have lost its canvas renderer's tint
            Paint(true);
        }

        protected override void OnDisable()
        {
            Track(false);
            base.OnDisable();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            inside = true;
            Paint(false);
        }

        public void OnPointerExit(PointerEventData e)
        {
            inside = false;
            Paint(false);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            down = true;
            Paint(false);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            down = false;
            Paint(false);
        }

        public void OnSelect(BaseEventData e)
        {
            selected = true;
            Paint(false);
        }

        public void OnDeselect(BaseEventData e)
        {
            selected = false;
            Paint(false);
        }

        /// <summary>Repaints the parts whose Selectable became (non-)interactable since their last paint: a field read each.</summary>
        sealed class Watcher : MonoBehaviour
        {
            void LateUpdate()
            {
                for (int i = 0; i < live.Count; i++)
                {
                    StyledPart p = live[i];
                    if (p.selectable != null && p.selectable.IsInteractable() != p.paintedInteractable) p.Paint(false);
                }
            }
        }
    }
}
