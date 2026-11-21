#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Text;

namespace KodeKit.Headless
{
    /// <summary>Where a gauge's value lies against its low and high bounds (HTML's meter regions).</summary>
    public enum GaugeZone { Low, Mid, High }

    /// <summary>How good a gauge's zone is, from where its optimum lies (HTML's meter); Plain: the gauge has no optimum, or no value.</summary>
    public enum GaugeRating { Plain, Good, Fair, Poor }

    /// <summary>
    /// The words of a gauge's sentence (<see cref="GaugeModel.Describe(GaugeWords)"/>), each a template with KSP's <c>&lt;&lt;1&gt;&gt;</c>
    /// placeholders. The defaults are English; KodeKit fills them from the localization keys.
    /// </summary>
    [NotAWidget("data: the templates of a gauge's sentence")]
    public sealed class GaugeWords
    {
        /// <summary>The label and the value: "Throttle: 60 %".</summary>
        public string Reading = "<<1>>: <<2>>";

        /// <summary>The scale: "on 0 % to 100 %".</summary>
        public string Scale = "on <<1>> to <<2>>";

        /// <summary>The low bound: "low under 40 %".</summary>
        public string LowBound = "low under <<1>>";

        /// <summary>The high bound: "high over 80 %".</summary>
        public string HighBound = "high over <<1>>";

        /// <summary>The zone and its rating: "high, fair".</summary>
        public string ZoneRated = "<<1>>, <<2>>";

        /// <summary>A value outside the scale, drawn at its end.</summary>
        public string OffScale = "off the scale";

        /// <summary>No value, or no scale: "Acceleration: not available".</summary>
        public string NotAvailable = "<<1>>: not available";

        /// <summary>The value's text on the bar when there is none.</summary>
        public string Missing = "n/a";

        public string Low = "low", Mid = "in range", High = "high";
        public string Good = "good", Fair = "fair", Poor = "poor";

        /// <summary>The word of a zone.</summary>
        public string Of(GaugeZone z) => z == GaugeZone.Low ? Low : z == GaugeZone.High ? High : Mid;

        /// <summary>The word of a rating (null for Plain).</summary>
        public string Of(GaugeRating r) => r == GaugeRating.Good ? Good : r == GaugeRating.Fair ? Fair : r == GaugeRating.Poor ? Poor : null;

        /// <summary>KSP's placeholders <c>&lt;&lt;1&gt;&gt;</c>.. replaced by <paramref name="args"/>; one left without an argument stays.</summary>
        public static string Fill(string template, params string[] args)
        {
            if (string.IsNullOrEmpty(template)) return template ?? "";
            var sb = new StringBuilder(template);
            for (int i = 0; i < args.Length; i++) sb.Replace("<<" + (i + 1) + ">>", args[i] ?? "");
            return sb.ToString();
        }
    }

    /// <summary>
    /// A value on a scale with optional low, high and optimum points, without Unity: ui-sdk's <c>gauge</c>, the rules of HTML's
    /// <c>meter</c>. KodeKit's <c>Meter</c> draws it as a filled bar; the agent bridge reads its sentence. It only reads: nothing a
    /// gauge shows feeds back into what it measures.
    /// <list type="bullet">
    /// <item>The bounds are read through delegates at each <see cref="Sync"/> (a setting can move them). <see cref="Min"/> is 0 and
    /// <see cref="Max"/> 1 without a delegate; a scale that is not finite, or whose max is not above its min, has no scale: the gauge
    /// is not available.</item>
    /// <item>A value that is not finite draws empty and says "not available" (<see cref="Available"/> false, <see cref="Fraction"/> 0,
    /// zone Mid, rating Plain).</item>
    /// <item>A value outside the scale is clamped for drawing (<see cref="Fraction"/>, <see cref="Zone"/>) and kept as it is in the
    /// text (<see cref="Value"/>, <see cref="FormatValue"/>); <see cref="OffScale"/> says so.</item>
    /// <item>A missing low bound (no delegate, or not a number) is the min and a missing high bound the max, as in HTML; each is
    /// clamped into the scale. A low bound above the high bound degrades to a plain bar: no zones (<see cref="Zoned"/> false), zone
    /// Mid, rating Plain.</item>
    /// <item>The zone is Low below the low bound, High above the high bound, Mid between them (both bounds included). A value
    /// within <see cref="Tolerance"/> of a bound counts as at it: a float value against a double setting, or a limiter that holds
    /// the value at its limit with a little noise, stays in range instead of flickering out of it.</item>
    /// <item>The rating follows HTML's optimum: an optimum between the bounds makes Mid good and Low and High fair; an optimum below
    /// the low bound makes Low good, Mid fair and High poor; above the high bound, the reverse. Without an optimum the rating is
    /// Plain.</item>
    /// <item><see cref="Sync"/> says whether anything drawn changed: the fraction by more than <see cref="Resolution"/>, the zone, the
    /// rating, the availability, the bounds' places, or the value's text (the value itself changing). It allocates nothing.</item>
    /// <item><see cref="Describe(GaugeWords)"/> is a sentence for the bridge and for players, from localized templates, with the
    /// numbers in <see cref="Format"/> (MechJeb's own formatters on a <see cref="CharBuffer"/>, not the current culture). It
    /// allocates: not for a frame.</item>
    /// </list>
    /// </summary>
    public sealed class GaugeModel : IDescribeParts
    {
        readonly Func<double> value, min, max, low, high, optimum;

