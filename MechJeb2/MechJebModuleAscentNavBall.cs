using System;

namespace MuMech
{
    class MechJebModuleAscentNavBall : ComputerModule
    {
        public MechJebModuleAscentNavBall(MechJebCore core) : base(core)
        {
            enabled = true;
        }
        
        public bool NavBallGuidance
        {
            get
            {
                return core.target.Target != null && core.target.Name == TARGET_NAME;
            }
            set
            {
                if (value)
                {
                    core.target.SetDirectionTarget(TARGET_NAME);
                }
                else if (core.target.Target != null && core.target.Name == TARGET_NAME)
                {
                    core.target.Unset();
                }
            }
        }

        private MechJebModuleAscentAutopilot autopilot;
        private const string TARGET_NAME = "Ascent Path Guidance";

        private double launchLatitude = 0;

        public override void OnStart(PartModule.StartState state)
        {
            autopilot = core.GetComputerModule<MechJebModuleAscentAutopilot>();
            NavBallGuidance = false;
            launchLatitude = vesselState.latitude;
        }

        public override void OnFixedUpdate()
        {
            if (NavBallGuidance && autopilot != null && autopilot.ascentPath != null)
            {
                double angle = UtilMath.Deg2Rad * autopilot.ascentPath.FlightPathAngle(vesselState.altitudeASL, vesselState.speedSurface);
                double heading = UtilMath.Deg2Rad * OrbitalManeuverCalculator.HeadingForLaunchInclination(vessel, vesselState, autopilot.desiredInclination);
                Vector3d horizontalDir = Math.Cos(heading) * vesselState.north + Math.Sin(heading) * vesselState.east;
                Vector3d dir = Math.Cos(angle) * horizontalDir + Math.Sin(angle) * vesselState.up;
                core.target.UpdateDirectionTarget(dir);
            }
        }

    }
}
