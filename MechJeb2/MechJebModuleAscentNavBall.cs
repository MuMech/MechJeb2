using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public override void OnStart(PartModule.StartState state)
        {
            autopilot = core.GetComputerModule<MechJebModuleAscentAutopilot>();
            NavBallGuidance = false;
        }

        public override void OnFixedUpdate()
        {
            if (NavBallGuidance && autopilot != null && autopilot.ascentPath != null)
            {
                double angle = Math.PI / 180 * autopilot.ascentPath.FlightPathAngle(vesselState.altitudeASL, vesselState.speedSurface);
                double heading = Math.PI / 180 * OrbitalManeuverCalculator.HeadingForInclination(autopilot.desiredInclination, vesselState.latitude);
                Vector3d horizontalDir = Math.Cos(heading) * vesselState.north + Math.Sin(heading) * vesselState.east;
                Vector3d dir = Math.Cos(angle) * horizontalDir + Math.Sin(angle) * vesselState.up;
                core.target.UpdateDirectionTarget(dir);
            }
        }

    }
}
