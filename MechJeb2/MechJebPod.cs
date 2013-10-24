using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebPod : PartModule
    {
        public enum State
        {
            OFF,
            AWAKENING,
            AWAKE,
            SLEEPING
        }

        MechJebCore core = null;
        Transform eye_base, eye_ball;
        AerodynamicsFX afx;
        float lastBlink = 0, lastAction = 0;
        float[] lastFlaps;
        public State state;

        public override void OnStart(StartState state)
        {
            core = part.Modules.OfType<MechJebCore>().FirstOrDefault();
            eye_base = part.FindModelTransform("r4m0n_Control_point_socket"); //    Rotation: 0, 0, Z Azimuth
            eye_ball = part.FindModelTransform("r4m0n_Control_point_Eye"); //       Rotation: X, 0, 0 Altitude
            lastFlaps = new float[] { 0, 0, 0, 0 };
        }

        public float AdvanceAnimationTo(Animation anim, string clip, float to, float dt, float last = -1)
        {
            float ret = to;

            AnimationState st = anim[clip];
            st.enabled = true;
            st.weight = 1;
            if (last < 0)
            {
                last = st.normalizedTime;
            }
            ret = st.normalizedTime = Mathf.MoveTowards(last, to, dt * st.length);
            anim.Sample();
            st.enabled = false;

            return ret;
        }

        public void Update()
        {
            if (afx == null)
            {
                GameObject fx = GameObject.Find("FXLogic");
                if (fx != null)
                {
                    afx = fx.GetComponent<AerodynamicsFX>();
                }
            }
            Animation flapsAnim = part.FindModelAnimators("Flap_Top_Right")[0];
            if (vessel != null && vessel.mainBody.atmosphere && vessel.altitude < vessel.mainBody.RealMaxAtmosphereAltitude())
            {
                float direction = ((vessel.GetSrfVelocity().magnitude > 25) && (Vector3.Angle(vessel.transform.up, vessel.GetSrfVelocity()) > 90)) ? -1 : 1;

                lastFlaps[0] = AdvanceAnimationTo(flapsAnim, "Flap_Top_Right", Mathf.Clamp01(direction * (vessel.ctrlState.pitch - vessel.ctrlState.yaw) / 2), TimeWarp.deltaTime, lastFlaps[0]);
                lastFlaps[1] = AdvanceAnimationTo(flapsAnim, "Flap_Top_Left", Mathf.Clamp01(direction * (vessel.ctrlState.pitch + vessel.ctrlState.yaw) / 2), TimeWarp.deltaTime, lastFlaps[1]);
                lastFlaps[2] = AdvanceAnimationTo(flapsAnim, "Flap_Bottom_Right", Mathf.Clamp01(direction * (-vessel.ctrlState.pitch - vessel.ctrlState.yaw) / 2), TimeWarp.deltaTime, lastFlaps[2]);
                lastFlaps[3] = AdvanceAnimationTo(flapsAnim, "Flap_Bottom_Left", Mathf.Clamp01(direction * (-vessel.ctrlState.pitch + vessel.ctrlState.yaw) / 2), TimeWarp.deltaTime, lastFlaps[3]);
            }
            else
            {
                lastFlaps[0] = AdvanceAnimationTo(flapsAnim, "Flap_Top_Right", 0, TimeWarp.deltaTime, lastFlaps[0]);
                lastFlaps[1] = AdvanceAnimationTo(flapsAnim, "Flap_Top_Left", 0, TimeWarp.deltaTime, lastFlaps[1]);
                lastFlaps[2] = AdvanceAnimationTo(flapsAnim, "Flap_Bottom_Right", 0, TimeWarp.deltaTime, lastFlaps[2]);
                lastFlaps[3] = AdvanceAnimationTo(flapsAnim, "Flap_Bottom_Left", 0, TimeWarp.deltaTime, lastFlaps[3]);
            }
            switch (state)
            {
                case State.OFF:
                    if (HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && vessel != null && core.attitude.enabled))
                    {
                        state = State.AWAKENING;
                        part.FindModelAnimators("Waking")[0].Play("Waking");
                    }
                    break;
                case State.AWAKENING:
                    if (!part.FindModelAnimators("Waking")[0].isPlaying)
                    {
                        AdvanceAnimationTo(part.FindModelAnimators("Waking")[0], "Waking", 1, 100);

                        state = State.AWAKE;
                        lastBlink = lastAction = Time.time;
                    }
                    break;
                case State.AWAKE:
                    if (!part.FindModelAnimators("Blink")[0].isPlaying)
                    {
                        if ((UnityEngine.Random.Range(0, 10.0F / (HighLogic.LoadedSceneIsEditor ? Time.deltaTime : TimeWarp.deltaTime)) < (Time.time - lastBlink)))
                        {
                            part.FindModelAnimators("Blink")[0].Play("Blink");
                            lastBlink = Time.time;
                        }
                        GameObject cam = HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.editorCamera.gameObject : FlightCamera.fetch.gameObject;
                        Vector3 target = (cam.transform.position - part.transform.position).normalized;
                        if (core.attitude.enabled)
                        {
                            target = core.attitude.attitudeGetReferenceRotation(core.attitude.attitudeReference) * core.attitude.attitudeTarget * Quaternion.Euler(90, 0, 0) * Vector3.up;
                            lastAction = Time.time;
                        }
                        Vector3 localTarget = part.transform.InverseTransformDirection(target);
                        Vector2 polarTarget = CartesianToPolar(localTarget);
                        if (Mathfx.Approx(polarTarget.x, -90, 1) || Mathfx.Approx(polarTarget.x, 90, 1))
                        {
                            polarTarget.y = eye_base.localEulerAngles.z + 90;
                        }
                        if ((!HighLogic.LoadedSceneIsEditor && (Time.time - lastAction > 30)))
                        {
                            if (Mathfx.Approx(eye_base.localEulerAngles.z, 0, 1) && Mathfx.Approx(eye_ball.localEulerAngles.x, 0, 1))
                            {
                                state = State.SLEEPING;
                                part.FindModelAnimators("Sleeping")[0].Play("Sleeping");
                            }
                            else
                            {
                                polarTarget = new Vector2(-90, 90);
                            }
                        }
                        if (afx != null && afx.FxScalar > 0)
                        {
                            polarTarget = new Vector2(90, 90);
                        }
                        eye_base.localEulerAngles = new Vector3(0, 0, Mathf.MoveTowardsAngle(eye_base.localEulerAngles.z, polarTarget.y - 90, 360 * Time.deltaTime));
                        eye_ball.localRotation = Quaternion.RotateTowards(eye_ball.localRotation, Quaternion.Euler(polarTarget.x + 90, 0, 0), 360 * Time.deltaTime);
                    }
                    break;
                case State.SLEEPING:
                    if (!part.FindModelAnimators("Sleeping")[0].isPlaying)
                    {
                        AdvanceAnimationTo(part.FindModelAnimators("Sleeping")[0], "Sleeping", 1, 100);

                        state = State.OFF;
                    }
                    break;
                default:
                    break;
            }
        }

        Vector2 CartesianToPolar(Vector3 vector)
        {
            Vector2 polar;
            polar.y = Mathf.Atan2(vector.x, vector.z);
            float xzLen = new Vector2(vector.x, vector.z).magnitude;
            polar.x = Mathf.Atan2(-vector.y, xzLen);
            polar *= Mathf.Rad2Deg;
            return polar;
        }
    }
}
