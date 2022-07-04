using System;
using KSP.Localization;
using KSP.UI.Screens;
using UnityEngine;

namespace MuMech
{
    public abstract class MechJebModuleAscentBaseAutopilot : ComputerModule
    {
        protected MechJebModuleAscentBaseAutopilot(MechJebCore core) : base(core) { }

        public string Status = "";

        protected MechJebModuleAscentSettings AscentSettings => core.ascentSettings;
        
        public bool   timedLaunch;
        public double launchTime;

        public double currentMaxAoA;

        // useful to track time since launch event (works around bugs with physics starting early and vessel.launchTime being wildly off)
        // FIXME?: any time we lift off from a rock this should probably be set to that time?
        public double launchStarted;

        public double tMinus => launchTime - vesselState.time;

        //internal state:
        private enum AscentMode { PRELAUNCH, ASCEND, CIRCULARIZE }

        private AscentMode mode;
        private bool       placedCircularizeNode;
        private double     lastTMinus = 999;

        // wiring for launchStarted
        private void OnLaunch(EventReport report)
        {
            launchStarted = vesselState.time;
            Debug.Log("[MechJebModuleAscentAutopilot] LaunchStarted = " + launchStarted);
        }

        // wiring for launchStarted
        public override void OnStart(PartModule.StartState state)
        {
            launchStarted = -1;
            GameEvents.onLaunch.Add(OnLaunch);
        }

        private void FixupLaunchStart()
        {
            // continuously update the launchStarted time if we're sitting on the ground on the water anywhere (once we are not, then we've launched)
            if (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.PRELAUNCH ||
                vessel.situation == Vessel.Situations.SPLASHED)
                launchStarted = vesselState.time;
        }

        // various events can cause launchStarted to be before or after vessel.launchTime, but the most recent one is so far always the most accurate
        // (physics wobbles can start vessel.launchTime (KSP's zero MET) early, while staging before engaging the autopilot can cause launchStarted to happen early)
        // this will only be valid AFTER launching
        protected double MET => vesselState.time - (launchStarted > vessel.launchTime ? launchStarted : vessel.launchTime);

        public override void OnModuleEnabled()
        {
            mode = AscentMode.PRELAUNCH;

            placedCircularizeNode = false;

            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
            if (AscentSettings.autostage) core.staging.users.Add(this);

            Status = Localizer.Format("#MechJeb_Ascent_status1"); //"Pre Launch"
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
            if (!core.rssMode)
                core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
            core.staging.users.Remove(this);

            if (placedCircularizeNode) core.node.Abort();

            Status = Localizer.Format("#MechJeb_Ascent_status2"); //"Off"
        }

        public void StartCountdown(double time)
        {
            timedLaunch = true;
            launchTime  = time;
            lastTMinus  = 999;
        }

        public override void OnFixedUpdate()
        {
            FixupLaunchStart();
            if (timedLaunch)
            {
                if (tMinus < 3 * vesselState.deltaT || (tMinus > 10.0 && lastTMinus < 1.0))
                {
                    if (enabled && vesselState.thrustAvailable < 10E-4) // only stage if we have no engines active
                        StageManager.ActivateNextStage();
                    timedLaunchHook(); // let ascentPath modules do stuff edge triggered on launch starting
                    timedLaunch = false;
                }
                else
                {
                    if (core.node.autowarp)
                        core.warp.WarpToUT(launchTime - AscentSettings.warpCountDown);
                }

                lastTMinus = tMinus;
            }
        }

        public override void Drive(FlightCtrlState s)
        {
            AscentSettings.limitingAoA = false;

            switch (mode)
            {
                case AscentMode.PRELAUNCH:
                    DrivePrelaunch(s);
                    break;

                case AscentMode.ASCEND:
                    DriveAscent(s);
                    break;

                case AscentMode.CIRCULARIZE:
                    DriveCircularizationBurn(s);
                    break;
            }
        }

