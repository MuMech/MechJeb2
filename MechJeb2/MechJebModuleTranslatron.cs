using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.UI.Screens;
using UnityEngine;
using static MechJebLib.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleTranslatron : DisplayModule
    {
        protected static string[] trans_texts =
        {
            Localizer.Format("#MechJeb_Translatron_off"), Localizer.Format("#MechJeb_Translatron_KEEP_OBT"),
            Localizer.Format("#MechJeb_Translatron_KEEP_SURF"), Localizer.Format("#MechJeb_Translatron_KEEP_VERT")
        };
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
        protected double     burnUpTime;

        protected bool autoMode;

        [Persistent(pass = (int)Pass.LOCAL)]
        public EditableDouble trans_spd = new EditableDouble(0);

        private static GUIStyle buttonStyle;

        public MechJebModuleTranslatron(MechJebCore core) : base(core) { }

        public override string GetName() => Localizer.Format("#MechJeb_Translatron_title");

        public override string IconName() => "Translatron";

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(130) };

        protected override void WindowGUI(int windowID)
        {
            if (buttonStyle == null)
            {
                buttonStyle                  = new GUIStyle(GUI.skin.button);
                buttonStyle.normal.textColor = buttonStyle.focused.textColor = Color.white;
                buttonStyle.hover.textColor  = buttonStyle.active.textColor  = Color.yellow;
                buttonStyle.onNormal.textColor =
                    buttonStyle.onFocused.textColor = buttonStyle.onHover.textColor = buttonStyle.onActive.textColor = Color.green;
                buttonStyle.padding = new RectOffset(8, 8, 8, 8);
            }

            GUILayout.BeginVertical();

            if (Core.Thrust.Users.Count > 1 && !Core.Thrust.Users.Contains(this))
            {
                if (!autoMode)
                {
                    WindowPos = new Rect(WindowPos.x, WindowPos.y, 10, 10);
                    autoMode  = true;
                }

                buttonStyle.normal.textColor = Color.red;
                buttonStyle.onActive = buttonStyle.onFocused = buttonStyle.onHover =
                    buttonStyle.onNormal = buttonStyle.active = buttonStyle.focused = buttonStyle.hover = buttonStyle.normal;
                GUILayout.Button(Localizer.Format("#MechJeb_Trans_auto"), buttonStyle, GUILayout.ExpandWidth(true));
            }
            else
            {
                if (autoMode)
                {
                    WindowPos = new Rect(WindowPos.x, WindowPos.y, 10, 10);
                    autoMode  = false;
                }

                var newMode = (MechJebModuleThrustController.TMode)GUILayout.SelectionGrid((int)Core.Thrust.Tmode, trans_texts, 2, buttonStyle);
                SetMode(newMode);

                float
                    val = GameSettings.MODIFIER_KEY.GetKey()
                        ? 5
                        : 1; // change by 5 if the mod_key is held down, else by 1 -- would be better if it actually worked...

                Core.Thrust.TransKillH = GUILayout.Toggle(Core.Thrust.TransKillH, Localizer.Format("#MechJeb_Trans_kill_h"),
                    GUILayout.ExpandWidth(true));
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Trans_spd"), trans_spd, "", 37);
                bool change = false;
                if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                {
                    trans_spd -= val;
                    change    =  true;
                }

                if (GUILayout.Button("0", GUILayout.ExpandWidth(false)))
                {
                    trans_spd = 0;
                    change    = true;
                }

                if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                {
                    trans_spd += val;
                    change    =  true;
                }

                GUILayout.EndHorizontal();

                if (GUILayout.Button(Localizer.Format("#MechJeb_Trans_spd_act") + ":", buttonStyle, GUILayout.ExpandWidth(true)) || change)
                {
                    Core.Thrust.TransSpdAct    = (float)trans_spd.Val;
                    GUIUtility.keyboardControl = 0;
                }
            }

            if (Core.Thrust.Tmode != MechJebModuleThrustController.TMode.OFF)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Trans_current_spd") + Core.Thrust.TransSpdAct.ToSI() + "m/s",
                    GUILayout.ExpandWidth(true));
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("Automation", GuiUtils.UpperCenterLabel, GUILayout.ExpandWidth(true));

            buttonStyle.normal.textColor = buttonStyle.focused.textColor = buttonStyle.hover.textColor = buttonStyle.active.textColor =
                buttonStyle.onNormal.textColor = buttonStyle.onFocused.textColor = buttonStyle.onHover.textColor =
                    buttonStyle.onActive.textColor = abort != AbortStage.OFF ? Color.red : Color.green;

            if (GUILayout.Button(abort != AbortStage.OFF ? Localizer.Format("#MechJeb_Trans_NOPANIC") : Localizer.Format("#MechJeb_Trans_PANIC"),
                    buttonStyle, GUILayout.ExpandWidth(true)))
            {
                PanicSwitch();
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public void SetMode(MechJebModuleThrustController.TMode newMode)
        {
            MechJebModuleThrustController.TMode oldMode = Core.Thrust.Tmode;
            Core.Thrust.Tmode = newMode;
            if (Core.Thrust.Tmode != oldMode)
            {
                Core.Thrust.TransSpdAct = Convert.ToInt16(trans_spd);
                WindowPos               = new Rect(WindowPos.x, WindowPos.y, 10, 10);
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
                    Core.Attitude.attitudeDeactivate();
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
                        abort          = AbortStage.DECOUPLE;
                        break;
                    case AbortStage.DECOUPLE:
                        recursiveDecouple();
                        abort      = AbortStage.BURNUP;
                        burnUpTime = Planetarium.GetUniversalTime();
                        break;
                    case AbortStage.BURNUP:
                        if (Planetarium.GetUniversalTime() - burnUpTime < 2 || VesselState.speedVertical < 10)
                        {
                            Core.Thrust.Tmode = MechJebModuleThrustController.TMode.DIRECT;
                            Core.Attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, this);
                            double int_error = Math.Abs(Vector3d.Angle(VesselState.up, VesselState.forward));
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
