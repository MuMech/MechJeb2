using System;
using Smooth.Pools;
using UnityEngine;

namespace MuMech
{
    public class SimulatedParachute : SimulatedPart
    {
        private ModuleParachute para;

        private ModuleParachute.deploymentStates state;

        private double openningTime;

        // Store some data about when and how the parachute opened during the simulation which will be useful for debugging
        //double activatedASL = 0;
        //double activatedAGL = 0;
        //double semiDeployASL = 0;
        //double semiDeployAGL = 0;
        //double fullDeployASL = 0;
        //double fullDeployAGL = 0;
        //double targetASLAtSemiDeploy = 0;
        //double targetASLAtFullDeploy = 0;
        private float deployLevel;
        public bool deploying;

        // stock plays the animations at these speeds, so they aren't durations
        private double semiDeployDuration;
        private double fullDeployDuration;

        // which way the canopy faces along the airflow, see Drag
        private float canopySign;

        // Stock wobbles the canopy with Perlin noise every frame (ModuleParachute.LateUpdate) before it takes the canopy's rotation for
        // the drag cube. For a canopy along the part's up axis, the usual case, that rotation is ill-defined, so the wobble alone decides
        // which side of the cube stock reads: a direction across the canopy at an angle that changes every frame, often near a diagonal,
        // which reads two side faces. The simulation reads the cube at these sampled wobbles and keeps their mean against the reading
        // without wobble.
        private const int WOBBLE_SAMPLES = 16;
        private readonly Quaternion[] wobbles = new Quaternion[WOBBLE_SAMPLES];
        private float spreadAngle;
        private double wobbleRatio = 1;
        private ModuleParachute.deploymentStates wobbleState;
        private float wobbleDeployLevel = -1;
        private float wobbleMach = -1;

        // stock's minAirPressureToOpen is in atm, and the simulation's pressure in kPa
        private const double ATM_PER_KPA = 0.00986923;

        // whether the landing autopilot arms this chute; one armed already opens as stock opens it either way
        private bool willDeploy;

        private static readonly Pool<SimulatedParachute> pool = new Pool<SimulatedParachute>(Create, Reset);

        public new static int PoolSize => pool.Size;

        private static SimulatedParachute Create() => new SimulatedParachute();

        public override void Release()
        {
            foreach (DragCube cube in cubes.Cubes)
            {
                DragCubePool.Instance.Release(cube);
            }

            pool.Release(this);
        }

        private static void Reset(SimulatedParachute obj)
        {
        }

        public static SimulatedParachute Borrow(ModuleParachute mp, ReentrySimulation.SimCurves simCurve, double startTime, int limitChutesStage)
        {
            SimulatedParachute part = pool.Borrow();
            part.Init(mp.part, simCurve);
            part.Init(mp, startTime, limitChutesStage);
            return part;
        }

