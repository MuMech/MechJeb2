using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MuMech
{
    public static class PartExtensions
    {
        public static float totalMass(this Part p)
        {
            return p.mass + p.GetResourceMass();
        }


        public static bool engineHasFuel(this Part p)
        {
            if (p is LiquidEngine || p is LiquidFuelEngine || p is AtmosphericEngine)
            {
                //I don't really know the details of how you're supposed to use RequestFuel, but this seems to work to
                //test whether something can get fuel.
                return p.RequestFuel(p, 0, Part.getFuelReqId());
            }
            else if (p.Modules.OfType<ModuleEngines>().Count() > 0)
            {
                return !p.Modules.OfType<ModuleEngines>().First().getFlameoutState;
            }
            else return false;
        }

    }
}
