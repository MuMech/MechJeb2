using KSP.Localization;
using KSP.UI.Screens;
using MechJebLib.Functions;

namespace MuMech
{
    /// <summary>
    ///     The launch sequence the ascent autopilots share: the countdown, warping to it, staging at T-0 and the prelaunch hold.
    ///     An engaged autopilot adds itself to Users, so a countdown started while no autopilot is engaged doesn't run until one is.
    /// </summary>
    public class MechJebModuleLaunchCountdown : ComputerModule
    {
        public MechJebModuleLaunchCountdown(MechJebCore core) : base(core) { }

        private MechJebModuleAscentSettings AscentSettings => Core.AscentSettings;

        private double _launchTime;
        private double _lastTMinus = 999;

        public bool Active { get; private set; }

        public double TMinus => _launchTime - VesselState.Time;

        // Starting a countdown flushes the PSG solution and the optimizer restarts cold late in the countdown, which can take longer
        // than the countdown does. Staging with no solution would release the rocket at zero throttle, so T-0 waits for one.
        private bool WaitingForGuidance => AscentSettings.AscentType == AscentType.PSG && Core.Guidance.Solution == null;

        // the inclination the selected ascent type flies
        public EditableDouble InclinationSetting =>
            AscentSettings.AscentType == AscentType.PSG ? Core.PSGSimulation.Inc : AscentSettings.DesiredInclination;

        /// <summary>Counts down to the next launch into the target's plane and sets the inclination to match. Returns the launch UT.</summary>
        public double StartToPlaneOfTarget()
        {
            AscentSettings.LaunchingToPlane = true;

            Orbit targetOrbit = Core.Target.TargetOrbit;
            (double timeToPlane, double inclination) = Astro.MinimumTimeToPlane(
                MainBody.rotationPeriod,
                VesselState.Latitude,
                VesselState.CelestialLongitude,
                targetOrbit.LAN - AscentSettings.LaunchLANDifference,
                targetOrbit.inclination);

            InclinationSetting.Val = inclination;
            return Start(timeToPlane);
        }

        /// <summary>Counts down to the next launch matching the target's LAN at the set inclination. Returns the launch UT.</summary>
        public double StartToTargetLan()
        {
            AscentSettings.LaunchingToMatchLan = true;
            return Start(TimeToPlane(Core.Target.TargetOrbit.LAN - AscentSettings.LaunchLANDifference));
        }

        /// <summary>Counts down to the next launch to the PSG LAN setting at the set inclination. Returns the launch UT.</summary>
        public double StartToLan()
        {
            AscentSettings.LaunchingToLan = true;
            return Start(TimeToPlane(Core.PSGSimulation.LAN));
        }

        public void Abort() => Active = false;

        /// <summary>
        ///     One tick of an autopilot's prelaunch: hold the vessel until T-10 (when there is a countdown), then get the solar panels
        ///     out of the way. Returns true once prelaunch is finished, and right away if the vessel is already flying.
        /// </summary>
        public bool DrivePrelaunch(AscentStatus status)
        {
            if (Vessel.LiftedOff() && !Vessel.Landed)
            {
                status.Set(Localizer.Format("#MechJeb_Ascent_status4")); //"Vessel is not landed, skipping pre-launch"
                return true;
            }

            Core.Thrust.ThrustOff();
            Core.Attitude.SetAxisControl(false, false, false);

            if (Active && TMinus > 10.0)
            {
                status.Set(Localizer.Format("#MechJeb_Ascent_status1")); //"Pre Launch"
                return false;
            }

            // null when a part's blacklist removed the controller
            if (Core.Solarpanel?.RetractForLaunch() ?? true)
                return true;

            status.Set(Localizer.Format("#MechJeb_Ascent_status5")); //"Retracting solar panels"
            return false;
        }

        /// <summary>Returns true while the countdown is still running after prelaunch, when the autopilot must not fly yet.</summary>
        public bool HoldForLiftoff(AscentStatus status)
        {
            if (!Active)
                return false;

            status.Set(TMinus <= 0 && WaitingForGuidance
                ? "Holding launch for guidance"
                : Localizer.Format("#MechJeb_Ascent_status6")); //"Awaiting liftoff"
            Core.Attitude.SetAxisControl(false, false, false);
            return true;
        }

        public override void OnFixedUpdate()
        {
            if (!Active)
                return;

            if (TMinus < 3 * VesselState.DeltaT || (TMinus > 10.0 && _lastTMinus < 1.0))
            {
                if (!WaitingForGuidance)
                {
                    if (VesselState.ThrustAvailable < 10E-4) // only stage if we have no engines active
                        StageManager.ActivateNextStage();
                    Active = false;
                }
            }
            else
            {
                if (Core.Node.Autowarp)
                    Core.Warp.WarpToUT(_launchTime - AscentSettings.WarpCountDown);
            }

            _lastTMinus = TMinus;
        }

        private double TimeToPlane(double lan) =>
            Astro.TimeToPlane(MainBody.rotationPeriod, VesselState.Latitude, VesselState.CelestialLongitude, lan, InclinationSetting);

        // every launch tag comes through here, so they all drop the old PSG solution the same way
        private double Start(double timeToLaunch)
        {
            if (AscentSettings.AscentType == AscentType.PSG)
                Core.PSGSimulation.FlushSolution();

            double launchTime = VesselState.Time + timeToLaunch;

            if (AscentSettings.OverrideWarpToPlane)
            {
                Active = false;
                _launchTime = VesselState.Time;
                _lastTMinus = 0;
            }
            else
            {
                Active = true;
                _launchTime = launchTime;
                _lastTMinus = 999;
            }

            return launchTime;
        }
    }
}
