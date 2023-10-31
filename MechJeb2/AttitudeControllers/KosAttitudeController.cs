using System;
using JetBrains.Annotations;
using UnityEngine;

namespace MuMech.AttitudeControllers
{
    internal class KosAttitudeController : BaseAttitudeController
    {
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble MaxStoppingTime = new EditableDouble(2);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDoubleMult RollControlRange = new EditableDoubleMult(5 * Mathf.Deg2Rad, Mathf.Deg2Rad);
        //public double RollControlRange {
        //    get { return this.rollControlRange; }
        //    set { this.rollControlRange.val = Math.Max(EPSILON, Math.Min(Math.PI, value)); }
        //}

        private readonly TorquePI _pitchPI = new TorquePI();
        private readonly TorquePI _yawPI   = new TorquePI();
        private readonly TorquePI _rollPI  = new TorquePI();

        private readonly KosPIDLoop _pitchRatePI = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);
        private readonly KosPIDLoop _yawRatePI   = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);
        private readonly KosPIDLoop _rollRatePI  = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);

        private Vector3d _actuation    = Vector3d.zero;
        private Vector3d _targetTorque = Vector3d.zero;
        private Vector3d _omega        = Vector3d.zero;

        /* error */
        private double _phiTotal;

        /* error in pitch, roll, yaw */
        private Vector3d _phiVector   = Vector3d.zero;
        private Vector3d _targetOmega = Vector3d.zero;

        /* max angular rotation */
        private Vector3d _maxOmega = Vector3d.zero;

        private Vector3d _controlTorque => Ac.torque;

        public KosAttitudeController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        private const double EPSILON = 1e-16;

        public override void DrivePre(FlightCtrlState s, out Vector3d act, out Vector3d deltaEuler)
        {
            UpdatePredictionPI();
            UpdateControl();

            deltaEuler = _phiVector * Mathf.Rad2Deg;
            act        = _actuation;
        }

        /* temporary state vectors */
        private Quaternion _vesselRotation;
        private Vector3d   _vesselForward;
        private Vector3d   _vesselTop;
        private Vector3d   _vesselStarboard;
        private Vector3d   _targetForward;
        private Vector3d   _targetTop;

        /* private Vector3d targetStarboard; */

        private void UpdateStateVectors()
        {
            /* FIXME: may get called more than once per tick */
            _vesselRotation  = Ac.Vessel.ReferenceTransform.rotation * Quaternion.Euler(-90, 0, 0);
            _vesselForward   = _vesselRotation * Vector3d.forward;
            _vesselTop       = _vesselRotation * Vector3d.up;
            _vesselStarboard = _vesselRotation * Vector3d.right;

            _targetForward = Ac.RequestedAttitude * Vector3d.forward;
            _targetTop     = Ac.RequestedAttitude * Vector3d.up;
            /* targetStarboard = target * Vector3d.right; */

            _omega = -Ac.Vessel.angularVelocity;
        }

        private double PhiTotal()
        {
            UpdateStateVectors();

            double phiTotal = Vector3d.Angle(_vesselForward, _targetForward) * Mathf.Deg2Rad;
            if (Vector3d.Angle(_vesselTop, _targetForward) > 90)
                phiTotal *= -1;

            return phiTotal;
        }

        private Vector3d PhiVector()
        {
            Vector3d phi = Vector3d.zero;

            phi[0] = Vector3d.Angle(_vesselForward, Vector3d.Exclude(_vesselStarboard, _targetForward)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(_vesselTop, Vector3d.Exclude(_vesselStarboard, _targetForward)) > 90)
                phi[0] *= -1;
            phi[1] = Vector3d.Angle(_vesselTop, Vector3d.Exclude(_vesselForward, _targetTop)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(_vesselStarboard, Vector3d.Exclude(_vesselForward, _targetTop)) > 90)
                phi[1] *= -1;
            phi[2] = Vector3d.Angle(_vesselForward, Vector3d.Exclude(_vesselTop, _targetForward)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(_vesselStarboard, Vector3d.Exclude(_vesselTop, _targetForward)) > 90)
                phi[2] *= -1;

            return phi;
        }

        private void UpdatePredictionPI()
        {
            _phiTotal = PhiTotal();

            _phiVector = PhiVector();

            for (int i = 0; i < 3; i++)
            {
                _maxOmega[i] = _controlTorque[i] * MaxStoppingTime / Ac.VesselState.MoI[i];
            }

            _targetOmega[0] = _pitchRatePI.Update(-_phiVector[0], 0, _maxOmega[0]);
            _targetOmega[1] = _rollRatePI.Update(-_phiVector[1], 0, _maxOmega[1]);
            _targetOmega[2] = _yawRatePI.Update(-_phiVector[2], 0, _maxOmega[2]);

            if (Math.Abs(_phiTotal) > RollControlRange)
            {
                _targetOmega[1] = 0;
                _rollRatePI.ResetI();
            }

            _targetTorque[0] = _pitchPI.Update(_omega[0], _targetOmega[0], Ac.VesselState.MoI[0], _controlTorque[0]);
            _targetTorque[1] = _rollPI.Update(_omega[1], _targetOmega[1], Ac.VesselState.MoI[1], _controlTorque[1]);
            _targetTorque[2] = _yawPI.Update(_omega[2], _targetOmega[2], Ac.VesselState.MoI[2], _controlTorque[2]);
        }

        public override void Reset()
        {
            _pitchPI.ResetI();
            _yawPI.ResetI();
            _rollPI.ResetI();
            _pitchRatePI.ResetI();
            _yawRatePI.ResetI();
            _rollRatePI.ResetI();
        }

        private void UpdateControl()
        {
            /* TODO: static engine torque and/or differential throttle */

            for (int i = 0; i < 3; i++)
            {
                double clamp = Math.Max(Math.Abs(_actuation[i]), 0.005) * 2;
                _actuation[i] = _targetTorque[i] / _controlTorque[i];
                if (Math.Abs(_actuation[i]) < EPSILON || double.IsNaN(_actuation[i]))
                    _actuation[i] = 0;
                _actuation[i] = Math.Max(Math.Min(_actuation[i], clamp), -clamp);
            }
        }

        public override void GUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("MaxStoppingTime", GUILayout.ExpandWidth(false));
            MaxStoppingTime.Text = GUILayout.TextField(MaxStoppingTime.Text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.Label("RollControlRange", GUILayout.ExpandWidth(false));
            RollControlRange.Text = GUILayout.TextField(RollControlRange.Text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Actuation", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_actuation), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("phiVector", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_phiVector), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("TargetTorque", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_targetTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.Label("ControlTorque", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_controlTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }
}
