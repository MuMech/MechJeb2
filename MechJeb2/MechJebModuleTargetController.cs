using JetBrains.Annotations;
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
    [UsedImplicitly]
    public class MechJebModuleTargetController : ComputerModule
    {
        public MechJebModuleTargetController(MechJebCore core) : base(core) { }

        public CelestialBody targetBody;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableAngle targetLatitude = new EditableAngle(0);

        [Persistent(pass = (int)Pass.GLOBAL)]
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
            }
        }

        public void SetPositionTarget(CelestialBody body, double latitude, double longitude)
        {
            targetBody      = body;
            targetLatitude  = latitude;
            targetLongitude = longitude;

            Set(new PositionTarget(string.Format(GetPositionTargetString(), latitude, longitude)));
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

        public Vector3d GetPositionTargetPosition()
        {
            return targetBody.GetWorldSurfacePosition(targetLatitude, targetLongitude, targetBody.TerrainAltitude(targetLatitude, targetLongitude)) -
                   targetBody.position;
        }

        public void SetDirectionTarget(string name)
        {
            Set(new DirectionTarget(name));
        }

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
            Cursor.visible        = true;
        }

        public void Unset()
        {
            Set(null);
        }

        public void UpdateDirectionTarget(Vector3d direction)
        {
            targetDirection = direction;
        }

        public bool NormalTargetExists => Target != null && (Target is Vessel || Target is CelestialBody || CanAlign);

        public bool PositionTargetExists => Target != null && (Target is PositionTarget || Target is Vessel) && !(Target is DirectionTarget);

        public bool CanAlign => Target.GetTargetingMode() == VesselTargetModes.DirectionVelocityAndOrientation;

        public ITargetable Target { get; private set; }

        public Orbit TargetOrbit
        {
            get
            {
                if (Target == null)
                    return null;
                return Target.GetOrbit();
            }
        }

        public Vector3 Position => Transform.position;

        public float Distance => Vector3.Distance(Position, Vessel.GetTransform().position);

        public Vector3d RelativeVelocity => Vessel.orbit.GetVel() - TargetOrbit.GetVel();

        public Vector3d RelativePosition => Vessel.GetTransform().position - Position;

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

        public string Name => Target.GetName();

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
            //Restore the saved target when we are made active vessel
            if (!wasActiveVessel && Vessel.isActiveVessel)
            {
                if (Target != null && Target.GetVessel() != null)
                {
                    Vessel.targetObject = Target;
                }
            }

            //notice when the user switches targets
            if (Target != Vessel.targetObject)
            {
                Target = Vessel.targetObject;
                if (Target is Vessel && ((Vessel)Target).LandedOrSplashed && ((Vessel)Target).mainBody == Vessel.mainBody)
                {
                    targetBody      = Vessel.mainBody;
                    targetLatitude  = Vessel.mainBody.GetLatitude(Target.GetTransform().position);
                    targetLongitude = Vessel.mainBody.GetLongitude(Target.GetTransform().position);
                }

                if (Target is CelestialBody)
                {
                    targetBody = (CelestialBody)Target;
                }
            }

            // .23 temp fix until I understand better what's going on
            if (targetBody == null)
                targetBody = Vessel.mainBody;

            //Update targets that need updating:
            if (Target is DirectionTarget) ((DirectionTarget)Target).Update(targetDirection);
            else if (Target is PositionTarget) ((PositionTarget)Target).Update(targetBody, targetLatitude, targetLongitude);

            wasActiveVessel = Vessel.isActiveVessel;
        }

        public override void OnUpdate()
        {
            if (MapView.MapIsEnabled && pickingPositionTarget)
            {
                if (!GuiUtils.MouseIsOverWindow(Core) && GuiUtils.GetMouseCoordinates(MainBody) != null)
                    Cursor.visible = false;
                else
                    Cursor.visible = true;
            }
        }

        private void DoMapView()
        {
            DoCoordinatePicking();

            DrawMapViewTarget();
        }

        private void DoCoordinatePicking()
        {
            if (pickingPositionTarget && !MapView.MapIsEnabled)
                StopPickPositionTargetOnMap(); //stop picking on leaving map view

            if (!pickingPositionTarget)
                return;

            if (MapView.MapIsEnabled && Vessel.isActiveVessel)
            {
                if (!GuiUtils.MouseIsOverWindow(Core))
                {
                    Coordinates mouseCoords = GuiUtils.GetMouseCoordinates(MainBody);

                    if (mouseCoords != null)
                    {
                        GLUtils.DrawGroundMarker(MainBody, mouseCoords.latitude, mouseCoords.longitude, new Color(1.0f, 0.56f, 0.0f), true, 60);

                        string biome = MainBody.GetExperimentBiomeSafe(mouseCoords.latitude, mouseCoords.longitude);
                        GUI.Label(new Rect(Input.mousePosition.x + 15, Screen.height - Input.mousePosition.y, 200, 50),
                            mouseCoords.ToStringDecimal() + "\n" + biome);

                        if (Input.GetMouseButtonDown(0))
                        {
                            SetPositionTarget(MainBody, mouseCoords.latitude, mouseCoords.longitude);
                            StopPickPositionTargetOnMap();
                        }
                    }
                }
            }
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
