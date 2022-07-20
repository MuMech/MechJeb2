using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using Smooth.Slinq;
using UnityEngine;
using KSP.Localization;
using System.Linq;

namespace MuMech
{
    public class MechJebModuleStagingController : ComputerModule
    {
        public MechJebModuleStagingController(MechJebCore core)
            : base(core)
        {
            priority = 1000;
        }

        //adjustable parameters:
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble autostagePreDelay = 0.0;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble autostagePostDelay = 0.5;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableInt autostageLimit = 0;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult fairingMaxDynamicPressure = new EditableDoubleMult(5000, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult fairingMinAltitude = new EditableDoubleMult(50000, 1000);
        [Persistent(pass = (int)Pass.Type)]
        public EditableDouble clampAutoStageThrustPct = 0.99;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult fairingMaxAerothermalFlux = new EditableDoubleMult(1135, 1);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool hotStaging = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble hotStagingLeadTime = 1.0;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool dropSolids = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble dropSolidsLeadTime = 1.0;


        public bool autostagingOnce = false;

        public bool waitingForFirstStaging = false;

        // this is for other modules to temporarily disable autostaging (e.g. PVG coast phases)
        public int autostageLimitInternal = 0;

        private readonly List<ModuleEngines> activeModuleEngines = new List<ModuleEngines>(16);
        private readonly List<ModuleEngines> allModuleEngines = new List<ModuleEngines>(16);
        private readonly List<PartModule> allDecouplers = new List<PartModule>(16);
        private readonly List<int> burnedResources = new List<int>(16);
        private readonly Dictionary<int, bool> inverseStageHasEngines = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool> inverseStageFiresDecouplerCache = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool> inverseStageReleasesClampsCache = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool> hasStayingChutesCache = new Dictionary<int, bool>(16);
        private readonly Dictionary<int, bool> hasFairingCache = new Dictionary<int, bool>(16);
        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.FuelStats[] vacStats { get { return stats.vacStats; } }

        private enum RemoteStagingState
        {
            Disabled,
            WaitingFocus,
            FocusFinished,
        }

        private RemoteStagingState remoteStagingStatus = RemoteStagingState.Disabled;

        public override void OnStart(PartModule.StartState state)
        {
            if (vessel != null && vessel.situation == Vessel.Situations.PRELAUNCH)
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
        private void OnGUIStageSequenceModified() => RegenerateCaches();
        private void OnVesselModified(Vessel v)
        {
            if (vessel == v) RegenerateCaches();
        }

        private void BuildEnginesCache(List<ModuleEngines> engines)
        {
            engines.Clear();
            foreach (Part p in vessel.Parts)
                if (p.IsEngine() && !p.IsSepratron())
                    engines.AddRange(p.FindModulesImplementing<ModuleEngines>());
        }

        private void BuildDecouplersCache(List<PartModule> decouplers)
        {
            decouplers.Clear();
            foreach (Part p in vessel.Parts)
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
            users.Add(user);
            autostagingOnce = true;
        }

        public override void OnModuleEnabled()
        {
            autostageLimitInternal = 0;
        }

        public override void OnModuleDisabled()
        {
            autostagingOnce = false;
        }

        private readonly string sFairingMinDynamicPressure = $"  {CachedLocalizer.Instance.MechJeb_Ascent_label39} <";
        private readonly string sFairingMinAltitude = $"  {CachedLocalizer.Instance.MechJeb_Ascent_label40} >";
        private readonly string sFairingMaxAerothermalFlux = $"  {CachedLocalizer.Instance.MechJeb_Ascent_label41} <";
        private readonly string sHotstaging = $"{CachedLocalizer.Instance.MechJeb_Ascent_hotStaging} {CachedLocalizer.Instance.MechJeb_Ascent_leadTime}";
        private readonly string sDropSolids = $"{CachedLocalizer.Instance.MechJeb_Ascent_dropSolids} {CachedLocalizer.Instance.MechJeb_Ascent_leadTime}";
        private readonly string sLeadTime = $"{CachedLocalizer.Instance.MechJeb_Ascent_leadTime}: ";
        [GeneralInfoItem("#MechJeb_AutostagingSettings", InfoItem.Category.Misc)]//Autostaging settings
        public void AutostageSettingsInfoItem()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Delays: ");
            GUILayout.Space(30);
            GuiUtils.SimpleTextBox("pre: ",autostagePreDelay,"s",35,horizontalFraming: false);
            GUILayout.FlexibleSpace();
            GuiUtils.SimpleTextBox("post: ",autostagePostDelay,"s",35,horizontalFraming: false);
            GUILayout.EndHorizontal();

            ClampAutostageThrust();

            GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label38);//"Stage fairings when:"
            GuiUtils.SimpleTextBox(sFairingMinDynamicPressure, fairingMaxDynamicPressure, "kPa", 50);//"dynamic pressure"
            GuiUtils.SimpleTextBox(sFairingMinAltitude, fairingMinAltitude, "km", 50);//altitude
            GuiUtils.SimpleTextBox(sFairingMaxAerothermalFlux, fairingMaxAerothermalFlux, "W/m²", 50);//aerothermal flux

            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label42, autostageLimit, width: 40);//"Stop at stage #"

