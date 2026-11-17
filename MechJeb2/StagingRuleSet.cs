extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;

namespace MuMech
{
    /// <summary>
    ///     The rules the staging controller stages by. The controller's own values (Utilities) apply while no autopilot has given it
    ///     its own set (MechJebModuleStagingController.SetRulesOverride): then the controller reads that set instead, and its own
    ///     values stay as they are.
    /// </summary>
    public interface IStagingRules
    {
        EditableDouble AutostagePreDelay { get; }

        EditableDouble AutostagePostDelay { get; }

        EditableInt AutostageLimit { get; }

        bool FairingMaxDynamicPressureEnabled { get; set; }

        EditableDoubleMult FairingMaxDynamicPressure { get; }

        bool FairingMinAltitudeEnabled { get; set; }

        EditableDoubleMult FairingMinAltitude { get; }

        /// <summary>Fraction of the available thrust before the launch clamps are released (0.99 == 99 %).</summary>
        EditableDoubleMult ClampAutoStageThrustPct { get; }

        bool FairingMaxAerothermalFluxEnabled { get; set; }

        EditableDoubleMult FairingMaxAerothermalFlux { get; }

        bool HotStaging { get; set; }

        EditableDouble HotStagingLeadTime { get; }

        bool DropSolids { get; set; }

        /// <summary>Fraction of the whole rocket's TWR a booster stack must drop to before the solids go (0.50 == 50 %).</summary>
        EditableDoubleMult DropSolidsTwrPct { get; }
    }

    public static class StagingRules
    {
        /// <summary>Copies every rule of <paramref name="from" /> into <paramref name="to" />.</summary>
        public static void Copy(IStagingRules from, IStagingRules to)
        {
            to.AutostagePreDelay.Val                = from.AutostagePreDelay.Val;
            to.AutostagePostDelay.Val               = from.AutostagePostDelay.Val;
            to.AutostageLimit.Val                   = from.AutostageLimit.Val;
            to.FairingMaxDynamicPressureEnabled     = from.FairingMaxDynamicPressureEnabled;
            to.FairingMaxDynamicPressure.Val        = from.FairingMaxDynamicPressure.Val;
            to.FairingMinAltitudeEnabled            = from.FairingMinAltitudeEnabled;
            to.FairingMinAltitude.Val               = from.FairingMinAltitude.Val;
            to.ClampAutoStageThrustPct.Val          = from.ClampAutoStageThrustPct.Val;
            to.FairingMaxAerothermalFluxEnabled     = from.FairingMaxAerothermalFluxEnabled;
            to.FairingMaxAerothermalFlux.Val        = from.FairingMaxAerothermalFlux.Val;
            to.HotStaging                           = from.HotStaging;
            to.HotStagingLeadTime.Val               = from.HotStagingLeadTime.Val;
            to.DropSolids                           = from.DropSolids;
            to.DropSolidsTwrPct.Val                 = from.DropSolidsTwrPct.Val;
        }

        /// <summary>Keeps ClampAutoStageThrustPct in [0, 1] (done after each edit, by both UIs).</summary>
        public static void ClampThrustPct(IStagingRules rules)
        {
            double clamped = UtilMath.Clamp(rules.ClampAutoStageThrustPct, 0, 1);
            if (clamped != rules.ClampAutoStageThrustPct)
                rules.ClampAutoStageThrustPct.Val = clamped;
        }
    }

    /// <summary>A module's own staging rules, saved with the module's settings: the defaults are the staging controller's.</summary>
    public class StagingRuleSet : IStagingRules
    {
        [UsedImplicitly, Persistent]
        public readonly EditableDouble AutostagePreDelay = 0.0;

        [UsedImplicitly, Persistent]
        public readonly EditableDouble AutostagePostDelay = PhysicsGlobals.StagingCooldownTimer;

        [UsedImplicitly, Persistent]
        public readonly EditableInt AutostageLimit = 0;

        [UsedImplicitly, Persistent]
        public bool FairingMaxDynamicPressureEnabled = true;

        [UsedImplicitly, Persistent]
        public readonly EditableDoubleMult FairingMaxDynamicPressure = new EditableDoubleMult(5000, 1000);

        [UsedImplicitly, Persistent]
        public bool FairingMinAltitudeEnabled = true;

        [UsedImplicitly, Persistent]
        public readonly EditableDoubleMult FairingMinAltitude = new EditableDoubleMult(50000, 1000);

        [UsedImplicitly, Persistent]
        public readonly EditableDoubleMult ClampAutoStageThrustPct = new EditableDoubleMult(0.99, 0.01);

        [UsedImplicitly, Persistent]
        public bool FairingMaxAerothermalFluxEnabled = true;

        [UsedImplicitly, Persistent]
        public readonly EditableDoubleMult FairingMaxAerothermalFlux = new EditableDoubleMult(1135);

        [UsedImplicitly, Persistent]
        public bool HotStaging;

        [UsedImplicitly, Persistent]
        public readonly EditableDouble HotStagingLeadTime = 2.0;

        [UsedImplicitly, Persistent]
        public bool DropSolids;

        [UsedImplicitly, Persistent]
        public readonly EditableDoubleMult DropSolidsTwrPct = new EditableDoubleMult(0.50, 0.01);

        EditableDouble IStagingRules.AutostagePreDelay => AutostagePreDelay;
        EditableDouble IStagingRules.AutostagePostDelay => AutostagePostDelay;
        EditableInt IStagingRules.AutostageLimit => AutostageLimit;
        bool IStagingRules.FairingMaxDynamicPressureEnabled { get => FairingMaxDynamicPressureEnabled; set => FairingMaxDynamicPressureEnabled = value; }
        EditableDoubleMult IStagingRules.FairingMaxDynamicPressure => FairingMaxDynamicPressure;
        bool IStagingRules.FairingMinAltitudeEnabled { get => FairingMinAltitudeEnabled; set => FairingMinAltitudeEnabled = value; }
        EditableDoubleMult IStagingRules.FairingMinAltitude => FairingMinAltitude;
        EditableDoubleMult IStagingRules.ClampAutoStageThrustPct => ClampAutoStageThrustPct;
        bool IStagingRules.FairingMaxAerothermalFluxEnabled { get => FairingMaxAerothermalFluxEnabled; set => FairingMaxAerothermalFluxEnabled = value; }
        EditableDoubleMult IStagingRules.FairingMaxAerothermalFlux => FairingMaxAerothermalFlux;
        bool IStagingRules.HotStaging { get => HotStaging; set => HotStaging = value; }
        EditableDouble IStagingRules.HotStagingLeadTime => HotStagingLeadTime;
        bool IStagingRules.DropSolids { get => DropSolids; set => DropSolids = value; }
        EditableDoubleMult IStagingRules.DropSolidsTwrPct => DropSolidsTwrPct;
    }
}
