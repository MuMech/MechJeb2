using System;
using UnityEngine;

namespace MuMech
{
    public class AutopilotModule : ComputerModule
    {
        AutopilotStep current_step = null;

        public AutopilotModule(MechJebCore core) : base(core)
        {
        }

        public override void Drive(FlightCtrlState s)
        {
            if (current_step != null)
            {
                try
                {
                    current_step = current_step.Drive(s);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public override void OnFixedUpdate()
        {
            if (current_step != null)
            {
                try
                {
                    current_step = current_step.OnFixedUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public void setStep(AutopilotStep step)
        {
            current_step = step;
        }

        public string status
        {
            get
            {
                if (current_step == null)
                    return "Off";
                else
                    return current_step.status;
            }
        }

        public bool active { get { return current_step != null; } }
    }

    public class AutopilotStep
    {
        public MechJebCore core = null;

        //conveniences:
        public VesselState vesselState { get { return core.vesselState; } }
        public Vessel vessel { get { return core.part.vessel; } }
        public CelestialBody mainBody { get { return core.part.vessel.mainBody; } }
        public Orbit orbit { get { return core.part.vessel.orbit; } }

        public AutopilotStep(MechJebCore core)
        {
            this.core = core;
        }

        public virtual AutopilotStep Drive(FlightCtrlState s) { return this; }
        public virtual AutopilotStep OnFixedUpdate() { return this; }
        public string status { get; protected set; }
    }
}
