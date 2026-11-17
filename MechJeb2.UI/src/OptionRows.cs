using System;
using KodeKit;
using KodeUI;
using MechJebLibBindings;
using Tone = KodeKit.KodeWindow.Tone;
using static MuMech.MechJebModuleThrustController;

namespace MuMech.UI
{
    /// <summary>
    /// The option blocks shared by several windows: the thrust limiters (Utilities, Ascent Settings) and
    /// the autostage settings, as property grids with the switches on the left. Each is bound to a set of values: the
    /// controller's general ones (Utilities) or a module's own (Ascent Settings).
    /// </summary>
    internal static class OptionRows
    {
        /// <summary>[switch] label [field] unit on an IEditable; the field dims while the switch is off.</summary>
        public static Layout Switch(KodeWindow.PropertyGrid g, string label, Func<bool> get, Action<bool> set, IEditable ed = null,
            string unit = null, float width = 60, Func<Tone> tone = null) =>
            ed == null
                ? g.Switch(label, get, set, tone: tone)
                : g.Switch(label, get, set, () => ed.Text, s => ed.Text = s, width, unit, EditUi.Valid(ed), tone);

        /// <summary>label [field] unit on an IEditable (MechJeb's labels often start with spaces: they are trimmed).</summary>
        public static Layout Field(KodeWindow.PropertyGrid g, string label, IEditable ed, string unit, float width = 60) =>
            g.Field(KodeWindow.Loc(label).Trim(), () => ed.Text, s => ed.Text = s, width, unit, EditUi.Valid(ed));

        /// <summary>
        /// The thrust limiters of <paramref name="l"/> (the controller's general ones, or a module's own), one switch row each,
        /// the label green while that limiter acts and <paramref name="l"/> applies now (<paramref name="applies"/>).
        /// <paramref name="psg"/>: PSG ascents fly without the throttle and electric limits, so those rows hide.
        /// </summary>
        public static KodeWindow.PropertyGrid ThrustLimits(KodeWindow w, UIObject parent, MechJebModuleThrustController t, IThrustLimits l,
            Func<bool> applies, Func<bool> psg = null, bool jets = true) => ThrustLimits(w, parent, t, l, applies, out _, psg, jets);

        /// <summary>As above, with the "Keep limited throttle over" row in <paramref name="minThrottle"/>.</summary>
        public static KodeWindow.PropertyGrid ThrustLimits(KodeWindow w, UIObject parent, MechJebModuleThrustController t, IThrustLimits l,
            Func<bool> applies, out Layout minThrottle, Func<bool> psg = null, bool jets = true)
        {
            KodeWindow.PropertyGrid g = w.Properties(parent);
            Switch(g, "#MechJeb_Ascent_checkbox12", () => l.LimitToPreventOverheats, v => l.LimitToPreventOverheats = v,
                tone: LimiterTones.For(t, LimitMode.TEMPERATURE, applies)); //"Prevent engine overheats"
            Switch(g, "#MechJeb_Ascent_checkbox11", () => l.LimitDynamicPressure, v => l.LimitDynamicPressure = v, l.MaxDynamicPressure, "Pa", 55,
                LimiterTones.For(t, LimitMode.DYNAMIC_PRESSURE, applies)); //Limit to Max Q
            Switch(g, "#MechJeb_Ascent_checkbox16", () => l.LimitAcceleration, v => l.LimitAcceleration = v, l.MaxAcceleration, "m/s²", 55,
                LimiterTones.For(t, LimitMode.ACCELERATION, applies)); //"Limit acceleration to"
            Layout throttle = Switch(g, "#MechJeb_Ascent_checkbox17", () => l.LimitThrottle, v => l.LimitThrottle = v, l.MaxThrottle, "%", 55,
                () => LimiterTones.Throttle(t, l, applies)); //"Limit throttle to"
            minThrottle = MinThrottle(g, t, l, applies, 55); //"Keep limited throttle over"

            // electric: [switch] Electric limit Lo [lo] % Hi [hi] %, the "% Hi [hi] %" in the unit column
            Layout electric = Switch(g, "#MechJeb_Ascent_checkbox20", () => l.ElectricThrottle, v => l.ElectricThrottle = v,
                tone: LimiterTones.Electric(t, applies));
            TMPro.TMP_InputField lo = EditFields.Field(w, electric, () => l.ElectricThrottleLo.Text, s => l.ElectricThrottleLo.Text = s, 55,
                () => l.ElectricThrottleLo.IsParsed);
            Layout hi = KodeWindow.Row(electric);
            hi.FlexibleLayout(false, false);
            KodeWindow.Label(hi, "% Hi");
            EditUi.Field(w, hi, () => l.ElectricThrottleHi, 40);
            KodeWindow.Label(hi, "%");
            g.Dim(lo.gameObject, hi.gameObject, () => !l.ElectricThrottle);

            if (psg != null)
            {
                w.Visible(throttle.gameObject, () => !psg());
                w.Visible(electric.gameObject, () => !psg());
            }

            if (jets)
                Switch(g, "#MechJeb_Ascent_checkbox13", () => t.LimitToPreventFlameout, v => t.LimitToPreventFlameout = v,
                    tone: LimiterTones.For(t, LimitMode.FLAMEOUT)); //"Prevent jet flameout"
            if (ReflectionUtils.IsLoadedRealFuels)
            {
                // does nothing in stock, so it is not shown if RF is not loaded
                Switch(g, "#MechJeb_Ascent_checkbox14", () => l.LimitToPreventUnstableIgnition, v => l.LimitToPreventUnstableIgnition = v,
                    tone: LimiterTones.For(t, LimitMode.UNSTABLE_IGNITION, applies)); //"Prevent unstable ignition"
                Switch(g, "#MechJeb_Ascent_checkbox15", () => l.AutoRCSUllaging, v => l.AutoRCSUllaging = v,
                    tone: LimiterTones.For(t, LimitMode.AUTO_RCS_ULLAGE, applies)); //"Use RCS to ullage"
            }
            return g;
        }

