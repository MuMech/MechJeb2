using KodeKit;
using KodeUI;
using KSP.Localization;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleWarpHelper ("Warp Helper") in KodeKit, row for row with its WindowGUI.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleWarpHelper))]
    public class WarpHelperView : KodeView
    {
        readonly MechJebModuleWarpHelper helper;

        public WarpHelperView(MechJebModuleWarpHelper helper) : base(helper) => this.helper = helper;

        protected override float MinWidth => 240;

        MechJebCore Core => helper.Core;

        bool TargetExists() => Core.Target != null && Core.Target.NormalTargetExists;

        // "Warping to [lead time before ]target.", rebuilt only when the lead time or the target changes
        double shownLead = double.NaN;
        int shownTarget = -1;
        string warpingText;

        string WarpingText()
        {
            double lead = helper.leadTime;
            int target = (int)helper.warpTarget;
            if (warpingText == null || lead != shownLead || target != shownTarget)
            {
                shownLead = lead;
                shownTarget = target;
                warpingText = Localizer.Format("#MechJeb_WarpHelper_label6") + (lead > 0 ? GuiUtils.TimeToDHMS(lead) + " before " : "") +
                    MechJebModuleWarpHelper.warpTargetStrings[target] + "."; //"Warping to "
            }

            return warpingText;
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout warpTo = KodeWindow.Row(r);
            KodeWindow.Label(warpTo, "#MechJeb_WarpHelper_label1"); //"Warp to: "
            new ComboBox(w, warpTo, () => (int)helper.warpTarget, i => helper.warpTarget = (MechJebModuleWarpHelper.WarpTarget)i,
                () => MechJebModuleWarpHelper.warpTargetStrings);

            Layout time = KodeWindow.Row(r);
            KodeWindow.Label(time, "#MechJeb_WarpHelper_label2", true); //"Warp for: "
            EditUi.Field(w, time, () => helper.timeOffset, 100);
            w.Visible(time.gameObject, () => helper.warpTarget == MechJebModuleWarpHelper.WarpTarget.Time);

            // I wonder if I should check for target that don't make sense
            UIText needTarget = KodeWindow.Label(r, "#MechJeb_WarpHelper_label3"); //"You need a target"
            w.Visible(needTarget.gameObject, () => helper.warpTarget == MechJebModuleWarpHelper.WarpTarget.PhaseAngleT && !TargetExists());
            Layout phase = KodeWindow.Row(r);
            KodeWindow.Label(phase, "#MechJeb_WarpHelper_label4"); //"Phase Angle:"
            EditUi.Field(w, phase, () => helper.phaseAngle, 60);
            KodeWindow.Label(phase, "º");
            w.Visible(phase.gameObject, () => helper.warpTarget == MechJebModuleWarpHelper.WarpTarget.PhaseAngleT && TargetExists());

            Layout lead = KodeWindow.Row(r);
            KodeWindow.Label(lead, "#MechJeb_WarpHelper_label5"); //"Lead time: "
            EditUi.Field(w, lead, () => helper.leadTime, 100);
            UIButton abort = KodeWindow.Button(lead, "#MechJeb_WarpHelper_button1", helper.AbortWarp, ButtonRole.Danger); //"Abort"
            w.Visible(abort.gameObject, () => helper.warping);
            UIButton warp = KodeWindow.Button(lead, "#MechJeb_WarpHelper_button2", helper.StartWarp, ButtonRole.Primary); //"Warp"
            w.Visible(warp.gameObject, () => !helper.warping);

            // useQuickWarpInfoItem()
            w.Toggle(r, "#MechJeb_WarpHelper_checkbox1", () => Core.Warp.useQuickWarp, v => Core.Warp.useQuickWarp = v); //"Quick warp"

            UIText warping = w.BoundLabel(r, "Warping to 99y 999d 99h 99m 99s before atmospheric entry.", WarpingText, true);
            w.Visible(warping, () => helper.warping);

            // ControlWarpButton
            GeneralItemBuilders.Build(w, r, "General:WarpController.ControlWarpButton", Core.Warp);
        }
    }
}
