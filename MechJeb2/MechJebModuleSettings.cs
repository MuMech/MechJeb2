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

        // the new UI's look, set from its own Settings window; IMGUI ignores them
        public enum UiSkinType { Classic, Flat, Glass, ZTheme }

        /// <summary>New UI skin: Classic (stock sprites, tinted button roles), Flat (dark, flat), Glass (translucent HUD), ZTheme (the ZTheme mod's colours).</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public int UiSkin = (int)UiSkinType.Flat;

        /// <summary>New UI: tighter margins and rows, in every skin. A custom window's own Compact setting overrides it.</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UiCompact = true;

        /// <summary>New UI: the modern layout (steppers, switches, segmented choices...) instead of IMGUI's layout.</summary>
        // a new key: configs from before it was the default say False, which nobody chose
        [Persistent(pass = (int)Pass.GLOBAL, name = "UiModernLayoutChoice")]
        public bool UiModernLayout = true;

        /// <summary>New UI, modern layout: the section headers it adds to long windows.</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UiSectionHeaders = true;

        /// <summary>Raised after skin, scale or menu settings change, so another UI can follow them.</summary>
        public static event Action<MechJebModuleSettings> Changed;

        /// <summary>Push the settings to GuiUtils and notify listeners (used by kOS and by this window).</summary>
        public void ApplySettings(bool loadSkin = false)
        {
            GuiUtils.SetGUIScale(UIScale.Val);
            if (loadSkin) GuiUtils.LoadSkin((GuiUtils.SkinType)SkinId);
            GuiUtils.DontUseDropDownMenu = DontUseDropDownMenu;
            GuiUtils.ShowAdvancedWindowSettings = ShowAdvancedWindowSettings;
            Changed?.Invoke(this);
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);
            ApplySettings();
        }

        /// <summary>Delete the global and vessel-type settings and reload every module (this module included).</summary>
        public void RestoreFactoryDefaults()
        {
            File.Delete(MuUtils.GetCfgPath("mechjeb_settings_global.cfg"));
            if (Vessel != null && Vessel.vesselName != null)
                File.Delete(MuUtils.GetTypeCfgPath(Vessel.vesselName));
            Core.ReloadAllComputerModules();
            GuiUtils.SetGUIScale(1);
        }

        public override string GetName() => Localizer.Format("#MechJeb_Settings_title"); //"Settings"

        public override string IconName() => "Settings";
    }
}
