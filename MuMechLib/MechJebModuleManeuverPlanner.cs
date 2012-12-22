using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MuMech
{
    class MechJebModuleManeuverPlanner : DisplayModule
    {
        public MechJebModuleManeuverPlanner(MechJebCore core) : base(core) { }

        //input dV should be in world coordinates
        public void PlaceManeuverNode(Orbit o, Vector3d dV, double UT)
        {
            //convert a dV in world coordinates into the coordinate system of the maneuver node,
            //where (x, y, z) are (radial+, normal+, prograde)
            Vector3d nodeDV = new Vector3d(Vector3d.Dot(o.RadialPlus(UT), dV),
                                           Vector3d.Dot(o.NormalPlus(UT), dV),
                                           Vector3d.Dot(o.Prograde(UT), dV));
            ManeuverNode mn = part.vessel.patchedConicSolver.AddManeuverNode(UT);
            mn.OnGizmoUpdated(nodeDV, UT);
        }


    }
}
