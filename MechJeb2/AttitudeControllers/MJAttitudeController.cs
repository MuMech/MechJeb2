using System;
using UnityEngine;
using KSP.Localization;

namespace MuMech.AttitudeControllers
{
    class MJAttitudeController : BaseAttitudeController
    {
        public PIDControllerV3 pid;

        public Vector3d lastAct = Vector3d.zero;
        public Vector3d pidAction; //info
        public Vector3d error; //info


        [Persistent(pass = (int) Pass.Global)]
        public bool Tf_autoTune = true;

        public Vector3d TfV = new Vector3d(0.3, 0.3, 0.3);

        [Persistent(pass = (int) Pass.Global)]
        private Vector3 TfVec = new Vector3(0.3f, 0.3f, 0.3f); // use the serialize since Vector3d does not

        [Persistent(pass = (int) Pass.Global)]
        public double TfMin = 0.1;

        [Persistent(pass = (int) Pass.Global)]
        public double TfMax = 0.5;

        [Persistent(pass = (int) Pass.Global)]
        public bool lowPassFilter = true;

        [Persistent(pass = (int) Pass.Global)]
        public double kpFactor = 3;

        [Persistent(pass = (int) Pass.Global)]
        public double kiFactor = 6;

        [Persistent(pass = (int) Pass.Global)]
        public double kdFactor = 0.5;

        [Persistent(pass = (int) Pass.Global)]
        public double deadband = 0.0001;

        //Lower value of "kWlimit" reduces maximum angular velocity
        [Persistent(pass = (int) Pass.Global)]
        public EditableDouble kWlimit = 0.15;

        private readonly Vector3d defaultTfV = new Vector3d(0.3, 0.3, 0.3);


        // GUI
        public EditableDouble UI_TfX;
        public EditableDouble UI_TfY;
        public EditableDouble UI_TfZ;
        public EditableDouble UI_TfMin;
        public EditableDouble UI_TfMax;
        public EditableDouble UI_kpFactor;
        public EditableDouble UI_kiFactor;
        public EditableDouble UI_kdFactor;
        public EditableDouble UI_deadband;




        public MJAttitudeController(MechJebModuleAttitudeController controller) : base(controller)
        {
        }

        public override void OnStart()
        {
            pid = new PIDControllerV3(Vector3d.zero, Vector3d.zero, Vector3d.zero, 1, -1);
            setPIDParameters();
            lastAct = Vector3d.zero;


            UI_TfX = new EditableDouble(TfV.x);
            UI_TfY = new EditableDouble(TfV.y);
            UI_TfZ = new EditableDouble(TfV.z);
            UI_TfMin = new EditableDouble(TfMin);
            UI_TfMax = new EditableDouble(TfMax);
            UI_kpFactor = new EditableDouble(kpFactor);
            UI_kiFactor = new EditableDouble(kiFactor);
            UI_kdFactor = new EditableDouble(kdFactor);
            UI_deadband = new EditableDouble(deadband);

        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);
            TfV = TfVec;
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            TfVec = TfV;
            base.OnSave(local, type, global);
        }

        public void setPIDParameters()
        {
            Vector3d invTf = (Tf_autoTune ? TfV : defaultTfV).InvertNoNaN();

            pid.Kd = kdFactor * invTf;

            pid.Kp = (1 / (kpFactor * Math.Sqrt(2))) * pid.Kd;
            pid.Kp.Scale(invTf);

            pid.Ki = (1 / (kiFactor * Math.Sqrt(2))) * pid.Kp;
            pid.Ki.Scale(invTf);

            pid.intAccum = pid.intAccum.Clamp(-5, 5);
        }

        public void tuneTf(Vector3d torque)
        {
            Vector3d ratio = new Vector3d(
                torque.x != 0 ? ac.vesselState.MoI.x / torque.x : 0,
                torque.y != 0 ? ac.vesselState.MoI.y / torque.y : 0,
                torque.z != 0 ? ac.vesselState.MoI.z / torque.z : 0
            );

            TfV = 0.05 * ratio;

            Vector3d delayFactor = Vector3d.one + 2 * ac.vesselState.torqueReactionSpeed;


            TfV.Scale(delayFactor);


            TfV = TfV.Clamp(2.0 * TimeWarp.fixedDeltaTime, TfMax);
            TfV = TfV.Clamp(TfMin, TfMax);

            //Tf = Mathf.Clamp((float)ratio.magnitude / 20f, 2 * TimeWarp.fixedDeltaTime, 1f);
            //Tf = Mathf.Clamp((float)Tf, (float)TfMin, (float)TfMax);
        }

