using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleSettings : DisplayModule
    {
        public MechJebModuleSettings(MechJebCore core) : base(core)
        {
            ShowInEditor = true;
            ShowInFlight = true;
        }

        // Kept for old conf compatibility
        [Persistent(pass = (int)Pass.Global)]
        public bool useOldSkin = false;

        [Persistent(pass = (int)Pass.Global)]
        public int skinId = 0;

        [Persistent(pass = (int)(Pass.Global))]
        public EditableDouble UIScale = 1.0;

        [Persistent(pass = (int)Pass.Global)]
        public bool dontUseDropDownMenu = false;

        [ToggleInfoItem("#MechJeb_hideBrakeOnEject", InfoItem.Category.Misc), Persistent(pass = (int)Pass.Global)]//Hide 'Brake on Eject' in Rover Controller
        public bool hideBrakeOnEject = false;

        [ToggleInfoItem("#MechJeb_useTitlebarDragging", InfoItem.Category.Misc), Persistent(pass = (int)Pass.Global)]//Use only the titlebar for window dragging
        public bool useTitlebarDragging = false;

        [ToggleInfoItem("#MechJeb_rssMode", InfoItem.Category.Misc), Persistent(pass = (int)Pass.Global)]//Module disabling does not kill throttle (RSS/RO)
        public bool rssMode = false;

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            GuiUtils.SetGUIScale(UIScale.val);
            GuiUtils.dontUseDropDownMenu = dontUseDropDownMenu;

            if (useOldSkin)
            {
                skinId = 1;
                useOldSkin = false;
            }
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button(Localizer.Format("#MechJeb_Settings_button1")))//"\nRestore factory default settings\n"
            {
                KSP.IO.FileInfo.CreateForType<MechJebCore>("mechjeb_settings_global.cfg").Delete();
                if (vessel != null && vessel.vesselName != null)
                    KSP.IO.FileInfo.CreateForType<MechJebCore>("mechjeb_settings_type_" + vessel.vesselName + ".cfg").Delete();
                core.ReloadAllComputerModules();
                GuiUtils.SetGUIScale(1);
            }

            GUILayout.Label(Localizer.Format("#MechJeb_Settings_label1", (GuiUtils.SkinType)skinId));//"Current skin: <<1>>"
            if (GuiUtils.skin == null || skinId != 1)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Settings_button2")))//"Use MechJeb 1 GUI skin"
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.MechJeb1);
                    skinId = 1;
                }
            }
            if (GuiUtils.skin == null || skinId != 0)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Settings_button3")))//"Use MechJeb 2 GUI skin"
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.Default);
                    skinId = 0;
                }
            }
            if (GuiUtils.skin == null || skinId != 2)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Settings_button4")))//"Use MJ2 Compact GUI skin"
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.Compact);
                    skinId = 2;
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_Settings_label2"), GUILayout.ExpandWidth(true));//"UI Scale:"
            UIScale.text = GUILayout.TextField(UIScale.text, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GuiUtils.SetGUIScale(UIScale.val);

            dontUseDropDownMenu = GUILayout.Toggle(dontUseDropDownMenu, Localizer.Format("#MechJeb_Settings_checkbox1"));//"Replace drop down menu with arrow selector"
            GuiUtils.dontUseDropDownMenu = dontUseDropDownMenu;

            MechJebModuleCustomWindowEditor ed = core.GetComputerModule<MechJebModuleCustomWindowEditor>();
            ed.registry.Find(i => i.id == "Toggle:Settings.hideBrakeOnEject").DrawItem();

            ed.registry.Find(i => i.id == "Toggle:Settings.useTitlebarDragging").DrawItem();

            ed.registry.Find(i => i.id == "Toggle:Menu.useAppLauncher").DrawItem();
            if (ToolbarManager.ToolbarAvailable || core.GetComputerModule<MechJebModuleMenu>().useAppLauncher)
                ed.registry.Find(i => i.id == "Toggle:Menu.hideButton").DrawItem();

            ed.registry.Find(i => i.id == "General:Menu.MenuPosition").DrawItem();

            ed.registry.Find(i => i.id == "Toggle:Settings.rssMode").DrawItem();

            core.warp.activateSASOnWarp = GUILayout.Toggle(core.warp.activateSASOnWarp, Localizer.Format("#MechJeb_Settings_checkbox2"));//"Activate SAS on Warp"

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_Settings_title");//"Settings"
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(100) };
        }
    }
}
