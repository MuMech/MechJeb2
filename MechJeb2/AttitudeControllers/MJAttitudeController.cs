extern alias JetBrainsAnnotations;
using System;
using JetBrainsAnnotations::JetBrains.Annotations;
using UnityEngine;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace MuMech.AttitudeControllers
{
    internal class MJAttitudeController : BaseAttitudeController
    {
        private PIDControllerV3 _pid;

        private Vector3d _lastAct = Vector3d.zero;
        private Vector3d _pidAction; //info
        internal Vector3d _error; //info

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public bool TfAutoTune = true;

        internal Vector3d _tfV = new Vector3d(0.3, 0.3, 0.3);

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public Vector3 TfVec = new Vector3(0.3f, 0.3f, 0.3f); // use the serialize since Vector3d does not

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public double TfMin = 0.1;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public double TfMax = 0.5;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public bool LowPassFilter = true;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public double KpFactor = 3;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public double KiFactor = 6;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public double KdFactor = 0.5;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public double Deadband = 0.0001;

        //Lower value of "kWlimit" reduces maximum angular velocity
        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble KWlimit = 0.15;

        private readonly Vector3d _defaultTfV = new Vector3d(0.3, 0.3, 0.3);

        // GUI
        internal EditableDouble _uiTfX;
        internal EditableDouble _uiTfY;
        internal EditableDouble _uiTfZ;
        internal EditableDouble _uiTfMin;
        internal EditableDouble _uiTfMax;
        internal EditableDouble _uiKpFactor;
        internal EditableDouble _uiKiFactor;
        internal EditableDouble _uiKdFactor;
        internal EditableDouble _uiDeadband;

        public MJAttitudeController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        public override void OnStart()
        {
            _pid = new PIDControllerV3(Vector3d.zero, Vector3d.zero, Vector3d.zero, 1, -1);
            SetPIDParameters();
            _lastAct = Vector3d.zero;
            CopyToUi();
        }

        // the values the window edits, from the live ones
        private void CopyToUi()
        {
            _uiTfX = new EditableDouble(_tfV.x);
            _uiTfY = new EditableDouble(_tfV.y);
            _uiTfZ = new EditableDouble(_tfV.z);
            _uiTfMin = new EditableDouble(TfMin);
            _uiTfMax = new EditableDouble(TfMax);
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

        // The window edits copies of these values, and an edit reaches the controller here, as MechJeb2.UI's panel syncs
        internal void ApplyUiValues()
        {
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
            else if (TfMin != _uiTfMin || TfMax != _uiTfMax)
            {
                TfMin = _uiTfMin;
                TfMax = _uiTfMax;
                SetPIDParameters();
            }

            if (KpFactor != _uiKpFactor || KiFactor != _uiKiFactor || KdFactor != _uiKdFactor)
            {
                KpFactor = _uiKpFactor;
                KiFactor = _uiKiFactor;
                KdFactor = _uiKdFactor;
                SetPIDParameters();
            }
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

            Vector3d delayFactor = Vector3d.one + 2 * Ac.VesselState.TorqueReactionSpeed;


            _tfV.Scale(delayFactor);


            _tfV = _tfV.Clamp(2.0 * TimeWarp.fixedDeltaTime, TfMax);
            _tfV = _tfV.Clamp(TfMin, TfMax);

            //Tf = Mathf.Clamp((float)ratio.magnitude / 20f, 2 * TimeWarp.fixedDeltaTime, 1f);
            //Tf = Mathf.Clamp((float)Tf, (float)TfMin, (float)TfMax);
        }

        public override void ResetConfig()
        {
            _tfV = _defaultTfV;
            TfMin = 0.1;
            TfMax = 0.5;
            KpFactor = 3;
            KiFactor = 6;
            KdFactor = 0.5;
            Deadband = 0.0001;
            KWlimit = 0.15;
            CopyToUi();
        }

        public override void Reset() => _pid.Reset();

        public override void DrivePre(FlightCtrlState s, out Vector3d act, out Vector3d deltaEuler)
        {
            Transform vesselTransform = Ac.Vessel.ReferenceTransform;

            // Find out the real shorter way to turn where we wan to.
            // Thanks to HoneyFox
            Vector3d tgtLocalUp = (QuaternionD)vesselTransform.transform.rotation.Inverse() * Ac.RequestedAttitude * Vector3d.forward;
            Vector3d curLocalUp = Vector3d.up;

            double turnAngle = Math.Abs(Vector3d.Angle(curLocalUp, tgtLocalUp));
            var rotDirection = new Vector2d(tgtLocalUp.x, tgtLocalUp.z);
            rotDirection = rotDirection.normalized * turnAngle;

            // And the lowest roll
            // Thanks to Crzyrndm
            var normVec = Vector3.Cross(Ac.RequestedAttitude * Vector3.forward, vesselTransform.up);
            QuaternionD targetDeRotated = QuaternionD.AngleAxis((float)turnAngle, normVec) * Ac.RequestedAttitude;
            float rollError = Vector3.Angle(vesselTransform.right, targetDeRotated * Vector3.right) *
                Math.Sign(Vector3.Dot(targetDeRotated * Vector3.right, vesselTransform.forward));

            // From here everything should use MOI order for Vectors (pitch, roll, yaw)

            _error = new Vector3d(
                -rotDirection.y * UtilMath.Deg2Rad,
                rollError * UtilMath.Deg2Rad,
                rotDirection.x * UtilMath.Deg2Rad
            );

            _error.Scale(Ac.AxisControl);

            Vector3d err = _error + Ac.Inertia;
            err = new Vector3d(
                Math.Max(-Math.PI, Math.Min(Math.PI, err.x)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.y)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.z)));

            // ( MoI / available torque ) factor:
            var normFactor = Vector3d.Scale(Ac.VesselState.MoI, Ac.Torque.InvertNoNaN());

            err.Scale(normFactor);

            // angular velocity:
            Vector3d omega = Ac.Vessel.angularVelocity;
            //omega.x = vessel.angularVelocity.x;
            //omega.y = vessel.angularVelocity.z; // y <=> z
            //omega.z = vessel.angularVelocity.y; // z <=> y
            omega.Scale(normFactor);

            if (TfAutoTune)
                TuneTf(Ac.Torque);
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

        // the clamps below are shared by the IMGUI and the uGUI panels; they only replace a value that is too small,
        // so the text being edited is not rewritten while it is valid
        internal void ClampTf()
        {
            if (_uiTfX < 0.01) _uiTfX = 0.01;
            if (_uiTfY < 0.01) _uiTfY = 0.01;
            if (_uiTfZ < 0.01) _uiTfZ = 0.01;
        }

        internal void ClampTfRange()
        {
            if (_uiTfMin < 0.01) _uiTfMin = 0.01;
            if (_uiTfMax < 0.01) _uiTfMax = 0.01;
        }

        internal void ClampFactors()
        {
            if (_uiKdFactor < 0.01) _uiKdFactor = 0.01;
            if (_uiKpFactor < 0.01) _uiKpFactor = 0.01;
            if (_uiKiFactor < 0.01) _uiKiFactor = 0.01;
        }

        internal void ClampDeadband() => Deadband = Math.Max(_uiDeadband, 0.0);

        internal void SetLowPassFilter(bool newLowPassFilter)
        {
            if (LowPassFilter != newLowPassFilter)
            {
                SetPIDParameters();
                LowPassFilter = newLowPassFilter;
            }
        }

        // This is tightly coupled to the MJAttitudeController, and should not be used by other consumers.
        private class PIDControllerV3
        {
            public Vector3d Kp, Ki, Kd, INTAccum, DerivativeAct, PropAct;
            private readonly double _max;
            private readonly double _min;

            public PIDControllerV3(Vector3d kp, Vector3d ki, Vector3d kd, double max = double.MaxValue, double min = double.MinValue)
            {
                Kp = kp;
                Ki = ki;
                Kd = kd;
                _max = max;
                _min = min;
                Reset();
            }

            public Vector3d Compute(Vector3d error, Vector3d omega, Vector3d wlimit)
            {
                DerivativeAct = Vector3d.Scale(omega, Kd);
                wlimit = Vector3d.Scale(wlimit, Kd);

                // integral actíon + Anti Windup
                INTAccum.x = Math.Abs(DerivativeAct.x) < 0.6 * _max ? INTAccum.x + error.x * Ki.x * TimeWarp.fixedDeltaTime : 0.9 * INTAccum.x;
                INTAccum.y = Math.Abs(DerivativeAct.y) < 0.6 * _max ? INTAccum.y + error.y * Ki.y * TimeWarp.fixedDeltaTime : 0.9 * INTAccum.y;
                INTAccum.z = Math.Abs(DerivativeAct.z) < 0.6 * _max ? INTAccum.z + error.z * Ki.z * TimeWarp.fixedDeltaTime : 0.9 * INTAccum.z;

                PropAct = Vector3d.Scale(error, Kp);

                Vector3d action = PropAct + INTAccum;

                // Clamp (propAct + intAccum) to limit the angular velocity:
                action = new Vector3d(Math.Max(-wlimit.x, Math.Min(wlimit.x, action.x)),
                    Math.Max(-wlimit.y, Math.Min(wlimit.y, action.y)),
                    Math.Max(-wlimit.z, Math.Min(wlimit.z, action.z)));

                // add. derivative action
                action += DerivativeAct;

                // action clamp
                action = new Vector3d(Math.Max(_min, Math.Min(_max, action.x)),
                    Math.Max(_min, Math.Min(_max, action.y)),
                    Math.Max(_min, Math.Min(_max, action.z)));
                return action;
            }

            public void Reset() => INTAccum = Vector3d.zero;
        }
    }
}
