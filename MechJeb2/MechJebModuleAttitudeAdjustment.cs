using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleAttitudeAdjustment : DisplayModule
    {
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showInfos;

        public MechJebModuleAttitudeAdjustment(MechJebCore core) : base(core) { }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            //core.GetComputerModule<MechJebModuleCustomWindowEditor>().registry.Find(i => i.id == "Toggle:AttitudeController.useSAS").DrawItem();

            Core.Attitude.RCS_auto = GUILayout.Toggle(Core.Attitude.RCS_auto, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox1")); //RCS auto mode

            int currentController = Core.Attitude.activeController;
            if (GUILayout.Toggle(currentController == 0, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox2"))) //MJAttitudeController
            {
                currentController = 0;
            }

            if (GUILayout.Toggle(currentController == 1, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox3"))) //KosAttitudeController
            {
                currentController = 1;
            }

            if (GUILayout.Toggle(currentController == 2, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox4"))) //HybridController
            {
                currentController = 2;
            }

            if (GUILayout.Toggle(currentController == 3, "BetterController"))
            {
                currentController = 3;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.BeginVertical();
            Core.Attitude.Controller.GUI();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (currentController != Core.Attitude.activeController)
            {
                Core.Attitude.SetActiveController(currentController);
            }

            showInfos = GUILayout.Toggle(showInfos, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox5")); //Show Numbers
            if (showInfos)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeAdjust_Label1"), GUILayout.ExpandWidth(true)); //Axis Control
                GUILayout.Label(MuUtils.PrettyPrint(Core.Attitude.AxisControl, "F0"), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeAdjust_Label2"), GUILayout.ExpandWidth(true)); //Torque
                GUILayout.Label("|" + Core.Attitude.torque.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(Core.Attitude.torque),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeAdjust_Label3"), GUILayout.ExpandWidth(true)); //torqueReactionSpeed
                GUILayout.Label(
                    "|" + VesselState.torqueReactionSpeed.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(VesselState.torqueReactionSpeed),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                var ratio = Vector3d.Scale(VesselState.MoI, Core.Attitude.torque.InvertNoNaN());

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeAdjust_Label4"), GUILayout.ExpandWidth(true)); //MOI / torque
                GUILayout.Label("|" + ratio.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(ratio), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeAdjust_Label5"), GUILayout.ExpandWidth(true)); //MOI
                GUILayout.Label("|" + VesselState.MoI.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(VesselState.MoI),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeAdjust_Label6"), GUILayout.ExpandWidth(true)); //Angular Velocity
                GUILayout.Label("|" + Vessel.angularVelocity.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(Vessel.angularVelocity),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeAdjust_Label7"), GUILayout.ExpandWidth(true)); //"Angular M"
                GUILayout.Label(
                    "|" + VesselState.angularMomentum.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(VesselState.angularMomentum),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeAdjust_Label8"), GUILayout.ExpandWidth(true)); //fixedDeltaTime
                GUILayout.Label(TimeWarp.fixedDeltaTime.ToString("F3"), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }


            MechJebModuleDebugArrows arrows = Core.GetComputerModule<MechJebModuleDebugArrows>();

            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AttitudeAdjust_Label9"), arrows.arrowsLength, "", 50); //Arrows length

            arrows.seeThrough = GUILayout.Toggle(arrows.seeThrough, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox6")); //Visible through object

            arrows.comSphereActive = GUILayout.Toggle(arrows.comSphereActive, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox7"),
                GUILayout.ExpandWidth(false)); //Display the CoM
            arrows.colSphereActive = GUILayout.Toggle(arrows.colSphereActive, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox8"),
                GUILayout.ExpandWidth(false)); //Display the CoL
            arrows.cotSphereActive = GUILayout.Toggle(arrows.cotSphereActive, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox9"),
                GUILayout.ExpandWidth(false)); //Display the CoT
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_AttitudeAdjust_Label10"), GUILayout.ExpandWidth(true)); //Radius of Sphere
            arrows.comSphereRadius.Text = GUILayout.TextField(arrows.comSphereRadius.Text, GUILayout.Width(40));
            GUILayout.EndHorizontal();
            arrows.displayAtCoM =
                GUILayout.Toggle(arrows.displayAtCoM, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox10")); //Arrows origins at the CoM
            arrows.srfVelocityArrowActive =
                GUILayout.Toggle(arrows.srfVelocityArrowActive,
                    Localizer.Format("#MechJeb_AttitudeAdjust_checkbox11")); //Pod Surface Velocity (green)
            arrows.obtVelocityArrowActive =
                GUILayout.Toggle(arrows.obtVelocityArrowActive, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox12")); //Pod Orbital Velocity (red)
            arrows.dotArrowActive =
                GUILayout.Toggle(arrows.dotArrowActive, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox13")); //Direction of Thrust (purple pink)
            arrows.forwardArrowActive =
                GUILayout.Toggle(arrows.forwardArrowActive,
                    Localizer.Format("#MechJeb_AttitudeAdjust_checkbox14")); //Command Pod Forward (electric blue)
            //arrows.avgForwardArrowActive = GUILayout.Toggle(arrows.avgForwardArrowActive, "Forward Avg (blue)");

            arrows.requestedAttitudeArrowActive =
                GUILayout.Toggle(arrows.requestedAttitudeArrowActive,
                    Localizer.Format("#MechJeb_AttitudeAdjust_checkbox15")); //Requested Attitude (gray)

            arrows.debugArrowActive =
                GUILayout.Toggle(arrows.debugArrowActive, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox16")); //Debug (magenta)

            arrows.debugArrow2Active =
                GUILayout.Toggle(arrows.debugArrow2Active, Localizer.Format("#MechJeb_AttitudeAdjust_checkbox17")); //Debug2 (light blue)


            GUILayout.EndVertical();


            base.WindowGUI(windowID);
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(350), GUILayout.Height(150) };

        public override string GetName() => Localizer.Format("#MechJeb_AttitudeAdjust_title"); //Attitude Adjustment

        public override string IconName() => "Attitude Adjustment";
    }
}