        public override void ResetConfig()
        {
            TfV = defaultTfV;
            TfMin = 0.1;
            TfMax = 0.5;
            kpFactor = 3;
            kiFactor = 6;
            kdFactor = 0.5;
            deadband = 0.0001;
            kWlimit = 0.15;
        }

        public override void Reset()
        {
            pid.Reset();
        }

        public override void DrivePre(FlightCtrlState s, out Vector3d act, out Vector3d deltaEuler)
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
            Quaternion targetDeRotated = Quaternion.AngleAxis((float) turnAngle, normVec) * ac.RequestedAttitude;
            float rollError = Vector3.Angle(vesselTransform.right, targetDeRotated * Vector3.right) *
                              Math.Sign(Vector3.Dot(targetDeRotated * Vector3.right, vesselTransform.forward));

            // From here everything should use MOI order for Vectors (pitch, roll, yaw)

            error = new Vector3d(
                -rotDirection.y * UtilMath.Deg2Rad,
                rollError * UtilMath.Deg2Rad,
                rotDirection.x * UtilMath.Deg2Rad
            );

            error.Scale(ac.AxisControl);

            Vector3d err = error + ac.inertia;
            err = new Vector3d(
                Math.Max(-Math.PI, Math.Min(Math.PI, err.x)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.y)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.z)));

            // ( MoI / available torque ) factor:
            Vector3d NormFactor = Vector3d.Scale(ac.vesselState.MoI, ac.torque.InvertNoNaN());

            err.Scale(NormFactor);

            // angular velocity:
            Vector3d omega = ac.vessel.angularVelocity;
            //omega.x = vessel.angularVelocity.x;
            //omega.y = vessel.angularVelocity.z; // y <=> z
            //omega.z = vessel.angularVelocity.y; // z <=> y
            omega.Scale(NormFactor);

            if (Tf_autoTune)
                tuneTf(ac.torque);
            setPIDParameters();

            // angular velocity limit:
            var Wlimit = new Vector3d(Math.Sqrt(NormFactor.x * Math.PI * kWlimit),
                Math.Sqrt(NormFactor.y * Math.PI * kWlimit),
                Math.Sqrt(NormFactor.z * Math.PI * kWlimit));

            pidAction = pid.Compute(err, omega, Wlimit);

            // deadband
            pidAction.x = Math.Abs(pidAction.x) >= deadband ? pidAction.x : 0.0;
            pidAction.y = Math.Abs(pidAction.y) >= deadband ? pidAction.y : 0.0;
            pidAction.z = Math.Abs(pidAction.z) >= deadband ? pidAction.z : 0.0;

            // low pass filter,  wf = 1/Tf:
            act = lastAct;
            if (lowPassFilter)
            {
                act.x += (pidAction.x - lastAct.x) * (1.0 / ((TfV.x / TimeWarp.fixedDeltaTime) + 1.0));
                act.y += (pidAction.y - lastAct.y) * (1.0 / ((TfV.y / TimeWarp.fixedDeltaTime) + 1.0));
                act.z += (pidAction.z - lastAct.z) * (1.0 / ((TfV.z / TimeWarp.fixedDeltaTime) + 1.0));
            }
            else
            {
                act = pidAction;
            }
            lastAct = act;

            deltaEuler = error * UtilMath.Rad2Deg;
        }

        public override void GUI()
        {
            if (GUILayout.Button(Localizer.Format("#MechJeb_adv_reset_button")))//"Reset"
            {
                ResetConfig();
            }

            Tf_autoTune = GUILayout.Toggle(Tf_autoTune, Localizer.Format("#MechJeb_AttitudeController_checkbox1"));//" Auto-tuning"

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.BeginVertical();

            if (!Tf_autoTune)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label1"));//"Larger ship do better with a larger Tf"

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label2"), GUILayout.ExpandWidth(true));//"Tf (s)"
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label3"), GUILayout.ExpandWidth(false));//"P"
                UI_TfX.text = GUILayout.TextField(UI_TfX.text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label4"), GUILayout.ExpandWidth(false));//"Y"
                UI_TfY.text = GUILayout.TextField(UI_TfY.text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label5"), GUILayout.ExpandWidth(false));//"R"
                UI_TfZ.text = GUILayout.TextField(UI_TfZ.text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
                GUILayout.EndHorizontal();

                UI_TfX = Math.Max(0.01, UI_TfX);
                UI_TfY = Math.Max(0.01, UI_TfY);
                UI_TfZ = Math.Max(0.01, UI_TfZ);
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label6"), GUILayout.ExpandWidth(true));//"Tf"
                GUILayout.Label(MuUtils.PrettyPrint(TfV), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label7"), GUILayout.ExpandWidth(true));//"Tf range"
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AttitudeController_label8"), UI_TfMin, "", 50);//"min"
                UI_TfMin = Math.Max(UI_TfMin, 0.01);
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AttitudeController_label9"), UI_TfMax, "", 50);//"max"
                UI_TfMax = Math.Max(UI_TfMax, 0.01);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            bool newLowPassFilter = GUILayout.Toggle(lowPassFilter, Localizer.Format("#MechJeb_AttitudeController_checkbox2"));//" Low Pass Filter"

            if (lowPassFilter != newLowPassFilter)
            {
                setPIDParameters();
                lowPassFilter = newLowPassFilter;
            }

            GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_PIDF"));//"PID factors"
            GuiUtils.SimpleTextBox("Kd = ", UI_kdFactor, " / Tf", 50);//
            UI_kdFactor = Math.Max(UI_kdFactor, 0.01);
            GuiUtils.SimpleTextBox("Kp = pid.Kd / (", UI_kpFactor, " * Math.Sqrt(2) * Tf)", 50);
            UI_kpFactor = Math.Max(UI_kpFactor, 0.01);
            GuiUtils.SimpleTextBox("Ki = pid.Kp / (", UI_kiFactor, " * Math.Sqrt(2) * Tf)", 50);
            UI_kiFactor = Math.Max(UI_kiFactor, 0.01);
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AttitudeController_PIDFactor1"), UI_deadband, "", 50);//"Deadband = "
            deadband = Math.Max(UI_deadband, 0.0);

            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AttitudeController_label11"), kWlimit, "%");//"Maximum Relative Angular Velocity"
            double tmp_kWlimit = kWlimit;
            tmp_kWlimit = (EditableDouble)GUILayout.HorizontalSlider((float)tmp_kWlimit, 0.0F, 1.0F);

            const int sliderPrecision = 3;
            if (Math.Round(Math.Abs(tmp_kWlimit - kWlimit), sliderPrecision) > 0)
            {
                kWlimit = Math.Round(tmp_kWlimit, sliderPrecision);
            }

            //showInfos = GUILayout.Toggle(showInfos, "Show Numbers");
            //if (showInfos)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label12"), GUILayout.ExpandWidth(true));//"Kp"
                GUILayout.Label(MuUtils.PrettyPrint(pid.Kp), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label13"), GUILayout.ExpandWidth(true));//"Ki"
                GUILayout.Label(MuUtils.PrettyPrint(pid.Ki), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label14"), GUILayout.ExpandWidth(true));//"Kd"
                GUILayout.Label(MuUtils.PrettyPrint(pid.Kd), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label15"), GUILayout.ExpandWidth(true));//"Error"
                GUILayout.Label(MuUtils.PrettyPrint(error * Mathf.Rad2Deg), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label16"), GUILayout.ExpandWidth(true));//"prop. action."
                GUILayout.Label(MuUtils.PrettyPrint(pid.propAct), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label17"), GUILayout.ExpandWidth(true));//"deriv. action"
                GUILayout.Label(MuUtils.PrettyPrint(pid.derivativeAct), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label18"), GUILayout.ExpandWidth(true));//"integral action."
                GUILayout.Label(MuUtils.PrettyPrint(pid.intAccum), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label19"), GUILayout.ExpandWidth(true));//"PID Action"
                GUILayout.Label(MuUtils.PrettyPrint(pidAction), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AttitudeController_label20"), GUILayout.ExpandWidth(true));//"Inertia"
                GUILayout.Label("|" + ac.inertia.magnitude.ToString("F3") + "| " + MuUtils.PrettyPrint(ac.inertia),
                    GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }

            if (!Tf_autoTune)
            {
                if (TfV.x != UI_TfX || TfV.y != UI_TfY || TfV.z != UI_TfZ)
                {
                    TfV.x = UI_TfX;
                    TfV.y = UI_TfY;
                    TfV.z = UI_TfZ;
                    setPIDParameters();
                }
            }
            else
            {
                if (TfMin != UI_TfMin || TfMax != UI_TfMax)
                {
                    TfMin = UI_TfMin;
                    TfMax = UI_TfMax;
                    setPIDParameters();
                }
            }
            if (kpFactor != UI_kpFactor || kiFactor != UI_kiFactor || kdFactor != UI_kdFactor)
            {
                kpFactor = UI_kpFactor;
                kiFactor = UI_kiFactor;
                kdFactor = UI_kdFactor;
                setPIDParameters();
            }
        }
    }
}
