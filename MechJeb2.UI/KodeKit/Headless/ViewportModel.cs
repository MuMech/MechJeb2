#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>The keys a viewport understands, as its host reports them.</summary>
    public enum ViewportKey { PanLeft, PanRight, ZoomIn, ZoomOut, Home, End, Reset }

    /// <summary>
    /// The visible window of one axis over a range of data, without Unity (ui-sdk's viewport): what a graph's X axis shows, and
    /// how the pointer and keys pan and zoom it. One model is one axis; a graph that zooms in X and Y holds two. Positions on
    /// the plot are fractions, 0 at the window's start and 1 at its end (<see cref="ToFraction"/>, <see cref="FromFraction"/>),
    /// so the host converts pixels and nothing here depends on the plot's size.
    /// <list type="bullet">
    /// <item>The host gives the data's range (<see cref="SetData"/>) and the span a reset shows (<see cref="DefaultSpan"/>,
    /// NaN for the whole data). Until the user zooms, the span is the default one, and follows it when it changes.</item>
    /// <item>A zoom keeps the data value under its anchor where it is (zoom about the pointer). The span stays between
    /// <see cref="MinSpan"/> and the larger of the data's extent and the default span; a zoom back out to the default span
    /// is a reset.</item>
    /// <item>A window narrower than the data stays inside it, give or take <see cref="Overscroll"/> of its span at each end.
    /// A window as wide as the data or wider starts at the data's start.</item>
    /// <item>While <see cref="Follow"/> is on, the window's end stays on the data's end as the data grows (a live recording).
    /// With <see cref="AutoFollow"/>, each pan or zoom by the user turns it on when the window reaches the data's end and off
    /// otherwise, as a scrollbar pulled to its end does.</item>
    /// <item>A reset goes back to the default span, at the data's end when <see cref="ResetFollows"/> (and following), at its
    /// start otherwise.</item>
    /// <item><see cref="Version"/> counts the changes of the window or the data (the data growing included); <see cref="MoveVersion"/>
    /// only the moves asked for (pan, zoom, scroll, Show, Fit, Reset), so a host can redraw everything at once for those and spread
    /// the redraw of a growing recording. A change of <see cref="Follow"/> alone bumps neither.</item>
    /// <item>A non-finite argument (NaN, infinity) changes nothing and returns false.</item>
    /// </list>
    /// </summary>
    public sealed class ViewportModel : IDescribeParts
    {
        /// <summary>What the graph is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The plot (a figure: the pointer pans and zooms it) and its scroll bar (a slider over the data).</summary>
        public void Describe(List<PartDescription> into)
        {
            into.Add(new PartDescription { Name = "plot", Role = PartRole.Figure, Label = Label });
            into.Add(new PartDescription { Name = "scroll", Role = PartRole.Slider, Label = string.IsNullOrEmpty(Label) ? null : "scroll " + Label });
        }

        public ViewportModel(double dataMin = 0, double dataMax = 0, double defaultSpan = double.NaN)
        {
            DefaultSpan = defaultSpan;
            DataMin = dataMin;
            DataMax = Math.Max(dataMin, dataMax);
            Span = DefaultSpanOrExtent();
            Start = DataMin;
            Clamp();
        }

        public double DataMin { get; private set; }
        public double DataMax { get; private set; }
        public double Extent => DataMax - DataMin;

        /// <summary>The window's start, in data units.</summary>
        public double Start { get; private set; }

        /// <summary>The window's width, in data units.</summary>
        public double Span { get; private set; }

        public double End => Start + Span;

        /// <summary>The span a reset shows; NaN or 0 for the whole data.</summary>
        public double DefaultSpan { get; private set; }

        /// <summary>The narrowest span a zoom reaches (0: a billionth of the data's extent).</summary>
        public double MinSpan;

        /// <summary>How far past each end of the data a pan reaches, as a fraction of the span (0: not past it).</summary>
        public double Overscroll;

        /// <summary>The zoom of one wheel notch, one key press (<see cref="KeyZoom"/>) and the pan of one key press.</summary>
        public double WheelZoom = 1.25, KeyZoom = 2, KeyPan = 0.1;

        /// <summary>Keeps the window's end on the data's end as it grows. Setting it moves nothing until the data grows (see
        /// <see cref="Key"/>'s End to go there) and bumps no version.</summary>
        public bool Follow;

        /// <summary>The user's pans and zooms turn <see cref="Follow"/> on at the data's end and off elsewhere.</summary>
        public bool AutoFollow = true;

        /// <summary>A reset shows the data's end and follows it; off, the data's start.</summary>
        public bool ResetFollows = true;

        /// <summary>The user (or the program) set the span: it no longer follows <see cref="DefaultSpan"/>.</summary>
        public bool Zoomed { get; private set; }

        /// <summary>Bumped by every change of the window or the data.</summary>
        public int Version { get; private set; }

        /// <summary>Bumped by the moves asked for (pan, zoom, scroll, Show, Fit, Reset), not by the data growing or a new
        /// default span.</summary>
        public int MoveVersion { get; private set; }

        /// <summary>The source of the last pan, zoom or reset (for an observer, ui-sdk's change source).</summary>
        public ChangeSource LastSource { get; private set; } = ChangeSource.Program;

        /// <summary>How many times the data's extent the window shows less than all of it: 1 when it shows it all.</summary>
        public double Magnification => Extent > 0 && Span < Extent ? Extent / Span : 1;

        // ---- the mapping ----

        /// <summary>Where a data value falls on the plot: 0 at the window's start, 1 at its end.</summary>
        public double ToFraction(double x) => Span > 0 ? (x - Start) / Span : 0;

        /// <summary>The data value at a fraction of the plot.</summary>
        public double FromFraction(double f) => Start + f * Span;

        /// <summary>A scrollbar's value for this window: 0 at the data's start, 1 at its end (1 when it shows all of it).</summary>
        public double ScrollValue
        {
            get
            {
                double room = Extent - Span;
                if (!(room > 0)) return 1;
                return Math.Max(0, Math.Min(1, (Start - DataMin) / room));
            }
        }

        /// <summary>A scrollbar's thumb size: the share of the data the window shows.</summary>
        public double ScrollSize => Extent > 0 ? Math.Min(1, Span / Extent) : 1;

        // ---- the host ----

        /// <summary>The data's range, as it grows or is replaced. True when the window or the data changed.</summary>
        public bool SetData(double min, double max)
        {
            if (!Finite(min) || !Finite(max)) return false;
            max = Math.Max(min, max);
            if (min.Equals(DataMin) && max.Equals(DataMax)) return false;
            DataMin = min;
            DataMax = max;
            if (!Zoomed) Span = DefaultSpanOrExtent();
            Clamp();
            Version++;
            return true;
        }

        /// <summary>The span a reset shows (NaN: the whole data). Unless the user zoomed, the window takes it now.</summary>
        public bool SetDefaultSpan(double span)
        {
            if (span.Equals(DefaultSpan)) return false;
            DefaultSpan = span;
            if (Zoomed) return false;
            return Apply(Start, DefaultSpanOrExtent());
        }

        // ---- the user ----

        /// <summary>Zooms in by <paramref name="factor"/> (below 1 zooms out) about the data value at <paramref name="fraction"/>
        /// of the plot, which stays there. True when the window changed.</summary>
        public bool ZoomAt(double factor, double fraction, ChangeSource source)
        {
            if (!(factor > 0) || double.IsInfinity(factor) || !Finite(fraction)) return false;
            fraction = Math.Max(0, Math.Min(1, fraction));
            double anchor = FromFraction(fraction);
            double span = Math.Max(Math.Min(Span / factor, MaxSpan()), MinSpanOrDefault());
            return User(anchor - fraction * span, span, true, source);
        }

        /// <summary>Zooms by <paramref name="factor"/> about a data value, which keeps its place on the plot (the middle
        /// when it is outside the window).</summary>
        public bool ZoomAtValue(double factor, double x, ChangeSource source)
        {
            if (!Finite(x)) return false;
            double f = ToFraction(x);
            return ZoomAt(factor, f >= 0 && f <= 1 ? f : 0.5, source);
        }

        /// <summary>Wheel notches over the plot (positive: away from the user, zooms in) at a fraction of it.</summary>
        public bool Wheel(double notches, double fraction, ChangeSource source) =>
            notches != 0 && Finite(notches) && ZoomAt(Math.Pow(WheelZoom, Math.Max(-10, Math.Min(10, notches))), fraction, source);

        /// <summary>Moves the window by a fraction of its span (positive: towards the data's end).</summary>
        public bool PanBy(double fraction, ChangeSource source) => Finite(fraction) && PanByData(fraction * Span, source);

        /// <summary>Moves the window by data units (positive: towards the data's end).</summary>
        public bool PanByData(double units, ChangeSource source) => units != 0 && Finite(units) && User(Start + units, Span, false, source);

        /// <summary>Puts the window's start at a data value.</summary>
        public bool PanTo(double start, ChangeSource source) => Finite(start) && User(start, Span, false, source);

        /// <summary>A scrollbar pulled to <paramref name="value"/> (see <see cref="ScrollValue"/>).</summary>
        public bool ScrollTo(double value, ChangeSource source)
        {
            if (!Finite(value)) return false;
            double room = Extent - Span;
            if (!(room > 0)) return User(Start, Span, false, source);
            return PanTo(DataMin + Math.Max(0, Math.Min(1, value)) * room, source);
        }

        /// <summary>Shows the data from <paramref name="from"/> to <paramref name="to"/> (zoom to a burn), within the limits.</summary>
        public bool Show(double from, double to, ChangeSource source)
        {
            if (!Finite(from) || !Finite(to)) return false;
            if (to < from)
            {
                double t = to;
                to = from;
                from = t;
            }

            double span = Math.Max(Math.Min(to - from, MaxSpan()), MinSpanOrDefault());
            return User((from + to) / 2 - span / 2, span, true, source);
        }

        /// <summary>The whole data in the window.</summary>
        public bool Fit(ChangeSource source) => Show(DataMin, DataMax, source);

        /// <summary>Back to the default span, at the data's end (following) or start: see <see cref="ResetFollows"/>.</summary>
        public bool Reset(ChangeSource source)
        {
            LastSource = source;
            Zoomed = false;
            Follow = ResetFollows;
            double span = DefaultSpanOrExtent();
            if (!Apply(ResetFollows ? DataMax - span : DataMin, span)) return false;
            MoveVersion++;
            return true;
        }

        /// <summary>A key on the focused graph. True when the viewport handled it.</summary>
        public bool Key(ViewportKey key, ChangeSource source = ChangeSource.Keyboard)
        {
            switch (key)
            {
                case ViewportKey.PanLeft: PanBy(-KeyPan, source); return true;
                case ViewportKey.PanRight: PanBy(KeyPan, source); return true;
                case ViewportKey.ZoomIn: ZoomAt(KeyZoom, 0.5, source); return true;
                case ViewportKey.ZoomOut: ZoomAt(1 / KeyZoom, 0.5, source); return true;
                case ViewportKey.Home: PanTo(DataMin - Overscroll * Span, source); return true;
                case ViewportKey.End: PanTo(DataMax - Span + Overscroll * Span, source); return true;
                case ViewportKey.Reset: Reset(source); return true;
                default: return false;
            }
        }

        // ---- the rules ----

        static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        double DefaultSpanOrExtent()
        {
            if (DefaultSpan > 0 && !double.IsInfinity(DefaultSpan)) return DefaultSpan;
            return Extent > 0 ? Extent : Math.Max(MinSpan, 1);
        }

        double MinSpanOrDefault() => MinSpan > 0 ? MinSpan : Math.Max(Extent, 1) * 1e-9;

        double MaxSpan() => Math.Max(Math.Max(Extent, DefaultSpanOrExtent()), MinSpanOrDefault());

        // a pan or zoom by the user: the limits, then Follow from where the window ends up
        bool User(double start, double span, bool zoom, ChangeSource source)
        {
            LastSource = source;
            if (zoom)
            {
                // zoomed back out to the default span: the span follows the default again
                double reset = DefaultSpanOrExtent();
                Zoomed = Math.Abs(span - reset) > 1e-9 * Math.Max(reset, 1e-300);
                if (!Zoomed) span = reset;
            }

            Follow = false;
            bool changed = Apply(start, span);
            if (changed) MoveVersion++;
            if (AutoFollow) Follow = Span >= Extent || End >= DataMax - 1e-9 * Math.Max(Span, 1e-300);
            return changed;
        }

        bool Apply(double start, double span)
        {
            double oldStart = Start, oldSpan = Span;
            Span = span;
            Start = start;
            Clamp();
            if (Start.Equals(oldStart) && Span.Equals(oldSpan)) return false;
            Version++;
            return true;
        }

        void Clamp()
        {
            if (!(Span > 0) || double.IsInfinity(Span)) Span = DefaultSpanOrExtent();
            if (Zoomed) Span = Math.Max(Math.Min(Span, MaxSpan()), MinSpanOrDefault());
            double extent = Extent;
            if (Span >= extent)
            {
                Start = DataMin;
                return;
            }

            if (Follow) Start = DataMax - Span;
            double over = Math.Max(0, Overscroll) * Span;
            Start = Math.Max(DataMin - over, Math.Min(Start, DataMax - Span + over));
        }
    }
}
