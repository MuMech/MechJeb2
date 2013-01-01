using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MuMech
{
    public static class VesselExtensions
    {
        public static List<T> GetParts<T>(this Vessel vessel) where T : Part
        {
            return vessel.Parts.FindAll(a => a.GetType() == typeof(T)).Cast<T>().ToList<T>();
        }

        public static List<T> GetModules<T>(this Vessel vessel) where T : PartModule
        {
            return (from modules in vessel.Parts from module in modules.Modules.OfType<T>() select module).Cast<T>().ToList<T>();
        }

        public static MechJebCore GetMasterMechJeb(this Vessel vessel)
        {
            return vessel.GetModules<MechJebCore>().Max();
        }

        public static bool LiftedOff(this Vessel vessel)
        {
            return vessel.situation != Vessel.Situations.PRELAUNCH;
        }


        public static Orbit GetPatchAtUT(this Vessel vessel, double UT)
        {
            IEnumerable<ManeuverNode> earlierNodes = vessel.patchedConicSolver.maneuverNodes.Where(n => n.UT < UT);
            Orbit o = vessel.orbit;
            if (earlierNodes.Count() > 0)
            {
                o = earlierNodes.OrderByDescending(n => n.UT).First().nextPatch;
            }
            while (o.nextPatch != null && o.nextPatch.activePatch && o.nextPatch.StartUT < UT)
            {
                o = o.nextPatch;
            }
            return o;
        }


        //input dV should be in world coordinates
        public static void PlaceManeuverNode(this Vessel vessel, Orbit patch, Vector3d dV, double UT)
        {
            //convert a dV in world coordinates into the coordinate system of the maneuver node,
            //which uses (x, y, z) = (radial+, normal-, prograde)
            Vector3d nodeDV = new Vector3d(Vector3d.Dot(patch.RadialPlus(UT), dV),
                                           Vector3d.Dot(-patch.NormalPlus(UT), dV),
                                           Vector3d.Dot(patch.Prograde(UT), dV));
            ManeuverNode mn = vessel.patchedConicSolver.AddManeuverNode(UT);
            mn.OnGizmoUpdated(nodeDV, UT);
        }
    }
}
