using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleMenu : DisplayModule
    {
        public MechJebModuleMenu(MechJebCore core)
            : base(core)
        {
            priority = -1000;
            enabled = true;
            hidden = true;
            showInFlight = true;
            showInEditor = true;

            if (toolbarButtons == null)
                toolbarButtons = new Dictionary<DisplayModule, Button>();
        }

        public enum WindowStat
        {
            HIDDEN,
            MINIMIZED,
            NORMAL,
            OPENING,
            CLOSING
        }

        private struct Button
        {
            public IButton button;
            public String texturePath;
            public String texturePathActive;
        }

        [Persistent(pass = (int)Pass.Global)]
        public WindowStat windowStat = WindowStat.HIDDEN;

        [Persistent(pass = (int)Pass.Global)]
        public float windowProgr = 0;

        [Persistent(pass = (int)Pass.Global)]
        public float windowVPos = -185;

        public bool firstDraw = true;

        bool movingButton = false;

        [ToggleInfoItem("Hide Menu Button", InfoItem.Category.Misc), Persistent(pass = (int)Pass.Global)]
        public bool hideButton = false;

        [ToggleInfoItem("Use AppLauncher", InfoItem.Category.Misc), Persistent(pass = (int)Pass.Global)]
        public bool useAppLauncher = true;

        // If the Toolbar is loaded and we don't want to display the big menu button
        public bool HideMenuButton
        {
            get
            {
                return (ToolbarManager.ToolbarAvailable || useAppLauncher) && hideButton;
            }
        }

        private static Dictionary<DisplayModule, Button> toolbarButtons;
        const string Qmark = "MechJeb2/Icons/QMark";

        IButton menuButton;

        private static ApplicationLauncherButton mjButton;

        protected override void WindowGUI(int windowID)
        {
            if (HideMenuButton && GUI.Button(new Rect(2, 2, 16, 16), ""))
            {
                ShowHideWindow();
            }

            GUIStyle toggleInactive = new GUIStyle(GUI.skin.toggle);
            toggleInactive.normal.textColor = toggleInactive.onNormal.textColor = Color.white;

            GUIStyle toggleActive = new GUIStyle(toggleInactive);
            toggleActive.normal.textColor = toggleActive.onNormal.textColor = Color.green;

            GUILayout.BeginVertical();

            foreach (DisplayModule module in core.GetDisplayModules(DisplayOrder.instance))
            {
                if (!module.hidden && module.showInCurrentScene)
                {
                    module.enabled = GUILayout.Toggle(module.enabled, module.GetName(), module.isActive() ? toggleActive : toggleInactive);
                }
            }

            if (core.someModuleAreLocked)
                GUILayout.Label("Some module are disabled until you unlock the proper node in the R&D tree or upgrade the tracking station.");


            if (GUILayout.Button("Online Manual"))
            {
                Application.OpenURL("http://wiki.mechjeb.com/index.php?title=Manual");
            }

            GUILayout.EndVertical();
        }

        public void SetupAppLauncher()
        {
            if (!ApplicationLauncher.Ready)
                return;

            if (useAppLauncher && mjButton == null)
            {
                Texture2D mjButtonTexture = GameDatabase.Instance.GetTexture("MechJeb2/Icons/MJ2", false);

                mjButton = ApplicationLauncher.Instance.AddModApplication(
                    ShowHideMasterWindow, ShowHideMasterWindow,
                    null, null,
                    null, null,
                    ApplicationLauncher.AppScenes.ALWAYS,
                    mjButtonTexture);
            }

            if (!useAppLauncher && mjButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(mjButton);
                mjButton = null;
            }
        }

        public void ShowHideMasterWindow()
        {
            MechJebModuleMenu mod = FlightGlobals.ActiveVessel.GetMasterMechJeb().GetComputerModule<MechJebModuleMenu>();
            mod.ShowHideWindow();
        }

        public void SetupToolBarButtons()
        {
            if (!ToolbarManager.ToolbarAvailable)
                return;

            SetupMainToolbarButton();

            foreach (DisplayModule module in core.GetDisplayModules(DisplayOrder.instance).Where(m => !m.hidden))
            {
                Button button;
                if (!toolbarButtons.ContainsKey(module))
                {

                    Debug.Log("Create button for module " + module.GetName());

                    String name = GetCleanName(module.GetName());

                    String TexturePath = "MechJeb2/Icons/" + name;
                    String TexturePathActive = TexturePath + "_active";

                    button = new Button();
                    button.button = ToolbarManager.Instance.add("MechJeb2", name);

                    if (GameDatabase.Instance.GetTexture(TexturePath, false) == null)
                    {
                        button.texturePath = Qmark;
                        print("No icon for " + name);
                    }
                    else
                    {
                        button.texturePath = TexturePath;
                    }

                    if (GameDatabase.Instance.GetTexture(TexturePathActive, false) == null)
                    {
                        button.texturePathActive = TexturePath;
                        //print("No icon for " + name + "_active");
                    }
                    else
                    {
                        button.texturePathActive = TexturePathActive;
                    }

                    toolbarButtons[module] = button;

                    button.button.ToolTip = "MechJeb " + module.GetName();
                    button.button.OnClick += (b) =>
                    {
                        DisplayModule mod = FlightGlobals.ActiveVessel.GetMasterMechJeb().GetDisplayModules(DisplayOrder.instance).FirstOrDefault(m => m == module);
                        if (mod != null)
                        {
                            mod.enabled = !mod.enabled;
                        }
                    };
                }
                else
                {
                    button = toolbarButtons[module];
                }

                button.button.Visible = module.showInCurrentScene;
                button.button.TexturePath = module.isActive() ? button.texturePathActive : button.texturePath;
            }
        }

        public void SetupMainToolbarButton()
        {
            if (!ToolbarManager.ToolbarAvailable)
                return;

            if (menuButton == null)
            {
                menuButton = ToolbarManager.Instance.add("MechJeb2", "MechJeb2MenuButton");
                menuButton.ToolTip = "MechJeb2";
                menuButton.TexturePath = "MechJeb2/Icons/MJ2";
                menuButton.OnClick += (b) => ShowHideMasterWindow();
            }
            menuButton.Visible = true;
        }


        private string GetCleanName(string name)
        {
            string regexSearch = " .:" + new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(name, "_");
        }


        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            ClearButtons();
            base.OnLoad(local, type, global);
        }

        // OnDestroy is actually run a bit too often when we have multiple MJ unit that get staged
        // But the edge case may get a bit too complex to handle properly so for now we 
        // recreate the buttons a bit often.
        public override void OnDestroy()
        {
            ClearButtons();
            base.OnDestroy();
        }

        private void ClearButtons()
        {
            if (mjButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(mjButton);
                mjButton = null;
            }

            if (ToolbarManager.ToolbarAvailable)
            {
                foreach (Button b in toolbarButtons.Values)
                {
                    if (b.button != null)
                    {
                        b.button.Destroy();
                    }
                }
                toolbarButtons.Clear();
                if (menuButton != null)
                {
                    menuButton.Destroy();
                }
            }
        }

        public override void DrawGUI(bool inEditor)
        {
            switch (windowStat)
            {
                case WindowStat.OPENING:
                    windowProgr += Time.deltaTime;
                    if (windowProgr >= 1)
                    {
                        windowProgr = 1;
                        windowStat = WindowStat.NORMAL;
                    }
                    break;
                case WindowStat.CLOSING:
                    windowProgr -= Time.deltaTime;
                    if (windowProgr <= 0)
                    {
                        windowProgr = 0;
                        windowStat = WindowStat.HIDDEN;
                    }
                    break;
            }

            GUI.depth = -100;
            GUI.SetNextControlName("MechJebOpen");
            Matrix4x4 previousGuiMatrix = GUI.matrix;
            GUI.matrix = GUI.matrix * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(new Vector3(0, 0, -90)), Vector3.one);
            if (!HideMenuButton && GUI.RepeatButton(new Rect(windowVPos, GuiUtils.scaledScreenWidth - 25 - (200 * windowProgr), 100, 25), (windowStat == WindowStat.HIDDEN) ? "/\\ MechJeb /\\" : "\\/ MechJeb \\/"))
            {
                if (Event.current.button == 0)
                {
                    ShowHideWindow();
                }
                else if (!movingButton && Event.current.button == 1)
                {
                    movingButton = true;
                }
            }
            GUI.matrix = previousGuiMatrix;

            GUI.depth = -99;

            if (windowStat != WindowStat.HIDDEN)
            {
                Rect pos = new Rect(GuiUtils.scaledScreenWidth - windowProgr * 200, Mathf.Clamp(-100 - windowVPos, 0, GuiUtils.scaledScreenHeight - windowPos.height), windowPos.width, windowPos.height);
                windowPos = GUILayout.Window(GetType().FullName.GetHashCode(), pos, WindowGUI, "MechJeb " + core.version, GUILayout.Width(200), GUILayout.Height(20));
            }
            else
            {
                windowPos = new Rect(GuiUtils.scaledScreenWidth, GuiUtils.scaledScreenHeight, 0, 0); // make it small so the mouse can't hoover it
            }

            GUI.depth = -98;

            if (firstDraw)
            {
                GUI.FocusControl("MechJebOpen");
                firstDraw = false;
            }
        }

        public void ShowHideWindow()
        {
            if (windowStat == WindowStat.HIDDEN)
            {
                windowStat = WindowStat.OPENING;
                windowProgr = 0;
                firstDraw = true;
            }
            else if (windowStat == WindowStat.NORMAL)
            {
                windowStat = WindowStat.CLOSING;
                windowProgr = 1;
            }
        }


        public void OnMenuUpdate()
        {
            if (movingButton)
            {
                if (Input.GetMouseButton(1))
                {
                    windowVPos = Mathf.Clamp(Input.mousePosition.y - Screen.height - 50, -Screen.height, -100) / GuiUtils.scale;
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    movingButton = false;
                }
            }

            if (HighLogic.LoadedSceneIsEditor || vessel.isActiveVessel)
            {
                SetupAppLauncher();
                SetupToolBarButtons();
            }
        }

        class DisplayOrder : IComparer<DisplayModule>
        {
            private DisplayOrder() { }
            public static DisplayOrder instance = new DisplayOrder();

            int IComparer<DisplayModule>.Compare(DisplayModule a, DisplayModule b)
            {
                bool customA = a is MechJebModuleCustomInfoWindow;
                bool customB = b is MechJebModuleCustomInfoWindow;
                if (!customA && customB) return -1;
                if (customA && !customB) return 1;
                return String.Compare(a.GetName(), b.GetName(), StringComparison.Ordinal);
            }
        }
    }
}
