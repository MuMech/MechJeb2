extern alias JetBrainsAnnotations;
using System;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using MechJebLib.Control;
using UnityEngine;

namespace MuMech.AttitudeControllers
{
    internal class BetterController : BaseAttitudeController
    {
        private const int SETTINGS_VERSION = 8;

        private const double EPS = 2.2204e-16;

        private const double POS_KP_DEFAULT = 1.98;
        private const double POS_DEADBAND_DEFAULT = 0.002;
        private const double VEL_KP_DEFAULT = 18.9;
        private const double VEL_TI_DEFAULT = 8.85;
        private const double VEL_TD_DEFAULT = 0.847;
        private const double VEL_N_DEFAULT = 84.1994541201249;
        private const double VEL_B_DEFAULT = 1.0;
        private const double VEL_C_DEFAULT = 1.0;
        private const double VEL_DEADBAND_DEFAULT = 0.0001;
        private const double VEL_SMOOTH_IN_DEFAULT = 1.0;
        private const double VEL_SMOOTH_OUT_DEFAULT = 1.0;
        private const double POS_SMOOTH_IN_DEFAULT = 1.0;
        private const double MAX_STOPPING_TIME_DEFAULT = 2;
        private const double MIN_FLIP_TIME_DEFAULT = 120;
        private const double ROLL_CONTROL_RANGE_DEFAULT = 5;
        private const double VEL_FORE_TERM_DEFAULT = -1.0;
        private const bool VEL_FORE_DEFAULT = false;

        private static readonly Vector3d _naN = new Vector3d(double.NaN, double.NaN, double.NaN);

