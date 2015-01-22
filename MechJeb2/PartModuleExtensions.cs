using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public static class PartModuleExtensions
    {

        public static Vector3d getLiftVector(this ModuleControlSurface cs, Vector3d velocity)
        {            
            Vector3d AoA = cs.part.transform.up * Mathf.Cos(Vector3.Angle(velocity, cs.part.transform.forward) * Mathf.PI / 180f);
            Vector3d liftDir = Vector3.Cross(velocity, cs.part.transform.right) * Vector3.Dot(AoA, cs.part.transform.up);
            return liftDir * cs.deflectionLiftCoeff * (float)FlightGlobals.getStaticPressure(cs.part.transform.position);            
        }
    }
}
