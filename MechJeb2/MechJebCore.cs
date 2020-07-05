using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KSP.IO;
using System.Diagnostics;
using UnityEngine.Profiling;
using UnityToolbag;
using Debug = UnityEngine.Debug;
using File = KSP.IO.File;
using KSP.Localization;

namespace MuMech
{
    public class MechJebCore : PartModule, IComparable<MechJebCore>
    {
        private List<ComputerModule> unorderedComputerModules = new List<ComputerModule>();
        private List<ComputerModule> modulesToLoad = new List<ComputerModule>();

        private Dictionary<Type, List<ComputerModule>> sortedModules = new Dictionary<Type, List<ComputerModule>>();
        private Dictionary<object, List<DisplayModule>> sortedDisplayModules = new Dictionary<object, List<DisplayModule>>();

        // Reference to the parts base config. See Onload for explanation
        private static Dictionary<string, ConfigNode> savedConfig = new Dictionary<string, ConfigNode>();

        private List<Callback> postDrawQueue = new List<Callback>();

        private static List<Type> moduleRegistry;

        private bool ready = false;

        public MechJebModuleGuidanceController guidance;
        public MechJebModuleAttitudeController attitude;
        public MechJebModuleStagingController staging;
        public MechJebModuleThrustController thrust;
        public MechJebModuleTargetController target;
        public MechJebModuleWarpController warp;
        public MechJebModuleRCSController rcs;
        public MechJebModuleRCSBalancer rcsbal;
        public MechJebModuleRoverController rover;
        public MechJebModuleNodeExecutor node;
        public MechJebModuleSolarPanelController solarpanel;
        public MechJebModuleDeployableAntennaController antennaControl;
        public MechJebModuleLandingAutopilot landing;
        public MechJebModuleSettings settings;
        public MechJebModuleAirplaneAutopilot airplane;
        public MechJebModuleStageStats stageStats;
        public MechJebModuleLogicalStageTracking stageTracking;

