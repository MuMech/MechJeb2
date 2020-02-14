using System;
using ModuleWheels;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleLandingAutopilot : AutopilotModule
    {
        bool deployedGears;
        public bool landAtTarget;

        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public EditableDouble touchdownSpeed = 0.5;

        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public bool deployGears = true;
        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public EditableInt limitGearsStage = 0;
        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public bool deployChutes = true;
        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public EditableInt limitChutesStage = 0;
        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public bool rcsAdjustment = true;

        // This is used to adjust the height at which the parachutes semi deploy as a means of
        // targeting the landing in an atmosphere where it is not possible to control atitude
        // to perform course correction burns.
        ParachutePlan parachutePlan = null;

        //Landing prediction data:
        MechJebModuleLandingPredictions predictor;
        public ReentrySimulation.Result prediction;
        ReentrySimulation.Result errorPrediction;

        public bool PredictionReady //We shouldn't do any autopilot stuff until this is true
        {
            get
            {
                // Check that there is a prediction and that it is a landing prediction.
                if (prediction == null)
                {
                    return false;
                }
                else if (prediction.outcome != ReentrySimulation.Outcome.LANDED)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        bool ErrorPredictionReady // We shouldn't try to use an ErrorPrediction until this is true.
        {
            get
            {
                return (errorPrediction != null) &&
                    (errorPrediction.outcome == ReentrySimulation.Outcome.LANDED);
            }
        }
        public double LandingAltitude // The altitude above sea level of the terrain at the landing site
        {
            get
            {
                if (PredictionReady)
                {
                    // Although we know the landingASL as it is in the prediction, we suspect that
                    // it might sometimes be incorrect. So to check we will calculate it again here,
                    // and if the two differ log an error. It seems that this terrain ASL calls when
                    // made from the simulatiuon thread are regularly incorrect, but are OK when made
                    // from this thread. At the time of writting (KSP0.23) there seem to be several
                    // other things going wrong with he terrain system, such as visual glitches as
                    // we as the occasional exceptions being thrown when calls to the CelestialBody
                    // object are made. I suspect a bug or some sort - for now this hack improves
                    // the landing results.
                    {
                        double checkASL = prediction.body.TerrainAltitude(prediction.endPosition.latitude, prediction.endPosition.longitude);
                        if (checkASL != prediction.endASL)
                        {
                            // I know that this check is not required as we might as well always make
                            // the asignment. However this allows for some debug monitoring of how often this is occuring.
                            prediction.endASL = checkASL;
                        }
                    }

                    return prediction.endASL;
                }
                else
                {
                    return 0;
                }
            }
        }

        public Vector3d LandingSite // The current position of the landing site
        {
            get
            {
                return mainBody.GetWorldSurfacePosition(prediction.endPosition.latitude,
                    prediction.endPosition.longitude, LandingAltitude) - mainBody.position;
            }
        }

        Vector3d RotatedLandingSite // The position where the landing site will be when we land at it
        {
            get { return prediction.WorldEndPosition(); }
        }

        public IDescentSpeedPolicy descentSpeedPolicy;
        public double vesselAverageDrag;

        public MechJebModuleLandingAutopilot(MechJebCore core)
            : base(core)
        {
        }

        public override void OnStart(PartModule.StartState state)
        {
            predictor = core.GetComputerModule<MechJebModuleLandingPredictions>();
        }

        //public interface:
        public void LandAtPositionTarget(object controller)
        {
            landAtTarget = true;
            users.Add(controller);

            predictor.users.Add(this);
            vessel.RemoveAllManeuverNodes(); // For the benefit of the landing predictions module

            deployedGears = false;

            // Create a new parachute plan
            parachutePlan = new ParachutePlan(this);
            parachutePlan.StartPlanning();

            if (orbit.PeA < 0)
                setStep(new Landing.CourseCorrection(core));
            else if (UseLowDeorbitStrategy())
                setStep(new Landing.PlaneChange(core));
            else
                setStep(new Landing.DeorbitBurn(core));
        }

        public void LandUntargeted(object controller)
        {
            landAtTarget = false;
            users.Add(controller);

            deployedGears = false;

            // Create a new parachute plan
            this.parachutePlan = new ParachutePlan(this);
            this.parachutePlan.StartPlanning();

            setStep(new Landing.UntargetedDeorbit(core));
        }

        public void StopLanding()
        {
            this.users.Clear();
            core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
            if (core.landing.rcsAdjustment)
                core.rcs.enabled = false;
            setStep(null);
        }

        public override void Drive(FlightCtrlState s)
        {
            if (!active)
                return;

            // If the latest prediction is a landing, aerobrake or no-reentry prediciton then keep it.
            // However if it is any other sort or result it is not much use to us, so do not bother!
            {
                ReentrySimulation.Result result = predictor.Result;
                if (null != result)
                {
                    if (result.outcome != ReentrySimulation.Outcome.ERROR && result.outcome != ReentrySimulation.Outcome.TIMED_OUT)
                    {
                        this.prediction = result;
                    }
                }
            }
            {
                ReentrySimulation.Result result = predictor.GetErrorResult();
                if (null != result)
                {
                    if (result.outcome != ReentrySimulation.Outcome.ERROR && result.outcome != ReentrySimulation.Outcome.TIMED_OUT)
                    {
                        this.errorPrediction = result;
                    }
                }
            }

            descentSpeedPolicy = PickDescentSpeedPolicy();

            predictor.descentSpeedPolicy = PickDescentSpeedPolicy(); //create a separate IDescentSpeedPolicy object for the simulation
            predictor.decelEndAltitudeASL = DecelerationEndAltitude();
            predictor.parachuteSemiDeployMultiplier = this.parachutePlan.Multiplier;

            // Consider lowering the langing gear
            {
                double minalt = Math.Min(vesselState.altitudeBottom, Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue));
                if (deployGears && !deployedGears && (minalt < 1000))
                    DeployLandingGears();
            }

            base.Drive(s);
        }

        public override void OnFixedUpdate()
        {
            vesselAverageDrag = VesselAverageDrag();
            base.OnFixedUpdate();
            DeployParachutes();
        }

        public override void OnModuleEnabled()
        {
            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
            predictor.users.Remove(this);
            predictor.descentSpeedPolicy = null;
            core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
            if (core.landing.rcsAdjustment)
                core.rcs.enabled = false;
            setStep(null);
        }

        // Estimate the delta-V of the correction burn that would be required to put us on
        // course for the target
        public Vector3d ComputeCourseCorrection(bool allowPrograde)
        {
            // actualLandingPosition is the predicted actual landing position
            Vector3d actualLandingPosition = RotatedLandingSite - mainBody.position;

            // orbitLandingPosition is the point where our current orbit intersects the planet
            double endRadius = mainBody.Radius + DecelerationEndAltitude() - 100;

            // Seems we are already landed ?
            if (endRadius > orbit.ApR || vessel.LandedOrSplashed)
                StopLanding();

            Vector3d orbitLandingPosition;
            if (orbit.PeR < endRadius)
                orbitLandingPosition = orbit.SwappedRelativePositionAtUT(orbit.NextTimeOfRadius(vesselState.time, endRadius));
            else
                orbitLandingPosition = orbit.SwappedRelativePositionAtUT(orbit.NextPeriapsisTime(vesselState.time));

            // convertOrbitToActual is a rotation that rotates orbitLandingPosition on actualLandingPosition
            Quaternion convertOrbitToActual = Quaternion.FromToRotation(orbitLandingPosition, actualLandingPosition);

            // Consider the effect small changes in the velocity in each of these three directions
            Vector3d[] perturbationDirections = { vesselState.surfaceVelocity.normalized, vesselState.radialPlusSurface, vesselState.normalPlusSurface };

            // Compute the effect burns in these directions would
            // have on the landing position, where we approximate the landing position as the place
            // the perturbed orbit would intersect the planet.
            Vector3d[] deltas = new Vector3d[3];
            for (int i = 0; i < 3; i++)
            {
                const double perturbationDeltaV = 1; //warning: hard experience shows that setting this too low leads to bewildering bugs due to finite precision of Orbit functions
                Orbit perturbedOrbit = orbit.PerturbedOrbit(vesselState.time, perturbationDeltaV * perturbationDirections[i]); //compute the perturbed orbit
                double perturbedLandingTime;
                if (perturbedOrbit.PeR < endRadius) perturbedLandingTime = perturbedOrbit.NextTimeOfRadius(vesselState.time, endRadius);
                else perturbedLandingTime = perturbedOrbit.NextPeriapsisTime(vesselState.time);
                Vector3d perturbedLandingPosition = perturbedOrbit.SwappedRelativePositionAtUT(perturbedLandingTime); //find where it hits the planet
                Vector3d landingDelta = perturbedLandingPosition - orbitLandingPosition; //find the difference between that and the original orbit's intersection point
                landingDelta = convertOrbitToActual * landingDelta; //rotate that difference vector so that we can now think of it as starting at the actual landing position
                landingDelta = Vector3d.Exclude(actualLandingPosition, landingDelta); //project the difference vector onto the plane tangent to the actual landing position
                deltas[i] = landingDelta / perturbationDeltaV; //normalize by the delta-V considered, so that deltas now has units of meters per (meter/second) [i.e., seconds]
            }

            // Now deltas stores the predicted offsets in landing position produced by each of the three perturbations.
            // We now figure out the offset we actually want

            // First we compute the target landing position. We have to convert the latitude and longitude of the target
            // into a position. We can't just get the current position of those coordinates, because the planet will
            // rotate during the descent, so we have to account for that.
            Vector3d desiredLandingPosition = mainBody.GetWorldSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0) - mainBody.position;
            float bodyRotationAngleDuringDescent = (float)(360 * (prediction.endUT - vesselState.time) / mainBody.rotationPeriod);
            Quaternion bodyRotationDuringFall = Quaternion.AngleAxis(bodyRotationAngleDuringDescent, mainBody.angularVelocity.normalized);
            desiredLandingPosition = bodyRotationDuringFall * desiredLandingPosition;

            Vector3d desiredDelta = desiredLandingPosition - actualLandingPosition;
            desiredDelta = Vector3d.Exclude(actualLandingPosition, desiredDelta);

            // Now desiredDelta gives the desired change in our actual landing position (projected onto a plane
            // tangent to the actual landing position).

            Vector3d downrangeDirection;
            Vector3d downrangeDelta;
            if (allowPrograde)
            {
                // Construct the linear combination of the prograde and radial+ perturbations
                // that produces the largest effect on the landing position. The Math.Sign is to
                // detect and handle the case where radial+ burns actually bring the landing sign closer
                // (e.g. when we are traveling close to straight up)
                downrangeDirection = (deltas[0].magnitude * perturbationDirections[0]
                    + Math.Sign(Vector3d.Dot(deltas[0], deltas[1])) * deltas[1].magnitude * perturbationDirections[1]).normalized;

                downrangeDelta = Vector3d.Dot(downrangeDirection, perturbationDirections[0]) * deltas[0]
                    + Vector3d.Dot(downrangeDirection, perturbationDirections[1]) * deltas[1];
            }
            else
            {
                // If we aren't allowed to burn prograde, downrange component of the landing
                // position has to be controlled by radial+/- burns:
                downrangeDirection = perturbationDirections[1];
                downrangeDelta = deltas[1];
            }

            // Now solve a 2x2 system of linear equations to determine the linear combination
            // of perturbationDirection01 and normal+ that will give the desired offset in the
            // predicted landing position.
            Matrix2x2 A = new Matrix2x2(
                downrangeDelta.sqrMagnitude, Vector3d.Dot(downrangeDelta, deltas[2]),
                Vector3d.Dot(downrangeDelta, deltas[2]), deltas[2].sqrMagnitude
            );

            Vector2d b = new Vector2d(Vector3d.Dot(desiredDelta, downrangeDelta), Vector3d.Dot(desiredDelta, deltas[2]));

            Vector2d coeffs = A.inverse() * b;

            Vector3d courseCorrection = coeffs.x * downrangeDirection + coeffs.y * perturbationDirections[2];

            return courseCorrection;
        }

        public void ControlParachutes()
        {
            // Firstly - do we have a parachute plan? If not then we had better get one quick!
            if (null == parachutePlan)
            {
                parachutePlan = new ParachutePlan(this);
            }

            // Are there any deployable parachute? If not then there is no point in us being here. Let's switch to cruising to the deceleration burn instead.
            if (!ParachutesDeployable())
            {
                predictor.runErrorSimulations = false;
                parachutePlan.ClearData();
                return;
            }
            else
            {
                predictor.runErrorSimulations = true;
            }

            // Is there an error prediction available? If so add that into the mix
            if (ErrorPredictionReady && !double.IsNaN(errorPrediction.parachuteMultiplier))
            {
                parachutePlan.AddResult(errorPrediction);
            }

            // Has the Landing prediction been updated? If so then we can use the result to refine our parachute plan.
            if (PredictionReady && !double.IsNaN(prediction.parachuteMultiplier))
            {
                parachutePlan.AddResult(prediction);
            }
        }

        void DeployParachutes()
        {
            if (vesselState.mainBody.atmosphere && deployChutes)
            {
                for (int i = 0; i < vesselState.parachutes.Count; i++)
                {
                    ModuleParachute p = vesselState.parachutes[i];
                    // what is the ASL at which we should deploy this parachute? It is the actual deployment height above the surface + the ASL of the predicted landing point.
                    double LandingSiteASL = LandingAltitude;
                    double ParachuteDeployAboveGroundAtLandingSite = p.deployAltitude * this.parachutePlan.Multiplier;

                    double ASLDeployAltitude = ParachuteDeployAboveGroundAtLandingSite + LandingSiteASL;

                    if (p.part.inverseStage >= limitChutesStage && p.deploymentState == ModuleParachute.deploymentStates.STOWED &&
                        ASLDeployAltitude > vesselState.altitudeASL && p.deploymentSafeState == ModuleParachute.deploymentSafeStates.SAFE)
                    {
                        p.Deploy();
                        //Debug.Log("Deploying parachute " + p.name + " at " + ASLDeployAltitude + ". (" + LandingSiteASL + " + " + ParachuteDeployAboveGroundAtLandingSite +")");
                    }
                }
            }
        }

        // This methods works out if there are any parachutes that are capable of being deployed
        public bool ParachutesDeployable()
        {
            if (!vesselState.mainBody.atmosphere) return false;
            if (!deployChutes) return false;

            for (int i = 0; i < vesselState.parachutes.Count; i++)
            {
                ModuleParachute p = vesselState.parachutes[i];
                if (Math.Max(p.part.inverseStage,0) >= limitChutesStage && p.deploymentState == ModuleParachute.deploymentStates.STOWED)
                {
                    return true;
                }
            }

            return false;
        }

        // This methods works out if there are any parachutes that have already been deployed (or semi deployed)
        bool ParachutesDeployed()
        {
            return vesselState.parachuteDeployed;
        }


        void DeployLandingGears()
        {
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                if (p.HasModule<ModuleWheelDeployment>())
                {
                    // p.inverseStage is -1 for some configuration ?!?
                    if (Math.Max(p.inverseStage, 0) >= limitGearsStage)
                    {
                        foreach (ModuleWheelDeployment wd in p.FindModulesImplementing<ModuleWheelDeployment>())
                        {
                            if (wd.fsm.CurrentState == wd.st_retracted || wd.fsm.CurrentState == wd.st_retracting)
                                wd.EventToggle();
                        }
                    }
                }
            }
            deployedGears = true;
        }

        IDescentSpeedPolicy PickDescentSpeedPolicy()
        {
            if (UseAtmosphereToBrake())
            {
                return new PoweredCoastDescentSpeedPolicy(mainBody.Radius + DecelerationEndAltitude(), mainBody.GeeASL * 9.81, vesselState.limitedMaxThrustAccel);
            }

            return new SafeDescentSpeedPolicy(mainBody.Radius + DecelerationEndAltitude(), mainBody.GeeASL * 9.81, vesselState.limitedMaxThrustAccel);
        }

        public double DecelerationEndAltitude()
        {
            if (UseAtmosphereToBrake())
            {
                // if the atmosphere is thick, deceleration (meaning freefall through the atmosphere)
                // should end a safe height above the landing site in order to allow braking from terminal velocity
                // FIXME: Drag Length is quite large now without parachutes, check this better
                double landingSiteDragLength = mainBody.DragLength(LandingAltitude, vesselAverageDrag + ParachuteAddedDragCoef(), vesselState.mass);

                //MechJebCore.print("DecelerationEndAltitude Atmo " + (2 * landingSiteDragLength + LandingAltitude).ToString("F2"));
                return 1.1 * landingSiteDragLength + LandingAltitude;
            }
            else
            {
                //if the atmosphere is thin, the deceleration burn should end
                //500 meters above the landing site to allow for a controlled final descent
                //MechJebCore.print("DecelerationEndAltitude Vacum " + (500 + LandingAltitude).ToString("F2"));
                return 500 + LandingAltitude;
            }
        }

        //On planets with thick enough atmospheres, we shouldn't do a deceleration burn. Rather,
        //we should let the atmosphere decelerate us and only burn during the final descent to
        //ensure a safe touchdown speed. How do we tell if the atmosphere is thick enough? We check
        //to see if there is an altitude within the atmosphere for which the characteristic distance
        //over which drag slows the ship is smaller than the altitude above the terrain. If so, we can
        //expect to get slowed to near terminal velocity before impacting the ground.
        public bool UseAtmosphereToBrake()
        {
            double landingSiteDragLength = mainBody.DragLength(LandingAltitude, vesselAverageDrag + ParachuteAddedDragCoef(), vesselState.mass);

            //if (mainBody.RealMaxAtmosphereAltitude() > 0 && (ParachutesDeployable() || ParachutesDeployed()))
            if (mainBody.RealMaxAtmosphereAltitude() > 0 && landingSiteDragLength < 0.7 * mainBody.RealMaxAtmosphereAltitude()) // the ratio is totally arbitrary until I get something better
            {
                return true;
            }
            return false;
        }

        // Get an average drag for the whole vessel. Far from precise but fast.
        public double VesselAverageDrag()
        {
            float dragCoef = 0;
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part part = vessel.parts[i];
                if (part.DragCubes.None || part.ShieldedFromAirstream)
                {
                    continue;
                }
                //DragCubeList.CubeData data = part.DragCubes.AddSurfaceDragDirection(Vector3.back, 1);
                //
                //dragCoef += data.areaDrag;

                float partAreaDrag = 0;
                for (int f = 0; f < 6; f++)
                {
                    partAreaDrag = part.DragCubes.WeightedDrag[f] * part.DragCubes.AreaOccluded[f]; // * PhysicsGlobals.DragCurveValue(0.5, machNumber) but I ll assume it is 1 for now
                }
                dragCoef += partAreaDrag / 6;
            }
            return dragCoef * PhysicsGlobals.DragCubeMultiplier;
        }

        // This is not the exact number, but it's good enough for our use
        public double ParachuteAddedDragCoef()
        {
            double addedDragCoef = 0;
            if (vesselState.mainBody.atmosphere && deployChutes)
            {
                for (int i = 0; i < vesselState.parachutes.Count; i++)
                {
                    ModuleParachute p = vesselState.parachutes[i];
                    if (p.part.inverseStage >= limitChutesStage)
                    {
                        //addedDragMass += p.part.DragCubes.Cubes.Where(c => c.Name == "DEPLOYED").m

                        float maxCoef = 0;
                        for (int c = 0; c < p.part.DragCubes.Cubes.Count; c++)
                        {
                            DragCube dragCube = p.part.DragCubes.Cubes[c];
                            if (dragCube.Name != "DEPLOYED")
                                continue;

                            for (int f = 0; f < 6; f++)
                            {
                                // we only want the additional coef from going fully deployed
                                maxCoef = Mathf.Max(maxCoef, p.part.DragCubes.WeightedDrag[f] - dragCube.Weight * dragCube.Drag[f]);
                            }
                        }
                        addedDragCoef += maxCoef;
                    }
                }
            }
            return addedDragCoef * PhysicsGlobals.DragCubeMultiplier;
        }

        bool UseLowDeorbitStrategy()
        {
            if (mainBody.atmosphere) return false;

            double periapsisSpeed = orbit.SwappedOrbitalVelocityAtUT(orbit.NextPeriapsisTime(vesselState.time)).magnitude;
            double stoppingDistance = Math.Pow(periapsisSpeed, 2) / (2 * vesselState.limitedMaxThrustAccel);

            return orbit.PeA < 2 * stoppingDistance + mainBody.Radius / 4;
        }

        public double MaxAllowedSpeed()
        {
            return descentSpeedPolicy.MaxAllowedSpeed(vesselState.CoM - mainBody.position, vesselState.surfaceVelocity);
        }

        public double MaxAllowedSpeedAfterDt(double dt)
        {
            return descentSpeedPolicy.MaxAllowedSpeed(vesselState.CoM + vesselState.orbitalVelocity * dt - mainBody.position,
                vesselState.surfaceVelocity + dt * vesselState.gravityForce);
        }

        [ValueInfoItem("#MechJeb_ParachuteControlInfo", InfoItem.Category.Misc, showInEditor = false)]//ParachuteControlInfo
        public string ParachuteControlInfo()
        {
            if (this.ParachutesDeployable())
            {
                string retVal = Localizer.Format("#MechJeb_ChuteMultiplier", this.parachutePlan.Multiplier.ToString("F7"));//"'Chute Multiplier: " +
                retVal += Localizer.Format("#MechJeb_MultiplierQuality", this.parachutePlan.MultiplierQuality.ToString("F1"));//"\nMultiplier Quality: " +  + "%"
                retVal += Localizer.Format("#MechJeb_Usingpredictions", this.parachutePlan.MultiplierDataAmount);//"\nUsing " +  + " predictions"

                return retVal;
            }
            else
            {
                return "N/A";
            }
        }

        public void SetTargetKSC(MechJebCore controller)
        {
            users.Add(controller);
            core.target.SetPositionTarget(mainBody, MechJebModuleLandingGuidance.landingSites[0].latitude, MechJebModuleLandingGuidance.landingSites[0].longitude);
        }
    }


    //A descent speed policy that gives the max safe speed if our entire velocity were straight down
    class SafeDescentSpeedPolicy : IDescentSpeedPolicy
    {
        double terrainRadius;
        double g;
        double thrust;

        public SafeDescentSpeedPolicy(double terrainRadius, double g, double thrust)
        {
            this.terrainRadius = terrainRadius;
            this.g = g;
            this.thrust = thrust;
        }

        public double MaxAllowedSpeed(Vector3d pos, Vector3d vel)
        {
            double altitude = pos.magnitude - terrainRadius;
            return 0.9 * Math.Sqrt(2 * (thrust - g) * altitude);
        }
    }

    class PoweredCoastDescentSpeedPolicy : IDescentSpeedPolicy
    {
        float terrainRadius;
        float g;
        float thrust;

        public PoweredCoastDescentSpeedPolicy(double terrainRadius, double g, double thrust)
        {
            this.terrainRadius = (float)terrainRadius;
            this.g = (float)g;
            this.thrust = (float)thrust;
        }

        public double MaxAllowedSpeed(Vector3d pos, Vector3d vel)
        {

            if (terrainRadius < pos.magnitude)
                return Double.MaxValue;

            double vSpeed = Vector3d.Dot(vel, pos.normalized);
            double ToF = (vSpeed + Math.Sqrt(vSpeed * vSpeed + 2 * g * (pos.magnitude - terrainRadius))) / g;

            //MechJebCore.print("ToF = " + ToF.ToString("F2"));
            return 0.8 * (thrust - g) * ToF;
        }
    }

    class GravityTurnDescentSpeedPolicy : IDescentSpeedPolicy
    {
        double terrainRadius;
        double g;
        double thrust;

        public GravityTurnDescentSpeedPolicy(double terrainRadius, double g, double thrust)
        {
            this.terrainRadius = terrainRadius;
            this.g = g;
            this.thrust = thrust;
        }

        public double MaxAllowedSpeed(Vector3d pos, Vector3d vel)
        {
            //do a binary search for the max speed that avoids death
            double maxFallDistance = pos.magnitude - terrainRadius;

            double lowerBound = 0;
            double upperBound = 1.1 * vel.magnitude;

            while (upperBound - lowerBound > 0.1)
            {
                double test = (upperBound + lowerBound) / 2;
                if (GravityTurnFallDistance(pos, test * vel.normalized) < maxFallDistance) lowerBound = test;
                else upperBound = test;
            }
            return 0.95 * ((upperBound + lowerBound) / 2);
        }

        public double GravityTurnFallDistance(Vector3d x, Vector3d v)
        {
            double startRadius = x.magnitude;

            const int steps = 10;
            for (int i = 0; i < steps; i++)
            {
                Vector3d gVec = -g * x.normalized;
                Vector3d thrustVec = -thrust * v.normalized;
                double dt = (1.0 / (steps - i)) * (v.magnitude / thrust);
                Vector3d newV = v + dt * (thrustVec + gVec);
                x += dt * (v + newV) / 2;
                v = newV;
            }

            double endRadius = x.magnitude;

            endRadius -= v.sqrMagnitude / (2 * (thrust - g));

            return startRadius - endRadius;
        }
    }

    // Class to hold all the information about the planned deployment of parachutes.
    class ParachutePlan
    {
        // We use a linear regresion to calculate the best place to deploy based on previous predictions
        LinearRegression regression;
        ReentrySimulation.Result lastResult; //  store the last result so that we can check if any new result is actually a new one, or the same one again.
        ReentrySimulation.Result lastErrorResult; //  store the last error result so that we can check if any new error result is actually a new one, or the same one again.
        CelestialBody body;
        MechJebModuleLandingAutopilot autoPilot;
        bool parachutePresent = false;
        double maxSemiDeployHeight;
        double minSemiDeployHeight;
        double maxMultiplier;
        double currentMultiplier;  // This is multiplied with the parachutes fulldeploy height to give the height at which the parachutes will be semideployed.
        double correlation; // This is the correlation coefficent of the dataset, and is used to tell if the data set is providing helpful information or not. It is exposed outside the class as a "quality percentage" where -1 -> 100% and 0 or more -> 0%
        const int dataSetSize = 40;

        public double Multiplier
        {
            get { return currentMultiplier; }
        }

        public int MultiplierDataAmount
        {
            get { return this.regression.dataSetSize; }
        }

        public double MultiplierQuality
        {
            get
            {
                double corr = this.correlation;
                if (Double.IsNaN(corr)) return 0;
                return Math.Max(0, corr * -100);
            }
        }

        // Incorporates a new simulation result into the simulation data set and calculate a new semi deployment multiplier. If the data set has a poor correlation, then it might just leave the mutiplier. If the correlation becomes positive then it will clear the dataset and start again.
        public void AddResult(ReentrySimulation.Result newResult)
        {
            // if this result is the same as the old result, then it is not new!
            if (newResult.multiplierHasError)
            {
                if (lastErrorResult != null)
                {
                    if (newResult.id == lastErrorResult.id) { return; }
                }
                lastErrorResult = newResult;
            }
            else
            {
                if (lastResult != null)
                {
                    if (newResult.id == lastResult.id) { return; }
                }
                lastResult = newResult;
            }

            // What was the overshoot for this new result?
            double overshoot = newResult.GetOvershoot(this.autoPilot.core.target.targetLatitude, this.autoPilot.core.target.targetLongitude);

            //Debug.Log("overshoot: " + overshoot.ToString("F2") + " multiplier: " + newResult.parachuteMultiplier.ToString("F4") + " hasError:" + newResult.multiplierHasError);

            // Add the new result to the linear regression
            regression.Add(overshoot, newResult.parachuteMultiplier);

            // What is the correlation coefficent of the data. If it is weak a correlation then we will dismiss the dataset and use it to change the current multiplier
            correlation = regression.CorrelationCoefficent;
            if (correlation > -0.2) // TODO this is the best value to test for non-correlation?
            {
                // If the correlation is less that 0 then we will give up controlling the parachutes and throw away the dataset. Also check that we have got several bits of data, just in case we get two datapoints that are badly correlated.
                if (correlation > 0 && this.regression.dataSetSize > 5)
                {
                    ClearData();
                    // Debug.Log("Giving up control of the parachutes as the data does not correlate: " + correlation);
                }
                else
                {
                    // Debug.Log("Ignoring the simulation dataset because the correlation is not significant enough: " + correlation);
                }
            }
            else
            {
                // How much data is there? If just one datapoint then we need to slightly vary the multplier to avoid doing exactly the same multiplier again and getting a divide by zero!. If there is just two then we will not update the multiplier as we can't conclude much from two points of data!
                int dataSetSize = regression.dataSetSize;
                if (dataSetSize == 1)
                {
                    this.currentMultiplier *= 0.99999;
                }
                else if (dataSetSize == 2)
                {
                    // Doing nothing
                }
                else
                {
                    // Use the linear regression to give us a new prediciton for when to open the parachutes
                    try
                    {
                        this.currentMultiplier = regression.yIntercept;
                    }
                    catch (Exception)
                    {
                        // If there is not enough data then we expect an exception. However we need to vary the multiplier everso slightly so that we get different data in order to start generating data. This should never happen as we have already checked the size of the dataset.
                        this.currentMultiplier *= 0.99999;
                    }
                }

                // Impose sensible limits on the multiplier
                if (this.currentMultiplier < 1 || double.IsNaN(currentMultiplier)) { this.currentMultiplier = 1; }
                if (this.currentMultiplier > this.maxMultiplier) { this.currentMultiplier = this.maxMultiplier; }
            }

            return;
        }

        public ParachutePlan(MechJebModuleLandingAutopilot _autopliot)
        {
            // Create the linear regression for storing previous prediction results
            this.regression = new LinearRegression(dataSetSize); // Store the previous however many predictions

            // Take a reference to the landing autopilot module that we are working for.
            this.autoPilot = _autopliot;

            // Take a note of which body this parachute plan is for. If we go to a different body, we will need a new plan!
            this.body = _autopliot.vessel.orbit.referenceBody;
        }

        // Throw away any old data, and create a new empty dataset
        public void ClearData()
        {
            // Create the linear regression for storing previous prediction results
            this.regression = new LinearRegression(dataSetSize); // Stored the previous 20 predictions
        }

        public void StartPlanning()
        {
            // what is the highest point at which we could semi deploy? - look at all the parachutes in the craft, and consider the lowest semi deployment pressure.
            float minSemiDeployPressure = 0;
            float maxFullDeployHeight = 0;
            parachutePresent = false; // First assume that there are no parachutes.

            // TODO should we check if each of these parachutes is withing the staging limit?
            for (int i = 0; i < autoPilot.vesselState.parachutes.Count; i++)
            {
                ModuleParachute p = autoPilot.vesselState.parachutes[i];
                if (p.minAirPressureToOpen > minSemiDeployPressure)
                // Although this is called "minSemiDeployPressure" we want to find the largest value for each of our parachutes. This can be used to calculate the corresponding height, and hence a height at which we can be guarenteed that all our parachutes will deploy if asked to.
                {
                    minSemiDeployPressure = p.minAirPressureToOpen;
                }
                if (p.deployAltitude > maxFullDeployHeight)
                {
                    maxFullDeployHeight = p.deployAltitude;
                }

                parachutePresent = true;
            }

            // If parachutes are present on the craft then work out the max / min semideployment heights and the starting value.
            if (parachutePresent)
            {
                // TODO is there benefit in running an initial simulation to calculate the height at which the ratio between vertical and horizontal velocity would be the best for being able to deply the chutes to control the landing site?

                // At what ASL height does the reference body have this pressure?
                maxSemiDeployHeight = body.AltitudeForPressure(minSemiDeployPressure);

                // We have to have semi deployed by the time we fully deploy.
                minSemiDeployHeight = maxFullDeployHeight;

                maxMultiplier = maxSemiDeployHeight / minSemiDeployHeight;

                // Set the inital multiplier to be the mid point.
                currentMultiplier = maxMultiplier / 2;
            }
        }
    }

    // A class to hold a set of x,y data and perform linear regression analysis on it
    class LinearRegression
    {
        double[] x;
        double[] y;
        int maxDataPoints;
        int dataPointCount;
        int currentDataPoint;
        double sumX;
        double sumY;
        double sumXX;
        double sumXY;
        double sumYY;

        public LinearRegression(int _maxDataPoints)
        {
            this.maxDataPoints = _maxDataPoints;
            this.dataPointCount = 0;
            this.currentDataPoint = -1;

            this.x = new double[_maxDataPoints];
            this.y = new double[_maxDataPoints];
        }

        // Add a new data point. It might be that this will replace an old data point, or it might be that this is the first or second datapoints
        public void Add(double _x, double _y)
        {
            currentDataPoint++;
            dataPointCount++;

            // wrap back to the begining if we have got the end of the array
            if (currentDataPoint >= maxDataPoints)
            {
                currentDataPoint = 0;
            }

            // If we have maxed out the number of data points then we need to remove the old values from the running totals
            if (dataPointCount > maxDataPoints)
            {
                dataPointCount = maxDataPoints;
            }

            x[currentDataPoint] = _x;
            y[currentDataPoint] = _y;

            // Calculate the new totals
            sumX = 0;
            sumY = 0;
            sumXX = 0;
            sumXY = 0;
            sumYY = 0;

            for (int i = 0; i < dataPointCount; i++)
            {
                double thisx = x[i];
                double thisy = y[i];

                sumX += thisx;
                sumXX += thisx * thisx;
                sumY += thisy;
                sumYY += thisy * thisy;
                sumXY += thisx * thisy;
            }
        }

        public double slope
        {
            get
            {
                // Require at least half the datapoints in the dataset
                if (this.dataPointCount < 2)
                {
                    throw new Exception("Not enough data to calculate trend line");
                }

                double result = (sumXY - ((sumX * sumY) / dataPointCount)) / (sumXX - ((sumX * sumX) / dataPointCount));

                return result;
            }
        }

        public double yIntercept
        {
            get
            {
                double result = (sumY / dataPointCount) - (slope * (sumX / dataPointCount));

                return result;
            }
        }

        public int dataSetSize
        {
            get
            {
                return this.dataPointCount;
            }
        }

        // Calculation the Pearson product-moment correlation coefficient
        public double CorrelationCoefficent
        {
            get
            {
                double stdX = Math.Sqrt(sumXX / dataPointCount - sumX * sumX / dataPointCount / dataPointCount);
                double stdY = Math.Sqrt(sumYY / dataPointCount - sumY * sumY / dataPointCount / dataPointCount);
                double covariance = (sumXY / dataPointCount - sumX * sumY / dataPointCount / dataPointCount);

                return covariance / stdX / stdY;
            }
        }
    }
}
