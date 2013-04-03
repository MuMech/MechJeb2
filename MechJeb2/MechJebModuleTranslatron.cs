﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;

namespace MuMech
{
    class MechJebModuleTranslatron : DisplayModule
    {
        protected static string[] trans_texts = { "OFF", "KEEP\nOBT", "KEEP\nSURF", "KEEP\nVERT" };

        public enum AbortStage
        {
            OFF,
            THRUSTOFF,
            DECOUPLE,
            BURNUP,
            LAND
        }

        protected AbortStage abort = AbortStage.OFF;
        protected double burnUpTime = 0;

        protected bool autoMode = false;

        [Persistent(pass = (int)Pass.Local)]
        public string trans_spd = "0";

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

            if (core.GetComputerModule<MechJebModuleLandingAutopilot>().enabled)
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

                MechJebModuleThrustController.TMode oldMode = core.thrust.tmode;
                core.thrust.tmode = (MechJebModuleThrustController.TMode)GUILayout.SelectionGrid((int)core.thrust.tmode, trans_texts, 2, sty);
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

                core.thrust.trans_kill_h = GUILayout.Toggle(core.thrust.trans_kill_h, "Kill H/S", GUILayout.ExpandWidth(true));
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label("Speed");
                trans_spd = GUILayout.TextField(trans_spd, GUILayout.ExpandWidth(true));
                trans_spd = Regex.Replace(trans_spd, @"[^\d.+-]", "");
                GUILayout.EndHorizontal();

                if (GUILayout.Button("EXECUTE", sty, GUILayout.ExpandWidth(true)))
                {
                    core.thrust.trans_spd_act = Convert.ToSingle(trans_spd);
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
                abort = (abort == AbortStage.OFF) ? AbortStage.THRUSTOFF : AbortStage.OFF;
                if (abort == AbortStage.OFF)
                {
                    core.thrust.users.Remove(this);
                }
                else
                {
                    core.thrust.users.Add(this);
                }
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public void recursiveDecouple()
        {
            int minStage = Staging.lastStage;
            foreach (Part child in part.vessel.parts)
            {
                if ((child is LiquidEngine) || (child is LiquidFuelEngine) || (child is SolidRocket) || (child is AtmosphericEngine) || child.Modules.Contains("ModuleEngines"))
                {
                    if (child.inverseStage < minStage)
                    {
                        minStage = child.inverseStage;
                    }
                }
            }
            List<Part> decouplers = new List<Part>();
            foreach (Part child in part.vessel.parts)
            {
                if ((child.inverseStage > minStage) && ((child is Decoupler) || (child is DecouplerGUI) || (child is RadialDecoupler) || child.Modules.Contains("ModuleDecouple") || child.Modules.Contains("ModuleAnchoredDecoupler")))
                {
                    decouplers.Add(child);
                }
            }
            foreach (Part decoupler in decouplers)
            {
                decoupler.force_activate();
            }
            if (part.vessel == FlightGlobals.ActiveVessel)
            {
                Staging.ActivateStage(minStage);
            }
        }

        public override void Drive(FlightCtrlState s)
        {
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
                        core.GetComputerModule<MechJebModuleLandingAutopilot>().LandUntargeted();
                        abort = AbortStage.OFF;
                        break;
                }
            }
            base.Drive(s);
        }
    }
}
