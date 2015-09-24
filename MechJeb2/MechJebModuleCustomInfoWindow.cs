using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
            for (int i = 0; i < items.Count; i++)
            {
                InfoItem item = items[i];
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

            if (GUI.Button(new Rect(10, 0, 13, 20), "E", GuiUtils.yellowOnHover))
            {
                MechJebModuleCustomWindowEditor editor = core.GetComputerModule<MechJebModuleCustomWindowEditor>();
                if (editor != null)
                {
                    editor.enabled = true;
                    editor.editedWindow = this;
                }
            }

            if (GUI.Button(new Rect(25, 0, 13, 20), "C", GuiUtils.yellowOnHover))
            {
                MuUtils.SystemClipboard = ToSharingString();
                ScreenMessages.PostScreenMessage("Configuration of \"" + GetName() + "\" window copied to clipboard.", 3.0f, ScreenMessageStyle.UPPER_RIGHT);
            }

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
            }
        }

        public string ToSharingString()
        {
            string windowSharingString = "--- MechJeb Custom Window ---\n";
            windowSharingString += "Name: " + GetName() + "\n";
            windowSharingString += "Show in:" + (showInEditor ? " editor" : "") + (showInFlight ? " flight" : "") + "\n";
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
                showInEditor = lines[2].Contains("editor");
                showInFlight = lines[2].Contains("flight");
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
            if (global == null) return;

            foreach (MechJebModuleCustomInfoWindow window in core.GetComputerModules<MechJebModuleCustomInfoWindow>())
            {
                string name = typeof(MechJebModuleCustomInfoWindow).Name;
                ConfigNode windowNode = ConfigNode.CreateConfigFromObject(window, (int)Pass.Global, null);
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
            if (HighLogic.LoadedSceneIsEditor) editedWindow.showInEditor = true;
            if (HighLogic.LoadedSceneIsFlight) editedWindow.showInFlight = true;
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

            if (editedWindow == null) editedWindow = core.GetComputerModule<MechJebModuleCustomInfoWindow>();

            if (editedWindow == null)
            {
                if (GUILayout.Button("New window")) AddNewWindow();
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("New window")) AddNewWindow();
                if (GUILayout.Button("Delete window")) RemoveCurrentWindow();
                GUILayout.EndHorizontal();
            }

            if (editedWindow != null)
            {
                List<MechJebModuleCustomInfoWindow> allWindows = core.GetComputerModules<MechJebModuleCustomInfoWindow>().ToList();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Title:", GUILayout.ExpandWidth(false));
                int editedWindowIndex = allWindows.IndexOf(editedWindow);
                editedWindowIndex = GuiUtils.ArrowSelector(editedWindowIndex, allWindows.Count, () =>
                    {
                        editedWindow.title = GUILayout.TextField(editedWindow.title, GUILayout.Width(120), GUILayout.ExpandWidth(false));
                    });
                editedWindow = allWindows[editedWindowIndex];
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Show in:");
                editedWindow.showInFlight = GUILayout.Toggle(editedWindow.showInFlight, "Flight", GUILayout.Width(60));
                editedWindow.showInEditor = GUILayout.Toggle(editedWindow.showInEditor, "Editor");
                GUILayout.EndHorizontal();

                GUILayout.Label("Window contents (click to edit):");

                GUILayout.BeginVertical(GUILayout.Height(100));
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                for (int i = 0; i < editedWindow.items.Count; i++)
                {
                    GUIStyle s = new GUIStyle(GUI.skin.label);
                    if (i == selectedItemIndex) s.normal.textColor = Color.yellow;

                    if (GUILayout.Button(editedWindow.items[i].description, s)) selectedItemIndex = i;
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                GUILayout.BeginHorizontal();

                if (!(selectedItemIndex >= 0 && selectedItemIndex < editedWindow.items.Count)) selectedItemIndex = -1;

                if (GUILayout.Button("Remove") && selectedItemIndex != -1) editedWindow.items.RemoveAt(selectedItemIndex);
                if (GUILayout.Button("Move up") && selectedItemIndex != -1)
                {
                    if (selectedItemIndex > 0)
                    {
                        InfoItem item = editedWindow.items[selectedItemIndex];
                        editedWindow.items.RemoveAt(selectedItemIndex);
                        editedWindow.items.Insert(selectedItemIndex - 1, item);
                        selectedItemIndex -= 1;
                    }
                }
                if (GUILayout.Button("Move down") && selectedItemIndex != -1)
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

                GUILayout.Label("Click an item to add it to the info window:");

                itemCategory = (InfoItem.Category)GuiUtils.ComboBox.Box((int)itemCategory, categories, this);

                scrollPos2 = GUILayout.BeginScrollView(scrollPos2);
                foreach (InfoItem item in registry.Where(it => it.category == itemCategory).OrderBy(it => it.description))
                {
                    if (GUILayout.Button(item.description, GuiUtils.yellowOnHover))
                    {
                        editedWindow.items.Add(item);
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.Label("Window presets:", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });

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
                ScreenMessages.PostScreenMessage("Pasted text wasn't a MechJeb custom window descriptor.", 3.0f, ScreenMessageStyle.UPPER_RIGHT);
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
            GUILayout.Label(name, GUILayout.ExpandWidth(true));
            GUILayout.Label(stringValue, GUILayout.ExpandWidth(false));
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
            if (GUILayout.Button(name)) action();
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
                GuiUtils.SimpleTextBox(name, val, rightLabel, width);
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
                name = "Orbit Info",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Orbit Info
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
-----------------------------"
            },

            new Preset
            {
                name = "Surface Info",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Surface Info
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
-----------------------------"
            },

            new Preset
            {
                name = "Vessel Info",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Vessel Info
Show in: flight editor
Value:InfoItems.MaxAcceleration
Value:InfoItems.CurrentAcceleration
Value:InfoItems.MaxThrust
Value:InfoItems.VesselMass
Value:InfoItems.SurfaceTWR
Value:InfoItems.CrewCapacity
-----------------------------"
            },

            new Preset
            {
                name = "Delta-V Stats",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Delta-V Stats
Show in: flight editor
Toggle:StageStats.dVLinearThrust
Value:InfoItems.StageDeltaVAtmosphereAndVac
Value:InfoItems.TotalDeltaVAtmosphereAndVac
General:InfoItems.AllStageStats
-----------------------------"
            },

            new Preset 
            {
                name = "Ascent Stats",
                sharingString = 
@"--- MechJeb Custom Window ---
Name: Ascent Stats
Show in: flight
Value:FlightRecorder.timeSinceMark
Value:FlightRecorder.deltaVExpended
Value:FlightRecorder.gravityLosses
Value:FlightRecorder.dragLosses
Value:FlightRecorder.steeringLosses
Value:FlightRecorder.phaseAngleFromMark
-----------------------------"
            },

            new Preset
            {
                name = "Rendezvous Info",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Rendezvous Info
Show in: flight
Value:InfoItems.TargetTimeToClosestApproach
Value:InfoItems.TargetClosestApproachDistance
Value:InfoItems.TargetClosestApproachRelativeVelocity
Value:InfoItems.TargetDistance
Value:InfoItems.TargetRelativeVelocity
Value:InfoItems.RelativeInclinationToTarget
Value:InfoItems.PhaseAngle
Value:InfoItems.SynodicPeriod
-----------------------------"
            },

            new Preset
            {
                name = "Landing Info",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Landing Info
Show in: flight
Value:VesselState.altitudeTrue
Value:VesselState.speedVertical
Value:VesselState.speedSurfaceHorizontal
Value:InfoItems.TimeToImpact
Value:InfoItems.SuicideBurnCountdown
Value:InfoItems.SurfaceTWR
Action:TargetController.PickPositionTargetOnMap
Value:InfoItems.TargetDistance
-----------------------------"
            },


            new Preset
            {
                name = "Target Orbit Info",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Target Orbit Info
Show in: flight
Value:InfoItems.TargetOrbitSpeed
Value:InfoItems.TargetApoapsis
Value:InfoItems.TargetPeriapsis
Value:InfoItems.TargetOrbitPeriod
Value:InfoItems.TargetOrbitTimeToAp
Value:InfoItems.TargetOrbitTimeToPe
Value:InfoItems.TargetInclination
Value:InfoItems.TargetEccentricity
-----------------------------"
            },


            new Preset
            {
                name = "Stopwatch",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Stopwatch
Show in: flight
Action:FlightRecorder.Mark
Value:FlightRecorder.timeSinceMark
Value:VesselState.time
-----------------------------"
            },


            new Preset
            {
                name = "Surface Navigation",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Surface Navigation
Show in: flight
Action:TargetController.PickPositionTargetOnMap
Value:InfoItems.TargetDistance
Value:InfoItems.HeadingToTarget
Value:TargetController.GetPositionTargetString
-----------------------------"
            },


            new Preset
            {
                name = "Atmosphere Info",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Atmosphere Info
Show in: flight
Value:VesselState.atmosphericDensityGrams
Value:InfoItems.AtmosphericPressure
Value:InfoItems.AtmosphericDrag
Value:VesselState.TerminalVelocity
-----------------------------"
            },

            new Preset
            {
                name = "Maneuver Node Info",
                sharingString =
@"--- MechJeb Custom Window ---
Name: Maneuver Node Info
Show in: flight
Value:InfoItems.TimeToManeuverNode
Value:InfoItems.NextManeuverNodeDeltaV
Value:InfoItems.NextManeuverNodeBurnTime
-----------------------------"
            }
        };
    }
}
