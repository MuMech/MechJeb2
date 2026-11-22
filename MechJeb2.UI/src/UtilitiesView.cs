using System;
using KodeKit;
using KodeUI;
using MechJebLibBindings;
using static MuMech.MechJebModuleThrustController;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleThrustWindow ("Utilities") in KodeKit. The limiter and autostage rows
    /// are the general info items' native builders (GeneralItemBuilders), shared with the custom windows.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleThrustWindow))]
    public class UtilitiesView : KodeView
    {
        readonly MechJebModuleThrustWindow window;

        public UtilitiesView(MechJebModuleThrustWindow window) : base(window) => this.window = window;

        protected override float MinWidth => 250;

        MechJebCore Core => window.Core;
        MechJebModuleThrustController Thrust => window.Core.Thrust;

        // The same options in property grids (switches on the left, fields dimmed when off), under headers
        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            UIObject g = w.Group(r, "#MechJeb_NewUI_ThrottleLimits"); //Throttle limits
            KodeWindow.PropertyGrid limits = OptionRows.ThrustLimits(w, g, Thrust, Thrust, OptionRows.GeneralLimitsApply(Thrust),
                out Layout minThrottle, null, false);
            // greyed while an autopilot flies with its own throttle limits: these apply again when it stops. The landing has only its
            // own minimum throttle and keeps the general other limits, so only that row greys then
            limits.Dim(limits.Table.gameObject, null, () => Thrust.LimitsOverrideUser != null && !(Thrust.Limits is ThrustLimitsWithOwnMinThrottle));
            limits.Dim(minThrottle.gameObject, null, () => Thrust.Limits is ThrustLimitsWithOwnMinThrottle);
            Meters(w, g);
            // not one of the limits an autopilot replaces: its own grid, never greyed
            KodeWindow.PropertyGrid diff = w.Properties(g);
            OptionRows.Switch(diff, "#MechJeb_Ascent_checkbox19", () => Thrust.DifferentialThrottle, LimiterTones.SetDifferential(Thrust),
                tone: () => LimiterTones.Differential(Thrust)); //"Differential throttle"
            Layout diffBar = w.StatusBar(g, () => KodeWindow.Tone.Warn);
            OpUi.DynamicParagraph(w, diffBar, DifferentialStatus());
            w.Visible(diffBar.gameObject, DifferentialFailed);

            g = w.Group(r, "#MechJeb_NewUI_Jets"); //Jet engines
            KodeWindow.PropertyGrid jets = w.Properties(g);
            OptionRows.Switch(jets, "#MechJeb_Ascent_checkbox13", () => Thrust.LimitToPreventFlameout, v => Thrust.LimitToPreventFlameout = v,
                tone: LimiterTones.For(Thrust, LimitMode.FLAMEOUT)); //"Prevent jet flameout"
            OptionRows.Switch(jets, "#MechJeb_Utilities_checkbox3", () => Thrust.ManageIntakes, v => Thrust.ManageIntakes = v);
            OptionRows.Field(jets, "#MechJeb_Utilities_label2", Thrust.FlameoutSafetyPct, "%", 50);

            if (Core.Solarpanel != null || Core.AntennaControl != null)
            {
                g = w.Group(r, "#MechJeb_NewUI_Deployment"); //Deployment
                string extend = KSP.Localization.Localizer.Format("#MechJeb_NewUI_Extend"), retract = KSP.Localization.Localizer.Format("#MechJeb_NewUI_Retract");
                DeployRow(w, g, Core.Solarpanel, "#MechJeb_SolarPanelDeployButton", extend, retract); //"Auto-deploy solar panels"
                DeployRow(w, g, Core.AntennaControl, "#MechJeb_Autodeployantennas", extend, retract); //"Auto-deploy antennas"
            }

            g = w.Group(r, "#MechJeb_NewUI_Autostage"); //Autostage
            Layout auto = KodeWindow.Row(g);
            UnityEngine.UI.Toggle autostage = w.SwitchRow(auto, "#MechJeb_Utilities_checkbox1", () => Core.Staging.Users.Contains(window), v =>
            {
                if (v) Core.Staging.Users.Add(window);
                else Core.Staging.Users.Remove(window);
                window.autostageSavedState = v;
            }); //"Autostage"
            // the IMGUI item stored the state on every draw
            w.AddSync(() => window.autostageSavedState = Core.Staging.Users.Contains(window));
            UIButton once = KodeWindow.Button(auto, "#MechJeb_Utilities_button1", () => Core.Staging.AutostageOnce(window)); //"Autostage once"
            w.Visible(once.gameObject, () => !Core.Staging.Enabled);

            // the rules can be set before autostage is turned on; folded, the window keeps its size when it turns on
            Layout settings = w.Section(g, KSP.Localization.Localizer.Format("#MechJeb_NewUI_AutostageRules"), false, "UtilitiesAutostageRules"); //Autostage rules
            KodeWindow.PropertyGrid rules = OptionRows.AutostageSettings(w, settings, Core.Staging, Core.Staging);
            // greyed while an autopilot stages by its own rules
            rules.Dim(rules.Table.gameObject, null, () => Core.Staging.RulesOverrideUser != null);

            Layout statusBar = w.StatusBar(g, () => KodeWindow.Tone.Normal);
            w.BoundLabel(statusBar, AutostageStatus(), true);
            w.Visible(statusBar.gameObject, () => Core.Staging.Enabled);
        }

        /// <summary>
        /// The throttle and the acceleration as meters, against the limits that apply now (the general ones, or an autopilot's own),
        /// shown only with the Settings toggle on (off by default). They only draw: nothing feeds back into the limiter.
        /// <list type="bullet">
        /// <item>Throttle, 0 to 100 %: high over "Limit throttle to" while it is on, low under "Keep limited throttle over" while
        /// it is on and the engines are throttled up (an idle throttle is not below that minimum); in range is good, either end fair.</item>
        /// <item>Acceleration, 0 to the vessel's maximum thrust acceleration: high over "Limit acceleration to" while it is on (good
        /// under it, fair over it); with no thrust available the meter says not available.</item>
        /// <item>The limiter holds both at their limits, in manual flight too: a value at its limit is in range (each meter's
        /// tolerance), so a bar at its mark reads as the limit acting, not as over it.</item>
        /// </list>
        /// </summary>
        void Meters(KodeWindow w, UIObject parent)
        {
            // under the limits, in their group; a hidden meter isn't read
            Layout box = KodeWindow.Panel(parent);
            box.name = "Meters";
            w.Visible(box.gameObject, () => Core.Settings != null && Core.Settings.UiMeters);
            // one grid: the two bars start at one column
            KodeWindow.PropertyGrid grid = w.Properties(box);
            grid.FlexibleColumn = 1;
            double Throttle() => window.Vessel != null ? window.Vessel.ctrlState.mainThrottle : double.NaN;
            double MinThrottle() => Thrust.Limits.LimiterMinThrottle && Throttle() > 0 ? Thrust.Limits.MinThrottle.Val : double.NaN;
            double MaxThrottle() => Thrust.Limits.LimitThrottle ? Thrust.Limits.MaxThrottle.Val : double.NaN;
            var throttle = new KodeKit.Headless.GaugeModel(Throttle, null, null, MinThrottle, MaxThrottle, () =>
            {
                double lo = MinThrottle(), hi = MaxThrottle();
                if (double.IsNaN(lo) && double.IsNaN(hi)) return double.NaN;
                return ((double.IsNaN(lo) ? 0 : lo) + (double.IsNaN(hi) ? 1 : hi)) / 2;
            }, (b, v) => b.Append(v * 100, 0).Append(" %")) { Tolerance = 0.001 }; // a float throttle held at a double limit
            grid.Meter("#MechJeb_NewUI_MeterThrottle", throttle, "100 % " + Longest()); //Throttle

            double MaxAccel() => Thrust.Limits.LimitAcceleration ? Thrust.Limits.MaxAcceleration.Val : double.NaN;
            var accel = new KodeKit.Headless.GaugeModel(() => window.Vessel != null ? Core.VesselState.CurrentThrustAcceleration : double.NaN, null,
                () => Core.VesselState.MaxThrustAcceleration, null, MaxAccel, () => double.IsNaN(MaxAccel()) ? double.NaN : 0,
                (b, v) => b.Append(v, 2).Append(" m/s²")) { Tolerance = 0.005 }; // the limiter holds it at the limit, give or take a frame's mass
            grid.Meter("#MechJeb_NewUI_MeterAcceleration", accel, "99.99 m/s² " + Longest()); //Acceleration
        }

        // the longest zone word a meter's text can end with, in the current language
        static string Longest()
        {
            KodeKit.Headless.GaugeWords words = KodeWindow.MeterWords;
            return words.Low.Length > words.High.Length ? words.Low : words.High;
        }

        /// <summary>[switch] Auto-deploy ... [Extend | Retract]: the button says what a press does now.</summary>
        static void DeployRow(KodeWindow w, UIObject parent, MechJebModuleDeployableController c, string autoKey, string extend, string retract)
        {
            // a part's blacklist can leave the core without the controller
            if (c == null) return;
            Layout row = KodeWindow.Row(parent);
            w.SwitchRow(row, autoKey, () => c.AutoDeploy, v => c.AutoDeploy = v);
            // both walk every deployable part: read five times a second
            bool retracts = c.ToggleRetracts, moving = c.Moving;
            float next = 0;
            UIButton b = KodeWindow.Button(row, retracts ? retract : extend, c.ToggleDeploy);
            // its text is the action a press does now: the bridge names it by its row
            KodeWindow.NameButton(b, autoKey + "/Deploy");
            GuidanceUi.Enable(w, b, () => !moving);
            w.AddSync(() =>
            {
                if (UnityEngine.Time.unscaledTime < next) return;
                next = UnityEngine.Time.unscaledTime + 0.2f;
                moving = c.Moving;
                bool now = moving ? retracts : c.ToggleRetracts; // while parts move, the button keeps the action that moves them
                if (now == retracts) return;
                retracts = now;
                b.Text(now ? retract : extend);
            });
        }

        bool DifferentialFailed() =>
            Thrust.DifferentialThrottle && window.Vessel.LiftedOff() && Thrust.DifferentialThrottleSuccess != DifferentialThrottleStatus.SUCCESS;

        // the texts, made when what they show changes (AutostageStatus concatenates a new string on every call)
        Func<string> DifferentialStatus() => Memo.Of(() => (int)Thrust.DifferentialThrottleSuccess, _ => DifferentialStatusText());

        Func<string> AutostageStatus() =>
            Memo.Of(() => (Core.Staging.Enabled, Core.Staging.AutostagingOnce, (int)Core.Staging.Rules.AutostageLimit), _ => Core.Staging.AutostageStatus());

        string DifferentialStatusText()
        {
            switch (Thrust.DifferentialThrottleSuccess)
            {
                case DifferentialThrottleStatus.MORE_ENGINES_REQUIRED: return KSP.Localization.Localizer.Format("#MechJeb_Utilities_label3");
                case DifferentialThrottleStatus.ALL_ENGINES_OFF: return KSP.Localization.Localizer.Format("#MechJeb_Utilities_label4");
                case DifferentialThrottleStatus.SOLVER_FAILED: return KSP.Localization.Localizer.Format("#MechJeb_Utilities_label5");
                default: return "";
            }
        }
    }
}
