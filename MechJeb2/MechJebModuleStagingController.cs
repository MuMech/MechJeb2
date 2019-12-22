using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using Smooth.Slinq;
using UnityEngine;
using KSP.Localization;

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
        public EditableDouble autostagePreDelay = 0.5;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble autostagePostDelay = 1.0;
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


        public bool autostagingOnce = false;

        public bool waitingForFirstStaging = false;

        // this is for other modules to temporarily disable autostaging (e.g. PVG coast phases)
        public int autostageLimitInternal = 0;

        private readonly List<ModuleEngines> activeModuleEngines = new List<ModuleEngines>();
        private readonly List<int> burnedResources = new List<int>();
        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }

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

        [GeneralInfoItem("#MechJeb_AutostagingSettings", InfoItem.Category.Misc)]//Autostaging settings
        public void AutostageSettingsInfoItem()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Delays: pre:", GUILayout.ExpandWidth(false));
            autostagePreDelay.text = GUILayout.TextField(autostagePreDelay.text, GUILayout.Width(35));
            GUILayout.Label("s  post:", GUILayout.ExpandWidth(false));
            autostagePostDelay.text = GUILayout.TextField(autostagePostDelay.text, GUILayout.Width(35));
            GUILayout.Label("s", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            ClampAutostageThrust();

            GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label38"));//"Stage fairings when:"
            GuiUtils.SimpleTextBox("  "+Localizer.Format("#MechJeb_Ascent_label39") +" <", fairingMaxDynamicPressure, "kPa", 50);//"dynamic pressure"
            GuiUtils.SimpleTextBox("  "+Localizer.Format("#MechJeb_Ascent_label40") +" >", fairingMinAltitude, "km", 50);//altitude
            GuiUtils.SimpleTextBox("  "+Localizer.Format("#MechJeb_Ascent_label41") +" <", fairingMaxAerothermalFlux, "W/m²", 50);//aerothermal flux

            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label42"), autostageLimit, "");//"Stop at stage #"

            hotStaging = GUILayout.Toggle(hotStaging, Localizer.Format("#MechJeb_Ascent_checkbox21"));//"Support hotstaging"
            if (hotStaging)
                GuiUtils.SimpleTextBox("  "+Localizer.Format("#MechJeb_Ascent_label43"), hotStagingLeadTime, "s");//"lead time"

            GUILayout.EndVertical();
        }

        [ValueInfoItem("#MechJeb_Autostagingstatus", InfoItem.Category.Misc)]//Autostaging status
        public string AutostageStatus()
        {
            if (!this.enabled) return Localizer.Format("#MechJeb_Ascent_status9");//"Autostaging off"
            if (autostagingOnce) return Localizer.Format("#MechJeb_Ascent_status10");//"Will autostage next stage only"
            return Localizer.Format("#MechJeb_Ascent_status11") + (int)autostageLimit;//"Autostaging until stage #"
        }

        [GeneralInfoItem("#MechJeb_ClampAutostageThrust", InfoItem.Category.Misc)]//Clamp Autostage Thrust
        public void ClampAutostageThrust()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label44"));//"Clamp AutoStage Thrust "
            core.staging.clampAutoStageThrustPct.text = GUILayout.TextField(core.staging.clampAutoStageThrustPct.text, 5);
            GUILayout.Label("%");
            core.staging.clampAutoStageThrustPct = UtilMath.Clamp(core.staging.clampAutoStageThrustPct, 0, 100);
            GUILayout.EndVertical();
        }

        //internal state:
        double lastStageTime = 0;

        bool countingDown = false;
        double stageCountdownStart = 0;
        private Vessel currentActiveVessel;

        public override void OnUpdate()
        {
            //Commenting this because now we support autostaging on non active vessels
            //if (!vessel.isActiveVessel)
            //    return;

            //if autostage enabled, and if we've already staged at least once, and if there are stages left,
            //and if we are allowed to continue staging, and if we didn't just fire the previous stage
            if (waitingForFirstStaging || vessel.currentStage <= 0 || vessel.currentStage <= autostageLimit || vessel.currentStage <= autostageLimitInternal
               || vesselState.time - lastStageTime < autostagePostDelay)
                return;

            //don't decouple active or idle engines or tanks
            UpdateActiveModuleEngines();
            UpdateBurnedResources();
            if (InverseStageDecouplesActiveOrIdleEngineOrTank(vessel.currentStage - 1, vessel, burnedResources, activeModuleEngines))
                return;

            // prevent staging when the current stage has active engines and the next stage has any engines (but not decouplers or clamps)
            if (hotStaging && InverseStageHasActiveEngines(vessel.currentStage, vessel) && InverseStageHasEngines(vessel.currentStage - 1, vessel) && !InverseStageFiresDecoupler(vessel.currentStage - 1, vessel) && !InverseStageReleasesClamps(vessel.currentStage - 1, vessel) && LastNonZeroDVStageBurnTime() > hotStagingLeadTime)
                return;

            //Don't fire a stage that will activate a parachute, unless that parachute gets decoupled:
            if (HasStayingChutes(vessel.currentStage - 1, vessel))
                return;

            //Always drop deactivated engines or tanks
            if (!InverseStageDecouplesDeactivatedEngineOrTank(vessel.currentStage - 1, vessel))
            {
                //only decouple fairings if the dynamic pressure, altitude, and aerothermal flux conditions are respected
                if ((core.vesselState.dynamicPressure > fairingMaxDynamicPressure || core.vesselState.altitudeASL < fairingMinAltitude || core.vesselState.freeMolecularAerothermalFlux > fairingMaxAerothermalFlux) &&
                    HasFairing(vessel.currentStage - 1, vessel))
                    return;

                //only release launch clamps if we're at nearly full thrust
                if (vesselState.thrustCurrent / vesselState.thrustAvailable < clampAutoStageThrustPct &&
                    InverseStageReleasesClamps(vessel.currentStage - 1, vessel))
                    return;
            }

            //When we find that we're allowed to stage, start a countdown (with a
            //length given by autostagePreDelay) and only stage once that countdown finishes,
            if (countingDown)
            {
                if (vesselState.time - stageCountdownStart > autostagePreDelay)
                {
                    if (InverseStageFiresDecoupler(vessel.currentStage - 1, vessel))
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
        public static bool InverseStageDecouplesActiveOrIdleEngineOrTank(int inverseStage, Vessel v, List<int> tankResources, List<ModuleEngines> activeModuleEngines)
        {
            for (int i = 0; i < v.parts.Count; i++)
            {
                Part p = v.parts[i];
                Part decoupledPart;
                if (p?.inverseStage == inverseStage && p.IsUnfiredDecoupler(out decoupledPart) &&
                    HasActiveOrIdleEngineOrTankDescendant(decoupledPart, tankResources, activeModuleEngines))
                    return true;
            }
            return false;
        }

        public double LastNonZeroDVStageBurnTime()
        {
            for ( int i = vacStats.Length-1; i >= 0; i-- )
                if ( vacStats[i].deltaTime > 0 )
                    return vacStats[i].deltaTime;
            return 0;
        }

        public static bool InverseStageHasActiveEngines(int inverseStage, Vessel v)
        {
            for (int i = 0; i < v.parts.Count; i++)
            {
                Part p = v.parts[i];
                if (p.inverseStage == inverseStage && p.IsEngine() && p.EngineHasFuel() && !p.IsSepratron())
                {
                    return true;
                }
            }
            return false;
        }

        public static bool InverseStageHasEngines(int inverseStage, Vessel v)
        {
            for (int i = 0; i < v.parts.Count; i++)
            {
                Part p = v.parts[i];
                if (p.inverseStage == inverseStage && p.IsEngine() && !p.IsSepratron())
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateActiveModuleEngines()
        {
            activeModuleEngines.Clear();
            var activeEngines = vessel.parts.Slinq().Where(p => p.inverseStage >= vessel.currentStage && p.IsEngine() && !p.IsSepratron() &&
                                                        !p.IsDecoupledInStage(vessel.currentStage - 1));
            // Part Modules lacks of List access shows the limits of slinq...
            while (activeEngines.current.isSome)
            {
                Part p = activeEngines.current.value;

                for (int i = 0; i < p.Modules.Count; i++)
                {
                    ModuleEngines eng = p.Modules[i] as ModuleEngines;
                    if (eng != null && eng.isEnabled)
                    {
                        activeModuleEngines.Add(eng);
                    }
                }
                activeEngines.Skip();
            }
            activeEngines.Dispose();
        }

        // Find resources burned by engines that will remain after staging (so we wait until tanks are empty before releasing drop tanks)
        public void UpdateBurnedResources()
        {
            burnedResources.Clear();
            activeModuleEngines.Slinq().SelectMany(eng => eng.propellants.Slinq()).Select(prop => prop.id).AddTo(burnedResources);
        }

        //detect if a part is above an active or idle engine in the part tree
        public static bool HasActiveOrIdleEngineOrTankDescendant(Part p, List<int> tankResources, List<ModuleEngines> activeModuleEngines)
        {
            if (p == null)
            {
                return false;
            }
            if ((p.State == PartStates.ACTIVE || p.State == PartStates.IDLE)
                && p.IsEngine() && !p.IsSepratron() && p.EngineHasFuel())
            {
                return true; // TODO: properly check if ModuleEngines is active
            }
            if (!p.IsSepratron())
            {
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
        public static bool InverseStageFiresDecoupler(int inverseStage, Vessel v)
        {
            for (int i = 0; i < v.parts.Count; i++)
            {
                Part p = v.parts[i];
                Part decoupled;
                if (p.inverseStage == inverseStage && p.IsUnfiredDecoupler(out decoupled))
                {
                    return true;
                }
            }
            return false;
        }

        //determine whether activating inverseStage will release launch clamps
        public static bool InverseStageReleasesClamps(int inverseStage, Vessel v)
        {
            for (int i = 0; i < v.parts.Count; i++)
            {
                Part p = v.parts[i];
                if (p.inverseStage == inverseStage && p.IsLaunchClamp())
                {
                    return true;
                }
            }
            return false;
        }

        //determine whether inverseStage sheds a dead engine
        public static bool InverseStageDecouplesDeactivatedEngineOrTank(int inverseStage, Vessel v)
        {
            for (int i = 0; i < v.parts.Count; i++)
            {
                Part p = v.parts[i];
                Part decoupledPart;
                if (p.inverseStage == inverseStage && p.IsUnfiredDecoupler(out decoupledPart) && HasDeactivatedEngineOrTankDescendant(decoupledPart))
                {
                    return true;
                }
            }
            return false;
        }

        //detect if a part is above a deactivated engine or fuel tank
        public static bool HasDeactivatedEngineOrTankDescendant(Part p)
        {
            if ((p.State == PartStates.DEACTIVATED) && (p.IsEngine()) && !p.IsSepratron())
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
                {
                    return true;
                }
            }
            return false;
        }

        //determine if there are chutes being fired that wouldn't also get decoupled
        public static bool HasStayingChutes(int inverseStage, Vessel v)
        {
            var chutes = v.parts.FindAll(p => p.inverseStage == inverseStage && p.IsParachute());

            for (int i = 0; i < chutes.Count; i++)
            {
                Part p = chutes[i];
                if (!p.IsDecoupledInStage(inverseStage))
                {
                    return true;
                }
            }

            return false;
        }

        // determine if there is a fairing to be deployed
        public static bool HasFairing(int inverseStage, Vessel v)
        {
            return v.parts.Slinq().Any((p,_inverseStage) =>
                p.inverseStage == _inverseStage &&
                (p.HasModule<ModuleProceduralFairing>() || (VesselState.isLoadedProceduralFairing && p.Modules.Contains("ProceduralFairingDecoupler"))), inverseStage);
        }
    }
}
