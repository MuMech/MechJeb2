extern alias JetBrainsAnnotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    //The target controller provides a nicer interface to access info about the player's currently selected target,
    //and to set the target to something new.
    //
    //It also provides the ability to set position and direction targets. Position targets make the navball
    //target indicator point to a give set of coordinates on a celestial body. The target is also shown
    //in the map view. Direction targets make the navball target indicator point in a certain direction.
    //
    //Finally, it makes the target persistent in the following sense: when you switch away from a vessel, it
    //will keep the same target, independent of whatever targets you set on your new vessel. When you switch
    //back to the original vessel, your old target will be restored.
    //
    //Todo: Make target persistence work even when the original vessel gets unloaded and reloaded.
    public class MechJebModuleTargetController : ComputerModule
    {
        public MechJebModuleTargetController(MechJebCore core) : base(core) { }

        public CelestialBody targetBody;

        // per vessel: a focus change must not hand one vessel's landing site to another
        [Persistent(pass = (int)Pass.LOCAL)]
        public EditableAngle targetLatitude = new EditableAngle(0);

        [Persistent(pass = (int)Pass.LOCAL)]
        public EditableAngle targetLongitude = new EditableAngle(0);

        private Vector3d targetDirection;

        private bool wasActiveVessel;

        public bool pickingPositionTarget;

        ////////////////////////
        // EXTERNAL INTERFACE //
        ////////////////////////

        public void Set(ITargetable t)
        {
            Target = t;
            if (Vessel != null)
            {
                Vessel.targetObject = Target;

                // KSP keeps the active vessel's target in FlightGlobals too, and the navball, the orbit targeter and kOS's TARGET read
                // it there: targetObject alone leaves KSP's old target in place, and KSP dropping that one later clears ours with it.
                // The override, as kOS's own SET TARGET: a focused kOS terminal holds a TARGETING input lock.
                if (Vessel.isActiveVessel && FlightGlobals.fetch != null)
                    FlightGlobals.fetch.SetVesselTarget(t, true);
            }
        }

        public void SetPositionTarget(CelestialBody body, double latitude, double longitude)
        {
            targetBody = body;
            targetLatitude = latitude;
            targetLongitude = longitude;

            // placed now, not at the next FixedUpdate: until then it would sit at the world origin
            var target = new PositionTarget(Coordinates.ToStringDMS(latitude, longitude, true));
            target.Update(body, latitude, longitude);
            Set(target);
        }

        [ValueInfoItem("#MechJeb_Targetcoordinates", InfoItem.Category.Target)] //Target coordinates
        public string GetPositionTargetString()
        {
            if (Target is PositionTarget) return Coordinates.ToStringDMS(targetLatitude, targetLongitude, true);

            if (NormalTargetExists)
                return Coordinates.ToStringDMS(TargetOrbit.referenceBody.GetLatitude(Position), TargetOrbit.referenceBody.GetLongitude(Position),
                    true);

            return "N/A";
        }

        public Vector3d GetPositionTargetPosition() =>
            targetBody.GetWorldSurfacePosition(targetLatitude, targetLongitude, targetBody.TerrainAltitude(targetLatitude, targetLongitude)) -
            targetBody.position;

        public void SetDirectionTarget(string name) => Set(new DirectionTarget(name));

        [ActionInfoItem("#MechJeb_Pickpositiontarget", InfoItem.Category.Target)] //Pick position target
        public void PickPositionTargetOnMap()
        {
            pickingPositionTarget = true;
            MapView.EnterMapView();
            string message =
                Localizer.Format("#MechJeb_pickingPositionMsg",
                    MainBody.displayName.LocalizeRemoveGender()); // "Click to select a target on " +  + "'s surface.\n(Leave map view to cancel.)"
            ScreenMessages.PostScreenMessage(message, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }

        public void StopPickPositionTargetOnMap()
        {
            pickingPositionTarget = false;
            Cursor.visible = true;
        }

        public void Unset() => Set(null);

        public void UpdateDirectionTarget(Vector3d direction) => targetDirection = direction;

        // A destroyed Unity object isn't null to the interface.
        public bool NormalTargetExists => Target != null && !(Target is UnityEngine.Object target && target == null) && !TargetPartGone &&
                                          (Target is Vessel || Target is CelestialBody || CanAlign);

        // A targeted part (a docking port) that left its vessel: EVA construction detaches it with Part.OnDetachFlight, which sets its vessel
        // to null while the part lives on in a kerbal's inventory. Its orbit and position then throw (#1488).
        private bool TargetPartGone => Target is PartModule module && module != null && module.vessel == null;

        public bool PositionTargetExists => Target != null && (Target is PositionTarget || Target is Vessel) && !(Target is DirectionTarget);

        /// <summary>
        /// True when targetLatitude and targetLongitude are a point on `body` to land at: a position target on that body, or the
        /// coordinates taken from a vessel that was landed on it when targeted. A vessel targeted in flight, a direction (a
        /// PositionTarget to KSP), or a target on another body has no such point there.
        /// </summary>
        public bool SurfaceTargetExists(CelestialBody body) =>
            body != null && targetBody == body && (Target is PositionTarget && !(Target is DirectionTarget) ||
                                                   Target is Vessel v && v != null && ReferenceEquals(v, _coordinatesTakenFrom));

        public bool CanAlign => Target.GetTargetingMode() == VesselTargetModes.DirectionVelocityAndOrientation;

        public ITargetable Target { get; private set; }

        public Orbit TargetOrbit
        {
            get
            {
                if (Target == null || TargetPartGone)
                    return null;
                return Target.GetOrbit();
            }
        }

        public Vector3 Position => PositionD;

        // A target on rails hasn't moved yet when FixedUpdate reads it, which puts its transform a tick of Krakensbane's frame velocity out: 78 m in
        // a high orbit. The main body hasn't moved yet either, so only our own vessel is in the new frame: the target is placed and cached from it,
        // with positions relative to the body. Cached per UT, as the rendezvous autopilot reads it several times a tick. It is right next to our
        // vessel only: against the main body, as for a latitude, read Transform, which is out with the body.
        public Vector3d PositionD
        {
            get
            {
                Vessel target = Target?.GetVessel();
                if (target == null || !target.packed)
                    return Transform.position;

                double now = Planetarium.GetUniversalTime();
                if (now != positionUT || Target != positionTarget)
                {
                    positionUT       = now;
                    positionTarget   = Target;
                    positionFromVessel = PackedOffset(target, now);
                }

                return Vessel.CoMD + positionFromVessel;
            }
        }

        private double      positionUT = double.NaN;
        private ITargetable positionTarget;
        private Vector3d    positionFromVessel;

        // On rails KSP puts a landed vessel's root at its saved latitude, longitude and altitude, and any other vessel's CoM on its orbit.
        // The target point's offset from either is the same in the stale frame as in the new one.
        private Vector3d PackedOffset(Vessel target, double ut)
        {
            if (target.LandedOrSplashed && target.mainBody == Vessel.mainBody)
                return target.mainBody.GetWorldSurfacePosition(target.latitude, target.longitude, target.altitude) - target.mainBody.position
                       - Vessel.orbit.getRelativePositionAtUT(ut).xzy
                       + ((Vector3d)Transform.position - (Vector3d)target.vesselTransform.position);

            return target.orbit.getPositionAtUT(ut) - Vessel.orbit.getPositionAtUT(ut)
                   + ((Vector3d)Transform.position - target.GetWorldPos3D());
        }

        public float Distance => (float)Vector3d.Distance(PositionD, Vessel.GetTransform().position);

        public Vector3d RelativeVelocity => Vessel.orbit.GetVel() - TargetOrbit.GetVel();

        public Vector3d RelativePosition => Vessel.GetTransform().position - PositionD;

        public Transform Transform => Target.GetTransform();

        //which way your vessel should be pointing to dock with the target
        public Vector3 DockingAxis
        {
            get
            {
                if (CanAlign) return -Transform.forward;
                return -Transform.up;
            }
        }

        // ModuleDockingNode.GetName reads its vessel's name: a port that left its vessel throws there, which is the NRE of #1488, until
        // the next physics tick drops it
        public string Name => TargetPartGone ? "" : Target.GetName();

        ////////////////////////
        // Internal functions //
        ////////////////////////

        public override void OnStart(PartModule.StartState state)
        {
            Core.AddToPostDrawQueue(DoMapView);

            Users.Add(this); //TargetController should always be running
        }

        public override void OnFixedUpdate()
        {
            SyncWithVesselTarget();

            //Update targets that need updating:
            if (Target is DirectionTarget) ((DirectionTarget)Target).Update(targetDirection);
            else if (Target is PositionTarget) ((PositionTarget)Target).Update(targetBody, targetLatitude, targetLongitude);

            wasActiveVessel = Vessel.isActiveVessel;
        }

        /// <summary>
        /// Take up the stock target (Vessel.targetObject): every physics tick, and also from kOS before it plans, since a script's
        /// SET TARGET changes the stock target at once and this module's FixedUpdate may come after the script in the same tick.
        /// Running it twice in a tick is harmless; wasActiveVessel is left to OnFixedUpdate so the restore below still happens there.
        /// </summary>
        internal void SyncWithVesselTarget()
        {
            // drop a part target that left its vessel, and the stock target with it: KSP's own FlightGlobals.UpdateInformation throws on it too
            if (TargetPartGone)
                Unset();

            // Restore the saved target when we are made active vessel: the switch cleared KSP's target, and KSP brings back only what it
            // saved and can find again, never a position or a direction
            if (!wasActiveVessel && Vessel.isActiveVessel && Target != null && !(Target is UnityEngine.Object restored && restored == null))
                Set(Target);

            //notice when the user switches targets
            if (Target != Vessel.targetObject)
            {
                Target = Vessel.targetObject;
                _coordinatesTakenFrom = null;

                // a landed vessel target gives its coordinates, once: the player can then edit them in Landing Guidance to land beside it
                if (Target is Vessel landed && landed != null && landed.LandedOrSplashed)
                    TakeLandedVesselCoordinates(landed);

                if (Target is CelestialBody)
                {
                    targetBody = (CelestialBody)Target;
                }
            }

            // .23 temp fix until I understand better what's going on
            if (targetBody == null)
                targetBody = Vessel.mainBody;
        }

        // the vessel target that targetLatitude and targetLongitude were taken from
        private Vessel _coordinatesTakenFrom;

        // KSP keeps a landed vessel's latitude and longitude: from its transform while it is loaded, its saved ones while it is packed
        private void TakeLandedVesselCoordinates(Vessel landed)
        {
            _coordinatesTakenFrom = landed;
            targetBody            = landed.mainBody;
            targetLatitude        = landed.latitude;
            targetLongitude       = landed.longitude;
        }

        // While picking a position on the map: the surface point under the pointer, and its coordinates and biome, which
        // MechJeb2.UI shows next to the pointer. Null when the pointer isn't over the body or is over a window.
        private Coordinates _pickCoordinates;
        internal string PickLabel { get; private set; }

        public override void OnUpdate()
        {
            _pickCoordinates = null;
            PickLabel        = null;

            // a pick ends when the map closes, or when another vessel becomes the active one
            if (pickingPositionTarget && (!MapView.MapIsEnabled || (Vessel != null && !Vessel.isActiveVessel)))
                StopPickPositionTargetOnMap();

            if (!pickingPositionTarget || Vessel == null || Vessel.GetMasterMechJeb() != Core)
                return;

            if (!GuiUtils.MouseIsOverWindow(Core))
                _pickCoordinates = GuiUtils.GetMouseCoordinates(MainBody);
            Cursor.visible = _pickCoordinates == null;
            if (_pickCoordinates == null)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                SetPositionTarget(MainBody, _pickCoordinates.Latitude, _pickCoordinates.Longitude);
                StopPickPositionTargetOnMap();
                _pickCoordinates = null;
                return;
            }

            string biome = MainBody.GetExperimentBiomeSafe(_pickCoordinates.Latitude, _pickCoordinates.Longitude);
            PickLabel = _pickCoordinates.ToStringDecimal() + "\n" + biome;
        }

        private void DoMapView()
        {
            // the point under the pointer while picking
            if (_pickCoordinates != null && MapView.MapIsEnabled)
                GLUtils.DrawGroundMarker(MainBody, _pickCoordinates.Latitude, _pickCoordinates.Longitude, new Color(1.0f, 0.56f, 0.0f), true,
                    60);

            DrawMapViewTarget();
        }

        private void DrawMapViewTarget()
        {
            if (HighLogic.LoadedSceneIsEditor) return;
            if (!MapView.MapIsEnabled) return;
            if (!Vessel.isActiveVessel || Vessel.GetMasterMechJeb() != Core) return;

            if (Target == null) return;
            if (!(Target is PositionTarget) && !(Target is Vessel)) return;
            if (Target is Vessel && (!((Vessel)Target).LandedOrSplashed || ((Vessel)Target).mainBody != Vessel.mainBody)) return;
            if (Target is DirectionTarget) return;

            GLUtils.DrawGroundMarker(targetBody, targetLatitude, targetLongitude, Color.red, true);
        }
    }
}
