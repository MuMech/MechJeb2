using KSP.IO;
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

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            GuiUtils.SetGUIScale(UIScale.Val);
            GuiUtils.DontUseDropDownMenu = DontUseDropDownMenu;
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button(Localizer.Format("#MechJeb_Settings_button1"))) //"\nRestore factory default settings\n"
            {
                FileInfo.CreateForType<MechJebCore>("mechjeb_settings_global.cfg").Delete();
                if (Vessel != null && Vessel.vesselName != null)
                    FileInfo.CreateForType<MechJebCore>("mechjeb_settings_type_" + Vessel.vesselName + ".cfg").Delete();
                Core.ReloadAllComputerModules();
                GuiUtils.SetGUIScale(1);
            }

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

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override string GetName() => Localizer.Format("#MechJeb_Settings_title"); //"Settings"

        public override string IconName() => "Settings";

        protected override GUILayoutOption[] WindowOptions() => new[] { GuiUtils.LayoutWidth(200), GUILayout.Height(100) };
    }
}
