using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                toolbarButtons = new Dictionary<string, IButton>();

            if (missingIcons == null)
                missingIcons = new HashSet<string>();
        }

        public enum WindowStat
        {
            HIDDEN,
            MINIMIZED,
            NORMAL,
            OPENING,
            CLOSING
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
        
        // If the Toolbar is loaded and we don't want to display the big menu button
        public bool HideMenuButton 
        {
            get
            {
                return ToolbarManager.ToolbarAvailable && hideButton;
            }
        }


        private static Dictionary<string, IButton> toolbarButtons;
        private static HashSet<string> missingIcons;

        IButton menuButton;

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

            foreach (DisplayModule module in core.GetComputerModules<DisplayModule>().OrderBy(m => m, DisplayOrder.instance))
            {
                if (!module.hidden && module.showInCurrentScene)
                {
                    module.enabled = GUILayout.Toggle(module.enabled, module.GetName(), module.isActive() ? toggleActive : toggleInactive);
                }
            }

            if (core.someModuleAreLocked)
                GUILayout.Label("Some module are disabled until you unlock the proper node in the R&D tree");


            if (GUILayout.Button("Online Manual"))
            {
                Application.OpenURL("http://wiki.mechjeb.com/index.php?title=Manual");
            }

            GUILayout.EndVertical();
        }
        
        public void SetupToolBarButtons()
        {
            SetupMainToolbarButton();
            foreach (DisplayModule module in core.GetComputerModules<DisplayModule>().OrderBy(m => m, DisplayOrder.instance))
            {
                if (!module.hidden && module.showInCurrentScene)
                {
                    SetupToolbarButton(module, module.isActive());
                }
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
                menuButton.OnClick += (b) =>
                {
                    MechJebModuleMenu mod = FlightGlobals.ActiveVessel.GetMasterMechJeb().GetComputerModule<MechJebModuleMenu>();
                    mod.ShowHideWindow();
                    //print("Change MechJebModuleMenu ");                
                };
            }
            menuButton.Visible = true;
        }
        
        public void SetupToolbarButton(DisplayModule module, bool active)
        {
            if (!ToolbarManager.ToolbarAvailable)
                return;

            if (!module.hidden)
            {
                IButton button;
                String name = CleanName(module.GetName());
                if (!toolbarButtons.ContainsKey(name))
                {
                    print("Adding button for " + name);
                    button = ToolbarManager.Instance.add("MechJeb2", name);
                    toolbarButtons[name] = button;
                    button.ToolTip = "MechJeb " + module.GetName();                    
                    //button.Visibility = new MJButtonVisibility(this);
                    button.OnClick += (b) =>
                    {
                        DisplayModule mod = FlightGlobals.ActiveVessel.GetMasterMechJeb().GetComputerModules<DisplayModule>().FirstOrDefault(m => m.GetName() == module.GetName());
                        if (mod != null)
                        {
                            mod.enabled = !mod.enabled;
                            //print("Change " + module.GetName() + " to " + module.enabled);
                        }
                    };
                }
                else
                {
                    button = toolbarButtons[name];
                    //if (button.Visible != module.showInCurrentScene)
                }
                button.Visible = module.showInCurrentScene;
                String TexturePath = "MechJeb2/Icons/" + name;
                String TexturePathActive = TexturePath + "_active";
                if (GameDatabase.Instance.GetTexture(TexturePath, false) == null)
                {
                    TexturePath = "MechJeb2/Icons/QMark";
                    if (!missingIcons.Contains(name))
                    {
                        missingIcons.Add(name);
                        print("No icon for " + name);
                    }
                }
                if (active & GameDatabase.Instance.GetTexture(TexturePathActive, false) == null)
                {
                    TexturePathActive = TexturePath;
                    if (!missingIcons.Contains(name + "_active"))
                    {
                        missingIcons.Add(name + "_active");
                        print("No icon for " + name + "_active");
                    }
                }
                button.TexturePath = active ? TexturePathActive : TexturePath;
            }
        }

        public override void OnDestroy()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                foreach (Button b in toolbarButtons.Values)
                {
                    b.Destroy();
                }
                toolbarButtons = new Dictionary<string, IButton>();
                menuButton.Destroy();
            }

            base.OnDestroy();
        }

        public static string CleanName(String name)
        {
            return name.Replace('.', '_').Replace(' ', '_').Replace(':', '_').Replace('/', '_');
        }
        
        public override void DrawGUI(bool inEditor)
        {
            SetupToolBarButtons();

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
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(new Vector3(0, 0, -90)), Vector3.one);
            if (!HideMenuButton && GUI.RepeatButton(new Rect(windowVPos, Screen.width - 25 - (200 * windowProgr), 100, 25), (windowStat == WindowStat.HIDDEN) ? "/\\ MechJeb /\\" : "\\/ MechJeb \\/"))
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
            GUI.matrix = Matrix4x4.identity;

            GUI.depth = -99;

            if (windowStat != WindowStat.HIDDEN)
            {
                Rect pos = new Rect(Screen.width - windowProgr * 200, Mathf.Clamp(-100 - windowVPos, 0, Screen.height - windowPos.height), windowPos.width, windowPos.height );
                windowPos = GUILayout.Window(GetType().FullName.GetHashCode(), pos, WindowGUI, "MechJeb " + core.version, GUILayout.Width(200), GUILayout.Height(20));
            }
            else
            {
                windowPos = new Rect(Screen.width, Screen.height, 0, 0); // make it small so the mouse can't hoover it
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


        // The button won't move in the editor since OnUpdate is never called...
        public override void OnUpdate()
        {
            if (movingButton)
                if (Input.GetMouseButton(1))
                {
                    windowVPos = Mathf.Clamp(Input.mousePosition.y - Screen.height - 50, - Screen.height, -100);
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    movingButton = false;
                }
        }

        class DisplayOrder : IComparer<DisplayModule>
        {
            private DisplayOrder() { }
            public static DisplayOrder instance = new DisplayOrder();

            int IComparer<DisplayModule>.Compare(DisplayModule a, DisplayModule b)
            {
                if (a is MechJebModuleCustomInfoWindow && b is MechJebModuleCustomInfoWindow) return a.GetName().CompareTo(b.GetName());
                if (a is MechJebModuleCustomInfoWindow) return 1;
                if (b is MechJebModuleCustomInfoWindow) return -1;
                return a.GetName().CompareTo(b.GetName());
            }
        }
    }
}
