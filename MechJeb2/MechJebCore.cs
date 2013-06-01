﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using UnityEngine;
using KSP.IO;

namespace MuMech
{
    public class MechJebCore : PartModule, IComparable<MechJebCore>
    {
        private List<ComputerModule> computerModules = new List<ComputerModule>();
        private List<ComputerModule> modulesToLoad = new List<ComputerModule>();
        private bool modulesUpdated = false;

        private static List<Type> moduleRegistry;

        public MechJebModuleAttitudeController attitude;
        public MechJebModuleStagingController staging;
        public MechJebModuleThrustController thrust;
        public MechJebModuleTargetController target;
        public MechJebModuleWarpController warp;
        public MechJebModuleRCSController rcs;
        public MechJebModuleRCSBalancer rcsbal;
        public MechJebModuleRoverController rover;
        public MechJebModuleNodeExecutor node;

        public VesselState vesselState = new VesselState();

        private Vessel controlledVessel; //keep track of which vessel we've added our onFlyByWire callback to

        public string version = "";

        [KSPField(isPersistant = false)]
        public string blacklist = "";

        private bool weLockedEditor = false;
        private float lastSettingsSaveTime;
        private bool showGui = true;
        public static RenderingManager renderingManager = null;
        protected bool wasMasterAndFocus = false;
        protected static Vessel lastFocus = null;

        //Returns whether the vessel we've registered OnFlyByWire with is the correct one. 
        //If it isn't the correct one, fixes it before returning false
        bool CheckControlledVessel()
        {
            if (controlledVessel == vessel) return true;

            //else we have an onFlyByWire callback registered with the wrong vessel:
            //handle vessel changes due to docking/undocking
            if (controlledVessel != null) controlledVessel.OnFlyByWire -= OnFlyByWire;
            if (vessel != null)
            {
                vessel.OnFlyByWire -= OnFlyByWire; //just a safety precaution to avoid duplicates
                vessel.OnFlyByWire += OnFlyByWire;
            }
            controlledVessel = vessel;

            return false;
        }

        public int GetImportance()
        {
            if (part.State == PartStates.DEAD)
            {
                return 0;
            }
            else
            {
                return GetInstanceID();
            }
        }

        public int CompareTo(MechJebCore other)
        {
            if (other == null) return 1;
            return GetImportance().CompareTo(other.GetImportance());
        }

        public T GetComputerModule<T>() where T : ComputerModule
        {
            return (T)computerModules.FirstOrDefault(m => m is T); //returns null if no matches
        }

        public List<T> GetComputerModules<T>() where T : ComputerModule
        {
            return computerModules.FindAll(a => a is T).Cast<T>().ToList();
        }

        public ComputerModule GetComputerModule(string type)
        {
            return computerModules.FirstOrDefault(a => a.GetType().Name.ToLowerInvariant() == type.ToLowerInvariant()); //null if none
        }

        public void AddComputerModule(ComputerModule module)
        {
            computerModules.Add(module);
            modulesUpdated = true;
        }

        public void AddComputerModuleLater(ComputerModule module)
        {
            //The actual loading is delayed to FixedUpdate because AddComputerModule can get called inside a foreach loop
            //over the modules, and modifying the list during the loop will cause an exception. Maybe there is a better
            //way to deal with this?
            modulesToLoad.Add(module);
        }

        void LoadDelayedModules()
        {
            if (modulesToLoad.Count > 0)
            {
                computerModules.AddRange(modulesToLoad);
                modulesUpdated = true;
                modulesToLoad.Clear();
            }
        }

        public void RemoveComputerModule(ComputerModule module)
        {
            computerModules.Remove(module);
            modulesUpdated = true;
        }

        public void ReloadAllComputerModules()
        {
            //Dispose of all the existing computer modules
            foreach (ComputerModule module in computerModules) module.OnDestroy();
            computerModules.Clear();

            if (vessel != null) vessel.OnFlyByWire -= OnFlyByWire;
            controlledVessel = null;

            //Start fresh
            OnLoad(null);
            OnStart(HighLogic.LoadedSceneIsEditor ? PartModule.StartState.Editor : PartModule.StartState.Flying);
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == PartModule.StartState.None) return; //don't do anything when we start up in the loading screen

            //OnLoad doesn't get called for parts created in editor, so do that manually so 
            //that we can load global settings.
            //However, if you press ctrl-Z, a new PartModule object gets created, on which the
            //game DOES call OnLoad, and then OnStart. So before calling OnLoad from OnStart,
            //check whether we have loaded any computer modules.
            if (state == StartState.Editor && computerModules.Count == 0)
            {
                OnLoad(null);
            }

            lastSettingsSaveTime = Time.time;

