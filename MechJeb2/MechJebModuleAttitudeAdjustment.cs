using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAttitudeAdjustment : DisplayModule
    {
        public EditableDouble Kp, Ki, Kd, Tf, factor, Ki_limit;

        public MechJebModuleAttitudeAdjustment(MechJebCore core) : base(core) { }

        public override void OnStart(PartModule.StartState state)
        {
            Kp = new EditableDouble(core.attitude.Kp);
            Ki = new EditableDouble(core.attitude.Ki);
            Kd = new EditableDouble(core.attitude.Kd);
            Tf = new EditableDouble(core.attitude.Tf);
            Ki_limit = new EditableDouble(core.attitude.Ki_limit);
            factor = new EditableDouble(core.attitude.drive_factor);

            base.OnStart(state);
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GuiUtils.SimpleTextBox("Kp", Kp);
            GuiUtils.SimpleTextBox("Ki", Ki);
            GuiUtils.SimpleTextBox("Kd", Kd);
            GuiUtils.SimpleTextBox("Tf", Tf);
            GuiUtils.SimpleTextBox("Ki_limit", Ki_limit);
            GuiUtils.SimpleTextBox("Factor", factor);

            GUILayout.BeginHorizontal();
            GUILayout.Label("prevError", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(core.attitude.pid.prevError), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("|prevError|", GUILayout.ExpandWidth(true));
            GUILayout.Label(core.attitude.pid.prevError.magnitude.ToString("F3"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("intAccum", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(core.attitude.pid.intAccum), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("|intAccum|", GUILayout.ExpandWidth(true));
            GUILayout.Label(core.attitude.pid.intAccum.magnitude.ToString("F3"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            double precision = Math.Max(0.5, Math.Min(10.0, (Math.Min(vesselState.torqueAvailable.x, vesselState.torqueAvailable.z) + vesselState.torqueThrustPYAvailable * vessel.ctrlState.mainThrottle) * 20.0 / vesselState.MoI.magnitude));
            GUILayout.BeginHorizontal();
            GUILayout.Label("precision", GUILayout.ExpandWidth(true));
            GUILayout.Label(precision.ToString("F3"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            Vector3d torque = new Vector3d(
                                                    vesselState.torqueAvailable.x + vesselState.torqueThrustPYAvailable * vessel.ctrlState.mainThrottle,
                                                    vesselState.torqueAvailable.y,
                                                    vesselState.torqueAvailable.z + vesselState.torqueThrustPYAvailable * vessel.ctrlState.mainThrottle
                                            );
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

            GUILayout.EndVertical();

            if ((Kp != core.attitude.Kp) || (Ki != core.attitude.Ki) || (Kd != core.attitude.Kd))
            {
                core.attitude.Kp = Kp;
                core.attitude.Ki = Ki;
                core.attitude.Kd = Kd;
                core.attitude.Ki_limit = Ki_limit;
                core.attitude.pid = new PIDControllerV(Kp, Ki, Kd, Ki_limit, -Ki_limit);
            }

            core.attitude.drive_factor = factor;
            core.attitude.Tf = Tf;

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
