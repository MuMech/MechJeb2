using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using System.Reflection;
using KSP.IO;

namespace MuMech
{
    public class MechJebCore : PartModule, IComparable<MechJebCore>
    {
        private const int windowIDbase = 60606;

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
        public MechJebModuleNodeExecutor node;

        public VesselState vesselState = new VesselState();

        private Vessel controlledVessel; //keep track of which vessel we've added our onFlyByWire callback to

        public string version = "";

        [KSPField(isPersistant = false)]
        public string blacklist = "";

        private bool killThrottle = false;

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

        public void RemoveComputerModule(ComputerModule module)
        {
            computerModules.Remove(module);
            modulesUpdated = true;
        }


        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log("============ONSTART====================");
            Debug.Log("start = " + state);

            if (state == PartModule.StartState.None) return; //don't do anything when we start up in the loading screen

            //OnLoad doesn't get called for parts created in editor, so do that manually so 
            //that we can load global settings:
            if (state == StartState.Editor) OnLoad(null);

            foreach (ComputerModule module in computerModules)
            {
                module.OnStart(state);
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
                module.OnActive();
            }
        }

        public override void OnInactive()
        {
            foreach (ComputerModule module in computerModules)
            {
                module.OnInactive();
            }
        }

        public override void OnAwake()
        {
            foreach (ComputerModule module in computerModules)
            {
                module.OnAwake();
            }
        }

        public void FixedUpdate()
        {
            if (modulesToLoad.Count > 0)
            {
                computerModules.AddRange(modulesToLoad);
                modulesUpdated = true;
                modulesToLoad.Clear();
            }

            CheckControlledVessel(); //make sure our onFlyByWire callback is registered with the right vessel

            if (this != vessel.GetMasterMechJeb())
            {
                return;
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
                if (module.enabled) module.OnFixedUpdate();
            }
        }

        public void Update()
        {
            if (this != vessel.GetMasterMechJeb())
            {
                return;
            }

            if (modulesUpdated)
            {
                computerModules.Sort();
                modulesUpdated = false;
            }

            if (vessel == null) return; //don't run ComputerModules' OnUpdate in editor

            foreach (ComputerModule module in computerModules)
            {
                if (module.enabled) module.OnUpdate();
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
                    && !blacklist.Contains(t.Name))
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
            node = GetComputerModule<MechJebModuleNodeExecutor>();
        }

        public override void OnLoad(ConfigNode configNode)
        {
            Debug.Log("OnLoad called !!!!!!!!!!!!!!!!!");

            base.OnLoad(configNode); //is this necessary?

            LoadComputerModules();

            Debug.Log("LOADED " + computerModules.Count + " MODULES");

            ConfigNode local = new ConfigNode("MechJebLocalSettings");
            if (configNode != null && configNode.HasNode("MechJebLocalSettings"))
            {
                local = configNode.GetNode("MechJebLocalSettings");
            }

            //Todo: load a different file for each vessel type
            ConfigNode type = new ConfigNode("MechJebTypeSettings");
            Debug.Log("type path: " + IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_type.cfg"));
            if (File.Exists<MechJebCore>("mechjeb_settings_type.cfg"))
            {
                try
                {
                    type = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_type.cfg"));
                }
                catch (Exception e)
                {
                    Debug.Log("MechJebCore.OnLoad caught an exception trying to load mechjeb_settings_type.cfg: " + e);
                }
            }

            ConfigNode global = new ConfigNode("MechJebGlobalSettings");
            if (File.Exists<MechJebCore>("mechjeb_settings_global.cfg"))
            {
                try
                {
                    global = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_global.cfg"));
                }
                catch (Exception e)
                {
                    Debug.Log("MechJebCore.OnLoad caught an exception trying to load mechjeb_settings_global.cfg: " + e);
                }
            }

            Debug.Log("OnLoad: loading from");
            Debug.Log("Local:");
            Debug.Log(local.ToString());
            Debug.Log("Type:");
            Debug.Log(type.ToString());
            Debug.Log("Global:");
            Debug.Log(global.ToString());

            foreach (ComputerModule module in computerModules)
            {
                string name = module.GetType().Name;
                ConfigNode moduleLocal = local.HasNode(name) ? local.GetNode(name) : null;
                ConfigNode moduleType = type.HasNode(name) ? type.GetNode(name) : null;
                ConfigNode moduleGlobal = global.HasNode(name) ? global.GetNode(name) : null;
                module.OnLoad(moduleLocal, moduleType, moduleGlobal);
            }
        }

        //Todo: periodically save global and type settings so that e.g. window positions are saved on quitting
        //even if the player doesn't explicitly quicksave.
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node); //is this necessary?

