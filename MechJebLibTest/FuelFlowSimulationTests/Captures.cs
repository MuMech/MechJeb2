/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Globalization;
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

        // the captures hold a dump in vacuum and a dump in the atmospheric conditions on the launchpad at KSC
        public static bool IsVacuum(string dump) => dump.Contains("ATMPressure=0 ATMDensity=0 MachNumber=0");

        private static readonly Regex _partHeader = new Regex(@"^\s+SimPart '(.*)':$");

        /// <summary>
        ///     The SimPart.Mass of each part in a dump, by SimPart.Ident.  These are what the previous FuelFlowSimulation run
        ///     in KSP left behind.  The dump omits a Mass of zero, along with the other fields left at their defaults.
        /// </summary>
        public static Dictionary<string, double> PartMasses(string dump)
        {
            var masses = new Dictionary<string, double>();
            string[] lines = dump.Replace("\r\n", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                Match header = _partHeader.Match(lines[i]);
                if (!header.Success)
                    continue;

                // the fields are on the line after the header, which is absent when every field has its default
                Match mass = i + 1 < lines.Length ? _partMass.Match(lines[i + 1]) : Match.Empty;
                masses.Add(header.Groups[1].Value,
                    mass.Success ? double.Parse(mass.Value.Substring(" Mass=".Length), CultureInfo.InvariantCulture) : 0);
            }

            return masses;
        }
    }
}
