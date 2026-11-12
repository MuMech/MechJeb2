/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:TARGET - MechJeb's target (MechJebModuleTargetController), the one its autopilots and windows use, which on the
    // active vessel is KSP's target too.
    //
    // A position target is a point on a body's surface, as the Landing Guidance window picks it; the landing autopilot lands there.
    // kOS's own TARGET doesn't know these (HASTARGET is true, but reading TARGET throws), so read them here. SELECT also works on a
    // vessel that isn't the active one, where kOS's SET TARGET throws.
    //
    //   SET t TO ADDONS:MECHJEB:TARGET.
    //   t:SELECT(LATLNG(-0.0972, -74.5577)).   // a position target
    //   PRINT t:TYPE.                          // POSITION
    //   PRINT t:GEOPOSITION.
    //   t:SELECT(VESSEL("Station")).
    //   t:CLEAR().
    [KOSNomenclature("MechJebTarget")]
    public class TargetBinding : ComputerModuleBinding<MechJebModuleTargetController>
    {
        private readonly SharedObjects _shared;

        public TargetBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core)
        {
            _shared = shared;
        }

        protected override void InitializeSuffixes()
        {
            // --- commands ---
            AddSuffix("SELECT", new OneArgsSuffix<Structure>(Select,
                "Target a position (a GeoCoordinates: LATLNG(...), or BODY:GEOPOSITIONLATLNG(...) on another body), or a Vessel, a Body " +
                "or a docking port (a Part)."));
            AddSuffix("CLEAR", new NoArgsVoidSuffix(() => Module.Unset(),
                "Clear the target."));
            AddSuffix("PICKPOSITION", new NoArgsVoidSuffix(PickPosition,
                "Open the map view for the player to click a position target on this vessel's body (the active vessel only). See PICKING."));

            // --- state ---
            AddSuffix("HASTARGET", new Suffix<BooleanValue>(() => TypeOf(Synced.Target) != NONE,
                "True while there is a target."));
            AddSuffix("TYPE", new Suffix<StringValue>(() => TypeOf(Synced.Target),
                "What is targeted: NONE, VESSEL, BODY, DOCKINGPORT, PART, POSITION, DIRECTION or OTHER."));
            AddSuffix("NAME", new Suffix<StringValue>(() =>
                {
                    MechJebModuleTargetController controller = Synced;
                    return TypeOf(controller.Target) == NONE ? "" : controller.Name;
                },
                "The target's name (\"\" for none)."));
            AddSuffix("GEOPOSITION", new Suffix<GeoCoordinates>(GeoPosition,
                "The surface coordinates the landing autopilot and the rover use: a position target's, or those of a vessel landed on " +
                "this vessel's body when it was targeted. Kept when the target changes."));
            AddSuffix("POSITION", new Suffix<Vector>(Position,
                "The target's position from this vessel's center of mass (as kOS's POSITION), as MechJeb sees it: corrected for a " +
                "target on rails."));
            AddSuffix("PICKING", new Suffix<BooleanValue>(() => Module.pickingPositionTarget,
                "True while PICKPOSITION waits for the player's click (leaving the map view cancels it)."));
        }

        private const string NONE = "NONE";

        // MechJeb takes up a SET TARGET at its next physics tick, so a read in the same tick takes it up first
        private MechJebModuleTargetController Synced
        {
            get
            {
                MechJebModuleTargetController controller = Module;
                controller.SyncWithVesselTarget();
                return controller;
            }
        }

        private static string TypeOf(ITargetable? target)
        {
            switch (target)
            {
                case null:
                    return NONE;
                case UnityEngine.Object o when o == null: // destroyed
                    return NONE;
                case Vessel _:
                    return "VESSEL";
                case CelestialBody _:
                    return "BODY";
                case ModuleDockingNode _:
                    return "DOCKINGPORT";
                case PartModule _:
                    return "PART";
                case DirectionTarget _: // before PositionTarget, its base class
                    return "DIRECTION";
                case PositionTarget _:
                    return "POSITION";
                default:
                    return "OTHER";
            }
        }

        private void Select(Structure target)
        {
            MechJebModuleTargetController controller = Module;
            Vessel vessel = Core.vessel;

            switch (target)
            {
                case GeoCoordinates geo:
                    double latitude = KosArgs.Finite(geo.Latitude, "SELECT latitude");
                    double longitude = KosArgs.Finite(geo.Longitude, "SELECT longitude");
                    if (geo.Body == null)
                        throw new KOSException("SELECT needs a GeoCoordinates on a body.");
                    CheckKspKeeps(null, vessel);
                    controller.SetPositionTarget(geo.Body, latitude, longitude);
                    return;

                case IKOSTargetable targetable:
                    ITargetable? t = targetable.Target;
                    if (t == null)
                        throw new KOSException($"{target} can't be targeted.");
                    if (t is UnityEngine.Object o && o == null)
                        throw new KOSException($"{target} no longer exists.");
                    // as kOS's SET TARGET
                    if (t.GetVessel() == vessel)
                        throw new KOSException(t is Vessel ? "A ship cannot set TARGET to itself." : "A ship cannot set TARGET to a part of itself.");
                    CheckKspKeeps(t.GetOrbitDriver(), vessel);
                    controller.Set(t);
                    return;

                default:
                    throw new KOSException($"SELECT takes a GeoCoordinates, a Vessel, a Body or a Part, not a {target.KOSName}.");
            }
        }

        /// <summary>
        /// A KOSException for a target KSP would drop on its next tick, rather than have it vanish then. For the active vessel
        /// (FlightGlobals.UpdateInformation) KSP drops the body the vessel orbits, and any body that one orbits. It compares orbit
        /// drivers, and the Sun has none, so in orbit of the Sun it also drops a target with no orbit, such as a position target.
        /// </summary>
        private static void CheckKspKeeps(OrbitDriver? driver, Vessel vessel)
        {
            if (!vessel.isActiveVessel)
                return;

            CelestialBody mainBody = vessel.mainBody;
            if (driver == mainBody.GetOrbitDriver())
                throw new KOSException(driver == null
                    ? $"KSP drops a target that has no orbit, such as a position target, while the vessel orbits {mainBody.bodyName}."
                    : $"KSP doesn't keep the body the vessel orbits ({mainBody.bodyName}) as its target.");
            if (driver != null && driver.celestialBody != null && mainBody.HasParent(driver.celestialBody))
                throw new KOSException(
                    $"KSP doesn't keep {driver.celestialBody.bodyName} as the target while the vessel orbits {mainBody.bodyName}, which orbits it.");
        }

        private GeoCoordinates GeoPosition()
        {
            MechJebModuleTargetController controller = Synced;
            // the sync sets targetBody when there was none
            return new GeoCoordinates(controller.targetBody, _shared, controller.targetLatitude, controller.targetLongitude);
        }

        private Vector Position()
        {
            MechJebModuleTargetController controller = Synced;
            if (TypeOf(controller.Target) == NONE)
                throw new KOSException("There is no target.");
            // kOS positions are from its vessel's center of mass
            return new Vector(controller.PositionD - _shared.Vessel.CoMD);
        }

        private void PickPosition()
        {
            MechJebModuleTargetController controller = Module;
            if (!Core.vessel.isActiveVessel)
                throw new KOSSituationallyInvalidException("PICKPOSITION needs the active vessel: the map view only picks for it.");
            controller.PickPositionTargetOnMap();
        }
    }
}