        /// <summary>
        /// A gauge on <paramref name="value"/>. Each bound is optional (null: its default, see the rules); <paramref name="optimum"/>
        /// null or not a number gives no rating. <paramref name="format"/> writes its numbers (null: three significant figures).
        /// </summary>
        public GaugeModel(Func<double> value, Func<double> min = null, Func<double> max = null, Func<double> low = null,
            Func<double> high = null, Func<double> optimum = null, Action<CharBuffer, double> format = null)
        {
            this.value = value ?? throw new ArgumentNullException(nameof(value));
            Format = format ?? ((b, v) => b.AppendSI(v, 3));
            this.min = min;
            this.max = max;
            this.low = low;
            this.high = high;
            this.optimum = optimum;
            Read();
        }

        /// <summary>What the gauge is called, for <see cref="IDescribeParts"/> and its sentence (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>
        /// Writes a number of the gauge (its value, a bound) as shown: MechJeb's formatters on a buffer, no culture, no garbage.
        /// </summary>
        public Action<CharBuffer, double> Format { get; }

        /// <summary>The smallest change of <see cref="Fraction"/> that <see cref="Sync"/> reports (a bar a thousand pixels wide).</summary>
        public double Resolution { get; set; } = 0.001;

        /// <summary>How far past a bound a value still counts as at it, as a fraction of the scale (default a millionth).</summary>
        public double Tolerance { get; set; } = 1e-6;

        /// <summary>The value as read, unclamped (NaN or infinite: not available).</summary>
        public double Value { get; private set; }

        public double Min { get; private set; }
        public double Max { get; private set; }

        /// <summary>The low bound in use: the one given, clamped into the scale, or the min.</summary>
        public double Low { get; private set; }

        /// <summary>The high bound in use: the one given, clamped into the scale, or the max.</summary>
        public double High { get; private set; }

        /// <summary>The optimum in use, clamped into the scale (NaN: none).</summary>
        public double Optimum { get; private set; }

        /// <summary>Whether a low bound was given (a number): drawn as a mark on the bar.</summary>
        public bool HasLow { get; private set; }

        /// <summary>Whether a high bound was given (a number): drawn as a mark on the bar.</summary>
        public bool HasHigh { get; private set; }

        /// <summary>Whether the scale is usable: min and max finite and max above min.</summary>
        public bool HasScale { get; private set; }

        /// <summary>Whether there is something to draw: a scale and a finite value.</summary>
        public bool Available { get; private set; }

        /// <summary>Whether the zones apply: available, and the low bound not above the high bound.</summary>
        public bool Zoned { get; private set; }

        /// <summary>Whether the value lies outside the scale (drawn at its end).</summary>
        public bool OffScale { get; private set; }

        /// <summary>How full the bar is, 0..1 (0 when not available).</summary>
        public double Fraction { get; private set; }

        public GaugeZone Zone { get; private set; }
        public GaugeRating Rating { get; private set; }

        /// <summary>Where a bound sits on the bar, 0..1.</summary>
        public double FractionOf(double v) => HasScale ? Clamp01((v - Min) / (Max - Min)) : 0;

        /// <summary>Reads the value and the bounds. True when anything drawn changed since the last read.</summary>
        public bool Sync()
        {
            double f = Fraction, v = Value, lo = Low, hi = High, mn = Min, mx = Max;
            GaugeZone z = Zone;
            GaugeRating r = Rating;
            bool a = Available, hl = HasLow, hh = HasHigh;
            Read();
            return Math.Abs(f - Fraction) > Resolution || z != Zone || r != Rating || a != Available || hl != HasLow || hh != HasHigh ||
                !Same(v, Value) || !Same(lo, Low) || !Same(hi, High) || !Same(mn, Min) || !Same(mx, Max);
        }

        static bool Same(double a, double b) => a == b || double.IsNaN(a) && double.IsNaN(b);

        static double Clamp01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;

        static double Clamp(double x, double a, double b) => x < a ? a : x > b ? b : x;

