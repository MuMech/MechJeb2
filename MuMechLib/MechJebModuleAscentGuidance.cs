using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MuMech
{
    //When enabled, the ascent guidance module makes the purple navball target point
    //along the ascent path. The ascent path can be set via SetPath. The ascent guidance
    //module disables itself if the player selects a different target.
    class MechJebModuleAscentGuidance : ComputerModule
    {
        public MechJebModuleAscentGuidance(MechJebCore core) : base(core) { }


        public IAscentPath ascentPath;
        DirectionTarget target = new DirectionTarget("Ascent Path Guidance");


        public void SetPath(IAscentPath ascentPath)
        {
            this.ascentPath = ascentPath;
        }

        public override void OnModuleEnabled()
        {
            FlightGlobals.fetch.SetVesselTarget(target);
        }

        public override void OnModuleDisabled()
        {
            if (FlightGlobals.fetch.VesselTarget == target) FlightGlobals.fetch.SetVesselTarget(null);
        }

        public override void OnFixedUpdate()
        {
            if (!enabled) return;

            if (FlightGlobals.fetch.VesselTarget != target)
            {
                enabled = false;
                return;
            }

            if (ascentPath == null) return;

            double angle = Math.PI / 180 * ascentPath.FlightPathAngle(vesselState.altitudeASL);
            Vector3d dir = Math.Cos(angle) * vesselState.east + Math.Sin(angle) * vesselState.up;
            target.Update(dir);
        }
    }
}