            GUILayout.BeginHorizontal();
            hotStaging = GUILayout.Toggle(hotStaging,CachedLocalizer.Instance.MechJeb_Ascent_hotStaging);//"Support hotstaging"
            GUILayout.FlexibleSpace();
            GuiUtils.SimpleTextBox(sLeadTime,hotStagingLeadTime,"s",35);//"lead time"
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            dropSolids = GUILayout.Toggle(dropSolids,CachedLocalizer.Instance.MechJeb_Ascent_dropSolids);//"Drop solids early"
            GUILayout.FlexibleSpace();
            GuiUtils.SimpleTextBox(sLeadTime,dropSolidsLeadTime,"s",35);//"lead time"
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        [ValueInfoItem("#MechJeb_Autostagingstatus", InfoItem.Category.Misc)]//Autostaging status
        public string AutostageStatus()
        {
            if (!this.enabled) return CachedLocalizer.Instance.MechJeb_Ascent_status9;//"Autostaging off"
            if (autostagingOnce) return CachedLocalizer.Instance.MechJeb_Ascent_status10;//"Will autostage next stage only"
            return CachedLocalizer.Instance.MechJeb_Ascent_status11 + (int)autostageLimit;//"Autostaging until stage #"
        }

        [GeneralInfoItem("#MechJeb_ClampAutostageThrust", InfoItem.Category.Misc)]//Clamp Autostage Thrust
        public void ClampAutostageThrust()
        {
            double prev = core.staging.clampAutoStageThrustPct;
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label44,core.staging.clampAutoStageThrustPct,"%",width: 50);//"Clamp AutoStage Thrust "
            if (prev != core.staging.clampAutoStageThrustPct)
                core.staging.clampAutoStageThrustPct = UtilMath.Clamp(core.staging.clampAutoStageThrustPct,0,100);
        }

        //internal state:
        double lastStageTime = 0;

        bool countingDown = false;
        double stageCountdownStart = 0;
        private Vessel currentActiveVessel;

        private bool initializedOnce = false;
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

            //if autostage enabled, and if we've already staged at least once, and if there are stages left,
            //and if we are allowed to continue staging, and if we didn't just fire the previous stage
            if (waitingForFirstStaging || vessel.currentStage <= 0 || vessel.currentStage <= autostageLimit || vessel.currentStage <= autostageLimitInternal
               || vesselState.time - lastStageTime < autostagePostDelay)
                return;

            //don't decouple active or idle engines or tanks
            UpdateActiveModuleEngines(allModuleEngines);
            UpdateBurnedResources();
            if (InverseStageDecouplesActiveOrIdleEngineOrTank(vessel.currentStage - 1, burnedResources, activeModuleEngines) && !InverseStageReleasesClamps(vessel.currentStage - 1))
                return;

