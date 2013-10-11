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

        public MechJebModuleAttitudeAdjustment(MechJebCore core) : base(core) { }

        public override void OnStart(PartModule.StartState state)
        {
            Tf = new EditableDouble(core.attitude.Tf);
            base.OnStart(state);
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Larger ship do better with a larger Tf");
            GuiUtils.SimpleTextBox("Tf (s)", Tf);
            Tf = Math.Max(0.01, Tf);

            core.attitude.RCS_auto = GUILayout.Toggle(core.attitude.RCS_auto, " RCS auto mode");         

            GUILayout.BeginHorizontal();
            GUILayout.Label("Kp, Ki, Kd", GUILayout.ExpandWidth(true));
            GUILayout.Label(core.attitude.pid.Kp.ToString("F3") + ", " + 
                            core.attitude.pid.Ki.ToString("F3") + ", " +
                            core.attitude.pid.Kd.ToString("F3") , GUILayout.ExpandWidth(false));
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
            GUILayout.Label(core.attitude.attitudeRollMatters?"true":"false", GUILayout.ExpandWidth(false));
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

            if ( (core.attitude.Tf != Tf) )
            {
                core.attitude.Tf = Tf;
                double Kd = 0.53 / Tf;
                double Kp = Kd / (3 * Math.Sqrt(2) * Tf);
                double Ki = Kp / (12 * Math.Sqrt(2) * Tf);
                core.attitude.pid = new PIDControllerV2(Kp, Ki, Kd, 1, -1);
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
