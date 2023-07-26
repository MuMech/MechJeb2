using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using KSP.UI.Screens;
using MechJebLib.Simulations;
using Smooth.Slinq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleStagingController : ComputerModule
    {
        public MechJebModuleStagingController(MechJebCore core)
            : base(core)
        {
            Priority = 1000;
        }

        //adjustable parameters:
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble AutostagePreDelay = 0.0;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble AutostagePostDelay = 0.5;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableInt AutostageLimit = 0;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult FairingMaxDynamicPressure = new EditableDoubleMult(5000, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult FairingMinAltitude = new EditableDoubleMult(50000, 1000);

        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableDouble ClampAutoStageThrustPct = 0.99;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult FairingMaxAerothermalFlux = new EditableDoubleMult(1135);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool HotStaging;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble HotStagingLeadTime = 1.0;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool DropSolids;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble DropSolidsLeadTime = 1.0;

        public bool AutostagingOnce;

        private bool _waitingForFirstStaging;

        private readonly Dictionary<object, int> _autoStageModuleLimit = new Dictionary<object, int>();

        public void AutoStageLimitRequest(int stage, object user) => _autoStageModuleLimit.TryAdd(user, stage);

        public void AutoStageLimitRemove(object user)
        {
            if (_autoStageModuleLimit.ContainsKey(user))
                _autoStageModuleLimit.Remove(user);
        }

        private int ActiveAutoStageModuleLimit()
        {
            int limit = 0;
            foreach (int value in _autoStageModuleLimit.Values)
                if (value > limit)
                    limit = value;
            return limit;
        }

        private readonly List<ModuleEngines>     _activeModuleEngines             = new List<ModuleEngines>(16);
        private readonly List<ModuleEngines>     _allModuleEngines                = new List<ModuleEngines>(16);
        private readonly List<PartModule>        _allDecouplers                   = new List<PartModule>(16);
        private readonly List<int>               _burnedResources                 = new List<int>(16);
        private readonly Dictionary<int, bool>   _inverseStageHasEngines          = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool>   _inverseStageFiresDecouplerCache = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool>   _inverseStageReleasesClampsCache = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool>   _hasStayingChutesCache           = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool>   _hasFairingCache                 = new Dictionary<int, bool>(16);
        private          MechJebModuleStageStats _stats    => Core.GetComputerModule<MechJebModuleStageStats>();
        private          List<FuelStats>         _vacStats => _stats.VacStats;

        private enum RemoteStagingState
        {
            DISABLED,
            WAITING_FOCUS,
            FOCUS_FINISHED
        }

        private RemoteStagingState _remoteStagingStatus = RemoteStagingState.DISABLED;

        public override void OnStart(PartModule.StartState state)
        {
            if (Vessel != null && Vessel.situation == Vessel.Situations.PRELAUNCH)
                _waitingForFirstStaging = true;

            GameEvents.onStageActivate.Add(StageActivate);
            GameEvents.onVesselResumeStaging.Add(VesselResumeStage);
        }

        private void RegenerateCaches()
        {
            ClearCaches();
            BuildEnginesCache(_allModuleEngines);
            BuildDecouplersCache(_allDecouplers);
        }

        private void OnGUIStageSequenceModified() => RegenerateCaches();

        private void OnVesselModified(Vessel v)
        {
            if (Vessel == v) RegenerateCaches();
        }

        private void BuildEnginesCache(List<ModuleEngines> engines)
        {
            engines.Clear();
            foreach (Part p in Vessel.Parts)
                if (p.IsEngine() && !p.IsSepratron())
                    engines.AddRange(p.FindModulesImplementing<ModuleEngines>());
        }

        private void BuildDecouplersCache(List<PartModule> decouplers)
        {
            decouplers.Clear();
            foreach (Part p in Vessel.Parts)
                if (p.IsDecoupler())
                    foreach (PartModule pm in p.Modules)
                        if (pm is ModuleDecouplerBase || pm is ModuleDockingNode || pm.moduleName == "ProceduralFairingDecoupler")
                            decouplers.Add(pm);
        }

        private void ClearCaches()
        {
            _inverseStageHasEngines.Clear();
            _inverseStageFiresDecouplerCache.Clear();
            _inverseStageReleasesClampsCache.Clear();
            _hasStayingChutesCache.Clear();
            _hasFairingCache.Clear();
        }

        private void VesselResumeStage(Vessel data)
        {
            if (_remoteStagingStatus == RemoteStagingState.WAITING_FOCUS)
            {
                _remoteStagingStatus = RemoteStagingState.FOCUS_FINISHED;
            }
        }

        public override void OnDestroy()
        {
            GameEvents.onStageActivate.Remove(StageActivate);
            GameEvents.onVesselResumeStaging.Remove(VesselResumeStage);
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.StageManager.OnGUIStageSequenceModified.Remove(OnGUIStageSequenceModified);
        }

        private void StageActivate(int data) => _waitingForFirstStaging = false;

        public void AutostageOnce(object user)
        {
            Users.Add(user);
            AutostagingOnce = true;
        }

        protected override void OnModuleEnabled() => _autoStageModuleLimit.Clear();

        protected override void OnModuleDisabled() => AutostagingOnce = false;

        private readonly string _sFairingMinDynamicPressure = $"  {CachedLocalizer.Instance.MechJeb_Ascent_label39} <";
        private readonly string _sFairingMinAltitude        = $"  {CachedLocalizer.Instance.MechJeb_Ascent_label40} >";
        private readonly string _sFairingMaxAerothermalFlux = $"  {CachedLocalizer.Instance.MechJeb_Ascent_label41} <";

        private readonly string _sHotstaging =
            $"{CachedLocalizer.Instance.MechJeb_Ascent_hotStaging} {CachedLocalizer.Instance.MechJeb_Ascent_leadTime}";

        private readonly string _sDropSolids =
            $"{CachedLocalizer.Instance.MechJeb_Ascent_dropSolids} {CachedLocalizer.Instance.MechJeb_Ascent_leadTime}";

        private readonly string _sLeadTime = $"{CachedLocalizer.Instance.MechJeb_Ascent_leadTime}: ";

        [GeneralInfoItem("#MechJeb_AutostagingSettings", InfoItem.Category.Misc)] //Autostaging settings
        public void AutostageSettingsInfoItem()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label42, AutostageLimit, width: 40); //"Stop at stage #"

            GUILayout.BeginHorizontal();
            GUILayout.Label("Delays: ");
            GUILayout.Space(30);
            GuiUtils.SimpleTextBox("pre: ", AutostagePreDelay, "s", 35, horizontalFraming: false);
            GUILayout.FlexibleSpace();
            GuiUtils.SimpleTextBox("post: ", AutostagePostDelay, "s", 35, horizontalFraming: false);
            GUILayout.EndHorizontal();

            ClampAutostageThrust();

            GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label38);                           //"Stage fairings when:"
            GuiUtils.SimpleTextBox(_sFairingMinDynamicPressure, FairingMaxDynamicPressure, "kPa", 50);  //"dynamic pressure"
            GuiUtils.SimpleTextBox(_sFairingMinAltitude, FairingMinAltitude, "km", 50);                 //altitude
            GuiUtils.SimpleTextBox(_sFairingMaxAerothermalFlux, FairingMaxAerothermalFlux, "W/m²", 50); //aerothermal flux

            GUILayout.BeginHorizontal();
            HotStaging = GUILayout.Toggle(HotStaging, CachedLocalizer.Instance.MechJeb_Ascent_hotStaging); //"Support hotstaging"
            GUILayout.FlexibleSpace();
            GuiUtils.SimpleTextBox(_sLeadTime, HotStagingLeadTime, "s", 35); //"lead time"
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DropSolids = GUILayout.Toggle(DropSolids, CachedLocalizer.Instance.MechJeb_Ascent_dropSolids); //"Drop solids early"
            GUILayout.FlexibleSpace();
            GuiUtils.SimpleTextBox(_sLeadTime, DropSolidsLeadTime, "s", 35); //"lead time"
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        [ValueInfoItem("#MechJeb_Autostagingstatus", InfoItem.Category.Misc)] //Autostaging status
        public string AutostageStatus()
        {
            if (!Enabled) return CachedLocalizer.Instance.MechJeb_Ascent_status9;          //"Autostaging off"
            if (AutostagingOnce) return CachedLocalizer.Instance.MechJeb_Ascent_status10;  //"Will autostage next stage only"
            return CachedLocalizer.Instance.MechJeb_Ascent_status11 + (int)AutostageLimit; //"Autostaging until stage #"
        }

        [GeneralInfoItem("#MechJeb_ClampAutostageThrust", InfoItem.Category.Misc)] //Clamp Autostage Thrust
        private void ClampAutostageThrust()
        {
            double prev = Core.Staging.ClampAutoStageThrustPct;
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label44, Core.Staging.ClampAutoStageThrustPct, "%",
                50); //"Clamp AutoStage Thrust "
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (prev != Core.Staging.ClampAutoStageThrustPct)
                Core.Staging.ClampAutoStageThrustPct.val = UtilMath.Clamp(Core.Staging.ClampAutoStageThrustPct, 0, 100);
        }

        //internal state:
        private double _lastStageTime;
        private bool   _countingDown;
        private double _stageCountdownStart;
        private Vessel _currentActiveVessel;
        private bool   _initializedOnce;

        public override void OnUpdate()
        {
            if (!_initializedOnce)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    GameEvents.onVesselWasModified.Add(OnVesselModified);
                    GameEvents.StageManager.OnGUIStageSequenceModified.Add(OnGUIStageSequenceModified);
                }

                _initializedOnce = true;
                RegenerateCaches();
            }

            // if autostage enabled, and if we've already staged at least once, and if there are stages left,
            // and if we are allowed to continue staging, and if we didn't just fire the previous stage
            if (_waitingForFirstStaging || Vessel.currentStage <= 0 || Vessel.currentStage <= AutostageLimit ||
                VesselState.time - _lastStageTime < AutostagePostDelay)
            {
                return;
            }

            // this is for PVG preventing staging doing coasts, possibly it should be more specific of an API
            // (e.g. bool PVGIsCoasting) since it is getting tightly coupled.
            if (Vessel.currentStage <= ActiveAutoStageModuleLimit())
            {
                // force staging once if fairing conditions are met in the next stage
                if (HasFairing(Vessel.currentStage - 1) && !WaitingForFairing())
                {
                    Stage();
                }
                else
                {
                    return;
                }
            }

            UpdateActiveModuleEngines(_allModuleEngines);
            UpdateBurnedResources();

            // don't decouple active or idle engines or tanks
            if (InverseStageDecouplesActiveOrIdleEngineOrTank(Vessel.currentStage - 1, _burnedResources, _activeModuleEngines) &&
                !InverseStageReleasesClamps(Vessel.currentStage - 1))
                return;

            // prevent staging if we have unstable ullage and we have RCS
            if (InverseStageHasUnstableEngines(Vessel.currentStage - 1) && Core.Thrust.AutoRCSUllaging && Vessel.hasEnabledRCSModules() &&
                Core.Thrust.LastThrottle > 0)
            {
                if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                    Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                return;
            }

            // always stage if we have no active engines
            if (!InverseStageHasActiveEngines(Vessel.currentStage))
                Stage();

            // prevent staging when the current stage has active engines and the next stage has any engines (but not decouplers or clamps)
            if (HotStaging && InverseStageHasEngines(Vessel.currentStage - 1) &&
                !InverseStageFiresDecoupler(Vessel.currentStage - 1) && !InverseStageReleasesClamps(Vessel.currentStage - 1) &&
                LastNonZeroDVStageBurnTime() > HotStagingLeadTime)
                return;

            // Don't fire a stage that will activate a parachute, unless that parachute gets decoupled:
            if (HasStayingChutes(Vessel.currentStage - 1))
                return;

            // Always drop deactivated engines or tanks
            if (InverseStageDecouplesDeactivatedEngineOrTank(Vessel.currentStage - 1))
                Stage();

            // only decouple fairings if the dynamic pressure, altitude, and aerothermal flux conditions are respected
            if (WaitingForFairing())
                return;

            // only release launch clamps if we're at nearly full thrust and no failed engines
            if ((VesselState.thrustCurrent / VesselState.thrustAvailable < ClampAutoStageThrustPct || AnyFailedEngines(_allModuleEngines)) &&
                InverseStageReleasesClamps(Vessel.currentStage - 1))
                return;

            Stage();
        }

        private bool WaitingForFairing()
        {
            if (!HasFairing(Vessel.currentStage - 1))
                return false;

            if (Core.VesselState.dynamicPressure > FairingMaxDynamicPressure)
                return true;

            if (Core.VesselState.altitudeASL < FairingMinAltitude)
                return true;

            if (Core.VesselState.freeMolecularAerothermalFlux > FairingMaxAerothermalFlux)
                return true;

            return false;
        }

        public void Stage()
        {
            //When we find that we're allowed to stage, start a countdown (with a
            //length given by autostagePreDelay) and only stage once that countdown finishes,
            if (_countingDown)
            {
                if (VesselState.time - _stageCountdownStart > AutostagePreDelay)
                {
                    if (InverseStageFiresDecoupler(Vessel.currentStage - 1))
                    {
                        //if we decouple things, delay the next stage a bit to avoid exploding the debris
                        _lastStageTime = VesselState.time;
                    }

                    if (!Vessel.isActiveVessel)
                    {
                        _currentActiveVessel = FlightGlobals.ActiveVessel;
                        Debug.Log($"Mechjeb Autostage: Switching from {FlightGlobals.ActiveVessel.name} to vessel {Vessel.name} to stage");

                        _remoteStagingStatus = RemoteStagingState.WAITING_FOCUS;
                        FlightGlobals.ForceSetActiveVessel(Vessel);
                    }
                    else
                    {
                        Debug.Log($"Mechjeb Autostage: Executing next stage on {FlightGlobals.ActiveVessel.name}");

                        if (_remoteStagingStatus == RemoteStagingState.DISABLED)
                        {
                            StageManager.ActivateNextStage();
                        }
                        else if (_remoteStagingStatus == RemoteStagingState.FOCUS_FINISHED)
                        {
                            StageManager.ActivateNextStage();
                            FlightGlobals.ForceSetActiveVessel(_currentActiveVessel);
                            Debug.Log($"Mechjeb Autostage: Has switching back to {FlightGlobals.ActiveVessel.name} ");
                            _remoteStagingStatus = RemoteStagingState.DISABLED;
                        }
                    }

                    _countingDown = false;

                    if (AutostagingOnce)
                        Users.Clear();
                }
            }
            else
            {
                _countingDown        = true;
                _stageCountdownStart = VesselState.time;
            }
        }

        //determine whether it's safe to activate inverseStage
        private bool InverseStageDecouplesActiveOrIdleEngineOrTank(int inverseStage, List<int> tankResources, List<ModuleEngines> activeModuleEngines)
        {
            foreach (PartModule pm in _allDecouplers)
                if (pm.part.inverseStage == inverseStage &&
                    pm.IsUnfiredDecoupler(out Part decoupledPart) &&
                    HasActiveOrIdleEngineOrTankDescendant(decoupledPart, tankResources, activeModuleEngines))
                    return true;
            return false;
        }

        private double LastNonZeroDVStageBurnTime()
        {
            for (int mjPhase = _vacStats.Count - 1; mjPhase >= 0; mjPhase--)
                if (_vacStats[mjPhase].DeltaTime > 0)
                    return _vacStats[mjPhase].DeltaTime;
            return 0;
        }

        // allModuleEngines => IsEngine() && !IsSepratron()
        private bool InverseStageHasActiveEngines(int inverseStage)
        {
            foreach (ModuleEngines engine in _allModuleEngines)
                if (engine.part.inverseStage == inverseStage && engine.EngineHasFuel())
                    return true;
            return false;
        }

        private bool InverseStageHasEngines(int inverseStage)
        {
            if (_inverseStageHasEngines.TryGetValue(inverseStage, out bool result))
                return result;
            result = _allModuleEngines.FirstOrDefault(me => me.part.inverseStage == inverseStage) != null;
            _inverseStageHasEngines.Add(inverseStage, result);
            return result;
        }

        private bool InverseStageHasUnstableEngines(int inverseStage)
        {
            // don't just blindly add caching here because this value is volatile and changes with ullage status
            foreach (ModuleEngines engine in _allModuleEngines)
                if (engine.part.inverseStage == inverseStage && engine.part.UnstableUllage())
                    return true;

            return false;
        }

        private bool AnyFailedEngines(List<ModuleEngines> allEngines)
        {
            foreach (ModuleEngines engine in allEngines)
            {
                Part p = engine.part;
                if (p.inverseStage >= Vessel.currentStage && !p.IsDecoupledInStage(Vessel.currentStage - 1) && engine.isEnabled &&
                    !engine.EngineIgnited && engine.allowShutdown)
                    return true;
            }

            return false;
        }

        private void UpdateActiveModuleEngines(List<ModuleEngines> allEngines)
        {
            _activeModuleEngines.Clear();
            foreach (ModuleEngines engine in allEngines)
            {
                Part p = engine.part;
                if (p.inverseStage >= Vessel.currentStage && !p.IsDecoupledInStage(Vessel.currentStage - 1) && engine.isEnabled)
                    _activeModuleEngines.Add(engine);
            }
        }

        // Find resources burned by engines that will remain after staging (so we wait until tanks are empty before releasing drop tanks)
        private void UpdateBurnedResources()
        {
            _burnedResources.Clear();
            _activeModuleEngines.Slinq().SelectMany(eng => eng.propellants.Slinq()).Select(prop => prop.id).AddTo(_burnedResources);
        }

        // detect if this part is an SRB, will be dropped in the next stage, and we are below the enabled dropSolidsLeadTime
        private bool IsBurnedOutSrbDecoupledInNextStage(Part p) =>
            DropSolids && p.IsThrottleLockedEngine() && LastNonZeroDVStageBurnTime() < DropSolidsLeadTime &&
            p.IsDecoupledInStage(Vessel.currentStage - 1);

        //detect if a part is above an active or idle engine in the part tree
        private bool HasActiveOrIdleEngineOrTankDescendant(Part p, List<int> tankResources, List<ModuleEngines> activeModuleEngines)
        {
            if (p is null)
                return false;

            if (!p.IsSepratron() && !IsBurnedOutSrbDecoupledInNextStage(p))
            {
                if ((p.State == PartStates.ACTIVE || p.State == PartStates.IDLE) && p.EngineHasFuel() && !p.UnrestartableDeadEngine())
                {
                    return true; // TODO: properly check if ModuleEngines is active
                }

                foreach (ModuleEngines engine in activeModuleEngines)
                {
                    foreach (Propellant propellant in engine.propellants)
                    {
                        if (!p.Resources.Contains(propellant.id))
                            continue;
                        PartResource r = p.Resources.Get(propellant.id);

                        if (r.amount <= p.resourceRequestRemainingThreshold)
                            continue;
                        if (r.info.id == PartResourceLibrary.ElectricityHashcode)
                            continue;
                        if (!tankResources.Contains(r.info.id))
                            continue;

                        if (propellant.GetFlowMode() == ResourceFlowMode.NO_FLOW)
                        {
                            if (engine.part == p)
                                return true;
                        }
                        else
                        {
                            if (engine.part.crossfeedPartSet.ContainsPart(p))
                                return true;
                        }
                    }
                }
            }

            for (int i = 0; i < p.children.Count; i++)
            {
                if (HasActiveOrIdleEngineOrTankDescendant(p.children[i], tankResources, activeModuleEngines))
                {
                    return true;
                }
            }

            return false;
        }

        //determine whether activating inverseStage will fire any sort of decoupler. This
        //is used to tell whether we should delay activating the next stage after activating inverseStage
        private bool InverseStageFiresDecoupler(int inverseStage)
        {
            if (_inverseStageFiresDecouplerCache.TryGetValue(inverseStage, out bool result))
                return result;
            result = Vessel.Parts.FirstOrDefault(p => p.inverseStage == inverseStage && p.IsUnfiredDecoupler(out Part _)) != null;
            _inverseStageFiresDecouplerCache.Add(inverseStage, result);
            return result;
        }

        //determine whether activating inverseStage will release launch clamps
        private bool InverseStageReleasesClamps(int inverseStage)
        {
            if (_inverseStageReleasesClampsCache.TryGetValue(inverseStage, out bool result))
                return result;
            result = Vessel.Parts.FirstOrDefault(p => p.inverseStage == inverseStage && p.IsLaunchClamp()) != null;
            _inverseStageReleasesClampsCache.Add(inverseStage, result);
            return result;
        }

        //determine whether inverseStage sheds a dead engine
        private bool InverseStageDecouplesDeactivatedEngineOrTank(int inverseStage)
        {
            foreach (PartModule pm in _allDecouplers)
                if (pm.part.inverseStage == inverseStage && pm.IsUnfiredDecoupler(out Part decoupledPart) &&
                    HasDeactivatedEngineOrTankDescendant(decoupledPart))
                    return true;
            return false;
        }

        //detect if a part is above a deactivated engine or fuel tank
        private static bool HasDeactivatedEngineOrTankDescendant(Part p)
        {
            if (p is null)
                return false;

            if (p.State == PartStates.DEACTIVATED && p.IsEngine() && !p.IsSepratron())
            {
                return true; // TODO: yet more ModuleEngine lazy checks
            }

            //check if this is a new-style fuel tank that's run out of resources:
            bool hadResources = false;
            bool hasResources = false;
            for (int i = 0; i < p.Resources.Count; i++)
            {
                PartResource r = p.Resources[i];
                if (r.info.id == PartResourceLibrary.ElectricityHashcode) continue;
                if (r.maxAmount > p.resourceRequestRemainingThreshold) hadResources = true;
                if (r.amount > p.resourceRequestRemainingThreshold) hasResources    = true;
            }

            if (hadResources && !hasResources) return true;

            if (p.IsEngine() && !p.EngineHasFuel()) return true;

            for (int i = 0; i < p.children.Count; i++)
            {
                if (HasDeactivatedEngineOrTankDescendant(p.children[i]))
                    return true;
            }

            return false;
        }

        //determine if there are chutes being fired that wouldn't also get decoupled
        private bool HasStayingChutes(int inverseStage)
        {
            if (_hasStayingChutesCache.TryGetValue(inverseStage, out bool result))
                return result;
            result = Vessel.Parts.FirstOrDefault(p => p.inverseStage == inverseStage && p.IsParachute() && !p.IsDecoupledInStage(inverseStage)) !=
                     null;
            _hasStayingChutesCache.Add(inverseStage, result);
            return result;
        }

        // determine if there is a fairing to be deployed
        private bool HasFairing(int inverseStage)
        {
            if (_hasFairingCache.TryGetValue(inverseStage, out bool result))
                return result;
            result = HasFairingUncached(inverseStage);
            _hasFairingCache.Add(inverseStage, result);
            return result;
        }

        private readonly List<Part> _partsInStage = new List<Part>();

        private bool HasFairingUncached(int inverseStage)
        {
            _partsInStage.Clear();
            Vessel.parts.Slinq().Where((p, s) => p.inverseStage == s, inverseStage).AddTo(_partsInStage);

            // proc parts are reasonably easy, but all the parts in the stage must be payload fairings for them to
            // be treated as payload fairings here.  a payload fairing and a stack decoupler will bypass the fairing
            // checks which will then cause it to be detatched normally when the stack decouples, fixing the issue where
            // fairings block stack separation.

            // ReSharper disable once SuggestVarOrType_Elsewhere
            if (_partsInStage.Slinq().Any(p => p.IsProceduralFairing()))
                // if we have any PF in the stage we must have ALL payload fairings, and we do not do the
                // decoupler check below
                return _partsInStage.Slinq().All(p => p.IsProceduralFairingPayloadFairing());

            // this is simple, but subtle:
            //   1. we do not identify fairings as separate from decouplers here because of part mods like RSB
            //      which only put a stock decoupler in the staging.
            //   2. if we see ONLY decouplers with no child parts and no other parts we assume payload fairing
            //   3. an interstage fairing mixed with a stack decoupler will not be identified as a fairing.
            //   4. a stack decoupler alone in the stage (like RO hotstaging) will not be identified as a fairing
            //      (stack decouplers have children).
            //   5. a payload fairing placed in a stage with a stack decoupler will also not be identified as a
            //      payload fairing now (fixing payload fairings causing stacks to not decouple).
            // if a user requires an interstage fairing that is alone in a stage with no stack decoupler, engine, or
            // anything else in the stage (for cinematics?) then the user MUST use proc fairings.
            return _partsInStage.Slinq().All(p => p.IsDecoupler() && !p.IsLaunchClamp() && p.children.Count == 0);
        }
    }
}
