using System.Linq;
using UnityEngine;
using KSP.Localization;
namespace MuMech
{
    class MechJebModuleAirplaneGuidance : DisplayModule
    {

        private static GUIStyle btNormal, btActive, btAuto, btGreen, btWhite;

        public MechJebModuleAirplaneAutopilot autopilot { get { return core.GetComputerModule<MechJebModuleAirplaneAutopilot> (); } }

        public MechJebModuleAirplaneGuidance (MechJebCore core) : base (core)
        {

        }

        bool showpid = false;

        [Persistent (pass = (int)Pass.Global)]
        EditableDouble AltitudeTargettmp = 0, HeadingTargettmp = 90, RollTargettmp = 0, SpeedTargettmp = 0, VertSpeedTargettmp = 0, VertSpeedMaxtmp = 10, RollMaxtmp = 30;

        protected override void WindowGUI (int windowID)
        {
            if (btNormal == null) {
                btNormal = new GUIStyle (GUI.skin.button);
                btNormal.normal.textColor = btNormal.focused.textColor = Color.white;
                btNormal.hover.textColor = btNormal.active.textColor = Color.yellow;
                btNormal.onNormal.textColor = btNormal.onFocused.textColor = btNormal.onHover.textColor = btNormal.onActive.textColor = Color.green;
                //btNormal.padding = new RectOffset(8, 8, 8, 8);

                btActive = new GUIStyle (btNormal);
                btActive.active = btActive.onActive;
                btActive.normal = btActive.onNormal;
                btActive.onFocused = btActive.focused;
                btActive.hover = btActive.onHover;

                btGreen = new GUIStyle (btNormal);
                btGreen.normal.textColor = Color.green;
                btGreen.fixedWidth = 35;

                btWhite = new GUIStyle (btNormal);
                btWhite.normal.textColor = Color.white;
                btWhite.fixedWidth = 35;

                btAuto = new GUIStyle (btNormal);
                btAuto.padding = new RectOffset (8, 8, 8, 8);
                btAuto.normal.textColor = Color.red;
                btAuto.onActive = btAuto.onFocused = btAuto.onHover = btAuto.onNormal = btAuto.active = btAuto.focused = btAuto.hover = btAuto.normal;
            }

            if (autopilot.enabled) {
                if (GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_button1"), btActive)) {//Disengage autopilot
                    autopilot.users.Remove (this);
                }
            } else if (core.attitude.enabled && core.attitude.users.Count (u => !this.Equals (u)) > 0) {
                if (core.attitude.users.Contains (this))
                    core.attitude.users.Remove (this); // so we don't suddenly turn on when the other autopilot finishes
                GUILayout.Button ("Auto", btAuto, GUILayout.ExpandWidth (true));
            } else {
                if (GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_button2"))) {//Engage autopilot
                    autopilot.users.Add (this);
                }
            }

            GUILayout.BeginHorizontal ();
            bool AltitudeHold = autopilot.AltitudeHoldEnabled;
            autopilot.AltitudeHoldEnabled = GUILayout.Toggle (autopilot.AltitudeHoldEnabled, Localizer.Format("#MechJeb_Aircraftauto_Label1"), GUILayout.Width (140));//Altitude Hold
            if (AltitudeHold != autopilot.AltitudeHoldEnabled) {
                if (autopilot.AltitudeHoldEnabled)
                    autopilot.EnableAltitudeHold ();
                else
                    autopilot.DisableAltitudeHold ();
            }
            bool change = false;
            if (GUILayout.Button("-", GUILayout.Width(18))) { AltitudeTargettmp.val -= (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
            AltitudeTargettmp.text = GUILayout.TextField (AltitudeTargettmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
            if (GUILayout.Button("+", GUILayout.Width(18))) { AltitudeTargettmp.val += (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
            if (AltitudeTargettmp < 0)
                AltitudeTargettmp = 0;
            GUILayout.Label ("m", GUILayout.ExpandWidth (true));
            if (change || GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_btnset1"), autopilot.AltitudeTarget == AltitudeTargettmp ? btWhite : btGreen)) {//Set
                autopilot.AltitudeTarget = AltitudeTargettmp;
            }
            GUILayout.EndHorizontal ();


            if (!autopilot.AltitudeHoldEnabled) {
                bool _VertSpeedHoldEnabled = autopilot.VertSpeedHoldEnabled;
                GUILayout.BeginHorizontal ();
                autopilot.VertSpeedHoldEnabled = GUILayout.Toggle (autopilot.VertSpeedHoldEnabled, Localizer.Format("#MechJeb_Aircraftauto_Label2"), GUILayout.Width (140));//Vertical Speed Hold
                if (_VertSpeedHoldEnabled != autopilot.VertSpeedHoldEnabled) {
                    if (autopilot.VertSpeedHoldEnabled)
                        autopilot.EnableVertSpeedHold ();
                    else
                        autopilot.DisableVertSpeedHold ();
                }
                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18))) { VertSpeedTargettmp.val -= (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
                VertSpeedTargettmp.text = GUILayout.TextField(VertSpeedTargettmp.text, GUILayout.ExpandWidth(true),GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(18))) { VertSpeedTargettmp.val += (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
                GUILayout.Label ("m/s", GUILayout.ExpandWidth (true));
                if (change || GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_btnset2"), autopilot.VertSpeedTarget == VertSpeedTargettmp ? btWhite : btGreen)) {
                    autopilot.VertSpeedTarget = VertSpeedTargettmp;
                }
                GUILayout.EndHorizontal ();
            } else {
                GUILayout.BeginHorizontal ();
                GUILayout.Label (Localizer.Format("#MechJeb_Aircraftauto_Label3"), GUILayout.Width (140));//Vertical Speed Limit
                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18))) { VertSpeedMaxtmp.val -= (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
                VertSpeedMaxtmp.text = GUILayout.TextField (VertSpeedMaxtmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
                if (GUILayout.Button("+", GUILayout.Width(18))) { VertSpeedMaxtmp.val += (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
                if (VertSpeedMaxtmp < 0)
                    VertSpeedMaxtmp = 0;
                GUILayout.Label ("m/s", GUILayout.ExpandWidth (true));
                if (change || GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_btnset3"), autopilot.VertSpeedMax == VertSpeedMaxtmp ? btWhite : btGreen)) {
                    autopilot.VertSpeedMax = VertSpeedMaxtmp;
                }
                GUILayout.EndHorizontal ();
            }


            GUILayout.BeginHorizontal ();
            bool _HeadingHoldEnabled = autopilot.HeadingHoldEnabled;
            autopilot.HeadingHoldEnabled = GUILayout.Toggle (autopilot.HeadingHoldEnabled, Localizer.Format("#MechJeb_Aircraftauto_Label4"), GUILayout.Width (140));//"Heading Hold"
            if (_HeadingHoldEnabled != autopilot.HeadingHoldEnabled) {
                if (autopilot.HeadingHoldEnabled)
                    autopilot.EnableHeadingHold ();
                else
                    autopilot.DisableHeadingHold ();
            }
            change = false;
            if (GUILayout.Button("-", GUILayout.Width(18))) { HeadingTargettmp.val -= (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
            HeadingTargettmp.text = GUILayout.TextField (HeadingTargettmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
            if (GUILayout.Button("+", GUILayout.Width(18))) { HeadingTargettmp.val += (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
            HeadingTargettmp = MuUtils.ClampDegrees360 (HeadingTargettmp);
            GUILayout.Label ("°", GUILayout.ExpandWidth (true));
            if (change || GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_btnset4"), autopilot.HeadingTarget == HeadingTargettmp ? btWhite : btGreen)) {
                autopilot.HeadingTarget = HeadingTargettmp;
            }
            GUILayout.EndHorizontal ();


            if (!autopilot.HeadingHoldEnabled) {
                GUILayout.BeginHorizontal ();
                autopilot.RollHoldEnabled = GUILayout.Toggle (autopilot.RollHoldEnabled, Localizer.Format("#MechJeb_Aircraftauto_Label5"), GUILayout.Width (140));//"Roll Hold"
                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18))) { RollTargettmp.val -= (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
                RollTargettmp.text = GUILayout.TextField (RollTargettmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
                if (GUILayout.Button("+", GUILayout.Width(18))) { RollTargettmp.val += (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
                RollTargettmp = MuUtils.Clamp (RollTargettmp, -90, 90);
                GUILayout.Label ("°", GUILayout.ExpandWidth (true));
                if (change || GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_btnset5"), autopilot.RollTarget == RollTargettmp ? btWhite : btGreen)) {
                    autopilot.RollTarget = RollTargettmp;
                }
                GUILayout.EndHorizontal ();
            } else {
                GUILayout.BeginHorizontal ();
                GUILayout.Label (Localizer.Format("#MechJeb_Aircraftauto_Label6"), GUILayout.Width (140));//"    Roll Limit ±"
                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18))) { RollMaxtmp.val -= (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
                RollMaxtmp.text = GUILayout.TextField (RollMaxtmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
                if (GUILayout.Button("+", GUILayout.Width(18))) { RollMaxtmp.val += (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
                RollMaxtmp = MuUtils.Clamp (RollMaxtmp, -60, 60);
                GUILayout.Label ("°", GUILayout.ExpandWidth (true));
                if (change || GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_btnset6"), autopilot.RollMax == RollMaxtmp ? btWhite : btGreen)) {
                    autopilot.RollMax = RollMaxtmp;
                }
                GUILayout.EndHorizontal ();
            }


            GUILayout.BeginHorizontal ();
            bool _AutoThrustCtrl = autopilot.SpeedHoldEnabled;
            autopilot.SpeedHoldEnabled = GUILayout.Toggle (autopilot.SpeedHoldEnabled, Localizer.Format("#MechJeb_Aircraftauto_Label7"), GUILayout.Width (140));//Speed Hold
            if (autopilot.SpeedHoldEnabled != _AutoThrustCtrl) {
                if (autopilot.SpeedHoldEnabled)
                    autopilot.EnableSpeedHold ();
                else
                    autopilot.DisableSpeedHold ();
            }
            change = false;
            if (GUILayout.Button("-", GUILayout.Width(18))) { SpeedTargettmp.val -= (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
            SpeedTargettmp.text = GUILayout.TextField (SpeedTargettmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
            if (GUILayout.Button("+", GUILayout.Width(18))) { SpeedTargettmp.val += (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); change = true; }
            if (SpeedTargettmp < 0)
                SpeedTargettmp = 0;
            GUILayout.Label ("m/s", GUILayout.ExpandWidth (true));
            if (change || GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_btnset7"), autopilot.SpeedTarget == SpeedTargettmp ? btWhite : btGreen)) {//Set
                autopilot.SpeedTarget = SpeedTargettmp;
            }
            GUILayout.EndHorizontal ();


            if (!showpid) {
                if (GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_button3"), GUILayout.Width (40))) {//"PID"
                    showpid = true;
                }
            } else {
                if (GUILayout.Button (Localizer.Format("#MechJeb_Aircraftauto_button4"), GUILayout.Width (140))) {//Hide PID
                    showpid = false;
                }
                GUILayout.BeginHorizontal ();
                GUILayout.Label (Localizer.Format("#MechJeb_Aircraftauto_Label8"), GUILayout.ExpandWidth (true));//Accceleration
                GUILayout.Label ("Kp", GUILayout.ExpandWidth (false));
                autopilot.AccKp.text = GUILayout.TextField (autopilot.AccKp.text, GUILayout.Width (40));
                GUILayout.Label ("i", GUILayout.ExpandWidth (false));
                autopilot.AccKi.text = GUILayout.TextField (autopilot.AccKi.text, GUILayout.Width (40));
                GUILayout.Label ("d", GUILayout.ExpandWidth (false));
                autopilot.AccKd.text = GUILayout.TextField (autopilot.AccKd.text, GUILayout.Width (40));
                GUILayout.EndHorizontal ();
                if (autopilot.SpeedHoldEnabled)
                    GUILayout.Label (Localizer.Format("#MecgJeb_Aircraftauto_error1", autopilot.a_err.ToString ("F2"),autopilot.RealAccelerationTarget.ToString ("F2"),autopilot.cur_acc.ToString ("F2")),GUILayout.ExpandWidth (false));//"error:"<<1>>" Target:"<<2>> " Cur:"<<3>>

                GUILayout.BeginHorizontal ();
                GUILayout.Label (Localizer.Format("#MechJeb_Aircraftauto_Label9"), GUILayout.ExpandWidth (true));//"VertSpeed"
                GUILayout.Label ("Kp", GUILayout.ExpandWidth (false));
                autopilot.VerKp.text = GUILayout.TextField (autopilot.VerKp.text, GUILayout.Width (40));
                GUILayout.Label ("i", GUILayout.ExpandWidth (false));
                autopilot.VerKi.text = GUILayout.TextField (autopilot.VerKi.text, GUILayout.Width (40));
                GUILayout.Label ("d", GUILayout.ExpandWidth (false));
                autopilot.VerKd.text = GUILayout.TextField (autopilot.VerKd.text, GUILayout.Width (40));
                GUILayout.EndHorizontal ();
                if (autopilot.VertSpeedHoldEnabled)
                    GUILayout.Label(Localizer.Format("#MecgJeb_Aircraftauto_error2", autopilot.v_err.ToString("F2"), autopilot.RealVertSpeedTarget.ToString("F2"), vesselState.speedVertical.ToString("F2"), GUILayout.ExpandWidth(false)));//error:" Target:"" Cur:"
                
                GUILayout.BeginHorizontal ();
                GUILayout.Label (Localizer.Format("#MechJeb_Aircraftauto_Label10"), GUILayout.ExpandWidth (true));//Roll
                GUILayout.Label ("Kp", GUILayout.ExpandWidth (false));
                autopilot.RolKp.text = GUILayout.TextField (autopilot.RolKp.text, GUILayout.Width (40));
                GUILayout.Label ("i", GUILayout.ExpandWidth (false));
                autopilot.RolKi.text = GUILayout.TextField (autopilot.RolKi.text, GUILayout.Width (40));
                GUILayout.Label ("d", GUILayout.ExpandWidth (false));
                autopilot.RolKd.text = GUILayout.TextField (autopilot.RolKd.text, GUILayout.Width (40));
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Label ("Yaw", GUILayout.ExpandWidth (true));
                GUILayout.Label ("Kp", GUILayout.ExpandWidth (false));
                autopilot.YawKp.text = GUILayout.TextField (autopilot.YawKp.text, GUILayout.Width (40));
                GUILayout.Label ("i", GUILayout.ExpandWidth (false));
                autopilot.YawKi.text = GUILayout.TextField (autopilot.YawKi.text, GUILayout.Width (40));
                GUILayout.Label ("d", GUILayout.ExpandWidth (false));
                autopilot.YawKd.text = GUILayout.TextField (autopilot.YawKd.text, GUILayout.Width (40));
                GUILayout.EndHorizontal ();
                GUILayout.BeginHorizontal ();
                GUILayout.Label (Localizer.Format("#MechJeb_Aircraftauto_Label11"), GUILayout.ExpandWidth (false));//Yaw Control Limit
                autopilot.YawLimit.text = GUILayout.TextField (autopilot.YawLimit.text, GUILayout.Width (40));
                GUILayout.EndHorizontal ();
            }
            base.WindowGUI (windowID);
        }

        public override GUILayoutOption[] WindowOptions ()
        {
            return new GUILayoutOption[] { GUILayout.Width (300), GUILayout.Height (200) };
        }

        public override string GetName ()
        {
            return Localizer.Format("#MechJeb_Aircraftauto_title");//Aircraft Autopilot
        }

    }
}
