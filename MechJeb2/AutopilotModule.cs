using System;
using UnityEngine;

namespace MuMech
{
    public class AutopilotModule : ComputerModule
    {
        public AutopilotModule(MechJebCore core) : base(core)
        {
        }

        public override void Drive(FlightCtrlState s)
        {
            if (CurrentStep != null)
            {
                try
                {
                    CurrentStep = CurrentStep.Drive(s);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public override void OnFixedUpdate()
        {
            if (CurrentStep != null)
            {
                try
                {
                    CurrentStep = CurrentStep.OnFixedUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public void setStep(AutopilotStep step)
        {
            CurrentStep = step;
        }

        public string status
        {
            get
            {
                if (CurrentStep == null)
                    return "Off";
                return CurrentStep.status;
            }
        }

        public bool active => CurrentStep != null;

        public AutopilotStep CurrentStep { get; private set; }
    }

    public class AutopilotStep
    {
        public MechJebCore core;

        //conveniences:
        public VesselState   vesselState => core.vesselState;
        public Vessel        vessel      => core.part.vessel;
        public CelestialBody mainBody    => core.part.vessel.mainBody;
        public Orbit         orbit       => core.part.vessel.orbit;

        public AutopilotStep(MechJebCore core)
        {
            this.core = core;
        }

        public virtual AutopilotStep Drive(FlightCtrlState s) { return this; }
        public virtual AutopilotStep OnFixedUpdate()          { return this; }
        public         string        status                   { get; protected set; }
    }
}
