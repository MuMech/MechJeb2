using System;

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
        public EditableDoubleMult turnStartAltitude = new EditableDoubleMult(500, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult turnStartVelocity = new EditableDoubleMult(50);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult turnStartPitch = new EditableDoubleMult(25);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult intermediateAltitude = new EditableDoubleMult(45000, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult holdAPTime = new EditableDoubleMult(1);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public int ascentTypeInteger;
        
        public ascentType ascentType
        {
            get => (ascentType)ascentTypeInteger;
            set
            {
                ascentTypeInteger = (int)value;
                DisableAscentModules();
            }
        }

        public MechJebModuleAscentBaseAutopilot AscentAutopilot => GetAscentModule(ascentType);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult desiredOrbitAltitude = new EditableDoubleMult(100000, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble desiredInclination = new EditableDouble(0.0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble desiredLAN = new EditableDouble(0.0);

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
        public EditableDouble rollAltitude = new EditableDouble(50);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool _autostage = true;

        public bool autostage
        {
            get => _autostage;
            set
            {
                bool changed = value != _autostage;
                _autostage = value;
                if (changed)
                {
                    if (_autostage && enabled) core.staging.users.Add(this);
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

        public EditableDouble launchLANDifference = 0;

        [Persistent(pass = (int)Pass.Global)] public EditableInt warpCountDown = 11;

        /*
         * "Classic" ascent path settings
         */
        
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult turnEndAltitude = new EditableDoubleMult(60000, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble turnEndAngle = 0;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult turnShapeExponent = new EditableDoubleMult(0.4, 0.01);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool autoPath = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public float autoTurnPerc = 0.05f;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public float autoTurnSpdFactor = 18.5f;
        
        public double autoTurnStartAltitude
        {
            get
            {
                return (vessel.mainBody.atmosphere ? vessel.mainBody.RealMaxAtmosphereAltitude() * autoTurnPerc : vessel.terrainAltitude + 25);
            }
        }

        public double autoTurnStartVelocity
        {
            get
            {
                return vessel.mainBody.atmosphere ? autoTurnSpdFactor * autoTurnSpdFactor * autoTurnSpdFactor * 0.015625f : double.PositiveInfinity;
            }
        }

        public double autoTurnEndAltitude
        {
            get
            {
                if (vessel.mainBody.atmosphere)
                {
                    return Math.Min(vessel.mainBody.RealMaxAtmosphereAltitude() * 0.85, desiredOrbitAltitude);
                }
                else
                {
                    return Math.Min(30000, desiredOrbitAltitude * 0.85);
                }
            }
        }
        
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
                if (type != ascentType)
                    GetAscentModule(type).enabled = false;
        }

        public override void OnFixedUpdate()
        {
            DisableAscentModules();
        }
    }
}
