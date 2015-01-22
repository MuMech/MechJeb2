using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class EngineWrapper
    {
        public readonly PartModule engine;

        public float minThrust
        {
            get
            {
                if (engine is ModuleEngines)
                    return (engine as ModuleEngines).minThrust;
                else if (engine is ModuleEnginesFX)
                    return (engine as ModuleEnginesFX).minThrust;
                else
                    return 0;
            }
        }

        public float maxThrust
        {
            get
            {
                if (engine is ModuleEngines)
                    return (engine as ModuleEngines).maxThrust;
                else if (engine is ModuleEnginesFX)
                    return (engine as ModuleEnginesFX).maxThrust;
                else
                    return 0;
            }
        }

        public float thrustRatio
        {
            get
            {
                if (engine is ModuleEngines)
                    return (engine as ModuleEngines).thrustPercentage / 100;
                else if (engine is ModuleEnginesFX)
                    return (engine as ModuleEnginesFX).thrustPercentage / 100;
                else
                    return 0;
            }
            set
            {
                if (engine is ModuleEngines)
                    (engine as ModuleEngines).thrustPercentage = value * 100;
                else if (engine is ModuleEnginesFX)
                    (engine as ModuleEnginesFX).thrustPercentage = value * 100;
            }
        }

        public bool throttleLocked
        {
            get
            {
                if (engine is ModuleEngines)
                    return (engine as ModuleEngines).throttleLocked;
                else if (engine is ModuleEnginesFX)
                    return (engine as ModuleEnginesFX).throttleLocked;
                else
                    return true;
            }
        }

        public bool ignited
        {
            get
            {
                if (engine is ModuleEngines)
                    return (engine as ModuleEngines).getIgnitionState;
                else if (engine is ModuleEnginesFX)
                    return (engine as ModuleEnginesFX).getIgnitionState;
                else
                    return true;
            }
        }

        public bool enabled
        {
            get
            {
                return engine.isEnabled;
            }
        }

        public bool flameout
        {
            get
            {
                if (engine is ModuleEngines)
                    return (engine as ModuleEngines).getFlameoutState;
                else if (engine is ModuleEnginesFX)
                    return (engine as ModuleEnginesFX).getFlameoutState;
                else
                    return false;
            }
        }

        public bool useVelocityCurve
        {
            get
            {
                if (engine is ModuleEngines)
                    return (engine as ModuleEngines).useVelocityCurve;
                else if (engine is ModuleEnginesFX)
                    return (engine as ModuleEnginesFX).useVelocityCurve;
                else
                    return false;
            }
        }

        public FloatCurve velocityCurve
        {
            get
            {
                if (engine is ModuleEngines)
                    return (engine as ModuleEngines).velocityCurve;
                else if (engine is ModuleEnginesFX)
                    return (engine as ModuleEnginesFX).velocityCurve;
                else
                    return null;
            }
        }

        public List<Transform> thrustTransforms
        {
            get
            {
                if (engine is ModuleEngines)
                    return (engine as ModuleEngines).thrustTransforms;
                else if (engine is ModuleEnginesFX)
                    return (engine as ModuleEnginesFX).thrustTransforms;
                else
                    return null;
            }
        }

        private Vector3d _constantForce;
        private Vector3d _maxVariableForce;
        private Vector3d _constantTorque;
        private Vector3d _maxVariableTorque;

        public Vector3d constantForce { get { return _constantForce; } }
        public Vector3d maxVariableForce { get { return _maxVariableForce; } }
        public Vector3d constantTorque { get { return _constantTorque; } }
        public Vector3d maxVariableTorque { get { return _maxVariableTorque; } }

        public void UpdateForceAndTorque(Vector3d com)
        {
            PartModule gimbal;
            VesselState.GimbalExt gimbalExt = VesselState.getGimbalExt(engine.part, out gimbal);
            Vessel v = engine.vessel;

            float currentMaxThrust, currentMinThrust;

            currentMaxThrust = maxThrust / (float)(thrustTransforms.Count);
            currentMinThrust = minThrust / (float)(thrustTransforms.Count);
            if (useVelocityCurve)
            {
                float lambda = velocityCurve.Evaluate((float)(v.orbit.GetVel() - v.mainBody.getRFrmVel(v.CoM)).magnitude);
                currentMaxThrust *= lambda;
                currentMinThrust *= lambda;
            }

            if (throttleLocked)
            {
                currentMaxThrust *= thrustRatio;
                currentMinThrust = currentMaxThrust;
            }

            for (int i = 0; i < thrustTransforms.Count; i++)
            {
                Vector3d thrust_dir = v.transform.rotation.Inverse() * gimbalExt.initialRot(gimbal, thrustTransforms[i], i) * Vector3d.back;

                Vector3d pos = v.transform.rotation.Inverse() * (engine.part.transform.position - com);

                _maxVariableForce += (currentMaxThrust - currentMinThrust) * thrust_dir;
                _constantForce += currentMinThrust * thrust_dir;
                _maxVariableTorque += (currentMaxThrust - currentMinThrust) * Vector3d.Cross(pos, thrust_dir);
                _constantTorque += currentMinThrust * Vector3d.Cross(pos, thrust_dir);
            }
        }

        public EngineWrapper(PartModule module)
        {
            if (!(module is ModuleEngines) && !(module is ModuleEnginesFX))
                throw new ArgumentException("module must be a ModuleEngines or ModuleEnginesFX");

            engine = module;
        }
    }
}

