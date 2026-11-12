/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace MuMech.MechJebKos
{
    // Argument checks shared by the bindings.
    internal static class KosArgs
    {
        /// <summary>
        /// The enum value named by <paramref name="value"/>, ignoring case. Only names are accepted (Enum.TryParse would also take
        /// "7" or "A, B"), and only those in <paramref name="allowed"/> when it is given.
        /// </summary>
        internal static T ParseEnum<T>(StringValue value, string what, IEnumerable<T>? allowed = null) where T : struct, Enum
        {
            T[] values = (allowed ?? (T[])Enum.GetValues(typeof(T))).ToArray();
            string name = value.ToString().Trim();

            foreach (T v in values)
                if (string.Equals(v.ToString(), name, StringComparison.OrdinalIgnoreCase))
                    return v;

            throw new KOSException($"Invalid {what} '{value}'. Expected one of: {string.Join(", ", values)}.");
        }

        /// <summary>The value, or a KOSException naming the suffix when it is NaN or infinite.</summary>
        internal static double Finite(ScalarValue value, string what)
        {
            double d = value.GetDoubleValue();
            if (double.IsNaN(d) || double.IsInfinity(d))
                throw new KOSException($"{what} must be a finite number.");
            return d;
        }
    }
}
