using System;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAttitudeAdjustment : DisplayModule
    {

        [Persistent(pass = (int)Pass.Global)]
        public bool showInfos = false;

        public MechJebModuleAttitudeAdjustment(MechJebCore core) : base(core) { }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            //core.GetComputerModule<MechJebModuleCustomWindowEditor>().registry.Find(i => i.id == "Toggle:AttitudeController.useSAS").DrawItem();

            core.attitude.RCS_auto = GUILayout.Toggle(core.attitude.RCS_auto, " RCS auto mode");


            int currentController = core.attitude.activeController;
            if (GUILayout.Toggle(currentController == 0, "MJAttitudeController"))
            {
                currentController = 0;
            }
            if (GUILayout.Toggle(currentController == 1, "KosAttitudeController"))
            {
                currentController = 1;
            }
            if (GUILayout.Toggle(currentController == 2, "HybridController"))
            {
                currentController = 2;
            }


            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.BeginVertical();
            core.attitude.Controller.GUI();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();


            if (currentController != core.attitude.activeController)
            {
                core.attitude.SetActiveController(currentController);
            }

            showInfos = GUILayout.Toggle(showInfos, "Show Numbers");
            if (showInfos)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Axis Control ", GUILayout.ExpandWidth(true));
                GUILayout.Label(MuUtils.PrettyPrint(core.attitude.AxisState, "F0"), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Torque", GUILayout.ExpandWidth(true));
                GUILayout.Label("|" + core.attitude.torque.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(core.attitude.torque),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("torqueReactionSpeed", GUILayout.ExpandWidth(true));
                GUILayout.Label(
                    "|" + vesselState.torqueReactionSpeed.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(vesselState.torqueReactionSpeed),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                Vector3d ratio = Vector3d.Scale(vesselState.MoI, core.attitude.torque.InvertNoNaN());

                GUILayout.BeginHorizontal();
                GUILayout.Label("MOI / torque", GUILayout.ExpandWidth(true));
                GUILayout.Label("|" + ratio.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(ratio), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("MOI", GUILayout.ExpandWidth(true));
                GUILayout.Label("|" + vesselState.MoI.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(vesselState.MoI), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Angular Velocity", GUILayout.ExpandWidth(true));
                GUILayout.Label("|" + vessel.angularVelocity.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(vessel.angularVelocity),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Angular M", GUILayout.ExpandWidth(true));
                GUILayout.Label(
                    "|" + vesselState.angularMomentum.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(vesselState.angularMomentum),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("fixedDeltaTime", GUILayout.ExpandWidth(true));
                GUILayout.Label(TimeWarp.fixedDeltaTime.ToString("F3"), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }


            MechJebModuleDebugArrows arrows = core.GetComputerModule<MechJebModuleDebugArrows>();

            GuiUtils.SimpleTextBox("Arrows length", arrows.arrowsLength, "", 50);

            arrows.seeThrough = GUILayout.Toggle(arrows.seeThrough, "Visible through object");

            arrows.comSphereActive = GUILayout.Toggle(arrows.comSphereActive, "Display the CoM", GUILayout.ExpandWidth(false));
            arrows.colSphereActive = GUILayout.Toggle(arrows.colSphereActive, "Display the CoL", GUILayout.ExpandWidth(false));
            arrows.cotSphereActive = GUILayout.Toggle(arrows.cotSphereActive, "Display the CoT", GUILayout.ExpandWidth(false));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Radius of Sphere", GUILayout.ExpandWidth(true));
            arrows.comSphereRadius.text = GUILayout.TextField(arrows.comSphereRadius.text, GUILayout.Width(40));
            GUILayout.EndHorizontal();
            arrows.displayAtCoM = GUILayout.Toggle(arrows.displayAtCoM, "Arrows origins at the CoM");
            arrows.srfVelocityArrowActive = GUILayout.Toggle(arrows.srfVelocityArrowActive, "Pod Surface Velocity (green)");
            arrows.obtVelocityArrowActive = GUILayout.Toggle(arrows.obtVelocityArrowActive, "Pod Orbital Velocity (red)");
            arrows.dotArrowActive = GUILayout.Toggle(arrows.dotArrowActive, "Direction of Thrust (purple pink)");
            arrows.forwardArrowActive = GUILayout.Toggle(arrows.forwardArrowActive, "Command Pod Forward (electric blue)");
            //arrows.avgForwardArrowActive = GUILayout.Toggle(arrows.avgForwardArrowActive, "Forward Avg (blue)");

            arrows.requestedAttitudeArrowActive = GUILayout.Toggle(arrows.requestedAttitudeArrowActive, "Requested Attitude (gray)");

            arrows.debugArrowActive = GUILayout.Toggle(arrows.debugArrowActive, "Debug (magenta)");

            arrows.debugArrow2Active = GUILayout.Toggle(arrows.debugArrow2Active, "Debug2 (light blue)");


            GUILayout.EndVertical();


            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(350), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return "Attitude Adjustment";
        }
    }
}