            foreach (ComputerModule module in computerModules)
            {
                //especially important to wrap OnStart in a try-catch so that a failure in one module
                //doesn't prevent others from initializing.
                try
                {
                    module.OnStart(state);
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnStart: " + e);
                }
            }

            if (vessel != null)
            {
                vessel.OnFlyByWire -= OnFlyByWire; //just a safety precaution to avoid duplicates
                vessel.OnFlyByWire += OnFlyByWire;
                controlledVessel = vessel;
            }
        }

        public override void OnActive()
        {
            foreach (ComputerModule module in computerModules)
            {
                try
                {
                    module.OnActive();
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnActive: " + e);
                }
            }
        }

        public override void OnInactive()
        {
            foreach (ComputerModule module in computerModules)
            {
                try
                {
                    module.OnInactive();
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnInactive: " + e);
                }
            }
        }

        public override void OnAwake()
        {
            foreach (ComputerModule module in computerModules)
            {
                try
                {
                    module.OnAwake();
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnAwake: " + e);
                }
            }
        }

        public void FixedUpdate()
        {
            LoadDelayedModules();

            CheckControlledVessel(); //make sure our onFlyByWire callback is registered with the right vessel

            if (this != vessel.GetMasterMechJeb() || (HighLogic.LoadedSceneIsFlight && !vessel.isActiveVessel))
            {
                wasMasterAndFocus = false;
            }

            if (this != vessel.GetMasterMechJeb())
            {
                return;
            }

            if (!wasMasterAndFocus && (HighLogic.LoadedSceneIsEditor || vessel.isActiveVessel))
            {
                if (HighLogic.LoadedSceneIsFlight && (lastFocus != null) && lastFocus.loaded && (lastFocus.GetMasterMechJeb() != null))
                {
                    print("Focus changed! Forcing " + lastFocus.vesselName + " to save");
                    lastFocus.GetMasterMechJeb().OnSave(null);
                }

                OnLoad(null); // Force Global reload

                wasMasterAndFocus = true;
                lastFocus = vessel;
            }

            if (modulesUpdated)
            {
                computerModules.Sort();
                modulesUpdated = false;
            }

            if (vessel == null) return; //don't run ComputerModules' OnFixedUpdate in editor

            vesselState.Update(vessel);

            foreach (ComputerModule module in computerModules)
            {
                try
                {
                    if (module.enabled) module.OnFixedUpdate();
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnFixedUpdate: " + e);
                }
            }
        }

        public void Update()
        {
            if (renderingManager == null)
            {
                renderingManager = (RenderingManager)GameObject.FindObjectOfType(typeof(RenderingManager));
            }
            if (HighLogic.LoadedSceneIsFlight && renderingManager != null)
            {
                if (renderingManager.uiElementsToDisable.Length >= 1) showGui = renderingManager.uiElementsToDisable[0].activeSelf;
            }

            if (this != vessel.GetMasterMechJeb())
            {
                return;
            }

            if (modulesUpdated)
            {
                computerModules.Sort();
                modulesUpdated = false;
            }

            if (Input.GetKeyDown(KeyCode.V) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                MechJebModuleCustomWindowEditor windowEditor = GetComputerModule<MechJebModuleCustomWindowEditor>();
                if (windowEditor != null) windowEditor.CreateWindowFromSharingString(MuUtils.SystemClipboard);
            }

            //periodically save settings in case we quit unexpectedly
            if (HighLogic.LoadedSceneIsEditor || vessel.isActiveVessel)
            {
                if (Time.time > lastSettingsSaveTime + 5)
                {
                    //Debug.Log("MechJeb doing periodic settings save");
                    OnSave(null);
                    lastSettingsSaveTime = Time.time;
                }
            }

            if (vessel == null) return; //don't run ComputerModules' OnUpdate in editor

            foreach (ComputerModule module in computerModules)
            {
                try
                {
                    if (module.enabled) module.OnUpdate();
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnUpdate: " + e);
                }
            }
        }

        void LoadComputerModules()
        {
            if (moduleRegistry == null)
            {
                moduleRegistry = (from ass in AppDomain.CurrentDomain.GetAssemblies() from t in ass.GetTypes() where t.IsSubclassOf(typeof(ComputerModule)) select t).ToList();
            }

            Version v = Assembly.GetAssembly(typeof(MechJebCore)).GetName().Version;
            version = v.Major.ToString() + "." + v.Minor.ToString() + "." + v.Build.ToString();

            foreach (Type t in moduleRegistry)
            {
                if ((t != typeof(ComputerModule)) && (t != typeof(DisplayModule) && (t != typeof(MechJebModuleCustomInfoWindow)))
                    && !blacklist.Contains(t.Name) && (GetComputerModule(t.Name) == null))
                {
                    AddComputerModule((ComputerModule)(t.GetConstructor(new Type[] { typeof(MechJebCore) }).Invoke(new object[] { this })));
                }
            }

            attitude = GetComputerModule<MechJebModuleAttitudeController>();
            staging = GetComputerModule<MechJebModuleStagingController>();
            thrust = GetComputerModule<MechJebModuleThrustController>();
            target = GetComputerModule<MechJebModuleTargetController>();
            warp = GetComputerModule<MechJebModuleWarpController>();
            rcs = GetComputerModule<MechJebModuleRCSController>();
            rcsbal = GetComputerModule<MechJebModuleRCSBalancer>();
            rover = GetComputerModule<MechJebModuleRoverController>();
            node = GetComputerModule<MechJebModuleNodeExecutor>();
        }

