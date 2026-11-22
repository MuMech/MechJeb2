using System;
using System.IO;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSettings : DisplayModule
    {
        public MechJebModuleSettings(MechJebCore core) : base(core)
        {
            ShowInEditor = true;
            ShowInFlight = true;
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int SkinId = 2;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble UIScale = 1.0;

        /// <summary>The scales both Settings windows offer next to the UI Scale field.</summary>
        public static readonly double[] UiScalePresets = { 0.75, 1, 1.25, 1.5, 2 };

        public static readonly string[] UiScaleNames = Array.ConvertAll(UiScalePresets, p => p.ToString());

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool DontUseDropDownMenu;

        [ToggleInfoItem("#MechJeb_hideBrakeOnEject", InfoItem.Category.Misc), Persistent(pass = (int)Pass.GLOBAL)]
        //Hide 'Brake on Eject' in Rover Controller
        public bool HideBrakeOnEject;

        [ToggleInfoItem("#MechJeb_useTitlebarDragging", InfoItem.Category.Misc), Persistent(pass = (int)Pass.GLOBAL)]
        //Use only the titlebar for window dragging
        public bool UseTitlebarDragging;

        [ToggleInfoItem("#MechJeb_rssMode", InfoItem.Category.Misc), Persistent(pass = (int)Pass.GLOBAL)]
        //Module disabling does not kill throttle (RSS/RO)
        public bool RssMode;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowAdvancedWindowSettings;

        /// <summary>Opacity of the new UI's window backgrounds (0.3 to 1). IMGUI windows ignore it.</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public float UiOpacity = 0.85f;

        /// <summary>Seconds the MechJeb menu takes to slide open or closed (0 to 1; 0 shows it at once).</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public float MenuAnimationTime = 1f;

        // the new UI's look, set from its own Settings window; IMGUI ignores them
        public enum UiSkinType { Classic, Flat, Glass, ZTheme }

        /// <summary>New UI skin: Classic (stock sprites, tinted button roles), Flat (dark, flat), Glass (translucent HUD), ZTheme (the ZTheme mod's colours).</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public int UiSkin = (int)UiSkinType.Flat;

        /// <summary>New UI: tighter margins and rows, in every skin. A custom window's own Compact setting overrides it.</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UiCompact = true;

        /// <summary>New UI: the section headers it adds to long windows.</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UiSectionHeaders = true;

        /// <summary>The name of <see cref="InputHistory"/>'s node in this module's global settings.</summary>
        public const string InputHistoryNodeName = "InputHistory";

        /// <summary>
        /// The new UI's input history (the recent values of some of its fields), kept in the global settings. Read from the first
        /// global settings loaded in a game session; from then on the UI's copy is the newer one (a value recorded since the last
        /// save), so later loads leave it, and every save writes it. Null: none.
        /// </summary>
        public static ConfigNode InputHistory { get; private set; }

        /// <summary>Counts the loads that set <see cref="InputHistory"/>: the UI reads it again when this changes.</summary>
        public static int InputHistoryLoads { get; private set; }

        private static bool _inputHistoryRead;

        /// <summary>
        /// The UI's input history changed: <paramref name="node"/> is saved with the global settings at the next save (mark a settings
        /// module <see cref="ComputerModule.Dirty"/> for the periodic one).
        /// </summary>
        public static void SetInputHistory(ConfigNode node)
        {
            InputHistory = node;
            // marked read as well: a global settings load that comes after this (a vessel loaded before the next save) would
            // otherwise replace the UI's newer lists with the file's older ones
            _inputHistoryRead = true;
        }

        /// <summary>New UI: a dragged window's edge snaps to a screen edge or another window's edge within a few pixels.</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UiSnapWindows = true;

        /// <summary>
        /// New UI: an operation hovered in the Maneuver Planner's list draws the orbit its burn would give, dashed, on the map (only
        /// the operations that opt in; no node is placed). Off by default.
        /// </summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UiPlannerPreview;

        /// <summary>
        /// New UI: Utilities shows the throttle and the acceleration as meters, against the throttle and acceleration limits that
        /// apply (display only). Off by default.
        /// </summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UiMeters;

        /// <summary>The name of <see cref="Shortcuts"/>' node in this module's global settings.</summary>
        public const string ShortcutsNodeName = "UiShortcuts";

        /// <summary>
        /// New UI: the keyboard shortcuts (opt-in window toggles: none by default), one SHORTCUT node per binding with its action id
        /// and chord, kept in the global settings. Read from the first global settings loaded in a game session, as
        /// <see cref="InputHistory"/>; from then on the UI's copy is the newer one, and every save writes it. Null: none.
        /// </summary>
        public static ConfigNode Shortcuts { get; private set; }

        /// <summary>Counts the loads that set <see cref="Shortcuts"/>: the UI reads them again when this changes.</summary>
        public static int ShortcutsLoads { get; private set; }

        private static bool _shortcutsRead;

        /// <summary>The UI's shortcuts changed: <paramref name="node"/> is saved with the global settings at the next save.</summary>
        public static void SetShortcuts(ConfigNode node)
        {
            Shortcuts      = node;
            _shortcutsRead = true;
        }

        /// <summary>Raised after skin, scale or menu settings change, so another UI can follow them.</summary>
        public static event Action<MechJebModuleSettings> Changed;

        /// <summary>
        /// Push the settings to GuiUtils and notify listeners (used by kOS and by this window). <paramref name="loadSkin"/> is
        /// ignored: nothing draws with the IMGUI skins, and loading one needs IMGUI's OnGUI.
        /// </summary>
        public void ApplySettings(bool loadSkin = false)
        {
            GuiUtils.SetGUIScale(UIScale.Val);
            GuiUtils.DontUseDropDownMenu = DontUseDropDownMenu;
            GuiUtils.ShowAdvancedWindowSettings = ShowAdvancedWindowSettings;
            Changed?.Invoke(this);
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);
            if (!_inputHistoryRead && global != null)
            {
                _inputHistoryRead = true;
                InputHistory      = global.GetNode(InputHistoryNodeName)?.CreateCopy();
                InputHistoryLoads++;
            }

            if (!_shortcutsRead && global != null)
            {
                _shortcutsRead = true;
                Shortcuts      = global.GetNode(ShortcutsNodeName)?.CreateCopy();
                ShortcutsLoads++;
            }

            ApplySettings();
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);
            if (global != null && InputHistory != null) global.AddNode(InputHistoryNodeName, InputHistory.CreateCopy());
            if (global != null && Shortcuts != null) global.AddNode(ShortcutsNodeName, Shortcuts.CreateCopy());
        }

        /// <summary>Delete the global and vessel-type settings and reload every module (this module included).</summary>
        public void RestoreFactoryDefaults()
        {
            File.Delete(MuUtils.GetCfgPath("mechjeb_settings_global.cfg"));
            // the input history goes with the file
            InputHistory      = null;
            _inputHistoryRead = true;
            InputHistoryLoads++;
            // and the shortcuts
            Shortcuts      = null;
            _shortcutsRead = true;
            ShortcutsLoads++;
            if (Vessel != null && Vessel.vesselName != null)
                File.Delete(MuUtils.GetTypeCfgPath(Vessel.vesselName));
            Core.ReloadAllComputerModules();
            GuiUtils.SetGUIScale(1);
        }

        public override string GetName() => Localizer.Format("#MechJeb_Settings_title"); //"Settings"

        public override string IconName() => "Settings";
    }
}
