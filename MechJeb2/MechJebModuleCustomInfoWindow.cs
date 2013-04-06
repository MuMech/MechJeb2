using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleCustomInfoWindow : DisplayModule
    {
        [Persistent(pass = (int)Pass.Global)]
        public string title = "Custom Info Window";
        [Persistent(collectionIndex = "InfoItem", pass = (int)Pass.Global)]
        public List<InfoItem> items = new List<InfoItem>();

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            //Do nothing: custom info windows will be saved in MechJebModuleCustomWindowEditor.OnSave
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();
            foreach (InfoItem item in items)
            {
                if (HighLogic.LoadedSceneIsEditor ? item.showInEditor : item.showInFlight)
                {
                    item.DrawItem();
                }
                else
                {
                    GUILayout.Label(item.name);
                }
            }
            if (items.Count == 0) GUILayout.Label("Add items to this window with the custom window editor.");
            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(250), GUILayout.Height(30) };
        }

        public override void DrawGUI(bool inEditor)
        {
            base.DrawGUI(inEditor);

            if (showInCurrentScene)
            {
                if (GUI.Button(new Rect(windowPos.x + 10, windowPos.y, 30, 20), "Edit", GuiUtils.yellowOnHover))
                {
                    MechJebModuleCustomWindowEditor editor = core.GetComputerModule<MechJebModuleCustomWindowEditor>();
                    if (editor != null)
                    {
                        editor.enabled = true;
                        editor.editedWindow = this;
                    }
                }

                if(GUI.Button(new Rect(windowPos.x + 45, windowPos.y, 30, 20), "Copy", GuiUtils.yellowOnHover))
                {
                    MuUtils.SystemClipboard = ToSharingString();
                    ScreenMessages.PostScreenMessage("Configuration of \"" + GetName() + "\" window copied to clipboard.", 1.5f, ScreenMessageStyle.UPPER_RIGHT);
                }
            }
        }

        public string ToSharingString()
        {
            string windowSharingString = "--- MechJeb Custom Window ---\n";
            windowSharingString += "Name: " + GetName() + "\n";
            windowSharingString += "Show in:" + (showInEditor ? " editor" : "") + (showInFlight ? " flight" : "") + "\n";
            foreach (InfoItem item in items)
            {
                windowSharingString += item.id + "\n";
            }
            windowSharingString += "-----------------------------\n";
            return windowSharingString;
        }

        public void FromSharingString(string[] lines, List<InfoItem> registry)
        {
            if (lines.Length > 1 && lines[1].StartsWith("Name: ")) title = lines[1].Substring("Name: ".Length);
            if (lines.Length > 2 && lines[2].StartsWith("Show in:"))
            {
                showInEditor = lines[2].Contains("editor");
                showInFlight = lines[2].Contains("flight");
            }

            for (int i = 3; i < lines.Length; i++)
            {
                InfoItem match = registry.FirstOrDefault(item => item.id == lines[i]);
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
        InfoItem selectedItem;
        [Persistent(pass = (int)Pass.Global)]
        InfoItem.Category itemCategory = InfoItem.Category.Orbit;
        static int numCategories = Enum.GetNames(typeof(InfoItem.Category)).Length;

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

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

                if (windowNode.HasValue("enabled"))
                {
                    bool loadedEnabled;
                    if (bool.TryParse(windowNode.GetValue("enabled"), out loadedEnabled)) window.enabled = loadedEnabled;
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

            foreach (MechJebModuleCustomInfoWindow window in core.GetComputerModules<MechJebModuleCustomInfoWindow>())
            {
                string name = typeof(MechJebModuleCustomInfoWindow).Name;
                ConfigNode windowNode = ConfigNode.CreateConfigFromObject(window, (int)Pass.Global);
                windowNode.AddValue("enabled", window.enabled);
                windowNode.CopyTo(global.AddNode(name));
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
            core.AddComputerModule(editedWindow);
            editedWindow.enabled = true;
        }

        void RemoveCurrentWindow()
        {
            if (editedWindow == null) return;

            core.RemoveComputerModule(editedWindow);
            editedWindow = core.GetComputerModule<MechJebModuleCustomInfoWindow>();
        }

        Vector2 scrollPos, scrollPos2;
        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("New window")) AddNewWindow();

            if (editedWindow == null) editedWindow = core.GetComputerModule<MechJebModuleCustomInfoWindow>();

            if (editedWindow == null)
            {
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUI.DragWindow();
                return;
            }

            if (GUILayout.Button("Delete window")) RemoveCurrentWindow();
            GUILayout.EndHorizontal();

            List<MechJebModuleCustomInfoWindow> allWindows = core.GetComputerModules<MechJebModuleCustomInfoWindow>();

            int editedWindowIndex = allWindows.IndexOf(editedWindow);
            editedWindowIndex = GuiUtils.ArrowSelector(editedWindowIndex, allWindows.Count, () =>
                {
                    editedWindow.title = GUILayout.TextField(editedWindow.title, GUILayout.ExpandWidth(true));
                });
            editedWindow = allWindows[editedWindowIndex];

            GUILayout.BeginHorizontal();
            GUILayout.Label("Show in:");
            editedWindow.showInFlight = GUILayout.Toggle(editedWindow.showInFlight, "Flight");
            editedWindow.showInEditor = GUILayout.Toggle(editedWindow.showInEditor, "Editor");
            GUILayout.EndHorizontal();

            GUILayout.Label("Window contents (click to edit):");

            GUILayout.BeginVertical(GUILayout.Height(100));
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (InfoItem item in editedWindow.items)
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                if (item == selectedItem) s.normal.textColor = Color.yellow;

                if (GUILayout.Button(item.description, s)) selectedItem = item;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Remove") && selectedItem != null) editedWindow.items.Remove(selectedItem);
            if (GUILayout.Button("Move up") && selectedItem != null)
            {
                int index = editedWindow.items.IndexOf(selectedItem);
                if (index > 0)
                {
                    editedWindow.items.Remove(selectedItem);
                    editedWindow.items.Insert(index - 1, selectedItem);
                }
            }
            if (GUILayout.Button("Move down") && selectedItem != null)
            {
                int index = editedWindow.items.IndexOf(selectedItem);
                if (index < editedWindow.items.Count - 1)
                {
                    editedWindow.items.Remove(selectedItem);
                    editedWindow.items.Insert(index + 1, selectedItem);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Click an item to add it to the info window:");

            itemCategory = (InfoItem.Category)GuiUtils.ArrowSelector((int)itemCategory, numCategories, itemCategory.ToString());

            scrollPos2 = GUILayout.BeginScrollView(scrollPos2);
            foreach (InfoItem item in registry.Where(it => it.category == itemCategory).OrderBy(it => it.description))
            {
                if (GUILayout.Button(item.description, GuiUtils.yellowOnHover))
                {
                    editedWindow.items.Add(item);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(500) };
        }

        public override string GetName()
        {
            return "Custom Window Editor";
        }

        public MechJebModuleCustomWindowEditor(MechJebCore core)
            : base(core)
        {
            showInFlight = true;
            showInEditor = true;
        }

        public void AddDefaultWindows()
        {
            MechJebModuleCustomInfoWindow newWin = new MechJebModuleCustomInfoWindow(core);
            core.AddComputerModule(newWin);
            newWin.enabled = false;
            newWin.showInFlight = true;
            newWin.showInEditor = true;
            newWin.title = "Vessel Info";
            string[] itemNames = new string[] { "Vessel mass", "Max thrust", "Max acceleration", "Stage stats (all)" };
            foreach (string itemName in itemNames) newWin.items.Add(registry.Find(i => i.name == itemName));

            newWin = new MechJebModuleCustomInfoWindow(core);
            core.AddComputerModule(newWin);
            newWin.enabled = false;
            newWin.showInFlight = true;
            newWin.title = "Orbit Info";
            itemNames = new string[] { "Altitude (ASL)", "Altitude (true)", "Vertical speed", "Apoapsis", "Periapsis", "Inclination", "Coordinates" };
            foreach (string itemName in itemNames) newWin.items.Add(registry.Find(i => i.name == itemName));

            newWin = new MechJebModuleCustomInfoWindow(core);
            core.AddComputerModule(newWin);
            newWin.enabled = false;
            newWin.showInFlight = true;
            newWin.title = "Target Info";
            itemNames = new string[] { "Distance to target", "Relative velocity", "Closest approach distance", "Time to closest approach", "Rel. vel. at closest approach", "Docking guidance: position", "Docking guidance: velocity" };
            foreach (string itemName in itemNames) newWin.items.Add(registry.Find(i => i.name == itemName));
        }

        public void CreateWindowFromSharingString(string sharingString)
        {
            string[] lines = sharingString.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines[0] != "--- MechJeb Custom Window ---")
            {
                ScreenMessages.PostScreenMessage("Pasted text wasn't a MechJeb custom window descriptor.", 1.5f, ScreenMessageStyle.UPPER_RIGHT);
                return;
            }

            MechJebModuleCustomInfoWindow window = new MechJebModuleCustomInfoWindow(core);
            core.AddComputerModule(window);
            window.enabled = true;

            window.FromSharingString(lines, registry);
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
        object obj;
        MemberInfo member;
        string units;
        string format;
        public const string SI = "SI";
        public const string TIME = "TIME";
        public const string ANGLE = "ANGLE";
        bool time;
        
        public ValueInfoItem(object obj, MemberInfo member, ValueInfoItemAttribute attribute)
            : base(attribute)
        {
            id = this.GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + member.Name;

            this.obj = obj;
            this.member = member;
            units = attribute.units;
            time = attribute.time;
            format = attribute.format;
        }

        object GetValue()
        {
            if (member is FieldInfo) return ((FieldInfo)member).GetValue(obj);
            else if (member is MethodInfo) return ((MethodInfo)member).Invoke(obj, new object[] { });
            else if (member is PropertyInfo) return ((PropertyInfo)member).GetValue(obj, new object[] { });
            else return null;
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

            if (format == TIME) return GuiUtils.TimeToDHMS(doubleValue);
            else if (format == ANGLE) return Coordinates.AngleToDMS(doubleValue);
            else if (format == SI) return (MuUtils.ToSI(doubleValue) + units);
            else return doubleValue.ToString(format) + " " + units;
        }

        public override void DrawItem()
        {
            object value = GetValue();

            string stringValue = GetStringValue(value);

            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.ExpandWidth(true));
            GUILayout.Label(stringValue, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }

    public class ActionInfoItem : InfoItem
    {
        object obj;
        MethodInfo method;

        public ActionInfoItem(object obj, MethodInfo method, ActionInfoItemAttribute attribute)
            : base(attribute)
        {
            id = this.GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + method.Name;

            this.obj = obj;
            this.method = method;
        }

        public override void DrawItem()
        {
            if (GUILayout.Button(name)) method.Invoke(obj, new object[] { });
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

            bool newValue = GUILayout.Toggle(currentValue, name);

            if (member is FieldInfo) ((FieldInfo)member).SetValue(obj, newValue);
            else if (member is PropertyInfo) ((PropertyInfo)member).SetValue(obj, newValue, new object[] { });
        }
    }

    public class GeneralInfoItem : InfoItem
    {
        object obj;
        MethodInfo method;

        public GeneralInfoItem(object obj, MethodInfo method, GeneralInfoItemAttribute attribute)
            : base(attribute)
        {
            id = this.GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + method.Name;

            this.obj = obj;
            this.method = method;
        }

        public override void DrawItem()
        {
            method.Invoke(obj, new object[] { });
        }
    }

    public class EditableInfoItem : InfoItem
    {
        object obj;
        MemberInfo member;

        public EditableInfoItem(object obj, MemberInfo member, EditableInfoItemAttribute attribute)
            : base(attribute)
        {
            id = this.GetType().Name.Replace("InfoItem", "") + ":" + obj.GetType().Name.Replace("MechJebModule", "") + "." + member.Name;

            this.obj = obj;
            this.member = member;
        }

        public override void DrawItem()
        {
            IEditable val = null;

            if (member is FieldInfo) val = (IEditable)((FieldInfo)member).GetValue(obj);
            else if (member is PropertyInfo) val = (IEditable)((PropertyInfo)member).GetValue(obj, new object[] { });
            if (val != null)
            {
                GuiUtils.SimpleTextBox(name, val);
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
        public bool time = false;
        public string format = "";

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
        public EditableInfoItemAttribute(string name, InfoItem.Category category) : base(name, category) { }
    }
}
