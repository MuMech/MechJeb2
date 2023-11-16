extern alias JetBrainsAnnotations;
using System;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech.AttitudeControllers
{
    internal class HybridController : BaseAttitudeController
    {
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble MaxStoppingTime = new EditableDouble(2);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDoubleMult RollControlRange = new EditableDoubleMult(5 * Mathf.Deg2Rad, Mathf.Deg2Rad);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UseControlRange = true;

        private readonly TorquePI _pitchPI = new TorquePI();
        private readonly TorquePI _yawPI   = new TorquePI();
        private readonly TorquePI _rollPI  = new TorquePI();

        private readonly KosPIDLoop _pitchRatePI = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);
        private readonly KosPIDLoop _yawRatePI   = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);
        private readonly KosPIDLoop _rollRatePI  = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UseInertia = true;

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

        public HybridController(MechJebModuleAttitudeController controller) : base(controller)
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

        private void UpdatePhi()
        {
            Transform vesselTransform = Ac.Vessel.ReferenceTransform;

            // 1. The Euler(-90) here is because the unity transform puts "up" as the pointy end, which is wrong.  The rotation means that
            // "forward" becomes the pointy end, and "up" and "right" correctly define e.g. AoA/pitch and AoS/yaw.  This is just KSP being KSP.
            // 2. We then use the inverse ship rotation to transform the requested attitude into the ship frame.
            Quaternion deltaRotation = Quaternion.Inverse(vesselTransform.transform.rotation * Quaternion.Euler(-90, 0, 0)) * Ac.RequestedAttitude;

            // get us some euler angles for the target transform
            Vector3d ea = deltaRotation.eulerAngles;
            double pitch = ea[0] * UtilMath.Deg2Rad;
            double yaw = ea[1] * UtilMath.Deg2Rad;
            double roll = ea[2] * UtilMath.Deg2Rad;

            // law of cosines for the "distance" of the miss in radians
            _phiTotal = Math.Acos(MuUtils.Clamp(Math.Cos(pitch) * Math.Cos(yaw), -1, 1));

            // this is the initial direction of the great circle route of the requested transform
            // (pitch is latitude, yaw is -longitude, and we are "navigating" from 0,0)
            var temp = new Vector3d(Math.Sin(pitch), Math.Cos(pitch) * Math.Sin(-yaw), 0);
            temp = temp.normalized * _phiTotal;

            // we assemble phi in the pitch, roll, yaw basis that vessel.MOI uses (right handed basis)
            var phi = new Vector3d(
                MuUtils.ClampRadiansPi(temp[0]), // pitch distance around the geodesic
                MuUtils.ClampRadiansPi(roll),
                MuUtils.ClampRadiansPi(temp[1]) // yaw distance around the geodesic
            );

            phi.Scale(Ac.AxisControl);

            if (UseInertia)
                phi -= Ac.inertia;

            _phiVector = phi;
        }

        private void UpdatePredictionPI()
        {
            _omega = -Ac.Vessel.angularVelocity;

            UpdatePhi();

            for (int i = 0; i < 3; i++)
            {
                _maxOmega[i] = _controlTorque[i] * MaxStoppingTime / Ac.VesselState.MoI[i];
            }

            _targetOmega[0] = _pitchRatePI.Update(_phiVector[0], 0, _maxOmega[0]);
            _targetOmega[1] = _rollRatePI.Update(_phiVector[1], 0, _maxOmega[1]);
            _targetOmega[2] = _yawRatePI.Update(_phiVector[2], 0, _maxOmega[2]);

            if (UseControlRange && Math.Abs(_phiTotal) > RollControlRange)
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
                _actuation[i] = _targetTorque[i] / _controlTorque[i];
                if (Math.Abs(_actuation[i]) < EPSILON || double.IsNaN(_actuation[i]))
                    _actuation[i] = 0;
            }
        }

        public override void GUI()
        {
            UseInertia = GUILayout.Toggle(UseInertia, Localizer.Format("#MechJeb_HybridController_checkbox1")); //"useInertia"

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label1"), GUILayout.ExpandWidth(false)); //"MaxStoppingTime"
            MaxStoppingTime.Text = GUILayout.TextField(MaxStoppingTime.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            UseControlRange = GUILayout.Toggle(UseControlRange, Localizer.Format("#MechJeb_HybridController_checkbox2"),
                GUILayout.ExpandWidth(false)); //"RollControlRange"
            RollControlRange.Text = GUILayout.TextField(RollControlRange.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            // Not used yet
            //GuiUtils.SimpleTextBox("Maximum Relative Angular Velocity", kWlimit, "%");
            //double tmp_kWlimit = kWlimit;
            //tmp_kWlimit = (EditableDouble)GUILayout.HorizontalSlider((float)tmp_kWlimit, 0.0F, 1.0F);
            //
            //const int sliderPrecision = 3;
            //if (Math.Round(Math.Abs(tmp_kWlimit - kWlimit), sliderPrecision) > 0)
            //{
            //    kWlimit.val = Math.Round(tmp_kWlimit, sliderPrecision);
            //}

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label2"), GUILayout.ExpandWidth(true)); //"Actuation"
            GUILayout.Label(MuUtils.PrettyPrint(_actuation), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label3"), GUILayout.ExpandWidth(true)); //"phiVector"
            GUILayout.Label(MuUtils.PrettyPrint(_phiVector), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Omega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_omega), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MaxOmega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(_maxOmega), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label4"), GUILayout.ExpandWidth(true)); //"TargetTorque"
            GUILayout.Label(MuUtils.PrettyPrint(_targetTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label5"), GUILayout.ExpandWidth(true)); //"ControlTorque"
            GUILayout.Label(MuUtils.PrettyPrint(_controlTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label6"), GUILayout.ExpandWidth(true)); //"Inertia"
            GUILayout.Label("|" + Ac.inertia.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(Ac.inertia), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }
}