        /// <summary>[switch] Keep limited throttle over [min] %, yellow when the setting looks wrong for the fuel model.</summary>
        public static Layout MinThrottle(KodeWindow.PropertyGrid g, MechJebModuleThrustController t, IThrustLimits l, Func<bool> applies,
            float width) =>
            Switch(g, "#MechJeb_Ascent_checkbox18", () => l.LimiterMinThrottle, v => l.LimiterMinThrottle = v, l.MinThrottle, "%", width,
                () => LimiterTones.MinThrottle(t, l, applies)); //"Keep limited throttle over"

        /// <summary>The general limits apply while no autopilot has given the controller its own.</summary>
        public static Func<bool> GeneralLimitsApply(MechJebModuleThrustController t) => () => t.LimitsOverrideUser == null;

        /// <summary>The clamp field's setter: the rules keep the clamped value apart from the text.</summary>
        public static void SetClamp(IStagingRules r, string text)
        {
            r.ClampAutoStageThrustPct.Text = text;
            StagingRules.ClampThrustPct(r);
        }

        /// <summary>
        /// StagingController.AutostageSettingsInfoItem on <paramref name="r"/> (the controller's general rules, or a module's own):
        /// limit, delays, clamp, fairings, hot staging, solids.
        /// </summary>
        public static KodeWindow.PropertyGrid AutostageSettings(KodeWindow w, UIObject parent, MechJebModuleStagingController s, IStagingRules r)
        {
            KodeWindow.PropertyGrid g = w.Properties(parent);
            Field(g, "#MechJeb_Ascent_label42", r.AutostageLimit, "", 40); //"Stop at stage #"
            Field(g, "#MechJeb_NewUI_PreDelay", r.AutostagePreDelay, "s", 50); //Delay before staging
            Field(g, "#MechJeb_NewUI_PostDelay", r.AutostagePostDelay, "s", 50); //Delay after staging
            g.Field(CachedLocalizer.Instance.MechJebAscentLabel44.Trim(), () => r.ClampAutoStageThrustPct.Text, x => SetClamp(r, x), 50, "%",
                () => r.ClampAutoStageThrustPct.IsParsed); //"Clamp AutoStage Thrust "

            g.Note("#MechJeb_Ascent_label38"); //"Stage fairings when:"
            Switch(g, s._sFairingMaxDynamicPressure.Trim(), () => r.FairingMaxDynamicPressureEnabled, v => r.FairingMaxDynamicPressureEnabled = v,
                r.FairingMaxDynamicPressure, "kPa", 50);
            Switch(g, s._sFairingMinAltitude.Trim(), () => r.FairingMinAltitudeEnabled, v => r.FairingMinAltitudeEnabled = v,
                r.FairingMinAltitude, "km", 50);
            Switch(g, s._sFairingMaxAerothermalFlux.Trim(), () => r.FairingMaxAerothermalFluxEnabled, v => r.FairingMaxAerothermalFluxEnabled = v,
                r.FairingMaxAerothermalFlux, "W/m²", 50);

            Switch(g, "#MechJeb_Ascent_hotStaging", () => r.HotStaging, v => r.HotStaging = v); //"Support hotstaging"
            Layout lead = Field(g, s._sLeadTime.TrimEnd(':', ' '), r.HotStagingLeadTime, "s", 50);
            g.Dim(lead.gameObject, null, () => !r.HotStaging);
            Switch(g, "#MechJeb_Ascent_dropSolids", () => r.DropSolids, v => r.DropSolids = v, r.DropSolidsTwrPct, "% rocket accel", 50); //"Drop solids early"
            return g;
        }
    }
}