            ConfigNode local = new ConfigNode("MechJebLocalSettings");
            ConfigNode type = new ConfigNode("MechJebTypeSettings");
            ConfigNode global = new ConfigNode("MechJebGlobalSettings");

            foreach (ComputerModule module in computerModules)
            {
                string name = module.GetType().Name;
                module.OnSave(local.AddNode(name), type.AddNode(name), global.AddNode(name));
            }

            Debug.Log("OnSave:");
            Debug.Log("Local:");
            Debug.Log(local.ToString());
            Debug.Log("Type:");
            Debug.Log(type.ToString());
            Debug.Log("Global:");
            Debug.Log(global.ToString());

            node.nodes.Add(local);

            type.Save(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_type.cfg")); //Todo: save a different file for each vessel type.
            global.Save(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_global.cfg"));
        }

        public void OnDestroy()
        {
            foreach (ComputerModule module in computerModules)
            {
                module.OnDestroy();
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

            if (killThrottle)
            {
                s.mainThrottle = 0;
                killThrottle = false;
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
                    if (module.enabled) module.Drive(s);
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

        //A function that modules can call in order to kill the throttle on the next frame.
        //This is useful when disabling an autopilot from FixedUpdate instead of Drive, since
        //FixedUpdate doesn't have access to the FlightCtrlState.
        public void KillThrottle()
        {
            killThrottle = true;
        }


        private void OnGUI()
        {
            if ((HighLogic.LoadedSceneIsEditor) || ((FlightGlobals.ready) && (vessel == FlightGlobals.ActiveVessel) && (part.State != PartStates.DEAD) && (this == vessel.GetMasterMechJeb())))
            {
                int wid = 0;
                foreach (DisplayModule module in GetComputerModules<DisplayModule>())
                {
                    if (module.enabled) module.DrawGUI(windowIDbase + wid, HighLogic.LoadedSceneIsEditor);
                    wid++;
                }
            }

            //Todo: move this out of the core
            //Todo: move this to the post-render queue since as it is it draws on top of windows
            MechJebModuleLandingPredictions p = GetComputerModule<MechJebModuleLandingPredictions>();
            ReentrySimulation.Result result = p.GetResult();
            if (MapView.MapIsEnabled && p.enabled && result != null)
            {
                //GLUtils.DrawPath(p.result.body, p.result.WorldTrajectory(), Color.cyan, false);
                if (result.outcome == ReentrySimulation.Outcome.LANDED)
                {
                    GLUtils.DrawMapViewGroundMarker(result.body, result.endPosition.latitude, result.endPosition.longitude, Color.blue, 60);
                }
                else if (result.outcome == ReentrySimulation.Outcome.AEROBRAKED)
                {
                    Orbit o = MuUtils.OrbitFromStateVectors(result.WorldEndPosition(), result.WorldEndVelocity(), result.body, result.endUT);
                    //GLUtils.DrawOrbit(o, Color.blue);
                }
            }
        }

        // VAB/SPH description
        public override string GetInfo()
        {
            return "Attitude control by MechJeb™";
        }
    }
}
