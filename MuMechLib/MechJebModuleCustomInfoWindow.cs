using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleCustomInfoWindow : DisplayModule
    {
        [Persistent(pass = (int)Pass.Global)]
        public string title = "Custom Info Window";
        [Persistent(collectionIndex = "InfoItem", pass = (int)Pass.Global)]
        public List<IInfoItem> items = new List<IInfoItem>();

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            //Do nothing: custom info windows will be saved in MechJebModuleCustomWindowEditor.OnSave
        }

        protected override void FlightWindowGUI(int windowID)
        {
            GUILayout.BeginVertical();
            foreach (IInfoItem item in items)
            {
                item.DrawItem();
            }
            if (items.Count == 0) GUILayout.Label("Add items to this window with the custom window editor.");
            GUILayout.EndVertical();

            base.FlightWindowGUI(windowID);
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(250), GUILayout.Height(30) };
        }


        public override string GetName()
        {
            return title;
        }


        public MechJebModuleCustomInfoWindow(MechJebCore core) : base(core) { }
    }


    public class MechJebModuleCustomWindowEditor : DisplayModule
    {
        public List<IInfoItem> registry = new List<IInfoItem>();
        MechJebModuleCustomInfoWindow editedWindow;
        IInfoItem selectedItem;

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            RegisterInfoItems(vesselState);
            foreach (ComputerModule m in core.GetComputerModules<ComputerModule>())
            {
                RegisterInfoItems(m);
            }

            if (global == null) return;

            Debug.Log("Loading custom windows from config node:" + global.ToString());

            //Load custom info windows, which are stored in our ConfigNode:

            ConfigNode[] windowNodes = global.GetNodes(typeof(MechJebModuleCustomInfoWindow).Name);
            Debug.Log("windowNodes.Length = " + windowNodes.Length);
            foreach (ConfigNode windowNode in windowNodes)
            {                
                MechJebModuleCustomInfoWindow window = new MechJebModuleCustomInfoWindow(core);

                window.title = windowNode.HasValue("title") ? windowNode.GetValue("title") : "Custom Info Window";

                if (windowNode.HasNode("items"))
                {
                    ConfigNode itemCollection = windowNode.GetNode("items");
                    ConfigNode[] itemNodes = itemCollection.GetNodes("InfoItem");
                    foreach (ConfigNode itemNode in itemNodes)
                    {
                        string id = itemNode.GetValue("id");
                        var matches = registry.Where(item => item.ID == id);
                        if (matches.Count() > 0) window.items.Add(matches.First());
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
                ConfigNode.CreateConfigFromObject(window, (int)Pass.Global).CopyTo(global.AddNode(name));
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            editedWindow = core.GetComputerModule<MechJebModuleCustomInfoWindow>();            
        }

        void RegisterInfoItems(object obj)
        {
            foreach (MemberInfo member in obj.GetType().GetMembers())
            {
                foreach (Attribute attribute in member.GetCustomAttributes(true))
                {
                    if (attribute is ValueInfoItemAttribute) registry.Add(new ValueInfoItem(obj, member, (ValueInfoItemAttribute)attribute));
                    else if (attribute is ActionInfoItemAttribute) registry.Add(new ActionInfoItem(obj, (MethodInfo)member, (ActionInfoItemAttribute)attribute));
                    else if (attribute is ToggleInfoItemAttribute) registry.Add(new ToggleInfoItem(obj, member, (ToggleInfoItemAttribute)attribute));
                    else if (attribute is GeneralInfoItemAttribute) registry.Add(new GeneralInfoItem(obj, (MethodInfo)member, (GeneralInfoItemAttribute)attribute));
                }
            }
        }

        void AddNewWindow()
        {
            editedWindow = new MechJebModuleCustomInfoWindow(core);
            core.AddComputerModule(editedWindow);
        }

        void RemoveCurrentWindow()
        {
            if (editedWindow == null) return;

            core.RemoveComputerModule(editedWindow);
            editedWindow = core.GetComputerModule<MechJebModuleCustomInfoWindow>();
        }


        Vector2 scrollPos, scrollPos2;
        protected override void FlightWindowGUI(int windowID)
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


            GUILayout.BeginHorizontal();

            if (allWindows.Count > 1 && GUILayout.Button("◀", GUILayout.ExpandWidth(false)))
            {
                editedWindow = allWindows[(allWindows.IndexOf(editedWindow) - 1 + allWindows.Count) % allWindows.Count];
            }

            editedWindow.title = GUILayout.TextField(editedWindow.title, GUILayout.ExpandWidth(true));

            if (allWindows.Count > 1 && GUILayout.Button("▶", GUILayout.ExpandWidth(false)))
            {
                editedWindow = allWindows[(allWindows.IndexOf(editedWindow) + 1) % allWindows.Count];
            }

            GUILayout.EndHorizontal();



            GUILayout.Label("Window contents (click to edit):");

            GUILayout.BeginVertical(GUILayout.Height(100));
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (IInfoItem item in editedWindow.items)
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                if (item == selectedItem) s.normal.textColor = Color.yellow;

                if (GUILayout.Button(item.GetDescription(), s)) selectedItem = item;
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

            scrollPos2 = GUILayout.BeginScrollView(scrollPos2);
            foreach (IInfoItem item in registry)
            {
                if (GUILayout.Button(item.GetDescription(), GuiUtils.yellowOnHover))
                {
                    editedWindow.items.Add(item);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            base.FlightWindowGUI(windowID);
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(500) };
        }

        public override string GetName()
        {
            return "Custom Window Editor";
        }

        public MechJebModuleCustomWindowEditor(MechJebCore core) : base(core) { }
    }


    public interface IInfoItem
    {
        string GetDescription();
        void DrawItem();
        string ID { get; }
    }

    //A ValueInfoItem is an info item that shows the value of some field, or the return value of some method.
    public class ValueInfoItem : IInfoItem
    {
        string name;
        MemberInfo member;
        object obj;
        string units;
        bool time;
        [Persistent]
        string id;
        public string ID { get { return id; } }

        public ValueInfoItem(object obj, MemberInfo member, ValueInfoItemAttribute attribute)
        {
            this.obj = obj;
            this.member = member;
            name = attribute.name;
            units = attribute.units;
            time = attribute.time;
            id = this.GetType().Name + ":" + obj.GetType().Name + "." + member.Name;
        }

        public string GetDescription() { return name; }

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

            if (time) return GuiUtils.TimeToDHMS(doubleValue);
            else return (MuUtils.ToSI(doubleValue) + units);
        }

        public void DrawItem()
        {
            object value = GetValue();

            string stringValue = GetStringValue(value);

            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.ExpandWidth(true));
            GUILayout.Label(stringValue, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }



    public class ActionInfoItem : IInfoItem
    {
        string name;
        object obj;
        MethodInfo method;
        [Persistent]
        string id;
        public string ID { get { return id; } }

        public ActionInfoItem(object obj, MethodInfo method, ActionInfoItemAttribute attribute)
        {
            this.obj = obj;
            this.method = method;
            name = attribute.name;
            id = this.GetType().Name + ":" + obj.GetType().Name + "." + method.Name;
        }

        public string GetDescription() { return name; }

        public void DrawItem()
        {
            if (GUILayout.Button(name)) method.Invoke(obj, new object[] { });
        }
    }


    public class ToggleInfoItem : IInfoItem
    {
        string name;
        object obj;
        MemberInfo member;
        [Persistent]
        string id;
        public string ID { get { return id; } }

        public ToggleInfoItem(object obj, MemberInfo member, ToggleInfoItemAttribute attribute)
        {
            this.obj = obj;
            this.member = member;
            name = attribute.name;
            id = this.GetType().Name + ":" + obj.GetType().Name + "." + member.Name;
        }

        public string GetDescription() { return name; }

        public void DrawItem()
        {
            bool currentValue = false;
            if (member is FieldInfo) currentValue = (bool)(((FieldInfo)member).GetValue(obj));
            else if (member is PropertyInfo) currentValue = (bool)(((PropertyInfo)member).GetValue(obj, new object[] { }));

            bool newValue = GUILayout.Toggle(currentValue, name);

            if (member is FieldInfo) ((FieldInfo)member).SetValue(obj, newValue);
            else if (member is PropertyInfo) ((PropertyInfo)member).SetValue(obj, newValue, new object[] { });
        }
    }

    public class GeneralInfoItem : IInfoItem
    {
        string name;
        object obj;
        MethodInfo method;
        [Persistent]
        string id;
        public string ID { get { return id; } }

        public GeneralInfoItem(object obj, MethodInfo method, GeneralInfoItemAttribute attribute)
        {
            this.obj = obj;
            this.method = method;
            name = attribute.name;
            id = this.GetType().Name + ":" + obj.GetType().Name + "." + method.Name;
        }

        public string GetDescription() { return name; }

        public void DrawItem()
        {
            method.Invoke(obj, new object[] { });
        }
    }


    //Apply this attribute to a field or method to make the field or method eligible to be made into a ValueInfoItem
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class ValueInfoItemAttribute : Attribute
    {
        public string name;
        public string units = "";
        public bool time = false;
    }

    //Apply this attribute to a method to make the method callable via an ActionInfoItem
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionInfoItemAttribute : Attribute
    {
        public string name;
    }
    
    //Apply this attribute to a boolean to make the boolean toggleable via a ToggleInfoItem
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ToggleInfoItemAttribute : Attribute
    {
        public string name;
    }

    //Apply this attribute to a method to indicate that it is a method that will draw 
    //an InfoItem
    [AttributeUsage(AttributeTargets.Method)]
    public class GeneralInfoItemAttribute : Attribute
    {
        public string name;
    }
}