        private void DriveDeployableComponents(FlightCtrlState s)
        {
            if (AscentSettings.autodeploySolarPanels)
            {
                if (vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude())
                {
                    core.solarpanel.ExtendAll();
                }
                else
                {
                    core.solarpanel.RetractAll();
                }
            }

            if (AscentSettings.autoDeployAntennas)
            {
                if (vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude())
                    core.antennaControl.ExtendAll();
                else
                    core.antennaControl.RetractAll();
            }
        }

        private void DrivePrelaunch(FlightCtrlState s)
        {
            if (vessel.LiftedOff() && !vessel.Landed)
            {
                Status = Localizer.Format("#MechJeb_Ascent_status4"); //"Vessel is not landed, skipping pre-launch"
                mode   = AscentMode.ASCEND;
                return;
            }

            Debug.Log("prelaunch killing throttle");
            core.thrust.ThrustOff();

            core.attitude.SetAxisControl(false, false, false);

            if (timedLaunch && tMinus > 10.0)
            {
                Status = Localizer.Format("#MechJeb_Ascent_status1"); //"Pre Launch"
                return;
            }

            if (AscentSettings.autodeploySolarPanels && mainBody.atmosphere)
            {
                core.solarpanel.RetractAll();
                if (core.solarpanel.AllRetracted())
                {
                    Debug.Log("Prelaunch -> Ascend");
                    mode = AscentMode.ASCEND;
                }
                else
                {
                    Status = Localizer.Format("#MechJeb_Ascent_status5"); //"Retracting solar panels"
                }
            }
            else
            {
                mode = AscentMode.ASCEND;
            }
        }

        private void DriveAscent(FlightCtrlState s)
        {
            if (timedLaunch)
            {
                Debug.Log("Awaiting Liftoff");
                Status = Localizer.Format("#MechJeb_Ascent_status6"); //"Awaiting liftoff"
                // kill the optimizer if it is running.
                core.guidance.enabled = false;

                core.attitude.SetAxisControl(false, false, false);
                return;
            }

            DriveDeployableComponents(s);

            if (DriveAscent2(s))
            {
                if (GameSettings.VERBOSE_DEBUG_LOG) { Debug.Log("Remaining in Ascent"); }
            }
            else
            {
                if (GameSettings.VERBOSE_DEBUG_LOG) { Debug.Log("Ascend -> Circularize"); }

                mode = AscentMode.CIRCULARIZE;
            }
        }

        private void DriveCircularizationBurn(FlightCtrlState s)
        {
            if (!vessel.patchedConicsUnlocked() || AscentSettings.skipCircularization)
            {
                users.Clear();
                return;
            }

            DriveDeployableComponents(s);

            if (placedCircularizeNode)
            {
                if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                {
                    MechJebModuleFlightRecorder recorder = core.GetComputerModule<MechJebModuleFlightRecorder>();
                    if (recorder != null) AscentSettings.launchPhaseAngle    = recorder.phaseAngleFromMark;
                    if (recorder != null) AscentSettings.launchLANDifference = vesselState.orbitLAN - recorder.markLAN;

                    //finished circularize
                    users.Clear();
                    return;
                }
            }
            else
            {
                //place circularization node
                vessel.RemoveAllManeuverNodes();
                double UT = orbit.NextApoapsisTime(vesselState.time);
                //During the circularization burn, try to correct any inclination errors because it's better to combine the two burns.
                //  For example, if you're about to do a 1500 m/s circularization burn, if you combine a 200 m/s inclination correction
                //  into it, you actually only spend 1513 m/s to execute combined manuver.  Mechjeb should also do correction burns before
                //  this if possible, and this can't correct all errors... but it's better then nothing.
                //   (A better version of this should try to match inclination & LAN if target is specified)
                // FIXME? this inclination correction is unlikely to be at tha AN/DN and will throw the LAN off with anything other than high
                // TWR launches from equatorial launch sites -- should probably be made optional (or clip it if the correction is too large).
                Vector3d inclinationCorrection =
                    OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, UT, Math.Abs(AscentSettings.desiredInclination));
                Vector3d smaCorrection = OrbitalManeuverCalculator.DeltaVForSemiMajorAxis(orbit.PerturbedOrbit(UT, inclinationCorrection), UT,
                    AscentSettings.desiredOrbitAltitude + mainBody.Radius);
                Vector3d dV = inclinationCorrection + smaCorrection;
                vessel.PlaceManeuverNode(orbit, dV, UT);
                placedCircularizeNode = true;

                core.node.ExecuteOneNode(this);
            }

