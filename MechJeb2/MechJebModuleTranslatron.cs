extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using KSP.Localization;
using KSP.UI.Screens;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    // The Translatron logic and its abort sequence: they run all flight, with the window (MechJebModuleTranslatronWindow) open or closed
    public class MechJebModuleTranslatron : ComputerModule
    {
        //protected static string[] trans_texts = { "OFF", "KEEP\nOBT", "KEEP\nSURF", "KEEP\nVERT" };

        public enum AbortStage
        {
            OFF,
            THRUSTOFF,
            DECOUPLE,
            BURNUP,
            LAND,
            LANDING
        }

        protected AbortStage abort = AbortStage.OFF;

        // for the new UI: true while the panic switch is engaged
        internal bool Aborting => abort != AbortStage.OFF;
        protected double burnUpTime;

        [Persistent(pass = (int)Pass.LOCAL)]
        public EditableDouble trans_spd = new EditableDouble(0);


        public MechJebModuleTranslatron(MechJebCore core) : base(core) => Enabled = true;

        public void SetMode(MechJebModuleThrustController.TMode newMode)
        {
            MechJebModuleThrustController.TMode oldMode = Core.Thrust.Tmode;
            Core.Thrust.Tmode = newMode;
            if (Core.Thrust.Tmode != oldMode)
            {
                Core.Thrust.TransSpdAct = Convert.ToInt16(trans_spd);
                if (Core.Thrust.Tmode == MechJebModuleThrustController.TMode.OFF)
                {
                    Core.Thrust.Users.Remove(this);
                }
                else
                {
                    Core.Thrust.Users.Add(this);
                }
            }
        }

        public void PanicSwitch()
        {
            if (abort != AbortStage.OFF)
            {
                if (abort == AbortStage.LAND || abort == AbortStage.LANDING)
                {
                    Core.GetComputerModule<MechJebModuleLandingAutopilot>().StopLanding();
                }
                else
                {
                    Core.Thrust.ThrustOff();
                    Core.Thrust.Users.Remove(this);
                    Core.Attitude.AttitudeDeactivate();
                }

                Core.Thrust.PanicCut = false;
                abort = AbortStage.OFF;
            }
            else
            {
                StopOtherAutopilots();
                Core.Thrust.ThrustOff();
                Core.Thrust.PanicCut = true;
                abort = AbortStage.THRUSTOFF;
                Core.Thrust.Users.Add(this);
            }
        }

        // PANIC takes the vessel from every autopilot that flies it: they stop, and give back the throttle and the attitude, before
        // the abort takes them. The rendezvous autopilot stops before the node executor it runs, and the landing autopilot that the
        // abort engages later starts afresh.
        private void StopOtherAutopilots()
        {
            Stop(Core.GetComputerModule<MechJebModuleRendezvousAutopilot>());
            Stop(Core.GetComputerModule<MechJebModuleDockingAutopilot>());
            Stop(Core.GetComputerModule<MechJebModuleAscentClassicAutopilot>());
            Stop(Core.GetComputerModule<MechJebModuleAscentPSGAutopilot>());
            Stop(Core.GetComputerModule<MechJebModuleHoverslamAutopilot>());
            Stop(Core.Spinup);

            if (Core.Landing != null && Core.Landing.Enabled)
                Core.Landing.StopLanding();

            if (Core.Node != null && Core.Node.Enabled)
                Core.Node.Abort();

            if (Core.SmartASS != null && Core.SmartASS.target != MechJebModuleSmartASS.Target.OFF)
            {
                Core.SmartASS.target = MechJebModuleSmartASS.Target.OFF;
                Core.SmartASS.Engage();
            }

            // the Translatron's own Keep mode ends with ThrustOff, after this
            Core.Attitude.AttitudeDeactivate();
        }

        private static void Stop(ComputerModule module)
        {
            if (module != null && module.Enabled)
                module.Users.Clear();
        }

        public void recursiveDecouple()
        {
            int minStage = StageManager.LastStage;
            for (int i = 0; i < Part.vessel.parts.Count; i++)
            {
                Part child = Part.vessel.parts[i];
                // TODO Sarbian : Cleanup - not sure if any mod still use those and they are not supported in other part of the code
                if (child.HasModule<ModuleEngines>())
                {
                    if (child.inverseStage < minStage)
                    {
                        minStage = child.inverseStage;
                    }
                }
            }

            var decouplers = new List<Part>();
            for (int i = 0; i < Part.vessel.parts.Count; i++)
            {
                Part child = Part.vessel.parts[i];
                if (child.inverseStage > minStage &&
                    (child.HasModule<ModuleDecouple>() || child.HasModule<ModuleAnchoredDecoupler>()))
                {
                    decouplers.Add(child);
                }
            }

            for (int i = 0; i < decouplers.Count; i++)
            {
                decouplers[i].force_activate();
            }

            if (Part.vessel == FlightGlobals.ActiveVessel)
            {
                StageManager.ActivateStage(minStage);
            }
        }

        public override void Drive(FlightCtrlState s)
        {
            // Fix the Translatron behavior which kill HS.
            // TODO : proper fix that register the attitude controler outside of Drive
            if (!Core.Attitude.Users.Contains(this) && Core.Thrust.TransKillH && Core.Thrust.Tmode != MechJebModuleThrustController.TMode.OFF)
            {
                Core.Attitude.Users.Add(this);
            }

            if (Core.Attitude.Users.Contains(this) && (!Core.Thrust.TransKillH || Core.Thrust.Tmode == MechJebModuleThrustController.TMode.OFF))
            {
                Core.Attitude.Users.Remove(this);
            }

            if (abort != AbortStage.OFF)
            {
                switch (abort)
                {
                    case AbortStage.THRUSTOFF:
                        FlightInputHandler.SetNeutralControls();
                        s.mainThrottle = 0;
                        abort = AbortStage.DECOUPLE;
                        break;
                    case AbortStage.DECOUPLE:
                        // the burn-up follows even if the decoupling throws: it ends the throttle cut
                        try
                        {
                            recursiveDecouple();
                        }
                        finally
                        {
                            abort = AbortStage.BURNUP;
                            burnUpTime = Planetarium.GetUniversalTime();
                        }

                        break;
                    case AbortStage.BURNUP:
                        Core.Thrust.PanicCut = false;
                        if (Planetarium.GetUniversalTime() - burnUpTime < 2 || VesselState.SpeedVertical < 10)
                        {
                            Core.Thrust.Tmode = MechJebModuleThrustController.TMode.DIRECT;
                            Core.Attitude.AttitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, this);
                            double int_error = Math.Abs(Vector3d.Angle(VesselState.Up, VesselState.Forward));
                            Core.Thrust.TransSpdAct = int_error < 90 ? 100 : 0;
                        }
                        else
                        {
                            abort = AbortStage.LAND;
                        }

                        break;
                    case AbortStage.LAND:
                        Core.Thrust.Users.Remove(this);
                        Core.GetComputerModule<MechJebModuleLandingAutopilot>().LandUntargeted(this);
                        abort = AbortStage.LANDING;
                        break;
                    case AbortStage.LANDING:
                        if (Vessel.LandedOrSplashed)
                        {
                            abort = AbortStage.OFF;
                        }

                        break;
                }
            }

            base.Drive(s);
        }
    }
}
