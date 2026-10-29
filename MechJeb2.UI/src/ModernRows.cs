using System;
using KodeKit;
using KodeUI;
using MechJebLibBindings;
using UnityEngine;
using Tone = KodeKit.KodeWindow.Tone;
using static MuMech.MechJebModuleThrustController;

namespace MuMech.UI
{
    /// <summary>
    /// The modern layout's option blocks shared by several windows: the thrust limiters (Utilities, Ascent Settings) and
    /// the autostage settings, as property grids with the switches on the left.
    /// </summary>
    internal static class ModernRows
    {
        static Func<bool> Valid(IEditable ed) => ed is EditableDoubleMult m ? (Func<bool>)(() => m.IsParsed) : null;

        /// <summary>[switch] label [field] unit on an IEditable; the field dims while the switch is off.</summary>
        public static Layout Switch(KodeWindow.PropertyGrid g, string label, Func<bool> get, Action<bool> set, IEditable ed = null,
            string unit = null, float width = 60, Func<Tone> tone = null) =>
            ed == null
                ? g.Switch(label, get, set, tone: tone)
                : g.Switch(label, get, set, () => ed.Text, s => ed.Text = s, width, unit, Valid(ed), tone);

        /// <summary>label [field] unit on an IEditable (MechJeb's labels often start with spaces: they are trimmed).</summary>
        public static Layout Field(KodeWindow.PropertyGrid g, string label, IEditable ed, string unit, float width = 60) =>
            g.Field(KodeWindow.Loc(label).Trim(), () => ed.Text, s => ed.Text = s, width, unit, Valid(ed));

        static Tone LimitTone(MechJebModuleThrustController t, LimitMode mode) => t.Limiter == mode ? Tone.Good : Tone.Normal;

        /// <summary>
        /// The thrust limiters, one switch row each, the label green while that limiter acts. <paramref name="psg"/>: PSG
        /// ascents set the throttle and electric limits themselves, so those rows hide.
        /// </summary>
        public static KodeWindow.PropertyGrid ThrustLimits(KodeWindow w, UIObject parent, MechJebModuleThrustController t, Func<bool> psg = null,
            bool jets = true)
        {
            KodeWindow.PropertyGrid g = w.Properties(parent);
            Switch(g, "#MechJeb_Ascent_checkbox12", () => t.LimitToPreventOverheats, v => t.LimitToPreventOverheats = v,
                tone: () => LimitTone(t, LimitMode.TEMPERATURE)); //"Prevent engine overheats"
            Switch(g, "#MechJeb_Ascent_checkbox11", () => t.LimitDynamicPressure, v => t.LimitDynamicPressure = v, t.MaxDynamicPressure, "Pa", 55,
                () => LimitTone(t, LimitMode.DYNAMIC_PRESSURE)); //Limit to Max Q
            Switch(g, "#MechJeb_Ascent_checkbox16", () => t.LimitAcceleration, v => t.LimitAcceleration = v, t.MaxAcceleration, "m/s²", 55,
                () => LimitTone(t, LimitMode.ACCELERATION)); //"Limit acceleration to"
            Layout throttle = Switch(g, "#MechJeb_Ascent_checkbox17", () => t.LimitThrottle, v => t.LimitThrottle = v, t.MaxThrottle, "%", 55,
                () => t.Limiter == LimitMode.THROTTLE ? t.MaxThrottle > 0d ? Tone.Good : Tone.Bad : Tone.Normal); //"Limit throttle to"
            Switch(g, "#MechJeb_Ascent_checkbox18", () => t.LimiterMinThrottle, v => t.LimiterMinThrottle = v, t.MinThrottle, "%", 55, () =>
            {
                // RF users should almost always have this set, "stock" users should almost always never have this set
                bool warn = ReflectionUtils.IsLoadedRealFuels ? !t.LimiterMinThrottle || t.MinThrottle <= 0d : t.LimiterMinThrottle && t.MinThrottle > 0d;
                return t.Limiter == LimitMode.MIN_THROTTLE ? Tone.Good : warn ? Tone.Warn : Tone.Normal;
            }); //"Keep limited throttle over"

            // electric: [switch] Electric limit Lo [lo] % Hi [hi] %, the "% Hi [hi] %" in the unit column
            float nextCheck = 0;
            bool running = false;
            Layout electric = Switch(g, "#MechJeb_Ascent_checkbox20", () => t.ElectricThrottle, v => t.ElectricThrottle = v, tone: () =>
            {
                if (t.Limiter == LimitMode.ELECTRIC) return t.ThrottleLimit < 0.001 ? Tone.Bad : Tone.Warn;
                // the part scan behind "engine running" is not worth doing every frame
                if (Time.unscaledTime >= nextCheck)
                {
                    nextCheck = Time.unscaledTime + 0.25f;
                    running = t.ElectricEngineRunning();
                }
                return running ? Tone.Good : Tone.Normal;
            });
            TMPro.TMP_InputField lo = EditFields.Field(w, electric, () => t.ElectricThrottleLo.Text, s => t.ElectricThrottleLo.Text = s, 55,
                () => t.ElectricThrottleLo.IsParsed);
            Layout hi = KodeWindow.Row(electric);
            hi.FlexibleLayout(false, false);
            KodeWindow.Label(hi, "% Hi");
            EditFields.Field(w, hi, () => t.ElectricThrottleHi.Text, s => t.ElectricThrottleHi.Text = s, 40, () => t.ElectricThrottleHi.IsParsed);
            KodeWindow.Label(hi, "%");
            g.Dim(lo.gameObject, hi.gameObject, () => !t.ElectricThrottle);

            if (psg != null)
            {
                w.Visible(throttle.gameObject, () => !psg());
                w.Visible(electric.gameObject, () => !psg());
            }

            if (jets)
                Switch(g, "#MechJeb_Ascent_checkbox13", () => t.LimitToPreventFlameout, v => t.LimitToPreventFlameout = v,
                    tone: () => LimitTone(t, LimitMode.FLAMEOUT)); //"Prevent jet flameout"
            if (ReflectionUtils.IsLoadedRealFuels)
            {
                // does nothing in stock, so it is not shown if RF is not loaded
                Switch(g, "#MechJeb_Ascent_checkbox14", () => t.LimitToPreventUnstableIgnition, v => t.LimitToPreventUnstableIgnition = v,
                    tone: () => LimitTone(t, LimitMode.UNSTABLE_IGNITION)); //"Prevent unstable ignition"
                Switch(g, "#MechJeb_Ascent_checkbox15", () => t.AutoRCSUllaging, v => t.AutoRCSUllaging = v,
                    tone: () => LimitTone(t, LimitMode.AUTO_RCS_ULLAGE)); //"Use RCS to ullage"
            }
            return g;
        }

