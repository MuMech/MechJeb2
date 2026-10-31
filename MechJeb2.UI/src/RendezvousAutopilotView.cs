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

        int Mode() => GuidanceUi.RendezvousMode(Core, window.Orbit);

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

            //"Select a target to rendezvous with.", "Rendezvous target must be in the same sphere of influence."
            Layout main = GuidanceUi.RendezvousGate(w, r, Mode, "#MechJeb_RZauto_label1", "#MechJeb_RZauto_label2");

            // "Rendezvous target": name on the right
            GuidanceUi.Pair(w, main, "#MechJeb_RZauto_label3", "Kerbin Station Alpha Mk2", ct => ct.Append(Core.Target.Name), () => Mode() == 2);

            UIButton engage = KodeWindow.Button(main, "#MechJeb_RZauto_button1", () => Autopilot.Users.Add(window), ButtonRole.Primary); //"Engage autopilot"
            w.Visible(engage.gameObject, () => Autopilot != null && !Autopilot.Enabled);
            UIButton disengage = KodeWindow.Button(main, "#MechJeb_RZauto_button2", () => Autopilot.Users.Remove(window), ButtonRole.Danger); //"Disengage autopilot"
            w.Visible(disengage.gameObject, () => Autopilot != null && Autopilot.Enabled);

            ValueRow(w, main, "#MechJeb_RZauto_label4", () => Autopilot?.desiredDistance, "m"); //"Desired final distance:"
            ValueRow(w, main, "#MechJeb_RZauto_label5", () => Autopilot?.maxPhasingOrbits, null); //"Max # of phasing orbits:"
            ValueRow(w, main, "#MechJeb_RZauto_label8", () => Autopilot?.maxClosingSpeed, "m/s"); //"Max closing velocity:"

            UIText warn = GuidanceUi.Warning(w, main, "#MechJeb_RZauto_label6"); //"Max # of phasing orbits must be at least 5."
            w.Visible(warn.gameObject, () => Autopilot != null && Autopilot.maxPhasingOrbits < 5);

            UIText status = w.Wrap(w.BoundLabel(main, GuidanceUi.Format1("#MechJeb_RZauto_label7", () => Autopilot?.status), true)); //"Status: <<1>>"
            w.Visible(status, () => Autopilot != null && Autopilot.Enabled);

            NodeUi.AutowarpToggle(w, main, Core, "#MechJeb_RZauto_checkbox1"); //"Auto-warp"
        }

        // GuiUtils.SimpleTextBox(label, editable, unit): the editable object is read again on every access
        static void ValueRow(KodeWindow w, UIObject parent, string key, System.Func<EditableDouble> ed, string unit) => EditUi.Row(w, parent, key, ed, unit);
    }
}
