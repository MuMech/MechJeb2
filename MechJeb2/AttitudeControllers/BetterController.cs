using System;
using KSP.Localization;
using UnityEngine;

namespace MuMech.AttitudeControllers
{
    internal class BetterController : BaseAttitudeController
    {
        private static readonly Vector3d _vector3dnan = new Vector3d(double.NaN, double.NaN, double.NaN);
        private                 Vessel   Vessel => ac.vessel;

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble VelKp = new EditableDouble(9.18299345180006);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble VelKi = new EditableDouble(16.2833478287224);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble VelKd = new EditableDouble(-0.0921320503942923);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble VelN = new EditableDouble(99.6720838459594);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble VelB = new EditableDouble(0.596313214751797);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble VelC = new EditableDouble(0.596313214751797);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble VelSmoothIn = new EditableDouble(0.3);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble VelSmoothOut = new EditableDouble(1);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble PosSmoothIn = new EditableDouble(1);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble PosFactor = new EditableDouble(1.0);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble maxStoppingTime = new EditableDouble(2.0);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble minFlipTime = new EditableDouble(120);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private readonly EditableDouble rollControlRange = new EditableDouble(5);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private bool useControlRange = true;

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private bool useFlipTime = true;

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        private bool useStoppingTime = true;

        private void PhaseMargin0()
        {
            VelKp.val = 9.16257996590235;
            VelKi.val = 16.8082178947434;
            VelKd.val = -0.264419293457006;
            VelN.val  = 34.6517073172354;
            VelB.val  = 0.641906946233989;
            VelC.val  = 0.641906946233989;
        }

        private void PhaseMargin30()
        {
            VelKp.val = 9.14127622620632;
            VelKi.val = 16.4800769077581;
            VelKd.val = -0.176394176076628;
            VelN.val  = 51.8230047585881;
            VelB.val  = 0.589571344018453;
            VelC.val  = 0.589571344018453;
        }

        private void PhaseMargin45()
        {
            VelKp.val = 9.18299345180006;
            VelKi.val = 16.2833478287224;
            VelKd.val = -0.0921320503942923;
            VelN.val  = 99.6720838459594;
            VelB.val  = 0.596313214751797;
            VelC.val  = 0.596313214751797;
        }

        private void PhaseMargin60()
        {
            VelKp.val = 9.28960634024859;
            VelKi.val = 16.2104766712016;
            VelKd.val = -0.00855473620341131;
            VelN.val  = 1085.90213881104;
            VelB.val  = 0.602732150757328;
            VelC.val  = 0.602732150757328;
        }

        private void PhaseMargin90()
        {
            VelKp.val = 8.16194252374655;
            VelKi.val = 12.0128187553426;
            VelKd.val = 0.30398608439146;
            VelN.val  = 16.3474512421439;
            VelB.val  = 0.619393167366281;
            VelC.val  = 0.0219634263259034;
        }

        private void OtherDefaults()
        {
            VelSmoothIn.val      = 0.3;
            VelSmoothOut.val     = 1.0;
            PosFactor.val        = 1.0;
            PosSmoothIn.val      = 1.0;
            maxStoppingTime.val  = 2;
            minFlipTime.val      = 120;
            rollControlRange.val = 5;
        }

        private readonly PIDLoop[] _pid =
        {
            new PIDLoop(),
            new PIDLoop(),
            new PIDLoop()
        };

        /* error in pitch, roll, yaw */
        private Vector3d _error0 = Vector3d.zero;
        private Vector3d _error1 = _vector3dnan;

        /* max angular acceleration */
        private Vector3d _maxAlpha = Vector3d.zero;

        /* max angular rotation */
        private Vector3d _maxOmega     = Vector3d.zero;
        private Vector3d _omega0       = _vector3dnan;
        private Vector3d _targetOmega  = Vector3d.zero;
        private Vector3d _targetTorque = Vector3d.zero;
        private Vector3d _actuation    = Vector3d.zero;

        /* error */
        private double _errorTotal;

        public BetterController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        public override void OnModuleEnabled()
        {
            Reset();
        }

        private const double EPS = 2.2204e-16;

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
            Transform vesselTransform = Vessel.ReferenceTransform;