        public override void OnLoad(ConfigNode sfsNode)
        {
            if (GuiUtils.skin == null)
            {
                //GuiUtils.skin = new GUISkin();
                GameObject zombieGUILoader = new GameObject("zombieGUILoader", typeof(ZombieGUILoader));
            }
            try
            {
                bool generateDefaultWindows = false;

                base.OnLoad(sfsNode); //is this necessary?

                LoadComputerModules();

                ConfigNode global = new ConfigNode("MechJebGlobalSettings");
                if (File.Exists<MechJebCore>("mechjeb_settings_global.cfg"))
                {
                    try
                    {
                        global = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_global.cfg"));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJebCore.OnLoad caught an exception trying to load mechjeb_settings_global.cfg: " + e);
                        generateDefaultWindows = true;
                    }
                }
                else
                {
                    generateDefaultWindows = true;
                }

                ConfigNode type = new ConfigNode("MechJebTypeSettings");
                if ((vessel != null) && File.Exists<MechJebCore>("mechjeb_settings_type_" + vessel.vesselName + ".cfg"))
                {
                    try
                    {
                        type = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_type_" + vessel.vesselName + ".cfg"));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJebCore.OnLoad caught an exception trying to load mechjeb_settings_type_" + vessel.vesselName + ".cfg: " + e);
                    }
                }

                ConfigNode local = new ConfigNode("MechJebLocalSettings");
                if (sfsNode != null && sfsNode.HasNode("MechJebLocalSettings"))
                {
                    local = sfsNode.GetNode("MechJebLocalSettings");
                }
                else if (sfsNode == null) // capture current Local settings
                {
                    foreach (ComputerModule module in computerModules)
                    {
                        try
                        {
                            module.OnSave(local.AddNode(module.GetType().Name), null, null);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnSave: " + e);
                        }
                    }
                }

                /*Debug.Log("OnLoad: loading from");
                Debug.Log("Local:");
                Debug.Log(local.ToString());
                Debug.Log("Type:");
                Debug.Log(type.ToString());
                Debug.Log("Global:");
                Debug.Log(global.ToString());*/

                // Remove any currently loaded custom windows
                MechJebModuleCustomInfoWindow win;
                while ((win = GetComputerModule<MechJebModuleCustomInfoWindow>()) != null)
                {
                    RemoveComputerModule(win);
                }

                foreach (ComputerModule module in computerModules)
                {
                    try
                    {
                        string name = module.GetType().Name;
                        ConfigNode moduleLocal = local.HasNode(name) ? local.GetNode(name) : null;
                        ConfigNode moduleType = type.HasNode(name) ? type.GetNode(name) : null;
                        ConfigNode moduleGlobal = global.HasNode(name) ? global.GetNode(name) : null;
                        module.OnLoad(moduleLocal, moduleType, moduleGlobal);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnLoad: " + e);
                    }
                }

                LoadDelayedModules();

