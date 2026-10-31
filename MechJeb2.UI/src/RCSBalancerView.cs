using System;
using KodeKit;
using KodeUI;

namespace MuMech.UI
{
    /// <summary>MechJebModuleRCSBalancerWindow ("RCS Balancer") in KodeKit, row for row with its WindowGUI.</summary>
    [KodeViewFor(typeof(MechJebModuleRCSBalancerWindow))]
    public class RCSBalancerView : KodeView
    {
        readonly MechJebModuleRCSBalancerWindow window;

        public RCSBalancerView(MechJebModuleRCSBalancerWindow window) : base(window) => this.window = window;

        protected override float MinWidth => 240;

        MechJebModuleRCSBalancer Balancer => window.balancer;

        // the tuning parameters as they were last frame, to detect an edit (IMGUI compares before and after drawing)
        bool haveLast;
        double lastOverdrive, lastScale, lastTorque, lastTranslate, lastWaste;

        protected override void BeforeSync()
        {
            MechJebModuleRCSBalancer b = Balancer;
            if (b == null) return;

            if (b.smartTranslation && haveLast &&
                (lastOverdrive != b.overdrive || lastScale != b.overdriveScale || lastTorque != b.tuningParamFactorTorque ||
                    lastTranslate != b.tuningParamFactorTranslate || lastWaste != b.tuningParamFactorWaste))
            {
                b.UpdateTuningParameters();
            }

            lastOverdrive = b.overdrive;
            lastScale = b.overdriveScale;
            lastTorque = b.tuningParamFactorTorque;
            lastTranslate = b.tuningParamFactorTranslate;
            lastWaste = b.tuningParamFactorWaste;
            haveLast = true;

            window.MaintainBalancerUser();
        }

        static void SimpleBox(KodeWindow w, UIObject parent, string key, Func<EditableDoubleMult> ed, string unit = null) =>
            EditUi.Row(w, parent, key, ed, unit);

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            w.Toggle(r, "#MechJeb_RCSBalancer_checkbox1", () => Balancer.smartTranslation, v => //"Smart translation"
            {
                bool was = Balancer.smartTranslation;
                Balancer.smartTranslation = v;
                if (was != v) window.OnSmartTranslationToggled();
            });

            Layout smart = KodeWindow.Panel(r);
            w.Visible(smart.gameObject, () => Balancer.smartTranslation);

            // Overdrive
            SimpleBox(w, smart, "#MechJeb_RCSBalancer_label1", () => Balancer.overdrive, "%"); //"Overdrive"
            w.Slider(smart, () => (float)Balancer.overdrive.Val, v =>
            {
                double sliderVal = v;
                const int sliderPrecision = 3;
                if (Math.Round(Math.Abs(sliderVal - Balancer.overdrive), sliderPrecision) > 0)
                {
                    double rounded = Math.Round(sliderVal, sliderPrecision);
                    Balancer.overdrive = new EditableDoubleMult(rounded, 0.01);
                }
            }, 0f, 1f);

            KodeWindow.Paragraph(smart, "#MechJeb_RCSBalancer_label2"); //"Overdrive increases power when possible, at the cost of RCS fuel efficiency."

            // Advanced options
            w.Toggle(smart, "#MechJeb_RCSBalancer_checkbox2", () => Balancer.advancedOptions, v => Balancer.advancedOptions = v); //"Advanced options"
            Layout advanced = KodeWindow.Panel(smart);
            w.Visible(advanced.gameObject, () => Balancer.advancedOptions);
            SimpleBox(w, advanced, "#MechJeb_RCSBalancer_label3", () => Balancer.overdriveScale); //"Overdrive scale"
            SimpleBox(w, advanced, "#MechJeb_RCSBalancer_label4", () => Balancer.tuningParamFactorTorque); //"torque factor"
            SimpleBox(w, advanced, "#MechJeb_RCSBalancer_label5", () => Balancer.tuningParamFactorTranslate); //"Translate factor"
            SimpleBox(w, advanced, "#MechJeb_RCSBalancer_label6", () => Balancer.tuningParamFactorWaste); //"Waste factor"
        }
    }
}