        public VesselState vesselState = new VesselState();
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "MechJeb"), UI_Toggle(disabledText = "#MechJeb_Disabled", enabledText = "#MechJeb_Enabled")]//DisabledEnabled
        public bool running = true;
        private Vessel controlledVessel; //keep track of which vessel we've added our onFlyByWire callback to
        public string version = "";
        private bool deactivateControl = false;
        private float currentThrottle = 0f;  // To maintain throttle when controls deactivated
        public MechJebCore MasterMechJeb
        {
            get { return vessel.GetMasterMechJeb(); }
        }

        // Allow other mods to kill MJ ability to control vessel (RemoteTech, RO...)
        public bool DeactivateControl
        {
            get
            {
                MechJebCore mj = vessel.GetMasterMechJeb();
                return mj != null && vessel.GetMasterMechJeb().deactivateControl;
            }
            set
            {
                MechJebCore mj = vessel.GetMasterMechJeb();
                if (mj != null)
                {
                    if (value && !mj.deactivateControl)
                    {
                        controlledVessel.ctrlState.mainThrottle = currentThrottle;
                    }
                    mj.deactivateControl = value;
                }
            }
        }

        [KSPField(isPersistant = false)]
        public string blacklist = "";

        [KSPField]
        public ConfigNode partSettings;

        [KSPField(isPersistant = false)]
        public bool eduMode = false;

        public bool rssMode { get { return settings.rssMode; } }

        public bool ShowGui
        {
            get { return showGui; }
        }

        [KSPAction("#MechJeb_OrbitPrograde")]//Orbit Prograde
        public void OnOrbitProgradeAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.PROGRADE);
        }

        [KSPAction("#MechJeb_OrbitRetrograde")]//Orbit Retrograde
        public void OnOrbitRetrogradeAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.RETROGRADE);
        }

        [KSPAction("#MechJeb_OrbitNormal")]//Orbit Normal
        public void OnOrbitNormalAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.NORMAL_PLUS);
        }

        [KSPAction("#MechJeb_OrbitAntinormal")]//Orbit Antinormal
        public void OnOrbitAntinormalAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.NORMAL_MINUS);
        }

        [KSPAction("#MechJeb_OrbitRadialIn")]//Orbit Radial In
        public void OnOrbitRadialInAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.RADIAL_MINUS);
        }

        [KSPAction("#MechJeb_OrbitRadialOut")]//Orbit Radial Out
        public void OnOrbitRadialOutAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.RADIAL_PLUS);
        }

        [KSPAction("#MechJeb_OrbitKillRotation")]//Orbit Kill Rotation
        public void OnKillRotationAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.KILLROT);
        }

        [KSPAction("#MechJeb_DeactivateSmartACS")]//Deactivate SmartACS
        public void OnDeactivateSmartASSAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.OFF);
        }

        [KSPAction("#MechJeb_LandSomewhere")]//Land somewhere
        public void OnLandsomewhereAction(KSPActionParam param)
        {

            LandSomewhere();
        }

        [KSPAction("#MechJeb_LandatKSC")]//Land at KSC
        public void OnLandTargetAction(KSPActionParam param)
        {
            LandTarget();
        }


        private void LandTarget()
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb != null)
            {
                MechJebModuleLandingGuidance moduleLandingGuidance = GetComputerModule<MechJebModuleLandingGuidance>();

                moduleLandingGuidance?.SetAndLandTargetKSC();
            }

        }

        private void LandSomewhere()
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();
            if (masterMechJeb != null)
            {
                MechJebModuleLandingGuidance moduleLandingGuidance = GetComputerModule<MechJebModuleLandingGuidance>();

                moduleLandingGuidance?.LandSomewhere();

            }
        }

        private void EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target target)
        {
            MechJebCore masterMechJeb = this.vessel.GetMasterMechJeb();

            if (masterMechJeb != null)
            {
                MechJebModuleSmartASS masterSmartASS = masterMechJeb.GetComputerModule<MechJebModuleSmartASS>();

                if (masterSmartASS != null && !masterSmartASS.hidden)
                {
                    masterSmartASS.mode = MechJebModuleSmartASS.Mode.ORBITAL;
                    masterSmartASS.target = target;

                    masterSmartASS.Engage();
                }
                else
                {
                    Debug.LogError(Localizer.Format("#MechJeb_LogError_msg1"));//"MechJeb couldn't find MechJebModuleSmartASS for orbital control via action group."
                }
            }
            else
            {
                Debug.LogError(Localizer.Format("#MechJeb_LogError_msg0"));//"MechJeb couldn't find the master MechJeb module for the current vessel."
            }
        }

        [KSPAction("#MechJeb_PANIC")]//PANIC!
        public void OnPanicAction(KSPActionParam param)
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();
            if (masterMechJeb != null)
            {
                MechJebModuleTranslatron moduleTranslatron = masterMechJeb.GetComputerModule<MechJebModuleTranslatron>();
                if (moduleTranslatron != null && !moduleTranslatron.hidden)
                {
                    moduleTranslatron.PanicSwitch();
                }
            }
        }

        [KSPAction("#MechJeb_TranslatronOFF")]//Translatron OFF
        public void OnTranslatronOffAction(KSPActionParam param)
        {
            EngageTranslatronControl(MechJebModuleThrustController.TMode.OFF);
        }

        [KSPAction("#MechJeb_TranslatronKeepVert")]//Translatron Keep Vert
        public void OnTranslatronKeepVertAction(KSPActionParam param)
        {
            EngageTranslatronControl(MechJebModuleThrustController.TMode.KEEP_VERTICAL);
        }

        [KSPAction("#MechJeb_TranslatronZerospeed")]//Translatron Zero speed
        public void OnTranslatronZeroSpeedAction(KSPActionParam param)
        {
            SetTranslatronSpeed(0);
        }

        [KSPAction("#MechJeb_TranslatronPlusspeed")]//Translatron +1 speed
        public void OnTranslatronPlusOneSpeedAction(KSPActionParam param)
        {
            SetTranslatronSpeed(1, true);
        }

        [KSPAction("#MechJeb_TranslatronMinusspeed")]//Translatron -1 speed
        public void OnTranslatronMinusOneSpeedAction(KSPActionParam param)
        {
            SetTranslatronSpeed(-1, true);
        }

        [KSPAction("#MechJeb_TranslatronToggleHS")]//Translatron Toggle H/S
        public void OnTranslatronToggleHSAction(KSPActionParam param)
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb != null)
            {
                MechJebModuleTranslatron moduleTranslatron = masterMechJeb.GetComputerModule<MechJebModuleTranslatron>();

                if (moduleTranslatron != null && !moduleTranslatron.hidden)
                {
                    thrust.trans_kill_h = !thrust.trans_kill_h;
                }
                else
                {
                    Debug.LogError(Localizer.Format("#MechJeb_LogError_msg2"));//"MechJeb couldn't find MechJebModuleTranslatron for translatron control via action group."
                }
            }
            else
            {
                Debug.LogError("MechJeb couldn't find the master MechJeb module for the current vessel.");
            }
        }

        [KSPAction("#MechJeb_AscentAPtoggle")]//Ascent AP toggle
        public void OnAscentAPToggleAction(KSPActionParam param)
        {

            MechJebModuleAscentAutopilot autopilot = GetComputerModule<MechJebModuleAscentAutopilot>();
            MechJebModuleAscentGuidance ascentGuidance = GetComputerModule<MechJebModuleAscentGuidance>();


            if (autopilot == null || ascentGuidance == null)
                return;

            if (autopilot.enabled)
            {
                    autopilot.users.Remove(ascentGuidance);
            }
            else
            {
                    autopilot.users.Add(ascentGuidance);
            }
        }


        private void EngageTranslatronControl(MechJebModuleThrustController.TMode mode)
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb != null)
            {
                MechJebModuleTranslatron moduleTranslatron = masterMechJeb.GetComputerModule<MechJebModuleTranslatron>();

                if (moduleTranslatron != null && !moduleTranslatron.hidden)
                {
                    if ((thrust.users.Count > 1) && !thrust.users.Contains(moduleTranslatron))
                        return;

                    moduleTranslatron.SetMode(mode);
                }
                else
                {
                    Debug.LogError("MechJeb couldn't find MechJebModuleTranslatron for translatron control via action group.");
                }
            }
            else
            {
                Debug.LogError("MechJeb couldn't find the master MechJeb module for the current vessel.");
            }
        }

        private void SetTranslatronSpeed(float speed, bool relative = false)
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb != null)
            {
                MechJebModuleTranslatron moduleTranslatron = masterMechJeb.GetComputerModule<MechJebModuleTranslatron>();

                if (moduleTranslatron != null && !moduleTranslatron.hidden)
                {
                    thrust.trans_spd_act = (relative ? thrust.trans_spd_act : 0) + speed;
                }
                else
                {
                    Debug.LogError("MechJeb couldn't find MechJebModuleTranslatron for translatron control via action group.");
                }
            }
            else
            {
                Debug.LogError("MechJeb couldn't find the master MechJeb module for the current vessel.");
            }
        }

        private bool weLockedInputs = false;
        private float lastSettingsSaveTime;
        private bool showGui = true;
        protected bool wasMasterAndFocus = false;
        protected static Vessel lastFocus = null;

        public bool someModuleAreLocked = false; // True if any module was locked by the R&D system

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
            for (int i = 0; i < unorderedComputerModules.Count; i++)
            {
                ComputerModule pm = unorderedComputerModules[i];
                T module = pm as T;
                if (module != null)
                    return module;
            }
            return null;
        }

        // The pure generic version was creating too much garbage
        public List<ComputerModule> GetComputerModules<T>() where T : ComputerModule
        {
            Type key = typeof(T);
            List<ComputerModule> value;
            if (sortedModules.TryGetValue(key, out value))
                return value;
            sortedModules[key] = value = unorderedComputerModules.OfType<T>().Cast<ComputerModule>().OrderBy(m => m).ToList();
            return value;
        }

        // Added because the generic version eats memory like candy when casting from ComputerModule to DisplayModule (.Cast<T>())
        public List<DisplayModule> GetDisplayModules(IComparer<DisplayModule> comparer)
        {
            List<DisplayModule> value;
            if (sortedDisplayModules.TryGetValue(comparer, out value))
                return value;
            sortedDisplayModules[comparer] = value = unorderedComputerModules.OfType<DisplayModule>().OrderBy(m => m, comparer).ToList();
            return value;
        }

        public ComputerModule GetComputerModule(string type)
        {
            return unorderedComputerModules.FirstOrDefault(a => a.GetType().Name.ToLowerInvariant() == type.ToLowerInvariant()); //null if none
        }

        public void AddComputerModule(ComputerModule module)
        {
            unorderedComputerModules.Add(module);
            ClearModulesCache();
        }

        private void ClearModulesCache()
        {
            sortedModules.Clear();
            sortedDisplayModules.Clear();
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
                unorderedComputerModules.AddRange(modulesToLoad);
                modulesToLoad.Clear();
                ClearModulesCache();
            }
        }

        public void RemoveComputerModule(ComputerModule module)
        {
            unorderedComputerModules.Remove(module);
            ClearModulesCache();
        }

        public void ReloadAllComputerModules()
        {
            //Dispose of all the existing computer modules
            foreach (ComputerModule module in unorderedComputerModules) module.OnDestroy();
            unorderedComputerModules.Clear();
            ClearModulesCache();

            if (vessel != null) vessel.OnFlyByWire -= OnFlyByWire;
            controlledVessel = null;

            //Start fresh
            OnLoad(null);
            OnStart(HighLogic.LoadedSceneIsEditor ? PartModule.StartState.Editor : PartModule.StartState.Flying);
        }



        public override void OnStart(PartModule.StartState state)
        {
            if (HighLogic.LoadedSceneIsEditor)
                ready = true;
            if (state == PartModule.StartState.None) return; //don't do anything when we start up in the loading screen

            //OnLoad doesn't get called for parts created in editor, so do that manually so
            //that we can load global settings.
            //However, if you press ctrl-Z, a new PartModule object gets created, on which the
            //game DOES call OnLoad, and then OnStart. So before calling OnLoad from OnStart,
            //check whether we have loaded any computer modules.

            //if (state == StartState.Editor && computerModules.Count == 0)
            // Seems to happend when launching without comming from the VAB too.
            if (unorderedComputerModules.Count == 0)
            {
                OnLoad(null);
            }

            GameEvents.onShowUI.Add(OnShowGUI);
            GameEvents.onHideUI.Add(OnHideGUI);
            GameEvents.onVesselChange.Add(UnlockControl);

            lastSettingsSaveTime = Time.time;

            foreach (ComputerModule module in GetComputerModules<ComputerModule>())
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

            if (vessel != null && this != vessel.GetMasterMechJeb())
            {
                vessel.OnFlyByWire -= OnFlyByWire; //just a safety precaution to avoid duplicates
                vessel.OnFlyByWire += OnFlyByWire;
                controlledVessel = vessel;
            }
        }

        public override void OnActive()
        {
            foreach (ComputerModule module in GetComputerModules<ComputerModule>())
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
            foreach (ComputerModule module in GetComputerModules<ComputerModule>())
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
            Dispatcher.CreateDispatcher();

            foreach (ComputerModule module in GetComputerModules<ComputerModule>())
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
            if (!FlightGlobals.ready)
                return;

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
            Profiler.BeginSample("MechJebCore.FixedUpdate");

            if (!wasMasterAndFocus && (HighLogic.LoadedSceneIsEditor || vessel.isActiveVessel))
            {
                if (HighLogic.LoadedSceneIsFlight && (lastFocus != null) && lastFocus.loaded && (lastFocus.GetMasterMechJeb() != null))
                {
                    print("Focus changed! Forcing " + lastFocus.vesselName + " to save");
                    lastFocus.GetMasterMechJeb().OnSave(null);
                }

                // Clear the modules cache
                ClearModulesCache();

                OnLoad(null); // Force Global reload

                wasMasterAndFocus = true;
                lastFocus = vessel;
            }

            if (vessel == null)
            {
                Profiler.EndSample();
                return; //don't run ComputerModules' OnFixedUpdate in editor
            }

            Profiler.BeginSample("vesselState");
            ready = vesselState.Update(vessel);
            Profiler.EndSample();

            foreach (ComputerModule module in GetComputerModules<ComputerModule>())
            {
                Profiler.BeginSample(module.profilerName);
                try
                {
                    if (module.enabled)
                    {
                        module.OnFixedUpdate();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnFixedUpdate: " + e);
                }
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }


        private bool NeedToSave()
        {
            bool needToSave = false;
            foreach (ComputerModule module in GetComputerModules<ComputerModule>())
            {
                //if (module.dirty)
                //    print(module.profilerName + " is dirty");
                needToSave |= module.dirty;
            }
            return needToSave;
        }

        public void Update()
        {
            if (this != vessel.GetMasterMechJeb() || (!FlightGlobals.ready && HighLogic.LoadedSceneIsFlight) || !ready)
            {
                return;
            }
            Profiler.BeginSample("MechJebCore.Update");

            if (Input.GetKeyDown(KeyCode.V) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                MechJebModuleCustomWindowEditor windowEditor = GetComputerModule<MechJebModuleCustomWindowEditor>();
                if (windowEditor != null) windowEditor.CreateWindowFromSharingString(MuUtils.SystemClipboard);
            }

            //periodically save settings in case we quit unexpectedly
            Profiler.BeginSample("MechJebCore.Update.OnSave");
            if (HighLogic.LoadedSceneIsEditor || (vessel != null && vessel.isActiveVessel))
            {
                if (Time.time > lastSettingsSaveTime + 5)
                {
                    if (NeedToSave())
                    {
                        //print("Periodic settings save");
                        OnSave(null);
                    }
                    lastSettingsSaveTime = Time.time;
                }
            }
            Profiler.EndSample();

            if (ResearchAndDevelopment.Instance != null && unorderedComputerModules.Any(a => !a.unlockChecked))
            {
                foreach (ComputerModule module in GetComputerModules<ComputerModule>())
                {
                    try
                    {
                        module.UnlockCheck();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in UnlockCheck: " + e);
                    }
                }
            }

            Profiler.BeginSample("OnMenuUpdate");
            GetComputerModule<MechJebModuleMenu>().OnMenuUpdate(); // Allow the menu movement, even while in Editor
            Profiler.EndSample();

            if (vessel == null)
            {
                Profiler.EndSample();
                return; //don't run ComputerModules' OnUpdate in editor
            }

            foreach (ComputerModule module in GetComputerModules<ComputerModule>())
            {
                Profiler.BeginSample(module.profilerName);
                try
                {
                    if (module.enabled) module.OnUpdate();
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnUpdate: " + e);
                }
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        void LoadComputerModules()
        {
            if (moduleRegistry == null)
            {
                moduleRegistry = new List<Type>();
                foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var module in (from t in ass.GetTypes() where t.IsSubclassOf(typeof(ComputerModule)) && !t.IsAbstract select t).ToList())
                        {
                            moduleRegistry.Add(module);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb moduleRegistry creation threw an exception in LoadComputerModules loading " + ass.FullName + ": " + e);
                    }
                }
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            // Mono compiler is stupid and use AssemblyVersion for the AssemblyFileVersion
            // So we use an other field to store the dev build number ...
            Attribute[] attributes = Attribute.GetCustomAttributes(assembly, typeof(AssemblyInformationalVersionAttribute));
            string dev_version = "";
            if (attributes.Length > 0)
            {
                dev_version = ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
            }

            if (dev_version == "")
                version = string.Format("{0}.{1}.{2}", fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart);
            else
                version = dev_version;

            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                print("Loading Mechjeb " + version);

            try
            {
                foreach (Type t in moduleRegistry)
                {
                    if ((t != typeof(ComputerModule)) && (t != typeof(DisplayModule) && (t != typeof(MechJebModuleCustomInfoWindow)))
                        && (t != typeof(AutopilotModule))
                        && !blacklist.Contains(t.Name) && (GetComputerModule(t.Name) == null))
                    {
                        ConstructorInfo constructorInfo = t.GetConstructor(new[] { typeof(MechJebCore) });
                        if (constructorInfo != null)
                            AddComputerModule((ComputerModule)(constructorInfo.Invoke(new object[] { this })));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("MechJeb moduleRegistry loading threw an exception in LoadComputerModules: " + e);
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
            solarpanel = GetComputerModule<MechJebModuleSolarPanelController>();
            antennaControl = GetComputerModule<MechJebModuleDeployableAntennaController>();
            landing = GetComputerModule<MechJebModuleLandingAutopilot>();
            settings = GetComputerModule<MechJebModuleSettings>();
            airplane = GetComputerModule<MechJebModuleAirplaneAutopilot>();
            guidance = GetComputerModule<MechJebModuleGuidanceController>();
            stageStats = GetComputerModule<MechJebModuleStageStats>();
            stageTracking = GetComputerModule<MechJebModuleLogicalStageTracking>();
        }

        public override void OnLoad(ConfigNode sfsNode)
        {
            if (GuiUtils.skin == null)
            {
                //GuiUtils.skin = new GUISkin();
                new GameObject("zombieGUILoader", typeof(ZombieGUILoader));
            }
            try
            {
                bool generateDefaultWindows = false;

                base.OnLoad(sfsNode); //is this necessary?


                // With the Unity 4.6 upgrade of KSP 1.0 we inherited a serialization problem
                // with object with high depth like config nodes
                // so the partmodule config node passed was not ok.
                // So we use a static dir to save the part config node.
                if (!savedConfig.ContainsKey(part.name))
                {
                    if (HighLogic.LoadedScene == GameScenes.LOADING)
                        savedConfig.Add(part.name, sfsNode);
                }
                else
                {
                    partSettings = savedConfig[part.name];
                }

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
                string vesselName = vessel != null ? string.Join("_", vessel.vesselName.Split(System.IO.Path.GetInvalidFileNameChars())) : ""; // Strip illegal char from the filename
                if ((vessel != null) && File.Exists<MechJebCore>("mechjeb_settings_type_" + vesselName + ".cfg"))
                {
                    try
                    {
                        type = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_type_" + vesselName + ".cfg"));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJebCore.OnLoad caught an exception trying to load mechjeb_settings_type_" + vesselName + ".cfg: " + e);
                    }
                }

                ConfigNode local = new ConfigNode("MechJebLocalSettings");
                if (sfsNode != null && sfsNode.HasNode("MechJebLocalSettings"))
                {
                    local = sfsNode.GetNode("MechJebLocalSettings");
                }
                else if (partSettings != null && partSettings.HasNode("MechJebLocalSettings"))
                {
                    local = partSettings.GetNode("MechJebLocalSettings");
                }
                else if (sfsNode == null) // capture current Local settings
                {
                    foreach (ComputerModule module in GetComputerModules<ComputerModule>())
                    {
                        try
                        {
                            module.OnSave(local.AddNode(module.GetType().Name), null, null);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnLoad: " + e);
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

                foreach (ComputerModule module in GetComputerModules<ComputerModule>())
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
            catch (ReflectionTypeLoadException ex)
            {
                Debug.LogError("MechJeb caught a ReflectionTypeLoadException. Those DLL are not built for this KSP version:");
                var brokenAssembly = ex.Types.Where(x => x != null).Select(x => x.Assembly).Distinct();
                foreach (Assembly assembly in brokenAssembly)
                {
                    Debug.LogError(assembly.GetName().Name + " " + assembly.GetName().Version + " " + assembly.Location.Remove(0, Path.GetFullPath(KSPUtil.ApplicationRootPath).Length));
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
            if (unorderedComputerModules.Count == 0) return;

            // .23 added a call to OnSave for undocking/decoupling vessel before they are properly init ...
            if (HighLogic.LoadedSceneIsFlight && vessel != null && vessel.vesselName == null)
                return;

            Profiler.BeginSample("MechJebCore.OnSave");
            try
            {
                //Add any to-be-loaded modules so they get saved properly
                Profiler.BeginSample("MechJebCore.OnSave.LoadDelayedModules");
                LoadDelayedModules();

                // base.OnSave(sfsNode); //is this necessary?
                Profiler.EndSample();
                Profiler.BeginSample("MechJebCore.OnSave.ConfigNode");
                // We have 2 type of saves:
                // - stock module save where sfsNode is provided and where we save the module inside the ship
                // - MechJeb global/type save where we do not save the module but save MJ global/type configs
                ConfigNode local  = sfsNode == null ? null : new ConfigNode("MechJebLocalSettings");
                ConfigNode type   = sfsNode != null ? null : new ConfigNode("MechJebTypeSettings");
                ConfigNode global = sfsNode != null ? null : new ConfigNode("MechJebGlobalSettings");
                Profiler.EndSample();
                Profiler.BeginSample("MechJebCore.OnSave.loop");
                foreach (ComputerModule module in GetComputerModules<ComputerModule>())
                {
                    Profiler.BeginSample(module.profilerName);
                    try
                    {
                        string name = module.GetType().Name;
                        module.OnSave(local?.AddNode(name), type?.AddNode(name), global?.AddNode(name));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnSave: " + e);
                    }
                    Profiler.EndSample();
                }

                /*Debug.Log("OnSave:");
                Debug.Log("Local:");
                Debug.Log(local.ToString());
                Debug.Log("Type:");
                Debug.Log(type.ToString());
                Debug.Log("Global:");
                Debug.Log(global.ToString());*/
                Profiler.EndSample();
                Profiler.BeginSample("MechJebCore.OnSave.sfsNode");
                if (sfsNode != null) sfsNode.nodes.Add(local);
                Profiler.EndSample();
                Profiler.BeginSample("MechJebCore.OnSave.type");
                // The EDITOR => FLIGHT transition is annoying to handle. OnDestroy is called when HighLogic.LoadedSceneIsEditor is already false
                // So we don't save in that case, which is not that bad since nearly nothing use vessel settings in the editor.
                if (type != null && (vessel != null || (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null)))
                {
                    string vesselName = (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch ? EditorLogic.fetch.shipNameField.text : vessel.vesselName);
                    vesselName = string.Join("_", vesselName.Split(Path.GetInvalidFileNameChars())); // Strip illegal char from the filename
                    type.Save(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_type_" + vesselName + ".cfg"));
                }
                Profiler.EndSample();
                Profiler.BeginSample("MechJebCore.OnSave.global");
                if (global != null && lastFocus == vessel)
                {
                    global.Save(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_global.cfg"));
                }
                Profiler.EndSample();
            }
            catch (Exception e)
            {
                Debug.LogError("MechJeb caught exception in core OnSave: " + e);
            }
            Profiler.EndSample();
        }

        public void OnDestroy()
        {
            if (this == vessel.GetMasterMechJeb() && (vessel == null || vessel.isActiveVessel))
            {
                OnSave(null);
            }

            GameEvents.onShowUI.Remove(OnShowGUI);
            GameEvents.onHideUI.Remove(OnHideGUI);
            GameEvents.onVesselChange.Remove(UnlockControl);

            if (weLockedInputs)
            {
                UnlockControl();
                ManeuverGizmo.HasMouseFocus = false;
            }

            foreach (ComputerModule module in GetComputerModules<ComputerModule>())
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
            if (deactivateControl || !CheckControlledVessel() || this != vessel.GetMasterMechJeb())
            {
                return;
            }

            Drive(s);

            CheckFlightCtrlState(s);

            // Update current throttle for control deactivation
            currentThrottle = s.mainThrottle;
        }

        private void Drive(FlightCtrlState s)
        {
            Profiler.BeginSample("vesselState");
            ready = vesselState.Update(vessel);
            Profiler.EndSample();

            Profiler.BeginSample("MechJebCore.Drive");
            if (this == vessel.GetMasterMechJeb())
            {
                foreach (ComputerModule module in GetComputerModules<ComputerModule>())
                {
                    Profiler.BeginSample(module.profilerName);
                    try
                    {
                        if (module.enabled) module.Drive(s);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in Drive: " + e);
                    }
                    Profiler.EndSample();
                }
            }
            Profiler.EndSample();
        }

        //Setting FlightCtrlState fields to bad values can really mess up the game.
        //Here we do some error checking.
        private static void CheckFlightCtrlState(FlightCtrlState s)
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

        private void OnShowGUI()
        {
            showGui = true;
        }
        private void OnHideGUI()
        {
            showGui = false;
        }

        private void OnGUI()
        {
            if (!showGui || this != vessel.GetMasterMechJeb() || (!FlightGlobals.ready && HighLogic.LoadedSceneIsFlight)  || !ready) return;

            Profiler.BeginSample("MechJebCore.OnGUI");

            if (HighLogic.LoadedSceneIsEditor || (FlightGlobals.ready && (vessel == FlightGlobals.ActiveVessel) && (part.State != PartStates.DEAD)))
            {
                Matrix4x4 previousGuiMatrix = GUI.matrix;
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(GuiUtils.scale, GuiUtils.scale, 1));

                GuiUtils.ComboBox.DrawGUI();

                GuiUtils.LoadSkin((GuiUtils.SkinType)settings.skinId);

                GUI.skin = GuiUtils.skin;

                foreach (DisplayModule module in GetComputerModules<DisplayModule>())
                {
                    Profiler.BeginSample(module.profilerName);
                    try
                    {
                        if (module.enabled) module.DrawGUI(HighLogic.LoadedSceneIsEditor);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in DrawGUI: " + e);
                    }
                    Profiler.EndSample();
                }

                PreventClickthrough();
                GUI.matrix = previousGuiMatrix;

                for (int i = 0; i < postDrawQueue.Count; i++)
                {
                    postDrawQueue[i]();
                }
            }
            Profiler.EndSample();
        }

        internal void AddToPostDrawQueue(Callback c)
        {
            postDrawQueue.Add(c);
        }

        // VAB/SPH description
        public override string GetInfo()
        {
            return Localizer.Format("#MechJeb_MechJebInfo_VABSPH");//"Attitude control by MechJeb™"
        }

        //Lifted this more or less directly from the Kerbal Engineer source. Thanks cybutek!
        void PreventClickthrough()
        {
            bool mouseOverWindow = GuiUtils.MouseIsOverWindow(this);
            if (!weLockedInputs && mouseOverWindow && !Input.GetMouseButton(1))
            {
                LockControl();
            }
            else if (weLockedInputs && !mouseOverWindow)
            {
                UnlockControl();
            }
        }

        private const string lockId = "MechJeb_noclick";

        void LockControl()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                EditorLogic.fetch.Lock(true, true, true, lockId);
            }
            else if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium)
            {
                InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, lockId);
            }
            weLockedInputs = true;
        }

        void UnlockControl(Vessel v)
        {
            UnlockControl();
        }

        void UnlockControl()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                EditorLogic.fetch.Unlock(lockId);
            }
            else if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium)
            {
                InputLockManager.RemoveControlLock(lockId);
            }
            weLockedInputs = false;
        }


        public new static void print(object message)
        {
            MonoBehaviour.print("[MechJeb2] " + message);
        }
    }
}