                if (generateDefaultWindows)
                {
                    GetComputerModule<MechJebModuleCustomWindowEditor>().AddDefaultWindows();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("MechJeb caught exception in core OnLoad: " + e);
            }
        }

        public override void OnSave(ConfigNode sfsNode)
        {
            //we have nothing worth saving if we're outside the editor or flight scenes:
            if (!(HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)) return;

            // Only Masters can save
            if (this != vessel.GetMasterMechJeb()) return;

            //KSP calls OnSave *before* OnLoad when the first command pod is created in the editor. 
            //Defend against saving empty settings.
            if (computerModules.Count == 0) return;

            try
            {
                //Add any to-be-loaded modules so they get saved properly
                LoadDelayedModules();

                // base.OnSave(sfsNode); //is this necessary?

                ConfigNode local = new ConfigNode("MechJebLocalSettings");
                ConfigNode type = new ConfigNode("MechJebTypeSettings");
                ConfigNode global = new ConfigNode("MechJebGlobalSettings");

                foreach (ComputerModule module in computerModules)
                {
                    try
                    {
                        string name = module.GetType().Name;
                        module.OnSave(local.AddNode(name), type.AddNode(name), global.AddNode(name));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnSave: " + e);
                    }
                }

                /*Debug.Log("OnSave:");
                Debug.Log("Local:");
                Debug.Log(local.ToString());
                Debug.Log("Type:");
                Debug.Log(type.ToString());
                Debug.Log("Global:");
                Debug.Log(global.ToString());*/

                if (sfsNode != null) sfsNode.nodes.Add(local);

                string vesselName = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.shipNameField.text : vessel.vesselName);
                type.Save(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_type_" + vesselName + ".cfg"));

                if (lastFocus == vessel)
                {
                    global.Save(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_global.cfg"));
                }
            }
            catch (Exception e)
            {
                Debug.LogError("MechJeb caught exception in core OnSave: " + e);
            }
        }

        public void OnDestroy()
        {
            if (this == vessel.GetMasterMechJeb() && (HighLogic.LoadedSceneIsEditor || vessel.isActiveVessel))
            {
                OnSave(null);
            }

            foreach (ComputerModule module in computerModules)
            {
                try
                {
                    module.OnDestroy();
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnDestroy: " + e);
                }
            }
            if (vessel != null)
            {
                vessel.OnFlyByWire -= OnFlyByWire;
            }
            controlledVessel = null;
        }

        private void OnFlyByWire(FlightCtrlState s)
        {
            if (!CheckControlledVessel() || this != vessel.GetMasterMechJeb())
            {
                return;
            }

            Drive(s);

            CheckFlightCtrlState(s);

            if (vessel == FlightGlobals.ActiveVessel)
            {
                FlightInputHandler.state.mainThrottle = s.mainThrottle; //so that the on-screen throttle gauge reflects the autopilot throttle
            }
        }

        private void Drive(FlightCtrlState s)
        {
            if (this == vessel.GetMasterMechJeb())
            {
                foreach (ComputerModule module in computerModules)
                {
                    try
                    {
                        if (module.enabled) module.Drive(s);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in Drive: " + e);
                    }
                }
            }
        }

        //Setting FlightCtrlState fields to bad values can really mess up the game.
        //Here we do some error checking.
        public void CheckFlightCtrlState(FlightCtrlState s)
        {
            if (float.IsNaN(s.mainThrottle)) s.mainThrottle = 0;
            if (float.IsNaN(s.yaw)) s.yaw = 0;
            if (float.IsNaN(s.pitch)) s.pitch = 0;
            if (float.IsNaN(s.roll)) s.roll = 0;
            if (float.IsNaN(s.X)) s.X = 0;
            if (float.IsNaN(s.Y)) s.Y = 0;
            if (float.IsNaN(s.Z)) s.Z = 0;

            s.mainThrottle = Mathf.Clamp01(s.mainThrottle);
            s.yaw = Mathf.Clamp(s.yaw, -1, 1);
            s.pitch = Mathf.Clamp(s.pitch, -1, 1);
            s.roll = Mathf.Clamp(s.roll, -1, 1);
            s.X = Mathf.Clamp(s.X, -1, 1);
            s.Y = Mathf.Clamp(s.Y, -1, 1);
            s.Z = Mathf.Clamp(s.Z, -1, 1);
        }

        private void OnGUI()
        {
            if (!showGui) return;

            GuiUtils.CheckSkin();

            GUI.skin = GuiUtils.skin;

            if (this == vessel.GetMasterMechJeb() &&
                ((HighLogic.LoadedSceneIsEditor) || ((FlightGlobals.ready) && (vessel == FlightGlobals.ActiveVessel) && (part.State != PartStates.DEAD))))
            {
                foreach (DisplayModule module in GetComputerModules<DisplayModule>())
                {
                    try
                    {
                        if (module.enabled) module.DrawGUI(HighLogic.LoadedSceneIsEditor);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in DrawGUI: " + e);
                    }
                }

                if (HighLogic.LoadedSceneIsEditor) PreventEditorClickthrough();
            }
        }

        // VAB/SPH description
        public override string GetInfo()
        {
            return "Attitude control by MechJeb™";
        }

        //Lifted this more or less directly from the Kerbal Engineer source. Thanks cybutek!
        void PreventEditorClickthrough()
        {
            bool mouseOverWindow = GuiUtils.MouseIsOverWindow(this);
            if (mouseOverWindow && !EditorLogic.editorLocked)
            {
                EditorLogic.fetch.Lock(true, true, true);
                weLockedEditor = true;
            }
            if (weLockedEditor && !mouseOverWindow && EditorLogic.editorLocked)
            {
                EditorLogic.fetch.Unlock();
            }
            if (!EditorLogic.editorLocked) weLockedEditor = false;
        }
    }
}
