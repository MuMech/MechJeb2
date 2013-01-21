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
        public string title = "Custom Info Window";
        public List<IInfoItem> items = new List<IInfoItem>();

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
        List<IInfoItem> registry = new List<IInfoItem>();
        MechJebModuleCustomInfoWindow window;

        public override void OnStart(PartModule.StartState state)
        {
            window = core.GetComputerModule<MechJebModuleCustomInfoWindow>();

            RegisterInfoItems(vesselState);
            foreach (ComputerModule m in core.GetComputerModules<ComputerModule>())
            {
                RegisterInfoItems(m);
            }
        }

        void RegisterInfoItems(object obj)
        {
            foreach (MemberInfo member in obj.GetType().GetMembers())
            {
                foreach(Attribute attribute in member.GetCustomAttributes(true)) 
                {
                    if (attribute is ValueInfoItemAttribute) registry.Add(new ValueInfoItem(obj, member, (ValueInfoItemAttribute)attribute));
                    else if (attribute is ActionInfoItemAttribute) registry.Add(new ActionInfoItem(obj, (MethodInfo)member, (ActionInfoItemAttribute)attribute));
                    else if (attribute is ToggleInfoItemAttribute) registry.Add(new ToggleInfoItem(obj, member, (ToggleInfoItemAttribute)attribute));
                }
            }
        }

        void AddNewWindow()
        {
            window = new MechJebModuleCustomInfoWindow(core);
            core.AddComputerModule(window);
        }

        void RemoveCurrentWindow()
        {
            if (window == null) return;

            core.RemoveComputerModule(window);
            window = core.GetComputerModule<MechJebModuleCustomInfoWindow>();
        }



        GUIStyle _buttonStyle;
        GUIStyle buttonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(GUI.skin.label);
                    _buttonStyle.hover.textColor = Color.yellow;
                    Texture2D t = new Texture2D(1, 1);
                    t.SetPixel(0, 0, new Color(0, 0, 0, 0));
                    t.Apply();
                    _buttonStyle.hover.background = t;
                }
                return _buttonStyle;
            }
        }

        Vector2 scrollPos;
        protected override void FlightWindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("Add new window")) AddNewWindow();

            if (GUILayout.Button("Delete this window")) RemoveCurrentWindow();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name: ");
            window.title = GUILayout.TextField(window.title, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.Label("Click an item to add it to the info window");

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            foreach (IInfoItem item in registry)
            {
                if (GUILayout.Button(item.GetDescription(), buttonStyle))
                {
                    window.items.Add(item);
                }
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            base.FlightWindowGUI(windowID);
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(400) };
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
    }

    //A ValueInfoItem is an info item that shows the value of some field, or the return value of some method.
    public class ValueInfoItem : IInfoItem
    {
        string name;
        MemberInfo member;
        object obj;
        string units;

        public ValueInfoItem(object obj, MemberInfo member, ValueInfoItemAttribute attribute)
        {
            this.obj = obj;
            this.member = member;
            name = attribute.name;
            units = attribute.units;
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

            return (MuUtils.ToSI(doubleValue) + " " + units);
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

        public ActionInfoItem(object obj, MethodInfo method, ActionInfoItemAttribute attribute)
        {
            this.obj = obj;
            this.method = method;
            name = attribute.name;
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

        public ToggleInfoItem(object obj, MemberInfo member, ToggleInfoItemAttribute attribute)
        {
            this.obj = obj;
            this.member = member;
            name = attribute.name;
        }

        public string GetDescription() { return name; }

        public void DrawItem()
        {
            bool currentValue = false;
            if (member is FieldInfo) currentValue = (bool)(((FieldInfo)member).GetValue(obj));
            else if (member is PropertyInfo) currentValue = (bool)(((PropertyInfo)member).GetValue(obj, new object[]{}));

            bool newValue = GUILayout.Toggle(currentValue, name);

            if (member is FieldInfo) ((FieldInfo)member).SetValue(obj, newValue);
            else if (member is PropertyInfo) ((PropertyInfo)member).SetValue(obj, newValue, new object[] { });
        }
    }


    //Apply this attribute to a field or method to make the field or method eligible to be made into a ValueInfoItem
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class ValueInfoItemAttribute : Attribute
    {
        public string name;
        public string units = "";
    }

    //Apply this attribute to a method to make the method callable via an ActionInfoItem
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionInfoItemAttribute : Attribute
    {
        public string name;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ToggleInfoItemAttribute : Attribute
    {
        public string name;
    }
}
