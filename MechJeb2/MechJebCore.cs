using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using UnityEngine.Profiling;
using UnityToolbag;
using Debug = UnityEngine.Debug;
using File = KSP.IO.File;
using Logger = MechJebLib.Utils.Logger;

namespace MuMech
{
    public class MechJebCore : PartModule, IComparable<MechJebCore>
    {
        private readonly List<ComputerModule> _unorderedComputerModules = new List<ComputerModule>();
        private readonly List<ComputerModule> _modulesToLoad            = new List<ComputerModule>();

        private readonly Dictionary<Type, List<ComputerModule>>  _sortedModules        = new Dictionary<Type, List<ComputerModule>>();
        private readonly Dictionary<object, List<DisplayModule>> _sortedDisplayModules = new Dictionary<object, List<DisplayModule>>();

        // Reference to the parts base config. See Onload for explanation
        private static readonly Dictionary<string, ConfigNode> _savedConfig = new Dictionary<string, ConfigNode>();

        private readonly List<Callback> _postDrawQueue = new List<Callback>();

        private static List<Type> _moduleRegistry;

        private bool _ready;

        public MechJebModuleGuidanceController          Guidance;
        public MechJebModulePVGGlueBall                 Glueball;
        public MechJebModuleAttitudeController          Attitude;
        public MechJebModuleStagingController           Staging;
        public MechJebModuleThrustController            Thrust;
        public MechJebModuleTargetController            Target;
        public MechJebModuleWarpController              Warp;
        public MechJebModuleRCSController               RCS;
        public MechJebModuleRCSBalancer                 Rcsbal;
        public MechJebModuleRoverController             Rover;
        public MechJebModuleNodeExecutor                Node;
        public MechJebModuleSolarPanelController        Solarpanel;
        public MechJebModuleDeployableAntennaController AntennaControl;
        public MechJebModuleLandingAutopilot            Landing;
        public MechJebModuleSettings                    Settings;
        public MechJebModuleAirplaneAutopilot           Airplane;
        public MechJebModuleStageStats                  StageStats;
        public MechJebModuleAscentSettings              AscentSettings;
        public MechJebModuleSpinupController            Spinup;
        public MechJebModuleAscentBaseAutopilot         Ascent => AscentSettings.AscentAutopilot;

