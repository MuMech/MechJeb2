using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.SuicideBurnSimulation
{
    public partial class Suicide : BackgroundJob<Suicide.SuicideResult>
    {
        public struct SuicideResult
        {
            public V3     Rf;
            public double Tf;
        }

        public static SuicideBuilder Builder() => new SuicideBuilder();

        protected override SuicideResult Run(object o) => throw new NotImplementedException();
    }
}
