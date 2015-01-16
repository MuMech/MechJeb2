using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSolarPanelController : ComputerModule
    {
        public MechJebModuleSolarPanelController(MechJebCore core)
            : base(core)
        {
            priority = 200;
            enabled = true;
        }

        [Persistent(pass = (int)(Pass.Global))]
        public bool autodeploySolarPanels = false;

        [Persistent(pass = (int)(Pass.Local))]
        private bool prev_ShouldOpenSolarPanels = false;

        public bool prev_autodeploySolarPanels = true;


        [GeneralInfoItem("Auto-deploy solar panels", InfoItem.Category.Misc)]
        public void AutoDeploySolarPanelsInfoItem()
        {
            autodeploySolarPanels = GUILayout.Toggle(autodeploySolarPanels, "Auto-deploy solar panels");
        }

        public void ExtendAll()
        {
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleDeployableSolarPanel sa in p.Modules.OfType<ModuleDeployableSolarPanel>())
                {
                    if (sa.isBreakable)
                        sa.Extend();
                }
            }
        }

        public void RetractAll()
        {
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleDeployableSolarPanel sa in p.Modules.OfType<ModuleDeployableSolarPanel>())
                {
                    if (sa.isBreakable)
                        sa.Retract();
                }
            }
        }

        public bool AllRetracted()
        {
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleDeployableSolarPanel sa in p.Modules.OfType<ModuleDeployableSolarPanel>())
                {
                    if (sa.isBreakable &&
                        ((sa.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED) ||
                         (sa.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDING) ||
                         (sa.panelState == ModuleDeployableSolarPanel.panelStates.RETRACTING)))
                        return false;
                }
            }

            return true;
        }

        private bool ShouldOpenSolarPanels()
        {
            if (!mainBody.atmosphere)
                return true;

            if (!vessel.LiftedOff())
                return false;

            if (vessel.LandedOrSplashed)
                return true;

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

        public override void OnUpdate()
        {
            // Let the ascent guidance handle the solar panels to retract them before launch
            if (autodeploySolarPanels &&
                !(core.GetComputerModule<MechJebModuleAscentAutopilot>() != null &&
                    core.GetComputerModule<MechJebModuleAscentAutopilot>().enabled))
            {
                bool tmp = ShouldOpenSolarPanels();

                if (tmp && (!prev_ShouldOpenSolarPanels || (autodeploySolarPanels != prev_autodeploySolarPanels)))
                    ExtendAll();
                else if (!tmp && (prev_ShouldOpenSolarPanels || (autodeploySolarPanels != prev_autodeploySolarPanels)))
                    RetractAll();

                prev_ShouldOpenSolarPanels = tmp;
                prev_autodeploySolarPanels = true;
            }
            else
            {
                prev_autodeploySolarPanels = false;
            }
        }
    }
}
