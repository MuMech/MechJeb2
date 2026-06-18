/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:SETTINGS - MechJeb's global settings (MechJebModuleSettings).
    //
    // These are the persisted (Pass.GLOBAL) settings from MechJeb's Settings window.  Several of them
    // are applied through GuiUtils when the window draws, so the setters here replicate that apply (skin
    // load, GUI scale, drop-down/advanced toggles) to make the write take effect immediately.
    [KOSNomenclature("MechJebSettings")]
    public class SettingsBinding : ComputerModuleBinding<MechJebModuleSettings>
    {
        public SettingsBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            AddSuffix("UISCALE", new SetSuffix<ScalarValue>(() => Module.UIScale.Val,
                value =>
                {
                    Module.UIScale.Val = value;
                    GuiUtils.SetGUIScale(value);
                },
                "MechJeb UI scale factor (1.0 = normal)."));

            AddSuffix("SKINID", new SetSuffix<ScalarValue>(() => Module.SkinId,
                value =>
                {
                    Module.SkinId = (int)value;
                    GuiUtils.LoadSkin((GuiUtils.SkinType)(int)value);
                },
                "GUI skin (0 = MJ2, 1 = MJ1, 2 = MJ2 compact)."));

            AddSuffix("DONTUSEDROPDOWNMENU", new SetSuffix<BooleanValue>(() => Module.DontUseDropDownMenu,
                value =>
                {
                    Module.DontUseDropDownMenu = value;
                    GuiUtils.DontUseDropDownMenu = value;
                },
                "Replace the drop-down menu with an arrow selector."));

            AddSuffix("SHOWADVANCEDWINDOWSETTINGS", new SetSuffix<BooleanValue>(() => Module.ShowAdvancedWindowSettings,
                value =>
                {
                    Module.ShowAdvancedWindowSettings = value;
                    GuiUtils.ShowAdvancedWindowSettings = value;
                },
                "Show advanced window settings."));

            AddSuffix("RSSMODE", new SetSuffix<BooleanValue>(() => Module.RssMode, value => Module.RssMode = value,
                "Disabling a module does not kill throttle (RSS/RO mode)."));

            AddSuffix("HIDEBRAKEONEJECT", new SetSuffix<BooleanValue>(() => Module.HideBrakeOnEject, value => Module.HideBrakeOnEject = value,
                "Hide 'Brake on Eject' in the Rover Controller."));

            AddSuffix("USETITLEBARDRAGGING", new SetSuffix<BooleanValue>(() => Module.UseTitlebarDragging, value => Module.UseTitlebarDragging = value,
                "Use only the titlebar for window dragging."));
        }
    }
}