            // prevent staging when the current stage has active engines and the next stage has any engines (but not decouplers or clamps)
            if (hotStaging && InverseStageHasActiveEngines(vessel.currentStage) && InverseStageHasEngines(vessel.currentStage - 1) && !InverseStageFiresDecoupler(vessel.currentStage - 1) && !InverseStageReleasesClamps(vessel.currentStage - 1) && LastNonZeroDVStageBurnTime() > hotStagingLeadTime)
                return;
            
            // FIXME: prevent staging if the next stage has engines with unstable ullage (and throttle is 100%?)

            //Don't fire a stage that will activate a parachute, unless that parachute gets decoupled:
            if (HasStayingChutes(vessel.currentStage - 1))
                return;

            //Always drop deactivated engines or tanks
            if (!InverseStageDecouplesDeactivatedEngineOrTank(vessel.currentStage - 1))
            {
                //only decouple fairings if the dynamic pressure, altitude, and aerothermal flux conditions are respected
                if ((core.vesselState.dynamicPressure > fairingMaxDynamicPressure || core.vesselState.altitudeASL < fairingMinAltitude || core.vesselState.freeMolecularAerothermalFlux > fairingMaxAerothermalFlux) &&
                    HasFairing(vessel.currentStage - 1))
                    return;

                //only release launch clamps if we're at nearly full thrust and no failed engines
                if ((vesselState.thrustCurrent / vesselState.thrustAvailable < clampAutoStageThrustPct || AnyFailedEngines(allModuleEngines)) &&
                    InverseStageReleasesClamps(vessel.currentStage - 1))
                    return;
            }

