extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using MechJebLib.Control;
using UnityEngine;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MuMech.AttitudeControllers
{
    internal class BetterController : BaseAttitudeController
    {
        private const int SETTINGS_VERSION = 16;

        private const double POS_KP_DEFAULT = 2.03;
        private const double POS_TI_DEFAULT = 1.97;
        private const double POS_TD_DEFAULT = 0.0;
        private const double POS_N_DEFAULT = 1.0;
        private const double POS_B_DEFAULT = 1.0;
        private const double POS_C_DEFAULT = 1.0;
        private const double POS_DEADBAND_DEFAULT = 0.0;
        private const bool POS_CLEGG_DEFAULT = false;
        private const double POS_SMOOTH_IN_DEFAULT = 1.0;
        private const double POS_SMOOTH_OUT_DEFAULT = 1.0;

        private const double VEL_KP_DEFAULT = 7.98;
        private const double VEL_TI_DEFAULT = 0;
        private const double VEL_TD_DEFAULT = 0;
        private const double VEL_N_DEFAULT = 1.0;
        private const double VEL_B_DEFAULT = 1.0;
        private const double VEL_C_DEFAULT = 1.0;
        private const double VEL_DEADBAND_DEFAULT = 0.0;
        private const bool VEL_CLEGG_DEFAULT = false;
        private const double VEL_SMOOTH_IN_DEFAULT = 1.0;
        private const double VEL_SMOOTH_OUT_DEFAULT = 1.0;

        private const double MAX_STOPPING_TIME_DEFAULT = 2;
        private const double MIN_FLIP_TIME_DEFAULT = 120;
        private const double ROLL_CONTROL_RANGE_DEFAULT = 5;
        private const double SMOOTH_TORQUE_DEFAULT = 0.10;
        private const double SOFTEN_DEFAULT = 0.5;

        internal readonly PIDLoop2[] _velPID = { new PIDLoop2(), new PIDLoop2(), new PIDLoop2() };
        internal readonly PIDLoop2[] _posPID = { new PIDLoop2(), new PIDLoop2(), new PIDLoop2() };
        private readonly DirectionTracker _directionTracker = new DirectionTracker();

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MaxStoppingTime = new EditableDouble(MAX_STOPPING_TIME_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MinFlipTime = new EditableDouble(MIN_FLIP_TIME_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosDeadband = new EditableDouble(POS_DEADBAND_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosKp = new EditableDouble(POS_KP_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosTi = new EditableDouble(POS_TI_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosTd = new EditableDouble(POS_TD_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosN = new EditableDouble(POS_N_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosB = new EditableDouble(POS_B_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosC = new EditableDouble(POS_C_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool PosClegg = POS_CLEGG_DEFAULT;

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosSmoothIn = new EditableDouble(POS_SMOOTH_IN_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosSmoothOut = new EditableDouble(POS_SMOOTH_OUT_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble RollControlRange = new EditableDouble(ROLL_CONTROL_RANGE_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelB = new EditableDouble(VEL_B_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelC = new EditableDouble(VEL_C_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelDeadband = new EditableDouble(VEL_DEADBAND_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelKp = new EditableDouble(VEL_KP_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelN = new EditableDouble(VEL_N_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelSmoothIn = new EditableDouble(VEL_SMOOTH_IN_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelSmoothOut = new EditableDouble(VEL_SMOOTH_OUT_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelTd = new EditableDouble(VEL_TD_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VelTi = new EditableDouble(VEL_TI_DEFAULT);

        // Soften should run between (0,1] to reduce overshoot on large angle maneuvers
        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble Soften = new EditableDouble(SOFTEN_DEFAULT);

        internal Vector3d _actuation = Vector3d.zero;

        /* error in pitch, roll, yaw */
        internal Vector3d _error = Vector3d.zero;
        internal Vector3d _current = Vector3d.zero;
        internal Vector3d _desired = Vector3d.zero;

        /* error */
        private double _distance;

        /* max angular acceleration */
        internal Vector3d _maxAlpha = Vector3d.zero;

        /* max angular rotation */
        internal Vector3d _targetOmega = Vector3d.zero;
        internal Vector3d _targetAlpha = Vector3d.zero;
        internal Vector3d _targetTorque = Vector3d.zero;
        internal Vector3d _controlTorque = Vector3d.zero;

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble SmoothTorque = new EditableDouble(SMOOTH_TORQUE_DEFAULT);

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool UseControlRange = true;

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool UseFlipTime = true;

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool UseStoppingTime = true;

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool VelClegg = VEL_CLEGG_DEFAULT;

        [UsedImplicitly, Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public int Version = -1;

        public BetterController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        internal Vessel _vessel => Ac.Vessel;

        internal void Defaults()
        {
            // Position PID defaults
            PosKp.Val = POS_KP_DEFAULT;
            PosTi.Val = POS_TI_DEFAULT;
            PosTd.Val = POS_TD_DEFAULT;
            PosN.Val = POS_N_DEFAULT;
            PosB.Val = POS_B_DEFAULT;
            PosC.Val = POS_C_DEFAULT;
            PosDeadband.Val = POS_DEADBAND_DEFAULT;
            PosSmoothOut.Val = POS_SMOOTH_OUT_DEFAULT;
            PosSmoothIn.Val = POS_SMOOTH_IN_DEFAULT;
            PosClegg = POS_CLEGG_DEFAULT;

            // Velocity PID defaults
            VelKp.Val = VEL_KP_DEFAULT;
            VelTi.Val = VEL_TI_DEFAULT;
            VelTd.Val = VEL_TD_DEFAULT;
            VelN.Val = VEL_N_DEFAULT;
            VelB.Val = VEL_B_DEFAULT;
            VelC.Val = VEL_C_DEFAULT;
            VelDeadband.Val = VEL_DEADBAND_DEFAULT;
            VelSmoothIn.Val = VEL_SMOOTH_IN_DEFAULT;
            VelSmoothOut.Val = VEL_SMOOTH_OUT_DEFAULT;
            VelClegg = VEL_CLEGG_DEFAULT;

            // Miscellaneous defaults
            MaxStoppingTime.Val = MAX_STOPPING_TIME_DEFAULT;
            MinFlipTime.Val = MIN_FLIP_TIME_DEFAULT;
            RollControlRange.Val = ROLL_CONTROL_RANGE_DEFAULT;
            UseControlRange = true;
            UseFlipTime = true;
            UseStoppingTime = true;
            SmoothTorque.Val = SMOOTH_TORQUE_DEFAULT;
            Soften.Val = SOFTEN_DEFAULT;

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

            deltaEuler = _error * Mathf.Rad2Deg;

            for (int i = 0; i < 3; i++)
                if (Abs(_actuation[i]) < EPS || double.IsNaN(_actuation[i]))
                    _actuation[i] = 0;

            act = _actuation;
        }

        private void UpdatePredictionPI()
        {
            Transform vesselTransform = _vessel.ReferenceTransform;
            QuaternionD currentAttitude = (QuaternionD)vesselTransform.transform.rotation * MathExtensions.Euler(-90, 0, 0);

            _current = _directionTracker.Update(currentAttitude);
            (_desired, _error, _distance) = _directionTracker.Desired(Ac.RequestedAttitude);

            // low-pass filter the control torque
            _controlTorque = _controlTorque == Vector3d.zero ? Ac.Torque : _controlTorque + SmoothTorque * (Ac.Torque - _controlTorque);

            // if torque is really zero, set it zero
            for (int i = 0; i < 3; i++)
                if (Ac.Torque[i] == 0)
                    _controlTorque[i] = 0;

            // needed to stop wiggling at higher phys warp
            double warpFactor = Ac.VesselState.DeltaT / 0.02;

            // see https://archive.is/NqoUm and the "Alt Hold Controller", the acceleration PID is not implemented, so we only
            // have the first two PIDs in the cascade.
            for (int i = 0; i < 3; i++)
            {
                _maxAlpha[i] = _controlTorque[i] / _vessel.MOI[i];

                if (_maxAlpha[i] == 0)
                    _maxAlpha[i] = 1;

                if (Ac.OmegaTarget[i].IsFinite())
                {
                    _targetOmega[i] = Ac.OmegaTarget[i];
                }
                else
                {
                    double soften = Clamp01(Soften);
                    double posKp = PosKp / warpFactor;
                    double effLD = soften * soften * _maxAlpha[i] / (2 * posKp * posKp);

                    double maxOmega = double.PositiveInfinity;

                    if (UseStoppingTime)
                    {
                        maxOmega = _maxAlpha[i] * MaxStoppingTime;
                        if (UseFlipTime) maxOmega = Max(maxOmega, PI / MinFlipTime.Val);
                    }

                    if (Abs(_error[i]) <= 2 * effLD)
                    {
                        _posPID[i].Kp = posKp;
                        _posPID[i].Ti = PosTi;
                        _posPID[i].Td = PosTd;
                        _posPID[i].N = PosN.Val;
                        _posPID[i].B = PosB.Val;
                        _posPID[i].C = PosC.Val;
                        _posPID[i].Ts = Ac.VesselState.DeltaT;
                        _posPID[i].SmoothIn = Clamp01(PosSmoothIn);
                        _posPID[i].SmoothOut = Clamp01(PosSmoothOut);
                        _posPID[i].MinOutput = -maxOmega;
                        _posPID[i].MaxOutput = maxOmega;
                        _posPID[i].IntegralDeadband = PosDeadband * maxOmega;
                        _posPID[i].Clegg = PosClegg;

                        _targetOmega[i] = _posPID[i].Update(_desired[i], _current[i]);
                    }
                    else
                    {
                        _posPID[i].Reset();
                        // v = - sqrt(2 * F * x / m) is target stopping velocity based on distance
                        _targetOmega[i] = soften * Sqrt(2 * _maxAlpha[i] * (Abs(_error[i]) - effLD)) * Sign(_error[i]);
                        _targetOmega[i] = Clamp(_targetOmega[i], -maxOmega, maxOmega);
                    }

                    if (UseControlRange && _distance * Mathf.Rad2Deg > RollControlRange)
                    {
                        _targetOmega[1] = 0;
                        _posPID[1].Reset();
                    }
                }

                _velPID[i].Kp = VelKp;
                _velPID[i].Ti = VelTi;
                _velPID[i].Td = VelTd;
                _velPID[i].N = VelN;
                _velPID[i].B = VelB;
                _velPID[i].C = VelC;
                _velPID[i].Ts = Ac.VesselState.DeltaT;
                _velPID[i].SmoothIn = Clamp01(VelSmoothIn);
                _velPID[i].SmoothOut = Clamp01(VelSmoothOut);
                _velPID[i].MinOutput = -_maxAlpha[i];
                _velPID[i].MaxOutput = _maxAlpha[i];
                _velPID[i].IntegralDeadband = VelDeadband * _maxAlpha[i];
                _velPID[i].Clegg = VelClegg;

                _targetAlpha[i] = _velPID[i].Update(_targetOmega[i], _vessel.angularVelocityD[i]);

                _targetTorque[i] = _vessel.MOI[i] * _targetAlpha[i];

                // need the negative from the pid due to KSP's orientation of actuation
                _actuation[i] = -_targetTorque[i] / _controlTorque[i];

                if (Ac.ActuationControl[i] == 0 || _controlTorque[i] == 0 || Ac.AxisControl[i] == 0)
                {
                    _actuation[i] = 0;
                    Reset(i);
                }

                if (Abs(_actuation[i]) < EPS || double.IsNaN(_actuation[i]))
                    _actuation[i] = 0;
            }
        }

        public override void Reset()
        {
            Reset(0);
            Reset(1);
            Reset(2);
            _directionTracker.Reset();
        }

        public override void Reset(int i)
        {
            _velPID[i].Reset();
            _posPID[i].Reset();
            _directionTracker.Reset(i);
        }

        // Minimum Flip Time needs Maximum Stopping Time
        internal void FixFlipTime()
        {
            if (!UseStoppingTime)
                UseFlipTime = false;
        }
    }
}
