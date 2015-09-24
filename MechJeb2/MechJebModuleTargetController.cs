using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        ITargetable target;

        public CelestialBody targetBody;
        [Persistent(pass = (int)Pass.Global)]
        public EditableAngle targetLatitude = new EditableAngle(0);
        [Persistent(pass = (int)Pass.Global)]
        public EditableAngle targetLongitude = new EditableAngle(0);

        Vector3d targetDirection;

        bool wasActiveVessel = false;

        public bool pickingPositionTarget = false;

        ////////////////////////
        // EXTERNAL INTERFACE //
        ////////////////////////

        public void Set(ITargetable t)
        {
            target = t;
            if (vessel != null && vessel.isActiveVessel)
            {
                if (FlightGlobals.fetch != null) FlightGlobals.fetch.SetVesselTarget(target);
            }
        }

        public void SetPositionTarget(CelestialBody body, double latitude, double longitude)
        {
            targetBody = body;
            targetLatitude = latitude;
            targetLongitude = longitude;

            Set(new PositionTarget(String.Format(GetPositionTargetString(), latitude, longitude)));
        }

        [ValueInfoItem("Target coordinates", InfoItem.Category.Target)]
        public string GetPositionTargetString()
        {
            if (target is PositionTarget) return Coordinates.ToStringDMS(targetLatitude, targetLongitude, true);

            if (NormalTargetExists) return Coordinates.ToStringDMS(TargetOrbit.referenceBody.GetLatitude(Position), TargetOrbit.referenceBody.GetLongitude(Position), true);

            return "N/A";
        }

        public Vector3d GetPositionTargetPosition()
        {
            return targetBody.GetWorldSurfacePosition(targetLatitude, targetLongitude, targetBody.TerrainAltitude(targetLatitude, targetLongitude));
        }

        public void SetDirectionTarget(string name)
        {
            Set(new DirectionTarget(name));
        }

        [ActionInfoItem("Pick position target", InfoItem.Category.Target)]
        public void PickPositionTargetOnMap()
        {
            pickingPositionTarget = true;
            MapView.EnterMapView();
            string message = "Click to select a target on " + mainBody.theName + "'s surface.\n(Leave map view to cancel.)";
            ScreenMessages.PostScreenMessage(message, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }

        public void StopPickPositionTargetOnMap()
        {
            pickingPositionTarget = false;
        }

        public void Unset()
        {
            Set(null);
        }

        public void UpdateDirectionTarget(Vector3d direction)
        {
            targetDirection = direction;
        }

        public bool NormalTargetExists
        {
            get
            {
                return (target != null && (target is Vessel || target is CelestialBody || CanAlign));
            }
        }

        public bool PositionTargetExists
        {
            get
            {
                return (target != null && ((target is PositionTarget) || (target is Vessel)) && !(target is DirectionTarget));
            }
        }

		public bool CanAlign
		{
			get { return target.GetTargetingMode() == VesselTargetModes.DirectionVelocityAndOrientation; }
		}

        public ITargetable Target
        {
            get { return target; }
        }

        public Orbit TargetOrbit
        {
            get {
                if (target == null)
                    return null;
                return target.GetOrbit();
            }
        }

        public Vector3 Position
        {
            get { return Transform.position; }
        }

        public float Distance
        {
            get { return Vector3.Distance(Position, vessel.GetTransform().position); }
        }

        public Vector3d RelativeVelocity
        {
            get { return (vessel.orbit.GetVel() - TargetOrbit.GetVel()); }
        }

        public Vector3d RelativePosition
        {
            get { return (vessel.GetTransform().position - Position); }
        }

        public Transform Transform
        {
            get { return target.GetTransform(); }
        }

        //which way your vessel should be pointing to dock with the target
        public Vector3 DockingAxis
        {
            get
            {
                if (CanAlign) return -Transform.forward;
                return -Transform.up;
            }
        }

        public string Name
        {
            get { return target.GetName(); }
        }

        ////////////////////////
        // Internal functions //
        ////////////////////////

        public override void OnStart(PartModule.StartState state)
        {
            RenderingManager.AddToPostDrawQueue(1, DoMapView);

            users.Add(this); //TargetController should always be running
        }

        public override void OnFixedUpdate()
        {
            //Restore the saved target when we are made active vessel
            if (!wasActiveVessel && vessel.isActiveVessel)
            {
                if (target != null && target.GetVessel() != null)
                {                    
                    FlightGlobals.fetch.SetVesselTarget(target);
                }
            }

            //notice when the user switches targets
            if (vessel.isActiveVessel && target != FlightGlobals.fetch.VesselTarget)
            {
                target = FlightGlobals.fetch.VesselTarget;
                if (target is Vessel && ((Vessel)target).LandedOrSplashed && (((Vessel)target).mainBody == vessel.mainBody))
                {
                    targetBody = vessel.mainBody;
                    targetLatitude = vessel.mainBody.GetLatitude(target.GetTransform().position);
                    targetLongitude = vessel.mainBody.GetLongitude(target.GetTransform().position);
                }
            }


            // .23 temp fix until I understand better what's going on
            if (targetBody == null)
                targetBody = vessel.mainBody;
            
            //Update targets that need updating:
            if (target is DirectionTarget) ((DirectionTarget)target).Update(targetDirection);
            else if (target is PositionTarget) ((PositionTarget)target).Update(targetBody, targetLatitude, targetLongitude);

            wasActiveVessel = vessel.isActiveVessel;
        }

        public override void OnUpdate()
        {
            if (pickingPositionTarget && !GuiUtils.MouseIsOverWindow(core) && GuiUtils.GetMouseCoordinates(mainBody) != null) Screen.showCursor = false;
            else Screen.showCursor = true;
        }

        void DoMapView()
        {
            DoCoordinatePicking();

            DrawMapViewTarget();
        }

        void DoCoordinatePicking()
        {
            if (pickingPositionTarget && !MapView.MapIsEnabled) pickingPositionTarget = false; //stop picking on leaving map view

            if (!pickingPositionTarget) return;

            if (MapView.MapIsEnabled && vessel.isActiveVessel)
            {
                if (!GuiUtils.MouseIsOverWindow(core))
                {
                    Coordinates mouseCoords = GuiUtils.GetMouseCoordinates(mainBody);

                    if (mouseCoords != null)
                    {
                        GLUtils.DrawMapViewGroundMarker(mainBody, mouseCoords.latitude, mouseCoords.longitude, new Color(1.0f, 0.56f, 0.0f));
                        GUI.Label(new Rect(Input.mousePosition.x + 15, Screen.height - Input.mousePosition.y, 200, 50), mouseCoords.ToStringDecimal() + "\n" + ScienceUtil.GetExperimentBiome(mainBody, mouseCoords.latitude, mouseCoords.longitude));

                        if (Input.GetMouseButtonDown(0))
                        {
                            SetPositionTarget(mainBody, mouseCoords.latitude, mouseCoords.longitude);
                            pickingPositionTarget = false;
                        }
                    }
                }
            }
        }

        void DrawMapViewTarget()
        {
            if (HighLogic.LoadedSceneIsEditor) return;
            if (!MapView.MapIsEnabled) return;
            if (!vessel.isActiveVessel || vessel.GetMasterMechJeb() != core) return;

            if (target == null) return;
            if (!(target is PositionTarget) && !(target is Vessel)) return;
            if ((target is Vessel) && (!((Vessel)target).LandedOrSplashed || (((Vessel)target).mainBody != vessel.mainBody))) return;
            if (target is DirectionTarget) return;

            GLUtils.DrawMapViewGroundMarker(targetBody, targetLatitude, targetLongitude, Color.red);
        }
    }

    public class PositionTarget : ITargetable
    {
        GameObject g = new GameObject();
        string name;
        Vector3d position;

        public PositionTarget(string name)
        {
            this.name = name;
        }

        public void Update(CelestialBody body, double latitude, double longitude)
        {
            double altitude = body.TerrainAltitude(latitude, longitude);
            Update(body, latitude, longitude, altitude);
        }

        public void Update(CelestialBody body, double latitude, double longitude, double altitude)
        {
            Update(body.GetWorldSurfacePosition(latitude, longitude, altitude));
        }

        //Call this every frame to make sure the target transform stays up to date
        public void Update(Vector3d position)
        {
            this.position = position;
            g.transform.position = position;
        }

        public Vector3 GetFwdVector() { return Vector3d.up; }
        public string GetName() { return name; }
        public Vector3 GetObtVelocity() { return Vector3.zero; }
        public Orbit GetOrbit() { return null; }
        public OrbitDriver GetOrbitDriver() { return null; }
        public Vector3 GetSrfVelocity() { return Vector3.zero; }
        public Transform GetTransform() { return g.transform; }
        public Vessel GetVessel() { return null; }
        public VesselTargetModes GetTargetingMode() { return VesselTargetModes.Direction ; }
    }

    public class DirectionTarget : PositionTarget
    {
        Vector3d direction;

        public DirectionTarget(string name) : base(name) { }

        //Call this every frame to make sure the target transform stays up to date
        public new void Update(Vector3d direction)
        {
            this.direction = direction;
            base.Update(FlightGlobals.ActiveVessel.transform.position + 10000 * direction);
        }
    }
}
