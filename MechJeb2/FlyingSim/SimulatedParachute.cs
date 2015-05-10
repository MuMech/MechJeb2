using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class SimulatedParachute : SimulatedPart
    {
        ModuleParachute para;

        ModuleParachute.deploymentStates state;

        double openningTime;

        // Store some data about when and how the parachute opened during the simulation which will be useful for debugging
        double activatedASL = 0;
        double activatedAGL = 0;
        double semiDeployASL = 0;
        double semiDeployAGL = 0;
        double fullDeployASL = 0;
        double fullDeployAGL = 0;
        double targetASLAtSemiDeploy = 0;
        double targetASLAtFullDeploy = 0;
        float deployLevel = 0;
        public bool deploying = false;

        private bool willDeploy = false;

        new static public SimulatedParachute New(ModuleParachute mp, ReentrySimulation.SimCurves simCurve, double startTime, int limitChutesStage)
        {
            SimulatedParachute part = new SimulatedParachute();
            part.Set(mp.part, simCurve);
            part.Set(mp, startTime, limitChutesStage);
            return part;
        }

        public void Set(ModuleParachute mp, double startTime, int limitChutesStage)
        {
            this.para = mp;
            this.state = mp.deploymentState;

            willDeploy = limitChutesStage != -1 && para.part.inverseStage >= limitChutesStage;

            // Work out when the chute was put into its current state based on the current drag as compared to the stowed, semi deployed and fully deployed drag

            double timeSinceDeployment = 0;
            
            switch (mp.deploymentState)
            {
                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    if (mp.Anim.isPlaying)
                        timeSinceDeployment = mp.Anim[mp.semiDeployedAnimation].time;
                    else
                        timeSinceDeployment = 10000000;
                    break;

                case ModuleParachute.deploymentStates.DEPLOYED:
                    if (mp.Anim.isPlaying)
                        timeSinceDeployment = mp.Anim[mp.fullyDeployedAnimation].time;
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

            this.openningTime = startTime - timeSinceDeployment;

            //Debug.Log("Parachute " + para.name + " parachuteDrag:" + this.parachuteDrag + " stowedDrag:" + para.stowedDrag + " semiDeployedDrag:" + para.semiDeployedDrag + " fullyDeployedDrag:" + para.fullyDeployedDrag + " part.maximum_drag:" + para.part.maximum_drag + " part.minimum_drag:" + para.part.minimum_drag + " semiDeploymentSpeed:" + para.semiDeploymentSpeed + " deploymentSpeed:" + para.deploymentSpeed + " deploymentState:" + para.deploymentState + " timeSinceDeployment:" + timeSinceDeployment);
            // Keep that test code until they fix the bug in the new parachute module
            //if ((realDrag / parachuteDrag) > 1.01d || (realDrag / parachuteDrag) < 0.99d)
            //    Debug.Log("Parachute " + para.name + " parachuteDrag:" + this.parachuteDrag.ToString("F3") + " RealDrag:" + realDrag.ToString("F3") + " MinDrag:" + para.part.minimum_drag.ToString("F3") + " MaxDrag:" + para.part.maximum_drag.ToString("F3"));
        }


        public override Vector3 Drag(Vector3 vesselVelocity, float dynamicPressurekPa, float mach)
        {
            if (state != ModuleParachute.deploymentStates.SEMIDEPLOYED && state != ModuleParachute.deploymentStates.DEPLOYED)
                return base.Drag(vesselVelocity, dynamicPressurekPa, mach);

            return Vector3.zero;

        }


        // Consider activating, semi deploying or deploying a parachute, but do not actually make any changes. returns true if the state has changed
        public override bool SimulateAndRollback(double altATGL, double altASL, double endASL, double pressure, double time, double semiDeployMultiplier)
        {
            if (!willDeploy)
                return false;

            bool stateChanged = false;
            switch (state)
            {
                case ModuleParachute.deploymentStates.STOWED:
                    if (altATGL < semiDeployMultiplier * para.deployAltitude)
                    {
                        stateChanged = true;
                    }
                    break;
                case ModuleParachute.deploymentStates.ACTIVE:
                    if (pressure >= para.minAirPressureToOpen)
                    {
                        stateChanged = true;
                    }
                    break;
                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    if (altATGL < para.deployAltitude)
                    {
                        stateChanged = true;
                    }
                    break;
            }
            return (stateChanged);
        }

        // Consider activating, semi deploying or deploying a parachute. returns true if the state has changed
        public override bool Simulate(double altATGL, double altASL, double endASL, double pressure, double time, double semiDeployMultiplier)
        {
            if (!willDeploy)
                return false;

            switch (state)
            {
                case ModuleParachute.deploymentStates.STOWED:
                    if (altATGL < semiDeployMultiplier * para.deployAltitude)
                    {
                        state = ModuleParachute.deploymentStates.ACTIVE;
                        activatedAGL = altATGL;
                        activatedASL = altASL;
                        // Immediately check to see if the parachute should be semi deployed, rather than waiting for another iteration.
                        if (pressure >= para.minAirPressureToOpen)
                        {
                            state = ModuleParachute.deploymentStates.SEMIDEPLOYED;
                            openningTime = time;
                            semiDeployAGL = altATGL;
                            semiDeployASL = altASL;
                            targetASLAtSemiDeploy = endASL;
                        }
                    }
                    break;
                case ModuleParachute.deploymentStates.ACTIVE:
                    if (pressure >= para.minAirPressureToOpen)
                    {
                        state = ModuleParachute.deploymentStates.SEMIDEPLOYED;
                        openningTime = time;
                        semiDeployAGL = altATGL;
                        semiDeployASL = altASL;
                        targetASLAtSemiDeploy = endASL;
                    }

                    break;
                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    if (altATGL < para.deployAltitude)
                    {
                        state = ModuleParachute.deploymentStates.DEPLOYED;
                        openningTime = time;
                        fullDeployAGL = altATGL;
                        fullDeployASL = altASL;
                        targetASLAtFullDeploy = endASL;
                    }
                    break;
            }

            // Now that we have potentially changed states calculate the current drag or the parachute in whatever state (or transition to a state) that it is in.
            float normalizedTime;

            // Depending on the state that we are in consider if we are part way through a deployment.
            if (state == ModuleParachute.deploymentStates.SEMIDEPLOYED)
            {
                normalizedTime = (float)Math.Min((time - openningTime) / para.semiDeploymentSpeed, 1);
            }
            else if (state == ModuleParachute.deploymentStates.DEPLOYED)
            {
                normalizedTime = (float)Math.Min((time - openningTime) / para.deploymentSpeed, 1);
            }
            else
            {
                normalizedTime = 1;
            }

            // Are we deploying in any way? We know if we are deploying or not if normalized time is less than 1
            if (normalizedTime < 1)
            {
                this.deploying = true;
            }
            else
            {
                this.deploying = false;
            }

            // If we are deploying or semi deploying then use Lerp to replicate the way the game increases the drag as we deploy.
            if (deploying && (state == ModuleParachute.deploymentStates.SEMIDEPLOYED || state == ModuleParachute.deploymentStates.DEPLOYED))
            {
                this.deployLevel = Mathf.Pow(normalizedTime, para.deploymentCurve);
            }
            else
            {
                this.deployLevel = 1;
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
                    break;

                case ModuleParachute.deploymentStates.DEPLOYED:
                    SetCubeWeight("PACKED", 0f);
                    SetCubeWeight("SEMIDEPLOYED", 1f - deployLevel);
                    SetCubeWeight("DEPLOYED", deployLevel);
                    break;
            }

            return deploying;

        }
    }
}
