using System;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public enum ascentType { CLASSIC, GRAVITYTURN, PVG }

    public class MechJebModuleAscentSettings : ComputerModule
    {
        public MechJebModuleAscentSettings(MechJebCore core) : base(core)
        {
        }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble PitchStartVelocity = new EditableDouble(50);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble PitchRate = new EditableDouble(0.50);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DesiredApoapsis = new EditableDoubleMult(0, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DesiredAttachAlt = new EditableDoubleMult(0, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DynamicPressureTrigger = new EditableDoubleMult(10000, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableInt StagingTrigger = new EditableInt(1);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool AttachAltFlag = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool StagingTriggerFlag = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult TurnStartAltitude = new EditableDoubleMult(500, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult TurnStartVelocity = new EditableDoubleMult(50);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult TurnStartPitch = new EditableDoubleMult(25);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult IntermediateAltitude = new EditableDoubleMult(45000, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult HoldAPTime = new EditableDoubleMult(1);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private int _ascentTypeInteger;
        
        public ascentType AscentType
        {
            get => (ascentType)_ascentTypeInteger;
            set
            {
                _ascentTypeInteger = (int)value;
                DisableAscentModules();
            }
        }

        public MechJebModuleAscentBaseAutopilot AscentAutopilot => GetAscentModule(AscentType);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DesiredOrbitAltitude = new EditableDoubleMult(100000, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble DesiredInclination = new EditableDouble(0.0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble DesiredLan = new EditableDouble(0.0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool CorrectiveSteering = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble CorrectiveSteeringGain = 0.6; //control gain

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool ForceRoll = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble VerticalRoll = new EditableDouble(0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble TurnRoll = new EditableDouble(0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool AutodeploySolarPanels = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool AutoDeployAntennas = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool SkipCircularization = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble RollAltitude = new EditableDouble(50);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private bool _autostage = true;

        public bool Autostage
        {
            get => _autostage;
            set
            {
                bool changed = value != _autostage;
                _autostage = value;
                if (!changed) return;
                if (_autostage && enabled)
                {
                    core.staging.users.Add(this);
                }
                else if (!_autostage)
                {
                    core.staging.users.Remove(this);
                }
            }
        }

        /* classic AoA limter */
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool LimitAoA = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble MaxAoA = 5;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult AOALimitFadeoutPressure = new EditableDoubleMult(2500);

        [Persistent(pass = (int)(Pass.Type))]
        public bool LimitingAoA = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble LimitQa = new EditableDouble(2000);

        [Persistent(pass = (int)(Pass.Type))]
        public bool LimitQaEnabled = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble LaunchPhaseAngle = 0;

        [Persistent(pass = (int)(Pass.Type))]
        public readonly EditableDouble LaunchLANDifference = 0;

        [Persistent(pass = (int)Pass.Global)] 
        public readonly EditableInt WarpCountDown = 11;

        /*
         * "Classic" ascent path settings
         */
        
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult TurnEndAltitude = new EditableDoubleMult(60000, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble TurnEndAngle = 0;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult TurnShapeExponent = new EditableDoubleMult(0.4, 0.01);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool AutoPath = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public float AutoTurnPerc = 0.05f;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public float AutoTurnSpdFactor = 18.5f;
        
        public double AutoTurnStartAltitude
        {
            get
            {
                return (vessel.mainBody.atmosphere ? vessel.mainBody.RealMaxAtmosphereAltitude() * AutoTurnPerc : vessel.terrainAltitude + 25);
            }
        }

        public double AutoTurnStartVelocity
        {
            get
            {
                return vessel.mainBody.atmosphere ? AutoTurnSpdFactor * AutoTurnSpdFactor * AutoTurnSpdFactor * 0.015625f : double.PositiveInfinity;
            }
        }

        public double AutoTurnEndAltitude
        {
            get
            {
                return vessel.mainBody.atmosphere ? 
                    Math.Min(vessel.mainBody.RealMaxAtmosphereAltitude() * 0.85, DesiredOrbitAltitude) : 
                    Math.Min(30000, DesiredOrbitAltitude * 0.85);
            }
        }
        
        /*
         * PVG Staging values
         */

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble MinDeltaV = 40;

        [Persistent(pass = (int)Pass.Type)] public bool ExtendIfRequired = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble MaxCoast = 450;

        [Persistent(pass = (int)Pass.Type)] public bool FixedCoast = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble FixedCoastLength = 450;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble PreStageTime = 10;
        
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble OptimizerPauseTime = 5;

        [Persistent(pass = (int)(Pass.Type))]
        public bool FixedBurntime = false;

        [Persistent(pass = (int)(Pass.Type))]
        public readonly EditableInt LastStage = 0;
        
        [Persistent(pass = (int)(Pass.Type))]
        public readonly EditableInt CoastStage = -1;
        
        [Persistent(pass = (int)(Pass.Type))]
        public readonly EditableInt OptimizeStage = 0;
        
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool CoastDuring = false;
        
        [Persistent(pass = (int)(Pass.Type))]
        public readonly EditableInt SpinupStage = -1;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult SpinupAngularVelocity = new EditableDoubleMult(0.5, TAU/60.0);

        /*
         * some non-persisted values
         */

        public bool LaunchingToPlane;
        public bool LaunchingToRendezvous;
        public bool LaunchingToMatchLan;
        public bool LaunchingToLan;
        
        /*
         * Helpers for dealing with switching between modules
         */
        
        private readonly ascentType[] _values = (ascentType[])Enum.GetValues(typeof(ascentType));

        private MechJebModuleAscentBaseAutopilot GetAscentModule(ascentType type)
        {
            switch (type)
            {
                case ascentType.CLASSIC:
                    return core.GetComputerModule<MechJebModuleAscentClassicAutopilot>();
                case ascentType.GRAVITYTURN:
                    return core.GetComputerModule<MechJebModuleAscentGTAutopilot>();
                case ascentType.PVG:
                    return core.GetComputerModule<MechJebModuleAscentPVGAutopilot>();
                default:
                    return core.GetComputerModule<MechJebModuleAscentClassicAutopilot>();
            }
        }
        
        private void DisableAscentModules()
        {
            foreach (ascentType type in _values)
                if (type != AscentType)
                    GetAscentModule(type).enabled = false;
        }

        public override void OnFixedUpdate()
        {
            DisableAscentModules();
        }
    }
}
