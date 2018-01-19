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


        List<ModuleDeployablePart> deployableModules;

        protected string buttonText;
        protected bool extended;

        [Persistent(pass = (int)Pass.Global)]
        public bool autoDeploy = false;

        [Persistent(pass = (int)(Pass.Local))]
        protected bool prev_shouldDeploy = false;

        public bool prev_autoDeploy = true;

        protected string type = "";
        
        protected bool isDeployable(ModuleDeployablePart sa)
        {
            return (sa.Events["Extend"].active || sa.Events["Retract"].active);
        }

        public void ExtendAll()
        {
            List<Part> vp = vessel.parts;
            for (int i = 0; i < vp.Count; i++)
            {
                Part p = vp[i];

                if (p.ShieldedFromAirstream)
                    return;

                var deployable = getModules(p);
                for (int j = 0; j < deployable.Count; j++)
                    if (isDeployable(deployable[j]))
                        deployable[j].Extend();
            };
        }
        
        public void RetractAll()
        {
            List<Part> vp = vessel.parts;
            for (int i = 0; i < vp.Count; i++) {
                Part p = vp[i];

                if (p.ShieldedFromAirstream)
                    return;

                var deployable = getModules(p);
                for (int j = 0; j < deployable.Count; j++)
                    if (isDeployable(deployable[j]))
                        deployable[j].Retract();
            }
        }

        public bool AllRetracted()
        {
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                var deployable = getModules(p);
                for (int j = 0; j < deployable.Count; j++)
                {
                    ModuleDeployablePart sa = deployable[j];
                    
                    if (isDeployable(sa) && sa.deployState != ModuleDeployablePart.DeployState.RETRACTED)
                    {
                        return false;
                    }
                }
            }
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

            if (AllRetracted())
                buttonText = getButtonText(DeployablePartState.RETRACTED);
            else
                buttonText = getButtonText(DeployablePartState.EXTENDED);

            extended = !AllRetracted();
        }

        protected bool ExtendingOrRetracting()
        {
            foreach (Part p in vessel.parts)
            {
                List<ModuleDeployablePart> deployableModules = getModules(p);

                foreach (ModuleDeployablePart deployableModule in deployableModules)
                {
                    if (deployableModule.deployState == ModuleDeployablePart.DeployState.EXTENDING ||
                        deployableModule.deployState == ModuleDeployablePart.DeployState.RETRACTING)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected abstract List<ModuleDeployablePart> getModules(Part p);


        protected enum DeployablePartState
        {
            RETRACTED,
            EXTENDED
        }

        protected abstract string getButtonText(DeployablePartState deployablePartState);
    }
}
