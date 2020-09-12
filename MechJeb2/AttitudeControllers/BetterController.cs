using System;
using UnityEngine;
using KSP.Localization;

namespace MuMech.AttitudeControllers
{
    class BetterController : BaseAttitudeController
    {

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private EditableDouble maxStoppingTime = new EditableDouble(2);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private bool useStoppingTime = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private EditableDouble minFlipTime = new EditableDouble(20);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private bool useFlipTime = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private EditableDoubleMult rollControlRange = new EditableDouble(5);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private bool useControlRange = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private EditableDouble Kp = new EditableDouble(50.0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private EditableDouble Ki = new EditableDouble(0.0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private EditableDouble Kd = new EditableDouble(0.0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private EditableDouble LD = new EditableDouble(0.10);

        public PIDLoop[] Pid = { new PIDLoop(extraUnwind:true), new PIDLoop(extraUnwind:true), new PIDLoop(extraUnwind:true) };

        private Vector3d Actuation = Vector3d.zero;
        private Vector3d TargetTorque = Vector3d.zero;
        private Vector3d Omega = Vector3d.zero;

        /* error */
        private double errorTotal;
        /* error in pitch, roll, yaw */
        private Vector3d errorVector = Vector3d.zero;
        private Vector3d TargetOmega = Vector3d.zero;

        /* max angular rotation */
        private Vector3d MaxOmega = Vector3d.zero;

        private Vector3d ControlTorque { get { return ac.torque; } }

        public BetterController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        private const double EPSILON = 1e-16;

        public override void DrivePre(FlightCtrlState s, out Vector3d act, out Vector3d deltaEuler)
        {
            UpdatePredictionPI();

            deltaEuler = -errorVector * Mathf.Rad2Deg;
            act = Actuation;
        }

        public void UpdateError()
        {
            Transform vesselTransform = ac.vessel.ReferenceTransform;

            // 1. The Euler(-90) here is because the unity transform puts "up" as the pointy end, which is wrong.  The rotation means that
            // "forward" becomes the pointy end, and "up" and "right" correctly define e.g. AoA/pitch and AoS/yaw.  This is just KSP being KSP.
            // 2. We then use the inverse ship rotation to transform the requested attitude into the ship frame (we do everything in the ship frame
            // first, and then negate the error to get the error in the target reference frame at the end).
            Quaternion deltaRotation = Quaternion.Inverse(vesselTransform.transform.rotation * Quaternion.Euler(-90, 0, 0))  * ac.RequestedAttitude;

            // get us some euler angles for the target transform
            Vector3d ea  = deltaRotation.eulerAngles;
            double pitch = ea[0] * UtilMath.Deg2Rad;
            double yaw   = ea[1] *UtilMath.Deg2Rad;
            double roll  = ea[2] *UtilMath.Deg2Rad;

            // law of cosines for the "distance" of the miss in radians
            errorTotal = Math.Acos( MuUtils.Clamp( Math.Cos(pitch)*Math.Cos(yaw), -1, 1 ) );

            // this is the initial direction of the great circle route of the requested transform
            // (pitch is latitude, yaw is -longitude, and we are "navigating" from 0,0)
            // doing this calculation is the ship frame is a bit easier to reason about.
            Vector3d temp = new Vector3d(Math.Sin(pitch), Math.Cos(pitch) * Math.Sin(-yaw), 0);
            temp = temp.normalized * errorTotal;

            // we assemble phi in the pitch, roll, yaw basis that vessel.MOI uses (right handed basis)
            Vector3d phi = new Vector3d(
                    MuUtils.ClampRadiansPi(temp[0]), // pitch distance around the geodesic
                    MuUtils.ClampRadiansPi(roll),
                    MuUtils.ClampRadiansPi(temp[1]) // yaw distance around the geodesic
                    );

            // apply the axis control from the parent controller
            phi.Scale(ac.AxisState);

            // the error in the ship's position is the negative of the reference position in the ship frame
            errorVector = -phi;
        }

        Vector3d lastOmega = Vector3d.zero;

        private void UpdatePredictionPI() {
            // velcity relative to the target is the minus of the velocity relative to the vessel
            // (The PID moves the vessel to zero in the target frame)
            Omega = -ac.vessel.angularVelocity;

            UpdateError();

            Vector3d MaxAlpha = Vector3d.zero;

            // see https://archive.is/NqoUm and the "Alt Hold Controller", the acceleration PID is not implemented so we only
            // have the first two PIDs in the cascade.
            for(int i = 0; i < 3; i++) {
                MaxAlpha[i] = ControlTorque[i] /  ac.vesselState.MoI[i];
                double Gain = Math.Sqrt(0.5 * MaxAlpha[i] / LD);
                if (Math.Abs(errorVector[i]) <= 2 * LD)
                    // linear ramp down of acceleration
                    TargetOmega[i] = Gain * errorVector[i];
                else
                    // v = - sqrt(2 * F * x / m) is target stopping velocity based on distance
                    TargetOmega[i] = Math.Sqrt(2 * MaxAlpha[i] * (Math.Abs(errorVector[i]) - LD)) * Math.Sign(errorVector[i]);

                if (useStoppingTime)
                {
                    MaxOmega[i] = MaxAlpha[i] * maxStoppingTime;
                    if (useFlipTime)
                    {
                        MaxOmega[i] = Math.Max(MaxOmega[i], Math.PI / minFlipTime);
                    }
                    TargetOmega[i] = MuUtils.Clamp(TargetOmega[i], -MaxOmega[i], MaxOmega[i]);
                }
            }

            if (useControlRange && errorTotal * Mathf.Rad2Deg > rollControlRange) {
                TargetOmega[1] = 0;
                Pid[1].ResetI();
            }

            for(int i = 0; i < 3; i++)
            {
                Pid[i].Ki = Ki;
                Pid[i].Kp = Kp;
                Pid[i].Kd = Kd;

                Actuation[i] =  Pid[i].Update(Omega[i] / Math.Pow(MaxAlpha[i], 2.0/3.0), TargetOmega[i] / Math.Pow(MaxAlpha[i], 2.0/3.0), 1.0);
                TargetTorque[i] = Actuation[i] * ControlTorque[i]; // for display
            }
        }

        public override void Reset()
        {
            for(int i = 0; i < 3; i++)
            {
                Pid[i].ResetI();
            }
        }

        public override void GUI()
        {
            GUILayout.BeginHorizontal();
            useStoppingTime = GUILayout.Toggle(useStoppingTime, "Maximum Stopping Time", GUILayout.ExpandWidth(false));
            maxStoppingTime.text = GUILayout.TextField(maxStoppingTime.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            useFlipTime = GUILayout.Toggle(useFlipTime, "Minimum Flip Time", GUILayout.ExpandWidth(false));
            minFlipTime.text = GUILayout.TextField(minFlipTime.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            if(!useStoppingTime)
                useFlipTime = false;

            GUILayout.BeginHorizontal();
            useControlRange = GUILayout.Toggle(useControlRange, Localizer.Format("#MechJeb_HybridController_checkbox2"), GUILayout.ExpandWidth(false));//"RollControlRange"
            rollControlRange.text = GUILayout.TextField(rollControlRange.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("LD", GUILayout.ExpandWidth(false));
            LD.text = GUILayout.TextField(LD.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Kp", GUILayout.ExpandWidth(false));
            Kp.text = GUILayout.TextField(Kp.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ki", GUILayout.ExpandWidth(false));
            Ki.text = GUILayout.TextField(Ki.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Kd", GUILayout.ExpandWidth(false));
            Kd.text = GUILayout.TextField(Kd.text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
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
            GUILayout.Label("Error", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(errorVector), GUILayout.ExpandWidth(false));
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
            GUILayout.Label("TargetOmega", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(TargetOmega), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label4"), GUILayout.ExpandWidth(true));//"TargetTorque"
            GUILayout.Label(MuUtils.PrettyPrint(TargetTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_HybridController_label5"), GUILayout.ExpandWidth(true));//"ControlTorque"
            GUILayout.Label(MuUtils.PrettyPrint(ControlTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }
}
