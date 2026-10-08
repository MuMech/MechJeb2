/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.FuelFlowSimulation;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    /// <summary>
    ///     KSP resource definitions, with the ids and densities taken from SimVessel dumps captured in KSP.
    /// </summary>
    public static class ResourceFixtures
    {
        public const int LIQUID_FUEL     = -1483389306;
        public const int OXIDIZER        = -1154601244;
        public const int ELECTRIC_CHARGE = 1566956177;

        public const float LIQUID_FUEL_DENSITY     = 0.005f;
        public const float OXIDIZER_DENSITY        = 0.005f;
        public const float ELECTRIC_CHARGE_DENSITY = 0;

        // RP-1 (RealismOverhaul and RealFuels) resources
        public const int COOLED_RP1        = 1704334082;
        public const int COOLED_LQD_OXYGEN = 321082453;
        public const int NITROGEN          = 254642152;
        public const int LEAD_BALLAST      = 42120693;
        public const int TEATEB            = 118851235;
        public const int CLAMP_PUMP        = -1519161728;
        public const int AIR_PUMP          = 852002701;

        public const double COOLED_RP1_DENSITY        = 0.00082700001075863838;
        public const double COOLED_LQD_OXYGEN_DENSITY = 0.0012359999818727374;
        public const double NITROGEN_DENSITY          = 1.2509999578469433E-06;
        public const double LEAD_BALLAST_DENSITY      = 0.011339999735355377;
        public const double TEATEB_DENSITY            = 0.00070030998904258013;

        // this mirrors SimVesselUpdater.UpdateResources for a resource with its flowState enabled
        public static void AddResource(SimPart part, int id, double amount, double maxAmount, double density) =>
            part.Resources[id] = new SimResource
            {
                Amount    = amount,
                MaxAmount = maxAmount,
                Id        = id,
                Free      = density == 0,
                Density   = density,
                Residual  = 0
            };

        public static void AddLiquidFuel(SimPart part, double amount, double maxAmount) =>
            AddResource(part, LIQUID_FUEL, amount, maxAmount, LIQUID_FUEL_DENSITY);

        public static void AddOxidizer(SimPart part, double amount, double maxAmount) =>
            AddResource(part, OXIDIZER, amount, maxAmount, OXIDIZER_DENSITY);

        public static void AddElectricCharge(SimPart part, double amount, double maxAmount) =>
            AddResource(part, ELECTRIC_CHARGE, amount, maxAmount, ELECTRIC_CHARGE_DENSITY);
    }
}
