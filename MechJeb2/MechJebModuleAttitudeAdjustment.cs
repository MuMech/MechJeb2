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

            core.GetComputerModule<MechJebModuleCustomWindowEditor>().registry.Find(i => i.id == "Toggle:AttitudeController.useSAS").DrawItem();

            if (!core.attitude.useSAS)
            {
                core.attitude.Tf_autoTune = GUILayout.Toggle(core.attitude.Tf_autoTune, " Tf auto tunning");
                
                if (!core.attitude.Tf_autoTune)
                {
                    GUILayout.Label("Larger ship do better with a larger Tf");
                    GuiUtils.SimpleTextBox("Tf (s)", Tf);
                    Tf = Math.Max(0.01, Tf);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Tf", GUILayout.ExpandWidth(true));
                    GUILayout.Label(core.attitude.Tf.ToString("F3"), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();
                }

                core.attitude.RCS_auto = GUILayout.Toggle(core.attitude.RCS_auto, " RCS auto mode");

                GUILayout.BeginHorizontal();
                GUILayout.Label("Kp, Ki, Kd", GUILayout.ExpandWidth(true));
                GUILayout.Label(core.attitude.pid.Kp.ToString("F3") + ", " +
                                core.attitude.pid.Ki.ToString("F3") + ", " +
                                core.attitude.pid.Kd.ToString("F3"), GUILayout.ExpandWidth(false));
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

            GUILayout.EndVertical();

            if (!core.attitude.Tf_autoTune && core.attitude.Tf != Tf)
            {
                core.attitude.Tf = Tf;
                core.attitude.setPIDParameters();                
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
