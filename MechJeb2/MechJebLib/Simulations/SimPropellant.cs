#nullable enable

using MechJebLib.Simulations.PartModules;

namespace MechJebLib.Simulations
{
    public readonly struct SimPropellant
    {
        public readonly int         id;
        public readonly bool        ignoreForIsp;
        public readonly double      ratio;
        public readonly SimFlowMode FlowMode;
        public readonly double      density;

        public SimPropellant(int id, bool ignoreForIsp, double ratio, SimFlowMode flowMode, double density)
        {
            this.id           = id;
            this.ignoreForIsp = ignoreForIsp;
            this.ratio        = ratio;
            FlowMode          = flowMode;
            this.density      = density;
        }
    }
}
