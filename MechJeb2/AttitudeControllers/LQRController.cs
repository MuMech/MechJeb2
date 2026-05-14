using JetBrains.Annotations;
using MechJebLib.Control;
using UnityEngine;
using static System.Math;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MuMech.AttitudeControllers
{
    public class LQRController : BaseAttitudeController
    {
        private readonly LQRLoop1[]       _lqr              = { new LQRLoop1(), new LQRLoop1(), new LQRLoop1() };
        private readonly DirectionTracker _directionTracker = new DirectionTracker();

        private Vector3d _actuation = Vector3d.zero;
        private Vector3d _current   = Vector3d.zero;
        private Vector3d _desired   = Vector3d.zero;
        private Vector3d _error     = Vector3d.zero;
        private double   _distance;
        private Vector3d _velocity      = Vector3d.zero;
        private Vector3d _mass          = Vector3d.zero;
        private Vector3d _controlTorque = Vector3d.zero;
        private Vector3d _targetTorque  = Vector3d.zero;

        [UsedImplicitly] [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble SmoothTorque = new EditableDouble(0.10);

        public LQRController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        private Vessel _vessel => Ac.Vessel;

        private void UpdateLQR()
        {
            Transform   vesselTransform = _vessel.ReferenceTransform;
            QuaternionD currentAttitude = (QuaternionD)vesselTransform.transform.rotation * MathExtensions.Euler(-90, 0, 0);
            _directionTracker.Update(currentAttitude);

            _current                      = _directionTracker.TrackedRotation;
            (_desired, _error, _distance) = _directionTracker.Desired(Ac.RequestedAttitude);
            _velocity                     = _vessel.angularVelocityD;

            // low-pass filter the control torque
            _controlTorque = _controlTorque == Vector3d.zero ? Ac.torque : _controlTorque + SmoothTorque * (Ac.torque - _controlTorque);

            // if torque is really zero, set it zero
            for (int i = 0; i < 3; i++)
                if (Ac.torque[i] == 0)
                    _controlTorque[i] = 0;

            for (int i = 0; i < 3; i++)
            {
                _mass[i]      = _vessel.MOI[i] / _controlTorque[i];
                _lqr[i].M     = _mass[i];
                _lqr[i].Ts    = Ac.VesselState.deltaT;
                _lqr[i].Grr   = 16;
                _lqr[i].UMin  = -1;
                _lqr[i].UMax  = 1;
                _actuation[i] = -_lqr[i].Update(_desired[i], _current[i], _velocity[i]);

                if (Ac.ActuationControl[i] == 0 || _controlTorque[i] == 0 || Ac.AxisControl[i] == 0)
                {
                    _actuation[i] = 0;
                    Reset(i);
                }

                if (Abs(_actuation[i]) < EPS || double.IsNaN(_actuation[i]))
                    _actuation[i] = 0;

                _targetTorque[i] = _controlTorque[i] * _actuation[i];
            }
        }

        public override void DrivePre(FlightCtrlState s, out Vector3d act, out Vector3d deltaEuler)
        {
            UpdateLQR();

            deltaEuler = -_error * Mathf.Rad2Deg;

            for (int i = 0; i < 3; i++)
                if (Abs(_actuation[i]) < EPS || double.IsNaN(_actuation[i]))
                    _actuation[i] = 0;

            act = _actuation;
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
            _lqr[i].Reset();
            _directionTracker.Reset(i);
        }

        public override void GUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Smooth Torque", GuiUtils.LayoutNoExpandWidth);
            SmoothTorque.Text = GUILayout.TextField(SmoothTorque.Text, GuiUtils.LayoutExpandWidth, GuiUtils.LayoutWidth(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Position", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(_current), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Desired", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(_desired), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Error", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(_error), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Velocity", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(_velocity), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Mass", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(_mass), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Actuation", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(_actuation), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MOI", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(_vessel.MOI), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Control Torque", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(_controlTorque), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Applied Torque", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(_targetTorque), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();
        }
    }
}
