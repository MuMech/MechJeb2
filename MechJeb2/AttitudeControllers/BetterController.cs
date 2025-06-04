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
        private const int SETTINGS_VERSION = 10;

        private const double POS_KP_DEFAULT         = 2.0;
        private const double POS_TI_DEFAULT         = 0.0;
        private const double POS_TD_DEFAULT         = 0.0;
        private const double POS_N_DEFAULT          = 1.0;
        private const double POS_B_DEFAULT          = 1.0;
        private const double POS_C_DEFAULT          = 1.0;
        private const double POS_DEADBAND_DEFAULT   = 0.002;
        private const double POS_FORE_TERM_DEFAULT  = -1.0;
        private const bool   POS_FORE_DEFAULT       = false;
        private const double POS_SMOOTH_IN_DEFAULT  = 1.0;
        private const double POS_SMOOTH_OUT_DEFAULT = 1.0;

        private const double VEL_KP_DEFAULT         = 10;
        private const double VEL_TI_DEFAULT         = 2.24;
        private const double VEL_TD_DEFAULT         = 0;
        private const double VEL_N_DEFAULT          = 1.0;
        private const double VEL_B_DEFAULT          = 1.0;
        private const double VEL_C_DEFAULT          = 1.0;
        private const double VEL_DEADBAND_DEFAULT   = 0.0001;
        private const double VEL_FORE_TERM_DEFAULT  = -1.0;
        private const bool   VEL_FORE_DEFAULT       = false;
        private const double VEL_SMOOTH_IN_DEFAULT  = 1.0;
        private const double VEL_SMOOTH_OUT_DEFAULT = 1.0;

        private const double MAX_STOPPING_TIME_DEFAULT  = 2;
        private const double MIN_FLIP_TIME_DEFAULT      = 120;
        private const double ROLL_CONTROL_RANGE_DEFAULT = 5;
        private const double SMOOTH_TORQUE_DEFAULT      = 0.10;

        private readonly PIDLoop2[]       _velPID           = { new PIDLoop2(), new PIDLoop2(), new PIDLoop2() };
        private readonly PIDLoop2[]       _posPID           = { new PIDLoop2(), new PIDLoop2(), new PIDLoop2() };
        private readonly DirectionTracker _directionTracker = new DirectionTracker();

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MaxStoppingTime = new EditableDouble(MAX_STOPPING_TIME_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MinFlipTime = new EditableDouble(MIN_FLIP_TIME_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosDeadband = new EditableDouble(POS_DEADBAND_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosKp = new EditableDouble(POS_KP_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosTi = new EditableDouble(POS_TI_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosTd = new EditableDouble(POS_TD_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosN = new EditableDouble(POS_N_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosB = new EditableDouble(POS_B_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosC = new EditableDouble(POS_C_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool PosFORE = POS_FORE_DEFAULT;

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public EditableDouble PosFORETerm = POS_FORE_TERM_DEFAULT;

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosSmoothIn = new EditableDouble(POS_SMOOTH_IN_DEFAULT);

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PosSmoothOut = new EditableDouble(POS_SMOOTH_OUT_DEFAULT);

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
        private Vector3d _error   = Vector3d.zero;
        private Vector3d _current = Vector3d.zero;
        private Vector3d _desired = Vector3d.zero;

        /* error */
        private double _distance;

        /* max angular acceleration */
        private Vector3d _maxAlpha = Vector3d.zero;

        /* max angular rotation */
        private Vector3d _targetOmega   = Vector3d.zero;
        private Vector3d _targetTorque  = Vector3d.zero;
        private Vector3d _controlTorque = Vector3d.zero;

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble SmoothTorque = new EditableDouble(SMOOTH_TORQUE_DEFAULT);

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
            // Position PID defaults
            PosKp.Val        = POS_KP_DEFAULT;
            PosTi.Val        = POS_TI_DEFAULT;
            PosTd.Val        = POS_TD_DEFAULT;
            PosN.Val         = POS_N_DEFAULT;
            PosB.Val         = POS_B_DEFAULT;
            PosC.Val         = POS_C_DEFAULT;
            PosDeadband.Val  = POS_DEADBAND_DEFAULT;
            PosSmoothOut.Val = POS_SMOOTH_OUT_DEFAULT;
            PosSmoothIn.Val  = POS_SMOOTH_IN_DEFAULT;
            PosFORE          = POS_FORE_DEFAULT;
            PosFORETerm.Val  = POS_FORE_TERM_DEFAULT;

            // Velocity PID defaults
            VelKp.Val        = VEL_KP_DEFAULT;
            VelTi.Val        = VEL_TI_DEFAULT;
            VelTd.Val        = VEL_TD_DEFAULT;
            VelN.Val         = VEL_N_DEFAULT;
            VelB.Val         = VEL_B_DEFAULT;
            VelC.Val         = VEL_C_DEFAULT;
            VelDeadband.Val  = VEL_DEADBAND_DEFAULT;
            VelSmoothIn.Val  = VEL_SMOOTH_IN_DEFAULT;
            VelSmoothOut.Val = VEL_SMOOTH_OUT_DEFAULT;
            VelFORE          = VEL_FORE_DEFAULT;
            VelFORETerm.Val  = VEL_FORE_TERM_DEFAULT;

            // Miscellaneous defaults
            MaxStoppingTime.Val  = MAX_STOPPING_TIME_DEFAULT;
            MinFlipTime.Val      = MIN_FLIP_TIME_DEFAULT;
            RollControlRange.Val = ROLL_CONTROL_RANGE_DEFAULT;
            UseControlRange      = true;
            UseFlipTime          = true;
            UseStoppingTime      = true;
            SmoothTorque.Val     = SMOOTH_TORQUE_DEFAULT;

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
            Transform   vesselTransform = _vessel.ReferenceTransform;
            QuaternionD currentAttitude = (QuaternionD)vesselTransform.transform.rotation * MathExtensions.Euler(-90, 0, 0);

            _current                      = _directionTracker.Update(currentAttitude);
            (_desired, _error, _distance) = _directionTracker.Desired(Ac.RequestedAttitude);

            // low-pass filter the control torque
            _controlTorque = _controlTorque == Vector3d.zero ? Ac.torque : _controlTorque + SmoothTorque * (Ac.torque - _controlTorque);

            // if torque is really zero, set it zero
            for (int i = 0; i < 3; i++)
                if (Ac.torque[i] == 0)
                    _controlTorque[i] = 0;

            // needed to stop wiggling at higher phys warp
            double warpFactor = Ac.VesselState.deltaT / 0.02;

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
                    double posKp = PosKp / warpFactor;
                    double effLD = _maxAlpha[i] / (2 * posKp * posKp);

                    double maxOmega = double.PositiveInfinity;

                    if (UseStoppingTime)
                    {
                        maxOmega = _maxAlpha[i] * MaxStoppingTime;
                        if (UseFlipTime) maxOmega = Max(maxOmega, PI / MinFlipTime.Val);
                    }

                    if (Abs(_error[i]) <= 2 * effLD)
                    {
                        Debug.Log($"{i}: PID");
                        _posPID[i].Kp               = posKp;
                        _posPID[i].Ti               = PosTi.Val;
                        _posPID[i].Td               = PosTd.Val;
                        _posPID[i].N                = PosN.Val;
                        _posPID[i].B                = PosB.Val;
                        _posPID[i].C                = PosC.Val;
                        _posPID[i].H                = Ac.VesselState.deltaT;
                        _posPID[i].SmoothIn         = MuUtils.Clamp01(PosSmoothIn);
                        _posPID[i].SmoothOut        = MuUtils.Clamp01(PosSmoothOut);
                        _posPID[i].MinOutput        = -maxOmega;
                        _posPID[i].MaxOutput        = maxOmega;
                        _posPID[i].IntegralDeadband = PosDeadband.Val;
                        _posPID[i].FORE             = PosFORE;
                        _posPID[i].FORETerm         = PosFORETerm.Val;

                        _targetOmega[i] = _posPID[i].Update(_desired[i], _current[i]);
                    }
                    else
                    {
                        Debug.Log($"{i}: distance");
                        _posPID[i].Reset();
                        // v = - sqrt(2 * F * x / m) is target stopping velocity based on distance
                        _targetOmega[i] = Sqrt(2 * _maxAlpha[i] * (Abs(_error[i]) - effLD)) * Sign(_error[i]);
                    }

                    if (UseControlRange && _distance * Mathf.Rad2Deg > RollControlRange)
                    {
                        _targetOmega[1] = 0;
                        _posPID[1].Reset();
                    }
                }

                _velPID[i].Kp               = VelKp / (_maxAlpha[i] * warpFactor);
                _velPID[i].Ti               = VelTi * warpFactor;
                _velPID[i].Td               = VelTd * warpFactor;
                _velPID[i].N                = VelN;
                _velPID[i].B                = VelB;
                _velPID[i].C                = VelC;
                _velPID[i].H                = Ac.VesselState.deltaT;
                _velPID[i].SmoothIn         = MuUtils.Clamp01(VelSmoothIn);
                _velPID[i].SmoothOut        = MuUtils.Clamp01(VelSmoothOut);
                _velPID[i].MinOutput        = -1;
                _velPID[i].MaxOutput        = 1;
                _velPID[i].IntegralDeadband = VelDeadband;
                _velPID[i].FORE             = VelFORE;
                _velPID[i].FORETerm         = VelFORETerm;

                // need the negative from the pid due to KSP's orientation of actuation
                _actuation[i] = -_velPID[i].Update(_targetOmega[i], _vessel.angularVelocityD[i]);

                if (Abs(_actuation[i]) < EPS || double.IsNaN(_actuation[i]))
                    _actuation[i] = 0;

                _targetTorque[i] = _actuation[i] * _controlTorque[i];

                if (Ac.ActuationControl[i] == 0 || _controlTorque[i] == 0 || Ac.AxisControl[i] == 0)
                {
                    _actuation[i] = 0;
                    Reset(i);
                }
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

        public override void GUI()
        {
            GUILayout.BeginHorizontal();
            UseStoppingTime = GUILayout.Toggle(UseStoppingTime, "Maximum Stopping Time", GUILayout.ExpandWidth(false));
            MaxStoppingTime.Text =
                GUILayout.TextField(MaxStoppingTime.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            UseFlipTime      = GUILayout.Toggle(UseFlipTime, "Minimum Flip Time", GUILayout.ExpandWidth(false));
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
            GUILayout.Label("Pos Kp", GUILayout.ExpandWidth(false));
            PosKp.Text = GUILayout.TextField(PosKp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos Ti", GUILayout.ExpandWidth(false));
            PosTi.Text = GUILayout.TextField(PosTi.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos Td", GUILayout.ExpandWidth(false));
            PosTd.Text = GUILayout.TextField(PosTd.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos N", GUILayout.ExpandWidth(false));
            PosN.Text = GUILayout.TextField(PosN.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos B", GUILayout.ExpandWidth(false));
            PosB.Text = GUILayout.TextField(PosB.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos C", GUILayout.ExpandWidth(false));
            PosC.Text = GUILayout.TextField(PosC.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos Deadband", GUILayout.ExpandWidth(false));
            PosDeadband.Text = GUILayout.TextField(PosDeadband.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            PosFORE          = GUILayout.Toggle(PosFORE, Localizer.Format("Pos FORE"), GUILayout.ExpandWidth(false));
            PosFORETerm.Text = GUILayout.TextField(PosFORETerm.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos SmoothIn", GUILayout.ExpandWidth(false));
            PosSmoothIn.Text = GUILayout.TextField(PosSmoothIn.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos SmoothOut", GUILayout.ExpandWidth(false));
            PosSmoothOut.Text = GUILayout.TextField(PosSmoothOut.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos PTerm", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrintSci(new Vector3d(_posPID[0].PTerm, _posPID[1].PTerm, _posPID[2].PTerm)), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos ITerm", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrintSci(new Vector3d(_posPID[0].ITerm, _posPID[1].ITerm, _posPID[2].ITerm)), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos DTerm", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrintSci(new Vector3d(_posPID[0].DTerm, _posPID[1].DTerm, _posPID[2].DTerm)), GUILayout.ExpandWidth(false));
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
            VelFORE          = GUILayout.Toggle(VelFORE, Localizer.Format("Vel FORE"), GUILayout.ExpandWidth(false));
            VelFORETerm.Text = GUILayout.TextField(VelFORETerm.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel SmoothIn", GUILayout.ExpandWidth(false));
            VelSmoothIn.Text = GUILayout.TextField(VelSmoothIn.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel SmoothOut", GUILayout.ExpandWidth(false));
            VelSmoothOut.Text = GUILayout.TextField(VelSmoothOut.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel PTerm", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrintSci(new Vector3d(_velPID[0].PTerm, _velPID[1].PTerm, _velPID[2].PTerm)), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel ITerm", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrintSci(new Vector3d(_velPID[0].ITerm, _velPID[1].ITerm, _velPID[2].ITerm)), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel DTerm", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrintSci(new Vector3d(_velPID[0].DTerm, _velPID[1].DTerm, _velPID[2].DTerm)), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Smooth Torque", GUILayout.ExpandWidth(false));
            SmoothTorque.Text = GUILayout.TextField(SmoothTorque.Text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("Reset Tuning Values")))
                Defaults();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_current), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Desired", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_desired), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Error", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_error), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("TargetOmega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_targetOmega), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Omega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_vessel.angularVelocityD), GUILayout.ExpandWidth(false));
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
            GUILayout.Label(MuUtils.PrettyPrint(_controlTorque), GUILayout.ExpandWidth(false));
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