        private readonly PIDLoop2[] _pid = { new PIDLoop2(), new PIDLoop2(), new PIDLoop2() };

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MaxStoppingTime = new EditableDouble(MAX_STOPPING_TIME_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MinFlipTime = new EditableDouble(MIN_FLIP_TIME_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosDeadband = new EditableDouble(POS_DEADBAND_DEFAULT);

        /* FIXME: when you do that look at ModuleGimbal gimbalResponseSpeed and model the time delay and use the XLR11 since it has slow gimbal */
        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosKp = new EditableDouble(POS_KP_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosSmoothIn = new EditableDouble(POS_SMOOTH_IN_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble RollControlRange = new EditableDouble(ROLL_CONTROL_RANGE_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelB = new EditableDouble(VEL_B_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelC = new EditableDouble(VEL_C_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelDeadband = new EditableDouble(VEL_DEADBAND_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelKp = new EditableDouble(VEL_KP_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelN = new EditableDouble(VEL_N_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelSmoothIn = new EditableDouble(VEL_SMOOTH_IN_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelSmoothOut = new EditableDouble(VEL_SMOOTH_OUT_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelTd = new EditableDouble(VEL_TD_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelTi = new EditableDouble(VEL_TI_DEFAULT);

        private Vector3d _actuation = Vector3d.zero;

        /* error in pitch, roll, yaw */
        private Vector3d _error0 = Vector3d.zero;
        private Vector3d _error1 = _naN;

        /* error */
        private double _errorTotal;

        /* max angular acceleration */
        private Vector3d _maxAlpha = Vector3d.zero;

        /* max angular rotation */
        private Vector3d _maxOmega = Vector3d.zero;
        private Vector3d _omega0 = _naN;
        private Vector3d _targetOmega = Vector3d.zero;
        private Vector3d _targetTorque = Vector3d.zero;

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool UseControlRange = true;

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool UseFlipTime = true;

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool UseStoppingTime = true;

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool VelFORE = VEL_FORE_DEFAULT;

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public EditableDouble VelFORETerm = VEL_FORE_TERM_DEFAULT;

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public int Version = -1;

        public BetterController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        private Vessel _vessel => Ac.Vessel;

        private void Defaults()
        {
            PosKp.Val = POS_KP_DEFAULT;
            PosDeadband.Val = POS_DEADBAND_DEFAULT;
            VelKp.Val = VEL_KP_DEFAULT;
            VelTi.Val = VEL_TI_DEFAULT;
            VelTd.Val = VEL_TD_DEFAULT;
            VelN.Val = VEL_N_DEFAULT;
            VelB.Val = VEL_B_DEFAULT;
            VelC.Val = VEL_C_DEFAULT;
            VelDeadband.Val = VEL_DEADBAND_DEFAULT;
            VelSmoothIn.Val = VEL_SMOOTH_IN_DEFAULT;
            VelSmoothOut.Val = VEL_SMOOTH_OUT_DEFAULT;
            VelFORE = VEL_FORE_DEFAULT;
            VelFORETerm.Val = VEL_FORE_TERM_DEFAULT;
            PosSmoothIn.Val = POS_SMOOTH_IN_DEFAULT;
            MaxStoppingTime.Val = MAX_STOPPING_TIME_DEFAULT;
            MinFlipTime.Val = MIN_FLIP_TIME_DEFAULT;
            RollControlRange.Val = ROLL_CONTROL_RANGE_DEFAULT;
            Version = SETTINGS_VERSION;
        }

        public override void OnModuleEnabled()
        {
            if (Version < SETTINGS_VERSION)
                Defaults();
            Reset();
        }

        public override void DrivePre(FlightCtrlState s, out Vector3d act, out Vector3d deltaEuler)
        {
            UpdatePredictionPI();

            deltaEuler = -_error0 * Mathf.Rad2Deg;

            for (int i = 0; i < 3; i++)
                if (Math.Abs(_actuation[i]) < EPS || double.IsNaN(_actuation[i]))
                    _actuation[i] = 0;

            act = _actuation;
        }

        private void UpdateError()
        {
            Transform vesselTransform = _vessel.ReferenceTransform;

            // 1. The Euler(-90) here is because the unity transform puts "up" as the pointy end, which is wrong.  The rotation means that
            // "forward" becomes the pointy end, and "up" and "right" correctly define e.g. AoA/pitch and AoS/yaw.  This is just KSP being KSP.
            // 2. We then use the inverse ship rotation to transform the requested attitude into the ship frame (we do everything in the ship frame
            // first, and then negate the error to get the error in the target reference frame at the end).
            Quaternion deltaRotation =
                Quaternion.Inverse(vesselTransform.transform.rotation * Quaternion.Euler(-90, 0, 0)) *
                Ac.RequestedAttitude;

            // get us some euler angles for the target transform
            Vector3d ea = deltaRotation.eulerAngles;
            double pitch = ea[0] * UtilMath.Deg2Rad;
            double yaw = ea[1] * UtilMath.Deg2Rad;
            double roll = ea[2] * UtilMath.Deg2Rad;

            // law of cosines for the "distance" of the miss in radians
            _errorTotal = Math.Acos(MuUtils.Clamp(Math.Cos(pitch) * Math.Cos(yaw), -1, 1));

            // we assemble phi in the pitch, roll, -yaw basis that KSP uses
            var phi = new Vector3d(
                MuUtils.ClampRadiansPi(pitch), // pitch distance around the geodesic
                MuUtils.ClampRadiansPi(roll),
                MuUtils.ClampRadiansPi(-yaw) // yaw distance around the geodesic
            );

            // apply the axis control from the parent controller
            phi.Scale(Ac.AxisControl);

            // the error in the ship's position is the negative of the reference position in the ship frame
            _error0 = -phi;
        }

        private void UpdatePredictionPI()
        {
            _omega0 = _vessel.angularVelocityD;

            UpdateError();

            // low-pass filter on the error input
            _error0 = _error1.IsFinite() ? _error1 + PosSmoothIn * (_error0 - _error1) : _error0;

            Vector3d controlTorque = Ac.torque;

            // needed to stop wiggling at higher phys warp
            double warpFactor = Ac.VesselState.deltaT / 0.02;

            // see https://archive.is/NqoUm and the "Alt Hold Controller", the acceleration PID is not implemented, so we only
            // have the first two PIDs in the cascade.
            for (int i = 0; i < 3; i++)
            {
                double error = _error0[i];

                if (Math.Abs(error) < PosDeadband)
                    error = 0;
                else
                    error -= Math.Sign(error) * PosDeadband;

                _maxAlpha[i] = controlTorque[i] / _vessel.MOI[i];

                if (_maxAlpha[i] == 0)
                    _maxAlpha[i] = 1;

                if (Ac.OmegaTarget[i].IsFinite())
                {
                    _targetOmega[i] = Ac.OmegaTarget[i];
                }
                else
                {
                    double posKp = PosKp / warpFactor;
                    double effLD = _maxAlpha[i] / (2 * posKp * posKp);

                    if (Math.Abs(error) <= 2 * effLD)
                    {
                        // linear ramp down of acceleration
                        _targetOmega[i] = -posKp * error;
                    }
                    else
                    {
                        // v = - sqrt(2 * F * x / m) is target stopping velocity based on distance
                        _targetOmega[i] = -Math.Sqrt(2 * _maxAlpha[i] * (Math.Abs(error) - effLD)) * Math.Sign(error);
                    }

                    if (UseStoppingTime)
                    {
                        _maxOmega[i] = _maxAlpha[i] * MaxStoppingTime;
                        if (UseFlipTime) _maxOmega[i] = Math.Max(_maxOmega[i], Math.PI / MinFlipTime);
                        _targetOmega[i] = MuUtils.Clamp(_targetOmega[i], -_maxOmega[i], _maxOmega[i]);
                    }

                    if (UseControlRange && _errorTotal * Mathf.Rad2Deg > RollControlRange)
                        _targetOmega[1] = 0;
                }

                _pid[i].Kp = VelKp / (_maxAlpha[i] * warpFactor);
                _pid[i].Ti = VelTi * warpFactor;
                _pid[i].Td = VelTd * warpFactor;
                _pid[i].N = VelN;
                _pid[i].B = VelB;
                _pid[i].C = VelC;
                _pid[i].H = Ac.VesselState.deltaT;
                _pid[i].SmoothIn = MuUtils.Clamp01(VelSmoothIn);
                _pid[i].SmoothOut = MuUtils.Clamp01(VelSmoothOut);
                _pid[i].MinOutput = -1;
                _pid[i].MaxOutput = 1;
                _pid[i].IntegralDeadband = VelDeadband;
                _pid[i].FORE = VelFORE;
                _pid[i].FORETerm = VelFORETerm;

                // need the negative from the pid due to KSP's orientation of actuation
                _actuation[i] = -_pid[i].Update(_targetOmega[i], _omega0[i]);

                if (Math.Abs(_actuation[i]) < EPS || double.IsNaN(_actuation[i]))
                    _actuation[i] = 0;

                _targetTorque[i] = _actuation[i] * Ac.torque[i];

                if (Ac.ActuationControl[i] == 0)
                    Reset(i);
            }

            _error1 = _error0;
        }

        public override void Reset()
        {
            Reset(0);
            Reset(1);
            Reset(2);
        }

        public override void Reset(int i)
        {
            _pid[i].Reset();
            _omega0[i] = _error0[i] = _error1[i] = double.NaN;
        }

        public override void GUI()
        {
            GUILayout.BeginHorizontal();
            UseStoppingTime = GUILayout.Toggle(UseStoppingTime, "Maximum Stopping Time", GUILayout.ExpandWidth(false));
            MaxStoppingTime.Text =
                GUILayout.TextField(MaxStoppingTime.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            UseFlipTime = GUILayout.Toggle(UseFlipTime, "Minimum Flip Time", GUILayout.ExpandWidth(false));
            MinFlipTime.Text = GUILayout.TextField(MinFlipTime.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            if (!UseStoppingTime)
                UseFlipTime = false;

            GUILayout.BeginHorizontal();
            UseControlRange = GUILayout.Toggle(UseControlRange, Localizer.Format("#MechJeb_HybridController_checkbox2"),
                GUILayout.ExpandWidth(false)); //"RollControlRange"
            RollControlRange.Text =
                GUILayout.TextField(RollControlRange.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos SmoothIn", GUILayout.ExpandWidth(false));
            PosSmoothIn.Text = GUILayout.TextField(PosSmoothIn.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos Kp", GUILayout.ExpandWidth(false));
            PosKp.Text = GUILayout.TextField(PosKp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos Deadband", GUILayout.ExpandWidth(false));
            PosDeadband.Text = GUILayout.TextField(PosDeadband.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel Kp", GUILayout.ExpandWidth(false));
            VelKp.Text = GUILayout.TextField(VelKp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel Ti", GUILayout.ExpandWidth(false));
            VelTi.Text = GUILayout.TextField(VelTi.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel Td", GUILayout.ExpandWidth(false));
            VelTd.Text = GUILayout.TextField(VelTd.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel N", GUILayout.ExpandWidth(false));
            VelN.Text = GUILayout.TextField(VelN.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel B", GUILayout.ExpandWidth(false));
            VelB.Text = GUILayout.TextField(VelB.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel C", GUILayout.ExpandWidth(false));
            VelC.Text = GUILayout.TextField(VelC.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel Deadband", GUILayout.ExpandWidth(false));
            VelDeadband.Text = GUILayout.TextField(VelDeadband.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            VelFORE = GUILayout.Toggle(VelFORE, Localizer.Format("Vel FORE"), GUILayout.ExpandWidth(false));
            VelFORETerm.Text = GUILayout.TextField(VelFORETerm.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel SmoothIn", GUILayout.ExpandWidth(false));
            VelSmoothIn.Text = GUILayout.TextField(VelSmoothIn.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel SmoothOut", GUILayout.ExpandWidth(false));
            VelSmoothOut.Text =
                GUILayout.TextField(VelSmoothOut.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel PTerm", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(new Vector3d(_pid[0].PTerm, _pid[1].PTerm, _pid[2].PTerm)),
                GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel ITerm", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(new Vector3d(_pid[0].ITerm, _pid[1].ITerm, _pid[2].ITerm)),
                GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel DTerm", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(new Vector3d(_pid[0].DTerm, _pid[1].DTerm, _pid[2].DTerm)),
                GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("Reset Tuning Values")))
                Defaults();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Error", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_error0), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("TargetOmega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_targetOmega), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Omega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_omega0), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MaxOmega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_maxOmega), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label2"),
                GUILayout.ExpandWidth(true)); //"Actuation"
            GUILayout.Label(MuUtils.PrettyPrint(_actuation), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label4"),
                GUILayout.ExpandWidth(true)); //"TargetTorque"
            GUILayout.Label(MuUtils.PrettyPrint(_targetTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label5"),
                GUILayout.ExpandWidth(true)); //"ControlTorque"
            GUILayout.Label(MuUtils.PrettyPrint(Ac.torque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MaxAlpha", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_maxAlpha), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MOI", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_vessel.MOI), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }
}