            if (core.node.burnTriggered) Status = Localizer.Format("#MechJeb_Ascent_status7"); //"Circularizing"
            else Status                         = Localizer.Format("#MechJeb_Ascent_status8"); //"Coasting to circularization burn"
        }


        
        public abstract bool DriveAscent2(FlightCtrlState s);

        public virtual void timedLaunchHook()
        {
            // triggered when timed launches start the actual launch
        }
        
        
        //data used by ThrottleToRaiseApoapsis
        private          float         raiseApoapsisLastThrottle;
        private          double        raiseApoapsisLastApR;
        private          double        raiseApoapsisLastUT;
        private readonly MovingAverage raiseApoapsisRatePerThrottle = new MovingAverage(3);


        //gives a throttle setting that reduces as we approach the desired apoapsis
        //so that we can precisely match the desired apoapsis instead of overshooting it
        protected float ThrottleToRaiseApoapsis(double currentApR, double finalApR)
        {
            float desiredThrottle;

            if (currentApR > finalApR + 5.0)
            {
                desiredThrottle = 0.0F; //done, throttle down
            }
            else if (raiseApoapsisLastUT > vesselState.time - 1)
            {
                //reduce throttle as apoapsis nears target
                double instantRatePerThrottle =
                    (orbit.ApR - raiseApoapsisLastApR) / ((vesselState.time - raiseApoapsisLastUT) * raiseApoapsisLastThrottle);
                instantRatePerThrottle             = Math.Max(1.0, instantRatePerThrottle); //avoid problems from negative rates
                raiseApoapsisRatePerThrottle.value = instantRatePerThrottle;
                double desiredApRate = (finalApR - currentApR) / 1.0;
                desiredThrottle = Mathf.Clamp((float)(desiredApRate / raiseApoapsisRatePerThrottle), 0.05F, 1.0F);
            }
            else
            {
                desiredThrottle = 1.0F; //no recent data point; just use max thrust.
            }

            //record data for next frame
            raiseApoapsisLastThrottle = desiredThrottle;
            raiseApoapsisLastApR      = orbit.ApR;
            raiseApoapsisLastUT       = vesselState.time;

            return desiredThrottle;
        }

        protected double srfvelPitch()
        {
            return 90.0 - Vector3d.Angle(vesselState.surfaceVelocity, vesselState.up);
        }

        protected double srfvelHeading()
        {
            return vesselState.HeadingFromDirection(vesselState.surfaceVelocity.ProjectOnPlane(vesselState.up));
        }

        // this provides ground track heading based on desired inclination and is what most consumers should call
        protected void attitudeTo(double desiredPitch)
        {
            double desiredHeading = OrbitalManeuverCalculator.HeadingForLaunchInclination(vessel, vesselState, AscentSettings.desiredInclination);
            attitudeTo(desiredPitch, desiredHeading);
        }

