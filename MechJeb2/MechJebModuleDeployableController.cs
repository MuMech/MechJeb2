using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public abstract class MechJebModuleDeployableController : ComputerModule
    {
        public MechJebModuleDeployableController(MechJebCore core) : base(core)
        {
            priority = 200;
            enabled = true;
        }


        protected string buttonText;
        protected bool extended;

        [Persistent(pass = (int)Pass.Global)]
        public bool autoDeploy = false;

        [Persistent(pass = (int)(Pass.Local))]
        protected bool prev_shouldDeploy = false;

        public bool prev_autoDeploy = true;

        protected string type = "";

        protected List<ModuleDeployablePart> cachedPartModules = new List<ModuleDeployablePart>(16);
        protected void DiscoverDeployablePartModules()
        {
            cachedPartModules.Clear();
            foreach (Part p in vessel.Parts)
                foreach (PartModule pm in p.Modules)
                    if (pm != null && pm is ModuleDeployablePart mdp && isModules(mdp))
                        cachedPartModules.Add(mdp);
        }

        protected bool isDeployable(ModuleDeployablePart sa)
        {
            return (sa.Events["Extend"].active || sa.Events["Retract"].active);
        }

        public void ExtendAll()
        {
            foreach (ModuleDeployablePart mdp in cachedPartModules)
                if (mdp != null && isDeployable(mdp) && !mdp.part.ShieldedFromAirstream)
                    mdp.Extend();
        }
        
        public void RetractAll()
        {
            foreach (ModuleDeployablePart mdp in cachedPartModules)
                if (mdp != null && isDeployable(mdp) && !mdp.part.ShieldedFromAirstream)
                    mdp.Retract();
        }
        public bool AllRetracted()
        {
            foreach (ModuleDeployablePart mdp in cachedPartModules)
                if (mdp != null && isDeployable(mdp) && mdp.deployState != ModuleDeployablePart.DeployState.RETRACTED)
                    return false;
            return true;
        }

        public bool ShouldDeploy()
        {
            if (!mainBody.atmosphere)
                return true;

            if (!vessel.LiftedOff())
                return false;

            if (vessel.LandedOrSplashed)
                return false; // True adds too many complex case

            double dt = 10;
            double min_alt; // minimum altitude between now and now+dt seconds
            double t = Planetarium.GetUniversalTime();

            double PeT = orbit.NextPeriapsisTime(t) - t;
            if (PeT > 0 && PeT < dt)
                min_alt = orbit.PeA;
            else
                min_alt = Math.Sqrt(Math.Min(orbit.getRelativePositionAtUT(t).sqrMagnitude, orbit.getRelativePositionAtUT(t + dt).sqrMagnitude)) - mainBody.Radius;

            if (min_alt > mainBody.RealMaxAtmosphereAltitude())
                return true;

            return false;
        }

        public override void OnFixedUpdate()
        {
            // Let the ascent guidance handle the solar panels to retract them before launch
            if (autoDeploy &&
                !(core.GetComputerModule<MechJebModuleAscentAutopilot>() != null &&
                    core.GetComputerModule<MechJebModuleAscentAutopilot>().enabled))
            {
                bool tmp = ShouldDeploy();

                if (tmp && (!prev_shouldDeploy || (autoDeploy != prev_autoDeploy)))
                    ExtendAll();
                else if (!tmp && (prev_shouldDeploy || (autoDeploy != prev_autoDeploy)))
                    RetractAll();

                prev_shouldDeploy = tmp;
                prev_autoDeploy = true;
            }
            else
            {
                prev_autoDeploy = false;
            }

            bool extendedThisPass = !AllRetracted();
            if (extended != extendedThisPass)
                buttonText = getButtonText(extendedThisPass ? DeployablePartState.EXTENDED : DeployablePartState.RETRACTED);

            extended = extendedThisPass;
        }

        protected bool ExtendingOrRetracting()
        {
            foreach (ModuleDeployablePart mdp in cachedPartModules)
                if (mdp != null && isDeployable(mdp) 
                    && (mdp.deployState == ModuleDeployablePart.DeployState.EXTENDING || mdp.deployState == ModuleDeployablePart.DeployState.RETRACTING))
                    return true;
            return false;
        }

        protected abstract bool isModules(ModuleDeployablePart p);


        protected enum DeployablePartState
        {
            RETRACTED,
            EXTENDED
        }

        protected abstract string getButtonText(DeployablePartState deployablePartState);
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight)
                DiscoverDeployablePartModules();
        }
        public override void OnVesselWasModified(Vessel v) => DiscoverDeployablePartModules();
    }
}
