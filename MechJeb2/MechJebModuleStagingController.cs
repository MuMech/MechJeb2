using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
        [Persistent(pass = (int)Pass.Type)]
        public EditableInt autostageLimit = 0;

        public bool autostagingOnce = false;

        public void AutostageOnce(object user)
        {
            users.Add(user);
            autostagingOnce = true;
        }

        public override void OnModuleDisabled()
        {
            autostagingOnce = false;
        }

        [GeneralInfoItem("Autostaging settings", InfoItem.Category.Misc)]
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

            GuiUtils.SimpleTextBox("Stop at stage #", autostageLimit, "");

            GUILayout.EndVertical();
        }

        [ValueInfoItem("Autostaging status", InfoItem.Category.Misc)]
        public string AutostageStatus()
        {
            if (!this.enabled) return "Autostaging off";
            if (autostagingOnce) return "Will autostage next stage only";
            return "Autostaging until stage #" + (int)autostageLimit;
        }

        //internal state:
        double lastStageTime = 0;

        bool countingDown = false;
        double stageCountdownStart = 0;

        public override void OnUpdate()
        {
            if (!vessel.isActiveVessel) return;
            
            //if autostage enabled, and if we are not waiting on the pad, and if there are stages left,
            //and if we are allowed to continue staging, and if we didn't just fire the previous stage
            if (vessel.LiftedOff() && Staging.CurrentStage > 0 && Staging.CurrentStage > autostageLimit
                && vesselState.time - lastStageTime > autostagePostDelay)
            {
                //don't decouple active or idle engines or tanks
                List<int> burnedResources = FindBurnedResources();
                if (!InverseStageDecouplesActiveOrIdleEngineOrTank(Staging.CurrentStage - 1, vessel, burnedResources))
                {
                    //Don't fire a stage that will activate a parachute, unless that parachute gets decoupled:
                    if (!HasStayingChutes(Staging.CurrentStage - 1, vessel))
                    {
                        //only fire decouplers to drop deactivated engines or tanks
                        bool firesDecoupler = InverseStageFiresDecoupler(Staging.CurrentStage - 1, vessel);
                        if (!firesDecoupler || InverseStageDecouplesDeactivatedEngineOrTank(Staging.CurrentStage - 1, vessel))
                        {
                            //When we find that we're allowed to stage, start a countdown (with a 
                            //length given by autostagePreDelay) and only stage once that countdown finishes,
                            if (countingDown)
                            {
                                if (vesselState.time - stageCountdownStart > autostagePreDelay)
                                {
                                    if (firesDecoupler)
                                    {
                                        //if we decouple things, delay the next stage a bit to avoid exploding the debris
                                        lastStageTime = vesselState.time;
                                    }

                                    Staging.ActivateNextStage();
                                    countingDown = false;

                                    if (autostagingOnce) users.Clear();
                                }
                            }
                            else
                            {
                                countingDown = true;
                                stageCountdownStart = vesselState.time;
                            }
                        }
                    }
                }
            }
        }

        //determine whether it's safe to activate inverseStage
        public static bool InverseStageDecouplesActiveOrIdleEngineOrTank(int inverseStage, Vessel v, List<int> tankResources)
        {
            for (int i = 0; i < v.parts.Count; i++)
            {
                Part p = v.parts[i];
                if (p.inverseStage == inverseStage && p.IsUnfiredDecoupler() && HasActiveOrIdleEngineOrTankDescendant(p, tankResources))
                {
                    return true;
                }
            }
            return false;
        }

        public List<int> FindBurnedResources()
        {
            var activeEngines = vessel.parts.Where(p => p.inverseStage >= Staging.CurrentStage && p.IsEngine() && !p.IsSepratron());
            var engineModules = activeEngines.Select(p => p.Modules.OfType<ModuleEngines>().First(e => e.isEnabled));
            var burnedPropellants = engineModules.SelectMany(eng => eng.propellants);
            List<int> propellantIDs = burnedPropellants.Select(prop => prop.id).ToList();

            return propellantIDs;
        }

        //detect if a part is above an active or idle engine in the part tree
        public static bool HasActiveOrIdleEngineOrTankDescendant(Part p, List<int> tankResources)
        {
            if ((p.State == PartStates.ACTIVE || p.State == PartStates.IDLE)
                && p.IsEngine() && !p.IsSepratron() && p.EngineHasFuel())
            {
                return true; // TODO: properly check if ModuleEngines is active
            }
            if ((p is FuelTank) && (((FuelTank)p).fuel > 0)) return true;
            if (!p.IsSepratron())
            {
                for (int i = 0; i < p.Resources.Count; i++)
                {
                    PartResource r = p.Resources[i];
                    if (r.amount > 0 && r.info.name != "ElectricCharge" && tankResources.Contains(r.info.id))
                    {
                        return true;
                    }
                }
            }
            for (int i = 0; i < p.children.Count; i++)
            {
                if (HasActiveOrIdleEngineOrTankDescendant(p.children[i], tankResources))
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
                if (p.inverseStage == inverseStage && p.IsUnfiredDecoupler())
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
                if (p.inverseStage == inverseStage && p.IsUnfiredDecoupler() && HasDeactivatedEngineOrTankDescendant(p))
                {
                    return true;
                }
            }
            return false;
        }

        //detect if a part is above a deactivated engine or fuel tank
        public static bool HasDeactivatedEngineOrTankDescendant(Part p)
        {
            if ((p.State == PartStates.DEACTIVATED) && (p is FuelTank || p.IsEngine()) && !p.IsSepratron())
            {
                return true; // TODO: yet more ModuleEngine lazy checks
            }

            //check if this is a new-style fuel tank that's run out of resources:
            bool hadResources = false;
            bool hasResources = false;
            for (int i = 0; i < p.Resources.Count; i++)
            {
                PartResource r = p.Resources[i];
                if (r.name == "ElectricCharge") continue;
                if (r.maxAmount > 0) hadResources = true;
                if (r.amount > 0) hasResources = true;
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
    }
}
