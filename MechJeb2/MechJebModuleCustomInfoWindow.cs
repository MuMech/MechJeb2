extern alias JetBrainsAnnotations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using UnityEngine.Profiling;
using static MechJebLib.Utils.Statics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MuMech
{
    public class MechJebModuleCustomInfoWindow : DisplayModule
    {
        [Persistent(pass = (int)Pass.GLOBAL)]
        public string title = Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_title"); //Custom Info Window

        [Persistent(collectionIndex = "InfoItem", pass = (int)Pass.GLOBAL)]
        public List<InfoItem> items = new List<InfoItem>();

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool isCompact;

        public bool IsCompact
        {
            get => isCompact;
            set
            {
                Dirty     = isCompact != value;
                isCompact = value;
            }
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public Color backgroundColor = new Color(0, 0, 0, 1);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public Color text = new Color(1, 1, 1, 1);

        public Texture2D background;

        private GUISkin localSkin;

        private TimeSpan refreshInterval = TimeSpan.FromSeconds(0.1);
        private DateTime lastRefresh     = DateTime.MinValue;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableInt refreshRate = 10;

        public void UpdateRefreshRate() => refreshInterval = TimeSpan.FromSeconds(1d / refreshRate);

        public override void OnDestroy()
        {
            if (background)
            {
                Object.Destroy(background);
            }

            base.OnDestroy();
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            //Do nothing: custom info windows will be saved in MechJebModuleCustomWindowEditor.OnSave
        }

        public virtual void UpdateWindowItems()
        {
            DateTime now = DateTime.Now;
            if (now - lastRefresh < refreshInterval) return;
            foreach (InfoItem item in items)
                if (HighLogic.LoadedSceneIsEditor ? item.showInEditor : item.showInFlight)
                    item.UpdateItem();
            lastRefresh = now;
        }

        private void RefreshRateGUI()
        {
            int oldRate = refreshRate;
            if (GuiUtils.ShowAdvancedWindowSettings)
                GuiUtils.SimpleTextBox("Update Interval", refreshRate, "Hz");
            if (oldRate != refreshRate)
            {
                refreshRate = Math.Max(refreshRate, 1);
                UpdateRefreshRate();
            }
        }

        protected override void WindowGUI(int windowID)
        {
            Profiler.BeginSample("MechJebModuleCustomInfoWindow.WindowGUI");

            GUI.skin         = isCompact ? GuiUtils.CompactSkin : GuiUtils.Skin;
            GUI.contentColor = text;

            // Only run the updater during the Layout pass, not the Repaint pass
            if (Event.current.type == EventType.Layout)
                UpdateWindowItems();

            GUILayout.BeginVertical();

            foreach (InfoItem item in items)
            {
                if (HighLogic.LoadedSceneIsEditor ? item.showInEditor : item.showInFlight)
                    item.DrawItem();
                else
                    GUILayout.Label(item.localizedName); //
            }

            if (items.Count == 0)
                GUILayout.Label(CachedLocalizer.Instance
                    .MechJebWindowEdCustomInfoWindowLabel1); //Add items to this window with the custom window editor.

            RefreshRateGUI();

            GUILayout.EndVertical();

            if (!IsOverlay && GUI.Button(new Rect(10, 0, 13, 20), "E", GuiUtils.YellowOnHover))
            {
                MechJebModuleCustomWindowEditor editor = Core.GetComputerModule<MechJebModuleCustomWindowEditor>();
                if (editor != null)
                {
                    editor.Enabled      = true;
                    editor.editedWindow = this;
                }
            }

            if (!IsOverlay && GUI.Button(new Rect(25, 0, 13, 20), "C", GuiUtils.YellowOnHover))
            {
                MuUtils.SystemClipboard = ToSharingString();
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Scrmsg1", GetName()), 3.0f,
                    ScreenMessageStyle.UPPER_RIGHT); //Configuration of <<1>> window copied to clipboard.
            }

            base.WindowGUI(windowID);

            Profiler.EndSample();
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(250), GUILayout.Height(30) };

        public override void DrawGUI(bool inEditor)
        {
            Init();
            if (IsOverlay)
            {
                GUI.skin = localSkin;
            }

            base.DrawGUI(inEditor);
            if (IsOverlay)
                GUI.skin = GuiUtils.Skin;
        }

        public void Init()
        {
            if (!background)
            {
                background = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                background.SetPixel(0, 0, backgroundColor);
                background.Apply();
            }

            if (IsOverlay && !localSkin)
            {
                localSkin                            = Object.Instantiate(GuiUtils.TransparentSkin);
                localSkin.window.normal.background   = background;
                localSkin.window.onNormal.background = background;
            }
        }

        public string ToSharingString()
        {
            string windowSharingString = "--- MechJeb Custom Window ---\n";
            windowSharingString += "Name: " + GetName() + "\n";
            windowSharingString += "Show in:" + (ShowInEditor ? " editor" : "") + (ShowInFlight ? " flight" : "") + "\n";
            for (int i = 0; i < items.Count; i++)
            {
                InfoItem item = items[i];
                windowSharingString += item.id + "\n";
            }

            windowSharingString += "-----------------------------\n";
            windowSharingString =  windowSharingString.Replace("\n", Environment.NewLine);
            return windowSharingString;
        }

        public void FromSharingString(string[] lines, List<InfoItem> registry)
        {
            if (lines.Length > 1 && lines[1].StartsWith("Name: ")) title = lines[1].Trim().Substring("Name: ".Length);
            if (lines.Length > 2 && lines[2].StartsWith("Show in:"))
            {
                ShowInEditor = lines[2].Contains("editor");
                ShowInFlight = lines[2].Contains("flight");
            }

            for (int i = 3; i < lines.Length; i++)
            {
                string id = lines[i].Trim();
                InfoItem match = registry.FirstOrDefault(item => item.id == id);
                if (match != null) items.Add(match);
            }
        }

        public override string GetName() => title;

        public override string IconName() => title;

        public MechJebModuleCustomInfoWindow(MechJebCore core) : base(core) { }
    }

    public class MechJebModuleCustomWindowEditor : DisplayModule
    {
        public readonly List<InfoItem>                registry = new List<InfoItem>();
        public          MechJebModuleCustomInfoWindow editedWindow;
        private         int                           selectedItemIndex = -1;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public InfoItem.Category itemCategory = InfoItem.Category.Orbit;

        private static readonly string[] categories = Enum.GetNames(typeof(InfoItem.Category));
        private                 int      presetIndex;

        private bool editingBackground;
        private bool editingText;

        private readonly Stopwatch _valueInfoItemStopwatch = new Stopwatch();
        private readonly Stopwatch _actionInfoItemStopwatch = new Stopwatch();
        private readonly Stopwatch _toggleInfoItemStopwatch = new Stopwatch();
        private readonly Stopwatch _generalInfoItemStopwatch = new Stopwatch();
        private readonly Stopwatch _editableInfoItemStopwatch = new Stopwatch();

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            Profiler.BeginSample("MechJebModuleCustomInfoEditor.OnLoad");

            base.OnLoad(local, type, global);

            registry.Clear();
            editedWindow = null;

            _valueInfoItemStopwatch.Reset();
            _actionInfoItemStopwatch.Reset();
            _toggleInfoItemStopwatch.Reset();
            _generalInfoItemStopwatch.Reset();
            _editableInfoItemStopwatch.Reset();

            var sw = new Stopwatch();
            sw.Start();

            RegisterInfoItems(VesselState);

            foreach (ComputerModule m in Core.GetComputerModules<ComputerModule>())
                RegisterInfoItems(m);

            sw.Stop();
            Print($"Registered {registry.Count} info items:  value:{_valueInfoItemStopwatch.ElapsedMilliseconds} ms action:{_actionInfoItemStopwatch.ElapsedMilliseconds} ms  toggle:{_toggleInfoItemStopwatch.ElapsedMilliseconds} ms  general:{_generalInfoItemStopwatch.ElapsedMilliseconds} ms  editable:{_editableInfoItemStopwatch.ElapsedMilliseconds} ms  total:{sw.ElapsedMilliseconds} ms");

            if (global == null) return;

            //Load custom info windows, which are stored in our ConfigNode:
            ConfigNode[] windowNodes = global.GetNodes(typeof(MechJebModuleCustomInfoWindow).Name);
            foreach (ConfigNode windowNode in windowNodes)
            {
                var window = new MechJebModuleCustomInfoWindow(Core);

                ConfigNode.LoadObjectFromConfig(window, windowNode);

                window.UpdateRefreshRate();

                bool useOldConfig = true;
                if (windowNode.HasValue("enabledEditor"))
                {
                    if (bool.TryParse(windowNode.GetValue("enabledEditor"), out bool loadedEnabled))
                    {
                        window.EnabledEditor = loadedEnabled;
                        useOldConfig         = false;
                        if (HighLogic.LoadedSceneIsEditor)
                            window.Enabled = loadedEnabled;
                    }
                }

                if (windowNode.HasValue("enabledFlight"))
                {
                    if (bool.TryParse(windowNode.GetValue("enabledFlight"), out bool loadedEnabled))
                    {
                        window.EnabledFlight = loadedEnabled;
                        useOldConfig         = false;
                        if (HighLogic.LoadedSceneIsFlight)
                            window.Enabled = loadedEnabled;
                    }
                }

                if (useOldConfig)
                {
                    if (windowNode.HasValue("enabled"))
                    {
                        if (bool.TryParse(windowNode.GetValue("enabled"), out bool loadedEnabled))
                        {
                            window.Enabled       = loadedEnabled;
                            window.EnabledEditor = window.Enabled;
                            window.EnabledFlight = window.Enabled;
                        }
                    }

                    window.EnabledEditor = window.Enabled;
                    window.EnabledFlight = window.Enabled;
                }

                window.items = new List<InfoItem>();

                if (windowNode.HasNode("items"))
                {
                    ConfigNode itemCollection = windowNode.GetNode("items");
                    ConfigNode[] itemNodes = itemCollection.GetNodes("InfoItem");
                    foreach (ConfigNode itemNode in itemNodes)
                    {
                        string id = itemNode.GetValue("id");
                        InfoItem match = registry.FirstOrDefault(item => item.id == id);
                        if (match != null) window.items.Add(match);
                    }
                }

                Core.AddComputerModuleLater(window);
            }

            Profiler.EndSample();
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            Profiler.BeginSample("MechJebModuleCustomInfoEditor.OnSave");

            base.OnSave(local, type, global);

            //Save custom info windows within our ConfigNode:
            if (global == null) return;

            foreach (MechJebModuleCustomInfoWindow window in Core.GetComputerModules<MechJebModuleCustomInfoWindow>())
            {
                string name = typeof(MechJebModuleCustomInfoWindow).Name;
                var windowNode = ConfigNode.CreateConfigFromObject(window, (int)Pass.GLOBAL, null);

                if (HighLogic.LoadedSceneIsEditor)
                    window.EnabledEditor = window.Enabled;
                if (HighLogic.LoadedSceneIsFlight)
                    window.EnabledFlight = window.Enabled;

                windowNode.AddValue("enabledFlight", window.EnabledFlight);
                windowNode.AddValue("enabledEditor", window.EnabledEditor);
                windowNode.CopyTo(global.AddNode(name));
                window.Dirty = false;
            }

            Profiler.EndSample();
        }

        public override void OnStart(PartModule.StartState state) => editedWindow = Core.GetComputerModule<MechJebModuleCustomInfoWindow>();

        private static readonly Dictionary<Type, List<Tuple<MemberInfo, Attribute>>> _cache = new Dictionary<Type, List<Tuple<MemberInfo, Attribute>>>();

        private void RegisterInfoItems(object obj)
        {
            Profiler.BeginSample("MechJebModuleCustomInfoEditor.RegisterInfoItems");

            Type objType = obj.GetType();

            if (!_cache.ContainsKey(objType))
            {
                _cache.Add(objType, new List<Tuple<MemberInfo, Attribute>>());

                foreach (MemberInfo member in objType.GetMembers(BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
                {
                    foreach (Attribute attribute in member.GetCustomAttributes(true))
                    {
                        switch (attribute)
                        {
                            case ValueInfoItemAttribute _:
                                _cache[objType].Add(new Tuple<MemberInfo, Attribute>(member, attribute));
                                break;
                            case ActionInfoItemAttribute _:
                                _cache[objType].Add(new Tuple<MemberInfo, Attribute>(member, attribute));
                                break;
                            case ToggleInfoItemAttribute _:
                                _cache[objType].Add(new Tuple<MemberInfo, Attribute>(member, attribute));
                                break;
                            case GeneralInfoItemAttribute _:
                                _cache[objType].Add(new Tuple<MemberInfo, Attribute>(member, attribute));
                                break;
                            case EditableInfoItemAttribute _:
                                _cache[objType].Add(new Tuple<MemberInfo, Attribute>(member, attribute));
                                break;
                        }
                    }
                }
            }

            foreach ((MemberInfo member, Attribute attribute) in _cache[objType])
            {
                    switch (attribute)
                    {
                        case ValueInfoItemAttribute item:
                            _valueInfoItemStopwatch.Start();
                            registry.Add(new ValueInfoItem(obj, member, item));
                            _valueInfoItemStopwatch.Stop();
                            break;
                        case ActionInfoItemAttribute item:
                            _actionInfoItemStopwatch.Start();
                            registry.Add(new ActionInfoItem(obj, (MethodInfo)member, item));
                            _actionInfoItemStopwatch.Stop();
                            break;
                        case ToggleInfoItemAttribute item:
                            _toggleInfoItemStopwatch.Start();
                            registry.Add(new ToggleInfoItem(obj, member, item));
                            _toggleInfoItemStopwatch.Stop();
                            break;
                        case GeneralInfoItemAttribute item:
                            _generalInfoItemStopwatch.Start();
                            registry.Add(new GeneralInfoItem(obj, (MethodInfo)member, item));
                            _generalInfoItemStopwatch.Stop();
                            break;
                        case EditableInfoItemAttribute item:
                            _editableInfoItemStopwatch.Start();
                            registry.Add(new EditableInfoItem(obj, member, item));
                            _editableInfoItemStopwatch.Stop();
                            break;
                    }
            }

            Profiler.EndSample();
        }

        private void AddNewWindow()
        {
            editedWindow = new MechJebModuleCustomInfoWindow(Core);
            if (HighLogic.LoadedSceneIsEditor) editedWindow.ShowInEditor = true;
            if (HighLogic.LoadedSceneIsFlight) editedWindow.ShowInFlight = true;
            Core.AddComputerModule(editedWindow);
            editedWindow.Enabled = true;
            editedWindow.Dirty   = true;
        }

        private void RemoveCurrentWindow()
        {
            if (editedWindow == null) return;

            Core.RemoveComputerModule(editedWindow);
            editedWindow = Core.GetComputerModule<MechJebModuleCustomInfoWindow>();
        }

        public override void DrawGUI(bool inEditor)
        {
            Profiler.BeginSample("MechJebModuleCustomWindowEditor.DrawGUI");

            base.DrawGUI(inEditor);

            if (editingBackground)
            {
                if (editedWindow != null)
                {
                    editedWindow.Init();

                    Color newColor = ColorPickerRGB.DrawGUI((int)WindowPos.xMax + 5, (int)WindowPos.yMin, editedWindow.backgroundColor);

                    if (editedWindow.backgroundColor != newColor)
                    {
                        editedWindow.backgroundColor = newColor;
                        editedWindow.background.SetPixel(0, 0, editedWindow.backgroundColor);
                        editedWindow.background.Apply();
                        editedWindow.Dirty = true;
                    }
                }
            }

            if (editingText)
            {
                if (editedWindow != null)
                {
                    Color newColor = ColorPickerRGB.DrawGUI((int)WindowPos.xMax + 5, (int)WindowPos.yMin, editedWindow.text);
                    if (editedWindow.text != newColor)
                    {
                        editedWindow.text  = newColor;
                        editedWindow.Dirty = true;
                    }
                }
            }

            Profiler.EndSample();
        }

        private Vector2 scrollPos, scrollPos2;

        protected override void WindowGUI(int windowID)
        {
            Profiler.BeginSample("MechJebModuleCustomWindowEditor.WindowGUI");

            GUILayout.BeginVertical();

            if (editedWindow == null) editedWindow = Core.GetComputerModule<MechJebModuleCustomInfoWindow>();

            if (editedWindow == null)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button1"))) AddNewWindow(); //New window
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button1"))) AddNewWindow();        //New window
                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button2"))) RemoveCurrentWindow(); //Delete window
                GUILayout.EndHorizontal();
            }

            if (editedWindow != null)
            {
                List<ComputerModule> allWindows = Core.GetComputerModules<MechJebModuleCustomInfoWindow>();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_Edtitle"), GUILayout.ExpandWidth(false)); //Title:
                int editedWindowIndex = allWindows.IndexOf(editedWindow);
                editedWindowIndex = GuiUtils.ArrowSelector(editedWindowIndex, allWindows.Count, () =>
                {
                    string newTitle = GUILayout.TextField(editedWindow.title, GUILayout.Width(120), GUILayout.ExpandWidth(false));

                    if (editedWindow.title != newTitle)
                    {
                        editedWindow.title = newTitle;
                        editedWindow.Dirty = true;
                    }
                });
                editedWindow = (MechJebModuleCustomInfoWindow)allWindows[editedWindowIndex];
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_label1")); //Show in:
                editedWindow.ShowInFlight =
                    GUILayout.Toggle(editedWindow.ShowInFlight, Localizer.Format("#MechJeb_WindowEd_checkbox1"), GUILayout.Width(60));    //Flight
                editedWindow.ShowInEditor = GUILayout.Toggle(editedWindow.ShowInEditor, Localizer.Format("#MechJeb_WindowEd_checkbox2")); //Editor
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                editedWindow.IsOverlay = GUILayout.Toggle(editedWindow.IsOverlay, Localizer.Format("#MechJeb_WindowEd_checkbox3")); //Overlay
                editedWindow.Locked    = GUILayout.Toggle(editedWindow.Locked, Localizer.Format("#MechJeb_WindowEd_checkbox4"));    //Locked
                editedWindow.IsCompact = GUILayout.Toggle(editedWindow.IsCompact, Localizer.Format("#MechJeb_WindowEd_checkbox5")); //Compact
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_label2")); //Color:
                bool previous = editingText;

                editingText = GUILayout.Toggle(editingText, Localizer.Format("#MechJeb_WindowEd_checkbox6")); //Text

                if (editingText && editingText != previous)
                    editingBackground = false;

                previous          = editingBackground;
                editingBackground = GUILayout.Toggle(editingBackground, Localizer.Format("#MechJeb_WindowEd_checkbox7")); //Background

                if (editingBackground && editingBackground != previous)
                    editingText = false;

                GUILayout.EndHorizontal();

                GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_label3")); //Window contents (click to edit):

                GUILayout.BeginVertical(GUILayout.Height(100));
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                for (int i = 0; i < editedWindow.items.Count; i++)
                {
                    GUIStyle s = i == selectedItemIndex ? GuiUtils.YellowLabel : GUI.skin.label;
                    if (GUILayout.Button(Localizer.Format(editedWindow.items[i].description), s)) selectedItemIndex = i; //
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                GUILayout.BeginHorizontal();

                if (!(selectedItemIndex >= 0 && selectedItemIndex < editedWindow.items.Count)) selectedItemIndex = -1;

                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button3")) && selectedItemIndex != -1)
                    editedWindow.items.RemoveAt(selectedItemIndex);                                             //Remove
                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button4")) && selectedItemIndex != -1) //"Move up"
                {
                    if (selectedItemIndex > 0)
                    {
                        InfoItem item = editedWindow.items[selectedItemIndex];
                        editedWindow.items.RemoveAt(selectedItemIndex);
                        editedWindow.items.Insert(selectedItemIndex - 1, item);
                        selectedItemIndex -= 1;
                    }
                }

                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button5")) && selectedItemIndex != -1) //Move down
                {
                    if (selectedItemIndex < editedWindow.items.Count)
                    {
                        InfoItem item = editedWindow.items[selectedItemIndex];
                        editedWindow.items.RemoveAt(selectedItemIndex);
                        editedWindow.items.Insert(selectedItemIndex + 1, item);
                        selectedItemIndex += 1;
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_label4")); //Click an item to add it to the info window:

                itemCategory = (InfoItem.Category)GuiUtils.ComboBox.Box((int)itemCategory, categories, this);

                scrollPos2 = GUILayout.BeginScrollView(scrollPos2);
                foreach (InfoItem item in registry.Where(it => it.category == itemCategory).OrderBy(it => it.description))
                {
                    if (GUILayout.Button(Localizer.Format(item.description), GuiUtils.YellowOnHover)) //
                    {
                        editedWindow.items.Add(item);
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_label5"), GuiUtils.MiddleCenterLabel); //Window presets:

            presetIndex = GuiUtils.ArrowSelector(presetIndex, CustomWindowPresets.presets.Length, () =>
            {
                if (GUILayout.Button(CustomWindowPresets.presets[presetIndex].name))
                {
                    MechJebModuleCustomInfoWindow newWindow = CreateWindowFromSharingString(CustomWindowPresets.presets[presetIndex].sharingString);
                    if (newWindow != null) editedWindow = newWindow;
                }
            });

            GUILayout.EndVertical();

            base.WindowGUI(windowID);

            Profiler.EndSample();
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(200), GUILayout.Height(540) };

        public override string GetName() => CachedLocalizer.Instance.MechJebWindowEdTitle; //Custom Window Editor

        public override string IconName() => "Custom Window Editor";

        public MechJebModuleCustomWindowEditor(MechJebCore core)
            : base(core)
        {
            ShowInFlight = true;
            ShowInEditor = true;
        }

        public void AddDefaultWindows()
        {
            Profiler.BeginSample("MechJebModuleCustomWindowEditor.AddDefaultWindows");

            CreateWindowFromSharingString(CustomWindowPresets.presets[0].sharingString).Enabled  = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[1].sharingString).Enabled  = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[2].sharingString).Enabled  = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[3].sharingString).Enabled  = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[4].sharingString).Enabled  = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[5].sharingString).Enabled  = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[6].sharingString).Enabled  = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[7].sharingString).Enabled  = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[10].sharingString).Enabled = false;

            Profiler.EndSample();
        }

        public MechJebModuleCustomInfoWindow CreateWindowFromSharingString(string sharingString)
        {
            string[] lines = sharingString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines[0] != "--- MechJeb Custom Window ---")
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Scrmsg2"), 3.0f,
                    ScreenMessageStyle.UPPER_RIGHT); //"Pasted text wasn't a MechJeb custom window descriptor."
                return null;
            }

            var window = new MechJebModuleCustomInfoWindow(Core);
            Core.AddComputerModule(window);
            window.Enabled = true;

            window.FromSharingString(lines, registry);

            return window;
        }
    }

    public class InfoItem
    {
        public readonly string name;
        public readonly string localizedName;
        public readonly string description;
        public readonly bool   showInEditor;
        public readonly bool   showInFlight;

        public enum Category
        {
            Orbit,
            Surface,
            Vessel,
            Target,
            Recorder,
            Thrust,
            Rover,
            Misc
        }

        public readonly Category category;

        [Persistent]
        public string id;

        public InfoItem() { }

        public InfoItem(InfoItemAttribute attribute)
        {
            name          = attribute.name;
            localizedName = Localizer.Format(name);
            category      = attribute.category;
            description   = attribute.description;
            showInEditor  = attribute.showInEditor;
            showInFlight  = attribute.showInFlight;
        }

        public virtual void DrawItem()   { }
        public virtual void UpdateItem() { }
    }

    //A ValueInfoItem is an info item that shows the value of some field, or the return value of some method.
    public class ValueInfoItem : InfoItem
    {
        private readonly string units;
        private readonly string format;
        public const     string SI       = "SI";
        public const     string TIME     = "TIME";
        public const     string ANGLE    = "ANGLE";
        public const     string ANGLE_NS = "ANGLE_NS";
        public const     string ANGLE_EW = "ANGLE_EW";
        private readonly int    siSigFigs;         //only used with the "SI" format
        private readonly int    siMaxPrecision;    //only used with the "SI" format
        private readonly int    timeDecimalPlaces; //only used with the "TIME" format

        private Func<object, object> getValue;
        private static readonly Dictionary<MemberInfo, Func<object, object>> _getterCache = new Dictionary<MemberInfo, Func<object, object>>();
        private readonly object _obj;

        private string stringValue;
        private int    cacheValidity = -1;
        public  bool   externalRefresh;

        public ValueInfoItem(object obj, MemberInfo member, ValueInfoItemAttribute attribute)
            : base(attribute)
        {
            Profiler.BeginSample("ValueInfoItem.ValueInfoItem");

            id = GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + member.Name;

            units             = attribute.units;
            format            = attribute.format;
            siSigFigs         = attribute.siSigFigs;
            siMaxPrecision    = attribute.siMaxPrecision;
            timeDecimalPlaces = attribute.timeDecimalPlaces;

            _obj = obj;

            if (!_getterCache.ContainsKey(member))
                _getterCache.Add(member, CompileAccessor(obj, member));

            getValue = _getterCache[member];

            Profiler.EndSample();
        }

        private Func<object, object> CompileAccessor(object obj, MemberInfo member)
        {
            Profiler.BeginSample("ValueInfoItem.CompileAccessor");

            Type objType = obj.GetType();
            var dynamicMethod = new DynamicMethod("GetMemberValue", typeof(object), new Type[] { typeof(object) }, objType, true);

            ILGenerator il = dynamicMethod.GetILGenerator();

            // Load the argument (object)
            il.Emit(OpCodes.Ldarg_0);
            // Cast it to the correct type
            il.Emit(OpCodes.Castclass, objType);

            switch (member)
            {
                case MethodInfo methodInfo:
                    il.Emit(OpCodes.Callvirt, methodInfo);
                    break;
                case PropertyInfo propertyInfo:
                    il.Emit(OpCodes.Callvirt, propertyInfo.GetGetMethod());
                    break;
                case FieldInfo fieldInfo:
                    il.Emit(OpCodes.Ldfld, fieldInfo);
                    break;
                default:
                    throw new ArgumentException("MemberInfo must be of type MethodInfo, PropertyInfo, or FieldInfo", nameof(member));
            }

            // Box the value if necessary
            if (member is PropertyInfo { PropertyType: { IsValueType: true } } ||
                member is FieldInfo { FieldType: { IsValueType: true } } ||
                member is MethodInfo { ReturnType: { IsValueType: true } })
            {
                il.Emit(OpCodes.Box, member switch
                {
                    PropertyInfo p => p.PropertyType,
                    FieldInfo f => f.FieldType,
                    _ => ((MethodInfo)member).ReturnType
                });
            }

            // Return the value
            il.Emit(OpCodes.Ret);

            var @delegate = (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));

            Profiler.EndSample();

            return @delegate;
        }

        private string GetStringValue(object value)
        {
            if (value == null) return "null";

            if (value is string) return $"{(string)value} {units}";

            if (value is int) return $"{(int)value} {units}";

            double doubleValue = -999;
            if (value is double) doubleValue              = (double)value;
            else if (value is float) doubleValue          = (float)value;
            else if (value is MovingAverage) doubleValue  = (MovingAverage)value;
            else if (value is Vector3d) doubleValue       = ((Vector3d)value).magnitude;
            else if (value is Vector3) doubleValue        = ((Vector3)value).magnitude;
            else if (value is EditableDouble) doubleValue = (EditableDouble)value;

            if (format == TIME) return GuiUtils.TimeToDHMS(doubleValue, timeDecimalPlaces);
            if (format == ANGLE) return Coordinates.AngleToDMS(doubleValue);
            if (format == ANGLE_NS) return Coordinates.AngleToDMS(doubleValue) + (doubleValue > 0 ? " N" : " S");
            if (format == ANGLE_EW) return Coordinates.AngleToDMS(doubleValue) + (doubleValue > 0 ? " E" : " W");
            if (format == SI) return doubleValue.ToSI(siSigFigs, siMaxPrecision) + units;
            return doubleValue.ToString(format) + " " + units;
        }

        private void UpdateItemCache()
        {
            int frameCount = Time.frameCount;
            if (frameCount != cacheValidity)
            {
                object value = getValue(_obj);
                stringValue   = Localizer.Format(GetStringValue(value));
                cacheValidity = frameCount;
            }
        }

        public override void UpdateItem()
        {
            externalRefresh = true;
            cacheValidity   = 0;
            UpdateItemCache();
        }

        public override void DrawItem()
        {
            if (!externalRefresh) UpdateItemCache();
            GUILayout.BeginHorizontal();
            GUILayout.Label(localizedName);                             //
            GUILayout.Label(stringValue, GUILayout.ExpandWidth(false)); //
            GUILayout.EndHorizontal();
        }
    }

    public class ActionInfoItem : InfoItem
    {
        private readonly Action action;

        public ActionInfoItem(object obj, MethodInfo method, ActionInfoItemAttribute attribute)
            : base(attribute)
        {
            Profiler.BeginSample("ActionInfoItem.ActionInfoItem");

            id = GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + method.Name;

            action = (Action)Delegate.CreateDelegate(typeof(Action), obj, method);

            Profiler.EndSample();
        }

        public override void DrawItem()
        {
            if (GUILayout.Button(localizedName)) action(); //
        }
    }

    public class ToggleInfoItem : InfoItem
    {
        private readonly object     obj;
        private readonly MemberInfo member;

        public ToggleInfoItem(object obj, MemberInfo member, ToggleInfoItemAttribute attribute)
            : base(attribute)
        {
            Profiler.BeginSample("ToggleInfoItem.ToggleInfoItem");

            id = GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + member.Name;

            this.obj    = obj;
            this.member = member;

            Profiler.EndSample();
        }

        public override void DrawItem()
        {
            bool currentValue = false;
            if (member is FieldInfo) currentValue         = (bool)((FieldInfo)member).GetValue(obj);
            else if (member is PropertyInfo) currentValue = (bool)((PropertyInfo)member).GetValue(obj, new object[] { });

            bool newValue = GUILayout.Toggle(currentValue, localizedName); //

            if (newValue != currentValue)
            {
                if (member is FieldInfo) ((FieldInfo)member).SetValue(obj, newValue);
                else if (member is PropertyInfo) ((PropertyInfo)member).SetValue(obj, newValue, new object[] { });
            }
        }
    }

    public class GeneralInfoItem : InfoItem
    {
        private readonly Action draw;
        private readonly object obj;

        public GeneralInfoItem(object obj, MethodInfo method, GeneralInfoItemAttribute attribute)
            : base(attribute)
        {
            Profiler.BeginSample("GeneralInfoItem.GeneralInfoItem");

            id = GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + method.Name;

            draw     = (Action)Delegate.CreateDelegate(typeof(Action), obj, method);
            this.obj = obj;

            Profiler.EndSample();
        }

        public override void DrawItem() => draw();

        public override void UpdateItem()
        {
            if (obj is MechJebModuleInfoItems items)
                items.UpdateItems();
        }
    }

    public class EditableInfoItem : InfoItem
    {
        public readonly  string    rightLabel;
        public readonly  float     width;
        private readonly IEditable val;

        public EditableInfoItem(object obj, MemberInfo member, EditableInfoItemAttribute attribute)
            : base(attribute)
        {
            Profiler.BeginSample("EditableInfoItem.EditableInfoItem");

            id = GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + member.Name;

            rightLabel = attribute.rightLabel;
            width      = attribute.width;

            if (member is FieldInfo) val         = (IEditable)((FieldInfo)member).GetValue(obj);
            else if (member is PropertyInfo) val = (IEditable)((PropertyInfo)member).GetValue(obj, new object[] { });

            Profiler.EndSample();
        }

        public override void DrawItem()
        {
            if (val != null)
            {
                GuiUtils.SimpleTextBox(localizedName, val, rightLabel, width); //
            }
        }
    }

    [MeansImplicitUse]
    public abstract class InfoItemAttribute : Attribute
    {
        public readonly string            name; //the name displayed in the info window
        public          InfoItem.Category category;
        public          string            description  = ""; //the description shown in the window editor list
        public          bool              showInEditor = false;
        public          bool              showInFlight = true;

        public InfoItemAttribute(string name, InfoItem.Category category)
        {
            this.name     = name;
            this.category = category;
            description   = name; //description defaults to name, but can be set to be something different
        }
    }

    //Apply this attribute to a field or method to make the field or method eligible to be made into a ValueInfoItem
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class ValueInfoItemAttribute : InfoItemAttribute
    {
        public          string units             = "";
        public          string format            = "";
        public          int    siSigFigs         = 4;
        public readonly int    siMaxPrecision    = -33;
        public          int    timeDecimalPlaces = 0;

        public ValueInfoItemAttribute(string name, InfoItem.Category category) : base(name, category) { }
    }

    //Apply this attribute to a method to make the method callable via an ActionInfoItem
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionInfoItemAttribute : InfoItemAttribute
    {
        public ActionInfoItemAttribute(string name, InfoItem.Category category) : base(name, category) { }
    }

    //Apply this attribute to a boolean to make the boolean toggleable via a ToggleInfoItem
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ToggleInfoItemAttribute : InfoItemAttribute
    {
        public ToggleInfoItemAttribute(string name, InfoItem.Category category) : base(name, category) { }
    }

    //Apply this attribute to a method to indicate that it is a method that will draw
    //an InfoItem
    [AttributeUsage(AttributeTargets.Method)]
    public class GeneralInfoItemAttribute : InfoItemAttribute
    {
        public GeneralInfoItemAttribute(string name, InfoItem.Category category) : base(name, category) { }
    }

    //Apply this attribute to a IEditable to make the contents editable via an EditableInfoItem
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class EditableInfoItemAttribute : InfoItemAttribute
    {
        public string rightLabel = "";
        public float  width      = 100;

        public EditableInfoItemAttribute(string name, InfoItem.Category category) : base(name, category) { }
    }

    public static class CustomWindowPresets
    {
        public struct Preset
        {
            public string name;
            public string sharingString;
        }

        public static readonly Preset[] presets =
        {
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname1"), //Orbit Info
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname1") + @"
Show in: flight
Value:VesselState.speedOrbital
Value:VesselState.orbitApA
Value:VesselState.orbitPeA
Value:VesselState.orbitPeriod
Value:VesselState.orbitTimeToAp
Value:VesselState.orbitTimeToPe
Value:VesselState.orbitSemiMajorAxis
Value:VesselState.orbitInclination
Value:VesselState.orbitEccentricity
Value:VesselState.orbitLAN
Value:VesselState.orbitArgumentOfPeriapsis
Value:VesselState.angleToPrograde
Value:InfoItems.RelativeInclinationToTarget
-----------------------------" //Orbit Info
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname2"), //Surface Info
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname2") + @"
Show in: flight
Value:VesselState.altitudeASL
Value:VesselState.altitudeTrue
Value:VesselState.vesselPitch
Value:VesselState.vesselHeading
Value:VesselState.vesselRoll
Value:VesselState.speedSurface
Value:VesselState.speedVertical
Value:VesselState.speedSurfaceHorizontal
Value:InfoItems.GetCoordinateString
Value:InfoItems.CurrentBiome
-----------------------------" //Surface Info
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname3"), //Vessel Info
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname3") + @"
Show in: flight editor
Value:InfoItems.MaxAcceleration
Value:InfoItems.CurrentAcceleration
Value:InfoItems.MaxThrust
Value:InfoItems.VesselMass
Value:InfoItems.SurfaceTWR
Value:InfoItems.CrewCapacity
-----------------------------" //Vessel Info
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname4"), //Delta-V Stats
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname4") + @"
Show in: flight editor
Toggle:StageStats.DVLinearThrust
Value:InfoItems.StageDeltaVAtmosphereAndVac
Value:InfoItems.TotalDeltaVAtmosphereAndVac
General:InfoItems.AllStageStats
-----------------------------" //Delta-V Stats
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname5"), //Ascent Stats
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname5") + @"
Show in: flight
Value:FlightRecorder.DeltaVExpended
Value:FlightRecorder.GravityLosses
Value:FlightRecorder.DragLosses
Value:FlightRecorder.SteeringLosses
Value:FlightRecorder.TimeSinceMark
Value:FlightRecorder.PhaseAngleFromMark
Value:FlightRecorder.GroundDistanceFromMark
-----------------------------" //Ascent Stats
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname6"), //Rendezvous Info
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname6") + @"
Show in: flight
Value:InfoItems.TargetTimeToClosestApproach
Value:InfoItems.TargetClosestApproachDistance
Value:InfoItems.TargetClosestApproachRelativeVelocity
Value:InfoItems.TargetDistance
Value:InfoItems.TargetRelativeVelocity
Value:InfoItems.RelativeInclinationToTarget
Value:InfoItems.PhaseAngle
Value:InfoItems.SynodicPeriod
-----------------------------" //Rendezvous Info
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname7"), //Landing Info
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname7") + @"
Show in: flight
Value:VesselState.altitudeTrue
Value:VesselState.speedVertical
Value:VesselState.speedSurfaceHorizontal
Value:InfoItems.TimeToImpact
Value:InfoItems.SuicideBurnCountdown
Value:InfoItems.SurfaceTWR
Action:TargetController.PickPositionTargetOnMap
Value:InfoItems.TargetDistance
Value:InfoItems.CurrentBiome
-----------------------------" //Landing Info
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname8"), //"Target Orbit Info"
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname8") + @"
Show in: flight
Value:InfoItems.TargetOrbitSpeed
Value:InfoItems.TargetApoapsis
Value:InfoItems.TargetPeriapsis
Value:InfoItems.TargetOrbitPeriod
Value:InfoItems.TargetOrbitTimeToAp
Value:InfoItems.TargetOrbitTimeToPe
Value:InfoItems.TargetSMA
Value:InfoItems.TargetInclination
Value:InfoItems.TargetEccentricity
Value:InfoItems.TargetLAN
Value:InfoItems.TargetAoP
Value:InfoItems.RelativeInclinationToTarget
-----------------------------" //Target Orbit Info
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname9"), //Stopwatch
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname9") + @"
Show in: flight
Action:FlightRecorder.Mark
Value:FlightRecorder.TimeSinceMark
Value:VesselState.time
-----------------------------" //Stopwatch
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname10"), //"Surface Navigation"
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname10") + @"
Show in: flight
Action:TargetController.PickPositionTargetOnMap
Value:InfoItems.TargetDistance
Value:InfoItems.HeadingToTarget
Value:TargetController.GetPositionTargetString
-----------------------------" //Surface Navigation
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname11"), //"Atmosphere Info"
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname11") + @"
Show in: flight
Value:VesselState.AoA
Value:VesselState.AoS
Value:VesselState.displacementAngle
Value:VesselState.mach
Value:VesselState.dynamicPressure
Value:VesselState.maxDynamicPressure
Value:VesselState.intakeAir
Value:VesselState.intakeAirAllIntakes
Value:VesselState.intakeAirNeeded
Value:VesselState.atmosphericDensityGrams
Value:InfoItems.AtmosphericPressure
Value:InfoItems.AtmosphericDrag
Value:VesselState.TerminalVelocity
-----------------------------" //Atmosphere Info
            },
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname12"), //"Maneuver Node Info"
                sharingString =
                    @"--- MechJeb Custom Window ---
Name: " + Localizer.Format("#MechJeb_WindowEd_Presetname12") + @"
Show in: flight
Value:InfoItems.TimeToManeuverNode
Value:InfoItems.NextManeuverNodeDeltaV
Value:InfoItems.NextManeuverNodeBurnTime
-----------------------------" //Maneuver Node Info
            }
        };
    }
}
