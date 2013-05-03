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

            GuiUtils.SimpleTextBox("Tf (s)", Tf);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Use SAS", GUILayout.ExpandWidth(true));
            GUILayout.Label(core.attitude.useSAS ? "True" : "False", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Use RCS", GUILayout.ExpandWidth(true));
            GUILayout.Label(core.attitude.useRCS ? "True" : "False", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Kp, Ki, Kd", GUILayout.ExpandWidth(true));
            GUILayout.Label(core.attitude.pid.Kp.ToString("F3") + ", " + 
                            core.attitude.pid.Ki.ToString("F3") + ", " +
                            core.attitude.pid.Kd.ToString("F3") , GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Attitud Error", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(core.attitude.pid.prevError), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("PID Integral Act.", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(core.attitude.pid.intAccum), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("PID Action", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(core.attitude.pidAction), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Drive Action", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(core.attitude.lastAct), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            Vector3d torque = new Vector3d(
                                                    vesselState.torquePYAvailable + vesselState.torqueThrustPYAvailable * vessel.ctrlState.mainThrottle,
                                                    vesselState.torqueRAvailable,
                                                    vesselState.torquePYAvailable + vesselState.torqueThrustPYAvailable * vessel.ctrlState.mainThrottle
                                            );

            GUILayout.BeginHorizontal();
            GUILayout.Label("torque", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(torque.Reorder(132)), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MoI", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(vesselState.MoI.Reorder(132)), GUILayout.ExpandWidth(false));
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


            GUILayout.EndVertical();

            if ( (core.attitude.Tf != Tf) )
            {
                core.attitude.Tf = Tf;
                double Kd = 0.6 / Tf;
                double Kp = 1 / (8 * Math.Sqrt(2) * Tf * Tf);
                double Ki = Kp / (4 * Math.Sqrt(2) * Tf);
                core.attitude.pid = new PIDControllerV(Kp, Ki, Kd, 1, -1);
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