        private void Init(ModuleParachute mp, double startTime, int limitChutesStage)
        {
            para = mp;
            state = mp.deploymentState;
            semiDeployDuration = mp.Anim[mp.semiDeployedAnimation].length / mp.semiDeploymentSpeed;
            fullDeployDuration = mp.Anim[mp.fullyDeployedAnimation].length / mp.deploymentSpeed;
            canopySign = mp.invertCanopy ? 1f : -1f;
            spreadAngle = mp.symmetryCount > 0 ? Mathf.Clamp(mp.spreadAngle * mp.symmetryCount, 0f, 45f) : 0f;
            // Mathf.PerlinNoise is called here, on the main thread; stock's noise input is Time.time plus the craft ID, so these points
            // stand for its values over time
            for (int i = 0; i < WOBBLE_SAMPLES; i++)
            {
                float x = 0.5f + 1.37f * i;
                wobbles[i] = Quaternion.Euler(10f * (Mathf.PerlinNoise(x, 0f) - 0.5f), 10f * (Mathf.PerlinNoise(x, 8f) - 0.5f),
                    10f * (Mathf.PerlinNoise(x, 16f) - 0.5f));
            }

            wobbleDeployLevel = -1;
            wobbleRatio = 1;

            willDeploy = limitChutesStage != -1 && para.part.inverseStage >= limitChutesStage;

            // Work out when the chute was put into its current state based on the current drag as compared to the stowed, semi deployed and fully deployed drag

            double timeSinceDeployment = 0;

            switch (mp.deploymentState)
            {
                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    if (mp.Anim.isPlaying)
                        timeSinceDeployment = mp.Anim[mp.semiDeployedAnimation].normalizedTime * semiDeployDuration;
                    else
                        timeSinceDeployment = 10000000;
                    break;

                case ModuleParachute.deploymentStates.DEPLOYED:
                    if (mp.Anim.isPlaying)
                        timeSinceDeployment = mp.Anim[mp.fullyDeployedAnimation].normalizedTime * fullDeployDuration;
                    else
                        timeSinceDeployment = 10000000;
                    break;

                case ModuleParachute.deploymentStates.STOWED:
                case ModuleParachute.deploymentStates.ACTIVE:
                    // If the parachute is stowed then for some reason para.parachuteDrag does not reflect the stowed drag. set this up by hand. 
                    timeSinceDeployment = 10000000;
                    break;

                default:
                    // otherwise set the time since deployment to be a very large number to indcate that it has been in that state for a long time (although we do not know how long!
                    timeSinceDeployment = 10000000;
                    break;
            }

            openningTime = startTime - timeSinceDeployment;

            //Debug.Log("Parachute " + para.name + " parachuteDrag:" + this.parachuteDrag + " stowedDrag:" + para.stowedDrag + " semiDeployedDrag:" + para.semiDeployedDrag + " fullyDeployedDrag:" + para.fullyDeployedDrag + " part.maximum_drag:" + para.part.maximum_drag + " part.minimum_drag:" + para.part.minimum_drag + " semiDeploymentSpeed:" + para.semiDeploymentSpeed + " deploymentSpeed:" + para.deploymentSpeed + " deploymentState:" + para.deploymentState + " timeSinceDeployment:" + timeSinceDeployment);
            // Keep that test code until they fix the bug in the new parachute module
            //if ((realDrag / parachuteDrag) > 1.01d || (realDrag / parachuteDrag) < 0.99d)
            //    Debug.Log("Parachute " + para.name + " parachuteDrag:" + this.parachuteDrag.ToString("F3") + " RealDrag:" + realDrag.ToString("F3") + " MinDrag:" + para.part.minimum_drag.ToString("F3") + " MaxDrag:" + para.part.maximum_drag.ToString("F3"));
        }

        // Stock turns the canopy into the airflow and rotates the drag vector with it before reading the cube (ModuleParachute.LateUpdate,
        // DragCubeList.SetDrag), so an open parachute reads the side faces of its cube: 52.6 m2 instead of 115 for the big one.
        public override Vector3d Drag(Vector3d vesselVelocity, double dragFactor, float mach)
        {
            if (state != ModuleParachute.deploymentStates.SEMIDEPLOYED && state != ModuleParachute.deploymentStates.DEPLOYED)
                return base.Drag(vesselVelocity, dragFactor, mach);

            Vector3 canopyAxis = canopySign * -(vesselToPart * vesselVelocity).normalized;

            // the ratio changes with the cube weights and the mach number, and barely with anything else
            if (state != wobbleState || Math.Abs(deployLevel - wobbleDeployLevel) > 0.05f || Math.Abs(mach - wobbleMach) > 0.05f)
                UpdateWobbleRatio(canopyAxis, vesselVelocity, mach);

            cubes.SetDragVectorRotation(Quaternion.LookRotation(canopyAxis));
            return base.Drag(vesselVelocity, dragFactor, mach) * wobbleRatio;
        }

