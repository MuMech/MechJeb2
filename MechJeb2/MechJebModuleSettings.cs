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

        [Persistent(pass = (int)Pass.Global)]
        public bool useOldSkin = false;

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            if (useOldSkin) GuiUtils.LoadSkin(GuiUtils.SkinType.MechJeb1);
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

            if (GuiUtils.skin == null || GuiUtils.skin.name != "KSP window 2")
            {
                GUILayout.Label("Current skin: MechJeb 2");
                if (GUILayout.Button("Use MechJeb 1 GUI skin"))
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.MechJeb1);
                    useOldSkin = true;
                }
            }
            else
            {
                GUILayout.Label("Current skin: MechJeb 1");
                if (GUILayout.Button("Use MechJeb 2 GUI skin"))
                {
                    GuiUtils.LoadSkin(GuiUtils.SkinType.Default);
                    useOldSkin = false;
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
