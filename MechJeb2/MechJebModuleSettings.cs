using UnityEngine;

namespace MuMech
{
    class MechJebModuleSettings : DisplayModule
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

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

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
