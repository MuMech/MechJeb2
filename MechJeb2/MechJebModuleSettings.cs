using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSettings : DisplayModule
    {
        public MechJebModuleSettings(MechJebCore core) : base(core) 
        { 
            showInEditor = true;
            showInFlight = true;
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
        
        [ToggleInfoItem("Hide 'Brake on Eject' in Rover Controller", InfoItem.Category.Misc), Persistent(pass = (int)Pass.Global)]
        public bool hideBrakeOnEject = false;

        [ToggleInfoItem("Use only the titlebar for window dragging", InfoItem.Category.Misc), Persistent(pass = (int)Pass.Global)]
        public bool useTitlebarDragging = false;

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

            if (GUILayout.Button("\nRestore factory default settings\n"))
            {
                KSP.IO.FileInfo.CreateForType<MechJebCore>("mechjeb_settings_global.cfg").Delete();
                KSP.IO.FileInfo.CreateForType<MechJebCore>("mechjeb_settings_type_" + vessel.vesselName + ".cfg").Delete();
                core.ReloadAllComputerModules();
                GuiUtils.SetGUIScale(1);
            }

            GUILayout.Label("Current skin: " + (GuiUtils.SkinType)skinId );
            if (GuiUtils.skin == null || skinId != 1)
            {                
                if (GUILayout.Button("Use MechJeb 1 GUI skin"))
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.MechJeb1);
                    skinId = 1;
                }
            }
            if (GuiUtils.skin == null || skinId != 0)
            {
                if (GUILayout.Button("Use MechJeb 2 GUI skin"))
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.Default);
                    skinId = 0;
                }
            }
            if (GuiUtils.skin == null || skinId != 2)
            {
                if (GUILayout.Button("Use MJ2 Compact GUI skin"))
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.Compact);
                    skinId = 2;
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("UI Scale:", GUILayout.ExpandWidth(true));
            UIScale.text = GUILayout.TextField(UIScale.text, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GuiUtils.SetGUIScale(UIScale.val);

            dontUseDropDownMenu = GUILayout.Toggle(dontUseDropDownMenu, "Replace drop down menu with arrow selector");
            GuiUtils.dontUseDropDownMenu = dontUseDropDownMenu;

            MechJebModuleCustomWindowEditor ed = core.GetComputerModule<MechJebModuleCustomWindowEditor>();
            ed.registry.Find(i => i.id == "Toggle:Settings.hideBrakeOnEject").DrawItem();

            ed.registry.Find(i => i.id == "Toggle:Settings.useTitlebarDragging").DrawItem();
            
            ed.registry.Find(i => i.id == "Toggle:Menu.useAppLauncher").DrawItem();
            if (ToolbarManager.ToolbarAvailable || core.GetComputerModule<MechJebModuleMenu>().useAppLauncher)
                ed.registry.Find(i => i.id == "Toggle:Menu.hideButton").DrawItem();

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override string GetName()
        {
            return "Settings";
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(100) };
        }
    }
}
