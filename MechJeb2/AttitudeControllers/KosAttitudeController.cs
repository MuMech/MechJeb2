using System;
using UnityEngine;

namespace MuMech.AttitudeControllers
{
    class KosAttitudeController : BaseAttitudeController
    {
        [Persistent(pass = (int)Pass.Global)]
        private EditableDouble maxStoppingTime = new EditableDouble(2);

        [Persistent(pass = (int)Pass.Global)]
        private EditableDoubleMult rollControlRange = new EditableDoubleMult(5 * Mathf.Deg2Rad, Mathf.Deg2Rad);
        //public double RollControlRange {
        //    get { return this.rollControlRange; }
        //    set { this.rollControlRange.val = Math.Max(EPSILON, Math.Min(Math.PI, value)); }
        //}

        public TorquePI pitchPI = new TorquePI();
        public TorquePI yawPI = new TorquePI();
        public TorquePI rollPI = new TorquePI();

        public KosPIDLoop pitchRatePI = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);
        public KosPIDLoop yawRatePI = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);
        public KosPIDLoop rollRatePI = new KosPIDLoop(1, 0.1, 0, extraUnwind: true);
        
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

        public KosAttitudeController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        private const double EPSILON = 1e-16;

        public override void DrivePre(FlightCtrlState s, out Vector3d act, out Vector3d deltaEuler)
        {
            UpdatePredictionPI();
            UpdateControl();

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

        public Vector3d PhiVector() {
            Vector3d Phi = Vector3d.zero;

            Phi[0] = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselStarboard, targetForward)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselStarboard, targetForward)) > 90)
                Phi[0] *= -1;
            Phi[1] = Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselForward, targetTop)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselForward, targetTop)) > 90)
                Phi[1] *= -1;
            Phi[2] = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselTop, targetForward)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselTop, targetForward)) > 90)
                Phi[2] *= -1;

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

            if (Math.Abs(phiTotal) > rollControlRange) {
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
        

        private void UpdateControl() {
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
            GUILayout.BeginHorizontal();
            GUILayout.Label("MaxStoppingTime", GUILayout.ExpandWidth(false));
            maxStoppingTime.text = GUILayout.TextField(maxStoppingTime.text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.Label("RollControlRange", GUILayout.ExpandWidth(false));
            rollControlRange.text = GUILayout.TextField(rollControlRange.text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Actuation", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(Actuation), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("phiVector", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(phiVector), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("TargetTorque", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(TargetTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.Label("ControlTorque", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(ControlTorque), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }
}