            // 1. The Euler(-90) here is because the unity transform puts "up" as the pointy end, which is wrong.  The rotation means that
            // "forward" becomes the pointy end, and "up" and "right" correctly define e.g. AoA/pitch and AoS/yaw.  This is just KSP being KSP.
            // 2. We then use the inverse ship rotation to transform the requested attitude into the ship frame (we do everything in the ship frame
            // first, and then negate the error to get the error in the target reference frame at the end).
            Quaternion deltaRotation = Quaternion.Inverse(vesselTransform.transform.rotation * Quaternion.Euler(-90, 0, 0)) * ac.RequestedAttitude;

            // get us some euler angles for the target transform
            Vector3d ea = deltaRotation.eulerAngles;
            double pitch = ea[0] * UtilMath.Deg2Rad;
            double yaw = ea[1] * UtilMath.Deg2Rad;
            double roll = ea[2] * UtilMath.Deg2Rad;

            // law of cosines for the "distance" of the miss in radians
            _errorTotal = Math.Acos(MuUtils.Clamp(Math.Cos(pitch) * Math.Cos(yaw), -1, 1));

            // this is the initial direction of the great circle route of the requested transform
            // (pitch is latitude, yaw is -longitude, and we are "navigating" from 0,0)
            // doing this calculation is the ship frame is a bit easier to reason about.
            var temp = new Vector3d(Math.Sin(pitch), Math.Cos(pitch) * Math.Sin(-yaw), 0);
            temp = temp.normalized * _errorTotal;

            // we assemble phi in the pitch, roll, yaw basis that vessel.MOI uses (right handed basis)
            var phi = new Vector3d(
                MuUtils.ClampRadiansPi(temp[0]), // pitch distance around the geodesic
                MuUtils.ClampRadiansPi(roll),
                MuUtils.ClampRadiansPi(temp[1]) // yaw distance around the geodesic
            );

            // apply the axis control from the parent controller
            phi.Scale(ac.AxisControl);

            // the error in the ship's position is the negative of the reference position in the ship frame
            _error0 = -phi;
        }

