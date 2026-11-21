using System;
using TMPro;
using UnityEngine;

namespace KodeKit
{
    /// <summary>
    /// Formats numbers into a reusable char buffer (<see cref="Headless.CharBuffer"/>) so live readouts can be pushed to TMP with
    /// SetCharArray, without allocating strings. Only writes to TMP when the characters changed, or when TMP shows them cut
    /// with an ellipsis and their box has changed width since (see Commit).
    /// </summary>
    public class CharText
    {
        const char Ellipsis = '…';
        readonly TMP_Text target;
        char[] shown = new char[32];
        int shownLen = -1;
        float pushWidth = -1; // the box's width at the last push
        float pushTime;       // realtime of the last push
        const float RepushInterval = 1f; // seconds between two pushes of an unchanged text that shows cut

        public CharText(TMP_Text target) : this(target, new Headless.CharBuffer()) { }

        /// <summary>A text over a buffer someone else fills (a table model's cell): <see cref="Commit"/> pushes what it holds.</summary>
        public CharText(TMP_Text target, Headless.CharBuffer chars)
        {
            this.target = target;
            Chars = chars ?? throw new ArgumentNullException(nameof(chars));
        }

        /// <summary>The characters, formatted by <see cref="Headless.CharBuffer"/> (no Unity there, tested in MechJebLibTest).</summary>
        public Headless.CharBuffer Chars { get; }

        public int Length => Chars.Length;

        /// <summary>The text it pushes to.</summary>
        public TMP_Text Target => target;

        public CharText Clear()
        {
            Chars.Clear();
            return this;
        }

        public CharText Append(char c)
        {
            Chars.Append(c);
            return this;
        }

        public CharText Append(string s)
        {
            Chars.Append(s);
            return this;
        }

        /// <summary>Fixed-point, like ToString("F" + decimals), invariant culture.</summary>
        public CharText Append(double v, int decimals)
        {
            Chars.Append(v, decimals);
            return this;
        }

        public CharText AppendInt(long v)
        {
            Chars.AppendInt(v);
            return this;
        }

        /// <summary>
        /// MechJeb's GuiUtils.TimeToDHMS(seconds, decimalPlaces) without allocations. The day and year lengths are in seconds, as
        /// KSP's date formatter gives them: a year need not be a whole number of days, nor a day of hours.
        /// </summary>
        public CharText AppendDHMS(double seconds, int decimalPlaces, long daySeconds, long yearSeconds)
        {
            Chars.AppendDHMS(seconds, decimalPlaces, daySeconds, yearSeconds);
            return this;
        }

        /// <summary>MechJebLib Statics.ToSI(d, sigFigs, maxPrecision): "{d:F n} {prefix}", same rounding rules.</summary>
        public CharText AppendSI(double d, int sigFigs = 4, int maxPrecision = int.MaxValue)
        {
            Chars.AppendSI(d, sigFigs, maxPrecision);
            return this;
        }

        /// <summary>Coordinates.AngleToDMS: {deg}° {min:00}' {sec:00}"</summary>
        public CharText AppendDMS(double angle)
        {
            Chars.AppendDMS(angle);
            return this;
        }

        /// <summary>For tests: the current buffer as a string (allocates).</summary>
        public override string ToString() => Chars.ToString();

        /// <summary>
        /// Push to TMP if the characters differ from what is shown, or if TMP shows them cut and their box is not the width
        /// they were pushed at. Returns true if the text was pushed.
        /// </summary>
        public bool Commit()
        {
            // KSP's TMP cuts a text with an ellipsis by writing the ellipsis and an end into its own char buffer, and
            // ParseInputText restores that buffer only for string texts, not SetCharArray ones: once cut, the value stays cut
            // at every later width, and the cut text ends in the ellipsis even where it fits. TMP can cut it again at each
            // layout between two commits, and its isTextTruncated only tells about the last one, so look at what it shows:
            // cut, in a box of another width than at the push, push it again (TMP then cuts it afresh, or shows all of it).
            // A box that keeps its width costs nothing more, cut or not, but for one push a second while it shows cut: the box can
            // narrow and come back to the same width between two commits, which leaves the text cut where it fits.
            int len = Chars.Length;
            if (Chars.SameAs(shown, shownLen) &&
                (!ShowsEllipsis() || target.rectTransform.rect.width == pushWidth && Time.realtimeSinceStartup - pushTime < RepushInterval))
                return false;
            if (shown.Length < len) shown = new char[Chars.Chars.Length];
            Array.Copy(Chars.Chars, shown, len);
            shownLen = len;
            pushWidth = target.rectTransform.rect.width;
            pushTime = Time.realtimeSinceStartup;
            target.SetCharArray(Chars.Chars, 0, len);
            return true;
        }

        bool ShowsEllipsis()
        {
            TMP_TextInfo info = target.textInfo;
            int n = info?.characterCount ?? 0;
            return n > 0 && info.characterInfo[n - 1].character == Ellipsis;
        }
    }
}
