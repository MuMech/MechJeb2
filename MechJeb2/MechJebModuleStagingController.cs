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
        public EditableDouble autostagePreDelay = 0.0;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public EditableDouble autostagePostDelay = 0.5;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public EditableInt autostageLimit = 0;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public EditableDoubleMult fairingMaxDynamicPressure = new EditableDoubleMult(5000, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public EditableDoubleMult fairingMinAltitude = new EditableDoubleMult(50000, 1000);

        [Persistent(pass = (int)Pass.TYPE)]
        public EditableDouble clampAutoStageThrustPct = 0.99;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public EditableDoubleMult fairingMaxAerothermalFlux = new EditableDoubleMult(1135);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool hotStaging;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public EditableDouble hotStagingLeadTime = 1.0;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool dropSolids;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public EditableDouble dropSolidsLeadTime = 1.0;

        public bool autostagingOnce;

        public bool waitingForFirstStaging;

        // this is for other modules to temporarily disable autostaging (e.g. PVG coast phases)
        public int autostageLimitInternal;

        private readonly List<ModuleEngines>     activeModuleEngines             = new List<ModuleEngines>(16);
        private readonly List<ModuleEngines>     allModuleEngines                = new List<ModuleEngines>(16);
        private readonly List<PartModule>        allDecouplers                   = new List<PartModule>(16);
        private readonly List<int>               burnedResources                 = new List<int>(16);
        private readonly Dictionary<int, bool>   inverseStageHasEngines          = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool>   inverseStageFiresDecouplerCache = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool>   inverseStageReleasesClampsCache = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool>   hasStayingChutesCache           = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool>   hasFairingCache                 = new Dictionary<int, bool>(16);
        private          MechJebModuleStageStats stats    => Core.GetComputerModule<MechJebModuleStageStats>();
        private          List<FuelStats>         vacStats => stats.vacStats;

        private enum RemoteStagingState
        {
            Disabled,
            WaitingFocus,
            FocusFinished
        }

        private RemoteStagingState remoteStagingStatus = RemoteStagingState.Disabled;

        public override void OnStart(PartModule.StartState state)
        {
            if (Vessel != null && Vessel.situation == Vessel.Situations.PRELAUNCH)
                waitingForFirstStaging = true;

            GameEvents.onStageActivate.Add(stageActivate);
            GameEvents.onVesselResumeStaging.Add(VesselResumeStage);
        }

        private void RegenerateCaches()
        {
            ClearCaches();
            BuildEnginesCache(allModuleEngines);
            BuildDecouplersCache(allDecouplers);
        }

        private void OnGUIStageSequenceModified()
        {
            RegenerateCaches();
        }

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
            inverseStageHasEngines.Clear();
            inverseStageFiresDecouplerCache.Clear();
            inverseStageReleasesClampsCache.Clear();
            hasStayingChutesCache.Clear();
            hasFairingCache.Clear();
        }

        private void VesselResumeStage(Vessel data)
        {
            if (remoteStagingStatus == RemoteStagingState.WaitingFocus)
            {
                remoteStagingStatus = RemoteStagingState.FocusFinished;
            }
        }

        public override void OnDestroy()
        {
            GameEvents.onStageActivate.Remove(stageActivate);
            GameEvents.onVesselResumeStaging.Remove(VesselResumeStage);
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.StageManager.OnGUIStageSequenceModified.Remove(OnGUIStageSequenceModified);
        }

        private void stageActivate(int data)
        {
            waitingForFirstStaging = false;
        }

        public void AutostageOnce(object user)
        {
            Users.Add(user);
            autostagingOnce = true;
        }

        protected override void OnModuleEnabled()
        {
            autostageLimitInternal = 0;
        }

        protected override void OnModuleDisabled()
        {
            autostagingOnce = false;
        }

        private readonly string sFairingMinDynamicPressure = $"  {CachedLocalizer.Instance.MechJeb_Ascent_label39} <";
        private readonly string sFairingMinAltitude        = $"  {CachedLocalizer.Instance.MechJeb_Ascent_label40} >";
        private readonly string sFairingMaxAerothermalFlux = $"  {CachedLocalizer.Instance.MechJeb_Ascent_label41} <";

        private readonly string sHotstaging =
            $"{CachedLocalizer.Instance.MechJeb_Ascent_hotStaging} {CachedLocalizer.Instance.MechJeb_Ascent_leadTime}";

        private readonly string sDropSolids =
            $"{CachedLocalizer.Instance.MechJeb_Ascent_dropSolids} {CachedLocalizer.Instance.MechJeb_Ascent_leadTime}";

        private readonly string sLeadTime = $"{CachedLocalizer.Instance.MechJeb_Ascent_leadTime}: ";

        [GeneralInfoItem("#MechJeb_AutostagingSettings", InfoItem.Category.Misc)] //Autostaging settings
        public void AutostageSettingsInfoItem()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label42, autostageLimit, width: 40); //"Stop at stage #"

            GUILayout.BeginHorizontal();
            GUILayout.Label("Delays: ");
            GUILayout.Space(30);
            GuiUtils.SimpleTextBox("pre: ", autostagePreDelay, "s", 35, horizontalFraming: false);
            GUILayout.FlexibleSpace();
            GuiUtils.SimpleTextBox("post: ", autostagePostDelay, "s", 35, horizontalFraming: false);
            GUILayout.EndHorizontal();

            ClampAutostageThrust();

            GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label38);                          //"Stage fairings when:"
            GuiUtils.SimpleTextBox(sFairingMinDynamicPressure, fairingMaxDynamicPressure, "kPa", 50);  //"dynamic pressure"
            GuiUtils.SimpleTextBox(sFairingMinAltitude, fairingMinAltitude, "km", 50);                 //altitude
            GuiUtils.SimpleTextBox(sFairingMaxAerothermalFlux, fairingMaxAerothermalFlux, "W/m²", 50); //aerothermal flux

            GUILayout.BeginHorizontal();
            hotStaging = GUILayout.Toggle(hotStaging, CachedLocalizer.Instance.MechJeb_Ascent_hotStaging); //"Support hotstaging"
            GUILayout.FlexibleSpace();
            GuiUtils.SimpleTextBox(sLeadTime, hotStagingLeadTime, "s", 35); //"lead time"
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            dropSolids = GUILayout.Toggle(dropSolids, CachedLocalizer.Instance.MechJeb_Ascent_dropSolids); //"Drop solids early"
            GUILayout.FlexibleSpace();
            GuiUtils.SimpleTextBox(sLeadTime, dropSolidsLeadTime, "s", 35); //"lead time"
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        [ValueInfoItem("#MechJeb_Autostagingstatus", InfoItem.Category.Misc)] //Autostaging status
        public string AutostageStatus()
        {
            if (!Enabled) return CachedLocalizer.Instance.MechJeb_Ascent_status9;          //"Autostaging off"
            if (autostagingOnce) return CachedLocalizer.Instance.MechJeb_Ascent_status10;  //"Will autostage next stage only"
            return CachedLocalizer.Instance.MechJeb_Ascent_status11 + (int)autostageLimit; //"Autostaging until stage #"
        }

        [GeneralInfoItem("#MechJeb_ClampAutostageThrust", InfoItem.Category.Misc)] //Clamp Autostage Thrust
        public void ClampAutostageThrust()
        {
            double prev = Core.Staging.clampAutoStageThrustPct;
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label44, Core.Staging.clampAutoStageThrustPct, "%",
                50); //"Clamp AutoStage Thrust "
            if (prev != Core.Staging.clampAutoStageThrustPct)
                Core.Staging.clampAutoStageThrustPct = UtilMath.Clamp(Core.Staging.clampAutoStageThrustPct, 0, 100);
        }

        //internal state:
        private double lastStageTime;

        private bool   countingDown;
        private double stageCountdownStart;
        private Vessel currentActiveVessel;

        private bool initializedOnce;

        public override void OnUpdate()
        {
            if (!initializedOnce)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    GameEvents.onVesselWasModified.Add(OnVesselModified);
                    GameEvents.StageManager.OnGUIStageSequenceModified.Add(OnGUIStageSequenceModified);
                }

                initializedOnce = true;
                RegenerateCaches();
            }

            // if autostage enabled, and if we've already staged at least once, and if there are stages left,
            // and if we are allowed to continue staging, and if we didn't just fire the previous stage
            if (waitingForFirstStaging || Vessel.currentStage <= 0 || Vessel.currentStage <= autostageLimit ||
                VesselState.time - lastStageTime < autostagePostDelay)
            {
                return;
            }

            // this is for PVG preventing staging doing coasts, possibly it should be more specific of an API
            // (e.g. bool PVGIsCoasting) since it is getting tightly coupled.
            if (Vessel.currentStage <= autostageLimitInternal)
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

            UpdateActiveModuleEngines(allModuleEngines);
            UpdateBurnedResources();

            // don't decouple active or idle engines or tanks
            if (InverseStageDecouplesActiveOrIdleEngineOrTank(Vessel.currentStage - 1, burnedResources, activeModuleEngines) &&
                !InverseStageReleasesClamps(Vessel.currentStage - 1))
                return;

            // prevent staging if we have unstable ullage and we have RCS
            if (InverseStageHasUnstableEngines(Vessel.currentStage - 1) && Core.Thrust.autoRCSUllaging && Vessel.hasEnabledRCSModules() &&
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
            if (hotStaging && InverseStageHasEngines(Vessel.currentStage - 1) &&
                !InverseStageFiresDecoupler(Vessel.currentStage - 1) && !InverseStageReleasesClamps(Vessel.currentStage - 1) &&
                LastNonZeroDVStageBurnTime() > hotStagingLeadTime)
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
            if ((VesselState.thrustCurrent / VesselState.thrustAvailable < clampAutoStageThrustPct || AnyFailedEngines(allModuleEngines)) &&
                InverseStageReleasesClamps(Vessel.currentStage - 1))
                return;

            Stage();
        }

        private bool WaitingForFairing()
        {
            if (!HasFairing(Vessel.currentStage - 1))
                return false;

            if (Core.VesselState.dynamicPressure > fairingMaxDynamicPressure)
                return true;

            if (Core.VesselState.altitudeASL < fairingMinAltitude)
                return true;

            if (Core.VesselState.freeMolecularAerothermalFlux > fairingMaxAerothermalFlux)
                return true;

            return false;
        }

        public void Stage()
        {
            //When we find that we're allowed to stage, start a countdown (with a
            //length given by autostagePreDelay) and only stage once that countdown finishes,
            if (countingDown)
            {
                if (VesselState.time - stageCountdownStart > autostagePreDelay)
                {
                    if (InverseStageFiresDecoupler(Vessel.currentStage - 1))
                    {
                        //if we decouple things, delay the next stage a bit to avoid exploding the debris
                        lastStageTime = VesselState.time;
                    }

                    if (!Vessel.isActiveVessel)
                    {
                        currentActiveVessel = FlightGlobals.ActiveVessel;
                        Debug.Log($"Mechjeb Autostage: Switching from {FlightGlobals.ActiveVessel.name} to vessel {Vessel.name} to stage");

                        remoteStagingStatus = RemoteStagingState.WaitingFocus;
                        FlightGlobals.ForceSetActiveVessel(Vessel);
                    }
                    else
                    {
                        Debug.Log($"Mechjeb Autostage: Executing next stage on {FlightGlobals.ActiveVessel.name}");

                        if (remoteStagingStatus == RemoteStagingState.Disabled)
                        {
                            StageManager.ActivateNextStage();
                        }
                        else if (remoteStagingStatus == RemoteStagingState.FocusFinished)
                        {
                            StageManager.ActivateNextStage();
                            FlightGlobals.ForceSetActiveVessel(currentActiveVessel);
                            Debug.Log($"Mechjeb Autostage: Has switching back to {FlightGlobals.ActiveVessel.name} ");
                            remoteStagingStatus = RemoteStagingState.Disabled;
                        }
                    }

                    countingDown = false;

                    if (autostagingOnce)
                        Users.Clear();
                }
            }
            else
            {
                countingDown        = true;
                stageCountdownStart = VesselState.time;
            }
        }

        //determine whether it's safe to activate inverseStage
        public bool InverseStageDecouplesActiveOrIdleEngineOrTank(int inverseStage, List<int> tankResources, List<ModuleEngines> activeModuleEngines)
        {
            foreach (PartModule pm in allDecouplers)
                if (pm.part.inverseStage == inverseStage &&
                    pm.IsUnfiredDecoupler(out Part decoupledPart) &&
                    HasActiveOrIdleEngineOrTankDescendant(decoupledPart, tankResources, activeModuleEngines))
                    return true;
            return false;
        }

        public double LastNonZeroDVStageBurnTime()
        {
            for (int i = vacStats.Count - 1; i >= 0; i--)
                if (vacStats[i].DeltaTime > 0)
                    return vacStats[i].DeltaTime;
            return 0;
        }

        // allModuleEngines => IsEngine() && !IsSepratron()
        public bool InverseStageHasActiveEngines(int inverseStage)
        {
            foreach (ModuleEngines engine in allModuleEngines)
                if (engine.part.inverseStage == inverseStage && engine.EngineHasFuel())
                    return true;
            return false;
        }

        public bool InverseStageHasEngines(int inverseStage)
        {
            if (inverseStageHasEngines.TryGetValue(inverseStage, out bool result))
                return result;
            result = allModuleEngines.FirstOrDefault(me => me.part.inverseStage == inverseStage) != null;
            inverseStageHasEngines.Add(inverseStage, result);
            return result;
        }

        public bool InverseStageHasUnstableEngines(int inverseStage)
        {
            // don't just blindly add caching here because this value is volatile and changes with ullage status
            foreach (ModuleEngines engine in allModuleEngines)
                if (engine.part.inverseStage == inverseStage && engine.part.UnstableUllage())
                    return true;

            return false;
        }

        public bool AnyFailedEngines(List<ModuleEngines> allEngines)
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

        public void UpdateActiveModuleEngines(List<ModuleEngines> allEngines)
        {
            activeModuleEngines.Clear();
            foreach (ModuleEngines engine in allEngines)
            {
                Part p = engine.part;
                if (p.inverseStage >= Vessel.currentStage && !p.IsDecoupledInStage(Vessel.currentStage - 1) && engine.isEnabled)
                    activeModuleEngines.Add(engine);
            }
        }

        // Find resources burned by engines that will remain after staging (so we wait until tanks are empty before releasing drop tanks)
        public void UpdateBurnedResources()
        {
            burnedResources.Clear();
            activeModuleEngines.Slinq().SelectMany(eng => eng.propellants.Slinq()).Select(prop => prop.id).AddTo(burnedResources);
        }

        // detect if this part is an SRB, will be dropped in the next stage, and we are below the enabled dropSolidsLeadTime
        public bool isBurnedOutSRBDecoupledInNextStage(Part p)
        {
            return dropSolids && p.IsThrottleLockedEngine() && LastNonZeroDVStageBurnTime() < dropSolidsLeadTime &&
                   p.IsDecoupledInStage(Vessel.currentStage - 1);
        }

        //detect if a part is above an active or idle engine in the part tree
        private bool HasActiveOrIdleEngineOrTankDescendant(Part p, List<int> tankResources, List<ModuleEngines> activeModuleEngines)
        {
            if (p is null)
                return false;

            if (!p.IsSepratron() && !isBurnedOutSRBDecoupledInNextStage(p))
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
        public bool InverseStageFiresDecoupler(int inverseStage)
        {
            if (inverseStageFiresDecouplerCache.TryGetValue(inverseStage, out bool result))
                return result;
            result = Vessel.Parts.FirstOrDefault(p => p.inverseStage == inverseStage && p.IsUnfiredDecoupler(out Part _)) != null;
            inverseStageFiresDecouplerCache.Add(inverseStage, result);
            return result;
        }

        //determine whether activating inverseStage will release launch clamps
        public bool InverseStageReleasesClamps(int inverseStage)
        {
            if (inverseStageReleasesClampsCache.TryGetValue(inverseStage, out bool result))
                return result;
            result = Vessel.Parts.FirstOrDefault(p => p.inverseStage == inverseStage && p.IsLaunchClamp()) != null;
            inverseStageReleasesClampsCache.Add(inverseStage, result);
            return result;
        }

        //determine whether inverseStage sheds a dead engine
        public bool InverseStageDecouplesDeactivatedEngineOrTank(int inverseStage)
        {
            foreach (PartModule pm in allDecouplers)
                if (pm.part.inverseStage == inverseStage && pm.IsUnfiredDecoupler(out Part decoupledPart) &&
                    HasDeactivatedEngineOrTankDescendant(decoupledPart))
                    return true;
            return false;
        }

        //detect if a part is above a deactivated engine or fuel tank
        public static bool HasDeactivatedEngineOrTankDescendant(Part p)
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
        public bool HasStayingChutes(int inverseStage)
        {
            if (hasStayingChutesCache.TryGetValue(inverseStage, out bool result))
                return result;
            result = Vessel.Parts.FirstOrDefault(p => p.inverseStage == inverseStage && p.IsParachute() && !p.IsDecoupledInStage(inverseStage)) !=
                     null;
            hasStayingChutesCache.Add(inverseStage, result);
            return result;
        }

        // determine if there is a fairing to be deployed
        private bool HasFairing(int inverseStage)
        {
            if (hasFairingCache.TryGetValue(inverseStage, out bool result))
                return result;
            result = HasFairingUncached(inverseStage);
            hasFairingCache.Add(inverseStage, result);
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