        private void UpdatePredictionPI()
        {
            _omega0 = Vessel.angularVelocityD;

            UpdateError();

            // lowpass filter on the error input
            _error0 = _error1.IsFinite() ? _error1 + PosSmoothIn * (_error0 - _error1) : _error0;

            Vector3d controlTorque = ac.torque;

            // needed to stop wiggling at higher phys warp
            double warpFactor = Math.Pow(ac.vesselState.deltaT / 0.02, 0.90); // the power law here comes ultimately from the simulink PID tuning app

            // see https://archive.is/NqoUm and the "Alt Hold Controller", the acceleration PID is not implemented so we only
            // have the first two PIDs in the cascade.
            for (int i = 0; i < 3; i++)
            {
                double error = _error0[i];

                _maxAlpha[i] = controlTorque[i] / Vessel.MOI[i];

                if (_maxAlpha[i] == 0)
                    _maxAlpha[i] = 1;

                if (ac.OmegaTarget[i].IsFinite())
                {
                    _targetOmega[i] = ac.OmegaTarget[i];
                }
                else
                {
                    // the cube root scaling was determined mostly via experience
                    double maxAlphaCbrt = Math.Pow(_maxAlpha[i], 1.0 / 3.0);
                    double effLD = maxAlphaCbrt * PosFactor;
                    double posKp = Math.Sqrt(_maxAlpha[i] / (2 * effLD));

                    if (Math.Abs(error) <= 2 * effLD)
                        // linear ramp down of acceleration
                        _targetOmega[i] = -posKp * error;
                    else
                        // v = - sqrt(2 * F * x / m) is target stopping velocity based on distance
                        _targetOmega[i] = -Math.Sqrt(2 * _maxAlpha[i] * (Math.Abs(error) - effLD)) * Math.Sign(error);

                    if (useStoppingTime)
                    {
                        _maxOmega[i] = _maxAlpha[i] * maxStoppingTime;
                        if (useFlipTime) _maxOmega[i] = Math.Max(_maxOmega[i], Math.PI / minFlipTime);
                        _targetOmega[i] = MuUtils.Clamp(_targetOmega[i], -_maxOmega[i], _maxOmega[i]);
                    }

                    if (useControlRange && _errorTotal * Mathf.Rad2Deg > rollControlRange)
                        _targetOmega[1] = 0;
                }

                _pid[i].Kp        = VelKp / (_maxAlpha[i] * warpFactor);
                _pid[i].Ki        = VelKi / (_maxAlpha[i] * warpFactor * warpFactor);
                _pid[i].Kd        = VelKd / _maxAlpha[i];
                _pid[i].N         = VelN / warpFactor;
                _pid[i].B         = VelB;
                _pid[i].C         = VelC;
                _pid[i].Ts        = ac.vesselState.deltaT;
                _pid[i].SmoothIn  = MuUtils.Clamp01(VelSmoothIn);
                _pid[i].SmoothOut = MuUtils.Clamp01(VelSmoothOut);
                _pid[i].MinOutput = -1;
                _pid[i].MaxOutput = 1;

                // need the negative from the pid due to KSP's orientation of actuation
                _actuation[i] = -_pid[i].Update(_targetOmega[i], _omega0[i]);

                if (Math.Abs(_actuation[i]) < EPS || double.IsNaN(_actuation[i]))
                    _actuation[i] = 0;

                _targetTorque[i] = _actuation[i] / ac.torque[i];

                if (ac.ActuationControl[i] == 0)
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
            useStoppingTime      = GUILayout.Toggle(useStoppingTime, "Maximum Stopping Time", GUILayout.ExpandWidth(false));
            maxStoppingTime.text = GUILayout.TextField(maxStoppingTime.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            useFlipTime      = GUILayout.Toggle(useFlipTime, "Minimum Flip Time", GUILayout.ExpandWidth(false));
            minFlipTime.text = GUILayout.TextField(minFlipTime.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            if (!useStoppingTime)
                useFlipTime = false;

            GUILayout.BeginHorizontal();
            useControlRange = GUILayout.Toggle(useControlRange, Localizer.Format("#MechJeb_HybridController_checkbox2"),
                GUILayout.ExpandWidth(false)); //"RollControlRange"
            rollControlRange.text = GUILayout.TextField(rollControlRange.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos Factor", GUILayout.ExpandWidth(false));
            PosFactor.text = GUILayout.TextField(PosFactor.text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos SmoothIn", GUILayout.ExpandWidth(false));
            PosSmoothIn.text = GUILayout.TextField(PosSmoothIn.text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel Kp", GUILayout.ExpandWidth(false));
            VelKp.text = GUILayout.TextField(VelKp.text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel Ki", GUILayout.ExpandWidth(false));
            VelKi.text = GUILayout.TextField(VelKi.text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel Kd", GUILayout.ExpandWidth(false));
            VelKd.text = GUILayout.TextField(VelKd.text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel Kd N", GUILayout.ExpandWidth(false));
            VelN.text = GUILayout.TextField(VelN.text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel B", GUILayout.ExpandWidth(false));
            VelB.text = GUILayout.TextField(VelB.text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel C", GUILayout.ExpandWidth(false));
            VelC.text = GUILayout.TextField(VelC.text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel SmoothIn", GUILayout.ExpandWidth(false));
            VelSmoothIn.text = GUILayout.TextField(VelSmoothIn.text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel SmoothOut", GUILayout.ExpandWidth(false));
            VelSmoothOut.text = GUILayout.TextField(VelSmoothOut.text, GUILayout.ExpandWidth(true), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vel PID Phase Margin Presets: ");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("0°")))
                PhaseMargin0();
            if (GUILayout.Button(Localizer.Format("30°")))
                PhaseMargin30();
            if (GUILayout.Button(Localizer.Format("45° (recommended)")))
                PhaseMargin45();
            if (GUILayout.Button(Localizer.Format("60°")))
                PhaseMargin60();
            if (GUILayout.Button(Localizer.Format("90°")))
                PhaseMargin90();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("Reset Other Tuning Values")))
                OtherDefaults();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label2"), GUILayout.ExpandWidth(true)); //"Actuation"
            GUILayout.Label(MuUtils.PrettyPrint(_actuation), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Error", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_error0), GUILayout.ExpandWidth(false));
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
            GUILayout.Label("TargetOmega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_targetOmega), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label4"), GUILayout.ExpandWidth(true)); //"TargetTorque"
            GUILayout.Label(MuUtils.PrettyPrint(_targetTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label5"), GUILayout.ExpandWidth(true)); //"ControlTorque"
            GUILayout.Label(MuUtils.PrettyPrint(ac.torque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MaxAlpha", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_maxAlpha), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }
}
