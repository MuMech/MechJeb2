using System;
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


        [GeneralInfoItem("Auto-deploy solar panels", InfoItem.Category.Misc, showInEditor = false)]
        public void AutoDeploySolarPanelsInfoItem()
        {
            autodeploySolarPanels = GUILayout.Toggle(autodeploySolarPanels, "Auto-deploy solar panels");
        }

        private bool isDeployable(ModuleDeployableSolarPanel sa)
        {
            return sa.Events["Extend"].active || sa.Events["Retract"].active;
        }

        [GeneralInfoItem("Toggle all solar panels", InfoItem.Category.Misc, showInEditor = false)]
        public void ToggleAllSolarPanelsInfoItem()
        {
            if (AllRetracted())
            {
                if (GUILayout.Button("Extend all solar panels"))
                {
                    ExtendAll();
                }
            }
            else
            {
                if (GUILayout.Button("Retract all solar panels"))
                {
                    RetractAll();
                }
            }
        }

        public void ExtendAll()
        {
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                if (p.ShieldedFromAirstream)
                    continue;

                var solar = p.Modules.GetModules<ModuleDeployableSolarPanel>();
                for (int j = 0; j < solar.Count; j++)
                {
                    if (isDeployable(solar[j]))
                    {
                        solar[j].Extend();
                    }
                }
            }
        }

        public void RetractAll()
        {
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                if (p.ShieldedFromAirstream)
                    continue;
                var solar = p.Modules.GetModules<ModuleDeployableSolarPanel>();
                for (int j = 0; j < solar.Count; j++)
                    {
                    if (isDeployable(solar[j]))
                    {
                        solar[j].Retract();
                    }
                }
            }
        }

        public bool AllRetracted()
        {
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                if (p.ShieldedFromAirstream)
                    continue;
                var solar = p.Modules.GetModules<ModuleDeployableSolarPanel>();
                for (int j = 0; j < solar.Count; j++)
                {
                    ModuleDeployableSolarPanel sa = solar[j];
                    if (isDeployable(sa) && 
                        ((sa.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED) ||
                         (sa.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDING) ||
                         (sa.panelState == ModuleDeployableSolarPanel.panelStates.RETRACTING)))
                    {
                        return false;
                    }
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
