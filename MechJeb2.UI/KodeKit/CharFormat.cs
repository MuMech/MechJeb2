using System;
using TMPro;

namespace KodeKit
{
    /// <summary>
    /// Formats numbers into a reusable char buffer so live readouts can be pushed to TMP with
    /// SetCharArray, without allocating strings. Only writes to TMP when the characters changed.
    /// </summary>
    public class CharText
    {
        readonly TMP_Text target;
        char[] buf = new char[32];
        char[] shown = new char[32];
        int len, shownLen = -1;

        public CharText(TMP_Text target) => this.target = target;

        public int Length => len;

        public CharText Clear()
        {
            len = 0;
            return this;
        }

        void Ensure(int extra)
        {
            if (len + extra <= buf.Length) return;
            Array.Resize(ref buf, Math.Max(buf.Length * 2, len + extra));
        }

        public CharText Append(char c)
        {
            Ensure(1);
            buf[len++] = c;
            return this;
        }

        public CharText Append(string s)
        {
            if (s == null) return this;
            Ensure(s.Length);
            for (int i = 0; i < s.Length; i++) buf[len++] = s[i];
            return this;
        }

        static readonly double[] pow10 = { 1, 10, 100, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9 };

        /// <summary>Fixed-point, like ToString("F" + decimals), invariant culture.</summary>
        public CharText Append(double v, int decimals)
        {
            if (double.IsNaN(v)) return Append("NaN");
            if (double.IsInfinity(v)) return Append(v > 0 ? "Inf" : "-Inf");
            decimals = Math.Max(0, Math.Min(9, decimals));
            // beyond long/decimal range: let .NET format it (allocates, but only for absurd values)
            if (Math.Abs(v) * pow10[decimals] >= 9.2e18) return Append(v.ToString("F" + decimals));
            if (v < 0)
            {
                // ToString prints -0.001 as "0.00", not "-0.00" (KSP's Mono, checked in game)
                if (Math.Round(-(decimal)v, decimals, MidpointRounding.AwayFromZero) > 0) Append('-');
                v = -v;
            }

            // round through decimal: (decimal)double keeps 15 significant digits, so 1.005 -> "1.01" as KSP's Mono
            // ToString("F2") prints it. A value 1 ulp under a rounding edge can still differ: Mono prints
            // -0.0049999999999999949 as "0.00" at F2, this gives "-0.01" (the only one of 455 edge cases tried)
            long n =(long)(Math.Round((decimal)v, decimals, MidpointRounding.AwayFromZero) * (decimal)pow10[decimals]);
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

        public CharText AppendInt(long v)
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

        /// <summary>MechJeb's GuiUtils.TimeToDHMS(seconds, decimalPlaces) without allocations.</summary>
        public CharText AppendDHMS(double seconds, int decimalPlaces, int hoursPerDay, int daysPerYear)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds)) return Append("Inf");
            // GuiUtils.TimeToDHMS: from a thousand years, years with an SI prefix ("398.6 Ma")
            double years = seconds / ((double)hoursPerDay * 3600 * daysPerYear);
            if (Math.Abs(years) >= 1000) return AppendSI(years).Append('a');
            int start = len;
            bool negative = seconds < 0;
            seconds = Math.Abs(seconds);
            // round to the decimals shown before splitting into units: 239.97 s is "4m 00.0s", not "3m 60.0s"
            if (decimalPlaces > 0)
                seconds = (double)Math.Round((decimal)seconds, Math.Min(decimalPlaces, 9), MidpointRounding.AwayFromZero);
            // no "-00.0s" for a value that rounds to zero
            if (negative && (decimalPlaces > 0 ? seconds > 0 : seconds >= 1)) Append('-');
            long year = (long)hoursPerDay * 3600 * daysPerYear, day = (long)hoursPerDay * 3600;
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
        public CharText AppendSI(double d, int sigFigs = 4, int maxPrecision = int.MaxValue)
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
        public CharText AppendDMS(double angle)
        {
            double a = Math.Abs(angle);
            int degrees = (int)Math.Floor(a);
            int minutes = (int)Math.Floor(60 * (a - degrees));
            int seconds = (int)Math.Floor(3600 * (a - degrees - minutes / 60.0));
            AppendInt(degrees).Append("° ");
            Pad(minutes, 2).Append("' ");
            return Pad(seconds, 2).Append('"');
        }

        /// <summary>For tests: the current buffer as a string (allocates).</summary>
        public override string ToString() => new string(buf, 0, len);

        CharText Pad(long v, int width)
        {
            for (long t = 10, w = 1; w < width; t *= 10, w++)
                if (v < t) Append('0');
            return AppendInt(v);
        }

        /// <summary>Push to TMP if the characters differ from what is shown. Returns true if the text changed.</summary>
        public bool Commit()
        {
            if (len == shownLen)
            {
                bool same = true;
                for (int i = 0; i < len; i++)
                    if (buf[i] != shown[i]) { same = false; break; }
                if (same) return false;
            }
            if (shown.Length < len) shown = new char[buf.Length];
            Array.Copy(buf, shown, len);
            shownLen = len;
            target.SetCharArray(buf, 0, len);
            return true;
        }
    }
}
