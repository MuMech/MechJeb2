#if !CSHARP_7_3
#nullable disable
#endif
using System;

namespace KodeKit.Headless
{
    /// <summary>Samples of a series, (X, Y) by index: what a graph draws (KodeUI's IGraphSource has the same members).</summary>
    public interface ISamples
    {
        int Count { get; }
        double X(int i);
        double Y(int i);
    }

    /// <summary>A series' values over a range of X (<see cref="SampleStats.Over"/>).</summary>
    public struct RangeStats
    {
        /// <summary>The values at the range's ends, interpolated between the samples around them.</summary>
        public double AtStart, AtEnd;

        /// <summary>The lowest and highest values in the range, the interpolated ends included, and where they are.</summary>
        public double Min, Max, MinAt, MaxAt;

        /// <summary>How many samples lie inside the range (ends included).</summary>
        public int Samples;

        /// <summary>The change over the range: <see cref="AtEnd"/> − <see cref="AtStart"/>.</summary>
        public double Delta => AtEnd - AtStart;
    }

    /// <summary>
    /// The values a range or a point reads off a series, without Unity or allocation.
    /// <list type="bullet">
    /// <item>Between two samples a value is interpolated linearly in X; outside the samples' X there is no value.</item>
    /// <item>The samples are scanned in order, so X need not grow everywhere (a downrange distance can step back): a value at X
    /// is read on the first segment that spans it.</item>
    /// <item>NaN samples are skipped by min and max.</item>
    /// </list>
    /// </summary>
    public static class SampleStats
    {
        /// <summary>The value at <paramref name="x"/>, interpolated. False (NaN) outside the samples or without any.</summary>
        public static bool ValueAt(ISamples s, double x, out double y)
        {
            y = double.NaN;
            int n = s?.Count ?? 0;
            if (n == 0 || double.IsNaN(x)) return false;
            double x0 = s.X(0);
            if (n == 1)
            {
                if (!x0.Equals(x)) return false;
                y = s.Y(0);
                return true;
            }

            for (int i = 1; i < n; i++)
            {
                double x1 = s.X(i);
                if (x >= Math.Min(x0, x1) && x <= Math.Max(x0, x1))
                {
                    y = Lerp(x0, s.Y(i - 1), x1, s.Y(i), x);
                    return true;
                }

                x0 = x1;
            }

            return false;
        }

        /// <summary>
        /// The value at <paramref name="x"/>, interpolated linearly between the two samples around it, found by bisection: a few
        /// reads of a long series where <see cref="ValueAt"/> scans it (a pointer moving over a graph asks at every move). On a
        /// sample, its own value (the first of samples that share that X). Where the X of the first and last samples bracket
        /// <paramref name="x"/>, the bisection always ends on a segment that spans it, even where X steps back; where they don't (a
        /// series that goes past <paramref name="x"/> and comes back), the scan of <see cref="ValueAt"/> answers. False (NaN) outside
        /// the samples, without any, and where a sample next to <paramref name="x"/> is NaN (a gap in the line). No allocation.
        /// </summary>
        public static bool ValueAtSorted(ISamples s, double x, out double y)
        {
            y = double.NaN;
            int n = s?.Count ?? 0;
            if (n == 0 || double.IsNaN(x) || double.IsInfinity(x)) return false;
            double first = s.X(0), last = s.X(n - 1);
            int lo = 0, hi = n - 1;
            if (first <= x && x <= last)
            {
                if (first.Equals(x)) return Sample(s, 0, out y);
                // X(lo) < x <= X(hi)
                while (hi - lo > 1)
                {
                    int mid = (lo + hi) >> 1;
                    if (s.X(mid) >= x) hi = mid;
                    else lo = mid;
                }
            }
            else if (last <= x && x <= first)
            {
                // X falling: X(lo) > x >= X(hi)
                if (first.Equals(x)) return Sample(s, 0, out y);
                while (hi - lo > 1)
                {
                    int mid = (lo + hi) >> 1;
                    if (s.X(mid) > x) lo = mid;
                    else hi = mid;
                }
            }
            else
            {
                if (ValueAt(s, x, out y) && !double.IsNaN(y)) return true;
                y = double.NaN;
                return false;
            }

            double xh = s.X(hi);
            if (xh.Equals(x)) return Sample(s, hi, out y);
            double y0 = s.Y(lo), y1 = s.Y(hi);
            if (double.IsNaN(y0) || double.IsNaN(y1)) return false;
            y = Lerp(s.X(lo), y0, xh, y1, x);
            return true;
        }

        static bool Sample(ISamples s, int i, out double y)
        {
            y = s.Y(i);
            if (!double.IsNaN(y)) return true;
            y = double.NaN;
            return false;
        }

        /// <summary>The values from <paramref name="from"/> to <paramref name="to"/> (in either order). False when the range
        /// has no value at either end.</summary>
        public static bool Over(ISamples s, double from, double to, out RangeStats stats)
        {
            stats = default;
            if (to < from)
            {
                double t = to;
                to = from;
                from = t;
            }

            if (!ValueAt(s, from, out double a) || !ValueAt(s, to, out double b)) return false;
            stats.AtStart = a;
            stats.AtEnd = b;
            stats.Min = stats.Max = a;
            stats.MinAt = stats.MaxAt = from;
            Take(ref stats, b, to);
            int n = s.Count;
            for (int i = 0; i < n; i++)
            {
                double x = s.X(i);
                if (x < from || x > to) continue;
                stats.Samples++;
                Take(ref stats, s.Y(i), x);
            }

            return true;
        }

        static void Take(ref RangeStats st, double y, double x)
        {
            if (double.IsNaN(y)) return;
            if (double.IsNaN(st.Min) || y < st.Min)
            {
                st.Min = y;
                st.MinAt = x;
            }

            if (double.IsNaN(st.Max) || y > st.Max)
            {
                st.Max = y;
                st.MaxAt = x;
            }
        }

        static double Lerp(double x0, double y0, double x1, double y1, double x) =>
            x1.Equals(x0) ? y1 : y0 + (y1 - y0) * (x - x0) / (x1 - x0);
    }
}
