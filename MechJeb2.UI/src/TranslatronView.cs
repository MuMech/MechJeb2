using KodeKit;
using KodeUI;
using KSP.Localization;
using TMPro;
using static MechJebLib.Utils.Statics;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleTranslatron ("Translatron") in KodeKit, row for row with its WindowGUI.
    /// While an other module drives the throttle controller the window only shows the red "auto" button.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleTranslatron))]
    public class TranslatronView : KodeView
    {
        readonly MechJebModuleTranslatron tr;

        public TranslatronView(MechJebModuleTranslatron tr) : base(tr) => this.tr = tr;

        protected override float MinWidth => 130;

        MechJebModuleThrustController Thrust => tr.Core.Thrust;

        bool AutoMode() => Thrust != null && Thrust.Users.Count > 1 && !Thrust.Users.Contains(tr);

        int Mode => (int)Thrust.Tmode;

        // "Current speed: <speed>m/s", rebuilt only when the speed changes
        float shownSpeed = float.NaN;
        string speedText;

        string SpeedText()
        {
            float v = Thrust.TransSpdAct;
            if (speedText == null || v != shownSpeed)
            {
                shownSpeed = v;
                speedText = Localizer.Format("#MechJeb_Trans_current_spd") + v.ToSI() + "m/s";
            }

            return speedText;
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            // other modules use the throttle: IMGUI shows one red, inert "auto" button
            UIButton auto = MiscViewKit.ToneButton(w, r, "#MechJeb_Trans_auto", () => { }, () => KodeWindow.Tone.Bad, true);
            w.Visible(auto.gameObject, AutoMode);

            Layout manual = KodeWindow.Panel(r);
            w.Visible(manual.gameObject, () => !AutoMode());

            // SelectionGrid(mode, texts, 2 columns)
            Layout gridTop = KodeWindow.Row(manual);
            w.SelectionGrid(gridTop, new[] { "#MechJeb_Translatron_off", "#MechJeb_Translatron_KEEP_OBT" },
                () => Mode < 2 ? Mode : -1, i => tr.SetMode((MechJebModuleThrustController.TMode)i));
            Layout gridBottom = KodeWindow.Row(manual);
            w.SelectionGrid(gridBottom, new[] { "#MechJeb_Translatron_KEEP_SURF", "#MechJeb_Translatron_KEEP_VERT" },
                () => Mode >= 2 ? Mode - 2 : -1, i => tr.SetMode((MechJebModuleThrustController.TMode)(i + 2)));

            w.Toggle(manual, "#MechJeb_Trans_kill_h", () => Thrust.TransKillH, v => Thrust.TransKillH = v);

            // change by 5 if the mod_key is held down, else by 1
            float Step() => GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
            void ApplySpeed() => Thrust.TransSpdAct = (float)tr.trans_spd.Val;

            Layout speed = KodeWindow.Row(manual);
            KodeWindow.Label(speed, "#MechJeb_Trans_spd");
            EditFields.Field(w, speed, () => tr.trans_spd.Text, s => tr.trans_spd.Text = s, 37, () => tr.trans_spd.IsParsed);
            KodeWindow.Button(speed, "-", () =>
            {
                tr.trans_spd -= Step();
                ApplySpeed();
            });
            KodeWindow.Button(speed, "0", () =>
            {
                tr.trans_spd = 0;
                ApplySpeed();
            });
            KodeWindow.Button(speed, "+", () =>
            {
                tr.trans_spd += Step();
                ApplySpeed();
            });

            KodeWindow.Button(manual, KodeWindow.Loc("#MechJeb_Trans_spd_act") + ":", ApplySpeed, true);

            UIText current = MiscViewKit.LiveBound(w, r, Localizer.Format("#MechJeb_Trans_current_spd") + "-000.0km/s", SpeedText, true);
            w.Visible(current.transform.parent.gameObject, () => Thrust.Tmode != MechJebModuleThrustController.TMode.OFF);

            UIText automation = KodeWindow.Label(r, "Automation", true);
            automation.tmpText.alignment = TextAlignmentOptions.Center;

            string panic = KodeWindow.Loc("#MechJeb_Trans_PANIC"), noPanic = KodeWindow.Loc("#MechJeb_Trans_NOPANIC");
            MiscViewKit.BoundButton(w, r, () => tr.Aborting ? noPanic : panic, tr.PanicSwitch,
                () => tr.Aborting ? KodeWindow.Tone.Bad : KodeWindow.Tone.Good, true);
        }
    }
}
