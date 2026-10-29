using KodeKit;
using KodeUI;
using KSP.Localization;

namespace MuMech.UI
{
    /// <summary>MechJebModuleRendezvousAutopilotWindow ("Rendezvous Autopilot") in KodeKit, row for row with its WindowGUI.</summary>
    [KodeViewFor(typeof(MechJebModuleRendezvousAutopilotWindow))]
    public class RendezvousAutopilotView : KodeView
    {
        readonly MechJebModuleRendezvousAutopilotWindow window;
        MechJebModuleRendezvousAutopilot autopilot;

        public RendezvousAutopilotView(MechJebModuleRendezvousAutopilotWindow window) : base(window) => this.window = window;

        protected override float MinWidth => 300;

        MechJebCore Core => window.Core;

        MechJebModuleRendezvousAutopilot Autopilot => autopilot ??= Core.GetComputerModule<MechJebModuleRendezvousAutopilot>();

        // 0 = no target, 1 = target in another sphere of influence, 2 = usable target
        int Mode()
        {
            if (Core.Target == null || !Core.Target.NormalTargetExists) return 0;
            return Core.Target.TargetOrbit.referenceBody != window.Orbit.referenceBody ? 1 : 2;
        }

        // the autopilot is released while the target is in another sphere of influence
        protected override void BeforeSync()
        {
            MechJebModuleRendezvousAutopilot ap = Autopilot;
            if (Mode() == 1 && ap != null && ap.Enabled)
                ap.Users.Remove(window);
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout none = KodeWindow.Panel(r);
            GuidanceUi.Wrapped(none, "#MechJeb_RZauto_label1"); //"Select a target to rendezvous with."
            w.Visible(none.gameObject, () => Mode() == 0);

            Layout other = KodeWindow.Panel(r);
            GuidanceUi.Wrapped(other, "#MechJeb_RZauto_label2"); //"Rendezvous target must be in the same sphere of influence."
            w.Visible(other.gameObject, () => Mode() == 1);

            Layout main = KodeWindow.Panel(r);
            w.Visible(main.gameObject, () => Mode() == 2);

            // "Rendezvous target": name on the right
            GuidanceUi.Pair(w, main, "#MechJeb_RZauto_label3", "Kerbin Station Alpha Mk2", ct => ct.Append(Core.Target.Name), () => Mode() == 2);

            UIButton engage = KodeWindow.Button(main, "#MechJeb_RZauto_button1", () => Autopilot.Users.Add(window)); //"Engage autopilot"
            w.Visible(engage.gameObject, () => Autopilot != null && !Autopilot.Enabled);
            UIButton disengage = KodeWindow.Button(main, "#MechJeb_RZauto_button2", () => Autopilot.Users.Remove(window)); //"Disengage autopilot"
            w.Visible(disengage.gameObject, () => Autopilot != null && Autopilot.Enabled);

            ValueRow(w, main, "#MechJeb_RZauto_label4", () => Autopilot?.desiredDistance, "m"); //"Desired final distance:"
            ValueRow(w, main, "#MechJeb_RZauto_label5", () => Autopilot?.maxPhasingOrbits, null); //"Max # of phasing orbits:"
            ValueRow(w, main, "#MechJeb_RZauto_label8", () => Autopilot?.maxClosingSpeed, "m/s"); //"Max closing velocity:"

            UIText warn = GuidanceUi.Warning(w, main, "#MechJeb_RZauto_label6"); //"Max # of phasing orbits must be at least 5."
            w.Visible(warn.gameObject, () => Autopilot != null && Autopilot.maxPhasingOrbits < 5);

            UIText status = w.BoundLabel(main, GuidanceUi.Format1("#MechJeb_RZauto_label7", () => Autopilot?.status), true); //"Status: <<1>>"
            w.Visible(status.transform.parent.gameObject, () => Autopilot != null && Autopilot.Enabled);

            w.Toggle(main, "#MechJeb_RZauto_checkbox1", () => Core.Node != null && Core.Node.Autowarp, v => //"Auto-warp"
            {
                if (Core.Node != null) Core.Node.Autowarp = v;
            });
        }

        // GuiUtils.SimpleTextBox(label, editable, unit): the editable object is read again on every access
        void ValueRow(KodeWindow w, UIObject parent, string key, System.Func<EditableDouble> ed, string unit)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, key);
            EditFields.Field(w, row, () => ed()?.Text ?? "", s =>
            {
                EditableDouble e = ed();
                if (e != null) e.Text = s;
            }, 100, () => ed()?.IsParsed ?? true);
            if (unit != null) KodeWindow.Label(row, unit);
        }
    }
}
