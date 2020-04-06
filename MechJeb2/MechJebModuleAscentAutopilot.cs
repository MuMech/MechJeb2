using System;
using KSP.UI.Screens;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public enum ascentType { CLASSIC, GRAVITYTURN, PVG };

    //Todo: -reimplement measurement of LPA
    //      -Figure out exactly how throttle-limiting should work and interact
    //       with the global throttle-limit option
    public class MechJebModuleAscentAutopilot : ComputerModule
    {
        public MechJebModuleAscentAutopilot(MechJebCore core) : base(core) { }

        public string status = "";

        // deliberately private, do not bypass
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private int ascentPathIdx;

        // this is the public API for ascentPathIdx which is enum type and does wiring
        public ascentType ascentPathIdxPublic {
            get {
                return (ascentType) this.ascentPathIdx;
            }
            set {
                this.ascentPathIdx = (int) value;
                doWiring();
            }
        }

        // after manually loading the private ascentPathIdx (e.g. from a ConfigNode) this needs to be called to force the wiring
        public void doWiring()
        {
            ascentPath = ascentPathForType((ascentType)ascentPathIdx);
            ascentMenu = ascentMenuForType((ascentType)ascentPathIdx);
            disablePathModulesOtherThan(ascentPathIdx);
        }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult desiredOrbitAltitude = new EditableDoubleMult(100000, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public double desiredInclination = 0.0;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool autoThrottle = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool correctiveSteering = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble correctiveSteeringGain = 0.6; //control gain
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool forceRoll = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble verticalRoll = new EditableDouble(90);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble turnRoll = new EditableDouble(90);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool autodeploySolarPanels = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool autoDeployAntennas = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool skipCircularization = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool _autostage = true;
        public bool autostage
        {
            get { return _autostage; }
            set
            {
                bool changed = (value != _autostage);
                _autostage = value;
                if (changed)
                {
                    if (_autostage && this.enabled) core.staging.users.Add(this);
                    if (!_autostage) core.staging.users.Remove(this);
                }
            }
        }


        /* classic AoA limter */
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool limitAoA = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble maxAoA = 5;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult aoALimitFadeoutPressure = new EditableDoubleMult(2500);
        public bool limitingAoA = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble limitQa = new EditableDouble(2000);
        public bool limitQaEnabled = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble launchPhaseAngle = 0;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble launchLANDifference = 0;

        [Persistent(pass = (int)(Pass.Global))]
        public EditableInt warpCountDown = 11;

        [Persistent(pass = (int)(Pass.Global))]
        public bool showSettings = true;
        [Persistent(pass = (int)(Pass.Global))]
        public bool showTargeting = true;
        [Persistent(pass = (int)(Pass.Global))]
        public bool showGuidanceSettings = true;
        [Persistent(pass = (int)(Pass.Global))]
        public bool showStatus = true;


        public bool timedLaunch = false;
        public double launchTime = 0;

        public double currentMaxAoA = 0;

        public double launchLatitude = 0 ;

        // useful to track time since launch event (works around bugs with physics starting early and vessel.launchTime being wildly off)
        // FIXME?: any time we lift off from a rock this should probably be set to that time?
        public double launchStarted;

        public double tMinus
        {
            get { return launchTime - vesselState.time; }
        }

        //internal state:
        enum AscentMode { PRELAUNCH, ASCEND, CIRCULARIZE };
        AscentMode mode;
        bool placedCircularizeNode = false;
        private double lastTMinus = 999;

        // wiring for launchStarted
        public void OnLaunch(EventReport report)
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
            if ( vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.PRELAUNCH || vessel.situation == Vessel.Situations.SPLASHED )
                launchStarted = vesselState.time;
        }

        // various events can cause launchStarted to be before or after vessel.launchTime, but the most recent one is so far always the most accurate
        // (physics wobbles can start vessel.launchTime (KSP's zero MET) early, while staging before engaging the autopilot can cause launchStarted to happen early)
        // this will only be valid AFTER launching
        public double MET { get { return vesselState.time - ( launchStarted > vessel.launchTime ? launchStarted : vessel.launchTime ); } }

        public override void OnModuleEnabled()
        {
            // since we cannot serialize enums, we serialize ascentPathIdx instead, but this bypasses the code in the property, so on module
            // enabling, we force that value back through the property to enforce sanity.
            doWiring();

            ascentPath.enabled = true;

            mode = AscentMode.PRELAUNCH;

            placedCircularizeNode = false;

            launchLatitude = vesselState.latitude;

            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
            if (autostage) core.staging.users.Add(this);

            status = Localizer.Format("#MechJeb_Ascent_status1");//"Pre Launch"
        }

        public override void OnModuleDisabled()
        {
            ascentPath.enabled = false;
            core.attitude.attitudeDeactivate();
            if (!core.rssMode)
                core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
            core.staging.users.Remove(this);

            if (placedCircularizeNode) core.node.Abort();

            status = Localizer.Format("#MechJeb_Ascent_status2");//"Off"
        }

        public void StartCountdown(double time)
        {
            timedLaunch = true;
            launchTime = time;
            lastTMinus = 999;
        }

        public override void OnFixedUpdate()
        {
            FixupLaunchStart();
            if (timedLaunch)
            {
                if (tMinus < 3*vesselState.deltaT || (tMinus > 10.0 && lastTMinus < 1.0))
                {
                    if (enabled && vesselState.thrustAvailable < 10E-4) // only stage if we have no engines active
                        StageManager.ActivateNextStage();
                    ascentPath.timedLaunchHook();  // let ascentPath modules do stuff edge triggered on launch starting
                    timedLaunch = false;
                }
                else
                {
                    if (core.node.autowarp)
                        core.warp.WarpToUT(launchTime - warpCountDown);
                }
                lastTMinus = tMinus;
            }
        }

        public override void Drive(FlightCtrlState s)
        {
            limitingAoA = false;

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

        void DriveDeployableComponents(FlightCtrlState s)
        {
            if (autodeploySolarPanels)
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

            if (autoDeployAntennas)
            {
                if (vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude())
                    core.antennaControl.ExtendAll();
                else
                    core.antennaControl.RetractAll();
            }
        }

        void DrivePrelaunch(FlightCtrlState s)
        {
            if (vessel.LiftedOff() && !vessel.Landed) {
                status = Localizer.Format("#MechJeb_Ascent_status4");//"Vessel is not landed, skipping pre-launch"
                mode = AscentMode.ASCEND;
                return;
            }

            if (autoThrottle) {
                Debug.Log("prelaunch killing throttle");
                core.thrust.ThrustOff();
            }

            core.attitude.AxisControl(false, false, false);

            if (timedLaunch && tMinus > 10.0)
            {
                status = Localizer.Format("#MechJeb_Ascent_status1");//"Pre Launch"
                return;
            }

            if (autodeploySolarPanels && mainBody.atmosphere)
            {
                core.solarpanel.RetractAll();
                if (core.solarpanel.AllRetracted()) {
                    Debug.Log("Prelaunch -> Ascend");
                    mode = AscentMode.ASCEND;
                }
                else
                {
                    status = Localizer.Format("#MechJeb_Ascent_status5");//"Retracting solar panels"
                }
            } else {
                mode = AscentMode.ASCEND;
            }
        }

        void DriveAscent(FlightCtrlState s)
        {
            if (timedLaunch)
            {
                Debug.Log("Awaiting Liftoff");
                status = Localizer.Format("#MechJeb_Ascent_status6");//"Awaiting liftoff"
                // kill the optimizer if it is running.
                core.guidance.enabled = false;

                core.attitude.AxisControl(false, false, false);
                return;
            }

            DriveDeployableComponents(s);

            if ( ascentPath.DriveAscent(s) ) {
                if (GameSettings.VERBOSE_DEBUG_LOG) { Debug.Log("Remaining in Ascent"); }
                status = ascentPath.status;
            } else {
                if (GameSettings.VERBOSE_DEBUG_LOG) { Debug.Log("Ascend -> Circularize"); }
                mode = AscentMode.CIRCULARIZE;
            }
        }

        void DriveCircularizationBurn(FlightCtrlState s)
        {
            if (!vessel.patchedConicsUnlocked() || skipCircularization)
            {
                this.users.Clear();
                return;
            }

            DriveDeployableComponents(s);

            if (placedCircularizeNode)
            {
                if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                {
                    MechJebModuleFlightRecorder recorder = core.GetComputerModule<MechJebModuleFlightRecorder>();
                    if (recorder != null) launchPhaseAngle = recorder.phaseAngleFromMark;
                    if (recorder != null) launchLANDifference = vesselState.orbitLAN - recorder.markLAN;

                    //finished circularize
                    this.users.Clear();
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
                Vector3d inclinationCorrection = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, UT, Math.Abs(desiredInclination));
                Vector3d smaCorrection = OrbitalManeuverCalculator.DeltaVForSemiMajorAxis(orbit.PerturbedOrbit(UT, inclinationCorrection), UT,
                    desiredOrbitAltitude + mainBody.Radius);
                Vector3d dV = inclinationCorrection + smaCorrection;
                vessel.PlaceManeuverNode(orbit, dV, UT);
                placedCircularizeNode = true;

                core.node.ExecuteOneNode(this);
            }

            if (core.node.burnTriggered) status = Localizer.Format("#MechJeb_Ascent_status7");//"Circularizing"
            else status = Localizer.Format("#MechJeb_Ascent_status8");//"Coasting to circularization burn"
        }

        //////////////////////////////////////////////////
        // wiring for switching the different ascent types
        //////////////////////////////////////////////////

        public string[] ascentPathList = {Localizer.Format("#MechJeb_Ascent_ascentPathList1"),Localizer.Format("#MechJeb_Ascent_ascentPathList2"),Localizer.Format("#MechJeb_Ascent_ascentPathList3") };// "Classic Ascent Profile", "Stock-style GravityTurn™", "Primer Vector Guidance (RSS/RO)"

        public MechJebModuleAscentBase ascentPath;
        public MechJebModuleAscentMenuBase ascentMenu;

        private void disablePathModulesOtherThan(int type)
        {
            foreach(int i in Enum.GetValues(typeof(ascentType)))
            {
                if (i != (int)type)
                    enablePathModules((ascentType)i, false);
            }
        }

        private void enablePathModules(ascentType type, bool enabled)
        {
            ascentPathForType(type).enabled = enabled;
            var menu = ascentMenuForType(type);
            if (menu != null)
                menu.enabled = enabled;
        }

        private MechJebModuleAscentBase ascentPathForType(ascentType type)
        {
            switch (type)
            {
                case ascentType.CLASSIC:
                    return core.GetComputerModule<MechJebModuleAscentClassic>();
                case ascentType.GRAVITYTURN:
                    return core.GetComputerModule<MechJebModuleAscentGT>();
                case ascentType.PVG:
                    return core.GetComputerModule<MechJebModuleAscentPVG>();
            }
            return null;
        }

        private MechJebModuleAscentMenuBase ascentMenuForType(ascentType type)
        {
            if ( type == ascentType.CLASSIC )
                return core.GetComputerModule<MechJebModuleAscentClassicMenu>();
            else
                return null;
        }

    }

    public abstract class MechJebModuleAscentMenuBase : DisplayModule
    {
        public MechJebModuleAscentMenuBase(MechJebCore core) : base(core) { }
    }

    public abstract class MechJebModuleAscentBase : ComputerModule
    {
        public MechJebModuleAscentBase(MechJebCore core) : base(core) { }

        public string status { get; set; }

        public MechJebModuleAscentAutopilot autopilot { get { return core.GetComputerModule<MechJebModuleAscentAutopilot>(); } }
        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }

        public abstract bool DriveAscent(FlightCtrlState s);

        public Vector3d thrustVectorForNavball;

        //data used by ThrottleToRaiseApoapsis
        float raiseApoapsisLastThrottle = 0;
        double raiseApoapsisLastApR = 0;
        double raiseApoapsisLastUT = 0;
        MovingAverage raiseApoapsisRatePerThrottle = new MovingAverage(3, 0);

        public virtual void timedLaunchHook()
        {
            // triggered when timed launches start the actual launch
        }

        public override void OnModuleEnabled()
        {
            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
            if (!core.rssMode)
                core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
        }

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
                double instantRatePerThrottle = (orbit.ApR - raiseApoapsisLastApR) / ((vesselState.time - raiseApoapsisLastUT) * raiseApoapsisLastThrottle);
                instantRatePerThrottle = Math.Max(1.0, instantRatePerThrottle); //avoid problems from negative rates
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
            raiseApoapsisLastApR = orbit.ApR;
            raiseApoapsisLastUT = vesselState.time;

            return desiredThrottle;
        }

        protected double srfvelPitch() {
            return 90.0 - Vector3d.Angle(vesselState.surfaceVelocity, vesselState.up);
        }

        protected double srfvelHeading() {
            return vesselState.HeadingFromDirection(vesselState.surfaceVelocity.ProjectOnPlane(vesselState.up));
        }

        // this provides ground track heading based on desired inclination and is what most consumers should call
        protected void attitudeTo(double desiredPitch)
        {
            double desiredHeading = OrbitalManeuverCalculator.HeadingForLaunchInclination(vessel, vesselState, autopilot.desiredInclination);
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

            Vector3d desiredHeadingVector = Math.Sin(desiredHeading * UtilMath.Deg2Rad) * vesselState.east + Math.Cos(desiredHeading * UtilMath.Deg2Rad) * vesselState.north;

            Vector3d desiredThrustVector = Math.Cos(desiredPitch * UtilMath.Deg2Rad) * desiredHeadingVector
                + Math.Sin(desiredPitch * UtilMath.Deg2Rad) * vesselState.up;

            desiredThrustVector = desiredThrustVector.normalized;

            thrustVectorForNavball = desiredThrustVector;

            /* old style AoA limiter */
            if (autopilot.limitAoA && !autopilot.limitQaEnabled)
            {
                float fade = vesselState.dynamicPressure < autopilot.aoALimitFadeoutPressure ? (float)(autopilot.aoALimitFadeoutPressure / vesselState.dynamicPressure) : 1;
                autopilot.currentMaxAoA = Math.Min(fade * autopilot.maxAoA, 180d);
                autopilot.limitingAoA = vessel.altitude < mainBody.atmosphereDepth && Vector3.Angle(vesselState.surfaceVelocity, desiredThrustVector) > autopilot.currentMaxAoA;

                if (autopilot.limitingAoA)
                {
                    desiredThrustVector = Vector3.RotateTowards(vesselState.surfaceVelocity, desiredThrustVector, (float)(autopilot.currentMaxAoA * Mathf.Deg2Rad), 1).normalized;
                }
            }

            /* AoA limiter for PVG */
            if (autopilot.limitQaEnabled)
            {
                double lim = MuUtils.Clamp(autopilot.limitQa, 100, 10000);
                autopilot.limitingAoA = vesselState.dynamicPressure * Vector3.Angle(vesselState.surfaceVelocity, desiredThrustVector) * UtilMath.Deg2Rad > lim;
                if (autopilot.limitingAoA)
                {
                    autopilot.currentMaxAoA = lim / vesselState.dynamicPressure * UtilMath.Rad2Deg;
                    desiredThrustVector = Vector3.RotateTowards(vesselState.surfaceVelocity, desiredThrustVector, (float)(autopilot.currentMaxAoA * UtilMath.Deg2Rad), 1).normalized;
                }
            }

            double pitch = 90 - Vector3d.Angle(desiredThrustVector, vesselState.up);

            double hdg;

            if (pitch > 89.9) {
                hdg = desiredHeading;
            } else {
                hdg = MuUtils.ClampDegrees360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(desiredThrustVector, vesselState.east), Vector3d.Dot(desiredThrustVector, vesselState.north)));
            }

            if (autopilot.forceRoll)
            {
                core.attitude.AxisControl(!vessel.Landed, !vessel.Landed, !vessel.Landed && (vesselState.altitudeBottom > 50));
                if ( desiredPitch == 90.0)
                {
                    core.attitude.attitudeTo(hdg, pitch, autopilot.verticalRoll, this, !vessel.Landed, !vessel.Landed, !vessel.Landed && (vesselState.altitudeBottom > 50));
                }
                else
                {
                    core.attitude.attitudeTo(hdg, pitch, autopilot.turnRoll, this, !vessel.Landed, !vessel.Landed, !vessel.Landed && (vesselState.altitudeBottom > 50));
                }
            }
            else
            {
                core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, this);
            }
        }

    }

    public static class LaunchTiming
    {
        //Computes the time until the phase angle between the launchpad and the target equals the given angle.
        //The convention used is that phase angle is the angle measured starting at the target and going east until
        //you get to the launchpad.
        //The time returned will not be exactly accurate unless the target is in an exactly circular orbit. However,
        //the time returned will go to exactly zero when the desired phase angle is reached.
        public static double TimeToPhaseAngle(double phaseAngle, CelestialBody launchBody, double launchLongitude, Orbit target)
        {
            double launchpadAngularRate = 360 / launchBody.rotationPeriod;
            double targetAngularRate = 360.0 / target.period;
            if (Vector3d.Dot(-target.GetOrbitNormal().Reorder(132).normalized, launchBody.angularVelocity) < 0) targetAngularRate *= -1; //retrograde target

            Vector3d currentLaunchpadDirection = launchBody.GetSurfaceNVector(0, launchLongitude);
            Vector3d currentTargetDirection = target.SwappedRelativePositionAtUT(Planetarium.GetUniversalTime());
            currentTargetDirection = Vector3d.Exclude(launchBody.angularVelocity, currentTargetDirection);

            double currentPhaseAngle = Math.Abs(Vector3d.Angle(currentLaunchpadDirection, currentTargetDirection));
            if (Vector3d.Dot(Vector3d.Cross(currentTargetDirection, currentLaunchpadDirection), launchBody.angularVelocity) < 0)
            {
                currentPhaseAngle = 360 - currentPhaseAngle;
            }

            double phaseAngleRate = launchpadAngularRate - targetAngularRate;

            double phaseAngleDifference = MuUtils.ClampDegrees360(phaseAngle - currentPhaseAngle);

            if (phaseAngleRate < 0)
            {
                phaseAngleRate *= -1;
                phaseAngleDifference = 360 - phaseAngleDifference;
            }


            return phaseAngleDifference / phaseAngleRate;
        }

        //Computes the time required for the given launch location to rotate under the target orbital plane.
        //If the latitude is too high for the launch location to ever actually rotate under the target plane,
        //returns the time of closest approach to the target plane.
        public static double TimeToPlane(double LANDifference, CelestialBody launchBody, double launchLatitude, double launchLongitude, Orbit target)
        {
            // Consider a coordinate system where the origin is at the center of the launch body,
            // the y-axis points toward the north pole of the launch body, and the orbit normal has
            // zero z-component and positive x-component. (Note that, due to KSP's left-handed
            // coordinate convention, the y-component of a low-inclination orbit's normal is
            // negative.) Then the plane containing the target orbit is defined by
            //
            //     y = tan(inc) x,
            //
            // where inc is the orbit's inclination. Meanwhile, the points that have the given
            // latitude are defined, up to scale, by
            //
            //     (cos(theta) cos(lat), sin(lat), sin(theta) cos(lat)),
            //
            // where lat is the latitude and theta is free to vary. We want to find the
            // intersections of these two sets of points; that is, we want
            //
            //     cos(theta) cos(lat) tan(inc) = sin(lat)
            //
            //     cos(theta) = tan(lat) / tan(inc)
            //
            //     theta = +/- acos(tan(lat) / tan(inc)).
            //
            // More broadly, when there are two solutions, they will be at an angle of acos(tan(lat)
            // / tan(inc)) on either side of the projection of the inclination vector into the x-z
            // (equatorial) plane.
            //
            // If, however, the latitude is too far from zero (i.e., |tan(lat)| > |tan(inc)|), there
            // is no solution; then we want to find the single point that is closest to the orbital
            // plane. If the latitude is too far toward the same pole that the orbit normal is
            // closer to, the closest point is opposite the orbit normal; if it's too far toward the
            // other pole, it's in the same direction as the normal.
            //
            // Both cases can be handled neatly in the same way as the two-solution case by setting
            // theta = pi or theta = 0, respectively (then the two "solutions" collapse to one
            // point). That is equivalent to clamping the ratio of the tangents to the range [-1, 1]
            // in the last equation above.

            // Because of KSP's left-handed coordinate convention, the angular velocity vector
            // points to the south, as does the orbit normal for prograde orbits.
            Vector3d south = launchBody.angularVelocity.normalized;
            Vector3d orbitNormal = target.SwappedOrbitNormal().normalized;
            double inclination = Vector3d.Angle(orbitNormal, south);

            // For nearly equatorial orbits (whether prograde or retrograde), the launch time makes
            // a negligible difference, so always say to launch right now. This also helps avoid odd
            // results when the projection of the orbit normal into the equatorial plane is
            // basically just noise.
            if (inclination < 0.01 || inclination > 179.99) {
                return 0;
            }

            // Compute the angle between the opposite of the orbit normal and each intersection
            // point (when everything is projected onto the equatorial plane). Because low
            // inclinations lead to an orbit normal pointing near the south pole, while latitude
            // increases toward the north, tanRatio > 0 means that the latitude and normal are on
            // opposite sides of the equator.
            double tanRatio = Math.Tan(UtilMath.Deg2Rad * launchLatitude) / Math.Tan(UtilMath.Deg2Rad * inclination);
            double separation = Math.Acos(MuUtils.Clamp(tanRatio, -1, 1));

            // Compute the signed angle from the launch point to the orbit normal within the
            // equatorial plane. Positive is following the rotation of the body. The formula relies
            // on one of v1 and v2 being orthogonal to south (v1, in this case) but not both.
            Vector3d v1 = launchBody.GetSurfaceNVector(0, launchLongitude).normalized;
            Vector3d v2 = orbitNormal;
            double centerAngle = Math.Atan2(Vector3d.Dot(Vector3d.Cross(v1, v2), south), Vector3.Dot(v1, v2)) - LANDifference * UtilMath.Deg2Rad;

            // Compute the angle from the launch point to each solution point. Here, we don't want a
            // signed angle; we want a result between 0 and 2pi, since that will tell us how much
            // the planet has to rotate.
            double angle1 = MuUtils.ClampRadiansTwoPi(centerAngle - separation);
            double angle2 = MuUtils.ClampRadiansTwoPi(centerAngle + separation);

            // Finally, the remaining time is the shorter angular distance over the angular speed.
            return Math.Min(angle1, angle2) / launchBody.angularVelocity.magnitude;
        }
    }
}
