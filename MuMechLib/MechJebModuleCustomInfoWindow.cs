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
        InfoItemRegistry registry = new InfoItemRegistry();

        public override void OnStart(PartModule.StartState state)
        {
            registry.ScanObject(vesselState);
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300) };
        }

        protected override void FlightWindowGUI(int windowID)
        {
            Debug.Log("registry == null? " + (registry == null));
            GUILayout.BeginVertical();
            foreach (IInfoItem item in registry.items)
            {
                if (!item.GetDescription().ToLower().StartsWith("t")) continue;
                Debug.Log("item == null ? " + (item == null));
                if (item != null) Debug.Log("item.GetDescription() = " + item.GetDescription());
                item.DrawItem();
            }
            GUILayout.EndVertical();

            base.FlightWindowGUI(windowID);
        }

        public override string GetName()
        {
            return "Custom Info Window";
        }


        public MechJebModuleCustomInfoWindow(MechJebCore core) : base(core) { }
    }



    public interface IInfoItem
    {
        string GetDescription();
        void DrawItem();
    }

    //A FieldInfoItem is an info item that shows the value of some field of some object.
    //The field must be convertable to a double. The field's value is shown in SI format with the specified units.
    public class MemberInfoItem : IInfoItem
    {
        string name;
        MemberInfo member;
        object obj;
        string units;

        public MemberInfoItem(string name, object obj, MemberInfo member, string units)
        {
            this.name = name;
            this.obj = obj;
            this.member = member;
            this.units = units;
        }

        public string GetDescription() { return name; }

        public void DrawItem()
        {
            object value;

            if (member is FieldInfo) value = ((FieldInfo)member).GetValue(obj);
            else if (member is MethodInfo) value = ((MethodInfo)member).Invoke(obj, new object[] { });
            else return;

            double doubleValue = -999;
            if (value is double) doubleValue = (double)value;
            else if (value is float) doubleValue = (float)value;
            else if (value is int) doubleValue = (int)value;
            else if (value is MovingAverage) doubleValue = (MovingAverage)value;
            else if (value is Vector3d) doubleValue = ((Vector3d)value).magnitude;
            else if (value is Vector3) doubleValue = ((Vector3)value).magnitude;
            //any other types we need here?

            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.ToSI(doubleValue) + units);
            GUILayout.EndHorizontal();
        }

        //Scan an object and create InfoItems for each member labeled with the InfoItem attribute
        public static List<IInfoItem> MakeInfoItems(object obj)
        {
            Debug.Log("Making field info items for " + obj);
            List<IInfoItem> items = new List<IInfoItem>();
            foreach(MemberInfo member in obj.GetType().GetMembers())
            {
                object[] attributes = member.GetCustomAttributes(typeof(InfoItemAttribute), true);
                if (attributes.Length > 0)
                {
                    InfoItemAttribute attribute = (InfoItemAttribute)attributes[0];
                    Debug.Log("Found tagged field: " + member.Name + "; name = " + attribute.name + "; units = " + attribute.units);
                    items.Add(new MemberInfoItem(attribute.name, obj, member, attribute.units));
                }
            }
            return items;
        }
    }


    //Apply this attribute to a field to make the field eligible to be made into a FieldInfoItem
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Method)]
    public class InfoItemAttribute : Attribute
    {
        public string name;
        public string units;
    }

    public class InfoItemRegistry
    {
        public List<IInfoItem> items = new List<IInfoItem>();

        public void ScanObject(object obj)
        {
            items.AddRange(MemberInfoItem.MakeInfoItems(obj));
        }
    }
}
