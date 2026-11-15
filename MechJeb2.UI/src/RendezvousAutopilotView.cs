using KodeKit;
using KodeUI;
using KSP.Localization;

namespace MuMech.UI
{
    /// <summary>MechJebModuleRendezvousAutopilotWindow ("Rendezvous Autopilot") in KodeKit.</summary>
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

        readonly GuidanceUi.TargetName targetName = new GuidanceUi.TargetName();

        // why the autopilot can't start, or null; read five times a second (it formats its text)
        string problem;
        float nextProblem;

        protected override void BeforeSync()
        {
            if (UnityEngine.Time.unscaledTime < nextProblem) return;
            nextProblem = UnityEngine.Time.unscaledTime + 0.2f;
            problem = Autopilot != null && !Autopilot.Enabled && Mode() == 2 ? Autopilot.Problem(out _) : null;
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            //"Select a target to rendezvous with.", "Rendezvous target must be in the same sphere of influence."
            Layout main = GuidanceUi.RendezvousGate(w, r, Mode, "#MechJeb_RZauto_label1", "#MechJeb_RZauto_label2");

            // "Rendezvous target": name on the right
            GuidanceUi.Pair(w, main, "#MechJeb_RZauto_label3", "Kerbin Station Alpha Mk2", ct => ct.Append(targetName.Get(Core.Target)), () => Mode() == 2);

            // why it can't start, before it is pressed (it would stop at once with the same reason)
            UIText cant = KodeWindow.WrapToWidth(w.BoundLabel(main, () => problem ?? "", true));
            KodeWindow.ToneText(cant, KodeWindow.Tone.Warn);
            w.Visible(cant.gameObject, () => problem != null);

            UIButton engage = KodeWindow.Button(main, "#MechJeb_RZauto_button1", () => Autopilot.Users.Add(window)); //"Engage autopilot"
            w.BindRole(engage, () => problem == null ? ButtonRole.Primary : ButtonRole.Normal);
            GuidanceUi.Enable(w, engage, () => problem == null);
            w.Visible(engage.gameObject, () => Autopilot != null && !Autopilot.Enabled);
            UIButton disengage = KodeWindow.Button(main, "#MechJeb_RZauto_button2", () => Autopilot.Users.Clear(), ButtonRole.Danger); //"Disengage autopilot", also a run kOS started
            w.Visible(disengage.gameObject, () => Autopilot != null && Autopilot.Enabled);

            // one field column
            KodeWindow.PropertyGrid g = w.Properties(main);
            EditUi.Grid(g, "#MechJeb_NewUI_RzFinalDistance", () => Autopilot?.desiredDistance, "m", 60); //Desired final distance
            EditUi.Grid(g, "#MechJeb_NewUI_RzMaxPhasingOrbits", () => Autopilot?.maxPhasingOrbits, null, 60); //Max phasing orbits
            EditUi.Grid(g, "#MechJeb_NewUI_RzMaxClosingVelocity", () => Autopilot?.maxClosingSpeed, "m/s", 60); //Max closing velocity

            UIText warn = GuidanceUi.Warning(w, main, "#MechJeb_RZauto_label6"); //"Max # of phasing orbits must be at least 5."
            w.Visible(warn.gameObject, () => Autopilot != null && Autopilot.maxPhasingOrbits < 5);

            UIText status = w.Wrap(w.BoundLabel(main, GuidanceUi.Format1("#MechJeb_RZauto_label7", () => Autopilot?.status), true)); //"Status: <<1>>"
            // it stays after the autopilot stops: that's where the reason shows
            w.Visible(status, () => Autopilot != null && !string.IsNullOrEmpty(Autopilot.status));

            NodeUi.AutowarpToggle(w, main, Core, "#MechJeb_RZauto_checkbox1"); //"Auto-warp"
        }
    }
}