        /// <summary>StagingController.AutostageSettingsInfoItem: limit, delays, clamp, fairings, hot staging, solids.</summary>
        public static KodeWindow.PropertyGrid AutostageSettings(KodeWindow w, UIObject parent, MechJebModuleStagingController s)
        {
            KodeWindow.PropertyGrid g = w.Properties(parent);
            Field(g, "#MechJeb_Ascent_label42", s.AutostageLimit, "", 40); //"Stop at stage #"
            Field(g, "#MechJeb_NewUI_PreDelay", s.AutostagePreDelay, "s", 50); //Delay before staging
            Field(g, "#MechJeb_NewUI_PostDelay", s.AutostagePostDelay, "s", 50); //Delay after staging
            g.Field(CachedLocalizer.Instance.MechJebAscentLabel44.Trim(), () => s.ClampAutoStageThrustPct.Text, x =>
            {
                s.ClampAutoStageThrustPct.Text = x;
                s.ClampAutostageThrustValue();
            }, 50, "%", () => s.ClampAutoStageThrustPct.IsParsed); //"Clamp AutoStage Thrust "

            g.Note("#MechJeb_Ascent_label38"); //"Stage fairings when:"
            Field(g, s._sFairingMinDynamicPressure, s.FairingMaxDynamicPressure, "kPa", 50);
            Field(g, s._sFairingMinAltitude, s.FairingMinAltitude, "km", 50);
            Field(g, s._sFairingMaxAerothermalFlux, s.FairingMaxAerothermalFlux, "W/m²", 50);

            Switch(g, "#MechJeb_Ascent_hotStaging", () => s.HotStaging, v => s.HotStaging = v); //"Support hotstaging"
            Layout lead = Field(g, s._sLeadTime.TrimEnd(':', ' '), s.HotStagingLeadTime, "s", 50);
            g.Dim(lead.gameObject, null, () => !s.HotStaging);
            Switch(g, "#MechJeb_Ascent_dropSolids", () => s.DropSolids, v => s.DropSolids = v, s.DropSolidsTwrPct, "% rocket accel", 50); //"Drop solids early"
            return g;
        }
    }
}
