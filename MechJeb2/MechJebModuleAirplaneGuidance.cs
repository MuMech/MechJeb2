using System;
using System.Linq;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    internal class MechJebModuleAirplaneGuidance : DisplayModule
    {
        private static GUIStyle btNormal, btActive, btAuto, btGreen, btWhite;

        public MechJebModuleAirplaneAutopilot autopilot => Core.GetComputerModule<MechJebModuleAirplaneAutopilot>();

        public MechJebModuleAirplaneGuidance(MechJebCore core) : base(core)
        {
        }

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        public bool showpid;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble AltitudeTargettmp = 0,
            HeadingTargettmp                    = 90,
            RollTargettmp                       = 0,
            SpeedTargettmp                      = 0,
            VertSpeedTargettmp                  = 0,
            RollMaxtmp                          = 30;

        protected override void WindowGUI(int windowID)
        {
            if (btNormal == null)
            {
                btNormal                    = new GUIStyle(GUI.skin.button);
                btNormal.normal.textColor   = btNormal.focused.textColor   = Color.white;
                btNormal.hover.textColor    = btNormal.active.textColor    = Color.yellow;
                btNormal.onNormal.textColor = btNormal.onFocused.textColor = btNormal.onHover.textColor = btNormal.onActive.textColor = Color.green;
                //btNormal.padding = new RectOffset(8, 8, 8, 8);

                btActive           = new GUIStyle(btNormal);
                btActive.active    = btActive.onActive;
                btActive.normal    = btActive.onNormal;
                btActive.onFocused = btActive.focused;
                btActive.hover     = btActive.onHover;

                btGreen                  = new GUIStyle(btNormal);
                btGreen.normal.textColor = Color.green;
                btGreen.fixedWidth       = 35;

                btWhite                  = new GUIStyle(btNormal);
                btWhite.normal.textColor = Color.white;
                btWhite.fixedWidth       = 35;

                btAuto = new GUIStyle(btNormal);
                btAuto.padding = new RectOffset(8, 8, 8, 8);
                btAuto.normal.textColor = Color.red;
                btAuto.onActive = btAuto.onFocused = btAuto.onHover = btAuto.onNormal = btAuto.active = btAuto.focused = btAuto.hover = btAuto.normal;
            }

            if (autopilot.Enabled)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_button1"), btActive))
                {
                    //Disengage autopilot
                    autopilot.Users.Remove(this);
                }
            }
            else if (Core.Attitude.Enabled && Core.Attitude.Users.Count(u => !Equals(u)) > 0)
            {
                if (Core.Attitude.Users.Contains(this))
                    Core.Attitude.Users.Remove(this); // so we don't suddenly turn on when the other autopilot finishes
                GUILayout.Button("Auto", btAuto, GUILayout.ExpandWidth(true));
            }
            else
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_button2")))
                {
                    //Engage autopilot
                    autopilot.Users.Add(this);
                }
            }

            GUILayout.BeginHorizontal();
            bool AltitudeHold = autopilot.AltitudeHoldEnabled;
            autopilot.AltitudeHoldEnabled = GUILayout.Toggle(autopilot.AltitudeHoldEnabled, Localizer.Format("#MechJeb_Aircraftauto_Label1"),
                GUILayout.Width(140)); //Altitude Hold
            if (AltitudeHold != autopilot.AltitudeHoldEnabled)
            {
                if (autopilot.AltitudeHoldEnabled)
                    autopilot.EnableAltitudeHold();
                else
                    autopilot.DisableAltitudeHold();
            }

            bool change = false;
            if (GUILayout.Button("-", GUILayout.Width(18)))
            {
                AltitudeTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change                =  true;
            }

            AltitudeTargettmp.Text = GUILayout.TextField(AltitudeTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            if (GUILayout.Button("+", GUILayout.Width(18)))
            {
                AltitudeTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change                =  true;
            }

            if (AltitudeTargettmp < 0)
                AltitudeTargettmp = 0;
            GUILayout.Label("m", GUILayout.ExpandWidth(true));
            if (change || GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_btnset1"),
                    autopilot.AltitudeTarget == AltitudeTargettmp ? btWhite : btGreen))
            {
                //Set
                autopilot.AltitudeTarget = AltitudeTargettmp;
            }

            GUILayout.EndHorizontal();


            if (!autopilot.AltitudeHoldEnabled)
            {
                bool _VertSpeedHoldEnabled = autopilot.VertSpeedHoldEnabled;
                GUILayout.BeginHorizontal();
                autopilot.VertSpeedHoldEnabled = GUILayout.Toggle(autopilot.VertSpeedHoldEnabled, Localizer.Format("#MechJeb_Aircraftauto_Label2"),
                    GUILayout.Width(140)); //Vertical Speed Hold
                if (_VertSpeedHoldEnabled != autopilot.VertSpeedHoldEnabled)
                {
                    if (autopilot.VertSpeedHoldEnabled)
                        autopilot.EnableVertSpeedHold();
                    else
                        autopilot.DisableVertSpeedHold();
                }

                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18)))
                {
                    VertSpeedTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change                 =  true;
                }

                VertSpeedTargettmp.Text = GUILayout.TextField(VertSpeedTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(18)))
                {
                    VertSpeedTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change                 =  true;
                }

                VertSpeedTargettmp = Math.Max(0, VertSpeedTargettmp);
                GUILayout.Label("m/s", GUILayout.ExpandWidth(true));
                if (change || GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_btnset2"),
                        autopilot.VertSpeedTarget == VertSpeedTargettmp ? btWhite : btGreen))
                {
                    autopilot.VertSpeedTarget = VertSpeedTargettmp;
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Aircraftauto_VS"), GUILayout.Width(140)); //"    V/S ±"
                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18)))
                {
                    VertSpeedTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change                 =  true;
                }

                VertSpeedTargettmp.Text = GUILayout.TextField(VertSpeedTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(18)))
                {
                    VertSpeedTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change                 =  true;
                }

                VertSpeedTargettmp = Math.Max(0, VertSpeedTargettmp);
                GUILayout.Label("m/s", GUILayout.ExpandWidth(true));
                if (change || GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_btnset6"),
                        autopilot.VertSpeedTarget == VertSpeedTargettmp ? btWhite : btGreen))
                {
                    autopilot.VertSpeedTarget = VertSpeedTargettmp;
                }

                GUILayout.EndHorizontal();
            }


            GUILayout.BeginHorizontal();
            bool _HeadingHoldEnabled = autopilot.HeadingHoldEnabled;
            autopilot.HeadingHoldEnabled = GUILayout.Toggle(autopilot.HeadingHoldEnabled, Localizer.Format("#MechJeb_Aircraftauto_Label4"),
                GUILayout.Width(140)); //"Heading Hold"
            if (_HeadingHoldEnabled != autopilot.HeadingHoldEnabled)
            {
                if (autopilot.HeadingHoldEnabled)
                    autopilot.EnableHeadingHold();
                else
                    autopilot.DisableHeadingHold();
            }

            change = false;
            if (GUILayout.Button("-", GUILayout.Width(18)))
            {
                HeadingTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change               =  true;
            }

            HeadingTargettmp.Text = GUILayout.TextField(HeadingTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            if (GUILayout.Button("+", GUILayout.Width(18)))
            {
                HeadingTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change               =  true;
            }

            HeadingTargettmp = MuUtils.ClampDegrees360(HeadingTargettmp);
            GUILayout.Label("°", GUILayout.ExpandWidth(true));
            if (change || GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_btnset4"),
                    autopilot.HeadingTarget == HeadingTargettmp ? btWhite : btGreen))
            {
                autopilot.HeadingTarget = HeadingTargettmp;
            }

            GUILayout.EndHorizontal();


            if (!autopilot.HeadingHoldEnabled)
            {
                GUILayout.BeginHorizontal();
                autopilot.RollHoldEnabled = GUILayout.Toggle(autopilot.RollHoldEnabled, Localizer.Format("#MechJeb_Aircraftauto_Label5"),
                    GUILayout.Width(140)); //"Roll Hold"
                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18)))
                {
                    RollTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change            =  true;
                }

                RollTargettmp.Text = GUILayout.TextField(RollTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(18)))
                {
                    RollTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change            =  true;
                }

                RollTargettmp = MuUtils.Clamp(RollTargettmp, -90, 90);
                GUILayout.Label("°", GUILayout.ExpandWidth(true));
                if (change || GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_btnset5"),
                        autopilot.RollTarget == RollTargettmp ? btWhite : btGreen))
                {
                    autopilot.RollTarget = RollTargettmp;
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Aircraftauto_Label6"), GUILayout.Width(140)); //"    Roll Limit ±"
                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18)))
                {
                    RollMaxtmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change         =  true;
                }

                RollMaxtmp.Text = GUILayout.TextField(RollMaxtmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(18)))
                {
                    RollMaxtmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change         =  true;
                }

                RollMaxtmp = MuUtils.Clamp(RollMaxtmp, -60, 60);
                GUILayout.Label("°", GUILayout.ExpandWidth(true));
                if (change || GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_btnset6"),
                        autopilot.BankAngle == RollMaxtmp ? btWhite : btGreen))
                {
                    autopilot.BankAngle = RollMaxtmp;
                }

                GUILayout.EndHorizontal();
            }


            GUILayout.BeginHorizontal();
            bool _AutoThrustCtrl = autopilot.SpeedHoldEnabled;
            autopilot.SpeedHoldEnabled =
                GUILayout.Toggle(autopilot.SpeedHoldEnabled, Localizer.Format("#MechJeb_Aircraftauto_Label7"), GUILayout.Width(140)); //Speed Hold
            if (autopilot.SpeedHoldEnabled != _AutoThrustCtrl)
            {
                if (autopilot.SpeedHoldEnabled)
                    autopilot.EnableSpeedHold();
                else
                    autopilot.DisableSpeedHold();
            }

            change = false;
            if (GUILayout.Button("-", GUILayout.Width(18)))
            {
                SpeedTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change             =  true;
            }

            SpeedTargettmp.Text = GUILayout.TextField(SpeedTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            if (GUILayout.Button("+", GUILayout.Width(18)))
            {
                SpeedTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change             =  true;
            }

            if (SpeedTargettmp < 0)
                SpeedTargettmp = 0;
            GUILayout.Label("m/s", GUILayout.ExpandWidth(true));
            if (change || GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_btnset7"),
                    autopilot.SpeedTarget == SpeedTargettmp ? btWhite : btGreen))
            {
                //Set
                autopilot.SpeedTarget = SpeedTargettmp;
            }

            GUILayout.EndHorizontal();


            if (!showpid)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_button3"), GUILayout.Width(40)))
                {
                    //"PID"
                    showpid = true;
                }
            }
            else
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Aircraftauto_button4"), GUILayout.Width(140)))
                {
                    //Hide PID
                    showpid = false;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Aircraftauto_Label8"), GUILayout.ExpandWidth(true)); //Accceleration
                GUILayout.Label("Kp", GUILayout.ExpandWidth(false));
                autopilot.AccKp.Text = GUILayout.TextField(autopilot.AccKp.Text, GUILayout.Width(40));
                GUILayout.Label("i", GUILayout.ExpandWidth(false));
                autopilot.AccKi.Text = GUILayout.TextField(autopilot.AccKi.Text, GUILayout.Width(40));
                GUILayout.Label("d", GUILayout.ExpandWidth(false));
                autopilot.AccKd.Text = GUILayout.TextField(autopilot.AccKd.Text, GUILayout.Width(40));
                GUILayout.EndHorizontal();
                if (autopilot.SpeedHoldEnabled)
                    GUILayout.Label(
                        Localizer.Format("#MecgJeb_Aircraftauto_error1", autopilot.AErr.ToString("F2"),
                            autopilot.RealAccelerationTarget.ToString("F2"), autopilot.CurAcc.ToString("F2")),
                        GUILayout.ExpandWidth(false)); //"error:"<<1>>" Target:"<<2>> " Cur:"<<3>>

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Aircraftauto_Pitch"), GUILayout.ExpandWidth(true)); //"VertSpeed"
                GUILayout.Label("Kp", GUILayout.ExpandWidth(false));
                autopilot.PitKp.Text = GUILayout.TextField(autopilot.PitKp.Text, GUILayout.Width(40));
                GUILayout.Label("i", GUILayout.ExpandWidth(false));
                autopilot.PitKi.Text = GUILayout.TextField(autopilot.PitKi.Text, GUILayout.Width(40));
                GUILayout.Label("d", GUILayout.ExpandWidth(false));
                autopilot.PitKd.Text = GUILayout.TextField(autopilot.PitKd.Text, GUILayout.Width(40));
                GUILayout.EndHorizontal();
                if (autopilot.VertSpeedHoldEnabled)
                    GUILayout.Label(Localizer.Format("#MecgJeb_Aircraftauto_error2", autopilot.PitchErr.ToString("F2"),
                        autopilot.RealPitchTarget.ToString("F2"), VesselState.currentPitch.ToString("F2"), autopilot.PitchAct.ToString("F5"),
                        GUILayout.ExpandWidth(false))); //error:" Target:"" Cur:"

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Aircraftauto_Label10"), GUILayout.ExpandWidth(true)); //Roll
                GUILayout.Label("Kp", GUILayout.ExpandWidth(false));
                autopilot.RolKp.Text = GUILayout.TextField(autopilot.RolKp.Text, GUILayout.Width(40));
                GUILayout.Label("i", GUILayout.ExpandWidth(false));
                autopilot.RolKi.Text = GUILayout.TextField(autopilot.RolKi.Text, GUILayout.Width(40));
                GUILayout.Label("d", GUILayout.ExpandWidth(false));
                autopilot.RolKd.Text = GUILayout.TextField(autopilot.RolKd.Text, GUILayout.Width(40));
                GUILayout.EndHorizontal();
                if (autopilot.RollHoldEnabled)
                    GUILayout.Label(Localizer.Format("#MecgJeb_Aircraftauto_error2", autopilot.RollErr.ToString("F2"),
                        autopilot.RealRollTarget.ToString("F2"), (-VesselState.currentRoll).ToString("F2"), autopilot.RollAct.ToString("F5"),
                        GUILayout.ExpandWidth(false))); //error:" Target:"" Cur:"

                GUILayout.BeginHorizontal();
                GUILayout.Label("Yaw", GUILayout.ExpandWidth(true));
                GUILayout.Label("Kp", GUILayout.ExpandWidth(false));
                autopilot.YawKp.Text = GUILayout.TextField(autopilot.YawKp.Text, GUILayout.Width(40));
                GUILayout.Label("i", GUILayout.ExpandWidth(false));
                autopilot.YawKi.Text = GUILayout.TextField(autopilot.YawKi.Text, GUILayout.Width(40));
                GUILayout.Label("d", GUILayout.ExpandWidth(false));
                autopilot.YawKd.Text = GUILayout.TextField(autopilot.YawKd.Text, GUILayout.Width(40));
                GUILayout.EndHorizontal();
                if (autopilot.HeadingHoldEnabled)
                    GUILayout.Label(Localizer.Format("#MecgJeb_Aircraftauto_error2", autopilot.YawErr.ToString("F2"),
                        autopilot.RealYawTarget.ToString("F2"), autopilot.CurrYaw.ToString("F2"), autopilot.YawAct.ToString("F5"),
                        GUILayout.ExpandWidth(false))); //error:" Target:"" Cur:"

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Aircraftauto_Limits"), GUILayout.ExpandWidth(false)); //Yaw Control Limit
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Aircraftauto_PitchDownLimit"), GUILayout.ExpandWidth(false));
                autopilot.PitchDownLimit.Text = GUILayout.TextField(autopilot.PitchDownLimit.Text, GUILayout.Width(40));
                GUILayout.Label(Localizer.Format("#MechJeb_Aircraftauto_PitchUpLimit"), GUILayout.ExpandWidth(false));
                autopilot.PitchUpLimit.Text = GUILayout.TextField(autopilot.PitchUpLimit.Text, GUILayout.Width(40));
                GUILayout.Label(Localizer.Format("#MechJeb_Aircraftauto_YawLimit"), GUILayout.ExpandWidth(false));
                autopilot.YawLimit.Text = GUILayout.TextField(autopilot.YawLimit.Text, GUILayout.Width(40));
                GUILayout.Label(Localizer.Format("#MechJeb_Aircraftauto_RollLimit"), GUILayout.ExpandWidth(false));
                autopilot.RollLimit.Text = GUILayout.TextField(autopilot.RollLimit.Text, GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }

            base.WindowGUI(windowID);
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(300), GUILayout.Height(200) };

        public override string GetName() => Localizer.Format("#MechJeb_Aircraftauto_title"); //Aircraft Autopilot

        public override string IconName() => "Aircraft Autopilot";
    }
}
