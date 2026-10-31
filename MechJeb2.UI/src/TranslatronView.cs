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
            if (speedText == null || !v.Equals(shownSpeed))
            {
                shownSpeed = v;
                speedText = Localizer.Format("#MechJeb_Trans_current_spd") + v.ToSI() + "m/s";
            }

            return speedText;
        }

        protected override void BuildContents(KodeWindow w)
        {
            if (KodeWindow.ModernLayout)
            {
                BuildModern(w);
                return;
            }

            UIObject r = w.Root;

            // other modules use the throttle: IMGUI shows one red, inert "auto" button
            // buttonStyle: padding 8
            UIButton auto = KodeWindow.Padded(MiscViewKit.ToneButton(w, r, "#MechJeb_Trans_auto", () => { }, () => KodeWindow.Tone.Bad, true));
            w.Visible(auto.gameObject, AutoMode);

            Layout manual = KodeWindow.Panel(r);
            w.Visible(manual.gameObject, () => !AutoMode());

            // SelectionGrid(mode, texts, 2 columns)
            Layout gridTop = KodeWindow.EqualHeights(KodeWindow.Row(manual));
            w.SelectionGrid(gridTop, new[] { "#MechJeb_Translatron_off", "#MechJeb_Translatron_KEEP_OBT" },
                () => Mode < 2 ? Mode : -1, i => tr.SetMode((MechJebModuleThrustController.TMode)i), 8);
            Layout gridBottom = KodeWindow.EqualHeights(KodeWindow.Row(manual));
            w.SelectionGrid(gridBottom, new[] { "#MechJeb_Translatron_KEEP_SURF", "#MechJeb_Translatron_KEEP_VERT" },
                () => Mode >= 2 ? Mode - 2 : -1, i => tr.SetMode((MechJebModuleThrustController.TMode)(i + 2)), 8);

            w.Toggle(manual, "#MechJeb_Trans_kill_h", () => Thrust.TransKillH, v => Thrust.TransKillH = v);

            // change by 5 if the mod_key is held down, else by 1
            float Step() => GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
            void ApplySpeed() => Thrust.TransSpdAct = (float)tr.trans_spd.Val;

            Layout speed = KodeWindow.Row(manual);
            KodeWindow.Label(speed, "#MechJeb_Trans_spd");
            EditUi.Field(w, speed, () => tr.trans_spd, 37);
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

            KodeWindow.Padded(KodeWindow.Button(manual, KodeWindow.Loc("#MechJeb_Trans_spd_act") + ":", ApplySpeed, true));

            UIText current = w.BoundLabel(r, Localizer.Format("#MechJeb_Trans_current_spd") + "-000.0km/s", SpeedText, true);
            w.Visible(current, () => Thrust.Tmode != MechJebModuleThrustController.TMode.OFF);

            UIText automation = KodeWindow.Label(r, "Automation", true);
            automation.tmpText.alignment = TextAlignmentOptions.Center;

            string panic = KodeWindow.Loc("#MechJeb_Trans_PANIC"), noPanic = KodeWindow.Loc("#MechJeb_Trans_NOPANIC");
            KodeWindow.Padded(MiscViewKit.BoundButton(w, r, () => tr.Aborting ? noPanic : panic, tr.PanicSwitch,
                () => tr.Aborting ? KodeWindow.Tone.Bad : KodeWindow.Tone.Good, true));
        }

        static readonly string[] modeKeys =
            { "#MechJeb_Translatron_off", "#MechJeb_Translatron_KEEP_OBT", "#MechJeb_Translatron_KEEP_SURF", "#MechJeb_Translatron_KEEP_VERT" };

        // modern layout: the modes as one choice, a speed stepper, PANIC as a destructive button, the AUTO status bar
        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;
            Layout body = KodeWindow.Panel(r);

            w.Segmented(body, modeKeys, () => Mode, i => tr.SetMode((MechJebModuleThrustController.TMode)i));
            w.SwitchRow(body, "#MechJeb_Trans_kill_h", () => Thrust.TransKillH, v => Thrust.TransKillH = v);

            void ApplySpeed() => Thrust.TransSpdAct = (float)tr.trans_spd.Val;
            Layout speed = KodeWindow.Row(body);
            KodeWindow.Label(speed, "#MechJeb_Trans_spd", true);
            w.Stepper(speed, () => tr.trans_spd.Text, s => tr.trans_spd.Text = s, d =>
            {
                tr.trans_spd += d;
                ApplySpeed();
            }, () => 1, 45, () => tr.trans_spd.IsParsed);
            KodeWindow.Label(speed, "m/s");
            KodeWindow.Button(body, "#MechJeb_Trans_spd_act", ApplySpeed, ButtonRole.Primary, true);

            UIText current = w.BoundLabel(body, Localizer.Format("#MechJeb_Trans_current_spd") + "-000.0km/s", SpeedText, true);
            w.Visible(current, () => Thrust.Tmode != MechJebModuleThrustController.TMode.OFF);

            KodeWindow.Header(body, "Automation");
            string panic = KodeWindow.Loc("#MechJeb_Trans_PANIC"), noPanic = KodeWindow.Loc("#MechJeb_Trans_NOPANIC");
            UIButton p = KodeWindow.Padded(MiscViewKit.BoundButton(w, body, () => tr.Aborting ? noPanic : panic, tr.PanicSwitch, null, true), 6);
            w.BindRole(p, () => tr.Aborting ? ButtonRole.Selected : ButtonRole.Danger);

            GuidanceUi.AutoBar(w, r, body.gameObject, "#MechJeb_Trans_auto", AutoMode, () => GuidanceUi.OtherUser(Thrust.Users, tr));
        }
    }
}
