#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>
    /// The state flags a styled part is painted by, as CSS's <c>:hover</c>, <c>:active</c>, <c>:focus</c>, <c>[data-state=on]</c>,
    /// <c>:disabled</c> and <c>:invalid</c>. On, Focus, Disabled and Invalid are <see cref="PartFlags"/>' own (<see cref="StyleStates.From"/>);
    /// Hover and Press come from the pointer, which a model doesn't track.
    /// </summary>
    [Flags]
    public enum StyleFlags
    {
        None = 0,

        /// <summary>The pointer is over the part.</summary>
        Hover = 1,

        /// <summary>The left button went down on the part and is still down.</summary>
        Press = 2,

        /// <summary>The part has the keyboard (uGUI's selected state, a focused field).</summary>
        Focus = 4,

        /// <summary>On, chosen or selected (a switch that is on, the chosen segment, a selected list row).</summary>
        On = 8,

        /// <summary>The part can't be used now (its Selectable isn't interactable).</summary>
        Disabled = 16,

        /// <summary>The part's value is refused (a field's text that doesn't parse).</summary>
        Invalid = 32
    }

    /// <summary>The flag combinations of <see cref="StyleFlags"/>, and the flags of a model's part.</summary>
    public static class StyleStates
    {
        /// <summary>The number of flag combinations, the length of every <see cref="StyleTable"/>.</summary>
        public const int Count = 64;

        /// <summary>The style flags of a model's part: On, Focus, Disabled and Invalid as they are; the other part flags have no style.</summary>
        public static StyleFlags From(PartFlags f)
        {
            StyleFlags s = StyleFlags.None;
            if ((f & PartFlags.On) != 0) s |= StyleFlags.On;
            if ((f & PartFlags.Focus) != 0) s |= StyleFlags.Focus;
            if ((f & PartFlags.Disabled) != 0) s |= StyleFlags.Disabled;
            if ((f & PartFlags.Invalid) != 0) s |= StyleFlags.Invalid;
            return s;
        }

        /// <summary>How many flags <paramref name="f"/> has.</summary>
        public static int Bits(StyleFlags f)
        {
            int n = 0;
            for (int v = (int)f; v != 0; v &= v - 1) n++;
            return n;
        }
    }

    /// <summary>A colour as four floats from 0 to 1, as Unity's <c>Color</c> (KodeKit converts it as it paints).</summary>
    public struct StyleColor : IEquatable<StyleColor>
    {
        public float R, G, B, A;

        public StyleColor(float r, float g, float b, float a = 1)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static readonly StyleColor White = new StyleColor(1, 1, 1);

        /// <summary>White at alpha 0: draws nothing, and a tint of it hides what it tints.</summary>
        public static readonly StyleColor Clear = new StyleColor(1, 1, 1, 0);

        /// <summary>A colour from bytes, as Unity's <c>Color32</c> converts to <c>Color</c> (each byte / 255).</summary>
        public static StyleColor FromBytes(byte r, byte g, byte b, byte a = 255) => new StyleColor(r / 255f, g / 255f, b / 255f, a / 255f);

        /// <summary>A colour from 0xRRGGBB.</summary>
        public static StyleColor Rgb(int rgb, float alpha = 1) => new StyleColor((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f, alpha);

        /// <summary>A grey of <paramref name="v"/>, opaque.</summary>
        public static StyleColor Grey(float v) => new StyleColor(v, v, v);

        /// <summary>This colour with its alpha set to <paramref name="a"/>.</summary>
        public StyleColor WithAlpha(float a) => new StyleColor(R, G, B, a);

        /// <summary>Component by component, as Unity's <c>Color * Color</c>.</summary>
        public static StyleColor operator *(StyleColor a, StyleColor b) => new StyleColor(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);

        public bool Equals(StyleColor o) => R == o.R && G == o.G && B == o.B && A == o.A;
        public override bool Equals(object obj) => obj is StyleColor c && Equals(c);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = R.GetHashCode();
                h = h * 397 ^ G.GetHashCode();
                h = h * 397 ^ B.GetHashCode();
                return h * 397 ^ A.GetHashCode();
            }
        }

        public static bool operator ==(StyleColor a, StyleColor b) => a.Equals(b);
        public static bool operator !=(StyleColor a, StyleColor b) => !a.Equals(b);

        public override string ToString() => $"({R:0.###}, {G:0.###}, {B:0.###}, {A:0.###})";
    }

    /// <summary>
    /// What a part looks like in one state, resolved from its rules. A value no rule sets keeps its default: no sprite change, a
    /// white fill and tint, the text and its weight left as the widget drew them, shown.
    /// </summary>
    public struct StyleLook
    {
        /// <summary>Whether a rule sets the background sprite (to <see cref="Sprite"/>; null: no sprite).</summary>
        public bool SetsSprite;

        /// <summary>The background sprite's id (<see cref="SpriteIds"/>), or null for none.</summary>
        public string Sprite;

        /// <summary>The sprite's index in its sheet's <see cref="StyleSheet.Sprites"/> (-1: none): KodeKit's sprites by index.</summary>
        public int SpriteIndex;

        /// <summary>The graphic's own colour (Unity's <c>Graphic.color</c>): the part's fill, changed with the look's state.</summary>
        public StyleColor Fill;

        /// <summary>
        /// The state tint over the fill (uGUI's transition colour, the canvas renderer's colour): hover and press change it without
        /// rebuilding the graphic's mesh.
        /// </summary>
        public StyleColor Tint;

        /// <summary>Whether a rule sets the colour of the part's texts and icons (to <see cref="Text"/>).</summary>
        public bool SetsText;

        public StyleColor Text;

        /// <summary>Whether a rule sets the texts' weight (to <see cref="Bold"/>).</summary>
        public bool SetsBold;

        public bool Bold;

        /// <summary>Whether a rule says if the part is drawn at all (<see cref="Hidden"/>).</summary>
        public bool SetsHidden;

        public bool Hidden;

        /// <summary>Whether two looks paint the same.</summary>
        public bool SameAs(StyleLook o) =>
            SetsSprite == o.SetsSprite && Sprite == o.Sprite && Fill == o.Fill && Tint == o.Tint && SetsText == o.SetsText && Text == o.Text &&
            SetsBold == o.SetsBold && Bold == o.Bold && SetsHidden == o.SetsHidden && Hidden == o.Hidden;
    }

    /// <summary>
    /// One rule of a <see cref="StyleSheet"/>: for a part (and the parts that refine it) when every flag of <see cref="Need"/> is
    /// set and none of <see cref="Not"/>, the values it sets. Made by <see cref="StyleSheet.Rule"/> and filled by its chained setters.
    /// </summary>
    [NotAWidget("data: one rule of a style sheet")]
    public sealed class StyleRule
    {
        internal StyleRule(StyleSheet sheet, string part, StyleFlags need, StyleFlags not, int order)
        {
            this.sheet = sheet;
            Part = part;
            Need = need;
            Not = not;
            Order = order;
        }

        readonly StyleSheet sheet;

        public string Part { get; }
        public StyleFlags Need { get; }
        public StyleFlags Not { get; }

        /// <summary>Its place in the sheet: of two rules equally specific, the later wins.</summary>
        public int Order { get; }

        internal bool setsSprite, setsFill, setsTint, setsText, setsBold, setsHidden;
        internal string sprite;
        internal StyleColor fill, tint, text;
        internal bool bold, hidden;

        /// <summary>Whether the rule applies to <paramref name="part"/>: its own part, or a part that refines it ("button" to "button.primary").</summary>
        public bool AppliesTo(string part) =>
            part == Part || part.Length > Part.Length && part[Part.Length] == '.' && part.StartsWith(Part, StringComparison.Ordinal);

        /// <summary>Whether the rule matches the flags <paramref name="f"/>.</summary>
        public bool Matches(StyleFlags f) => (f & Need) == Need && (f & Not) == 0;

        /// <summary>The background sprite (an id of <see cref="SpriteIds"/>; null: none).</summary>
        public StyleRule Sprite(string id)
        {
            sheet.Changing();
            setsSprite = true;
            sprite = id;
            return this;
        }

        public StyleRule Fill(StyleColor c)
        {
            sheet.Changing();
            setsFill = true;
            fill = c;
            return this;
        }

        public StyleRule Tint(StyleColor c)
        {
            sheet.Changing();
            setsTint = true;
            tint = c;
            return this;
        }

        public StyleRule Text(StyleColor c)
        {
            sheet.Changing();
            setsText = true;
            text = c;
            return this;
        }

        public StyleRule Bold(bool on)
        {
            sheet.Changing();
            setsBold = true;
            bold = on;
            return this;
        }

        public StyleRule Hidden(bool on)
        {
            sheet.Changing();
            setsHidden = true;
            hidden = on;
            return this;
        }

        /// <summary>The part's depth: 1 for "button", 2 for "button.primary".</summary>
        internal static int Depth(string part)
        {
            int d = 1;
            for (int i = 0; i < part.Length; i++)
                if (part[i] == '.')
                    d++;
            return d;
        }
    }

    /// <summary>A part's looks in all <see cref="StyleStates.Count"/> flag combinations, compiled once by its sheet.</summary>
    [NotAWidget("data: the compiled looks of one part")]
    public sealed class StyleTable
    {
        internal StyleTable(string part, StyleLook[] looks, int rules)
        {
            Part = part;
            this.looks = looks;
            Rules = rules;
        }

        readonly StyleLook[] looks;

        public string Part { get; }

        /// <summary>How many of the sheet's rules apply to the part (0: nothing styles it).</summary>
        public int Rules { get; }

        /// <summary>The look in the state <paramref name="f"/>: an array index, no matching, no allocation.</summary>
        public StyleLook this[StyleFlags f] => looks[(int)f & StyleStates.Count - 1];
    }

    /// <summary>
    /// A skin as data: rules keyed by part name and state flags (Spike 131's resolver), compiled into one table per part.
    /// <list type="bullet">
    /// <item>A rule applies to its part and to every part that refines it: "button" to "button.primary" (a '.' after the name).</item>
    /// <item>Of the rules that match a part's state, the more specific is applied later and wins, value by value: first the deeper
    /// part ("button.primary" over "button"), then the rule with more flags in <see cref="StyleRule.Need"/>, then the later rule.
    /// A rule sets only the values it names; the others come from the less specific rules, or the defaults (<see cref="StyleLook"/>).</item>
    /// <item><see cref="StyleRule.Not"/> excludes states: a rule with Not = On never matches an "on" part.</item>
    /// <item><see cref="Table"/> compiles a part's 64 looks at its first use and keeps them: a part then paints from an array index,
    /// with no matching and no allocation. The sheet is fixed from its first table: a rule added later throws.</item>
    /// <item><see cref="Sprites"/> numbers the sprite ids its rules name, so a painter can hold its sprites by index.</item>
    /// </list>
    /// </summary>
    [NotAWidget("data: a skin's rules and their compiled tables, which KodeKit's styled parts paint from")]
    public sealed class StyleSheet
    {
        public StyleSheet(string name) => Name = name;

        public string Name { get; }

        readonly List<StyleRule> rules = new List<StyleRule>();
        readonly Dictionary<string, StyleTable> tables = new Dictionary<string, StyleTable>();
        readonly List<string> sprites = new List<string>();

        public IReadOnlyList<StyleRule> Rules => rules;

        /// <summary>The sprite ids the rules name, each once, in the order of <see cref="StyleLook.SpriteIndex"/>.</summary>
        public IReadOnlyList<string> Sprites => sprites;

        /// <summary>Whether a table was compiled (the sheet takes no more rules).</summary>
        public bool Frozen => tables.Count > 0;

        /// <summary>A new rule for <paramref name="part"/>, matching when the flags of <paramref name="need"/> are set and none of <paramref name="not"/>.</summary>
        public StyleRule Rule(string part, StyleFlags need = StyleFlags.None, StyleFlags not = StyleFlags.None)
        {
            if (string.IsNullOrEmpty(part)) throw new ArgumentException("a rule needs a part", nameof(part));
            if ((need & not) != 0) throw new ArgumentException($"rule for {part}: {need & not} is both needed and excluded");
            Changing();
            var r = new StyleRule(this, part, need, not, rules.Count);
            rules.Add(r);
            return r;
        }

        internal void Changing()
        {
            if (Frozen) throw new InvalidOperationException($"style sheet {Name}: a rule changed after the first table was compiled");
        }

        /// <summary>Whether any rule applies to <paramref name="part"/>.</summary>
        public bool Styles(string part)
        {
            for (int i = 0; i < rules.Count; i++)
                if (rules[i].AppliesTo(part))
                    return true;
            return false;
        }

        /// <summary>The look of <paramref name="part"/> in the state <paramref name="f"/> (through its table).</summary>
        public StyleLook Resolve(string part, StyleFlags f) => Table(part)[f];

        /// <summary>The part's table, compiled at its first use.</summary>
        public StyleTable Table(string part)
        {
            if (tables.TryGetValue(part, out StyleTable t)) return t;
            if (tables.Count == 0) IndexSprites();
            var applicable = new List<StyleRule>();
            for (int i = 0; i < rules.Count; i++)
                if (rules[i].AppliesTo(part))
                    applicable.Add(rules[i]);
            // the more specific last: deeper part, then more needed flags, then declaration order
            applicable.Sort((a, b) =>
            {
                int c = StyleRule.Depth(a.Part).CompareTo(StyleRule.Depth(b.Part));
                if (c == 0) c = StyleStates.Bits(a.Need).CompareTo(StyleStates.Bits(b.Need));
                return c != 0 ? c : a.Order.CompareTo(b.Order);
            });
            var looks = new StyleLook[StyleStates.Count];
            for (int s = 0; s < StyleStates.Count; s++)
            {
                var look = new StyleLook { Fill = StyleColor.White, Tint = StyleColor.White, Text = StyleColor.White, SpriteIndex = -1 };
                var f = (StyleFlags)s;
                for (int i = 0; i < applicable.Count; i++)
                {
                    StyleRule r = applicable[i];
                    if (!r.Matches(f)) continue;
                    if (r.setsSprite)
                    {
                        look.SetsSprite = true;
                        look.Sprite = r.sprite;
                        look.SpriteIndex = r.sprite == null ? -1 : sprites.IndexOf(r.sprite);
                    }
                    if (r.setsFill) look.Fill = r.fill;
                    if (r.setsTint) look.Tint = r.tint;
                    if (r.setsText)
                    {
                        look.SetsText = true;
                        look.Text = r.text;
                    }
                    if (r.setsBold)
                    {
                        look.SetsBold = true;
                        look.Bold = r.bold;
                    }
                    if (r.setsHidden)
                    {
                        look.SetsHidden = true;
                        look.Hidden = r.hidden;
                    }
                }
                looks[s] = look;
            }
            t = new StyleTable(part, looks, applicable.Count);
            tables[part] = t;
            return t;
        }

        void IndexSprites()
        {
            for (int i = 0; i < rules.Count; i++)
                if (rules[i].setsSprite && rules[i].sprite != null && !sprites.Contains(rules[i].sprite))
                    sprites.Add(rules[i].sprite);
        }
    }
}
