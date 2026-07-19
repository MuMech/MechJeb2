/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using static System.FormattableString;

namespace MechJebLib.FuelFlowSimulation
{
    public abstract class SimPartModule : IDisposable
    {
        public bool IsEnabled = false;
        public SimPart Part = null!;
        public bool ModuleIsEnabled = true;
        public bool StagingEnabled = true;

        public abstract void Dispose();

        // Adds "name=value" to the token list only when the value differs from its declared default, so the concrete
        // ToString() debug dumps stay focused on what a fixture actually needs to set.
        protected static void AddField(List<string> fields, string name, bool val, bool def)
        {
            if (val != def) fields.Add(Invariant($"{name}={val}"));
        }

        protected static void AddField(List<string> fields, string name, double val, double def)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (val != def) fields.Add(Invariant($"{name}={val}"));
        }

        // The fields common to every SimPartModule, for the concrete ToString() dumps to prepend to their own fields.
        protected List<string> CommonFieldList()
        {
            var fields = new List<string>();
            AddField(fields, "IsEnabled", IsEnabled, false);
            AddField(fields, "ModuleIsEnabled", ModuleIsEnabled, true);
            AddField(fields, "StagingEnabled", StagingEnabled, true);
            return fields;
        }

        // Composes the header line of a module dump ("SimModuleX: tok tok ..."), with no trailing space when no fields
        // differ from their defaults.
        protected static string ModuleLine(string type, List<string> fields) =>
            fields.Count == 0 ? type + ":" : type + ": " + string.Join(" ", fields);
    }
}