        // provides AoA limiting and roll control
        // provides no ground tracking and should only be called by autopilots like PVG that deeply know what they're doing with yaw control
        // (possibly should be moved into the attitude controller, but right now it collaborates too heavily with the ascent autopilot)
        //
        protected void attitudeTo(double desiredPitch, double desiredHeading)
        {
            /*
            Vector6 rcs = vesselState.rcsThrustAvailable;

            // FIXME?  should this be up/down and not forward/back?  seems wrong?  why was i using down before for the ullage direction?
            bool has_rcs = vessel.hasEnabledRCSModules() && vessel.ActionGroups[KSPActionGroup.RCS] && ( rcs.left > 0.01 ) && ( rcs.right > 0.01 ) && ( rcs.forward > 0.01 ) && ( rcs.back > 0.01 );

            if ( (vesselState.thrustCurrent / vesselState.thrustAvailable < 0.50) && !has_rcs )
            {
                // if engines are spooled up at less than 50% and we have no RCS in the stage, do not issue any guidance commands yet
                return;
            }
            */

            Vector3d desiredHeadingVector = Math.Sin(desiredHeading * UtilMath.Deg2Rad) * vesselState.east +
                                            Math.Cos(desiredHeading * UtilMath.Deg2Rad) * vesselState.north;

            Vector3d desiredThrustVector = Math.Cos(desiredPitch * UtilMath.Deg2Rad) * desiredHeadingVector
                                           + Math.Sin(desiredPitch * UtilMath.Deg2Rad) * vesselState.up;

            desiredThrustVector = desiredThrustVector.normalized;
            
            /* old style AoA limiter */
            if (AscentSettings.limitAoA && !AscentSettings.limitQaEnabled)
            {
                float fade = vesselState.dynamicPressure < AscentSettings.aoALimitFadeoutPressure
                    ? (float)(AscentSettings.aoALimitFadeoutPressure / vesselState.dynamicPressure)
                    : 1;
                currentMaxAoA = Math.Min(fade * AscentSettings.maxAoA, 180d);
                AscentSettings.limitingAoA = vessel.altitude < mainBody.atmosphereDepth &&
                                              Vector3.Angle(vesselState.surfaceVelocity, desiredThrustVector) > currentMaxAoA;

                if (AscentSettings.limitingAoA)
                {
                    desiredThrustVector = Vector3.RotateTowards(vesselState.surfaceVelocity, desiredThrustVector,
                        (float)(currentMaxAoA * Mathf.Deg2Rad), 1).normalized;
                }
            }

            /* AoA limiter for PVG */
            if (AscentSettings.limitQaEnabled)
            {
                double lim = MuUtils.Clamp(AscentSettings.limitQa, 100, 10000);
                AscentSettings.limitingAoA =
                    vesselState.dynamicPressure * Vector3.Angle(vesselState.surfaceVelocity, desiredThrustVector) * UtilMath.Deg2Rad > lim;
                if (AscentSettings.limitingAoA)
                {
                    currentMaxAoA = lim / vesselState.dynamicPressure * UtilMath.Rad2Deg;
                    desiredThrustVector = Vector3.RotateTowards(vesselState.surfaceVelocity, desiredThrustVector,
                        (float)(currentMaxAoA * UtilMath.Deg2Rad), 1).normalized;
                }
            }

            bool liftedOff = vessel.LiftedOff() && !vessel.Landed && vesselState.altitudeBottom > 5;

            double pitch = 90 - Vector3d.Angle(desiredThrustVector, vesselState.up);

            double hdg;

            if (pitch > 89.9)
            {
                hdg = desiredHeading;
            }
            else
            {
                hdg = MuUtils.ClampDegrees360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(desiredThrustVector, vesselState.east),
                    Vector3d.Dot(desiredThrustVector, vesselState.north)));
            }

            if (AscentSettings.forceRoll)
            {
                if (desiredPitch == 90.0)
                {
                    core.attitude.attitudeTo(hdg, pitch, AscentSettings.verticalRoll, this, liftedOff, liftedOff,
                        liftedOff && vesselState.altitudeBottom > AscentSettings.rollAltitude, true);
                }
                else
                {
                    core.attitude.attitudeTo(hdg, pitch, AscentSettings.turnRoll, this, liftedOff, liftedOff,
                        liftedOff && vesselState.altitudeBottom > AscentSettings.rollAltitude, true);
                }
            }
            else
            {
                core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL_COT, this);
            }

            core.attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && vesselState.altitudeBottom > AscentSettings.rollAltitude);
        }
    }
}
