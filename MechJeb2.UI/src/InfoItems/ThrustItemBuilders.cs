using System;
using KodeKit;
using KodeUI;
using static MuMech.MechJebModuleThrustController;

namespace MuMech.UI
{
    /// <summary>
    /// Native versions of the MechJebModuleThrustController limiter items, ThrustWindow.Autostage and
    /// WarpController.ControlWarpButton (shared by the Utilities and Warp Helper windows and the custom windows).
    /// </summary>
    public static partial class GeneralItemBuilders
    {
        // [x] label [field] unit, with a toggle label coloured by the limiter state
        static void ToggledField(KodeWindow w, UIObject parent, string key, Func<bool> get, Action<bool> set, EditableDoubleMult ed, string unit, float width,
            Func<KodeWindow.Tone> tone, string tooltipKey = null)
        {
            Layout row = KodeWindow.Row(parent);
            Tooltips.Add(KodeWindow.LabelOf(w.Toggle(row, key, get, set, tone)).gameObject, tooltipKey);
            EditUi.Field(w, row, () => ed, width, label: key);
            KodeWindow.Label(row, unit);
        }

        static void LimitToMaxDynamicPressure(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            ToggledField(w, p, "#MechJeb_Ascent_checkbox11", () => t.LimitDynamicPressure, v => t.LimitDynamicPressure = v,
                t.MaxDynamicPressure, "Pa", 80, LimiterTones.For(t, LimitMode.DYNAMIC_PRESSURE)); //Limit to Max Q

        static void LimitToPreventOverheats(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            w.Toggle(p, "#MechJeb_Ascent_checkbox12", () => t.LimitToPreventOverheats, v => t.LimitToPreventOverheats = v,
                LimiterTones.For(t, LimitMode.TEMPERATURE, OptionRows.GeneralLimitsApply(t))); //"Prevent engine overheats"

        static void LimitToPreventFlameout(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            w.Toggle(p, "#MechJeb_Ascent_checkbox13", () => t.LimitToPreventFlameout, v => t.LimitToPreventFlameout = v,
                LimiterTones.For(t, LimitMode.FLAMEOUT)); //"Prevent jet flameout"

        static void LimitToPreventUnstableIgnition(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            w.Toggle(p, "#MechJeb_Ascent_checkbox14", () => t.LimitToPreventUnstableIgnition, v => t.LimitToPreventUnstableIgnition = v,
                LimiterTones.For(t, LimitMode.UNSTABLE_IGNITION, OptionRows.GeneralLimitsApply(t))); //"Prevent unstable ignition"

        static void AutoRcsUllage(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            w.Toggle(p, "#MechJeb_Ascent_checkbox15", () => t.AutoRCSUllaging, v => t.AutoRCSUllaging = v,
                LimiterTones.For(t, LimitMode.AUTO_RCS_ULLAGE, OptionRows.GeneralLimitsApply(t))); //"Use RCS to ullage"

        static void LimitAcceleration(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            ToggledField(w, p, "#MechJeb_Ascent_checkbox16", () => t.LimitAcceleration, v => t.LimitAcceleration = v,
                t.MaxAcceleration, "m/s²", 30, LimiterTones.For(t, LimitMode.ACCELERATION, OptionRows.GeneralLimitsApply(t))); //"Limit acceleration to"

        static void LimitThrottle(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            ToggledField(w, p, "#MechJeb_Ascent_checkbox17", () => t.LimitThrottle, v => t.LimitThrottle = v,
                t.MaxThrottle, "%", 30, GeneralTone(t, LimiterTones.Throttle)); //"Limit throttle to"

        static void LimiterMinThrottle(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            ToggledField(w, p, "#MechJeb_Ascent_checkbox18", () => t.LimiterMinThrottle, v => t.LimiterMinThrottle = v,
                t.MinThrottle, "%", 30, GeneralTone(t, LimiterTones.MinThrottle), "#MechJeb_Ascent_checkbox18_tooltip"); //"Keep limited throttle over"

        // a tone of the general limits, live while they apply
        static Func<KodeWindow.Tone> GeneralTone(MechJebModuleThrustController t,
            Func<MechJebModuleThrustController, IThrustLimits, Func<bool>, KodeWindow.Tone> tone)
        {
            Func<bool> general = OptionRows.GeneralLimitsApply(t);
            return () => tone(t, t, general);
        }

        static void DifferentialThrottle(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            w.Toggle(p, "#MechJeb_Ascent_checkbox19", () => t.DifferentialThrottle, LimiterTones.SetDifferential(t),
                () => LimiterTones.Differential(t)); //"Differential throttle"

        // the electric limit as in Utilities: two rows of a grid of its own, "Lo" and "Hi" next to their fields,
        // red/yellow while limiting, green while an electric engine runs
        static void LimitElectric(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            OptionRows.ElectricLimit(w, w.Properties(p), t, t, OptionRows.GeneralLimitsApply(t), 30, out _);

        /// <summary>MechJebModuleThrustWindow.Autostage: the "Autostage" toggle (the window is a user of the staging controller).</summary>
        static void Autostage(KodeWindow w, UIObject p, MechJebModuleThrustWindow window)
        {
            UnityEngine.UI.Toggle toggle = w.Toggle(p, "#MechJeb_Utilities_checkbox1", () => window.Core.Staging.Users.Contains(window), v =>
            {
                if (v) window.Core.Staging.Users.Add(window);
                else window.Core.Staging.Users.Remove(window);
                window.autostageSavedState = v;
            });
            // the IMGUI item stored the state on every draw
            w.AddSync(() =>
            {
                if (toggle.gameObject.activeInHierarchy) window.autostageSavedState = window.Core.Staging.Users.Contains(window);
            });
        }

        /// <summary>MechJebModuleWarpController.ControlWarpButton: "Resume MJ Warp" while paused, "Pause MJ Warp" otherwise.</summary>
        static void ControlWarpButton(KodeWindow w, UIObject p, MechJebModuleWarpController warp)
        {
            UIButton resume = KodeWindow.Button(p, "#MechJeb_WarpHelper_button3", () => warp.ResumeWarp(), true); //"Resume MJ Warp"
            w.Visible(resume.gameObject, () => warp.WarpPaused);
            UIButton pause = KodeWindow.Button(p, "#MechJeb_WarpHelper_button4", () => warp.PauseWarp(), true); //"Pause MJ Warp"
            w.Visible(pause.gameObject, () => !warp.WarpPaused);
        }
    }
}
