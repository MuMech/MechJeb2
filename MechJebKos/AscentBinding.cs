/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:ASCENT - entry point for the ascent autopilots.
    //
    // The classic and PSG autopilots have substantially different controls, so each gets its own
    // child binding (:CLASSIC / :PSG). This node carries the shared selection/readouts: TYPE chooses
    // the ascent style (and flips the ascent window to match) without engaging, and DISENGAGE/STATUS
    // /etc. act on whichever style is currently active.
    [KOSNomenclature("MechJebAscent")]
    public class AscentBinding : Structure
    {
        private readonly Func<MechJebCore?> _core;
        private readonly AscentClassicBinding _classic;
        private readonly AscentPSGBinding _psg;

        public AscentBinding(Func<MechJebCore?> core)
        {
            _core = core;
            _classic = new AscentClassicBinding(core);
            _psg = new AscentPSGBinding(core);
            RegisterInitializer(InitializeSuffixes);
        }

        private MechJebCore Core => _core() ?? throw new KOSException("MechJeb is not available on this vessel.");

        private void InitializeSuffixes()
        {
            AddSuffix("CLASSIC", new NoArgsSuffix<AscentClassicBinding>(() => _classic,
                "The classic (gravity-turn) ascent autopilot."));
            AddSuffix("PSG", new NoArgsSuffix<AscentPSGBinding>(() => _psg,
                "The PSG (powered-explicit-guidance) ascent autopilot."));

            AddSuffix("TYPE", new SetSuffix<StringValue>(() => Core.AscentSettings.AscentType.ToString(), SetType,
                "Active ascent style, \"CLASSIC\" or \"PSG\". Setting it flips MechJeb and the ascent window without engaging."));

            AddSuffix("ENABLED", new Suffix<BooleanValue>(() => Core.Ascent.Enabled,
                "Whether an ascent autopilot is currently engaged."));
            AddSuffix("DISENGAGE", new NoArgsVoidSuffix(Disengage,
                "Disengage whichever ascent autopilot is active."));
            AddSuffix("STATUS", new Suffix<StringValue>(() => Core.Ascent.Status,
                "Status text of the active ascent autopilot."));
            AddSuffix("TMINUS", new Suffix<ScalarValue>(() => Core.Ascent.TMinus,
                "Seconds until a timed launch (meaningful only when TIMEDLAUNCH is true)."));
            AddSuffix("TIMEDLAUNCH", new Suffix<BooleanValue>(() => Core.Ascent.TimedLaunch,
                "Whether a timed launch countdown is active."));
        }

        private void SetType(StringValue value)
        {
            AscentType type;
            switch (value.ToString().Trim().ToUpperInvariant())
            {
                case "CLASSIC":
                    type = AscentType.CLASSIC;
                    break;
                case "PSG":
                    type = AscentType.PSG;
                    break;
                default:
                    throw new KOSException($"Unknown ascent type '{value}' (expected \"CLASSIC\" or \"PSG\").");
            }

            Core.AscentSettings.AscentType = type;
            Core.GetComputerModule<MechJebModuleAscentMenu>()._lastPSGSettingsEnabled = type == AscentType.PSG;
        }

        private void Disengage()
        {
            if (Core.AscentSettings.AscentType == AscentType.PSG)
                _psg.Disengage();
            else
                _classic.Disengage();
        }
    }
}
