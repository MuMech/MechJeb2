using System;
using KodeKit;
using MuMech.AttitudeControllers;
using KodeUI;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleAttitudeAdjustment in KodeKit, row for row with its WindowGUI.
    /// The active controller's own settings are built natively (AttitudeControllerPanels), one panel per controller.
    /// The window is grouped in collapsible sections (new UI only): Controller, Numbers, Debug arrows.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleAttitudeAdjustment))]
    public class AttitudeAdjustmentView : KodeView
    {
        readonly MechJebModuleAttitudeAdjustment adj;

        public AttitudeAdjustmentView(MechJebModuleAttitudeAdjustment adj) : base(adj) => this.adj = adj;

        protected override float MinWidth => 350;

        // in the order of MechJebModuleAttitudeController.activeController
        static readonly System.Type[] ControllerTypes =
        {
            typeof(MJAttitudeController), typeof(KosAttitudeController), typeof(HybridController), typeof(BetterController),
            typeof(LQRController),
        };

        MechJebModuleAttitudeController Attitude => adj.Core.Attitude;

        /// <summary>One radio-like toggle: checked while it is the active controller; clicking sets it.</summary>
        void ControllerToggle(KodeWindow w, UIObject parent, string text, int index) =>
            w.Toggle(parent, text, () => Attitude.activeController == index, v =>
            {
                // GUILayout.Toggle(current == index) returned true only when switched on; unchecking the active one did nothing
                if (v && Attitude.activeController != index) Attitude.SetActiveController(index);
            });

        static void Padded(CharText t, double v, int decimals) => AttitudeControllerPanels.Padded(t, v, decimals);
        static void Vector(CharText t, Vector3d v) => AttitudeControllerPanels.WithMagnitude(t, v);

        const string VectorTemplate = AttitudeControllerPanels.VectorTemplate;

        /// <summary>label (flexible) + a live value, committed in a sync only while the numbers are shown.</summary>
        void InfoRow(KodeWindow w, UIObject parent, string labelKey, string template, Action<CharText> fill)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, labelKey, true);
            UIText value = KodeWindow.LiveLabel(row, template, false);
            value.tmpText.alignment = TMPro.TextAlignmentOptions.Right;
            var text = new CharText(value.tmpText);
            w.AddSync(() =>
            {
                if (!adj.showInfos || adj.Vessel == null) return;
                text.Clear();
                fill(text);
                text.Commit();
            });
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            w.Toggle(r, "#MechJeb_AttitudeAdjust_checkbox1", () => Attitude.RCS_auto, v => Attitude.RCS_auto = v); //RCS auto mode

            // the radio choice and the active controller's settings
            Layout controller = w.Section(r, "Controller");
            ControllerToggle(w, controller, "#MechJeb_AttitudeAdjust_checkbox2", 0); //MJAttitudeController
            ControllerToggle(w, controller, "#MechJeb_AttitudeAdjust_checkbox3", 1); //KosAttitudeController
            ControllerToggle(w, controller, "#MechJeb_AttitudeAdjust_checkbox4", 2); //HybridController
            ControllerToggle(w, controller, "BetterController", 3);
            ControllerToggle(w, controller, "LQRController", 4);

            // the active controller's settings, indented by 20; one panel per controller, built once, the active one shown
            Layout indent = KodeWindow.Row(controller);
            KodeWindow.Space(indent, 20);
            Layout panels = KodeWindow.Panel(indent);
            for (int i = 0; i < ControllerTypes.Length; i++)
            {
                if (!AttitudeControllerPanels.Builders.TryGetValue(ControllerTypes[i], out var build)) continue;
                Layout panel = KodeWindow.Panel(panels);
                build(w, panel, Attitude);
                int index = i;
                w.Visible(panel.gameObject, () => Attitude.activeController == index);
            }

            // "Show Numbers" is the section: open = numbers shown. adj.showInfos (persisted, shared with IMGUI) follows
            // the section, so the readouts below are only computed while it is open.
            Layout infos = w.Section(r, "Numbers", adj.showInfos);
            w.AddSync(() =>
            {
                bool open = infos.gameObject.activeSelf;
                if (adj.showInfos != open) adj.showInfos = open;
            });
            InfoRow(w, infos, "#MechJeb_AttitudeAdjust_Label1", "[ 1,  1,  1 ]", t => //Axis Control
            {
                Vector3d a = Attitude.AxisControl;
                t.Append('[');
                Padded(t, a.x, 0);
                t.Append(", ");
                Padded(t, a.y, 0);
                t.Append(", ");
                Padded(t, a.z, 0);
                t.Append(" ]");
            });
            InfoRow(w, infos, "#MechJeb_AttitudeAdjust_Label2", VectorTemplate, t => Vector(t, Attitude.torque)); //Torque
            InfoRow(w, infos, "#MechJeb_AttitudeAdjust_Label3", VectorTemplate, //torqueReactionSpeed
                t => Vector(t, adj.VesselState.TorqueReactionSpeed));
            InfoRow(w, infos, "#MechJeb_AttitudeAdjust_Label4", VectorTemplate, t => //MOI / torque
                Vector(t, Vector3d.Scale(adj.VesselState.MoI, Attitude.torque.InvertNoNaN())));
            InfoRow(w, infos, "#MechJeb_AttitudeAdjust_Label5", VectorTemplate, t => Vector(t, adj.VesselState.MoI)); //MOI
            InfoRow(w, infos, "#MechJeb_AttitudeAdjust_Label6", VectorTemplate, //Angular Velocity
                t => Vector(t, adj.Vessel.angularVelocity));
            InfoRow(w, infos, "#MechJeb_AttitudeAdjust_Label7", VectorTemplate, //Angular M
                t => Vector(t, adj.VesselState.AngularMomentum));
            InfoRow(w, infos, "#MechJeb_AttitudeAdjust_Label8", "0.000", t => t.Append(TimeWarp.fixedDeltaTime, 3)); //fixedDeltaTime

            MechJebModuleDebugArrows arrows = adj.Core.GetComputerModule<MechJebModuleDebugArrows>();
            if (arrows == null) return;

            Layout debug = w.Section(r, "Debug arrows", false);
            Layout length = KodeWindow.Row(debug);
            KodeWindow.Label(length, "#MechJeb_AttitudeAdjust_Label9"); //Arrows length
            EditFields.Field(w, length, () => arrows.arrowsLength.Text, s => arrows.arrowsLength.Text = s, 50, () => arrows.arrowsLength.IsParsed);

            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox6", () => arrows.seeThrough, v => arrows.seeThrough = v); //Visible through object
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox7", () => arrows.comSphereActive, v => arrows.comSphereActive = v); //Display the CoM
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox8", () => arrows.colSphereActive, v => arrows.colSphereActive = v); //Display the CoL
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox9", () => arrows.cotSphereActive, v => arrows.cotSphereActive = v); //Display the CoT

            Layout radius = KodeWindow.Row(debug);
            KodeWindow.Label(radius, "#MechJeb_AttitudeAdjust_Label10", true); //Radius of Sphere
            EditFields.Field(w, radius, () => arrows.comSphereRadius.Text, s => arrows.comSphereRadius.Text = s, 40,
                () => arrows.comSphereRadius.IsParsed);

            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox10", () => arrows.displayAtCoM, v => arrows.displayAtCoM = v); //Arrows origins at the CoM
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox11", () => arrows.srfVelocityArrowActive, v => arrows.srfVelocityArrowActive = v); //Pod Surface Velocity (green)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox12", () => arrows.obtVelocityArrowActive, v => arrows.obtVelocityArrowActive = v); //Pod Orbital Velocity (red)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox13", () => arrows.dotArrowActive, v => arrows.dotArrowActive = v); //Direction of Thrust (purple pink)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox14", () => arrows.forwardArrowActive, v => arrows.forwardArrowActive = v); //Command Pod Forward (electric blue)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox15", () => arrows.requestedAttitudeArrowActive, v => arrows.requestedAttitudeArrowActive = v); //Requested Attitude (gray)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox16", () => arrows.debugArrowActive, v => arrows.debugArrowActive = v); //Debug (magenta)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox17", () => arrows.debugArrow2Active, v => arrows.debugArrow2Active = v); //Debug2 (light blue)
        }
    }
}
