using System.Linq;
using UnityEngine;

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
                if (GUILayout.Button ("Disengage autopilot", btActive)) {
                    autopilot.users.Remove (this);
                }
            } else if (core.attitude.enabled && core.attitude.users.Count (u => !this.Equals (u)) > 0) {
                if (core.attitude.users.Contains (this))
                    core.attitude.users.Remove (this); // so we don't suddenly turn on when the other autopilot finishes
                GUILayout.Button ("Auto", btAuto, GUILayout.ExpandWidth (true));
            } else {
                if (GUILayout.Button ("Engage autopilot")) {
                    autopilot.users.Add (this);
                }
            }

            GUILayout.BeginHorizontal ();
            bool AltitudeHold = autopilot.AltitudeHoldEnabled;
            autopilot.AltitudeHoldEnabled = GUILayout.Toggle (autopilot.AltitudeHoldEnabled, "Altitude Hold", GUILayout.Width (140));
            if (AltitudeHold != autopilot.AltitudeHoldEnabled) {
                if (autopilot.AltitudeHoldEnabled)
                    autopilot.EnableAltitudeHold ();
                else
                    autopilot.DisableAltitudeHold ();
            }
            AltitudeTargettmp.text = GUILayout.TextField (AltitudeTargettmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
            if (AltitudeTargettmp < 0)
                AltitudeTargettmp = 0;
            GUILayout.Label ("m", GUILayout.ExpandWidth (true));
            if (GUILayout.Button ("Set", autopilot.AltitudeTarget == AltitudeTargettmp ? btWhite : btGreen)) {
                autopilot.AltitudeTarget = AltitudeTargettmp;
            }
            GUILayout.EndHorizontal ();


            if (!autopilot.AltitudeHoldEnabled) {
                bool _VertSpeedHoldEnabled = autopilot.VertSpeedHoldEnabled;
                GUILayout.BeginHorizontal ();
                autopilot.VertSpeedHoldEnabled = GUILayout.Toggle (autopilot.VertSpeedHoldEnabled, "VERTSPEED Hold", GUILayout.Width (140));
                if (_VertSpeedHoldEnabled != autopilot.VertSpeedHoldEnabled) {
                    if (autopilot.VertSpeedHoldEnabled)
                        autopilot.EnableVertSpeedHold ();
                    else
                        autopilot.DisableVertSpeedHold ();
                }
                VertSpeedTargettmp.text = GUILayout.TextField (VertSpeedTargettmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
                GUILayout.Label ("m/s", GUILayout.ExpandWidth (true));
                if (GUILayout.Button ("Set", autopilot.VertSpeedTarget == VertSpeedTargettmp ? btWhite : btGreen)) {
                    autopilot.VertSpeedTarget = VertSpeedTargettmp;
                }
                GUILayout.EndHorizontal ();
            } else {
                GUILayout.BeginHorizontal ();
                GUILayout.Label ("    VERTSPEED Limit", GUILayout.Width (140));
                VertSpeedMaxtmp.text = GUILayout.TextField (VertSpeedMaxtmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
                if (VertSpeedMaxtmp < 0)
                    VertSpeedMaxtmp = 0;
                GUILayout.Label ("m/s", GUILayout.ExpandWidth (true));
                if (GUILayout.Button ("Set", autopilot.VertSpeedMax == VertSpeedMaxtmp ? btWhite : btGreen)) {
                    autopilot.VertSpeedMax = VertSpeedMaxtmp;
                }
                GUILayout.EndHorizontal ();
            }


            GUILayout.BeginHorizontal ();
            bool _HeadingHoldEnabled = autopilot.HeadingHoldEnabled;
            autopilot.HeadingHoldEnabled = GUILayout.Toggle (autopilot.HeadingHoldEnabled, "Heading Hold", GUILayout.Width (140));
            if (_HeadingHoldEnabled != autopilot.HeadingHoldEnabled) {
                if (autopilot.HeadingHoldEnabled)
                    autopilot.EnableHeadingHold ();
                else
                    autopilot.DisableHeadingHold ();
            }
            HeadingTargettmp.text = GUILayout.TextField (HeadingTargettmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
            HeadingTargettmp = MuUtils.ClampDegrees360 (HeadingTargettmp);
            GUILayout.Label ("°", GUILayout.ExpandWidth (true));
            if (GUILayout.Button ("Set", autopilot.HeadingTarget == HeadingTargettmp ? btWhite : btGreen)) {
                autopilot.HeadingTarget = HeadingTargettmp;
            }
            GUILayout.EndHorizontal ();


            if (!autopilot.HeadingHoldEnabled) {
                GUILayout.BeginHorizontal ();
                autopilot.RollHoldEnabled = GUILayout.Toggle (autopilot.RollHoldEnabled, "Roll Hold", GUILayout.Width (140));
                RollTargettmp.text = GUILayout.TextField (RollTargettmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
                RollTargettmp = MuUtils.Clamp (RollTargettmp, -90, 90);
                GUILayout.Label ("°", GUILayout.ExpandWidth (true));
                if (GUILayout.Button ("Set", autopilot.RollTarget == RollTargettmp ? btWhite : btGreen)) {
                    autopilot.RollTarget = RollTargettmp;
                }
                GUILayout.EndHorizontal ();
            } else {
                GUILayout.BeginHorizontal ();
                GUILayout.Label ("    Roll Limit ±", GUILayout.Width (140));
                RollMaxtmp.text = GUILayout.TextField (RollMaxtmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
                RollMaxtmp = MuUtils.Clamp (RollMaxtmp, -60, 60);
                GUILayout.Label ("°", GUILayout.ExpandWidth (true));
                if (GUILayout.Button ("Set", autopilot.RollMax == RollMaxtmp ? btWhite : btGreen)) {
                    autopilot.RollMax = RollMaxtmp;
                }
                GUILayout.EndHorizontal ();
            }


            GUILayout.BeginHorizontal ();
            bool _AutoThrustCtrl = autopilot.SpeedHoldEnabled;
            autopilot.SpeedHoldEnabled = GUILayout.Toggle (autopilot.SpeedHoldEnabled, "Speed Hold", GUILayout.Width (140));
            if (autopilot.SpeedHoldEnabled != _AutoThrustCtrl) {
                if (autopilot.SpeedHoldEnabled)
                    autopilot.EnableSpeedHold ();
                else
                    autopilot.DisableSpeedHold ();
            }
            SpeedTargettmp.text = GUILayout.TextField (SpeedTargettmp.text, GUILayout.ExpandWidth (true), GUILayout.Width (60));
            if (SpeedTargettmp < 0)
                SpeedTargettmp = 0;
            GUILayout.Label ("m/s", GUILayout.ExpandWidth (true));
            if (GUILayout.Button ("Set", autopilot.SpeedTarget == SpeedTargettmp ? btWhite : btGreen)) {
                autopilot.SpeedTarget = SpeedTargettmp;
            }
            GUILayout.EndHorizontal ();


            if (!showpid) {
                if (GUILayout.Button ("PID", GUILayout.Width (40))) {
                    showpid = true;
                }
            } else {
                if (GUILayout.Button ("Hide PID", GUILayout.Width (140))) {
                    showpid = false;
                }
                GUILayout.BeginHorizontal ();
                GUILayout.Label ("Accceleration", GUILayout.ExpandWidth (true));
                GUILayout.Label ("Kp", GUILayout.ExpandWidth (false));
                autopilot.AccKp.text = GUILayout.TextField (autopilot.AccKp.text, GUILayout.Width (40));
                GUILayout.Label ("i", GUILayout.ExpandWidth (false));
                autopilot.AccKi.text = GUILayout.TextField (autopilot.AccKi.text, GUILayout.Width (40));
                GUILayout.Label ("d", GUILayout.ExpandWidth (false));
                autopilot.AccKd.text = GUILayout.TextField (autopilot.AccKd.text, GUILayout.Width (40));
                GUILayout.EndHorizontal ();
                if (autopilot.SpeedHoldEnabled)
                    GUILayout.Label ("error:" + autopilot.a_err.ToString ("F2") + " Target:" + autopilot.RealAccelerationTarget.ToString ("F2") + " Cur:" + autopilot.cur_acc.ToString ("F2"), GUILayout.ExpandWidth (false));

                GUILayout.BeginHorizontal ();
                GUILayout.Label ("VertSpeed", GUILayout.ExpandWidth (true));
                GUILayout.Label ("Kp", GUILayout.ExpandWidth (false));
                autopilot.VerKp.text = GUILayout.TextField (autopilot.VerKp.text, GUILayout.Width (40));
                GUILayout.Label ("i", GUILayout.ExpandWidth (false));
                autopilot.VerKi.text = GUILayout.TextField (autopilot.VerKi.text, GUILayout.Width (40));
                GUILayout.Label ("d", GUILayout.ExpandWidth (false));
                autopilot.VerKd.text = GUILayout.TextField (autopilot.VerKd.text, GUILayout.Width (40));
                GUILayout.EndHorizontal ();
                if (autopilot.VertSpeedHoldEnabled)
                    GUILayout.Label ("error:" + autopilot.v_err.ToString ("F2") + " Target:" + autopilot.RealVertSpeedTarget.ToString ("F2") + " Cur:" + vesselState.speedVertical.ToString ("F2"), GUILayout.ExpandWidth (false));
                
                GUILayout.BeginHorizontal ();
                GUILayout.Label ("Roll", GUILayout.ExpandWidth (true));
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
                GUILayout.Label ("Yaw Control Limit", GUILayout.ExpandWidth (false));
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
            return "Airplane AutoPilot";
        }

    }
}
