﻿using System.Collections;
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
        public string MechJeb_Ascent_label36, MechJeb_Ascent_label37, MechJeb_Ascent_label38, MechJeb_Ascent_label39, MechJeb_Ascent_label40;
        public string MechJeb_Ascent_label41, MechJeb_Ascent_label42, MechJeb_Ascent_label44;
        public string MechJeb_Ascent_attachAlt, MechJeb_Ascent_warnAttachAltHigh, MechJeb_Ascent_warnAttachAltLow;
        public string MechJeb_Ascent_LaunchToTargetLan, MechJeb_Ascent_LaunchToLan, MechJeb_Ascent_LaunchingToTargetLAN, MechJeb_Ascent_LaunchingToManualLAN;
        public string MechJeb_Ascent_msg2, MechJeb_Ascent_msg3;
        public string MechJeb_Ascent_hotStaging, MechJeb_Ascent_dropSolids, MechJeb_Ascent_leadTime;

        public string MechJeb_Ascent_checkbox2, MechJeb_Ascent_checkbox3, MechJeb_Ascent_checkbox4, MechJeb_Ascent_checkbox5;
        public string MechJeb_Ascent_checkbox6, MechJeb_Ascent_checkbox7, MechJeb_Ascent_checkbox8, MechJeb_Ascent_checkbox9, MechJeb_Ascent_checkbox10;
        public string MechJeb_Ascent_checkbox11, MechJeb_Ascent_checkbox12, MechJeb_Ascent_checkbox13, MechJeb_Ascent_checkbox14, MechJeb_Ascent_checkbox15;

        public string MechJeb_Ascent_status9, MechJeb_Ascent_status10;
        public string MechJeb_Ascent_status11;

        public string MechJeb_Ascent_checkbox16, MechJeb_Ascent_checkbox17, MechJeb_Ascent_checkbox18, MechJeb_Ascent_checkbox19, MechJeb_Ascent_checkbox20;
        public string MechJeb_NavBallGuidance_btn1, MechJeb_NavBallGuidance_btn2;

        public string MechJeb_InfoItems_UnlimitedText;
        public string MechJeb_InfoItems_label1;
        public string MechJeb_InfoItems_showEmpty, MechJeb_InfoItems_hideEmpty;
        public string MechJeb_InfoItems_button5;
        public string MechJeb_InfoItems_button6;
        public string MechJeb_InfoItems_StatsColumn0;
        public string MechJeb_InfoItems_StatsColumn1, MechJeb_InfoItems_StatsColumn2, MechJeb_InfoItems_StatsColumn3, MechJeb_InfoItems_StatsColumn4, MechJeb_InfoItems_StatsColumn5;
        public string MechJeb_InfoItems_StatsColumn6, MechJeb_InfoItems_StatsColumn7, MechJeb_InfoItems_StatsColumn8, MechJeb_InfoItems_StatsColumn9, MechJeb_InfoItems_StatsColumn10;
        public string MechJeb_InfoItems_StatsColumn11, MechJeb_InfoItems_StatsColumn12, MechJeb_InfoItems_StatsColumn13;

        public string MechJeb_Ascent_title, MechJeb_WindowEd_title;
        public string MechJeb_WindowEd_CustomInfoWindow_Label1;

        public static void Bootstrap()
        {
            if (Instance != null) return;
            GameObject go = new GameObject("MechJeb.CachedLocalizer");
            Instance = go.AddComponent<CachedLocalizer>();
        }

        public void OnDestroy() => Instance = null;

        public void Awake() => UpdateCachedStrings();

        private void UpdateCachedStrings()
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
            MechJeb_Ascent_label38 = Localizer.Format("#MechJeb_Ascent_label38");
            MechJeb_Ascent_label39 = Localizer.Format("#MechJeb_Ascent_label39");
            MechJeb_Ascent_label40 = Localizer.Format("#MechJeb_Ascent_label40");
            MechJeb_Ascent_label41 = Localizer.Format("#MechJeb_Ascent_label41");
            MechJeb_Ascent_label42 = Localizer.Format("#MechJeb_Ascent_label42");

            MechJeb_Ascent_label44 = Localizer.Format("#MechJeb_Ascent_label44");

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

            MechJeb_Ascent_status9 = Localizer.Format("#MechJeb_Ascent_status9");
            MechJeb_Ascent_status10 = Localizer.Format("#MechJeb_Ascent_status10");
            MechJeb_Ascent_status11 = Localizer.Format("#MechJeb_Ascent_status11");


            MechJeb_Ascent_attachAlt = Localizer.Format("#MechJeb_Ascent_attachAlt");
            MechJeb_Ascent_warnAttachAltHigh = Localizer.Format("#MechJeb_Ascent_warnAttachAltHigh");
            MechJeb_Ascent_warnAttachAltLow = Localizer.Format("#MechJeb_Ascent_warnAttachAltLow");
            MechJeb_Ascent_LaunchToTargetLan = Localizer.Format("#MechJeb_Ascent_LaunchToTargetLan");
            MechJeb_Ascent_LaunchToLan = Localizer.Format("#MechJeb_Ascent_LaunchToLan");
            MechJeb_Ascent_LaunchingToTargetLAN = Localizer.Format("#MechJeb_Ascent_LaunchingToTargetLAN");
            MechJeb_Ascent_LaunchingToManualLAN = Localizer.Format("#MechJeb_Ascent_LaunchingToManualLAN");

            MechJeb_Ascent_msg2 = Localizer.Format("#MechJeb_Ascent_msg2");
            MechJeb_Ascent_msg3 = Localizer.Format("#MechJeb_Ascent_msg3");
            MechJeb_Ascent_hotStaging = Localizer.Format("#MechJeb_Ascent_hotStaging");
            MechJeb_Ascent_dropSolids = Localizer.Format("#MechJeb_Ascent_dropSolids");
            MechJeb_Ascent_leadTime = Localizer.Format("#MechJeb_Ascent_leadTime");

            MechJeb_InfoItems_UnlimitedText = Localizer.Format("#MechJeb_InfoItems_UnlimitedText");
            MechJeb_InfoItems_label1 = Localizer.Format("#MechJeb_InfoItems_label1");

            MechJeb_InfoItems_button5 = Localizer.Format("#MechJeb_InfoItems_button5");
            MechJeb_InfoItems_button6 = Localizer.Format("#MechJeb_InfoItems_button6");

            MechJeb_InfoItems_StatsColumn0  = Localizer.Format("#MechJeb_InfoItems_StatsColumn0");
            MechJeb_InfoItems_StatsColumn1  = Localizer.Format("#MechJeb_InfoItems_StatsColumn1");
            MechJeb_InfoItems_StatsColumn2  = Localizer.Format("#MechJeb_InfoItems_StatsColumn2");
            MechJeb_InfoItems_StatsColumn3  = Localizer.Format("#MechJeb_InfoItems_StatsColumn3");
            MechJeb_InfoItems_StatsColumn4  = Localizer.Format("#MechJeb_InfoItems_StatsColumn4");
            MechJeb_InfoItems_StatsColumn5  = Localizer.Format("#MechJeb_InfoItems_StatsColumn5");
            MechJeb_InfoItems_StatsColumn6  = Localizer.Format("#MechJeb_InfoItems_StatsColumn6");
            MechJeb_InfoItems_StatsColumn7  = Localizer.Format("#MechJeb_InfoItems_StatsColumn7");
            MechJeb_InfoItems_StatsColumn8  = Localizer.Format("#MechJeb_InfoItems_StatsColumn8");
            MechJeb_InfoItems_StatsColumn9  = Localizer.Format("#MechJeb_InfoItems_StatsColumn9");
            MechJeb_InfoItems_StatsColumn10 = Localizer.Format("#MechJeb_InfoItems_StatsColumn10");
            MechJeb_InfoItems_StatsColumn11 = Localizer.Format("#MechJeb_InfoItems_StatsColumn11");
            MechJeb_InfoItems_StatsColumn12 = Localizer.Format("#MechJeb_InfoItems_StatsColumn12");
            MechJeb_InfoItems_StatsColumn13 = Localizer.Format("#MechJeb_InfoItems_StatsColumn13");

            MechJeb_InfoItems_showEmpty = Localizer.Format("#MechJeb_InfoItems_showEmpty");
            MechJeb_InfoItems_hideEmpty = Localizer.Format("#MechJeb_InfoItems_hideEmpty");

            MechJeb_NavBallGuidance_btn1 = Localizer.Format("#MechJeb_NavBallGuidance_btn1");
            MechJeb_NavBallGuidance_btn2 = Localizer.Format("#MechJeb_NavBallGuidance_btn2");

            MechJeb_Ascent_title = Localizer.Format("#MechJeb_Ascent_title");
            MechJeb_WindowEd_title = Localizer.Format("#MechJeb_WindowEd_title");
            MechJeb_WindowEd_CustomInfoWindow_Label1 = Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Label1");
        }
    }
}
