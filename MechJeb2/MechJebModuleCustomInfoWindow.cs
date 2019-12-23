using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleCustomInfoWindow : DisplayModule
    {
        [Persistent(pass = (int)Pass.Global)]
        public string title = Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_title");//Custom Info Window
        [Persistent(collectionIndex = "InfoItem", pass = (int)Pass.Global)]
        public List<InfoItem> items = new List<InfoItem>();

        [SerializeField]
        [Persistent(pass = (int)Pass.Global)]
        private bool isCompact = false;
        public bool IsCompact
        {
            get { return isCompact; }
            set
            {
                isCompact = value;
                if (isCompact != value)
                {
                    isCompact = value;
                    dirty = true;
                }
            }
        }


        [Persistent(pass = (int) Pass.Global)]
        public Color backgroundColor = new Color(0,0,0,1);

        [Persistent(pass = (int)Pass.Global)]
        public Color text = new Color(1, 1, 1, 1);

        public Texture2D background;

        private GUISkin localSkin;

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

        protected override void WindowGUI(int windowID)
        {
            GUI.skin = isCompact ? GuiUtils.compactSkin : GuiUtils.skin;
            GUI.contentColor = text;

            GUILayout.BeginVertical();
            for (int i = 0; i < items.Count; i++)
            {
                InfoItem item = items[i];
                if (HighLogic.LoadedSceneIsEditor ? item.showInEditor : item.showInFlight)
                {
                    item.DrawItem();
                }
                else
                {
                    GUILayout.Label(Localizer.Format(item.name));//
                }
            }
            if (items.Count == 0) GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Label1"));//Add items to this window with the custom window editor.
            GUILayout.EndVertical();

            if (!IsOverlay && GUI.Button(new Rect(10, 0, 13, 20), "E", GuiUtils.yellowOnHover))
            {
                MechJebModuleCustomWindowEditor editor = core.GetComputerModule<MechJebModuleCustomWindowEditor>();
                if (editor != null)
                {
                    editor.enabled = true;
                    editor.editedWindow = this;
                }
            }

            if (!IsOverlay && GUI.Button(new Rect(25, 0, 13, 20), "C", GuiUtils.yellowOnHover))
            {
                MuUtils.SystemClipboard = ToSharingString();
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Scrmsg1", GetName()), 3.0f, ScreenMessageStyle.UPPER_RIGHT);//Configuration of <<1>> window copied to clipboard.
            }

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(250), GUILayout.Height(30) };
        }

        public override void DrawGUI(bool inEditor)
        {
            Init();
            if (IsOverlay)
            {
                GUI.skin = localSkin;
            }

            base.DrawGUI(inEditor);
            if (IsOverlay)
                GUI.skin = GuiUtils.skin;
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
                localSkin = Object.Instantiate(GuiUtils.transparentSkin);
                localSkin.window.normal.background = background;
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
            windowSharingString = windowSharingString.Replace("\n", Environment.NewLine);
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

        public override string GetName()
        {
            return title;
        }

        public MechJebModuleCustomInfoWindow(MechJebCore core) : base(core) { }
    }


    public class MechJebModuleCustomWindowEditor : DisplayModule
    {
        public List<InfoItem> registry = new List<InfoItem>();
        public MechJebModuleCustomInfoWindow editedWindow;
        int selectedItemIndex = -1;
        [Persistent(pass = (int)Pass.Global)]
        InfoItem.Category itemCategory = InfoItem.Category.Orbit;
        static string[] categories = Enum.GetNames(typeof(InfoItem.Category));
        int presetIndex = 0;

        private bool editingBackground = false;
        private bool editingText = false;

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            registry.Clear();
            editedWindow = null;

            RegisterInfoItems(vesselState);
            foreach (ComputerModule m in core.GetComputerModules<ComputerModule>())
            {
                RegisterInfoItems(m);
            }

            if (global == null) return;

            //Load custom info windows, which are stored in our ConfigNode:
            ConfigNode[] windowNodes = global.GetNodes(typeof(MechJebModuleCustomInfoWindow).Name);
            foreach (ConfigNode windowNode in windowNodes)
            {
                MechJebModuleCustomInfoWindow window = new MechJebModuleCustomInfoWindow(core);

                ConfigNode.LoadObjectFromConfig(window, windowNode);

                bool useOldConfig = true;
                if (windowNode.HasValue("enabledEditor"))
                {
                    bool loadedEnabled;
                    if (bool.TryParse(windowNode.GetValue("enabledEditor"), out loadedEnabled))
                    {
                        window.enabledEditor = loadedEnabled;
                        useOldConfig = false;
                        if (HighLogic.LoadedSceneIsEditor)
                            window.enabled = loadedEnabled;
                    }
                }

                if (windowNode.HasValue("enabledFlight"))
                {
                    bool loadedEnabled;
                    if (bool.TryParse(windowNode.GetValue("enabledFlight"), out loadedEnabled))
                    {
                        window.enabledFlight = loadedEnabled;
                        useOldConfig = false;
                        if (HighLogic.LoadedSceneIsFlight)
                            window.enabled = loadedEnabled;
                    }
                }

                if (useOldConfig)
                {
                    if (windowNode.HasValue("enabled"))
                    {
                        bool loadedEnabled;
                        if (bool.TryParse(windowNode.GetValue("enabled"), out loadedEnabled))
                        {
                            window.enabled = loadedEnabled;
                            window.enabledEditor = window.enabled;
                            window.enabledFlight = window.enabled;
                        }
                    }
                    window.enabledEditor = window.enabled;
                    window.enabledFlight = window.enabled;
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

                core.AddComputerModuleLater(window);
            }
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);

            //Save custom info windows within our ConfigNode:
            if (global == null) return;

            foreach (MechJebModuleCustomInfoWindow window in core.GetComputerModules<MechJebModuleCustomInfoWindow>())
            {
                string name = typeof(MechJebModuleCustomInfoWindow).Name;
                ConfigNode windowNode = ConfigNode.CreateConfigFromObject(window, (int)Pass.Global, null);

                if (HighLogic.LoadedSceneIsEditor)
                    window.enabledEditor = window.enabled;
                if (HighLogic.LoadedSceneIsFlight)
                    window.enabledFlight = window.enabled;

                windowNode.AddValue("enabledFlight", window.enabledFlight);
                windowNode.AddValue("enabledEditor", window.enabledEditor);
                windowNode.CopyTo(global.AddNode(name));
                window.dirty = false;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            editedWindow = core.GetComputerModule<MechJebModuleCustomInfoWindow>();
        }

        void RegisterInfoItems(object obj)
        {
            foreach (MemberInfo member in obj.GetType().GetMembers(BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                foreach (Attribute attribute in member.GetCustomAttributes(true))
                {
                    if (attribute is ValueInfoItemAttribute) registry.Add(new ValueInfoItem(obj, member, (ValueInfoItemAttribute)attribute));
                    else if (attribute is ActionInfoItemAttribute) registry.Add(new ActionInfoItem(obj, (MethodInfo)member, (ActionInfoItemAttribute)attribute));
                    else if (attribute is ToggleInfoItemAttribute) registry.Add(new ToggleInfoItem(obj, member, (ToggleInfoItemAttribute)attribute));
                    else if (attribute is GeneralInfoItemAttribute) registry.Add(new GeneralInfoItem(obj, (MethodInfo)member, (GeneralInfoItemAttribute)attribute));
                    else if (attribute is EditableInfoItemAttribute) registry.Add(new EditableInfoItem(obj, member, (EditableInfoItemAttribute)attribute));
                }
            }
        }

        void AddNewWindow()
        {
            editedWindow = new MechJebModuleCustomInfoWindow(core);
            if (HighLogic.LoadedSceneIsEditor) editedWindow.ShowInEditor = true;
            if (HighLogic.LoadedSceneIsFlight) editedWindow.ShowInFlight = true;
            core.AddComputerModule(editedWindow);
            editedWindow.enabled = true;
            editedWindow.dirty = true;
        }

        void RemoveCurrentWindow()
        {
            if (editedWindow == null) return;

            core.RemoveComputerModule(editedWindow);
            editedWindow = core.GetComputerModule<MechJebModuleCustomInfoWindow>();
        }


        public override void DrawGUI(bool inEditor)
        {
            base.DrawGUI(inEditor);

            if (editingBackground)
            {
                if (editedWindow != null)
                {
                    editedWindow.Init();

                    Color newColor = ColorPickerRGB.DrawGUI((int)windowPos.xMax + 5, (int)windowPos.yMin, editedWindow.backgroundColor);

                    if (editedWindow.backgroundColor != newColor)
                    {
                        editedWindow.backgroundColor = newColor;
                        editedWindow.background.SetPixel(0, 0, editedWindow.backgroundColor);
                        editedWindow.background.Apply();
                        editedWindow.dirty = true;
                    }
                }
            }

            if (editingText)
            {
                if (editedWindow != null)
                {
                    Color newColor = ColorPickerRGB.DrawGUI((int)windowPos.xMax + 5, (int)windowPos.yMin, editedWindow.text);
                    if (editedWindow.text != newColor)
                    {
                        editedWindow.text = newColor;
                        editedWindow.dirty = true;
                    }
                }
            }
        }

        Vector2 scrollPos, scrollPos2;
        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (editedWindow == null) editedWindow = core.GetComputerModule<MechJebModuleCustomInfoWindow>();

            if (editedWindow == null)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button1"))) AddNewWindow();//New window
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button1"))) AddNewWindow();//New window
                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button2"))) RemoveCurrentWindow();//Delete window
                GUILayout.EndHorizontal();
            }

            if (editedWindow != null)
            {
                List<ComputerModule> allWindows = core.GetComputerModules<MechJebModuleCustomInfoWindow>();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_Edtitle"), GUILayout.ExpandWidth(false));//Title:
                int editedWindowIndex = allWindows.IndexOf(editedWindow);
                editedWindowIndex = GuiUtils.ArrowSelector(editedWindowIndex, allWindows.Count, () =>
                    {
                        string newTitle = GUILayout.TextField(editedWindow.title, GUILayout.Width(120), GUILayout.ExpandWidth(false));

                        if (editedWindow.title != newTitle)
                        {
                            editedWindow.title = newTitle;
                            editedWindow.dirty = true;
                        }
                    });
                editedWindow = (MechJebModuleCustomInfoWindow)allWindows[editedWindowIndex];
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_label1"));//Show in:
                editedWindow.ShowInFlight = GUILayout.Toggle(editedWindow.ShowInFlight, Localizer.Format("#MechJeb_WindowEd_checkbox1"), GUILayout.Width(60));//Flight
                editedWindow.ShowInEditor = GUILayout.Toggle(editedWindow.ShowInEditor, Localizer.Format("#MechJeb_WindowEd_checkbox2"));//Editor
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                editedWindow.IsOverlay = GUILayout.Toggle(editedWindow.IsOverlay, Localizer.Format("#MechJeb_WindowEd_checkbox3"));//Overlay
                editedWindow.Locked = GUILayout.Toggle(editedWindow.Locked, Localizer.Format("#MechJeb_WindowEd_checkbox4"));//Locked
                editedWindow.IsCompact = GUILayout.Toggle(editedWindow.IsCompact, Localizer.Format("#MechJeb_WindowEd_checkbox5"));//Compact
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_label2"));//Color:
                bool previous = editingText;

                editingText = GUILayout.Toggle(editingText, Localizer.Format("#MechJeb_WindowEd_checkbox6"));//Text

                if (editingText && editingText != previous)
                    editingBackground = false;

                previous = editingBackground;
                editingBackground = GUILayout.Toggle(editingBackground, Localizer.Format("#MechJeb_WindowEd_checkbox7"));//Background

                if (editingBackground && editingBackground != previous)
                    editingText = false;

                GUILayout.EndHorizontal();

                GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_label3"));//Window contents (click to edit):

                GUILayout.BeginVertical(GUILayout.Height(100));
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                for (int i = 0; i < editedWindow.items.Count; i++)
                {
                    GUIStyle s = new GUIStyle(GUI.skin.label);
                    if (i == selectedItemIndex) s.normal.textColor = Color.yellow;

                    if (GUILayout.Button(Localizer.Format(editedWindow.items[i].description), s)) selectedItemIndex = i;//
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                GUILayout.BeginHorizontal();

                if (!(selectedItemIndex >= 0 && selectedItemIndex < editedWindow.items.Count)) selectedItemIndex = -1;

                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button3")) && selectedItemIndex != -1) editedWindow.items.RemoveAt(selectedItemIndex);//Remove
                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button4")) && selectedItemIndex != -1)//"Move up"
                {
                    if (selectedItemIndex > 0)
                    {
                        InfoItem item = editedWindow.items[selectedItemIndex];
                        editedWindow.items.RemoveAt(selectedItemIndex);
                        editedWindow.items.Insert(selectedItemIndex - 1, item);
                        selectedItemIndex -= 1;
                    }
                }
                if (GUILayout.Button(Localizer.Format("#MechJeb_WindowEd_button5")) && selectedItemIndex != -1)//Move down
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

                GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_label4"));//Click an item to add it to the info window:

                itemCategory = (InfoItem.Category)GuiUtils.ComboBox.Box((int)itemCategory, categories, this);

                scrollPos2 = GUILayout.BeginScrollView(scrollPos2);
                foreach (InfoItem item in registry.Where(it => it.category == itemCategory).OrderBy(it => it.description))
                {
                    if (GUILayout.Button(Localizer.Format(item.description), GuiUtils.yellowOnHover))//
                    {
                        editedWindow.items.Add(item);
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.Label(Localizer.Format("#MechJeb_WindowEd_label5"), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });//Window presets:

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
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(540) };
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_WindowEd_title");//Custom Window Editor
        }

        public MechJebModuleCustomWindowEditor(MechJebCore core)
            : base(core)
        {
            ShowInFlight = true;
            ShowInEditor = true;
        }

        public void AddDefaultWindows()
        {
            CreateWindowFromSharingString(CustomWindowPresets.presets[0].sharingString).enabled = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[1].sharingString).enabled = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[2].sharingString).enabled = false;
            CreateWindowFromSharingString(CustomWindowPresets.presets[3].sharingString).enabled = false;
        }

        public MechJebModuleCustomInfoWindow CreateWindowFromSharingString(string sharingString)
        {
            string[] lines = sharingString.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines[0] != "--- MechJeb Custom Window ---")
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Scrmsg2"), 3.0f, ScreenMessageStyle.UPPER_RIGHT);//"Pasted text wasn't a MechJeb custom window descriptor."
                return null;
            }

            MechJebModuleCustomInfoWindow window = new MechJebModuleCustomInfoWindow(core);
            core.AddComputerModule(window);
            window.enabled = true;

            window.FromSharingString(lines, registry);

            return window;
        }
    }

    public class InfoItem
    {
        public string name;
        public string description;
        public bool showInEditor;
        public bool showInFlight;

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
        public Category category;

        [Persistent]
        public string id;

        public InfoItem() { }

        public InfoItem(InfoItemAttribute attribute)
        {
            name = attribute.name;
            category = attribute.category;
            description = attribute.description;
            showInEditor = attribute.showInEditor;
            showInFlight = attribute.showInFlight;
        }

        public virtual void DrawItem() { }
    }

    //A ValueInfoItem is an info item that shows the value of some field, or the return value of some method.
    public class ValueInfoItem : InfoItem
    {
        string units;
        string format;
        public const string SI = "SI";
        public const string TIME = "TIME";
        public const string ANGLE = "ANGLE";
        public const string ANGLE_NS = "ANGLE_NS";
        public const string ANGLE_EW = "ANGLE_EW";
        int siSigFigs; //only used with the "SI" format
        int siMaxPrecision; //only used with the "SI" format
        int timeDecimalPlaces; //only used with the "TIME" format

        Func<object> getValue;

        private string stringValue;
        private int cacheValidity = -1;

        public ValueInfoItem(object obj, MemberInfo member, ValueInfoItemAttribute attribute)
            : base(attribute)
        {
            id = this.GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + member.Name;

            units = attribute.units;
            format = attribute.format;
            siSigFigs = attribute.siSigFigs;
            siMaxPrecision = attribute.siMaxPrecision;
            timeDecimalPlaces = attribute.timeDecimalPlaces;

            // This ugly stuff compiles a small function to grab the value of member, so that we don't
            // have to use reflection to get it every frame.
            ParameterExpression objExpr = Expression.Parameter(typeof(object), ""); // obj
            Expression castObjExpr = Expression.Convert(objExpr, obj.GetType()); // (T)obj
            Expression memberExpr;
            if (member is MethodInfo) memberExpr = Expression.Call(castObjExpr, (MethodInfo)member);
            else memberExpr = Expression.MakeMemberAccess(castObjExpr, member); // ((T)obj).member
            Expression castMemberExpr = Expression.Convert(memberExpr, typeof(object)); // (object)(((T)obj).member);
            Func<object, object> getFromObj = Expression.Lambda<Func<object, object>>(castMemberExpr, new[] { objExpr }).Compile();
            getValue = () => getFromObj(obj);
        }

        string GetStringValue(object value)
        {
            if (value == null) return "null";

            if (value is string) return (string)value + " " + units;

            if (value is int) return ((int)value).ToString() + " " + units;

            double doubleValue = -999;
            if (value is double) doubleValue = (double)value;
            else if (value is float) doubleValue = (float)value;
            else if (value is MovingAverage) doubleValue = (MovingAverage)value;
            else if (value is Vector3d) doubleValue = ((Vector3d)value).magnitude;
            else if (value is Vector3) doubleValue = ((Vector3)value).magnitude;
            else if (value is EditableDouble) doubleValue = (EditableDouble)value;

            if (format == TIME) return GuiUtils.TimeToDHMS(doubleValue, timeDecimalPlaces);
            else if (format == ANGLE) return Coordinates.AngleToDMS(doubleValue);
            else if (format == ANGLE_NS) return Coordinates.AngleToDMS(doubleValue) + " " + (doubleValue > 0 ? "N" : "S");
            else if (format == ANGLE_EW) return Coordinates.AngleToDMS(doubleValue) + " " + (doubleValue > 0 ? "E" : "W");
            else if (format == SI) return (MuUtils.ToSI(doubleValue, siMaxPrecision, siSigFigs) + units);
            else return doubleValue.ToString(format) + " " + units;
        }

        public override void DrawItem()
        {
            int frameCount = Time.frameCount;
            if (frameCount != cacheValidity)
            {
                object value = getValue();
                stringValue = GetStringValue(value);
                cacheValidity = frameCount;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format(name), GUILayout.ExpandWidth(true));//
            GUILayout.Label(Localizer.Format(stringValue), GUILayout.ExpandWidth(false));//
            GUILayout.EndHorizontal();
        }
    }

    public class ActionInfoItem : InfoItem
    {
        Action action;

        public ActionInfoItem(object obj, MethodInfo method, ActionInfoItemAttribute attribute)
            : base(attribute)
        {
            id = this.GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + method.Name;

            action = (Action)Delegate.CreateDelegate(typeof(Action), obj, method);
        }

        public override void DrawItem()
        {
            if (GUILayout.Button(Localizer.Format(name))) action();//
        }
    }

    public class ToggleInfoItem : InfoItem
    {
        object obj;
        MemberInfo member;

        public ToggleInfoItem(object obj, MemberInfo member, ToggleInfoItemAttribute attribute)
            : base(attribute)
        {
            id = this.GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + member.Name;

            this.obj = obj;
            this.member = member;
        }

        public override void DrawItem()
        {
            bool currentValue = false;
            if (member is FieldInfo) currentValue = (bool)(((FieldInfo)member).GetValue(obj));
            else if (member is PropertyInfo) currentValue = (bool)(((PropertyInfo)member).GetValue(obj, new object[] { }));

            bool newValue = GUILayout.Toggle(currentValue,Localizer.Format(name));//

            if (newValue != currentValue)
            {
                if (member is FieldInfo) ((FieldInfo)member).SetValue(obj, newValue);
                else if (member is PropertyInfo) ((PropertyInfo)member).SetValue(obj, newValue, new object[] { });
            }
        }
    }

    public class GeneralInfoItem : InfoItem
    {
        Action draw;

        public GeneralInfoItem(object obj, MethodInfo method, GeneralInfoItemAttribute attribute)
            : base(attribute)
        {
            id = this.GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + method.Name;

            draw = (Action)Delegate.CreateDelegate(typeof(Action), obj, method);
        }

        public override void DrawItem()
        {
            draw();
        }
    }

    public class EditableInfoItem : InfoItem
    {
        public string rightLabel;
        public float width;
        IEditable val;

        public EditableInfoItem(object obj, MemberInfo member, EditableInfoItemAttribute attribute)
            : base(attribute)
        {
            id = this.GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + member.Name;

            this.rightLabel = attribute.rightLabel;
            this.width = attribute.width;

            if (member is FieldInfo) val = (IEditable)((FieldInfo)member).GetValue(obj);
            else if (member is PropertyInfo) val = (IEditable)((PropertyInfo)member).GetValue(obj, new object[] { });
        }

        public override void DrawItem()
        {
            if (val != null)
            {
                GuiUtils.SimpleTextBox(Localizer.Format(name), val, rightLabel, width);//
            }
        }
    }

    public abstract class InfoItemAttribute : Attribute
    {
        public string name; //the name displayed in the info window
        public InfoItem.Category category;
        public string description = ""; //the description shown in the window editor list
        public bool showInEditor = false;
        public bool showInFlight = true;

        public InfoItemAttribute(string name, InfoItem.Category category)
        {
            this.name = name;
            this.category = category;
            description = name; //description defaults to name, but can be set to be something different
        }
    }

    //Apply this attribute to a field or method to make the field or method eligible to be made into a ValueInfoItem
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class ValueInfoItemAttribute : InfoItemAttribute
    {
        public string units = "";
        public string format = "";
        public int siSigFigs = 4;
        public int siMaxPrecision = -99;
        public int timeDecimalPlaces = 0;

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
        public float width = 100;

        public EditableInfoItemAttribute(string name, InfoItem.Category category) : base(name, category) { }
    }

    public static class CustomWindowPresets
    {
        public struct Preset
        {
            public string name;
            public string sharingString;
        }

        public static Preset[] presets = new Preset[] 
        {
            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname1"),//Orbit Info
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname1")+@"
Show in: flight
Value:VesselState.speedOrbital
Value:VesselState.orbitApA
Value:VesselState.orbitPeA
Value:VesselState.orbitPeriod
Value:VesselState.orbitTimeToAp
Value:VesselState.orbitTimeToPe
Value:VesselState.orbitInclination
Value:VesselState.orbitEccentricity
Value:VesselState.angleToPrograde
-----------------------------"//Orbit Info
            },

            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname2"),//Surface Info
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname2")+@"
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
-----------------------------"//Surface Info
            },

            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname3"),//Vessel Info
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname3")+@"
Show in: flight editor
Value:InfoItems.MaxAcceleration
Value:InfoItems.CurrentAcceleration
Value:InfoItems.MaxThrust
Value:InfoItems.VesselMass
Value:InfoItems.SurfaceTWR
Value:InfoItems.CrewCapacity
-----------------------------"//Vessel Info
            },

            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname4"),//Delta-V Stats
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname4")+@"
Show in: flight editor
Toggle:StageStats.dVLinearThrust
Value:InfoItems.StageDeltaVAtmosphereAndVac
Value:InfoItems.TotalDeltaVAtmosphereAndVac
General:InfoItems.AllStageStats
-----------------------------"//Delta-V Stats
            },

            new Preset 
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname5"),//Ascent Stats
                sharingString = 
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname5")+@"
Show in: flight
Value:FlightRecorder.timeSinceMark
Value:FlightRecorder.deltaVExpended
Value:FlightRecorder.gravityLosses
Value:FlightRecorder.dragLosses
Value:FlightRecorder.steeringLosses
Value:FlightRecorder.phaseAngleFromMark
-----------------------------"//Ascent Stats
            },

            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname6"),//Rendezvous Info
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname6")+@"
Show in: flight
Value:InfoItems.TargetTimeToClosestApproach
Value:InfoItems.TargetClosestApproachDistance
Value:InfoItems.TargetClosestApproachRelativeVelocity
Value:InfoItems.TargetDistance
Value:InfoItems.TargetRelativeVelocity
Value:InfoItems.RelativeInclinationToTarget
Value:InfoItems.PhaseAngle
Value:InfoItems.SynodicPeriod
-----------------------------"//Rendezvous Info
            },

            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname7"),//Landing Info
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname7")+@"
Show in: flight
Value:VesselState.altitudeTrue
Value:VesselState.speedVertical
Value:VesselState.speedSurfaceHorizontal
Value:InfoItems.TimeToImpact
Value:InfoItems.SuicideBurnCountdown
Value:InfoItems.SurfaceTWR
Action:TargetController.PickPositionTargetOnMap
Value:InfoItems.TargetDistance
-----------------------------"//Landing Info
            },


            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname8"),//"Target Orbit Info"
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname8")+@"
Show in: flight
Value:InfoItems.TargetOrbitSpeed
Value:InfoItems.TargetApoapsis
Value:InfoItems.TargetPeriapsis
Value:InfoItems.TargetOrbitPeriod
Value:InfoItems.TargetOrbitTimeToAp
Value:InfoItems.TargetOrbitTimeToPe
Value:InfoItems.TargetInclination
Value:InfoItems.TargetEccentricity
-----------------------------"//Target Orbit Info
            },


            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname9"),//Stopwatch
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname9")+@"
Show in: flight
Action:FlightRecorder.Mark
Value:FlightRecorder.timeSinceMark
Value:VesselState.time
-----------------------------"//Stopwatch
            },


            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname10"),//"Surface Navigation"
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname10")+@"
Show in: flight
Action:TargetController.PickPositionTargetOnMap
Value:InfoItems.TargetDistance
Value:InfoItems.HeadingToTarget
Value:TargetController.GetPositionTargetString
-----------------------------"//Surface Navigation
            },


            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname11"),//"Atmosphere Info"
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname11")+@"
Show in: flight
Value:VesselState.atmosphericDensityGrams
Value:InfoItems.AtmosphericPressure
Value:InfoItems.AtmosphericDrag
Value:VesselState.TerminalVelocity
-----------------------------"//Atmosphere Info
            },

            new Preset
            {
                name = Localizer.Format("#MechJeb_WindowEd_Presetname12"),//"Maneuver Node Info"
                sharingString =
@"--- MechJeb Custom Window ---
Name: "+Localizer.Format("#MechJeb_WindowEd_Presetname12")+@"
Show in: flight
Value:InfoItems.TimeToManeuverNode
Value:InfoItems.NextManeuverNodeDeltaV
Value:InfoItems.NextManeuverNodeBurnTime
-----------------------------"//Maneuver Node Info
            }
        };
    }
}
