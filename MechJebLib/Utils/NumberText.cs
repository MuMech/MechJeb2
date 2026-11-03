/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Globalization;

namespace MechJebLib.Utils
{
    /// <summary>Culture independent parsing of the numbers a user types in a text field.</summary>
    public static class NumberText
    {
        /// <summary>Largest number of entries a typed range like "1-50" may expand to.</summary>
        public const int MAX_RANGE_LENGTH = 1000;

        /// <summary>
        ///     Parses a typed number. A lone comma with no point is a decimal separator ("0,5" is 0.5), otherwise commas are
        ///     thousands separators and dropped ("1,000.5" is 1000.5).
        /// </summary>
        public static bool TryParseDouble(string text, out double val)
        {
            text = text.Trim();
            int firstComma = text.IndexOf(',');
            if (firstComma >= 0)
            {
                if (text.IndexOf('.') < 0 && text.IndexOf(',', firstComma + 1) < 0)
                    text = text.Replace(',', '.');
                else
                    text = text.Replace(",", "");
            }

            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out val);
        }

        /// <summary>
        ///     Expands "1,2,3" and "1-3" into a list. A range longer than <see cref="MAX_RANGE_LENGTH" /> entries is ignored.
        /// </summary>
        public static void ParseIntList(string text, List<int> result)
        {
            result.Clear();

            foreach (string x in text.Split(','))
            {
                string[] y = x.Split('-');

                if (!int.TryParse(y[0].Trim(), out int start)) continue;
                if (!int.TryParse(y[y.Length - 1].Trim(), out int end)) continue;

                if ((long)end - start >= MAX_RANGE_LENGTH) continue;

                for (long n = start; n <= end; n++)
                    result.Add((int)n);
            }
        }
    }
}
