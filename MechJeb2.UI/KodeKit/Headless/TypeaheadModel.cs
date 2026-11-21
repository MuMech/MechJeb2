#if !CSHARP_7_3
#nullable disable
#endif
using System;

namespace KodeKit.Headless
{
    /// <summary>
    /// Type letters on a focused list to jump to an entry, without Unity (ui-sdk's typeahead): what a drop-down or a list does
    /// with a key that is not a command. The host calls <see cref="Type"/> for each typed character with the time, and moves its
    /// focus to the index it returns.
    /// <list type="bullet">
    /// <item>The typed characters make a buffer, and the first entry whose label starts with it (ignoring case) is the answer.
    /// Pausing longer than <see cref="Timeout"/> seconds empties the buffer, so the next letter starts a new word. The time is
    /// the caller's (<c>Time.unscaledTime</c> in game), so a test drives it.</item>
    /// <item>Search starts at the current entry, so a longer prefix stays on the entry that already matches it, and wraps.</item>
    /// <item>One letter typed again and again (a, a, a) steps through the entries starting with it, from the one after the
    /// current, as a list box does; the first press of a letter also moves on from the current entry.</item>
    /// <item>With no match nothing moves (-1). A control character, or a space that would start the buffer, is not typeahead (-1).</item>
    /// <item>An entry matches by its label or by its key (the English name), as <see cref="FuzzyFilter"/> does.</item>
    /// <item>A keystroke allocates nothing: the buffer is a fixed array; the label and key delegates should be kept by the caller.</item>
    /// </list>
    /// </summary>
    [NotAWidget("helper: the typed prefix a list jumps to (ComboModel describes the list)")]
    public sealed class TypeaheadModel
    {
        public const int MaxLength = 32;

        readonly char[] buffer = new char[MaxLength];
        int length;
        double lastTime;

        public TypeaheadModel(double timeoutSeconds = 1.0)
        {
            if (timeoutSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            Timeout = timeoutSeconds;
        }

        /// <summary>Seconds of pause after which the typed characters are forgotten.</summary>
        public double Timeout { get; }

        /// <summary>What has been typed since the last pause.</summary>
        public string Buffer => new string(buffer, 0, length);

        public int Length => length;

        /// <summary>Forgets the typed characters (the list lost the focus, or took another key).</summary>
        public void Reset() => length = 0;

        /// <summary>
        /// A typed character at time <paramref name="now"/> (seconds). Returns the index to move to among
        /// <paramref name="count"/> entries (<paramref name="current"/>: the one now focused, -1 for none), or -1 when nothing
        /// matches or the character is not typeahead.
        /// </summary>
        public int Type(char c, double now, int count, Func<int, string> label, int current, Func<int, string> key = null)
        {
            if (char.IsControl(c)) return -1;
            if (length > 0 && (now - lastTime > Timeout || now < lastTime)) length = 0;
            if (length == 0 && char.IsWhiteSpace(c)) return -1;
            if (length == buffer.Length) return -1;
            lastTime = now;
            buffer[length++] = c;
            if (count <= 0) return -1;

            bool repeated = length > 1;
            for (int i = 1; repeated && i < length; i++)
                if (char.ToLowerInvariant(buffer[i]) != char.ToLowerInvariant(buffer[0])) repeated = false;

            // one letter, or the same one again: from the entry after the current. A longer prefix: from the current one
            int prefix = repeated ? 1 : length;
            int start = length == 1 || repeated ? current + 1 : Math.Max(current, 0);
            for (int i = 0; i < count; i++)
            {
                int index = ((start + i) % count + count) % count;
                if (StartsWith(label(index), prefix) || (key != null && StartsWith(key(index), prefix))) return index;
            }
            return -1;
        }

        bool StartsWith(string text, int prefix)
        {
            if (text == null || text.Length < prefix) return false;
            for (int i = 0; i < prefix; i++)
                if (char.ToLowerInvariant(text[i]) != char.ToLowerInvariant(buffer[i])) return false;
            return true;
        }
    }
}
