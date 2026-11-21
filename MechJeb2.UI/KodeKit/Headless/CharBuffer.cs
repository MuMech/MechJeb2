#if !CSHARP_7_3
#nullable disable
#endif
using System;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>
    /// A reusable char buffer that numbers are formatted into without allocating strings, as MechJeb formats them: KodeKit's
    /// <c>CharText</c> pushes one to TMP, and <see cref="TableModel"/> keeps one per cell.
    /// <list type="bullet">
    /// <item><see cref="Append(double, int)"/> is <c>ToString("F" + decimals)</c> in the invariant culture, rounding as KSP's Mono
    /// does; <see cref="AppendSI"/> is MechJebLib's <c>Statics.ToSI</c>; <see cref="AppendDHMS"/> is <c>GuiUtils.TimeToDHMS</c>;
    /// <see cref="AppendDMS"/> is <c>Coordinates.AngleToDMS</c>.</item>
    /// <item>The buffer grows as needed and never shrinks; <see cref="Clear"/> keeps it.</item>
    /// </list>
    /// </summary>
    [NotAWidget("helper: a char buffer that formats numbers, for CharText and the table's cells")]
    public sealed class CharBuffer
    {
        char[] buf;
        int len;

        public CharBuffer(int capacity = 32) => buf = new char[Math.Max(1, capacity)];

        public int Length => len;

        /// <summary>The characters: the first <see cref="Length"/> of them are the text. Read-only for callers.</summary>
        public char[] Chars => buf;

        public char this[int i] => buf[i];

        public CharBuffer Clear()
        {
            len = 0;
            return this;
        }

        void Ensure(int extra)
        {
            if (len + extra <= buf.Length) return;
            Array.Resize(ref buf, Math.Max(buf.Length * 2, len + extra));
        }

        public CharBuffer Append(char c)
        {
            Ensure(1);
            buf[len++] = c;
            return this;
        }

        public CharBuffer Append(string s)
        {
            if (s == null) return this;
            Ensure(s.Length);
            for (int i = 0; i < s.Length; i++) buf[len++] = s[i];
            return this;
        }

        /// <summary>Appends the characters of another buffer.</summary>
        public CharBuffer Append(CharBuffer b)
        {
            if (b == null) return this;
            Ensure(b.len);
            Array.Copy(b.buf, 0, buf, len, b.len);
            len += b.len;
            return this;
        }

        static readonly double[] pow10 = { 1, 10, 100, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9 };

        /// <summary>Fixed-point, like ToString("F" + decimals), invariant culture.</summary>
        public CharBuffer Append(double v, int decimals)
        {
            if (double.IsNaN(v)) return Append("NaN");
            if (double.IsInfinity(v)) return Append(v > 0 ? "Inf" : "-Inf");
            decimals = Math.Max(0, Math.Min(9, decimals));
            // beyond long/decimal range: let .NET format it (allocates, but only for absurd values)
            if (Math.Abs(v) * pow10[decimals] >= 9.2e18) return Append(v.ToString("F" + decimals, System.Globalization.CultureInfo.InvariantCulture));
            if (v < 0)
            {
                // ToString prints -0.001 as "0.00", not "-0.00" (KSP's Mono, checked in game)
                if (Math.Round(-(decimal)v, decimals, MidpointRounding.AwayFromZero) > 0) Append('-');
                v = -v;
            }

            // round through decimal: (decimal)double keeps 15 significant digits, so 1.005 -> "1.01" as KSP's Mono
            // ToString("F2") prints it. A value 1 ulp under a rounding edge can still differ: Mono prints
            // -0.0049999999999999949 as "0.00" at F2, this gives "-0.01" (the only one of 455 edge cases tried)
            long n = (long)(Math.Round((decimal)v, decimals, MidpointRounding.AwayFromZero) * (decimal)pow10[decimals]);
            long ip = n / (long)pow10[decimals];
            long fp = n % (long)pow10[decimals];

            AppendInt(ip);
            if (decimals > 0)
            {
                Append('.');
                Ensure(decimals);
                for (int i = decimals - 1; i >= 0; i--)
                {
                    buf[len + i] = (char)('0' + fp % 10);
                    fp /= 10;
                }
                len += decimals;
            }
            return this;
        }

        public CharBuffer AppendInt(long v)
        {
            if (v < 0)
            {
                Append('-');
                v = -v;
            }
            int digits = 1;
            for (long t = v; t >= 10; t /= 10) digits++;
            Ensure(digits);
            for (int i = digits - 1; i >= 0; i--)
            {
                buf[len + i] = (char)('0' + v % 10);
                v /= 10;
            }
            len += digits;
            return this;
        }

        /// <summary>
        /// MechJeb's GuiUtils.TimeToDHMS(seconds, decimalPlaces) without allocations. The day and year lengths are in seconds, as
        /// KSP's date formatter gives them: a year need not be a whole number of days, nor a day of hours.
        /// </summary>
        public CharBuffer AppendDHMS(double seconds, int decimalPlaces, long daySeconds, long yearSeconds)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds)) return Append("Inf");
            // a calendar without days or years (a length of 0): hours are the largest unit
            if (daySeconds <= 0) daySeconds = long.MaxValue;
            if (yearSeconds <= 0) yearSeconds = long.MaxValue;
            // GuiUtils.TimeToDHMS: from a thousand years, years with an SI prefix ("398.6 Ma")
            double years = seconds / yearSeconds;
            if (Math.Abs(years) >= 1000) return AppendSI(years).Append('a');
            bool negative = seconds < 0;
            seconds = Math.Abs(seconds);
            // round to the decimals shown before splitting into units: 239.97 s is "4m 00.0s", not "3m 60.0s"
            if (decimalPlaces > 0)
                seconds = (double)Math.Round((decimal)seconds, Math.Min(decimalPlaces, 9), MidpointRounding.AwayFromZero);
            // no "-00.0s" for a value that rounds to zero
            if (negative && (decimalPlaces > 0 ? seconds > 0 : seconds >= 1)) Append('-');
            long year = yearSeconds, day = daySeconds;
            long y = (long)(seconds / year);
            long d = (long)(seconds % year / day);
            long h = (long)(seconds % day / 3600);
            long m = (long)(seconds % 3600 / 60);
            double s = seconds % 60;
            bool any = false;
            if (y > 0) { AppendInt(y).Append('y'); any = true; }
            if (any || d > 0) { if (any) Append(' '); Pad(d, any ? 3 : 1).Append('d'); any = true; }
            if (any || h > 0) { if (any) Append(' '); Pad(h, any ? 2 : 1).Append('h'); any = true; }
            if (any || m > 0) { if (any) Append(' '); Pad(m, any ? 2 : 1).Append('m'); any = true; }
            if (any) Append(' ');
            // GuiUtils.TimeToDHMS tests the remaining seconds (always < 60 here), so decimals always show when asked
            if (decimalPlaces > 0)
            {
                if (s < 10) Append('0'); // GuiUtils uses "00.0" here; s is already rounded
                Append(s, decimalPlaces);
            }
            else Pad((long)s, any ? 2 : 1);
            Append('s');
            return this;
        }

        static readonly string[] posPrefix = { " ", "k", "M", "G", "T", "P", "E", "Z", "Y", "R", "Q" };
        static readonly string[] negPrefix = { " ", "m", "μ", "n", "p", "f", "a", "z", "y", "r", "q" };

        /// <summary>MechJebLib Statics.ToSI(d, sigFigs, maxPrecision): "{d:F n} {prefix}", same rounding rules.</summary>
        public CharBuffer AppendSI(double d, int sigFigs = 4, int maxPrecision = int.MaxValue)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) return Append(double.IsNaN(d) ? "NaN" : d > 0 ? "Infinity" : "-Infinity");
            if (maxPrecision == int.MaxValue) maxPrecision = -29 - sigFigs;

            double offset = 5 * (d != 0 ? Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) - sigFigs) : 0);
            int exponent = (int)Math.Floor(Math.Log10(Math.Abs(d) + offset));
            int index = d != 0 ? (int)Math.Abs(Math.Floor(exponent / 3.0)) : 0;
            if (index > 10) index = 10;
            int siExponent = Math.Sign(exponent) * index * 3;
            string unit = exponent < 0 ? negPrefix[index] : posPrefix[index];
            d /= Math.Pow(10, siExponent);
            int wholeDigits = d != 0 ? exponent - siExponent + 1 : 1;
            int maxDecimalDigits = siExponent - maxPrecision;
            int decimalDigits = sigFigs - wholeDigits;
            decimalDigits = decimalDigits > maxDecimalDigits ? maxDecimalDigits : decimalDigits;
            decimalDigits = decimalDigits < 0 ? 0 : decimalDigits;
            return Append(d, decimalDigits).Append(' ').Append(unit);
        }

        /// <summary>Coordinates.AngleToDMS: {deg}° {min:00}' {sec:00}"</summary>
        public CharBuffer AppendDMS(double angle)
        {
            double a = Math.Abs(angle);
            int degrees = (int)Math.Floor(a);
            int minutes = (int)Math.Floor(60 * (a - degrees));
            int seconds = (int)Math.Floor(3600 * (a - degrees - minutes / 60.0));
            AppendInt(degrees).Append("° ");
            Pad(minutes, 2).Append("' ");
            return Pad(seconds, 2).Append('"');
        }

        /// <summary>True when the text is the first <paramref name="length"/> characters of <paramref name="other"/>.</summary>
        public bool SameAs(char[] other, int length)
        {
            if (length != len || other == null || other.Length < len) return false;
            for (int i = 0; i < len; i++)
                if (buf[i] != other[i]) return false;
            return true;
        }

        /// <summary>The text as a string (allocates: for tests, a description, a copy).</summary>
        public override string ToString() => new string(buf, 0, len);

        CharBuffer Pad(long v, int width)
        {
            for (long t = 10, w = 1; w < width; t *= 10, w++)
                if (v < t) Append('0');
            return AppendInt(v);
        }
    }
}
