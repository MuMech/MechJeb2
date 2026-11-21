#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>
    /// A graph's hover read-out, without Unity: the place on the X axis under the pointer and each series' value there, for a
    /// tooltip that follows the pointer and a cursor line. It works over the graph's <see cref="ViewportModel"/>: the pointer is
    /// given as a fraction of the plot's width, so nothing here depends on the plot's size.
    /// <list type="bullet">
    /// <item>The pointer at a fraction of the plot (<see cref="Move"/>) reads the X there through the viewport
    /// (<see cref="ViewportModel.FromFraction"/>), so a zoom or a pan is taken into account; a fraction outside 0..1 (the pointer
    /// off the plot) reads nothing.</item>
    /// <item>Each series' value at that X is interpolated linearly between the two samples around it, as the graph draws its line
    /// (a straight segment from sample to sample), so the value read is the height of the line under the cursor; on a sample it
    /// is that sample's value (<see cref="SampleStats.ValueAtSorted"/>, a bisection: a few reads per move however long the
    /// series). Outside a series' samples, or where a sample next to X is NaN (a gap), it has no value.</item>
    /// <item>A row whose series is hidden (its <c>shown</c> delegate false) is left out of the read-out.</item>
    /// <item>The read-out is <see cref="Active"/> while the pointer is over the plot, no button is held there
    /// (<see cref="Press"/>: a press starts a pan, a range drag or a click, which have the pointer) and the X is finite; leaving
    /// (<see cref="Leave"/>) ends it.</item>
    /// <item>The same pointer reads again when the view moves under it (<see cref="Sync"/>: a wheel zoom, a pan by the scrollbar,
    /// the view following new data) or when the host asks (<see cref="Refresh"/>: the data changed).</item>
    /// <item><see cref="Version"/> moves only when what a read-out shows changed (active or not, the X, a value, which rows show);
    /// <see cref="ShapeVersion"/> when the rows shown changed (a host builds its tooltip again then, and only writes its values
    /// otherwise). A read that changes nothing moves neither, so a still pointer redraws nothing.</item>
    /// <item><see cref="Text"/> holds the whole read-out as text (the X, then a line per shown row), rewritten at each change
    /// into the same buffer. Nothing here allocates once the rows are added and the buffers have grown.</item>
    /// </list>
    /// </summary>
    [NotAWidget("a read-out of a graph's values under the pointer, drawn as a tooltip: it takes no input of its own; the graph's ViewportModel and RangeModel describe the axis and its picks")]
    public sealed class GraphReadoutModel
    {
        sealed class Row
        {
            public ISamples Samples;
            public string Name;
            public Func<bool> Shown;
            public Action<CharBuffer, double> Format;
            public bool IsShown, Has;
            public double Value = double.NaN;
        }

        readonly List<Row> rows = new List<Row>();
        Func<string> xName;
        Action<CharBuffer, double> xFormat;
        int readViewVersion = -1;

        public GraphReadoutModel(ViewportModel view) => View = view ?? throw new ArgumentNullException(nameof(view));

        /// <summary>The axis the pointer is read on.</summary>
        public ViewportModel View { get; }

        /// <summary>
        /// How the X is named and written (null name: none; null format: <see cref="CharBuffer.AppendSI"/> at 4 figures). The name
        /// is read at each change, so it can follow the host's units (time or distance).
        /// </summary>
        public void SetX(Func<string> name, Action<CharBuffer, double> format)
        {
            xName = name;
            xFormat = format;
            Reread();
        }

        /// <summary>
        /// A series to read: its samples, its name as shown, how its value is written (with its unit; null: SI at 4 figures),
        /// and whether it is shown now (null: always). Returns the row's index. Allocates: call it when the graph is built.
        /// </summary>
        public int Add(ISamples samples, string name, Action<CharBuffer, double> format = null, Func<bool> shown = null)
        {
            rows.Add(new Row { Samples = samples, Name = name ?? "", Format = format, Shown = shown });
            ShapeVersion++;
            Reread();
            return rows.Count - 1;
        }

        /// <summary>The rows added.</summary>
        public int Count => rows.Count;

        /// <summary>A row's name as given.</summary>
        public string Name(int row) => rows[row].Name;

        /// <summary>A row's samples.</summary>
        public ISamples Samples(int row) => rows[row].Samples;

        /// <summary>Whether a row is in the read-out (its series shown), as of the last read.</summary>
        public bool Shown(int row) => rows[row].IsShown;

        /// <summary>Whether a shown row has a value at <see cref="X"/>.</summary>
        public bool HasValue(int row) => rows[row].IsShown && rows[row].Has;

        /// <summary>A shown row's value at <see cref="X"/> (NaN: none).</summary>
        public double Value(int row) => HasValue(row) ? rows[row].Value : double.NaN;

        /// <summary>How many rows are shown, as of the last read.</summary>
        public int ShownCount { get; private set; }

        /// <summary>The pointer is over the plot (whether or not a button is held).</summary>
        public bool Hovering { get; private set; }

        /// <summary>A button is held on the plot: the read-out waits for its release.</summary>
        public bool Pressed { get; private set; }

        /// <summary>The pointer's place across the plot, 0 at its left edge, 1 at its right (NaN: none yet).</summary>
        public double Fraction { get; private set; } = double.NaN;

        /// <summary>A read-out to show: the pointer over the plot, no button held, a finite X.</summary>
        public bool Active { get; private set; }

        /// <summary>The X under the pointer, in data units (NaN while not <see cref="Active"/>).</summary>
        public double X { get; private set; } = double.NaN;

        /// <summary>Moves when what a read-out shows changed.</summary>
        public int Version { get; private set; }

        /// <summary>Moves when the rows shown changed (or a row was added).</summary>
        public int ShapeVersion { get; private set; }

        /// <summary>What moved the pointer last.</summary>
        public ChangeSource LastSource { get; private set; } = ChangeSource.Program;

        /// <summary>The whole read-out as text: the X's name and value, then "name value" per shown row, one a line ('—' where a row
        /// has no value). Empty while not <see cref="Active"/>.</summary>
        public CharBuffer Text { get; } = new CharBuffer(128);

        /// <summary>The pointer at <paramref name="fraction"/> of the plot's width. True when the read-out changed.</summary>
        public bool Move(double fraction, ChangeSource source = ChangeSource.Pointer)
        {
            LastSource = source;
            Hovering = true;
            Fraction = fraction;
            return Reread();
        }

        /// <summary>The pointer left the plot. True when a read-out was shown.</summary>
        public bool Leave()
        {
            Hovering = false;
            Pressed = false;
            Fraction = double.NaN;
            return Reread();
        }

        /// <summary>A button went down (true) or up (false) on the plot. True when the read-out changed.</summary>
        public bool Press(bool down)
        {
            Pressed = down;
            return Reread();
        }

        /// <summary>Reads again when the view moved since the last read (the pointer stays where it is). True when it changed.</summary>
        public bool Sync() => View.Version != readViewVersion && Reread();

        /// <summary>Reads again at the same pointer (the data changed). True when the read-out changed.</summary>
        public bool Refresh() => Reread();

        /// <summary>Writes row <paramref name="row"/>'s value as the read-out shows it: with its unit, or '—' without one.</summary>
        public CharBuffer WriteValue(int row, CharBuffer into)
        {
            Row r = rows[row];
            if (!r.IsShown || !r.Has) return into.Append('—');
            if (r.Format != null) r.Format(into, r.Value);
            else into.AppendSI(r.Value, 4);
            return into;
        }

        /// <summary>Writes the X as the read-out shows it ('—' while not <see cref="Active"/>).</summary>
        public CharBuffer WriteX(CharBuffer into)
        {
            if (!Active) return into.Append('—');
            if (xFormat != null) xFormat(into, X);
            else into.AppendSI(X, 4);
            return into;
        }

        /// <summary>The X's name (empty without one).</summary>
        public string XName => xName?.Invoke() ?? "";

        string readXName;

        bool Reread()
        {
            readViewVersion = View.Version;
            double f = Fraction;
            double x = Hovering && !Pressed && f >= 0 && f <= 1 ? View.FromFraction(f) : double.NaN;
            bool active = !double.IsNaN(x) && !double.IsInfinity(x);
            if (!active) x = double.NaN;
            bool changed = active != Active || !x.Equals(X);
            Active = active;
            X = x;

            bool shapeChanged = false;
            int shown = 0;
            foreach (Row r in rows)
            {
                bool isShown = r.Shown == null || r.Shown();
                if (isShown != r.IsShown)
                {
                    r.IsShown = isShown;
                    shapeChanged = true;
                }

                bool has = false;
                double v = double.NaN;
                if (isShown)
                {
                    shown++;
                    if (active) has = SampleStats.ValueAtSorted(r.Samples, x, out v);
                }

                if (has != r.Has || !v.Equals(r.Value)) changed = true;
                r.Has = has;
                r.Value = v;
            }

            ShownCount = shown;
            string name = XName;
            if (!string.Equals(name, readXName, StringComparison.Ordinal))
            {
                readXName = name;
                shapeChanged = true;
            }

            if (shapeChanged)
            {
                ShapeVersion++;
                changed = true;
            }

            if (!changed) return false;
            Version++;
            WriteText();
            return true;
        }

        void WriteText()
        {
            Text.Clear();
            if (!Active) return;
            string name = XName;
            if (name.Length > 0) Text.Append(name).Append(' ');
            WriteX(Text);
            for (int i = 0; i < rows.Count; i++)
            {
                if (!rows[i].IsShown) continue;
                Text.Append('\n').Append(rows[i].Name).Append(' ');
                WriteValue(i, Text);
            }
        }
    }
}