        public readonly VesselState VesselState = new VesselState();

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "MechJeb")]
        [UI_Toggle(disabledText = "#MechJeb_Disabled", enabledText = "#MechJeb_Enabled")] //DisabledEnabled
        public bool running = true;

        private Vessel _controlledVessel; //keep track of which vessel we've added our onFlyByWire callback to
        public  string version = "";
        private bool   _deactivateControl;
        private float  _currentThrottle; // To maintain throttle when controls deactivated

        [UsedImplicitly]
        public MechJebCore MasterMechJeb => vessel.GetMasterMechJeb();

        // Allow other mods to kill MJ ability to control vessel (RemoteTech, RO...)
        [UsedImplicitly]
        public bool DeactivateControl
        {
            get
            {
                MechJebCore mj = vessel.GetMasterMechJeb();
                return !(mj is null) && vessel.GetMasterMechJeb()._deactivateControl;
            }
            set
            {
                MechJebCore mj = vessel.GetMasterMechJeb();
                if (mj == null) return;

                if (value && !mj._deactivateControl)
                    _controlledVessel.ctrlState.mainThrottle = _currentThrottle;

                mj._deactivateControl = value;
            }
        }

        [KSPField(isPersistant = false)]
        public string blacklist = "";

        [KSPField]
        private ConfigNode _partSettings;

        [KSPField(isPersistant = false)]
        public bool eduMode;

        public bool RssMode => Settings.rssMode;

        public bool ShowGui { get; private set; } = true;

        [KSPAction("#MechJeb_OrbitPrograde")] //Orbit Prograde
        public void OnOrbitProgradeAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.PROGRADE);
        }

        [KSPAction("#MechJeb_OrbitRetrograde")] //Orbit Retrograde
        public void OnOrbitRetrogradeAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.RETROGRADE);
        }

        [KSPAction("#MechJeb_OrbitNormal")] //Orbit Normal
        public void OnOrbitNormalAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.NORMAL_PLUS);
        }

        [KSPAction("#MechJeb_OrbitAntinormal")] //Orbit Antinormal
        public void OnOrbitAntinormalAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.NORMAL_MINUS);
        }

        [KSPAction("#MechJeb_OrbitRadialIn")] //Orbit Radial In
        public void OnOrbitRadialInAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.RADIAL_MINUS);
        }

        [KSPAction("#MechJeb_OrbitRadialOut")] //Orbit Radial Out
        public void OnOrbitRadialOutAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.RADIAL_PLUS);
        }

        [KSPAction("#MechJeb_OrbitKillRotation")] //Orbit Kill Rotation
        public void OnKillRotationAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.KILLROT);
        }

        [KSPAction("#MechJeb_DeactivateSmartACS")] //Deactivate SmartACS
        public void OnDeactivateSmartASSAction(KSPActionParam param)
        {
            EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target.OFF);
        }

        [KSPAction("#MechJeb_LandSomewhere")] //Land somewhere
        public void OnLandsomewhereAction(KSPActionParam param)
        {
            LandSomewhere();
        }

        [KSPAction("#MechJeb_LandatKSC")] //Land at KSC
        public void OnLandTargetAction(KSPActionParam param)
        {
            LandTarget();
        }

        private void LandTarget()
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb == null) return;

            MechJebModuleLandingGuidance moduleLandingGuidance = GetComputerModule<MechJebModuleLandingGuidance>();

            moduleLandingGuidance?.SetAndLandTargetKSC();
        }

        private void LandSomewhere()
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb == null) return;

            MechJebModuleLandingGuidance moduleLandingGuidance = GetComputerModule<MechJebModuleLandingGuidance>();

            moduleLandingGuidance?.LandSomewhere();
        }

        private void EngageSmartASSOrbitalControl(MechJebModuleSmartASS.Target smartassTarget)
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb == null)
            {
                Debug.LogError(
                    Localizer.Format("#MechJeb_LogError_msg0")); //"MechJeb couldn't find the master MechJeb module for the current vessel."
                return;
            }

            MechJebModuleSmartASS masterSmartASS = masterMechJeb.GetComputerModule<MechJebModuleSmartASS>();

            if (masterSmartASS is { hidden: false })
            {
                masterSmartASS.mode   = MechJebModuleSmartASS.Mode.ORBITAL;
                masterSmartASS.target = smartassTarget;

                masterSmartASS.Engage();
            }
            else
            {
                Debug.LogError(
                    Localizer.Format(
                        "#MechJeb_LogError_msg1")); //"MechJeb couldn't find MechJebModuleSmartASS for orbital control via action group."
            }
        }

        [KSPAction("#MechJeb_PANIC")] //PANIC!
        public void OnPanicAction(KSPActionParam param)
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb == null) return;

            MechJebModuleTranslatron moduleTranslatron = masterMechJeb.GetComputerModule<MechJebModuleTranslatron>();

            if (moduleTranslatron is { hidden: false })
                moduleTranslatron.PanicSwitch();
        }

        [KSPAction("#MechJeb_TranslatronOFF")] //Translatron OFF
        public void OnTranslatronOffAction(KSPActionParam param)
        {
            EngageTranslatronControl(MechJebModuleThrustController.TMode.OFF);
        }

        [KSPAction("#MechJeb_TranslatronKeepVert")] //Translatron Keep Vert
        public void OnTranslatronKeepVertAction(KSPActionParam param)
        {
            EngageTranslatronControl(MechJebModuleThrustController.TMode.KEEP_VERTICAL);
        }

        [KSPAction("#MechJeb_TranslatronZerospeed")] //Translatron Zero speed
        public void OnTranslatronZeroSpeedAction(KSPActionParam param)
        {
            SetTranslatronSpeed(0);
        }

        [KSPAction("#MechJeb_TranslatronPlusspeed")] //Translatron +1 speed
        public void OnTranslatronPlusOneSpeedAction(KSPActionParam param)
        {
            SetTranslatronSpeed(1, true);
        }

        [KSPAction("#MechJeb_TranslatronMinusspeed")] //Translatron -1 speed
        public void OnTranslatronMinusOneSpeedAction(KSPActionParam param)
        {
            SetTranslatronSpeed(-1, true);
        }

        [KSPAction("#MechJeb_TranslatronToggleHS")] //Translatron Toggle H/S
        public void OnTranslatronToggleHSAction(KSPActionParam param)
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb != null)
            {
                MechJebModuleTranslatron moduleTranslatron = masterMechJeb.GetComputerModule<MechJebModuleTranslatron>();

                if (moduleTranslatron is { hidden: false })
                {
                    Thrust.trans_kill_h = !Thrust.trans_kill_h;
                }
                else
                {
                    Debug.LogError(
                        Localizer.Format(
                            "#MechJeb_LogError_msg2")); //"MechJeb couldn't find MechJebModuleTranslatron for translatron control via action group."
                }
            }
            else
            {
                Debug.LogError("MechJeb couldn't find the master MechJeb module for the current vessel.");
            }
        }

        [KSPAction("#MechJeb_AscentAPtoggle")] //Ascent AP toggle
        public void OnAscentAPToggleAction(KSPActionParam param)
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            MechJebModuleAscentBaseAutopilot autopilot = masterMechJeb.AscentSettings.AscentAutopilot;
            MechJebModuleAscentMenu ascentMenu = GetComputerModule<MechJebModuleAscentMenu>();

            if (autopilot == null || ascentMenu == null)
                return;

            if (autopilot.Enabled)
                autopilot.Users.Remove(ascentMenu);
            else
                autopilot.Users.Add(ascentMenu);
        }

        private void EngageTranslatronControl(MechJebModuleThrustController.TMode mode)
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb == null)
            {
                Debug.LogError("MechJeb couldn't find the master MechJeb module for the current vessel.");
                return;
            }

            MechJebModuleTranslatron moduleTranslatron = masterMechJeb.GetComputerModule<MechJebModuleTranslatron>();

            if (moduleTranslatron is { hidden: false })
            {
                if (Thrust.Users.Count > 1 && !Thrust.Users.Contains(moduleTranslatron))
                    return;

                moduleTranslatron.SetMode(mode);
            }
            else
            {
                Debug.LogError("MechJeb couldn't find MechJebModuleTranslatron for translatron control via action group.");
            }
        }

        private void SetTranslatronSpeed(float speed, bool relative = false)
        {
            MechJebCore masterMechJeb = vessel.GetMasterMechJeb();

            if (masterMechJeb == null)
            {
                Debug.LogError("MechJeb couldn't find the master MechJeb module for the current vessel.");
                return;
            }

            MechJebModuleTranslatron moduleTranslatron = masterMechJeb.GetComputerModule<MechJebModuleTranslatron>();

            if (moduleTranslatron is { hidden: false })
                Thrust.trans_spd_act = (relative ? Thrust.trans_spd_act : 0) + speed;
            else
                Debug.LogError("MechJeb couldn't find MechJebModuleTranslatron for translatron control via action group.");
        }

        private        bool   _weLockedInputs;
        private        float  _lastSettingsSaveTime;
        private        bool   _wasMasterAndFocus;
        private static Vessel _lastFocus;

        public bool someModuleAreLocked; // True if any module was locked by the R&D system

        //Returns whether the vessel we've registered OnFlyByWire with is the correct one.
        //If it isn't the correct one, fixes it before returning false
        private bool CheckControlledVessel()
        {
            if (_controlledVessel == vessel) return true;

            //else we have an onFlyByWire callback registered with the wrong vessel:
            //handle vessel changes due to docking/undocking
            if (_controlledVessel != null) _controlledVessel.OnFlyByWire -= OnFlyByWire;
            if (vessel != null)
            {
                vessel.OnFlyByWire -= OnFlyByWire; //just a safety precaution to avoid duplicates
                vessel.OnFlyByWire += OnFlyByWire;
            }

            _controlledVessel = vessel;

            return false;
        }

        [UsedImplicitly]
        public int GetImportance()
        {
            return part.State == PartStates.DEAD ? 0 : GetInstanceID();
        }

        public int CompareTo(MechJebCore other)
        {
            if (other == null) return 1;
            return GetImportance().CompareTo(other.GetImportance());
        }

        public T GetComputerModule<T>() where T : ComputerModule
        {
            for (int i = 0; i < _unorderedComputerModules.Count; i++)
            {
                ComputerModule pm = _unorderedComputerModules[i];
                if (pm is T module)
                    return module;
            }

            return null;
        }

        // The pure generic version was creating too much garbage
        public List<ComputerModule> GetComputerModules<T>() where T : ComputerModule
        {
            Type key = typeof(T);
            if (_sortedModules.TryGetValue(key, out List<ComputerModule> value))
                return value;
            _sortedModules[key] = value = _unorderedComputerModules.OfType<T>().Cast<ComputerModule>().OrderBy(m => m).ToList();
            return value;
        }

        // Added because the generic version eats memory like candy when casting from ComputerModule to DisplayModule (.Cast<T>())
        public List<DisplayModule> GetDisplayModules(IComparer<DisplayModule> comparer)
        {
            if (_sortedDisplayModules.TryGetValue(comparer, out List<DisplayModule> value))
                return value;
            _sortedDisplayModules[comparer] = value = _unorderedComputerModules.OfType<DisplayModule>().OrderBy(m => m, comparer).ToList();
            return value;
        }

        [UsedImplicitly]
        public ComputerModule GetComputerModule(string type)
        {
            return _unorderedComputerModules.FirstOrDefault(a => a.GetType().Name.ToLowerInvariant() == type.ToLowerInvariant()); //null if none
        }

        public void AddComputerModule(ComputerModule module)
        {
            _unorderedComputerModules.Add(module);
            ClearModulesCache();
        }

        private void ClearModulesCache()
        {
            _sortedModules.Clear();
            _sortedDisplayModules.Clear();
        }

        public void AddComputerModuleLater(ComputerModule module)
        {
            //The actual loading is delayed to FixedUpdate because AddComputerModule can get called inside a foreach loop
            //over the modules, and modifying the list during the loop will cause an exception. Maybe there is a better
            //way to deal with this?
            _modulesToLoad.Add(module);
        }

        private void LoadDelayedModules()
        {
            if (_modulesToLoad.Count > 0)
            {
                _unorderedComputerModules.AddRange(_modulesToLoad);
                _modulesToLoad.Clear();
                ClearModulesCache();
            }
        }

        public void RemoveComputerModule(ComputerModule module)
        {
            _unorderedComputerModules.Remove(module);
            ClearModulesCache();
        }

        public void ReloadAllComputerModules()
        {
            //Dispose of all the existing computer modules
            foreach (ComputerModule module in _unorderedComputerModules) module.OnDestroy();
            _unorderedComputerModules.Clear();
            ClearModulesCache();

            if (vessel != null) vessel.OnFlyByWire -= OnFlyByWire;
            _controlledVessel = null;

            //Start fresh
            OnLoad(null);
            OnStart(HighLogic.LoadedSceneIsEditor ? StartState.Editor : StartState.Flying);
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsEditor)
                _ready = true;
            if (state == StartState.None) return; //don't do anything when we start up in the loading screen

            //OnLoad doesn't get called for parts created in editor, so do that manually so
            //that we can load global settings.
            //However, if you press ctrl-Z, a new PartModule object gets created, on which the
            //game DOES call OnLoad, and then OnStart. So before calling OnLoad from OnStart,
            //check whether we have loaded any computer modules.

            //if (state == StartState.Editor && computerModules.Count == 0)
            // Seems to happend when launching without coming from the VAB too.
            if (_unorderedComputerModules.Count == 0)
            {
                OnLoad(null);
            }

            GameEvents.onShowUI.Add(OnShowGUI);
            GameEvents.onHideUI.Add(OnHideGUI);
            GameEvents.onVesselChange.Add(UnlockControl);
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            GameEvents.onVesselStandardModification.Add(OnVesselStandardModification);

            _lastSettingsSaveTime = Time.time;

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
                _controlledVessel  =  vessel;
            }

            Logger.Register(SafePrint);
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
            FuelFlowSimulation.FuelNode.DoReflection();
            CachedLocalizer.Bootstrap();

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
                _wasMasterAndFocus = false;
            }

            if (this != vessel.GetMasterMechJeb())
            {
                return;
            }

            Profiler.BeginSample("MechJebCore.FixedUpdate");

            if (!_wasMasterAndFocus && (HighLogic.LoadedSceneIsEditor || vessel.isActiveVessel))
            {
                if (HighLogic.LoadedSceneIsFlight && _lastFocus != null && _lastFocus.loaded && _lastFocus.GetMasterMechJeb() != null)
                {
                    Print("Focus changed! Forcing " + _lastFocus.vesselName + " to save");
                    _lastFocus.GetMasterMechJeb().OnSave(null);
                }

                // Clear the modules cache
                ClearModulesCache();

                OnLoad(null); // Force Global reload

                _wasMasterAndFocus = true;
                _lastFocus         = vessel;
            }

            if (vessel == null)
            {
                Profiler.EndSample();
                return; //don't run ComputerModules' OnFixedUpdate in editor
            }

            Profiler.BeginSample("vesselState");
            _ready = VesselState.Update(vessel);
            Profiler.EndSample();

            foreach (ComputerModule module in GetComputerModules<ComputerModule>())
            {
                Profiler.BeginSample(module.ProfilerName);
                try
                {
                    if (module.Enabled)
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
                needToSave |= module.Dirty;
            }

            return needToSave;
        }

        public void Update()
        {
            if (this != vessel.GetMasterMechJeb() || (!FlightGlobals.ready && HighLogic.LoadedSceneIsFlight) || !_ready)
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
                if (Time.time > _lastSettingsSaveTime + 5)
                {
                    if (NeedToSave())
                    {
                        //print("Periodic settings save");
                        OnSave(null);
                    }

                    _lastSettingsSaveTime = Time.time;
                }
            }

            Profiler.EndSample();

            if (ResearchAndDevelopment.Instance != null && _unorderedComputerModules.Any(a => !a.UnlockChecked))
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
                Profiler.BeginSample(module.ProfilerName);
                try
                {
                    if (module.Enabled) module.OnUpdate();
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in OnUpdate: " + e);
                }

                Profiler.EndSample();
            }

            Profiler.EndSample();
        }

        private void LoadComputerModules()
        {
            if (_moduleRegistry == null)
            {
                _moduleRegistry = new List<Type>();
                foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (Type module in (from t in ass.GetTypes() where t.IsSubclassOf(typeof(ComputerModule)) && !t.IsAbstract select t)
                                 .ToList())
                        {
                            _moduleRegistry.Add(module);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb moduleRegistry creation threw an exception in LoadComputerModules loading " + ass.FullName + ": " +
                                       e);
                    }
                }
            }

            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            // Mono compiler is stupid and use AssemblyVersion for the AssemblyFileVersion
            // So we use an other field to store the dev build number ...
            Attribute[] attributes = Attribute.GetCustomAttributes(assembly, typeof(AssemblyInformationalVersionAttribute));
            string devVersion = "";
            if (attributes.Length > 0)
                devVersion = ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;

            version = devVersion == ""
                ? $"{fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}.{fileVersionInfo.FileBuildPart}"
                : devVersion;

            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                Print("Loading Mechjeb " + version);

            try
            {
                foreach (Type t in _moduleRegistry)
                {
                    if (t != typeof(ComputerModule) && t != typeof(DisplayModule) && t != typeof(MechJebModuleCustomInfoWindow)
                        && t != typeof(AutopilotModule) && t != typeof(MechJebModuleAscentBaseAutopilot)
                        && !blacklist.Contains(t.Name) && GetComputerModule(t.Name) == null)
                    {
                        ConstructorInfo constructorInfo = t.GetConstructor(new[] { typeof(MechJebCore) });
                        if (constructorInfo != null)
                            AddComputerModule((ComputerModule)constructorInfo.Invoke(new object[] { this }));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("MechJeb moduleRegistry loading threw an exception in LoadComputerModules: " + e);
            }

            Attitude       = GetComputerModule<MechJebModuleAttitudeController>();
            Staging        = GetComputerModule<MechJebModuleStagingController>();
            Thrust         = GetComputerModule<MechJebModuleThrustController>();
            Target         = GetComputerModule<MechJebModuleTargetController>();
            Warp           = GetComputerModule<MechJebModuleWarpController>();
            RCS            = GetComputerModule<MechJebModuleRCSController>();
            Rcsbal         = GetComputerModule<MechJebModuleRCSBalancer>();
            Rover          = GetComputerModule<MechJebModuleRoverController>();
            Node           = GetComputerModule<MechJebModuleNodeExecutor>();
            Solarpanel     = GetComputerModule<MechJebModuleSolarPanelController>();
            AntennaControl = GetComputerModule<MechJebModuleDeployableAntennaController>();
            Landing        = GetComputerModule<MechJebModuleLandingAutopilot>();
            Settings       = GetComputerModule<MechJebModuleSettings>();
            Airplane       = GetComputerModule<MechJebModuleAirplaneAutopilot>();
            Guidance       = GetComputerModule<MechJebModuleGuidanceController>();
            Glueball       = GetComputerModule<MechJebModulePVGGlueBall>();
            StageStats     = GetComputerModule<MechJebModuleStageStats>();
            AscentSettings = GetComputerModule<MechJebModuleAscentSettings>();
            Spinup         = GetComputerModule<MechJebModuleSpinupController>();
        }

        public override void OnLoad(ConfigNode sfsNode)
        {
            if (GuiUtils.skin == null)
            {
                //GuiUtils.skin = new GUISkin();
                // ReSharper disable once ObjectCreationAsStatement
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
                if (!_savedConfig.ContainsKey(part.name))
                {
                    if (HighLogic.LoadedScene == GameScenes.LOADING)
                        _savedConfig.Add(part.name, sfsNode);
                }
                else
                {
                    _partSettings = _savedConfig[part.name];
                }

                LoadComputerModules();

                var global = new ConfigNode("MechJebGlobalSettings");
                if (File.Exists<MechJebCore>("mechjeb_settings_global.cfg"))
                {
                    try
                    {
                        global = ConfigNode.Load(MuUtils.GetCfgPath("mechjeb_settings_global.cfg"));
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

                var type = new ConfigNode("MechJebTypeSettings");
                string vesselName =
                    vessel != null
                        ? string.Join("_", vessel.vesselName.Split(Path.GetInvalidFileNameChars()))
                        : ""; // Strip illegal char from the filename
                if (vessel != null && File.Exists<MechJebCore>("mechjeb_settings_type_" + vesselName + ".cfg"))
                {
                    try
                    {
                        type = ConfigNode.Load(MuUtils.GetCfgPath("mechjeb_settings_type_" + vesselName + ".cfg"));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJebCore.OnLoad caught an exception trying to load mechjeb_settings_type_" + vesselName + ".cfg: " + e);
                    }
                }

                var local = new ConfigNode("MechJebLocalSettings");
                if (sfsNode != null && sfsNode.HasNode("MechJebLocalSettings"))
                {
                    local = sfsNode.GetNode("MechJebLocalSettings");
                }
                else if (_partSettings != null && _partSettings.HasNode("MechJebLocalSettings"))
                {
                    local = _partSettings.GetNode("MechJebLocalSettings");
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
                while (GetComputerModule<MechJebModuleCustomInfoWindow>() is { } win)
                {
                    RemoveComputerModule(win);
                }

                foreach (ComputerModule module in GetComputerModules<ComputerModule>())
                {
                    try
                    {
                        string moduleTypeName = module.GetType().Name;
                        ConfigNode moduleLocal = local.HasNode(moduleTypeName) ? local.GetNode(moduleTypeName) : null;
                        ConfigNode moduleType = type.HasNode(moduleTypeName) ? type.GetNode(moduleTypeName) : null;
                        ConfigNode moduleGlobal = global.HasNode(moduleTypeName) ? global.GetNode(moduleTypeName) : null;
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
                IEnumerable<Assembly> brokenAssembly = ex.Types.Where(x => x != null).Select(x => x.Assembly).Distinct();
                foreach (Assembly assembly in brokenAssembly)
                {
                    Debug.LogError(assembly.GetName().Name + " " + assembly.GetName().Version + " " +
                                   assembly.Location.Remove(0, Path.GetFullPath(KSPUtil.ApplicationRootPath).Length));
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
            if (_unorderedComputerModules.Count == 0) return;

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
                ConfigNode local = sfsNode == null ? null : new ConfigNode("MechJebLocalSettings");
                ConfigNode type = sfsNode != null ? null : new ConfigNode("MechJebTypeSettings");
                ConfigNode global = sfsNode != null ? null : new ConfigNode("MechJebGlobalSettings");
                Profiler.EndSample();
                Profiler.BeginSample("MechJebCore.OnSave.loop");
                foreach (ComputerModule module in GetComputerModules<ComputerModule>())
                {
                    Profiler.BeginSample(module.ProfilerName);
                    try
                    {
                        string moduleTypeName = module.GetType().Name;
                        module.OnSave(local?.AddNode(moduleTypeName), type?.AddNode(moduleTypeName), global?.AddNode(moduleTypeName));
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
                    string vesselName = HighLogic.LoadedSceneIsEditor && EditorLogic.fetch ? EditorLogic.fetch.shipNameField.text : vessel.vesselName;
                    vesselName = string.Join("_", vesselName.Split(Path.GetInvalidFileNameChars())); // Strip illegal char from the filename
                    type.Save(MuUtils.GetCfgPath("mechjeb_settings_type_" + vesselName + ".cfg"));
                }

                Profiler.EndSample();
                Profiler.BeginSample("MechJebCore.OnSave.global");
                if (global != null && _lastFocus == vessel)
                {
                    global.Save(MuUtils.GetCfgPath("mechjeb_settings_global.cfg"));
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
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
            GameEvents.onVesselStandardModification.Remove(OnVesselStandardModification);

            if (_weLockedInputs)
            {
                UnlockControl();
                ManeuverGizmoBase.HasMouseFocus = false;
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

            _controlledVessel = null;
        }

        private void OnFlyByWire(FlightCtrlState s)
        {
            if (_deactivateControl || !CheckControlledVessel() || this != vessel.GetMasterMechJeb())
            {
                return;
            }

            Drive(s);

            CheckFlightCtrlState(s);

            // Update current throttle for control deactivation
            _currentThrottle = s.mainThrottle;
        }

        private void Drive(FlightCtrlState s)
        {
            Profiler.BeginSample("vesselState");
            _ready = VesselState.Update(vessel);
            Profiler.EndSample();

            Profiler.BeginSample("MechJebCore.Drive");
            if (this == vessel.GetMasterMechJeb())
            {
                foreach (ComputerModule module in GetComputerModules<ComputerModule>())
                {
                    Profiler.BeginSample(module.ProfilerName);
                    try
                    {
                        if (module.Enabled) module.Drive(s);
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
            if (float.IsNaN(s.yaw)) s.yaw                   = 0;
            if (float.IsNaN(s.pitch)) s.pitch               = 0;
            if (float.IsNaN(s.roll)) s.roll                 = 0;
            if (float.IsNaN(s.X)) s.X                       = 0;
            if (float.IsNaN(s.Y)) s.Y                       = 0;
            if (float.IsNaN(s.Z)) s.Z                       = 0;

            s.mainThrottle = Mathf.Clamp01(s.mainThrottle);
            s.yaw          = Mathf.Clamp(s.yaw, -1, 1);
            s.pitch        = Mathf.Clamp(s.pitch, -1, 1);
            s.roll         = Mathf.Clamp(s.roll, -1, 1);
            s.X            = Mathf.Clamp(s.X, -1, 1);
            s.Y            = Mathf.Clamp(s.Y, -1, 1);
            s.Z            = Mathf.Clamp(s.Z, -1, 1);
        }

        private void OnShowGUI()
        {
            ShowGui = true;
        }

        private void OnHideGUI()
        {
            ShowGui = false;
        }

        private void OnGUI()
        {
            if (!ShowGui || this != vessel.GetMasterMechJeb() || (!FlightGlobals.ready && HighLogic.LoadedSceneIsFlight) || !_ready) return;

            Profiler.BeginSample("MechJebCore.OnGUI");

            if (HighLogic.LoadedSceneIsEditor || (FlightGlobals.ready && vessel == FlightGlobals.ActiveVessel && part.State != PartStates.DEAD))
            {
                Matrix4x4 previousGuiMatrix = GUI.matrix;
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(GuiUtils.scale, GuiUtils.scale, 1));

                GuiUtils.ComboBox.DrawGUI();

                GuiUtils.LoadSkin((GuiUtils.SkinType)Settings.skinId);

                GUI.skin = GuiUtils.skin;

                foreach (ComputerModule computerModule in GetComputerModules<DisplayModule>())
                {
                    var module = (DisplayModule)computerModule;
                    Profiler.BeginSample(module.ProfilerName);
                    try
                    {
                        if (module.Enabled) module.DrawGUI(HighLogic.LoadedSceneIsEditor);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("MechJeb module " + module.GetType().Name + " threw an exception in DrawGUI: " + e);
                    }

                    Profiler.EndSample();
                }

                PreventClickthrough();
                GUI.matrix = previousGuiMatrix;

                for (int i = 0; i < _postDrawQueue.Count; i++)
                {
                    _postDrawQueue[i]();
                }
            }

            Profiler.EndSample();
        }

        internal void AddToPostDrawQueue(Callback c)
        {
            _postDrawQueue.Add(c);
        }

        // VAB/SPH description
        public override string GetInfo()
        {
            return Localizer.Format("#MechJeb_MechJebInfo_VABSPH"); //"Attitude control by MechJeb™"
        }

        //Lifted this more or less directly from the Kerbal Engineer source. Thanks cybutek!
        private void PreventClickthrough()
        {
            bool mouseOverWindow = GuiUtils.MouseIsOverWindow(this);
            switch (_weLockedInputs)
            {
                case false when mouseOverWindow && !Input.GetMouseButton(1):
                    LockControl();
                    break;
                case true when !mouseOverWindow:
                    UnlockControl();
                    break;
            }
        }

        private const string LOCK_ID = "MechJeb_noclick";

        private void LockControl()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                EditorLogic.fetch.Lock(true, true, true, LOCK_ID);
            }
            else if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium)
            {
                InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, LOCK_ID);
            }

            _weLockedInputs = true;
        }

        private void UnlockControl(Vessel v)
        {
            UnlockControl();
        }

        private void UnlockControl()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                EditorLogic.fetch.Unlock(LOCK_ID);
            }
            else if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium)
            {
                InputLockManager.RemoveControlLock(LOCK_ID);
            }

            _weLockedInputs = false;
        }

        private void OnVesselWasModified(Vessel v)
        {
            if (v != vessel || this != vessel.GetMasterMechJeb())
                return;

            const string SAMPLE_NAME = "MechJebCore.OnVesselWasModified";
            Profiler.BeginSample(SAMPLE_NAME);
            foreach (ComputerModule module in GetComputerModules<ComputerModule>().Where(x => x.Enabled))
            {
                Profiler.BeginSample(module.ProfilerName);
                try
                {
                    module.OnVesselWasModified(v);
                }
                catch (Exception e)
                {
                    Debug.LogError($"MechJeb module {module.GetType().Name} threw an exception in {SAMPLE_NAME}: {e}");
                }

                Profiler.EndSample();
            }

            Profiler.EndSample();
        }

        // Copy-pasta, but preferred over a Reflection-based approach (pass MethodInfos around)
        private void OnVesselStandardModification(Vessel v)
        {
            if (v != vessel || this != vessel.GetMasterMechJeb())
                return;

            const string SAMPLE_NAME = "MechJebCore.OnVesselStandardModification";
            Profiler.BeginSample(SAMPLE_NAME);
            foreach (ComputerModule module in GetComputerModules<ComputerModule>().Where(x => x.Enabled))
            {
                Profiler.BeginSample(module.ProfilerName);
                try
                {
                    module.OnVesselStandardModification(v);
                }
                catch (Exception e)
                {
                    Debug.LogError($"MechJeb module {module.GetType().Name} threw an exception in {SAMPLE_NAME}: {e}");
                }

                Profiler.EndSample();
            }

            Profiler.EndSample();
        }

        public static void Print(object message)
        {
            print("[MechJeb2] " + message);
        }

        [UsedImplicitly]
        public static void SafePrint(object message)
        {
            Dispatcher.InvokeAsync(() => print("[MechJeb2] " + message));
        }
    }
}