        // The mean area stock reads with its wobble against the area read along the canopy without it. Stock's canopy faces the airflow
        // with the part's forward axis as its up, is spread for symmetry, then wobbled, and the cube is turned with the rotation of the
        // wobbled canopy's forward alone (ModuleParachute.LateUpdate).
        private void UpdateWobbleRatio(Vector3 canopyAxis, Vector3d vesselVelocity, float mach)
        {
            wobbleState = state;
            wobbleDeployLevel = deployLevel;
            wobbleMach = mach;

            Vector3 velocityLocal = (vesselToPart * vesselVelocity).normalized;
            cubes.SetDragVectorRotation(Quaternion.LookRotation(canopyAxis));
            cubes.SetDrag(-velocityLocal, mach);
            double exact = cubes.AreaDrag;
            if (exact <= 0)
            {
                wobbleRatio = 1;
                return;
            }

            float spread = spreadAngle;
            if (state == ModuleParachute.deploymentStates.SEMIDEPLOYED)
                spread /= 3f;
            Quaternion canopy = Quaternion.LookRotation(canopyAxis, Vector3.forward) * Quaternion.Euler(spread, 0f, 0f);

            double sum = 0;
            for (int i = 0; i < WOBBLE_SAMPLES; i++)
            {
                cubes.SetDragVectorRotation(Quaternion.LookRotation(canopy * wobbles[i] * Vector3.forward));
                cubes.SetDrag(-velocityLocal, mach);
                sum += cubes.AreaDrag;
            }

            wobbleRatio = sum / WOBBLE_SAMPLES / exact;
        }

        // Stock's ShouldDeploy: below the deploy altitude above sea level or above the ground. It semi deploys an armed chute on this or
        // on the pressure, and deploys it fully on this.
        private bool BelowDeployAltitude(double altATGL, double altASL) => altATGL < para.deployAltitude || altASL < para.deployAltitude;

        // Stock opens an armed chute only in a deploy-safe state its automateSafeDeploy allows (SAFE by default). SAFE is this heat test;
        // RISKY depends on the chute's own temperature, which the simulation does not follow, so it counts as unsafe here.
        private bool SafeToOpen(double shockTemp) =>
            para.automateSafeDeploy >= (int)ModuleParachute.deploymentSafeStates.UNSAFE || shockTemp * para.machHeatMult <= para.chuteMaxTemp * para.safeMult;

        private bool ShouldSemiDeploy(double altATGL, double altASL, double pressure, double shockTemp) =>
            (pressure * ATM_PER_KPA >= para.minAirPressureToOpen || BelowDeployAltitude(altATGL, altASL)) && SafeToOpen(shockTemp);

        // Consider activating, semi deploying or deploying a parachute, but do not actually make any changes. returns true if the state has changed
        public override bool SimulateAndRollback(double altATGL, double altASL, double endASL, double pressure, double shockTemp, double time,
            double semiDeployMultiplier)
        {
            bool stateChanged = false;
            switch (state)
            {
                case ModuleParachute.deploymentStates.STOWED:
                    if (willDeploy && altATGL < semiDeployMultiplier * para.deployAltitude &&
                        shockTemp * para.machHeatMult < para.chuteMaxTemp * para.safeMult)
                    {
                        stateChanged = true;
                    }

                    break;
                case ModuleParachute.deploymentStates.ACTIVE:
                    if (ShouldSemiDeploy(altATGL, altASL, pressure, shockTemp))
                    {
                        stateChanged = true;
                    }

                    break;
                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    if (BelowDeployAltitude(altATGL, altASL) && time - openningTime >= semiDeployDuration)
                    {
                        stateChanged = true;
                    }

                    break;
            }

            return stateChanged;
        }

