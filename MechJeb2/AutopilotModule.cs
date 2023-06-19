using System;
using UnityEngine;

namespace MuMech
{
    public class AutopilotModule : ComputerModule
    {
        protected AutopilotModule(MechJebCore core) : base(core)
        {
        }

        public override void Drive(FlightCtrlState s)
        {
            if (CurrentStep == null) return;

            try
            {
                CurrentStep = CurrentStep.Drive(s);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public override void OnFixedUpdate()
        {
            if (CurrentStep == null) return;

            try
            {
                CurrentStep = CurrentStep.OnFixedUpdate();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        protected void SetStep(AutopilotStep step)
        {
            CurrentStep = step;
        }

        public string Status
        {
            get
            {
                return CurrentStep == null ? "Off" : CurrentStep.Status;
            }
        }

        protected bool Active => CurrentStep != null;

        public AutopilotStep CurrentStep { get; private set; }
    }

    public class AutopilotStep
    {
        protected readonly MechJebCore Core;

        //conveniences:
        protected VesselState   VesselState => Core.VesselState;
        protected Vessel        Vessel      => Core.part.vessel;
        protected CelestialBody MainBody    => Core.part.vessel.mainBody;
        protected Orbit         Orbit       => Core.part.vessel.orbit;

        protected AutopilotStep(MechJebCore core)
        {
            this.Core = core;
        }

        public virtual AutopilotStep Drive(FlightCtrlState s) { return this; }
        public virtual AutopilotStep OnFixedUpdate()          { return this; }
        public         string        Status                   { get; protected set; }
    }
}
