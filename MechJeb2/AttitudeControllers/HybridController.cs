using System;
using UnityEngine;
using KSP.Localization;

namespace MuMech.AttitudeControllers
{
    class HybridController : BaseAttitudeController
    {
        /* target = Quaternion.LookRotation(vessel.GetObtVelocity().normalized, vessel.up); */
        //public Quaternion target { get; set; }

        [Persistent(pass = (int)Pass.Global)]
        private EditableDouble maxStoppingTime = new EditableDouble(2);

        [Persistent(pass = (int)Pass.Global)]
        private EditableDoubleMult rollControlRange = new EditableDoubleMult(5 * Mathf.Deg2Rad, Mathf.Deg2Rad);

        [Persistent(pass = (int) Pass.Global)]
        private bool useControlRange = true;
        //public double RollControlRange {
        //    get { return this.rollControlRange; }
        //    set { this.rollControlRange.val = Math.Max(EPSILON, Math.Min(Math.PI, value)); }
        //}

        //[Persistent(pass = (int)Pass.Global)]
        //public EditableDoubleMult kWlimit = new EditableDoubleMult(0.15, 0.01);

        public TorquePI pitchPI = new TorquePI();
        public TorquePI yawPI = new TorquePI();
        public TorquePI rollPI = new TorquePI();

        public PIDLoop pitchRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);
        public PIDLoop yawRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);
        public PIDLoop rollRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);

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
            //if (ac.vessel.ActionGroups[KSPActionGroup.SAS])
            //{
            //    /* SAS seems to be busted in 1.2.1? */
            //    ac.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            //}
            UpdatePredictionPI();
            UpdateControl(s);

            deltaEuler = phiVector * Mathf.Rad2Deg;
            act = Actuation;
        }

        /* temporary state vectors */
        private Quaternion vesselRotation;
        private Vector3d vesselForward;
        private Vector3d vesselTop;
        private Vector3d vesselStarboard;
        private Vector3d targetForward;
        private Vector3d targetTop;
        /* private Vector3d targetStarboard; */

        private void UpdateStateVectors() {
            /* FIXME: may get called more than once per tick */
            vesselRotation = ac.vessel.ReferenceTransform.rotation * Quaternion.Euler(-90, 0, 0);
            vesselForward = vesselRotation * Vector3d.forward;
            vesselTop = vesselRotation * Vector3d.up;
            vesselStarboard = vesselRotation * Vector3d.right;

            targetForward = ac.RequestedAttitude * Vector3d.forward;
            targetTop = ac.RequestedAttitude * Vector3d.up;
            /* targetStarboard = target * Vector3d.right; */

            Omega = -ac.vessel.angularVelocity;
        }

        public double PhiTotal() {
            UpdateStateVectors();

            double PhiTotal = Vector3d.Angle(vesselForward, targetForward) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselTop, targetForward) > 90)
                PhiTotal *= -1;

            return PhiTotal;
        }


        public Vector3d PhiVector()
        {
            Transform vesselTransform = ac.vessel.ReferenceTransform;

            // Find out the real shorter way to turn where we wan to.
            // Thanks to HoneyFox
            Vector3d tgtLocalUp = vesselTransform.transform.rotation.Inverse() * ac.RequestedAttitude * Vector3d.forward;
            Vector3d curLocalUp = Vector3d.up;

            double turnAngle = Math.Abs(Vector3d.Angle(curLocalUp, tgtLocalUp));
            Vector2d rotDirection = new Vector2d(tgtLocalUp.x, tgtLocalUp.z);
            rotDirection = rotDirection.normalized * turnAngle;

            // And the lowest roll
            // Thanks to Crzyrndm
            Vector3 normVec = Vector3.Cross(ac.RequestedAttitude * Vector3.forward, vesselTransform.up);
            Quaternion targetDeRotated = Quaternion.AngleAxis((float)turnAngle, normVec) * ac.RequestedAttitude;
            float rollError = Vector3.Angle(vesselTransform.right, targetDeRotated * Vector3.right) *
                              Math.Sign(Vector3.Dot(targetDeRotated * Vector3.right, vesselTransform.forward));

            // From here everything should use MOI order for Vectors (pitch, roll, yaw)

            Vector3d Phi = new Vector3d(
                -rotDirection.y * UtilMath.Deg2Rad,
                rollError * UtilMath.Deg2Rad,
                rotDirection.x * UtilMath.Deg2Rad
            );

            Phi.Scale(ac.AxisState);

            if (useInertia)
                Phi += ac.inertia;

            return Phi;
        }


        private void UpdatePredictionPI() {
            phiTotal = PhiTotal();

            phiVector = PhiVector();

            for(int i = 0; i < 3; i++) {
                MaxOmega[i] = ControlTorque[i] * maxStoppingTime / ac.vesselState.MoI[i];
            }

            TargetOmega[0] = pitchRatePI.Update(-phiVector[0], 0, MaxOmega[0]);
            TargetOmega[1] = rollRatePI.Update(-phiVector[1], 0, MaxOmega[1]);
            TargetOmega[2] = yawRatePI.Update(-phiVector[2], 0, MaxOmega[2]);

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
                double clamp = Math.Max(Math.Abs(Actuation[i]), 0.005) * 2;
                Actuation[i] = TargetTorque[i] / ControlTorque[i];
                if (Math.Abs(Actuation[i]) < EPSILON || double.IsNaN(Actuation[i]))
                    Actuation[i] = 0;
                Actuation[i] = Math.Max(Math.Min(Actuation[i], clamp), -clamp);
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
