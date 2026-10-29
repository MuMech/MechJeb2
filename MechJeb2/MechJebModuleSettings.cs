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

        public enum UiBackendType { Imgui, Kode }

        /// <summary>Which UI draws the windows a MechJebUiHost can draw. IMGUI draws everything else.</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public int UiBackend = (int)UiBackendType.Imgui;

        public UiBackendType Backend => (UiBackendType)UiBackend;

        /// <summary>Opacity of the new UI's window backgrounds (0.3 to 1). IMGUI windows ignore it.</summary>
        [Persistent(pass = (int)Pass.GLOBAL)]
        public float UiOpacity = 0.85f;

        /// <summary>Raised after skin, scale, backend or menu settings change, so another UI can follow them.</summary>
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

        protected override void WindowGUI(int windowID)
        {
            int oldSkin = SkinId, oldBackend = UiBackend;
            double oldScale = UIScale.Val;
            bool oldDropDown = DontUseDropDownMenu, oldAdvanced = ShowAdvancedWindowSettings;

            GUILayout.BeginVertical();

            if (GUILayout.Button(Localizer.Format("#MechJeb_Settings_button1"))) //"\nRestore factory default settings\n"
                RestoreFactoryDefaults();

            GUILayout.Label(Localizer.Format("#MechJeb_Settings_label1", (GuiUtils.SkinType)SkinId)); //"Current skin: <<1>>"
            if (GuiUtils.Skin == null || SkinId != 1)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Settings_button2"))) //"Use MechJeb 1 GUI skin"
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.MECH_JEB1);
                    SkinId = 1;
                }
            }

            if (GuiUtils.Skin == null || SkinId != 0)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Settings_button3"))) //"Use MechJeb 2 GUI skin"
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.DEFAULT);
                    SkinId = 0;
                }
            }

            if (GuiUtils.Skin == null || SkinId != 2)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Settings_button4"))) //"Use MJ2 Compact GUI skin"
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.COMPACT);
                    SkinId = 2;
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_Settings_label2"), GuiUtils.LayoutExpandWidth); //"UI Scale:"
            UIScale.Text = GUILayout.TextField(UIScale.Text, GuiUtils.LayoutWidth(60));
            GUILayout.EndHorizontal();

            GuiUtils.SetGUIScale(UIScale.Val);

            DontUseDropDownMenu =
                GUILayout.Toggle(DontUseDropDownMenu, Localizer.Format("#MechJeb_Settings_checkbox1")); //"Replace drop down menu with arrow selector"
            GuiUtils.DontUseDropDownMenu = DontUseDropDownMenu;

            ShowAdvancedWindowSettings = GUILayout.Toggle(ShowAdvancedWindowSettings, "Show Advanced Window Settings");
            GuiUtils.ShowAdvancedWindowSettings = ShowAdvancedWindowSettings;

            MechJebModuleCustomWindowEditor ed = Core.GetComputerModule<MechJebModuleCustomWindowEditor>();
            ed.registry.Find(i => i.id == "Toggle:Settings.HideBrakeOnEject").DrawItem();

            ed.registry.Find(i => i.id == "Toggle:Settings.UseTitlebarDragging").DrawItem();

            ed.registry.Find(i => i.id == "Toggle:Menu.useAppLauncher").DrawItem();
            if (ToolbarManager.ToolbarAvailable || Core.GetComputerModule<MechJebModuleMenu>().useAppLauncher)
                ed.registry.Find(i => i.id == "Toggle:Menu.hideButton").DrawItem();

            ed.registry.Find(i => i.id == "General:Menu.MenuPosition").DrawItem();

            ed.registry.Find(i => i.id == "Toggle:Settings.RssMode").DrawItem();

            Core.Warp.activateSASOnWarp =
                GUILayout.Toggle(Core.Warp.activateSASOnWarp, Localizer.Format("#MechJeb_Settings_checkbox2")); //"Activate SAS on Warp"

            // only offered when a second UI is installed
            if (MechJebUiHost.Current != null)
            {
                bool kode = GUILayout.Toggle(Backend == UiBackendType.Kode, "Use the new UI for ported windows (experimental)");
                UiBackend = (int)(kode ? UiBackendType.Kode : UiBackendType.Imgui);
                if (kode)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("New UI opacity", GuiUtils.LayoutNoExpandWidth);
                    float opacity = GUILayout.HorizontalSlider(UiOpacity, 0.3f, 1f, GuiUtils.LayoutExpandWidth);
                    GUILayout.EndHorizontal();
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (opacity != UiOpacity)
                    {
                        UiOpacity = opacity;
                        Dirty = true;
                        Changed?.Invoke(this);
                    }
                }
            }

            GUILayout.EndVertical();

            if (oldSkin != SkinId || oldBackend != UiBackend || oldScale != UIScale.Val || oldDropDown != DontUseDropDownMenu ||
                oldAdvanced != ShowAdvancedWindowSettings)
            {
                Dirty = true;
                Changed?.Invoke(this);
            }

            base.WindowGUI(windowID);
        }

        public override string GetName() => Localizer.Format("#MechJeb_Settings_title"); //"Settings"

        public override string IconName() => "Settings";

        protected override GUILayoutOption[] WindowOptions() => new[] { GuiUtils.LayoutWidth(200), GUILayout.Height(100) };
    }
}
