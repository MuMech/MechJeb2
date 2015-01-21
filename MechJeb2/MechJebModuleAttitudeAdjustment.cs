using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAttitudeAdjustment : DisplayModule
    {
        public EditableDouble Tf;
        public EditableDouble TfMin;
        public EditableDouble TfMax;
        public EditableDouble kpFactor;
        public EditableDouble kiFactor;
        public EditableDouble kdFactor;

        [Persistent(pass = (int)Pass.Global)]
        public bool showInfos = false;

        public MechJebModuleAttitudeAdjustment(MechJebCore core) : base(core) { }

        public override void OnStart(PartModule.StartState state)
        {
            Tf = new EditableDouble(core.attitude.Tf);
            TfMin = new EditableDouble(core.attitude.TfMin);
            TfMax = new EditableDouble(core.attitude.TfMax);
            kpFactor = new EditableDouble(core.attitude.kpFactor);
            kiFactor = new EditableDouble(core.attitude.kiFactor);
            kdFactor = new EditableDouble(core.attitude.kdFactor);
            base.OnStart(state);
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            core.GetComputerModule<MechJebModuleCustomWindowEditor>().registry.Find(i => i.id == "Toggle:AttitudeController.useSAS").DrawItem();

            if (!core.attitude.useSAS)
            {
                core.attitude.Tf_autoTune = GUILayout.Toggle(core.attitude.Tf_autoTune, " Tf auto-tuning");

                if (!core.attitude.Tf_autoTune)
                {
                    GUILayout.Label("Larger ship do better with a larger Tf");
                    GuiUtils.SimpleTextBox("Tf (s)", Tf);
                    Tf = Math.Max(0.01, Tf);
                }
                else
                {
//            pid.Kd = kpFactor / Tf;
//            pid.Kp = pid.Kd / (kiFactor * Math.Sqrt(2) * Tf);
//            pid.Ki = pid.Kp / (kpFactor * Math.Sqrt(2) * Tf);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Tf", GUILayout.ExpandWidth(true));
                    GUILayout.Label(core.attitude.Tf.ToString("F3"), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Tf range");
                    GuiUtils.SimpleTextBox("min", TfMin, "", 50);
                    TfMin = Math.Max(TfMin, 0.01);
                    GuiUtils.SimpleTextBox("max", TfMax, "", 50);
                    TfMax = Math.Max(TfMax, 0.01);
                    GUILayout.EndHorizontal();

                    GUILayout.Label("PID factors");
                    GuiUtils.SimpleTextBox("Kd = ", kdFactor, " / Tf", 50);
                    kdFactor = Math.Max(kdFactor, 0.01);
                    GuiUtils.SimpleTextBox("Kp = pid.Kd / (", kpFactor, " * Math.Sqrt(2) * Tf)", 50);
                    kpFactor = Math.Max(kpFactor, 0.01);
                    GuiUtils.SimpleTextBox("Ki = pid.Kp / (", kiFactor, " * Math.Sqrt(2) * Tf)", 50);
                    kiFactor = Math.Max(kiFactor, 0.01);
                }

                core.attitude.RCS_auto = GUILayout.Toggle(core.attitude.RCS_auto, " RCS auto mode");
                core.rcs.rcsThrottle = GUILayout.Toggle(core.rcs.rcsThrottle, " RCS throttle when 0k thrust");

                showInfos = GUILayout.Toggle(showInfos, "Show Numbers");
                if (showInfos)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Kp, Ki, Kd", GUILayout.ExpandWidth(true));
                    GUILayout.Label(
                        core.attitude.pid.Kp.ToString("F3") + ", " +
                        core.attitude.pid.Ki.ToString("F3") + ", " +
                        core.attitude.pid.Kd.ToString("F3"),
                        GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("prop. action.", GUILayout.ExpandWidth(true));
                    GUILayout.Label(MuUtils.PrettyPrint(core.attitude.pid.propAct), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("deriv. action", GUILayout.ExpandWidth(true));
                    GUILayout.Label(MuUtils.PrettyPrint(core.attitude.pid.derivativeAct), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("integral action.", GUILayout.ExpandWidth(true));
                    GUILayout.Label(MuUtils.PrettyPrint(core.attitude.pid.intAccum), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("PID Action", GUILayout.ExpandWidth(true));
                    GUILayout.Label(MuUtils.PrettyPrint(core.attitude.pidAction), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("AttitudeRollMatters ", GUILayout.ExpandWidth(true));
                    GUILayout.Label(core.attitude.attitudeRollMatters ? "true" : "false", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    Vector3d torque = vesselState.torqueAvailable + vesselState.torqueFromEngine * vessel.ctrlState.mainThrottle;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("torque", GUILayout.ExpandWidth(true));
                    GUILayout.Label(MuUtils.PrettyPrint(torque), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("|torque|", GUILayout.ExpandWidth(true));
                    GUILayout.Label(torque.magnitude.ToString("F3"), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    Vector3d inertia = Vector3d.Scale(
                        vesselState.angularMomentum.Sign(),
                        Vector3d.Scale(
                            Vector3d.Scale(vesselState.angularMomentum, vesselState.angularMomentum),
                            Vector3d.Scale(torque, vesselState.MoI).Invert()
                            )
                        );
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("inertia", GUILayout.ExpandWidth(true));
                    GUILayout.Label(MuUtils.PrettyPrint(inertia), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("|inertia|", GUILayout.ExpandWidth(true));
                    GUILayout.Label(inertia.magnitude.ToString("F3"), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    Vector3d ratio = Vector3d.Scale(vesselState.MoI, torque.Invert());

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("|MOI| / |Torque|", GUILayout.ExpandWidth(true));
                    GUILayout.Label(ratio.magnitude.ToString("F3"), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("fixedDeltaTime", GUILayout.ExpandWidth(true));
                    GUILayout.Label(TimeWarp.fixedDeltaTime.ToString("F3"), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();
                }
            }

            MechJebModuleAttitudeController.useCoMVelocity = GUILayout.Toggle(MechJebModuleAttitudeController.useCoMVelocity, "Use CoM velocity instead of stock");

            MechJebModuleDebugArrows arrows = core.GetComputerModule<MechJebModuleDebugArrows>();

            GuiUtils.SimpleTextBox("Arrows length", arrows.arrowsLength, "", 50);

            arrows.displayAtCoM = GUILayout.Toggle(arrows.displayAtCoM, "Display the arrow at the CoM");
            arrows.podSrfVelocityArrowActive = GUILayout.Toggle(arrows.podSrfVelocityArrowActive, "Pod Surface Velocity (yellow)");
            arrows.comSrfVelocityArrowActive = GUILayout.Toggle(arrows.comSrfVelocityArrowActive, "CoM Surface Velocity (green)");
            arrows.podObtVelocityArrowActive = GUILayout.Toggle(arrows.podObtVelocityArrowActive, "Pod Orbital Velocity (red)");
            arrows.comObtVelocityArrowActive = GUILayout.Toggle(arrows.comObtVelocityArrowActive, "CoM Orbital Velocity (orange)");
            arrows.forwardArrowActive = GUILayout.Toggle(arrows.forwardArrowActive, "Command Pod Forward (Navy Blue)");
            //arrows.avgForwardArrowActive = GUILayout.Toggle(arrows.avgForwardArrowActive, "Forward Avg (blue)");

            arrows.requestedAttitudeArrowActive = GUILayout.Toggle(arrows.requestedAttitudeArrowActive, "Requested Attitude (Gray)");

            arrows.debugArrowActive = GUILayout.Toggle(arrows.debugArrowActive, "Debug (Magenta)");
            

            GUILayout.EndVertical();

            if (!core.attitude.Tf_autoTune)
            {
            	if (core.attitude.Tf != Tf)
            	{
            		core.attitude.Tf = Tf;
            		core.attitude.setPIDParameters();
            	}
            }
            else
            {
            	if (core.attitude.TfMin != TfMin || core.attitude.TfMax != TfMax)
            	{
            		core.attitude.TfMin = TfMin;
            		core.attitude.TfMax = TfMax;
            		core.attitude.setPIDParameters();
            	}
            	if (core.attitude.kpFactor != kpFactor || core.attitude.kiFactor != kiFactor || core.attitude.kdFactor != kdFactor)
            	{
            		core.attitude.kpFactor = kpFactor;
            		core.attitude.kiFactor = kiFactor;
            		core.attitude.kdFactor = kdFactor;
            		core.attitude.setPIDParameters();
            	}
            }
            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return "Attitude Adjustment";
        }
    }
}
