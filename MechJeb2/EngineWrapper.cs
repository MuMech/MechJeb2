using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
#warning not really needed anymore. REMOVE

    
    public class EngineWrapper
    {
        public readonly ModuleEngines engine;

        public float thrustRatio
        {
            get
            {
                return engine.thrustPercentage / 100;
            }
            set
            {
                engine.thrustPercentage = value * 100;
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

            currentMaxThrust = engine.maxFuelFlow * engine.flowMultiplier * engine.realIsp * engine.g / engine.thrustTransforms.Count;
            currentMinThrust = engine.minFuelFlow * engine.flowMultiplier * engine.realIsp * engine.g / engine.thrustTransforms.Count;

            if (engine.throttleLocked)
            {
                currentMaxThrust *= thrustRatio;
                currentMinThrust = currentMaxThrust;
            }

            for (int i = 0; i < engine.thrustTransforms.Count; i++)
            {
                Vector3d thrust_dir = v.ReferenceTransform.rotation.Inverse() * gimbalExt.initialRot(gimbal, engine.thrustTransforms[i], i) * Vector3d.back;
                Vector3d pos = v.ReferenceTransform.rotation.Inverse() * (engine.part.transform.position - com);

                _maxVariableForce += (currentMaxThrust - currentMinThrust) * thrust_dir;
                _constantForce += currentMinThrust * thrust_dir;
                _maxVariableTorque += (currentMaxThrust - currentMinThrust) * Vector3d.Cross(pos, thrust_dir);
                _constantTorque += currentMinThrust * Vector3d.Cross(pos, thrust_dir);
            }
        }

        public EngineWrapper(ModuleEngines module)
        {
            engine = module;
        }
    }
 
}