            //When we find that we're allowed to stage, start a countdown (with a
            //length given by autostagePreDelay) and only stage once that countdown finishes,
            if (countingDown)
            {
                if (vesselState.time - stageCountdownStart > autostagePreDelay)
                {
                    if (InverseStageFiresDecoupler(vessel.currentStage - 1))
                    {
                        //if we decouple things, delay the next stage a bit to avoid exploding the debris
                        lastStageTime = vesselState.time;
                    }

                    if (!this.vessel.isActiveVessel)
                    {
                        this.currentActiveVessel = FlightGlobals.ActiveVessel;
                        Debug.Log($"Mechjeb Autostage: Switching from {FlightGlobals.ActiveVessel.name} to vessel {this.vessel.name} to stage");

                        this.remoteStagingStatus = RemoteStagingState.WaitingFocus;
                        FlightGlobals.ForceSetActiveVessel(this.vessel);
                    }
                    else
                    {
                        Debug.Log($"Mechjeb Autostage: Executing next stage on {FlightGlobals.ActiveVessel.name}");

                        if (remoteStagingStatus == RemoteStagingState.Disabled)
                        {
                            StageManager.ActivateNextStage();
                        }
                        else if(remoteStagingStatus == RemoteStagingState.FocusFinished)
                        {
                            StageManager.ActivateNextStage();
                            FlightGlobals.ForceSetActiveVessel(currentActiveVessel);
                            Debug.Log($"Mechjeb Autostage: Has switching back to {FlightGlobals.ActiveVessel.name} ");
                            this.remoteStagingStatus = RemoteStagingState.Disabled;
                        }
                    }
                    countingDown = false;

                    if (autostagingOnce)
                        users.Clear();
                }
            }
            else
            {
                countingDown = true;
                stageCountdownStart = vesselState.time;
            }
        }

        //determine whether it's safe to activate inverseStage
        public bool InverseStageDecouplesActiveOrIdleEngineOrTank(int inverseStage, List<int> tankResources, List<ModuleEngines> activeModuleEngines)
        {
            foreach (PartModule pm in allDecouplers)
                if (pm.part.inverseStage == inverseStage && pm.IsUnfiredDecoupler(out Part decoupledPart) && HasActiveOrIdleEngineOrTankDescendant(decoupledPart, tankResources, activeModuleEngines))
                    return true;
            return false;
        }

        public double LastNonZeroDVStageBurnTime()
        {
            for ( int i = vacStats.Length-1; i >= 0; i-- )
                if ( vacStats[i].DeltaTime > 0 )
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
        
        // FIXME: need to be able to tell if an unignited, unactived ModuleEngines has unstable ullage
        public bool InverseStageHasUnstableEngines(int inverseStage)
        {
            foreach (ModuleEngines engine in allModuleEngines)
                if (engine.part.inverseStage == inverseStage)
                    return true;
            
            return false;
        }

        public bool AnyFailedEngines(List<ModuleEngines> allEngines)
        {
            foreach (ModuleEngines engine in allEngines)
            {
                Part p = engine.part;
                if (p.inverseStage >= vessel.currentStage && !p.IsDecoupledInStage(vessel.currentStage - 1) && engine.isEnabled && !engine.EngineIgnited && engine.allowShutdown)
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
                if (p.inverseStage >= vessel.currentStage && !p.IsDecoupledInStage(vessel.currentStage - 1) && engine.isEnabled)
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
            return dropSolids && p.IsThrottleLockedEngine() && LastNonZeroDVStageBurnTime() < dropSolidsLeadTime && p.IsDecoupledInStage(vessel.currentStage - 1);
        }

        //detect if a part is above an active or idle engine in the part tree
        public bool HasActiveOrIdleEngineOrTankDescendant(Part p, List<int> tankResources, List<ModuleEngines> activeModuleEngines)
        {
            if (p == null)
            {
                return false;
            }

            if (!p.IsSepratron() && !isBurnedOutSRBDecoupledInNextStage(p))
            {
                if ((p.State == PartStates.ACTIVE || p.State == PartStates.IDLE) && p.EngineHasFuel())
                {
                    return true; // TODO: properly check if ModuleEngines is active
                }
                for (int i = 0; i < p.Resources.Count; i++)
                {
                    PartResource r = p.Resources[i];
                    if (r.amount > p.resourceRequestRemainingThreshold && r.info.id != PartResourceLibrary.ElectricityHashcode && tankResources.Contains(r.info.id))
                    {
                        foreach (ModuleEngines engine in activeModuleEngines)
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
            result = vessel.Parts.FirstOrDefault(p => p.inverseStage == inverseStage && p.IsUnfiredDecoupler(out Part _)) != null;
            inverseStageFiresDecouplerCache.Add(inverseStage, result);
            return result;
        }

        //determine whether activating inverseStage will release launch clamps
        public bool InverseStageReleasesClamps(int inverseStage)
        {
            if (inverseStageReleasesClampsCache.TryGetValue(inverseStage, out bool result))
                return result;
            result = vessel.Parts.FirstOrDefault(p => p.inverseStage == inverseStage && p.IsLaunchClamp()) != null;
            inverseStageReleasesClampsCache.Add(inverseStage, result);
            return result;
        }

        //determine whether inverseStage sheds a dead engine
        public bool InverseStageDecouplesDeactivatedEngineOrTank(int inverseStage)
        {
            foreach (PartModule pm in allDecouplers)
                if (pm.part.inverseStage == inverseStage && pm.IsUnfiredDecoupler(out Part decoupledPart) && HasDeactivatedEngineOrTankDescendant(decoupledPart))
                    return true;
            return false;
        }

        //detect if a part is above a deactivated engine or fuel tank
        public static bool HasDeactivatedEngineOrTankDescendant(Part p)
        {
            if (p is null)
                return false;

            if ((p.State == PartStates.DEACTIVATED) && p.IsEngine() && !p.IsSepratron())
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
                if (r.amount > p.resourceRequestRemainingThreshold) hasResources = true;
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
            result = vessel.Parts.FirstOrDefault(p => p.inverseStage == inverseStage && p.IsParachute() && !p.IsDecoupledInStage(inverseStage)) != null;
            hasStayingChutesCache.Add(inverseStage, result);
            return result;
        }

        // determine if there is a fairing to be deployed
        public bool HasFairing(int inverseStage)
        {
            if (hasFairingCache.TryGetValue(inverseStage, out bool result))
                return result;
            result = vessel.Parts.FirstOrDefault(p => p.inverseStage == inverseStage
                                                && (p.HasModule<ModuleProceduralFairing>() || (VesselState.isLoadedProceduralFairing && p.Modules.Contains("ProceduralFairingDecoupler")))) != null;
            hasFairingCache.Add(inverseStage, result);
            return result;
        }
    }
}