        // Consider activating, semi deploying or deploying a parachute. returns true if the state has changed
        public override bool Simulate(double altATGL, double altASL, double endASL, double pressure, double shockTemp, double time,
            double semiDeployMultiplier)
        {
            switch (state)
            {
                case ModuleParachute.deploymentStates.STOWED:
                    if (willDeploy && altATGL < semiDeployMultiplier * para.deployAltitude &&
                        shockTemp * para.machHeatMult < para.chuteMaxTemp * para.safeMult)
                    {
                        state = ModuleParachute.deploymentStates.ACTIVE;
                        //activatedAGL = altATGL;
                        //activatedASL = altASL;
                        // Immediately check to see if the parachute should be semi deployed, rather than waiting for another iteration.
                        if (ShouldSemiDeploy(altATGL, altASL, pressure, shockTemp))
                        {
                            state = ModuleParachute.deploymentStates.SEMIDEPLOYED;
                            openningTime = time;
                            //semiDeployAGL = altATGL;
                            //semiDeployASL = altASL;
                            //targetASLAtSemiDeploy = endASL;
                        }
                    }

                    break;
                case ModuleParachute.deploymentStates.ACTIVE:
                    if (ShouldSemiDeploy(altATGL, altASL, pressure, shockTemp))
                    {
                        state = ModuleParachute.deploymentStates.SEMIDEPLOYED;
                        openningTime = time;
                        //semiDeployAGL = altATGL;
                        //semiDeployASL = altASL;
                        //targetASLAtSemiDeploy = endASL;
                    }

                    break;
                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    if (BelowDeployAltitude(altATGL, altASL) && time - openningTime >= semiDeployDuration)
                    {
                        state = ModuleParachute.deploymentStates.DEPLOYED;
                        openningTime = time;
                        //fullDeployAGL = altATGL;
                        //fullDeployASL = altASL;
                        //targetASLAtFullDeploy = endASL;
                    }

                    break;
            }

            // Now that we have potentially changed states calculate the current drag or the parachute in whatever state (or transition to a state) that it is in.
            float normalizedTime;
            // Depending on the state that we are in consider if we are part way through a deployment.
            if (state == ModuleParachute.deploymentStates.SEMIDEPLOYED)
            {
                normalizedTime = (float)Math.Min((time - openningTime) / semiDeployDuration, 1);
            }
            else if (state == ModuleParachute.deploymentStates.DEPLOYED)
            {
                normalizedTime = (float)Math.Min((time - openningTime) / fullDeployDuration, 1);
            }
            else
            {
                normalizedTime = 1;
            }

            // Are we deploying in any way? We know if we are deploying or not if normalized time is less than 1
            if (normalizedTime < 1)
            {
                deploying = true;
            }
            else
            {
                deploying = false;
            }

            // If we are deploying or semi deploying then use Lerp to replicate the way the game increases the drag as we deploy.
            if (deploying && (state == ModuleParachute.deploymentStates.SEMIDEPLOYED || state == ModuleParachute.deploymentStates.DEPLOYED))
            {
                deployLevel = Mathf.Pow(normalizedTime, para.deploymentCurve);
            }
            else
            {
                deployLevel = 1;
            }

            switch (state)
            {
                case ModuleParachute.deploymentStates.STOWED:
                case ModuleParachute.deploymentStates.ACTIVE:
                case ModuleParachute.deploymentStates.CUT:
                    SetCubeWeight("PACKED", 1f);
                    SetCubeWeight("SEMIDEPLOYED", 0f);
                    SetCubeWeight("DEPLOYED", 0f);
                    break;

                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    SetCubeWeight("PACKED", 1f - deployLevel);
                    SetCubeWeight("SEMIDEPLOYED", deployLevel);
                    SetCubeWeight("DEPLOYED", 0f);
                    ApplyCubeWeightsWithoutOcclusion();
                    break;

                case ModuleParachute.deploymentStates.DEPLOYED:
                    SetCubeWeight("PACKED", 0f);
                    SetCubeWeight("SEMIDEPLOYED", 1f - deployLevel);
                    SetCubeWeight("DEPLOYED", deployLevel);
                    ApplyCubeWeightsWithoutOcclusion();
                    break;
            }

            return deploying;
        }
    }
}
