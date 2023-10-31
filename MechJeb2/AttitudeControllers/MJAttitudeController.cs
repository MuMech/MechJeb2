using System;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace MuMech.AttitudeControllers
{
    internal class MJAttitudeController : BaseAttitudeController
    {
        private PIDControllerV3 _pid;

        private Vector3d _lastAct = Vector3d.zero;
        private Vector3d _pidAction; //info
        private Vector3d _error;     //info

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool TfAutoTune = true;

        private Vector3d _tfV = new Vector3d(0.3, 0.3, 0.3);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public Vector3 TfVec = new Vector3(0.3f, 0.3f, 0.3f); // use the serialize since Vector3d does not

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public double TfMin = 0.1;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public double TfMax = 0.5;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool LowPassFilter = true;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public double KpFactor = 3;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public double KiFactor = 6;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public double KdFactor = 0.5;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public double Deadband = 0.0001;

        //Lower value of "kWlimit" reduces maximum angular velocity
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble KWlimit = 0.15;

        private readonly Vector3d _defaultTfV = new Vector3d(0.3, 0.3, 0.3);

        // GUI
        private EditableDouble _uiTfX;
        private EditableDouble _uiTfY;
        private EditableDouble _uiTfZ;
        private EditableDouble _uiTfMin;
        private EditableDouble _uiTfMax;
        private EditableDouble _uiKpFactor;
        private EditableDouble _uiKiFactor;
        private EditableDouble _uiKdFactor;
        private EditableDouble _uiDeadband;

        public MJAttitudeController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        public override void OnStart()
        {
            _pid = new PIDControllerV3(Vector3d.zero, Vector3d.zero, Vector3d.zero, 1, -1);
            SetPIDParameters();
            _lastAct = Vector3d.zero;


            _uiTfX      = new EditableDouble(_tfV.x);
            _uiTfY      = new EditableDouble(_tfV.y);
            _uiTfZ      = new EditableDouble(_tfV.z);
            _uiTfMin    = new EditableDouble(TfMin);
            _uiTfMax    = new EditableDouble(TfMax);
            _uiKpFactor = new EditableDouble(KpFactor);
            _uiKiFactor = new EditableDouble(KiFactor);
            _uiKdFactor = new EditableDouble(KdFactor);
            _uiDeadband = new EditableDouble(Deadband);
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);
            _tfV = TfVec;
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            TfVec = _tfV;
            base.OnSave(local, type, global);
        }

        private void SetPIDParameters()
        {
            Vector3d invTf = (TfAutoTune ? _tfV : _defaultTfV).InvertNoNaN();

            _pid.Kd = KdFactor * invTf;

            _pid.Kp = 1 / (KpFactor * Math.Sqrt(2)) * _pid.Kd;
            _pid.Kp.Scale(invTf);

            _pid.Ki = 1 / (KiFactor * Math.Sqrt(2)) * _pid.Kp;
            _pid.Ki.Scale(invTf);

            _pid.INTAccum = _pid.INTAccum.Clamp(-5, 5);
        }

        private void TuneTf(Vector3d torque)
        {
            var ratio = new Vector3d(
                torque.x != 0 ? Ac.VesselState.MoI.x / torque.x : 0,
                torque.y != 0 ? Ac.VesselState.MoI.y / torque.y : 0,
                torque.z != 0 ? Ac.VesselState.MoI.z / torque.z : 0
            );

            _tfV = 0.05 * ratio;

            Vector3d delayFactor = Vector3d.one + 2 * Ac.VesselState.torqueReactionSpeed;


            _tfV.Scale(delayFactor);


            _tfV = _tfV.Clamp(2.0 * TimeWarp.fixedDeltaTime, TfMax);
            _tfV = _tfV.Clamp(TfMin, TfMax);

            //Tf = Mathf.Clamp((float)ratio.magnitude / 20f, 2 * TimeWarp.fixedDeltaTime, 1f);
            //Tf = Mathf.Clamp((float)Tf, (float)TfMin, (float)TfMax);
        }

        public override void ResetConfig()
        {
            _tfV     = _defaultTfV;
            TfMin    = 0.1;
            TfMax    = 0.5;
            KpFactor = 3;
            KiFactor = 6;
            KdFactor = 0.5;
            Deadband = 0.0001;
            KWlimit  = 0.15;
        }

        public override void Reset() => _pid.Reset();

        public override void DrivePre(FlightCtrlState s, out Vector3d act, out Vector3d deltaEuler)
        {
            Transform vesselTransform = Ac.Vessel.ReferenceTransform;

            // Find out the real shorter way to turn where we wan to.
            // Thanks to HoneyFox
            Vector3d tgtLocalUp = vesselTransform.transform.rotation.Inverse() * Ac.RequestedAttitude * Vector3d.forward;
            Vector3d curLocalUp = Vector3d.up;

            double turnAngle = Math.Abs(Vector3d.Angle(curLocalUp, tgtLocalUp));
            var rotDirection = new Vector2d(tgtLocalUp.x, tgtLocalUp.z);
            rotDirection = rotDirection.normalized * turnAngle;

            // And the lowest roll
            // Thanks to Crzyrndm
            var normVec = Vector3.Cross(Ac.RequestedAttitude * Vector3.forward, vesselTransform.up);
            Quaternion targetDeRotated = Quaternion.AngleAxis((float)turnAngle, normVec) * Ac.RequestedAttitude;
            float rollError = Vector3.Angle(vesselTransform.right, targetDeRotated * Vector3.right) *
                              Math.Sign(Vector3.Dot(targetDeRotated * Vector3.right, vesselTransform.forward));

            // From here everything should use MOI order for Vectors (pitch, roll, yaw)

            _error = new Vector3d(
                -rotDirection.y * UtilMath.Deg2Rad,
                rollError * UtilMath.Deg2Rad,
                rotDirection.x * UtilMath.Deg2Rad
            );

            _error.Scale(Ac.AxisControl);

            Vector3d err = _error + Ac.inertia;
            err = new Vector3d(
                Math.Max(-Math.PI, Math.Min(Math.PI, err.x)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.y)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.z)));

            // ( MoI / available torque ) factor:
            var normFactor = Vector3d.Scale(Ac.VesselState.MoI, Ac.torque.InvertNoNaN());

            err.Scale(normFactor);

            // angular velocity:
            Vector3d omega = Ac.Vessel.angularVelocity;
            //omega.x = vessel.angularVelocity.x;
            //omega.y = vessel.angularVelocity.z; // y <=> z
            //omega.z = vessel.angularVelocity.y; // z <=> y
            omega.Scale(normFactor);

            if (TfAutoTune)
                TuneTf(Ac.torque);
            SetPIDParameters();

            // angular velocity limit:
            var wlimit = new Vector3d(Math.Sqrt(normFactor.x * Math.PI * KWlimit),
                Math.Sqrt(normFactor.y * Math.PI * KWlimit),
                Math.Sqrt(normFactor.z * Math.PI * KWlimit));

            _pidAction = _pid.Compute(err, omega, wlimit);

            // deadband
            _pidAction.x = Math.Abs(_pidAction.x) >= Deadband ? _pidAction.x : 0.0;
            _pidAction.y = Math.Abs(_pidAction.y) >= Deadband ? _pidAction.y : 0.0;
            _pidAction.z = Math.Abs(_pidAction.z) >= Deadband ? _pidAction.z : 0.0;

            // low pass filter,  wf = 1/Tf:
            act = _lastAct;
            if (LowPassFilter)
            {
                act.x += (_pidAction.x - _lastAct.x) * (1.0 / (_tfV.x / TimeWarp.fixedDeltaTime + 1.0));
                act.y += (_pidAction.y - _lastAct.y) * (1.0 / (_tfV.y / TimeWarp.fixedDeltaTime + 1.0));
                act.z += (_pidAction.z - _lastAct.z) * (1.0 / (_tfV.z / TimeWarp.fixedDeltaTime + 1.0));
            }
            else
            {
                act = _pidAction;
            }

            _lastAct = act;

            deltaEuler = _error * UtilMath.Rad2Deg;
        }

        public override void GUI()
        {
            if (GUILayout.Button(Localizer.Format("#MechJeb_adv_reset_button"))) //"Reset"
            {
                ResetConfig();
            }

            TfAutoTune = GUILayout.Toggle(TfAutoTune, Localizer.Format("#MechJeb_AttitudeController_checkbox1")); //" Auto-tuning"

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.BeginVertical();

            if (!TfAutoTune)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label1")); //"Larger ship do better with a larger Tf"

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label2"), GUILayout.ExpandWidth(true));  //"Tf (s)"
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label3"), GUILayout.ExpandWidth(false)); //"P"
                _uiTfX.Text = GUILayout.TextField(_uiTfX.Text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label4"), GUILayout.ExpandWidth(false)); //"Y"
                _uiTfY.Text = GUILayout.TextField(_uiTfY.Text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label5"), GUILayout.ExpandWidth(false)); //"R"
                _uiTfZ.Text = GUILayout.TextField(_uiTfZ.Text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
                GUILayout.EndHorizontal();

                _uiTfX = Math.Max(0.01, _uiTfX);
                _uiTfY = Math.Max(0.01, _uiTfY);
                _uiTfZ = Math.Max(0.01, _uiTfZ);
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label6"), GUILayout.ExpandWidth(true)); //"Tf"
                GUILayout.Label(MuUtils.PrettyPrint(_tfV), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label7"), GUILayout.ExpandWidth(true)); //"Tf range"
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AttitudeController_label8"), _uiTfMin, "", 50);     //"min"
                _uiTfMin = Math.Max(_uiTfMin, 0.01);
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AttitudeController_label9"), _uiTfMax, "", 50); //"max"
                _uiTfMax = Math.Max(_uiTfMax, 0.01);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            bool newLowPassFilter = GUILayout.Toggle(LowPassFilter, Localizer.Format("#MechJeb_AttitudeController_checkbox2")); //" Low Pass Filter"

            if (LowPassFilter != newLowPassFilter)
            {
                SetPIDParameters();
                LowPassFilter = newLowPassFilter;
            }

            GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_PIDF")); //"PID factors"
            GuiUtils.SimpleTextBox("Kd = ", _uiKdFactor, " / Tf", 50);             //
            _uiKdFactor = Math.Max(_uiKdFactor, 0.01);
            GuiUtils.SimpleTextBox("Kp = pid.Kd / (", _uiKpFactor, " * Math.Sqrt(2) * Tf)", 50);
            _uiKpFactor = Math.Max(_uiKpFactor, 0.01);
            GuiUtils.SimpleTextBox("Ki = pid.Kp / (", _uiKiFactor, " * Math.Sqrt(2) * Tf)", 50);
            _uiKiFactor = Math.Max(_uiKiFactor, 0.01);
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AttitudeController_PIDFactor1"), _uiDeadband, "", 50); //"Deadband = "
            Deadband = Math.Max(_uiDeadband, 0.0);

            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AttitudeController_label11"), KWlimit, "%"); //"Maximum Relative Angular Velocity"
            double tmpKWlimit = KWlimit;
            tmpKWlimit = (EditableDouble)GUILayout.HorizontalSlider((float)tmpKWlimit, 0.0F, 1.0F);

            const int SLIDER_PRECISION = 3;
            if (Math.Round(Math.Abs(tmpKWlimit - KWlimit), SLIDER_PRECISION) > 0)
            {
                KWlimit = Math.Round(tmpKWlimit, SLIDER_PRECISION);
            }

            //showInfos = GUILayout.Toggle(showInfos, "Show Numbers");
            //if (showInfos)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label12"), GUILayout.ExpandWidth(true)); //"Kp"
                GUILayout.Label(MuUtils.PrettyPrint(_pid.Kp), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label13"), GUILayout.ExpandWidth(true)); //"Ki"
                GUILayout.Label(MuUtils.PrettyPrint(_pid.Ki), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label14"), GUILayout.ExpandWidth(true)); //"Kd"
                GUILayout.Label(MuUtils.PrettyPrint(_pid.Kd), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label15"), GUILayout.ExpandWidth(true)); //"Error"
                GUILayout.Label(MuUtils.PrettyPrint(_error * Mathf.Rad2Deg), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label16"), GUILayout.ExpandWidth(true)); //"prop. action."
                GUILayout.Label(MuUtils.PrettyPrint(_pid.PropAct), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label17"), GUILayout.ExpandWidth(true)); //"deriv. action"
                GUILayout.Label(MuUtils.PrettyPrint(_pid.DerivativeAct), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label18"), GUILayout.ExpandWidth(true)); //"integral action."
                GUILayout.Label(MuUtils.PrettyPrint(_pid.INTAccum), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label19"), GUILayout.ExpandWidth(true)); //"PID Action"
                GUILayout.Label(MuUtils.PrettyPrint(_pidAction), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label20"), GUILayout.ExpandWidth(true)); //"Inertia"
                GUILayout.Label("|" + Ac.inertia.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(Ac.inertia),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }

            if (!TfAutoTune)
            {
                if (_tfV.x != _uiTfX || _tfV.y != _uiTfY || _tfV.z != _uiTfZ)
                {
                    _tfV.x = _uiTfX;
                    _tfV.y = _uiTfY;
                    _tfV.z = _uiTfZ;
                    SetPIDParameters();
                }
            }
            else
            {
                if (TfMin != _uiTfMin || TfMax != _uiTfMax)
                {
                    TfMin = _uiTfMin;
                    TfMax = _uiTfMax;
                    SetPIDParameters();
                }
            }

            if (KpFactor != _uiKpFactor || KiFactor != _uiKiFactor || KdFactor != _uiKdFactor)
            {
                KpFactor = _uiKpFactor;
                KiFactor = _uiKiFactor;
                KdFactor = _uiKdFactor;
                SetPIDParameters();
            }
        }
    }
}
