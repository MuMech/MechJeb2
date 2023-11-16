using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.PVG;

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

            public SuicideBuilder AddStageUsingFinalMass(double m0, double mf, double isp, double bt, int kspStage, int mjPhase)
            {
                _phases.Add(Phase.NewStageUsingFinalMass(m0, mf, isp, bt, kspStage, mjPhase));

                return this;
            }

            public SuicideBuilder SetGround(double height) => this;
        }
    }
}
