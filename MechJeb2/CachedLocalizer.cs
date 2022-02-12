using System.Collections;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class CachedLocalizer : MonoBehaviour
    {
        public static CachedLocalizer Instance { get; private set; }

        public string MechJeb_Ascent_button1, MechJeb_Ascent_button2, MechJeb_Ascent_button3, MechJeb_Ascent_button4, MechJeb_Ascent_button5;
        public string MechJeb_Ascent_button6, MechJeb_Ascent_button7, MechJeb_Ascent_button8, MechJeb_Ascent_button9, MechJeb_Ascent_button10;
        public string MechJeb_Ascent_button11, MechJeb_Ascent_button12, MechJeb_Ascent_button13, MechJeb_Ascent_button14, MechJeb_Ascent_button15;
        public string MechJeb_Ascent_button17;
        public string MechJeb_Ascent_label1, MechJeb_Ascent_label2, MechJeb_Ascent_label3, MechJeb_Ascent_label4, MechJeb_Ascent_label5;
        public string MechJeb_Ascent_label6, MechJeb_Ascent_label7, MechJeb_Ascent_label8, MechJeb_Ascent_label9, MechJeb_Ascent_label10;
        public string MechJeb_Ascent_label11, MechJeb_Ascent_label12, MechJeb_Ascent_label13, MechJeb_Ascent_label14, MechJeb_Ascent_label15;
        public string MechJeb_Ascent_label16, MechJeb_Ascent_label17, MechJeb_Ascent_label18, MechJeb_Ascent_label19, MechJeb_Ascent_label20;
        public string MechJeb_Ascent_label21, MechJeb_Ascent_label22, MechJeb_Ascent_label23, MechJeb_Ascent_label24, MechJeb_Ascent_label25;
        public string MechJeb_Ascent_label26, MechJeb_Ascent_label27, MechJeb_Ascent_label28, MechJeb_Ascent_label29, MechJeb_Ascent_label30;
        public string MechJeb_Ascent_label31, MechJeb_Ascent_label32, MechJeb_Ascent_label33, MechJeb_Ascent_label34, MechJeb_Ascent_label35;
        public string MechJeb_Ascent_label36, MechJeb_Ascent_label37;
        public string MechJeb_Ascent_attachAlt, MechJeb_Ascent_warnAttachAltHigh, MechJeb_Ascent_warnAttachAltLow;
        public string MechJeb_Ascent_LaunchToTargetLan, MechJeb_Ascent_LaunchToLan, MechJeb_Ascent_LaunchingToTargetLAN, MechJeb_Ascent_LaunchingToManualLAN;
        public string MechJeb_Ascent_msg2, MechJeb_Ascent_msg3;

        public string MechJeb_Ascent_checkbox2, MechJeb_Ascent_checkbox3, MechJeb_Ascent_checkbox4, MechJeb_Ascent_checkbox5;
        public string MechJeb_Ascent_checkbox6, MechJeb_Ascent_checkbox7, MechJeb_Ascent_checkbox8, MechJeb_Ascent_checkbox9, MechJeb_Ascent_checkbox10;
        public string MechJeb_Ascent_checkbox11, MechJeb_Ascent_checkbox12, MechJeb_Ascent_checkbox13, MechJeb_Ascent_checkbox14, MechJeb_Ascent_checkbox15;

        public string MechJeb_Ascent_checkbox16, MechJeb_Ascent_checkbox17, MechJeb_Ascent_checkbox18, MechJeb_Ascent_checkbox19, MechJeb_Ascent_checkbox20;
        public string MechJeb_NavBallGuidance_btn1, MechJeb_NavBallGuidance_btn2;

        public string MechJeb_Ascent_title, MechJeb_WindowEd_title;
        public string MechJeb_WindowEd_CustomInfoWindow_Label1;

        private static WaitForSeconds _WaitForSeconds;
        public static void Bootstrap()
        {
            if (Instance != null) return;
            GameObject go = new GameObject("MechJeb.CachedLocalizer");
            Instance = go.AddComponent<CachedLocalizer>();
        }

        public void OnDestroy() => Instance = null;

        public void Start()
        {
            _WaitForSeconds = new WaitForSeconds(5);
            StartCoroutine(UpdateCachedStrings());
        }

        private IEnumerator UpdateCachedStrings()
        {
            while (true)
            {
                MechJeb_Ascent_button1 = Localizer.Format("#MechJeb_Ascent_button1");
                MechJeb_Ascent_button2 = Localizer.Format("#MechJeb_Ascent_button2");
                MechJeb_Ascent_button3 = Localizer.Format("#MechJeb_Ascent_button3");
                MechJeb_Ascent_button4 = Localizer.Format("#MechJeb_Ascent_button4");
                MechJeb_Ascent_button5 = Localizer.Format("#MechJeb_Ascent_button5");
                MechJeb_Ascent_button6 = Localizer.Format("#MechJeb_Ascent_button6");
                MechJeb_Ascent_button7 = Localizer.Format("#MechJeb_Ascent_button7");
                MechJeb_Ascent_button8 = Localizer.Format("#MechJeb_Ascent_button8");
                MechJeb_Ascent_button9 = Localizer.Format("#MechJeb_Ascent_button9");
                MechJeb_Ascent_button10 = Localizer.Format("#MechJeb_Ascent_button10");
                MechJeb_Ascent_button11 = Localizer.Format("#MechJeb_Ascent_button11");
                MechJeb_Ascent_button12 = Localizer.Format("#MechJeb_Ascent_button12");
                MechJeb_Ascent_button13 = Localizer.Format("#MechJeb_Ascent_button13");
                MechJeb_Ascent_button14 = Localizer.Format("#MechJeb_Ascent_button14");
                MechJeb_Ascent_button15 = Localizer.Format("#MechJeb_Ascent_button15");
                MechJeb_Ascent_button17 = Localizer.Format("#MechJeb_Ascent_button17");

                MechJeb_Ascent_label1 = Localizer.Format("#MechJeb_Ascent_label1");
                MechJeb_Ascent_label2 = Localizer.Format("#MechJeb_Ascent_label2");
                MechJeb_Ascent_label3 = Localizer.Format("#MechJeb_Ascent_label3");
                MechJeb_Ascent_label4 = Localizer.Format("#MechJeb_Ascent_label4");
                MechJeb_Ascent_label5 = Localizer.Format("#MechJeb_Ascent_label5");
                MechJeb_Ascent_label6 = Localizer.Format("#MechJeb_Ascent_label6");
                //MechJeb_Ascent_label7 = Localizer.Format("#MechJeb_Ascent_label7");
                MechJeb_Ascent_label8 = Localizer.Format("#MechJeb_Ascent_label8");
                MechJeb_Ascent_label9 = Localizer.Format("#MechJeb_Ascent_label9");
                MechJeb_Ascent_label10 = Localizer.Format("#MechJeb_Ascent_label10");
                MechJeb_Ascent_label11 = Localizer.Format("#MechJeb_Ascent_label11");
                MechJeb_Ascent_label12 = Localizer.Format("#MechJeb_Ascent_label12");
                MechJeb_Ascent_label13 = Localizer.Format("#MechJeb_Ascent_label13");
                MechJeb_Ascent_label14 = Localizer.Format("#MechJeb_Ascent_label14");
                MechJeb_Ascent_label15 = Localizer.Format("#MechJeb_Ascent_label15");
                MechJeb_Ascent_label16 = Localizer.Format("#MechJeb_Ascent_label16");
                MechJeb_Ascent_label17 = Localizer.Format("#MechJeb_Ascent_label17");
                MechJeb_Ascent_label18 = Localizer.Format("#MechJeb_Ascent_label18");
                MechJeb_Ascent_label19 = Localizer.Format("#MechJeb_Ascent_label19");
                MechJeb_Ascent_label20 = Localizer.Format("#MechJeb_Ascent_label20");
                MechJeb_Ascent_label21 = Localizer.Format("#MechJeb_Ascent_label21");
                MechJeb_Ascent_label22 = Localizer.Format("#MechJeb_Ascent_label22");
                MechJeb_Ascent_label23 = Localizer.Format("#MechJeb_Ascent_label23");
                MechJeb_Ascent_label24 = Localizer.Format("#MechJeb_Ascent_label24");
                MechJeb_Ascent_label25 = Localizer.Format("#MechJeb_Ascent_label25");
                MechJeb_Ascent_label26 = Localizer.Format("#MechJeb_Ascent_label26");
                MechJeb_Ascent_label27 = Localizer.Format("#MechJeb_Ascent_label27");
                MechJeb_Ascent_label28 = Localizer.Format("#MechJeb_Ascent_label28");
                MechJeb_Ascent_label29 = Localizer.Format("#MechJeb_Ascent_label29");
                MechJeb_Ascent_label30 = Localizer.Format("#MechJeb_Ascent_label30");
                MechJeb_Ascent_label31 = Localizer.Format("#MechJeb_Ascent_label31");
                MechJeb_Ascent_label32 = Localizer.Format("#MechJeb_Ascent_label32");
                MechJeb_Ascent_label33 = Localizer.Format("#MechJeb_Ascent_label33");
                MechJeb_Ascent_label34 = Localizer.Format("#MechJeb_Ascent_label34");
                MechJeb_Ascent_label35 = Localizer.Format("#MechJeb_Ascent_label35");
                MechJeb_Ascent_label36 = Localizer.Format("#MechJeb_Ascent_label36");
                MechJeb_Ascent_label37 = Localizer.Format("#MechJeb_Ascent_label37");

                MechJeb_Ascent_checkbox2 = Localizer.Format("#MechJeb_Ascent_checkbox2");
                MechJeb_Ascent_checkbox3 = Localizer.Format("#MechJeb_Ascent_checkbox3");
                MechJeb_Ascent_checkbox4 = Localizer.Format("#MechJeb_Ascent_checkbox4");
                MechJeb_Ascent_checkbox5 = Localizer.Format("#MechJeb_Ascent_checkbox5");
                MechJeb_Ascent_checkbox6 = Localizer.Format("#MechJeb_Ascent_checkbox6");
                MechJeb_Ascent_checkbox7 = Localizer.Format("#MechJeb_Ascent_checkbox7");
                MechJeb_Ascent_checkbox8 = Localizer.Format("#MechJeb_Ascent_checkbox8");
                MechJeb_Ascent_checkbox9 = Localizer.Format("#MechJeb_Ascent_checkbox9");
                MechJeb_Ascent_checkbox10 = Localizer.Format("#MechJeb_Ascent_checkbox10");
                MechJeb_Ascent_checkbox11 = Localizer.Format("#MechJeb_Ascent_checkbox11");
                MechJeb_Ascent_checkbox12 = Localizer.Format("#MechJeb_Ascent_checkbox12");
                MechJeb_Ascent_checkbox13 = Localizer.Format("#MechJeb_Ascent_checkbox13");
                MechJeb_Ascent_checkbox14 = Localizer.Format("#MechJeb_Ascent_checkbox14");
                MechJeb_Ascent_checkbox15 = Localizer.Format("#MechJeb_Ascent_checkbox15");
                MechJeb_Ascent_checkbox16 = Localizer.Format("#MechJeb_Ascent_checkbox16");
                MechJeb_Ascent_checkbox17 = Localizer.Format("#MechJeb_Ascent_checkbox17");
                MechJeb_Ascent_checkbox18 = Localizer.Format("#MechJeb_Ascent_checkbox18");
                MechJeb_Ascent_checkbox19 = Localizer.Format("#MechJeb_Ascent_checkbox19");
                MechJeb_Ascent_checkbox20 = Localizer.Format("#MechJeb_Ascent_checkbox20");


                MechJeb_Ascent_attachAlt = Localizer.Format("#MechJeb_Ascent_attachAlt");
                MechJeb_Ascent_warnAttachAltHigh = Localizer.Format("#MechJeb_Ascent_warnAttachAltHigh");
                MechJeb_Ascent_warnAttachAltLow = Localizer.Format("#MechJeb_Ascent_warnAttachAltLow");
                MechJeb_Ascent_LaunchToTargetLan = Localizer.Format("#MechJeb_Ascent_LaunchToTargetLan");
                MechJeb_Ascent_LaunchToLan = Localizer.Format("#MechJeb_Ascent_LaunchToLan");
                MechJeb_Ascent_LaunchingToTargetLAN = Localizer.Format("#MechJeb_Ascent_LaunchingToTargetLAN");
                MechJeb_Ascent_LaunchingToManualLAN = Localizer.Format("#MechJeb_Ascent_LaunchingToManualLAN");

                MechJeb_Ascent_msg2 = Localizer.Format("#MechJeb_Ascent_msg2");
                MechJeb_Ascent_msg3 = Localizer.Format("#MechJeb_Ascent_msg3");

                MechJeb_NavBallGuidance_btn1 = Localizer.Format("#MechJeb_NavBallGuidance_btn1");
                MechJeb_NavBallGuidance_btn2 = Localizer.Format("#MechJeb_NavBallGuidance_btn2");

                MechJeb_Ascent_title = Localizer.Format("#MechJeb_Ascent_title");
                MechJeb_WindowEd_title = Localizer.Format("#MechJeb_WindowEd_title");
                MechJeb_WindowEd_CustomInfoWindow_Label1 = Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Label1");

                yield return _WaitForSeconds;
            }
        }
    }
}
