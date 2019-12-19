using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KSP.UI.Screens;
using UnityEngine;
using KSP.Localization;

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
            ShowInFlight = true;
            ShowInEditor = true;

            if (toolbarButtons == null)
                toolbarButtons = new Dictionary<DisplayModule, Button>();

            if (featureButtons == null)
                featureButtons = new Dictionary<Action, Button>();
        }

        public enum WindowStat
        {
            HIDDEN,
            MINIMIZED,
            NORMAL,
            OPENING,
            CLOSING
        }

        public enum WindowSide
        {
            LEFT,
            RIGHT,
            TOP,
            BOTTOM
        }

        private struct Button
        {
            public IButton button;
            public string texturePath;
            public string texturePathActive;
        }

        [Persistent(pass = (int)Pass.Global)]
        public WindowStat windowStat = WindowStat.HIDDEN;

        [Persistent(pass = (int)Pass.Global)]
        public WindowSide windowSide = WindowSide.TOP;

        [Persistent(pass = (int)Pass.Global)]
        public float windowProgr = 0;

        [Persistent(pass = (int)Pass.Global)]
        public float windowVPos = -185;

        [Persistent(pass = (int)Pass.Global)]
        public float windowHPos = 250;

        [Persistent(pass = (int)Pass.Global)]
        public int columns = 2;

        const int colWidth = 200;
        
        public Rect displayedPos
        {
            get
            {
                switch (windowSide)
                {
                    case WindowSide.LEFT:
                        return new Rect((windowProgr - 1) * windowPos.width, Mathf.Clamp(-100 - windowVPos, 0, GuiUtils.scaledScreenHeight - windowPos.height), windowPos.width, windowPos.height);
                    case WindowSide.RIGHT:
                        return new Rect(GuiUtils.scaledScreenWidth - windowProgr * windowPos.width, Mathf.Clamp(-100 - windowVPos, 0, GuiUtils.scaledScreenHeight - windowPos.height), windowPos.width, windowPos.height);
                    case WindowSide.TOP:
                        return new Rect(Mathf.Clamp(GuiUtils.scaledScreenWidth - 50 - windowPos.width * 0.5f - windowHPos, 0, GuiUtils.scaledScreenWidth - windowPos.width), (windowProgr - 1) * windowPos.height, windowPos.width, windowPos.height);
                    case WindowSide.BOTTOM:
                    default:
                        return new Rect(Mathf.Clamp(GuiUtils.scaledScreenWidth - 50 - windowPos.width * 0.5f - windowHPos, 0, GuiUtils.scaledScreenWidth - windowPos.width), GuiUtils.scaledScreenHeight - windowProgr * windowPos.height, windowPos.width, windowPos.height);
                }
            }
        }
        
        public Rect buttonPos
        {
            get
            {
                switch (windowSide)
                {
                    case WindowSide.LEFT:
                        return new Rect(Mathf.Clamp(windowVPos, - GuiUtils.scaledScreenHeight, -100), windowPos.width * windowProgr, 100, 25);
                    case WindowSide.RIGHT:
                        return new Rect(Mathf.Clamp(windowVPos, - GuiUtils.scaledScreenHeight, -100), GuiUtils.scaledScreenWidth - 25 - windowPos.width * windowProgr, 100, 25);
                    case WindowSide.TOP:
                        return new Rect(Mathf.Clamp(GuiUtils.scaledScreenWidth - windowHPos - 100, 0, GuiUtils.scaledScreenWidth - 50), windowProgr * windowPos.height, 100, 25);
                    case WindowSide.BOTTOM:
                    default:
                        return new Rect(Mathf.Clamp(GuiUtils.scaledScreenWidth - windowHPos - 100, 0, GuiUtils.scaledScreenWidth - 50), GuiUtils.scaledScreenHeight - windowProgr * windowPos.height - 25, 100, 25);
                }
            }
        }

        public bool firstDraw = true;

        bool movingButton = false;

        [ToggleInfoItem("#MechJeb_HideMenuButton", InfoItem.Category.Misc), Persistent(pass = (int) Pass.Global)] public bool hideButton = false;//Hide Menu Button

        [ToggleInfoItem("#MechJeb_UseAppLauncher", InfoItem.Category.Misc), Persistent(pass = (int) Pass.Global)] public bool useAppLauncher = true;//Use AppLauncher

        [GeneralInfoItem("#MechJeb_MenuPosition", InfoItem.Category.Misc)]//Menu Position
        void MenuPosition()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(windowSide.ToString()))
            {
                switch (windowSide)
                {
                    case WindowSide.LEFT:
                        windowSide = WindowSide.RIGHT;
                        break;
                    case WindowSide.RIGHT:
                        windowSide = WindowSide.TOP;
                        break;
                    case WindowSide.TOP:
                        windowSide = WindowSide.BOTTOM;
                        break;
                    case WindowSide.BOTTOM:
                        windowSide = WindowSide.LEFT;
                        break;
                    default:
                        windowSide = WindowSide.RIGHT;
                        break;
                }
            }

            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                columns--;
            }

            GUILayout.Label(columns.ToString(), GUILayout.ExpandWidth(false));

            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                columns++;
            }

            GUILayout.EndHorizontal();

            if (columns < 1)
                columns = 1;
        }

        // If the Toolbar is loaded and we don't want to display the big menu button
        public bool HideMenuButton
        {
            get { return (ToolbarManager.ToolbarAvailable || useAppLauncher) && hideButton; }
        }

        private static Dictionary<DisplayModule, Button> toolbarButtons;
        private static Dictionary<Action, Button> featureButtons;
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

            List<DisplayModule> displayModules = core.GetDisplayModules(DisplayOrder.instance);
            int i = 0;
            int step = Mathf.CeilToInt((float)(displayModules.Count(d => !d.hidden && d.showInCurrentScene) + 1 ) / columns);
            
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            foreach (DisplayModule module in displayModules)
            {
                if (!module.hidden && module.showInCurrentScene)
                {
                    if (i == step)
                    {
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                        i = 0;
                    }
                    module.enabled = GUILayout.Toggle(module.enabled, module.GetName(), module.isActive() ? toggleActive : toggleInactive);
                    i++;
                }
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_OnlineManualbutton")))//"Online Manual"
            {
                Application.OpenURL("https://github.com/MuMech/MechJeb2/wiki");
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (core.someModuleAreLocked)
                GUILayout.Label(Localizer.Format("#MechJeb_ModuleAreLocked"));//"Some modules are disabled until you unlock the proper node in the R&D tree or upgrade the tracking station."
        }

        public void SetupAppLauncher()
        {
            if (!ApplicationLauncher.Ready)
                return;

            if (useAppLauncher && mjButton == null)
            {
                Texture2D mjButtonTexture = GameDatabase.Instance.GetTexture("MechJeb2/Icons/MJ2", false);

                mjButton = ApplicationLauncher.Instance.AddModApplication(ShowHideMasterWindow, ShowHideMasterWindow, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, mjButtonTexture);
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

            // create toolbar buttons for features
            if (featureButtons.Count == 0)
            {
                var maneuverPlannerModule = core.GetComputerModule<MechJebModuleManeuverPlanner>();
                if (!HighLogic.LoadedSceneIsEditor && maneuverPlannerModule != null && !maneuverPlannerModule.hidden)
                {
                    CreateFeatureButton(maneuverPlannerModule, "Exec_Node", "MechJeb Execute Next Node", (b) =>
                    {
                        if (vessel.patchedConicSolver.maneuverNodes.Count > 0 && core.node != null)
                        {
                            if (core.node.enabled)
                            {
                                core.node.Abort();
                            }
                            else
                            {
                                if (vessel.patchedConicSolver.maneuverNodes[0].DeltaV.magnitude > 0.0001)
                                {
                                    core.node.ExecuteOneNode(maneuverPlannerModule);
                                }
                                else
                                {
                                    ScreenMessages.PostScreenMessage("Maneuver burn vector not set", 3f);
                                }
                            }
                        }
                        else
                        {
                            ScreenMessages.PostScreenMessage("No maneuver nodes", 2f);
                        }
                    }, () => vessel.patchedConicSolver.maneuverNodes.Count > 0 && core.node != null && core.node.enabled);

                    CreateFeatureButton(maneuverPlannerModule, "Autostage_Once", "MechJeb Autostage Once", (b) =>
                    {
                        var w = core.GetComputerModule<MechJebModuleThrustWindow>();

                        if (core.staging.enabled && core.staging.autostagingOnce)
                        {
                            if (core.staging.users.Contains(w))
                            {
                                core.staging.users.Remove(w);
                                w.autostageSavedState = false;
                            }
                        }
                        else
                        {
                            core.staging.AutostageOnce(w);
                        }
                    }, () => core.staging.enabled && core.staging.autostagingOnce);

                    CreateFeatureButton(maneuverPlannerModule, "Auto_Warp", "MechJeb Auto-warp", (b) =>
                    {
                        core.node.autowarp = !core.node.autowarp;
                    }, () => core.node.autowarp);
                }
            }
        }

        /// <summary>
        /// Creates feature button and puts it to <see cref="featureButtons"/> list.
        /// </summary>
        /// <param name="module">Feature related module.</param>
        /// <param name="nameId">Feature unique name.</param>
        /// <param name="tooltip">The tooltip for button.</param>
        /// <param name="onClick">The button click handler.</param>
        /// <param name="isActive">Super light weight function to check whether the feature is activated at the moment.</param>
        public void CreateFeatureButton(DisplayModule module, string nameId, string tooltip, ClickHandler onClick, Func<bool> isActive)
        {
            var texturePath = "MechJeb2/Icons/" + nameId;
            var texturePathActive = texturePath + "_active";

            var button = new Button();
            button.button = ToolbarManager.Instance.add("MechJeb2", nameId);

            if (GameDatabase.Instance.GetTexture(texturePath, false) == null)
            {
                button.texturePath = Qmark;
                print("No icon for " + nameId);
            }
            else
            {
                button.texturePath = texturePath;
            }

            if (GameDatabase.Instance.GetTexture(texturePathActive, false) == null)
            {
                button.texturePathActive = texturePath;
            }
            else
            {
                button.texturePathActive = texturePathActive;
            }

            button.button.ToolTip = tooltip;
            button.button.OnClick += onClick;
            featureButtons.Add(() =>
            {
                button.button.TexturePath = isActive()
                    ? button.texturePathActive
                    : button.texturePath;
            }, button);

            button.button.Visible = module.showInCurrentScene;
            button.button.TexturePath = button.texturePath;
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
            if (mjButton != null && mjButton.gameObject != null)
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

                foreach (Button b in featureButtons.Values)
                {
                    if (b.button != null)
                    {
                        b.button.Destroy();
                    }
                }
                featureButtons.Clear();

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
            if (windowSide == WindowSide.RIGHT || windowSide == WindowSide.LEFT)
                GUI.matrix = GUI.matrix*Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(new Vector3(0, 0, -90)), Vector3.one);
            
            if (!HideMenuButton && GUI.RepeatButton(buttonPos, (windowStat == WindowStat.HIDDEN) ^ (windowSide == WindowSide.TOP || windowSide == WindowSide.LEFT) ? "▲ MechJeb ▲" : "▼ MechJeb ▼"))
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
                windowPos = GUILayout.Window(GetType().FullName.GetHashCode(), displayedPos, WindowGUI, "MechJeb " + core.version, GUILayout.Width(colWidth), GUILayout.Height(20));
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
                    if (windowSide == WindowSide.RIGHT || windowSide == WindowSide.LEFT)
                    {
                        windowVPos = Mathf.Clamp(Input.mousePosition.y - Screen.height - 50, -Screen.width, 0)/GuiUtils.scale;
                        
                    }
                    else
                    {
                        windowHPos = Mathf.Clamp(Screen.width - Input.mousePosition.x - 50, 0 , Screen.width) / GuiUtils.scale;
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    movingButton = false;
                }
            }

            if (HighLogic.LoadedSceneIsEditor || (vessel != null && vessel.isActiveVessel))
            {
                SetupAppLauncher();
                SetupToolBarButtons();
            }

            // update feature buttons
            foreach (var b in featureButtons)
            {
                b.Key();
            }
        }

        public class DisplayOrder : IComparer<DisplayModule>
        {
            private DisplayOrder()
            {
            }

            public static DisplayOrder instance = new DisplayOrder();

            int IComparer<DisplayModule>.Compare(DisplayModule a, DisplayModule b)
            {
                bool customA = a is MechJebModuleCustomInfoWindow;
                bool customB = b is MechJebModuleCustomInfoWindow;
                if (!customA && customB) return -1;
                if (customA && !customB) return 1;
                return string.Compare(a.GetName(), b.GetName(), StringComparison.Ordinal);
            }
        }
    }
}
