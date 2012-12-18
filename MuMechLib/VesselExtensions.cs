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
    }
}
