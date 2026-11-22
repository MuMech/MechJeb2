#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>A run of matched characters in a text: <c>Start</c> and <c>Length</c>, for a highlight.</summary>
    public struct TextRange
    {
        public int Start;
        public int Length;

        public TextRange(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int End => Start + Length;
    }

    /// <summary>
    /// Scores how well a query matches a text, without Unity: subsequence matching with bonuses (the idea of ui-sdk's fuzzy
    /// search, written for KodeKit). Set <see cref="Query"/> once, then score each text.
    /// <list type="bullet">
    /// <item>Every query character must appear in the text, in order, ignoring case (invariant) and ignoring spaces in the query;
    /// otherwise the text scores <see cref="NoMatch"/>. An empty query matches everything with score 0.</item>
    /// <item>The best alignment wins, not the first one: a character that continues the previous one, starts a word (after a
    /// space, a symbol, or a lower to upper case change as in <c>TimeToApoapsis</c>) or starts the text scores more, and a gap
    /// between matched characters costs. So a prefix ranks above a word start, which ranks above a scattered subsequence.</item>
    /// <item>The matched characters come back as ranges (consecutive characters merged) for a highlight.</item>
    /// <item>Scoring allocates nothing once the buffers fit the longest query and text seen.</item>
    /// </list>
    /// </summary>
    [NotAWidget("helper: ranks texts against a query (a drop-down's filter, a tree's search)")]
    public sealed class FuzzyMatcher
    {
        public const int NoMatch = int.MinValue;

        const int Unreachable = int.MinValue / 2;
        const int CharScore = 16;
        const int WordStartBonus = 12;
        const int TextStartBonus = 8;
        const int ConsecutiveBonus = 14;
        const int GapPenalty = 2;
        const int LeadingPenalty = 1;
        const int LeadingPenaltyMax = 6;

        char[] query = new char[16];
        int queryLength;
        int[] score = new int[16 * 64];
        int[] parent = new int[16 * 64];

        string queryText = "";

        /// <summary>The query: its non-space characters, lowered, are what a text must contain in order.</summary>
        public string Query
        {
            get => queryText;
            set
            {
                value = value ?? "";
                if (string.Equals(value, queryText, StringComparison.Ordinal)) return;
                queryText = value;
                if (query.Length < value.Length) query = new char[Math.Max(value.Length, query.Length * 2)];
                queryLength = 0;
                for (int i = 0; i < value.Length; i++)
                    if (!char.IsWhiteSpace(value[i])) query[queryLength++] = char.ToLowerInvariant(value[i]);
            }
        }

        /// <summary>True when the query has no character to match.</summary>
        public bool IsEmpty => queryLength == 0;

        /// <summary>The text's score for the query, or <see cref="NoMatch"/>. Higher is better.</summary>
        public int Score(string text) => Score(text, null);

        /// <summary>As <see cref="Score(string)"/>; also fills <paramref name="ranges"/> (cleared first) with the matched characters.</summary>
        public int Score(string text, List<TextRange> ranges)
        {
            ranges?.Clear();
            if (queryLength == 0) return 0;
            if (string.IsNullOrEmpty(text)) return NoMatch;
            int n = text.Length, m = queryLength;
            if (m > n) return NoMatch;
            if (!IsSubsequence(text)) return NoMatch;

            if (score.Length < m * n)
            {
                score = new int[m * n];
                parent = new int[m * n];
            }

            for (int j = 0; j < m; j++)
            {
                char q = query[j];
                int row = j * n;
                int prev = row - n;
                // the best score of the previous query character at a text position two or more before t, less the gap to t
                int gap = Unreachable, gapAt = -1;
                for (int t = 0; t < n; t++)
                {
                    if (j > 0 && t >= 2)
                    {
                        // the candidate at t - 2 joins the pool; every older one has lost one more skipped character
                        if (gap > Unreachable) gap -= GapPenalty;
                        int candidate = score[prev + t - 2];
                        if (candidate > Unreachable && candidate - GapPenalty > gap)
                        {
                            gap = candidate - GapPenalty;
                            gapAt = t - 2;
                        }
                    }
                    score[row + t] = Unreachable;
                    if (char.ToLowerInvariant(text[t]) != q) continue;
                    int bonus = CharScore;
                    if (t == 0) bonus += TextStartBonus + WordStartBonus;
                    else if (StartsWord(text, t)) bonus += WordStartBonus;

                    if (j == 0)
                    {
                        score[row + t] = bonus - Math.Min(t * LeadingPenalty, LeadingPenaltyMax);
                        parent[row + t] = -1;
                        continue;
                    }
                    int best = Unreachable, from = -1;
                    if (t >= 1 && score[prev + t - 1] > Unreachable)
                    {
                        best = score[prev + t - 1] + ConsecutiveBonus;
                        from = t - 1;
                    }
                    if (gap > Unreachable && gap > best)
                    {
                        best = gap;
                        from = gapAt;
                    }
                    if (from < 0) continue;
                    score[row + t] = best + bonus;
                    parent[row + t] = from;
                }
            }

            int last = (m - 1) * n, bestScore = Unreachable, bestAt = -1;
            for (int t = 0; t < n; t++)
                if (score[last + t] > bestScore)
                {
                    bestScore = score[last + t];
                    bestAt = t;
                }
            if (bestAt < 0) return NoMatch;

            if (ranges != null)
            {
                // walked back from the end, so each range is added before the ones in front of it: the list is reversed after
                int at = bestAt;
                for (int j = m - 1; j >= 0; j--)
                {
                    int count = ranges.Count;
                    if (count > 0 && ranges[count - 1].Start == at + 1)
                        ranges[count - 1] = new TextRange(at, ranges[count - 1].Length + 1);
                    else
                        ranges.Add(new TextRange(at, 1));
                    at = parent[j * n + at];
                }
                ranges.Reverse();
            }
            return bestScore;
        }

        bool IsSubsequence(string text)
        {
            int q = 0;
            for (int i = 0; i < text.Length && q < queryLength; i++)
                if (char.ToLowerInvariant(text[i]) == query[q]) q++;
            return q == queryLength;
        }

        /// <summary>A character that begins a word: a letter or digit after a symbol or space, or an upper case after a lower case.</summary>
        static bool StartsWord(string text, int t)
        {
            char p = text[t - 1], c = text[t];
            if (!char.IsLetterOrDigit(p)) return char.IsLetterOrDigit(c);
            return char.IsUpper(c) && !char.IsUpper(p);
        }
    }

    /// <summary>The English name an item is also found by, from its localization token.</summary>
    public static class FuzzyKeys
    {
        const string Prefix = "MechJeb_";

        /// <summary>
        /// The token without its <c>#</c> and <c>MechJeb_</c> prefix: <c>#MechJeb_Altitude_ASL</c> gives <c>Altitude_ASL</c>
        /// (an underscore is a word break to the matcher). Null stays null; a token without the prefix keeps its text.
        /// </summary>
        public static string FromToken(string token)
        {
            if (token == null) return null;
            if (token.StartsWith("#", StringComparison.Ordinal)) token = token.Substring(1);
            if (token.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) token = token.Substring(Prefix.Length);
            return token;
        }
    }

    /// <summary>One item that matched: its index, its score, and whether its key, not its label, matched better.</summary>
    public struct FuzzyResult
    {
        public int Index;
        public int Score;
        public bool ByKey;

        /// <summary>The length of the text that matched, the tie-break after the score (a shorter text is the closer match).</summary>
        public int Length;
    }

    /// <summary>
    /// Filters and ranks a list of items by a typed query, without Unity: what a filter field above a long list needs.
    /// Each item has a label (what the user sees, localized) and optionally a key (a language-independent name such as the
    /// English one): an item matches when either does.
    /// <list type="bullet">
    /// <item>An empty query lists every item in its own order, which is how a cleared filter brings the whole list back.</item>
    /// <item>Otherwise the matches come best first. The order is total and stable: higher score, then the shorter text, then the
    /// item's own position, so the same query always gives the same list.</item>
    /// <item>The label wins a tie with the key: a key's score is lowered by <see cref="KeyPenalty"/>.</item>
    /// <item><see cref="Apply"/> allocates nothing: the result list is reused. A query equal to the last one does no work.</item>
    /// <item><see cref="Highlight"/> gives the matched characters of an item's label.</item>
    /// </list>
    /// </summary>
    [NotAWidget("helper: a list's entries filtered and ranked (ComboModel describes the list)")]
    public sealed class FuzzyFilter
    {
        public const int KeyPenalty = 4;

        // an IComparer, not a Comparison: List.Sort wraps a Comparison in a new object on every call
        sealed class RankOrder : IComparer<FuzzyResult>
        {
            public int Compare(FuzzyResult a, FuzzyResult b)
            {
                if (a.Score != b.Score) return b.Score.CompareTo(a.Score);
                if (a.Length != b.Length) return a.Length.CompareTo(b.Length);
                return a.Index.CompareTo(b.Index);
            }
        }

        static readonly RankOrder ByRank = new RankOrder();

        readonly FuzzyMatcher matcher = new FuzzyMatcher();
        string[] labels;
        string[] keys;
        string applied;

        /// <summary>The items' labels; <paramref name="keys"/> (null, or with null entries) are the alternative names.</summary>
        public FuzzyFilter(string[] labels, string[] keys = null) => SetItems(labels, keys);

        public int Count => labels.Length;

        /// <summary>The matches of the last <see cref="Apply"/>, best first (the items' order for an empty query). Owned by the filter.</summary>
        public List<FuzzyResult> Results { get; } = new List<FuzzyResult>();

        public string Query => applied ?? "";

        public void SetItems(string[] labels, string[] keys = null)
        {
            this.labels = labels ?? throw new ArgumentNullException(nameof(labels));
            if (keys != null && keys.Length != labels.Length) throw new ArgumentException("one key (or null) per label", nameof(keys));
            this.keys = keys;
            applied = null;
            Apply("");
        }

        /// <summary>Filters for <paramref name="query"/>. False when it is the query already applied (nothing changed).</summary>
        public bool Apply(string query)
        {
            query = query ?? "";
            if (string.Equals(query, applied, StringComparison.Ordinal)) return false;
            applied = query;
            matcher.Query = query;
            Results.Clear();
            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i];
                int len = label?.Length ?? 0;
                if (matcher.IsEmpty)
                {
                    Results.Add(new FuzzyResult { Index = i, Score = 0, Length = len });
                    continue;
                }
                int s = matcher.Score(label);
                bool byKey = false;
                string key = keys?[i];
                if (key != null)
                {
                    int k = matcher.Score(key);
                    if (k != FuzzyMatcher.NoMatch && (s == FuzzyMatcher.NoMatch || k - KeyPenalty > s))
                    {
                        s = k - KeyPenalty;
                        len = key.Length;
                        byKey = true;
                    }
                }
                if (s == FuzzyMatcher.NoMatch) continue;
                Results.Add(new FuzzyResult { Index = i, Score = s, ByKey = byKey, Length = len });
            }
            if (!matcher.IsEmpty) Results.Sort(ByRank);
            return true;
        }

        /// <summary>The matched characters of the item's label (cleared and filled). Empty when the key matched, or the query is empty.</summary>
        public void Highlight(FuzzyResult result, List<TextRange> ranges)
        {
            ranges.Clear();
            if (result.ByKey || matcher.IsEmpty) return;
            matcher.Score(labels[result.Index], ranges);
        }
    }
}
