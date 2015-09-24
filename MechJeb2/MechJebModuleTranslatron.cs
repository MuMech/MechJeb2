using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;

namespace MuMech
{
    public class MechJebModuleTranslatron : DisplayModule
    {
        protected static string[] trans_texts = { "OFF", "KEEP\nOBT", "KEEP\nSURF", "KEEP\nVERT" };

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
        protected double burnUpTime = 0;

        protected bool autoMode = false;

        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble trans_spd = new EditableDouble(0);

        public MechJebModuleTranslatron(MechJebCore core) : base(core) { }

        public override string GetName()
        {
            return "Translatron";
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(130) };
        }

        protected override void WindowGUI(int windowID)
        {
            GUIStyle sty = new GUIStyle(GUI.skin.button);
            sty.normal.textColor = sty.focused.textColor = Color.white;
            sty.hover.textColor = sty.active.textColor = Color.yellow;
            sty.onNormal.textColor = sty.onFocused.textColor = sty.onHover.textColor = sty.onActive.textColor = Color.green;
            sty.padding = new RectOffset(8, 8, 8, 8);

            GUILayout.BeginVertical();

            if ((core.thrust.users.Count > 1) && !core.thrust.users.Contains(this))
            {
                if (!autoMode)
                {
                    windowPos = new Rect(windowPos.x, windowPos.y, 10, 10);
                    autoMode = true;
                }

                sty.normal.textColor = Color.red;
                sty.onActive = sty.onFocused = sty.onHover = sty.onNormal = sty.active = sty.focused = sty.hover = sty.normal;
                GUILayout.Button("AUTO", sty, GUILayout.ExpandWidth(true));
            }
            else
            {
                if (autoMode)
                {
                    windowPos = new Rect(windowPos.x, windowPos.y, 10, 10);
                    autoMode = false;
                }

                MechJebModuleThrustController.TMode newMode = (MechJebModuleThrustController.TMode)GUILayout.SelectionGrid((int)core.thrust.tmode, trans_texts, 2, sty);
                SetMode(newMode);

                float val = (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); // change by 5 if the mod_key is held down, else by 1 -- would be better if it actually worked...

                core.thrust.trans_kill_h = GUILayout.Toggle(core.thrust.trans_kill_h, "Kill H/S", GUILayout.ExpandWidth(true));
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GuiUtils.SimpleTextBox("Speed", trans_spd, "", 37);
                bool change = false;
                if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                {
                    trans_spd -= val;
                    change = true;
                }
                if (GUILayout.Button("0", GUILayout.ExpandWidth(false)))
                {
                    trans_spd = 0;
                    change = true;
                }
                if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                {
                    trans_spd += val;
                    change = true;
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("EXECUTE", sty, GUILayout.ExpandWidth(true)) || change)
                {
                    core.thrust.trans_spd_act = (float)trans_spd.val;
                    GUIUtility.keyboardControl = 0;
                }
            }

            if (core.thrust.tmode != MechJebModuleThrustController.TMode.OFF)
            {
                GUILayout.Label("Active speed: " + MuMech.MuUtils.ToSI(core.thrust.trans_spd_act) + "m/s", GUILayout.ExpandWidth(true));
            }

            GUILayout.FlexibleSpace();

            GUIStyle tsty = new GUIStyle(GUI.skin.label);
            tsty.alignment = TextAnchor.UpperCenter;
            GUILayout.Label("Automation", tsty, GUILayout.ExpandWidth(true));

            sty.normal.textColor = sty.focused.textColor = sty.hover.textColor = sty.active.textColor = sty.onNormal.textColor = sty.onFocused.textColor = sty.onHover.textColor = sty.onActive.textColor = (abort != AbortStage.OFF) ? Color.red : Color.green;

            if (GUILayout.Button((abort != AbortStage.OFF) ? "DON'T PANIC!" : "PANIC!!!", sty, GUILayout.ExpandWidth(true)))
            {
                PanicSwitch();
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public void SetMode(MechJebModuleThrustController.TMode newMode)
        {
            MechJebModuleThrustController.TMode oldMode = core.thrust.tmode;
            core.thrust.tmode = newMode;
            if (core.thrust.tmode != oldMode)
            {
                core.thrust.trans_spd_act = Convert.ToInt16(trans_spd);
                windowPos = new Rect(windowPos.x, windowPos.y, 10, 10);
                if (core.thrust.tmode == MechJebModuleThrustController.TMode.OFF)
                {
                    core.thrust.users.Remove(this);
                }
                else
                {
                    core.thrust.users.Add(this);
                }
            }
        }

        public void PanicSwitch()
        {
            if (abort != AbortStage.OFF)
            {
                if ((abort == AbortStage.LAND) || (abort == AbortStage.LANDING))
                {
                    core.GetComputerModule<MechJebModuleLandingAutopilot>().StopLanding();
                }
                else
                {
                    core.thrust.ThrustOff();
                    core.thrust.users.Remove(this);
                    core.attitude.attitudeDeactivate();
                }
                abort = AbortStage.OFF;
            }
            else
            {
                abort = AbortStage.THRUSTOFF;
                core.thrust.users.Add(this);
            }
        }

        public void recursiveDecouple()
        {
            int minStage = Staging.lastStage;
            for (int i = 0; i < part.vessel.parts.Count; i++)
            {
                Part child = part.vessel.parts[i];
                // TODO Sarbian : Cleanup - not sure if any mod still use those and they are not supported in other part of the code
                if (child.HasModule<ModuleEngines>())
                {
                    if (child.inverseStage < minStage)
                    {
                        minStage = child.inverseStage;
                    }
                }
            }
            List<Part> decouplers = new List<Part>();
            for (int i = 0; i < part.vessel.parts.Count; i++)
            {
                Part child = part.vessel.parts[i];
                if ((child.inverseStage > minStage) &&
                    (child.HasModule<ModuleDecouple>() || child.HasModule<ModuleAnchoredDecoupler>()))
                {
                    decouplers.Add(child);
                }
            }
            for (int i = 0; i < decouplers.Count; i++)
            {
                decouplers[i].force_activate();
            }
            if (part.vessel == FlightGlobals.ActiveVessel)
            {
                Staging.ActivateStage(minStage);
            }
        }

        public override void Drive(FlightCtrlState s)
        {
            // Fix the Translatron behavuous with kill HS.
            // TODO : proper fix that register the attitude controler oustide of Drive
            if (!core.attitude.users.Contains(this) && ( core.thrust.trans_kill_h && core.thrust.tmode != MechJebModuleThrustController.TMode.OFF)) { core.attitude.users.Add(this); }
            if ( core.attitude.users.Contains(this) && (!core.thrust.trans_kill_h || core.thrust.tmode == MechJebModuleThrustController.TMode.OFF)) { core.attitude.users.Remove(this); }

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
                        if ((Planetarium.GetUniversalTime() - burnUpTime < 2) || (vesselState.speedVertical < 10))
                        {
                            core.thrust.tmode = MechJebModuleThrustController.TMode.DIRECT;
                            core.attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, this);
                            double int_error = Math.Abs(Vector3d.Angle(vesselState.up, vesselState.forward));
                            core.thrust.trans_spd_act = (int_error < 90) ? 100 : 0;
                        }
                        else
                        {
                            abort = AbortStage.LAND;
                        }
                        break;
                    case AbortStage.LAND:
                        core.thrust.users.Remove(this);
                        core.GetComputerModule<MechJebModuleLandingAutopilot>().LandUntargeted(this);
                        abort = AbortStage.LANDING;
                        break;
                    case AbortStage.LANDING:
                        if (vessel.LandedOrSplashed)
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