        void Read()
        {
            double mn = min != null ? min() : 0, mx = max != null ? max() : 1;
            HasScale = IsFinite(mn) && IsFinite(mx) && mx > mn;
            Min = mn;
            Max = mx;
            double v = value();
            Value = v;
            double lo = low != null ? low() : double.NaN, hi = high != null ? high() : double.NaN;
            HasLow = !double.IsNaN(lo);
            HasHigh = !double.IsNaN(hi);
            if (!HasScale)
            {
                Low = lo;
                High = hi;
                Optimum = double.NaN;
                Unavailable();
                return;
            }
            Low = HasLow ? Clamp(lo, mn, mx) : mn;
            High = HasHigh ? Clamp(hi, mn, mx) : mx;
            double opt = optimum != null ? optimum() : double.NaN;
            Optimum = double.IsNaN(opt) ? double.NaN : Clamp(opt, mn, mx);
            if (!IsFinite(v))
            {
                Unavailable();
                return;
            }
            Available = true;
            double at = Clamp(v, mn, mx);
            OffScale = at != v;
            Fraction = (at - mn) / (mx - mn);
            Zoned = Low <= High;
            if (!Zoned)
            {
                Zone = GaugeZone.Mid;
                Rating = GaugeRating.Plain;
                return;
            }
            double tol = Tolerance * (mx - mn);
            Zone = at < Low - tol ? GaugeZone.Low : at > High + tol ? GaugeZone.High : GaugeZone.Mid;
            Rating = RatingOf(Zone);
        }

        void Unavailable()
        {
            Available = false;
            Zoned = false;
            OffScale = false;
            Fraction = 0;
            Zone = GaugeZone.Mid;
            Rating = GaugeRating.Plain;
        }

        GaugeRating RatingOf(GaugeZone z)
        {
            if (double.IsNaN(Optimum)) return GaugeRating.Plain;
            // HTML's meter: the optimum's region is good, the next fair, the far one poor
            GaugeZone best = Optimum < Low ? GaugeZone.Low : Optimum > High ? GaugeZone.High : GaugeZone.Mid;
            if (z == best) return GaugeRating.Good;
            if (best == GaugeZone.Mid || z == GaugeZone.Mid) return GaugeRating.Fair;
            return GaugeRating.Poor;
        }

        static bool IsFinite(double d) => !double.IsNaN(d) && !double.IsInfinity(d);

        /// <summary>Writes the value as shown (unclamped) into <paramref name="into"/>, after clearing it; nothing when not available.</summary>
        public void FormatValue(CharBuffer into)
        {
            into.Clear();
            if (Available) Format(into, Value);
        }

        string Formatted(double v)
        {
            var b = new CharBuffer();
            Format(b, v);
            return b.ToString();
        }

        /// <summary>
        /// The gauge as a sentence, as it is now: "Throttle: 90 % on 0 % to 100 %, low under 40 %, high over 80 %: high, fair".
        /// Allocates: for the bridge, a tooltip or a test. <paramref name="label"/> replaces <see cref="Label"/> (a host that keeps a
        /// localization key there passes the text shown).
        /// </summary>
        public string Describe(GaugeWords words = null, string label = null)
        {
            if (words == null) words = DefaultWords;
            if (label == null) label = Label ?? "";
            if (!Available) return GaugeWords.Fill(words.NotAvailable, label);
            var sb = new StringBuilder(GaugeWords.Fill(words.Reading, label, Formatted(Value)));
            sb.Append(' ').Append(GaugeWords.Fill(words.Scale, Formatted(Min), Formatted(Max)));
            if (OffScale) sb.Append(" (").Append(words.OffScale).Append(')');
            if (!Zoned) return sb.ToString();
            if (HasLow) sb.Append(", ").Append(GaugeWords.Fill(words.LowBound, Formatted(Low)));
            if (HasHigh) sb.Append(", ").Append(GaugeWords.Fill(words.HighBound, Formatted(High)));
            if (Rating == GaugeRating.Plain && !HasLow && !HasHigh) return sb.ToString();
            string zone = words.Of(Zone);
            sb.Append(": ").Append(Rating == GaugeRating.Plain ? zone : GaugeWords.Fill(words.ZoneRated, zone, words.Of(Rating)));
            return sb.ToString();
        }

        /// <summary>The English words, the sentence's default.</summary>
        public static readonly GaugeWords DefaultWords = new GaugeWords();

        static readonly PartKey[] NoKeys = new PartKey[0];

        /// <summary>One part: the meter, labelled, Invalid while it is not available. It takes no focus and handles no key.</summary>
        public void Describe(List<PartDescription> into) =>
            into.Add(new PartDescription
            {
                Name = "meter", Role = PartRole.Meter, Label = Label, Flags = Available ? PartFlags.None : PartFlags.Invalid, Focusable = false,
                Keys = NoKeys
            });
    }
}
