/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    /// <summary>
    ///     Loads the SimVessel dumps from KSP.log captures in the Captures directory, which are what the fixtures
    ///     reproduce.
    /// </summary>
    public static class Captures
    {
        private static readonly Regex _logHeader = new Regex(@"^\[LOG [^\]]*\] \[MechJeb2\] ");

        // SimPart.Mass is derived state which UpdateMass() recomputes before it is used.  The captured values are left
        // over from the previous FuelFlowSimulation run, since SimVesselManager.Update() does not refresh them.
        private static readonly Regex _partMass = new Regex(@" Mass=\S+");

        /// <summary>
        ///     Returns each dump in the capture with the KSP.log line header stripped, in the order they were logged.
        /// </summary>
        public static List<string> Load(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FuelFlowSimulationTests", "Captures", fileName);

            var dumps = new List<string>();
            StringBuilder? dump = null;

            foreach (string line in File.ReadAllLines(path))
            {
                Match header = _logHeader.Match(line);

                if (header.Success)
                {
                    if (dump != null)
                        dumps.Add(dump.ToString().TrimEnd());
                    dump = new StringBuilder();
                    dump.Append(line.Substring(header.Length)).Append('\n');
                    continue;
                }

                dump?.Append(line).Append('\n');
            }

            if (dump != null)
                dumps.Add(dump.ToString().TrimEnd());

            return dumps;
        }

        /// <summary>
        ///     Normalizes a dump for comparison, unifying the line endings and dropping the SimPart.Mass values.
        /// </summary>
        public static string Normalize(string dump) => _partMass.Replace(dump.Replace("\r\n", "\n"), "");
    }
}
