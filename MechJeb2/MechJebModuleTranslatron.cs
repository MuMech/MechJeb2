extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using KSP.Localization;
using KSP.UI.Screens;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleTranslatron : DisplayModule
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


        public MechJebModuleTranslatron(MechJebCore core) : base(core) { }

        public override string GetName() => Localizer.Format("#MechJeb_Translatron_title");

        public override string IconName() => "Translatron";

        public void SetMode(MechJebModuleThrustController.TMode newMode)
        {
            MechJebModuleThrustController.TMode oldMode = Core.Thrust.Tmode;
            Core.Thrust.Tmode = newMode;
            if (Core.Thrust.Tmode != oldMode)
            {
                Core.Thrust.TransSpdAct = Convert.ToInt16(trans_spd);
                WindowPos = new Rect(WindowPos.x, WindowPos.y, 10, 10);
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

                abort = AbortStage.OFF;
            }
            else
            {
                abort = AbortStage.THRUSTOFF;
                Core.Thrust.Users.Add(this);
            }
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
                        recursiveDecouple();
                        abort = AbortStage.BURNUP;
                        burnUpTime = Planetarium.GetUniversalTime();
                        break;
                    case AbortStage.BURNUP:
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
