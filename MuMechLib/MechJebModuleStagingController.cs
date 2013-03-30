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
        public EditableDouble autoStageDelay = 1.0;
        [Persistent(pass = (int)Pass.Type)]
        public EditableInt autoStageLimit = 0;

        [GeneralInfoItem("Autostaging", InfoItem.Category.Misc)]
        public void AutostageInfoItem()
        {
            GUILayout.BeginVertical();
            enabled = GUILayout.Toggle(enabled, "Auto-stage");
            GuiUtils.SimpleTextBox("Staging delay:", autoStageDelay, "s");
            GuiUtils.SimpleTextBox("Stop at stage #", autoStageLimit, "");
            GUILayout.EndVertical();
        }

        //internal state:
        double lastStageTime = 0;


        public override void OnFixedUpdate()
        {
            if (!vessel.isActiveVessel || !this.enabled) return;

            //if autostage enabled, and if we are not waiting on the pad, and if there are stages left,
            //and if we are allowed to continue staging, and if we didn't just fire the previous stage
            if (vessel.LiftedOff() && Staging.CurrentStage > 0 && Staging.CurrentStage > autoStageLimit
                && vesselState.time - lastStageTime > autoStageDelay)
            {
                //don't decouple active or idle engines or tanks
                List<int> burnedResources = FindBurnedResources();
                if (!InverseStageDecouplesActiveOrIdleEngineOrTank(Staging.CurrentStage - 1, vessel, burnedResources))
                {
                    //only fire decouplers to drop deactivated engines or tanks
                    bool firesDecoupler = InverseStageFiresDecoupler(Staging.CurrentStage - 1, vessel);
                    if (!firesDecoupler
                        || InverseStageDecouplesDeactivatedEngineOrTank(Staging.CurrentStage - 1, vessel))
                    {
                        if (firesDecoupler)
                        {
                            //if we decouple things, delay the next stage a bit to avoid exploding the debris
                            lastStageTime = vesselState.time;
                        }

                        Staging.ActivateNextStage();
                    }
                }
            }
        }

        //determine whether it's safe to activate inverseStage
        public static bool InverseStageDecouplesActiveOrIdleEngineOrTank(int inverseStage, Vessel v, List<int> tankResources)
        {
            foreach (Part p in v.parts)
            {
                if (p.inverseStage == inverseStage && p.IsDecoupler() && HasActiveOrIdleEngineOrTankDescendant(p, tankResources))
                {
                    return true;
                }
            }
            return false;
        }

        public List<int> FindBurnedResources()
        {
            var activeEngines = vessel.parts.Where(p => p.inverseStage >= Staging.CurrentStage && p.IsEngine() && !p.IsSepratron());
            var engineModules = activeEngines.Select(p => p.Modules.OfType<ModuleEngines>().First());
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
                foreach (PartResource r in p.Resources)
                {
                    if (r.amount > 0 && r.info.name != "ElectricCharge" && tankResources.Contains(r.info.id))
                    {
                        return true;
                    }
                }
            }
            foreach (Part child in p.children)
            {
                if (HasActiveOrIdleEngineOrTankDescendant(child, tankResources)) return true;
            }
            return false;
        }

        //determine whether activating inverseStage will fire any sort of decoupler. This
        //is used to tell whether we should delay activating the next stage after activating inverseStage
        public static bool InverseStageFiresDecoupler(int inverseStage, Vessel v)
        {
            foreach (Part p in v.parts)
            {
                if (p.inverseStage == inverseStage && p.IsDecoupler()) return true;
            }
            return false;
        }

        //determine whether inverseStage sheds a dead engine
        public static bool InverseStageDecouplesDeactivatedEngineOrTank(int inverseStage, Vessel v)
        {
            foreach (Part p in v.parts)
            {
                if (p.inverseStage == inverseStage && p.IsDecoupler() && HasDeactivatedEngineOrTankDescendant(p)) return true;
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
            foreach (PartResource r in p.Resources)
            {
                if (r.name == "ElectricCharge") continue;
                if (r.maxAmount > 0) hadResources = true;
                if (r.amount > 0) hasResources = true;
            }
            if (hadResources && !hasResources) return true;

            if (p.IsEngine() && !p.EngineHasFuel()) return true;

            foreach (Part child in p.children)
            {
                if (HasDeactivatedEngineOrTankDescendant(child)) return true;
            }
            return false;
        }
    }
}
