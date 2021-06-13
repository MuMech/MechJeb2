using System;
using UnityEngine;
using KSP.Localization;

namespace MuMech.AttitudeControllers
{
    class HybridController : BaseAttitudeController
    {
        [Persistent(pass = (int)Pass.Global)]
        private EditableDouble maxStoppingTime = new EditableDouble(2);

        [Persistent(pass = (int)Pass.Global)]
        private EditableDoubleMult rollControlRange = new EditableDoubleMult(5 * Mathf.Deg2Rad, Mathf.Deg2Rad);

        [Persistent(pass = (int) Pass.Global)]
        private bool useControlRange = true;

        public TorquePI pitchPI = new TorquePI();
        public TorquePI yawPI = new TorquePI();
        public TorquePI rollPI = new TorquePI();

        public KosPIDLoop pitchRatePI = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);
        public KosPIDLoop yawRatePI = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);
        public KosPIDLoop rollRatePI = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);

        [Persistent(pass = (int)Pass.Global)]
        public bool useInertia = true;

        private Vector3d Actuation = Vector3d.zero;
        private Vector3d TargetTorque = Vector3d.zero;
        private Vector3d Omega = Vector3d.zero;

        /* error */
        private double phiTotal;
        /* error in pitch, roll, yaw */
        private Vector3d phiVector = Vector3d.zero;
        private Vector3d TargetOmega = Vector3d.zero;

        /* max angular rotation */
        private Vector3d MaxOmega = Vector3d.zero;

        private Vector3d ControlTorque { get { return ac.torque; } }

        public HybridController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        private const double EPSILON = 1e-16;


        public override void DrivePre(FlightCtrlState s, out Vector3d act, out Vector3d deltaEuler)
        {
            UpdatePredictionPI();
            UpdateControl(s);

            deltaEuler = phiVector * Mathf.Rad2Deg;
            act = Actuation;
        }

        public void UpdatePhi()
        {
            Transform vesselTransform = ac.vessel.ReferenceTransform;

            // 1. The Euler(-90) here is because the unity transform puts "up" as the pointy end, which is wrong.  The rotation means that
            // "forward" becomes the pointy end, and "up" and "right" correctly define e.g. AoA/pitch and AoS/yaw.  This is just KSP being KSP.
            // 2. We then use the inverse ship rotation to transform the requested attitude into the ship frame.
            Quaternion deltaRotation = Quaternion.Inverse(vesselTransform.transform.rotation * Quaternion.Euler(-90, 0, 0))  * ac.RequestedAttitude;

            // get us some euler angles for the target transform
            Vector3d ea = deltaRotation.eulerAngles;
            double pitch = ea[0] * UtilMath.Deg2Rad;
            double yaw = ea[1] *UtilMath.Deg2Rad;
            double roll = ea[2] *UtilMath.Deg2Rad;

            // law of cosines for the "distance" of the miss in radians
            phiTotal = Math.Acos( MuUtils.Clamp( Math.Cos(pitch)*Math.Cos(yaw), -1, 1 ) );

            // this is the initial direction of the great circle route of the requested transform
            // (pitch is latitude, yaw is -longitude, and we are "navigating" from 0,0)
            Vector3d temp = new Vector3d(Math.Sin(pitch), Math.Cos(pitch) * Math.Sin(-yaw), 0);
            temp = temp.normalized * phiTotal;

            // we assemble phi in the pitch, roll, yaw basis that vessel.MOI uses (right handed basis)
            Vector3d phi = new Vector3d(
                    MuUtils.ClampRadiansPi(temp[0]), // pitch distance around the geodesic
                    MuUtils.ClampRadiansPi(roll),
                    MuUtils.ClampRadiansPi(temp[1]) // yaw distance around the geodesic
                    );

            phi.Scale(ac.AxisControl);

            if (useInertia)
                phi -= ac.inertia;

            phiVector = phi;
        }


        private void UpdatePredictionPI() {
            Omega = -ac.vessel.angularVelocity;

            UpdatePhi();

            for(int i = 0; i < 3; i++) {
                MaxOmega[i] = ControlTorque[i] * maxStoppingTime / ac.vesselState.MoI[i];
            }

            TargetOmega[0] = pitchRatePI.Update(phiVector[0], 0, MaxOmega[0]);
            TargetOmega[1] = rollRatePI.Update(phiVector[1], 0, MaxOmega[1]);
            TargetOmega[2] = yawRatePI.Update(phiVector[2], 0, MaxOmega[2]);

            if (useControlRange && Math.Abs(phiTotal) > rollControlRange) {
                TargetOmega[1] = 0;
                rollRatePI.ResetI();
            }

            TargetTorque[0] = pitchPI.Update(Omega[0], TargetOmega[0], ac.vesselState.MoI[0], ControlTorque[0]);
            TargetTorque[1] = rollPI.Update(Omega[1], TargetOmega[1], ac.vesselState.MoI[1], ControlTorque[1]);
            TargetTorque[2] = yawPI.Update(Omega[2], TargetOmega[2], ac.vesselState.MoI[2], ControlTorque[2]);
        }

        public override void Reset()
        {
            pitchPI.ResetI();
            yawPI.ResetI();
            rollPI.ResetI();
            pitchRatePI.ResetI();
            yawRatePI.ResetI();
            rollRatePI.ResetI();
        }

        private void UpdateControl(FlightCtrlState c) {
            /* TODO: static engine torque and/or differential throttle */

            for(int i = 0; i < 3; i++) {
                Actuation[i] = TargetTorque[i] / ControlTorque[i];
                if (Math.Abs(Actuation[i]) < EPSILON || double.IsNaN(Actuation[i]))
                    Actuation[i] = 0;
            }
        }


        public override void GUI()
        {
            useInertia = GUILayout.Toggle(useInertia, Localizer.Format("#MechJeb_HybridController_checkbox1"));//"useInertia"

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label1"), GUILayout.ExpandWidth(false));//"MaxStoppingTime"
            maxStoppingTime.text = GUILayout.TextField(maxStoppingTime.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            useControlRange = GUILayout.Toggle(useControlRange, Localizer.Format("#MechJeb_HybridController_checkbox2"), GUILayout.ExpandWidth(false));//"RollControlRange"
            rollControlRange.text = GUILayout.TextField(rollControlRange.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
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
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label2"), GUILayout.ExpandWidth(true));//"Actuation"
            GUILayout.Label(MuUtils.PrettyPrint(Actuation), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label3"), GUILayout.ExpandWidth(true));//"phiVector"
            GUILayout.Label(MuUtils.PrettyPrint(phiVector), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Omega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(Omega), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MaxOmega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(MaxOmega), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label4"), GUILayout.ExpandWidth(true));//"TargetTorque"
            GUILayout.Label(MuUtils.PrettyPrint(TargetTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label5"), GUILayout.ExpandWidth(true));//"ControlTorque"
            GUILayout.Label(MuUtils.PrettyPrint(ControlTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label6"), GUILayout.ExpandWidth(true));//"Inertia"
            GUILayout.Label("|" + ac.inertia.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(ac.inertia), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }
}
