using KodeKit;
using KodeUI;

namespace MuMech.UI
{
    /// <summary>MechJebModuleSmartRcs ("SmartRcs") in KodeKit, row for row with its WindowGUI.</summary>
    [KodeViewFor(typeof(MechJebModuleSmartRcs))]
    internal class SmartRcsView : KodeView
    {
        readonly MechJebModuleSmartRcs smart;

        public SmartRcsView(MechJebModuleSmartRcs smart) : base(smart) => this.smart = smart;

        protected override float MinWidth => 180;

        MechJebCore Core => smart.Core;

        // RCS is used by an other module (WindowGUI's first branch, including its side effect)
        bool blocked;

        protected override void BeforeSync() => blocked = smart.RcsUsedByOtherModule();

        bool NoTarget() => Core.Target == null || Core.Target.Target == null;

        protected override void BuildContents(KodeWindow w)
        {
            if (KodeWindow.ModernLayout)
            {
                BuildModern(w);
                return;
            }

            UIObject r = w.Root;

            UIButton auto = KodeWindow.Padded(MiscViewKit.ToneButton(w, r, "#MechJeb_SmartRcs_button3", () => { }, () => KodeWindow.Tone.Bad, true)); //"AUTO"
            w.Visible(auto.gameObject, () => blocked);

            UIText choose = KodeWindow.Label(r, "#MechJeb_SmartRcs_label1"); //"Choose a target"
            w.Visible(choose.gameObject, () => !blocked && NoTarget());

            Layout targets = KodeWindow.Panel(r);
            w.Visible(targets.gameObject, () => !blocked && !NoTarget());
            TargetButton(w, targets, "#MechJeb_SmartRcs_button1", MechJebModuleSmartRcs.Target.OFF); //"OFF"
            TargetButton(w, targets, "#MechJeb_SmartRcs_button2", MechJebModuleSmartRcs.Target.ZERO_RVEL); //"ZERO RVEL"

            w.Toggle(r, "#MechJeb_SmartRcs_checkbox2", () => Core.RCS.rcsThrottle, v => Core.RCS.rcsThrottle = v); //" RCS throttle when engines are offline"
            w.Toggle(r, "#MechJeb_SmartRcs_checkbox3", () => Core.RCS.rcsForRotation, v => Core.RCS.rcsForRotation = v); // " Use RCS for rotation"
        }

        // modern layout: the two targets as tiles, switches, and the AUTO status bar (the body stays in view, greyed)
        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;
            Layout body = KodeWindow.Panel(r);

            UIText choose = KodeWindow.Label(body, "#MechJeb_SmartRcs_label1"); //"Choose a target"
            w.Visible(choose.gameObject, NoTarget);
            Layout targets = KodeWindow.UniformGrid(body, 2);
            w.Visible(targets.gameObject, () => !NoTarget());
            Tile(w, targets, "#MechJeb_SmartRcs_button1", MechJebModuleSmartRcs.Target.OFF); //"OFF"
            Tile(w, targets, "#MechJeb_SmartRcs_button2", MechJebModuleSmartRcs.Target.ZERO_RVEL); //"ZERO RVEL"

            w.SwitchRow(body, "#MechJeb_SmartRcs_checkbox2", () => Core.RCS.rcsThrottle, v => Core.RCS.rcsThrottle = v); //" RCS throttle when engines are offline"
            w.SwitchRow(body, "#MechJeb_SmartRcs_checkbox3", () => Core.RCS.rcsForRotation, v => Core.RCS.rcsForRotation = v); // " Use RCS for rotation"

            GuidanceUi.AutoBar(w, r, body.gameObject, "#MechJeb_SmartRcs_button3", () => blocked, () => GuidanceUi.OtherUser(Core.RCS.Users, smart)); //"AUTO"
        }

        void Tile(KodeWindow w, UIObject parent, string key, MechJebModuleSmartRcs.Target bt)
        {
            UIButton b = KodeWindow.Padded(KodeWindow.Button(parent, key, () =>
            {
                smart.target = bt;
                smart.Engage();
            }, true), 6);
            w.BindRole(b, () => smart.target == bt ? ButtonRole.Selected : ButtonRole.Normal);
        }

        void TargetButton(KodeWindow w, UIObject parent, string key, MechJebModuleSmartRcs.Target bt)
        {
            KodeWindow.Padded(MiscViewKit.ToneButton(w, parent, key, () =>
            {
                smart.target = bt;
                smart.Engage();
            }, () => smart.target == bt ? KodeWindow.Tone.Good : KodeWindow.Tone.Normal, true)); // btNormal: padding 8
        }
    }
}
