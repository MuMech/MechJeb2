using KodeKit;
using KodeUI;
using KSP.Localization;
using TMPro;
using static MechJebLib.Utils.Statics;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleTranslatron ("Translatron") in KodeKit.
    /// While an other module drives the throttle controller the window only shows the red "auto" button.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleTranslatronWindow))]
    public class TranslatronView : KodeView
    {
        readonly MechJebModuleTranslatron tr;

        public TranslatronView(MechJebModuleTranslatronWindow window) : base(window) => tr = window.Translatron;

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

        static readonly string[] modeKeys =
            { "#MechJeb_Translatron_off", "#MechJeb_Translatron_KEEP_OBT", "#MechJeb_Translatron_KEEP_SURF", "#MechJeb_Translatron_KEEP_VERT" };

        // The modes as one choice, a speed stepper, PANIC as a destructive button, the AUTO status bar
        protected override void BuildContents(KodeWindow w)
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
