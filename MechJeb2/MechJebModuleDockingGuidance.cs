extern alias JetBrainsAnnotations;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleDockingGuidance : DisplayModule
    {
        public MechJebModuleDockingGuidance(MechJebCore core) : base(core) { }

        private MechJebModuleDockingAutopilot autopilot;

        public override void OnStart(PartModule.StartState state) => autopilot = Core.GetComputerModule<MechJebModuleDockingAutopilot>();

        protected override void WindowGUI(int windowID)
        {
            if (!Core.Target.NormalTargetExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label1")); //"Choose a target to dock with"
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            // GetReferenceTransformPart is null after undocking ...
            if (Vessel.GetReferenceTransformPart() == null || !Vessel.GetReferenceTransformPart().Modules.Contains("ModuleDockingNode"))
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label2"),
                    GuiUtils.YellowLabel); //Warning: You need to control the vessel from a docking port. Right click a docking port and select "Control from here"
            }

            if (!(Core.Target.Target is ModuleDockingNode))
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label3"),
                    GuiUtils.YellowLabel); //Warning: target is not a docking port. Right click the target docking port and select "Set as target"
            }

            bool onAxisNodeExists = false;
            foreach (ITargetable node in Vessel.GetTargetables()
                         .Where(t => t.GetTargetingMode() == VesselTargetModes.DirectionVelocityAndOrientation))
            {
                if (Vector3d.Angle(node.GetTransform().forward, Vessel.ReferenceTransform.up) < 2)
                {
                    onAxisNodeExists = true;
                    break;
                }
            }

            if (!onAxisNodeExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label4"),
                    GuiUtils.YellowLabel); //Warning: this vessel is not controlled from a docking node. Right click the desired docking node on this vessel and select "Control from here."
            }

            bool active = GUILayout.Toggle(autopilot.Enabled, Localizer.Format("#MechJeb_Docking_checkbox1")); // "Autopilot enabled"
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Docking_label5"), autopilot.speedLimit, "m/s");  //"Speed limit"

            autopilot.overrideSafeDistance =
                GUILayout.Toggle(autopilot.overrideSafeDistance, Localizer.Format("#MechJeb_Docking_checkbox2")); //"Override Safe Distance"
            if (autopilot.overrideSafeDistance)
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Docking_checkbox3"), autopilot.overridenSafeDistance, "m"); //"Safe Distance"

            autopilot.overrideTargetSize =
                GUILayout.Toggle(autopilot.overrideTargetSize, Localizer.Format("#MechJeb_Docking_checkbox4")); //"Override Start Distance"
            if (autopilot.overrideTargetSize)
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Docking_label6"), autopilot.overridenTargetSize, "m"); //"Start Distance"

            if (autopilot.overridenSafeDistance < 0)
                autopilot.overridenSafeDistance = 0;

            if (autopilot.overridenTargetSize < 10)
                autopilot.overridenTargetSize = 10;

            autopilot.drawBoundingBox =
                GUILayout.Toggle(autopilot.drawBoundingBox, Localizer.Format("#MechJeb_Docking_checkbox5")); //"Draw Bounding Box"

            if (GUILayout.Button(Localizer.Format("#MechJeb_Docking_button"))) //"Dump Bounding Box Info"
            {
                Vessel.GetBoundingBox(true);

                if (Core.Target.Target != null)
                {
                    Vessel targetVessel = Core.Target.Target.GetVessel();
                    targetVessel.GetBoundingBox(true);
                }
            }


            GUILayout.Label(Localizer.Format("#MechJeb_Docking_label7", autopilot.safeDistance.ToString("F2")),
                GUILayout.ExpandWidth(false)); //"safeDistance "
            GUILayout.Label(Localizer.Format("#MechJeb_Docking_label8", autopilot.targetSize.ToString("F2")),
                GUILayout.ExpandWidth(false)); //"targetSize   "

            if (autopilot.speedLimit < 0)
                autopilot.speedLimit = 0;


            GUILayout.BeginHorizontal();
            autopilot.forceRol =
                GUILayout.Toggle(autopilot.forceRol, Localizer.Format("#MechJeb_Docking_checkbox6"), GUILayout.ExpandWidth(false)); //"Force Roll :"

            autopilot.rol.Text = GUILayout.TextField(autopilot.rol.Text, GUILayout.Width(30));
            GUILayout.Label("°", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            if (autopilot.Enabled != active)
            {
                if (active)
                {
                    autopilot.Users.Add(this);
                }
                else
                {
                    autopilot.Users.Remove(this);
                }
            }

            if (autopilot.Enabled)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label9", autopilot.status)); //"Status: <<1>>"
                Vector3d error = Core.RCS.targetVelocity - VesselState.orbitalVelocity;
                double error_x = Vector3d.Dot(error, Vessel.GetTransform().right);
                double error_y = Vector3d.Dot(error, Vessel.GetTransform().forward);
                double error_z = Vector3d.Dot(error, Vessel.GetTransform().up);
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label10", error_x.ToString("F2")) + " m/s  [L/J]"); //Error X: <<1>>
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label11", error_y.ToString("F2")) + " m/s  [I/K]"); //Error Y: <<1>>
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label12", error_z.ToString("F2")) + " m/s  [H/N]"); //Error Z: <<1>>

                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label13", autopilot.zSep.ToString("F2")) + "m"); //Distance Dock: <<1>>
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label14", autopilot.lateralSep.magnitude.ToString("F2")) +
                                "m"); //Distance Dock Axis: <<1>>
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(300), GUILayout.Height(50) };

        protected override void OnModuleDisabled()
        {
            if (autopilot != null) autopilot.Users.Remove(this);
        }

        public override string GetName() => Localizer.Format("#MechJeb_Docking_title"); //"Docking Autopilot"

        public override string IconName() => "Docking Autopilot";
    }
}
