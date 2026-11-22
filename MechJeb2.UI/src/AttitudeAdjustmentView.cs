using System;
using KodeKit;
using MuMech.AttitudeControllers;
using KodeUI;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleAttitudeAdjustment in KodeKit.
    /// The active controller's own settings are built natively (AttitudeControllerPanels), one panel per controller.
    /// The window is grouped in collapsible sections (new UI only): Controller, Numbers, Debug arrows.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleAttitudeAdjustment))]
    public class AttitudeAdjustmentView : KodeView
    {
        readonly MechJebModuleAttitudeAdjustment adj;

        public AttitudeAdjustmentView(MechJebModuleAttitudeAdjustment adj) : base(adj) => this.adj = adj;

        protected override float MinWidth => 350;

        // in the order of MechJebModuleAttitudeController.ActiveController
        static readonly System.Type[] ControllerTypes =
        {
            typeof(MJAttitudeController), typeof(KosAttitudeController), typeof(HybridController), typeof(BetterController),
        };

        MechJebModuleAttitudeController Attitude => adj.Core.Attitude;

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

        /// <summary>One panel per attitude controller, the active one shown, each built the first time it is active.</summary>
        void ControllerPanels(KodeWindow w, UIObject panels)
        {
            for (int i = 0; i < ControllerTypes.Length; i++)
            {
                if (!AttitudeControllerPanels.Builders.TryGetValue(ControllerTypes[i], out var build)) continue;
                int index = i;
                w.LazyPanel(panels, () => Attitude.ActiveController == index, panel => build(w, panel, Attitude));
            }
        }

        // adj.showInfos (persisted, shared with IMGUI) follows the "Numbers" section's fold
        void FollowSection(KodeWindow w, Layout infos) =>
            w.AddSync(() =>
            {
                bool open = infos.gameObject.activeSelf;
                if (adj.showInfos != open) adj.showInfos = open;
            });

        // the debug arrow options, in IMGUI's order
        static void SphereToggles(KodeWindow w, UIObject debug, MechJebModuleDebugArrows arrows)
        {
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox6", () => arrows.seeThrough, v => arrows.seeThrough = v); //Visible through object
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox7", () => arrows.comSphereActive, v => arrows.comSphereActive = v); //Display the CoM
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox8", () => arrows.colSphereActive, v => arrows.colSphereActive = v); //Display the CoL
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox9", () => arrows.cotSphereActive, v => arrows.cotSphereActive = v); //Display the CoT
        }

        static void ArrowToggles(KodeWindow w, UIObject debug, MechJebModuleDebugArrows arrows)
        {
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox10", () => arrows.displayAtCoM, v => arrows.displayAtCoM = v); //Arrows origins at the CoM
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox11", () => arrows.srfVelocityArrowActive, v => arrows.srfVelocityArrowActive = v); //Pod Surface Velocity (green)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox12", () => arrows.obtVelocityArrowActive, v => arrows.obtVelocityArrowActive = v); //Pod Orbital Velocity (red)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox13", () => arrows.dotArrowActive, v => arrows.dotArrowActive = v); //Direction of Thrust (purple pink)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox14", () => arrows.forwardArrowActive, v => arrows.forwardArrowActive = v); //Command Pod Forward (electric blue)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox15", () => arrows.requestedAttitudeArrowActive, v => arrows.requestedAttitudeArrowActive = v); //Requested Attitude (gray)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox16", () => arrows.debugArrowActive, v => arrows.debugArrowActive = v); //Debug (magenta)
            w.Toggle(debug, "#MechJeb_AttitudeAdjust_checkbox17", () => arrows.debugArrow2Active, v => arrows.debugArrow2Active = v); //Debug2 (light blue)
        }

        static readonly string[] controllerNames = { "MJ", "kOS", "Hybrid", "Better" };

        // The controller as one choice, switches, and the numbers as an X, Y, Z grid
        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            w.SwitchRow(r, "#MechJeb_AttitudeAdjust_checkbox1", () => Attitude.RCSAuto, v => Attitude.RCSAuto = v); //RCS auto mode

            Layout controller = w.Section(r, "#MechJeb_NewUI_SectionController");
            w.Segmented(controller, controllerNames, () => Attitude.ActiveController, i =>
            {
                if (Attitude.ActiveController != i) Attitude.SetActiveController(i);
            }, label: "#MechJeb_NewUI_SectionController"); //Controller
            Layout panels = KodeWindow.Panel(controller);
            ControllerPanels(w, panels);

            Layout infos = w.Section(r, "#MechJeb_NewUI_SectionNumbers", adj.showInfos);
            FollowSection(w, infos);
            bool Shown() => adj.showInfos && adj.Vessel != null;
            var grid = new AttitudeControllerPanels.VectorGrid(w, infos, true);
            grid.Row(KodeWindow.Loc("#MechJeb_AttitudeAdjust_Label1"), Shown, () => Attitude.AxisControl, false, false); //Axis Control
            grid.Row(KodeWindow.Loc("#MechJeb_AttitudeAdjust_Label2"), Shown, () => Attitude.Torque, false, true); //Torque
            grid.Row(KodeWindow.Loc("#MechJeb_AttitudeAdjust_Label3"), Shown, () => adj.VesselState.TorqueReactionSpeed, false, true); //torqueReactionSpeed
            grid.Row(KodeWindow.Loc("#MechJeb_AttitudeAdjust_Label4"), Shown, //MOI / torque
                () => Vector3d.Scale(adj.VesselState.MoI, Attitude.Torque.InvertNoNaN()), false, true);
            grid.Row(KodeWindow.Loc("#MechJeb_AttitudeAdjust_Label5"), Shown, () => adj.VesselState.MoI, false, true); //MOI
            grid.Row(KodeWindow.Loc("#MechJeb_AttitudeAdjust_Label6"), Shown, () => adj.Vessel.angularVelocity, false, true); //Angular Velocity
            grid.Row(KodeWindow.Loc("#MechJeb_AttitudeAdjust_Label7"), Shown, () => adj.VesselState.AngularMomentum, false, true); //Angular M
            InfoRow(w, infos, "#MechJeb_AttitudeAdjust_Label8", "0.000", t => t.Append(TimeWarp.fixedDeltaTime, 3)); //fixedDeltaTime

            MechJebModuleDebugArrows arrows = adj.Core.GetComputerModule<MechJebModuleDebugArrows>();
            if (arrows == null) return;

            w.Section(r, "#MechJeb_NewUI_SectionDebugArrows", false, debug =>
            {
                KodeWindow.PropertyGrid sizes = w.Properties(debug);
                sizes.FlexibleColumn = 0;
                Layout length = sizes.Row();
                KodeWindow.Label(length, "#MechJeb_AttitudeAdjust_Label9"); //Arrows length
                EditUi.Field(w, length, () => arrows.arrowsLength, 50);
                Layout radius = sizes.Row();
                KodeWindow.Label(radius, "#MechJeb_AttitudeAdjust_Label10"); //Radius of Sphere
                EditUi.Field(w, radius, () => arrows.comSphereRadius, 50);

                SphereToggles(w, debug, arrows);
                ArrowToggles(w, debug, arrows);
            });
        }
    }
}
