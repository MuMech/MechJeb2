extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KSP.UI.Screens;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleMenu : DisplayModule
    {
        public MechJebModuleMenu(MechJebCore core)
            : base(core)
        {
            Priority = -1000;
            Enabled = true;
            ShowInFlight = true;
            ShowInEditor = true;

            if (toolbarButtons == null)
                toolbarButtons = new Dictionary<string, Button>();

            if (featureButtons == null)
                featureButtons = new Dictionary<string, Button>();
        }

        public override bool ShowInMenu => false;

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

        private class Button
        {
            public IButton button;
            public string texturePath;
            public string texturePathActive;
            public Func<bool> isActive; // feature buttons only

            private bool? _visible;
            private string _texture;

            // Every IButton property write is a reflection call that allocates: only write what changed
            public void SetVisible(bool visible)
            {
                if (_visible == visible)
                    return;

                _visible = visible;
                button.Visible = visible;
            }

            public void SetTexture(string path)
            {
                if (_texture == path)
                    return;

                _texture = path;
                button.TexturePath = path;
            }
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public WindowStat windowStat = WindowStat.HIDDEN;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public WindowSide windowSide = WindowSide.TOP;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public float windowProgr;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public float windowVPos = -185;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public float windowHPos = 250;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int columns = 2;

        public Rect displayedPos
        {
            get
            {
                switch (windowSide)
                {
                    case WindowSide.LEFT:
                        return new Rect((windowProgr - 1) * WindowPos.width,
                            Mathf.Clamp(-100 - windowVPos, 0, GuiUtils.ScaledScreenHeight - WindowPos.height), WindowPos.width, WindowPos.height);
                    case WindowSide.RIGHT:
                        return new Rect(GuiUtils.ScaledScreenWidth - windowProgr * WindowPos.width,
                            Mathf.Clamp(-100 - windowVPos, 0, GuiUtils.ScaledScreenHeight - WindowPos.height), WindowPos.width, WindowPos.height);
                    case WindowSide.TOP:
                        return new Rect(
                            Mathf.Clamp(GuiUtils.ScaledScreenWidth - 50 - WindowPos.width * 0.5f - windowHPos, 0,
                                GuiUtils.ScaledScreenWidth - WindowPos.width), (windowProgr - 1) * WindowPos.height, WindowPos.width,
                            WindowPos.height);
                    case WindowSide.BOTTOM:
                    default:
                        return new Rect(
                            Mathf.Clamp(GuiUtils.ScaledScreenWidth - 50 - WindowPos.width * 0.5f - windowHPos, 0,
                                GuiUtils.ScaledScreenWidth - WindowPos.width), GuiUtils.ScaledScreenHeight - windowProgr * WindowPos.height,
                            WindowPos.width, WindowPos.height);
                }
            }
        }

        public Rect buttonPos
        {
            get
            {
                float w = buttonSize.x, h = buttonSize.y;
                switch (windowSide)
                {
                    case WindowSide.LEFT:
                        return new Rect(Mathf.Clamp(windowVPos + 50 - w * 0.5f, -GuiUtils.ScaledScreenHeight, -w), WindowPos.width * windowProgr, w, h);
                    case WindowSide.RIGHT:
                        return new Rect(Mathf.Clamp(windowVPos + 50 - w * 0.5f, -GuiUtils.ScaledScreenHeight, -w),
                            GuiUtils.ScaledScreenWidth - h - WindowPos.width * windowProgr, w, h);
                    case WindowSide.TOP:
                        return new Rect(Mathf.Clamp(GuiUtils.ScaledScreenWidth - windowHPos - 50 - w * 0.5f, 0, GuiUtils.ScaledScreenWidth - w * 0.5f),
                            windowProgr * WindowPos.height, w, h);
                    case WindowSide.BOTTOM:
                    default:
                        return new Rect(Mathf.Clamp(GuiUtils.ScaledScreenWidth - windowHPos - 50 - w * 0.5f, 0, GuiUtils.ScaledScreenWidth - w * 0.5f),
                            GuiUtils.ScaledScreenHeight - windowProgr * WindowPos.height - h, w, h);
                }
            }
        }

        internal bool movingButton;

        [ToggleInfoItem("#MechJeb_HideMenuButton", InfoItem.Category.Misc), Persistent(pass = (int)Pass.GLOBAL)]
        public readonly bool hideButton; //Hide Menu Button

        [ToggleInfoItem("#MechJeb_UseAppLauncher", InfoItem.Category.Misc), Persistent(pass = (int)Pass.GLOBAL)]
        public readonly bool useAppLauncher = true; //Use AppLauncher

        [GeneralInfoItem("#MechJeb_MenuPosition", InfoItem.Category.Misc)] //Menu Position
        private void MenuPosition() { }

        // shared by the IMGUI MenuPosition item and the new UI
        internal void CycleSide()
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

        // the new UI reports the size of its button too: KSP's UI scale grows it, and IMGUI units don't include that scale
        private Vector2 buttonSize = new Vector2(100, 25);

        internal void SetButtonSize(float width, float height) => buttonSize = new Vector2(width, height);

        // the new UI reports the size of its panel in IMGUI units, which displayedPos and buttonPos use
        internal void SetMeasuredSize(float width, float height)
        {
            Rect pos = WindowPos;
            if (pos.width != width || pos.height != height)
                WindowPos = new Rect(pos.x, pos.y, width, height);
        }

        // If the Toolbar is loaded and we don't want to display the big menu button
        public bool HideMenuButton => (ToolbarManager.ToolbarAvailable || useAppLauncher) && hideButton;

        // Keyed by the button id: the menu of the active vessel changes, the buttons do not
        private static Dictionary<string, Button> toolbarButtons;
        private static Dictionary<string, Button> featureButtons;
        private const string Qmark = "MechJeb2/Icons/QMark";

        private static IButton menuButton;

        private static ApplicationLauncherButton mjButton;

        public void SetupAppLauncher()
        {
            if (!ApplicationLauncher.Ready)
                return;

            if (useAppLauncher && mjButton == null)
            {
                Texture2D mjButtonTexture = GameDatabase.Instance.GetTexture("MechJeb2/Icons/MJ2", false);

                mjButton = ApplicationLauncher.Instance.AddModApplication(ShowHideMasterWindow, ShowHideMasterWindow, null, null, null, null,
                    ApplicationLauncher.AppScenes.ALWAYS, mjButtonTexture);
            }

            if (!useAppLauncher && mjButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(mjButton);
                mjButton = null;
            }
        }

        // The buttons outlive the menu that created them, so their handlers look the master MechJeb up when they run.
        // It is null when the active vessel has no running MechJeb, or outside the flight and editor scenes.
        private static MechJebCore ActiveMasterMechJeb()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return ((Vessel)null).GetMasterMechJeb();

            return HighLogic.LoadedSceneIsFlight ? FlightGlobals.ActiveVessel.GetMasterMechJeb() : null;
        }

        public void ShowHideMasterWindow()
        {
            MechJebCore mj = ActiveMasterMechJeb();
            if (mj == null)
                return;

            MechJebModuleMenu mod = mj.GetComputerModule<MechJebModuleMenu>();
            if (mod != null)
                mod.ShowHideWindow();
        }

        public void SetupToolBarButtons()
        {
            if (!ToolbarManager.ToolbarAvailable)
                return;

            SetupMainToolbarButton();

            foreach (DisplayModule module in Core.GetDisplayModules(DisplayOrder.instance).Where(m => m.ShowInMenu && !m.TechLocked))
            {
                string name = GetCleanName(module.IconName());

                if (!toolbarButtons.TryGetValue(name, out Button button))
                {
                    Debug.Log("Create button for module " + module.GetName());

                    string TexturePath = "MechJeb2/Icons/" + name;
                    string TexturePathActive = TexturePath + "_active";

                    button = new Button();
                    button.button = ToolbarManager.Instance.add("MechJeb2", name);

                    if (GameDatabase.Instance.GetTexture(TexturePath, false) == null)
                    {
                        button.texturePath = Qmark;
                        Print("No icon for " + name);
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

                    toolbarButtons[name] = button;

                    button.button.ToolTip = "MechJeb " + module.GetName();
                    button.button.OnClick += b =>
                    {
                        MechJebCore mj = ActiveMasterMechJeb();
                        if (mj == null)
                            return;

                        // the module that made the button belongs to the vessel that was active then
                        DisplayModule mod = mj.GetDisplayModules(DisplayOrder.instance)
                           .FirstOrDefault(m => m.ShowInMenu && GetCleanName(m.IconName()) == name);
                        if (mod != null)
                        {
                            mod.Enabled = !mod.Enabled;
                        }
                    };
                }

                button.SetVisible(module.ShowInCurrentScene);
                button.SetTexture(module.IsActive() ? button.texturePathActive : button.texturePath);
            }

            // create toolbar buttons for features
            if (featureButtons.Count == 0)
            {
                MechJebModuleManeuverPlanner maneuverPlannerModule = Core.GetComputerModule<MechJebModuleManeuverPlanner>();
                if (!HighLogic.LoadedSceneIsEditor && maneuverPlannerModule != null && !maneuverPlannerModule.TechLocked)
                {
                    // The handlers run for whichever vessel is active, not the one this menu belongs to
                    CreateFeatureButton(maneuverPlannerModule, "Exec_Node", "MechJeb Execute Next Node", b =>
                    {
                        MechJebCore mj = ActiveMasterMechJeb();
                        if (mj == null)
                            return;

                        if (mj.vessel.patchedConicSolver.maneuverNodes.Count > 0 && mj.Node != null)
                        {
                            if (mj.Node.Enabled)
                            {
                                mj.Node.Abort();
                            }
                            else
                            {
                                if (mj.vessel.patchedConicSolver.maneuverNodes[0].DeltaV.magnitude > 0.0001)
                                {
                                    mj.Node.ExecuteOneNode(mj.GetComputerModule<MechJebModuleManeuverPlanner>());
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
                    }, () =>
                    {
                        MechJebCore mj = ActiveMasterMechJeb();
                        return mj != null && mj.vessel.patchedConicSolver.maneuverNodes.Count > 0 && mj.Node != null && mj.Node.Enabled;
                    });

                    CreateFeatureButton(maneuverPlannerModule, "Autostage_Once", "MechJeb Autostage Once", b =>
                    {
                        MechJebCore mj = ActiveMasterMechJeb();
                        if (mj == null)
                            return;

                        MechJebModuleThrustWindow w = mj.GetComputerModule<MechJebModuleThrustWindow>();

                        if (mj.Staging.Enabled && mj.Staging.AutostagingOnce)
                        {
                            if (mj.Staging.Users.Contains(w))
                            {
                                mj.Staging.Users.Remove(w);
                                w.autostageSavedState = false;
                            }
                        }
                        else
                        {
                            mj.Staging.AutostageOnce(w);
                        }
                    }, () =>
                    {
                        MechJebCore mj = ActiveMasterMechJeb();
                        return mj != null && mj.Staging.Enabled && mj.Staging.AutostagingOnce;
                    });

                    CreateFeatureButton(maneuverPlannerModule, "Auto_Warp", "MechJeb Auto-warp", b =>
                    {
                        MechJebCore mj = ActiveMasterMechJeb();
                        if (mj != null)
                            mj.Node.Autowarp = !mj.Node.Autowarp;
                    }, () =>
                    {
                        MechJebCore mj = ActiveMasterMechJeb();
                        return mj != null && mj.Node.Autowarp;
                    });
                }
            }
        }

        /// <summary>
        ///     Creates feature button and puts it to <see cref="featureButtons" /> list.
        /// </summary>
        /// <param name="module">Feature related module.</param>
        /// <param name="nameId">Feature unique name.</param>
        /// <param name="tooltip">The tooltip for button.</param>
        /// <param name="onClick">The button click handler.</param>
        /// <param name="isActive">Super light weight function to check whether the feature is activated at the moment.</param>
        public void CreateFeatureButton(DisplayModule module, string nameId, string tooltip, ClickHandler onClick, Func<bool> isActive)
        {
            string texturePath = "MechJeb2/Icons/" + nameId;
            string texturePathActive = texturePath + "_active";

            var button = new Button();
            button.button = ToolbarManager.Instance.add("MechJeb2", nameId);

            if (GameDatabase.Instance.GetTexture(texturePath, false) == null)
            {
                button.texturePath = Qmark;
                Print("No icon for " + nameId);
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
            button.isActive = isActive;
            featureButtons[nameId] = button;

            button.SetVisible(module.ShowInCurrentScene);
            button.SetTexture(button.texturePath);
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
                menuButton.OnClick += b => ShowHideMasterWindow();
            }

            menuButton.Visible = true;
        }

        private static readonly Dictionary<string, string> _cleanNames = new Dictionary<string, string>();
        private static Regex _invalidChars;

        // Cached: it runs for every module on every update
        private static string GetCleanName(string name)
        {
            if (_cleanNames.TryGetValue(name, out string clean))
                return clean;

            if (_invalidChars == null)
            {
                string regexSearch = " .:" + new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                _invalidChars = new Regex($"[{Regex.Escape(regexSearch)}]");
            }

            return _cleanNames[name] = _invalidChars.Replace(name, "_");
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            if (!HighLogic.LoadedSceneIsGame)
                return;
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
                    menuButton = null;
                }
            }
        }

        // the slide animation; shared by the IMGUI DrawGUI and the new UI
        internal void AdvanceAnimation()
        {
            float time = Core.Settings.MenuAnimationTime;
            float step = time > 0 ? Time.deltaTime / time : 1;
            switch (windowStat)
            {
                case WindowStat.OPENING:
                    windowProgr += step;
                    if (windowProgr >= 1)
                    {
                        windowProgr = 1;
                        windowStat = WindowStat.NORMAL;
                    }

                    break;
                case WindowStat.CLOSING:
                    windowProgr -= step;
                    if (windowProgr <= 0)
                    {
                        windowProgr = 0;
                        windowStat = WindowStat.HIDDEN;
                    }

                    break;
            }
        }

        public void ShowHideWindow()
        {
            if (windowStat == WindowStat.HIDDEN)
            {
                windowStat = WindowStat.OPENING;
                windowProgr = 0;
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
                        windowVPos = Mathf.Clamp(Input.mousePosition.y - Screen.height - 50, -Screen.width, 0) / GuiUtils.Scale;
                    }
                    else
                    {
                        windowHPos = Mathf.Clamp(Screen.width - Input.mousePosition.x - 50, 0, Screen.width) / GuiUtils.Scale;
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    movingButton = false;
                }
            }

            if (HighLogic.LoadedSceneIsEditor || (Vessel != null && Vessel.isActiveVessel))
            {
                SetupAppLauncher();
                SetupToolBarButtons();
            }

            // update feature buttons
            foreach (Button b in featureButtons.Values)
            {
                b.SetTexture(b.isActive() ? b.texturePathActive : b.texturePath);
            }
        }

        public class DisplayOrder : IComparer<DisplayModule>
        {
            private DisplayOrder()
            {
            }

            public static readonly DisplayOrder instance = new DisplayOrder();

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
