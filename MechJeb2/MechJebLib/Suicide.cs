using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MuMech.MechJebLib
{
    public partial class Suicide : BackgroundJob<Suicide.SuicideResult>
    {
        public struct SuicideResult
        {
            public V3     Rf;
            public double Tf;
        }

        public static SuicideBuilder Builder()
        {
            return new SuicideBuilder();
        }

        protected override SuicideResult Run(object o)
        {
            throw new NotImplementedException();
        }
    }
}
