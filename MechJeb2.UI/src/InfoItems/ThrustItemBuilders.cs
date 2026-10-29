using System;
using KodeKit;
using KodeUI;
using UnityEngine;
using static MuMech.MechJebModuleThrustController;

namespace MuMech.UI
{
    /// <summary>
    /// Native versions of the MechJebModuleThrustController limiter items, ThrustWindow.Autostage and
    /// WarpController.ControlWarpButton (shared by the Utilities and Warp Helper windows and the custom windows).
    /// </summary>
    public static partial class GeneralItemBuilders
    {
        static KodeWindow.Tone LimitTone(MechJebModuleThrustController t, LimitMode mode) =>
            t.Limiter == mode ? KodeWindow.Tone.Good : KodeWindow.Tone.Normal;

        // GuiUtils.ToggledTextBox with a toggle label coloured by the limiter state
        static void ToggledField(KodeWindow w, UIObject parent, string key, Func<bool> get, Action<bool> set, EditableDoubleMult ed, string unit, float width,
            Func<KodeWindow.Tone> tone, string tooltipKey = null)
        {
            Layout row = KodeWindow.Row(parent);
            w.Toggle(row, key, get, set, tone);
            if (tooltipKey != null) Tooltips.Add(row.GetComponentInChildren<ToggleLabelClick>().gameObject, tooltipKey);
            EditFields.Field(w, row, () => ed.Text, s => ed.Text = s, width, () => ed.IsParsed);
            KodeWindow.Label(row, unit);
        }

        static bool LiftedOff(MechJebModuleThrustController t)
        {
            Vessel v = t.Vessel;
            return v != null && v.LiftedOff();
        }

        static void LimitToMaxDynamicPressure(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            ToggledField(w, p, "#MechJeb_Ascent_checkbox11", () => t.LimitDynamicPressure, v => t.LimitDynamicPressure = v,
                t.MaxDynamicPressure, "Pa", 80, () => LimitTone(t, LimitMode.DYNAMIC_PRESSURE)); //Limit to Max Q

        static void LimitToPreventOverheats(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            w.Toggle(p, "#MechJeb_Ascent_checkbox12", () => t.LimitToPreventOverheats, v => t.LimitToPreventOverheats = v,
                () => LimitTone(t, LimitMode.TEMPERATURE)); //"Prevent engine overheats"

        static void LimitToPreventFlameout(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            w.Toggle(p, "#MechJeb_Ascent_checkbox13", () => t.LimitToPreventFlameout, v => t.LimitToPreventFlameout = v,
                () => LimitTone(t, LimitMode.FLAMEOUT)); //"Prevent jet flameout"

        static void LimitToPreventUnstableIgnition(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            w.Toggle(p, "#MechJeb_Ascent_checkbox14", () => t.LimitToPreventUnstableIgnition, v => t.LimitToPreventUnstableIgnition = v,
                () => LimitTone(t, LimitMode.UNSTABLE_IGNITION)); //"Prevent unstable ignition"

        static void AutoRcsUllage(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            w.Toggle(p, "#MechJeb_Ascent_checkbox15", () => t.AutoRCSUllaging, v => t.AutoRCSUllaging = v,
                () => LimitTone(t, LimitMode.AUTO_RCS_ULLAGE)); //"Use RCS to ullage"

        static void LimitAcceleration(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            ToggledField(w, p, "#MechJeb_Ascent_checkbox16", () => t.LimitAcceleration, v => t.LimitAcceleration = v,
                t.MaxAcceleration, "m/s²", 30, () => LimitTone(t, LimitMode.ACCELERATION)); //"Limit acceleration to"

        static void LimitThrottle(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            ToggledField(w, p, "#MechJeb_Ascent_checkbox17", () => t.LimitThrottle, v => t.LimitThrottle = v,
                t.MaxThrottle, "%", 30,
                () => t.Limiter == LimitMode.THROTTLE ? t.MaxThrottle > 0d ? KodeWindow.Tone.Good : KodeWindow.Tone.Bad : KodeWindow.Tone.Normal); //"Limit throttle to"

        static void LimiterMinThrottle(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            ToggledField(w, p, "#MechJeb_Ascent_checkbox18", () => t.LimiterMinThrottle, v => t.LimiterMinThrottle = v,
                t.MinThrottle, "%", 30, () =>
                {
                    // RF users should almost always have this set, "stock" users should almost always never have this set
                    bool warn = MechJebLibBindings.ReflectionUtils.IsLoadedRealFuels
                        ? !t.LimiterMinThrottle || t.MinThrottle <= 0d
                        : t.LimiterMinThrottle && t.MinThrottle > 0d;
                    return t.Limiter == LimitMode.MIN_THROTTLE ? KodeWindow.Tone.Good : warn ? KodeWindow.Tone.Warn : KodeWindow.Tone.Normal;
                }, "#MechJeb_Ascent_checkbox18_tooltip"); //"Keep limited throttle over"

        static void DifferentialThrottle(KodeWindow w, UIObject p, MechJebModuleThrustController t) =>
            w.Toggle(p, "#MechJeb_Ascent_checkbox19", () => t.DifferentialThrottle, v =>
                {
                    bool old = t.DifferentialThrottle;
                    t.DifferentialThrottle = v;
                    if (old && !v) t.DisableDifferentialThrottle();
                },
                () => t.DifferentialThrottle && LiftedOff(t)
                    ? t.DifferentialThrottleSuccess == DifferentialThrottleStatus.SUCCESS ? KodeWindow.Tone.Good : KodeWindow.Tone.Warn
                    : KodeWindow.Tone.Normal); //"Differential throttle"

        // [x] "Electric limit Lo" [lo] % Hi [hi] %, red/yellow while limiting, green while an electric engine runs
        static void LimitElectric(KodeWindow w, UIObject p, MechJebModuleThrustController t)
        {
            Layout electric = KodeWindow.Row(p);
            // the part scan behind "engine running" is not worth doing every frame
            float nextCheck = 0;
            bool running = false;
            w.Toggle(electric, "#MechJeb_Ascent_checkbox20", () => t.ElectricThrottle, v => t.ElectricThrottle = v, () =>
            {
                if (t.Limiter == LimitMode.ELECTRIC) return t.ThrottleLimit < 0.001 ? KodeWindow.Tone.Bad : KodeWindow.Tone.Warn;
                if (Time.unscaledTime >= nextCheck)
                {
                    nextCheck = Time.unscaledTime + 0.25f;
                    running = t.ElectricEngineRunning();
                }

                return running ? KodeWindow.Tone.Good : KodeWindow.Tone.Normal;
            });
            EditFields.Field(w, electric, () => t.ElectricThrottleLo.Text, s => t.ElectricThrottleLo.Text = s, 30, () => t.ElectricThrottleLo.IsParsed);
            KodeWindow.Label(electric, "% Hi");
            EditFields.Field(w, electric, () => t.ElectricThrottleHi.Text, s => t.ElectricThrottleHi.Text = s, 30, () => t.ElectricThrottleHi.IsParsed);
            KodeWindow.Label(electric, "%");
        }

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
