using System;
using UnityEngine;
using KSP.Localization;


namespace MuMech
{
    public class MechJebModuleRCSBalancerWindow : DisplayModule
    {
        public MechJebModuleRCSBalancer balancer;

        public override void OnStart(PartModule.StartState state)
        {
            balancer = core.GetComputerModule<MechJebModuleRCSBalancer>();

            if (balancer.smartTranslation)
            {
                balancer.users.Add(this);
            }

            base.OnStart(state);
        }

        private void SimpleTextInfo(string left, string right)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(left, GUILayout.ExpandWidth(true));
            GUILayout.Label(right, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            bool wasEnabled = balancer.smartTranslation;

            GUILayout.BeginHorizontal();
            balancer.smartTranslation = GUILayout.Toggle(balancer.smartTranslation, Localizer.Format("#MechJeb_RCSBalancer_checkbox1"), GUILayout.Width(130));//"Smart translation"
            GUILayout.EndHorizontal();

            if (wasEnabled != balancer.smartTranslation)
            {
                balancer.ResetThrusterForces();

                if (balancer.smartTranslation)
                {
                    balancer.users.Add(this);
                }
                else
                {
                    balancer.users.Remove(this);
                }
            }

            if (balancer.smartTranslation)
            {
                // Overdrive
                double oldOverdrive = balancer.overdrive;
                double oldOverdriveScale = balancer.overdriveScale;
                double oldFactorTorque = balancer.tuningParamFactorTorque;
                double oldFactorTranslate = balancer.tuningParamFactorTranslate;
                double oldFactorWaste = balancer.tuningParamFactorWaste;

                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RCSBalancer_label1"), balancer.overdrive, "%");//"Overdrive"

                double sliderVal = GUILayout.HorizontalSlider((float)balancer.overdrive, 0.0F, 1.0F);
                const int sliderPrecision = 3;
                if (Math.Round(Math.Abs(sliderVal - oldOverdrive), sliderPrecision) > 0)
                {
                    double rounded = Math.Round(sliderVal, sliderPrecision);
                    balancer.overdrive = new EditableDoubleMult(rounded, 0.01);
                }

                GUILayout.Label(Localizer.Format("#MechJeb_RCSBalancer_label2"));//"Overdrive increases power when possible, at the cost of RCS fuel efficiency."

                // Advanced options
                balancer.advancedOptions = GUILayout.Toggle(balancer.advancedOptions, Localizer.Format("#MechJeb_RCSBalancer_checkbox2"));//"Advanced options"
                if (balancer.advancedOptions)
                {
                    // This doesn't work properly, and it might not even be needed.
                    //balancer.smartRotation = GUILayout.Toggle(balancer.smartRotation, "Smart rotation");

                    GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RCSBalancer_label3"), balancer.overdriveScale);//"Overdrive scale"
                    GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RCSBalancer_label4"), balancer.tuningParamFactorTorque);//"torque factor"
                    GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RCSBalancer_label5"), balancer.tuningParamFactorTranslate);//"Translate factor"
                    GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RCSBalancer_label6"), balancer.tuningParamFactorWaste);//"Waste factor"
                }

                // Apply tuning parameters.
                if (oldOverdrive != balancer.overdrive
                    || oldOverdriveScale != balancer.overdriveScale
                    || oldFactorTorque != balancer.tuningParamFactorTorque
                    || oldFactorTranslate != balancer.tuningParamFactorTranslate
                    || oldFactorWaste != balancer.tuningParamFactorWaste)
                {
                    balancer.UpdateTuningParameters();
                }
            }

            if (balancer.smartTranslation)
            {
                balancer.users.Add(this);
            }
            else
            {
                balancer.users.Remove(this);
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(240), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_RCSBalancer_title");//"RCS Balancer"
        }

        public MechJebModuleRCSBalancerWindow(MechJebCore core) : base(core) { }
    }
}
