using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.PSG;

namespace MechJebLib.SuicideBurnSimulation
{
    public partial class Suicide
    {
        public class SuicideBuilder
        {
            private readonly List<Phase> _phases = new List<Phase>();

            public Suicide Build()
            {
                var suicide = new Suicide();

                return suicide;
            }

            public SuicideBuilder Initial(V3 r0, V3 v0, V3 u0, double t0, double mu, double rbody) => this;

            public SuicideBuilder AddStage(double m0, double mf, double thrust, double isp, int kspStage, int mjPhase)
            {
                _phases.Add(Phase.NewStage(m0, mf, thrust, isp, kspStage, mjPhase));

                return this;
            }

            public SuicideBuilder SetGround(double height) => this;
        }
    }
}
