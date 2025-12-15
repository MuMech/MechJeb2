using System.Collections.Generic;
using MechJebLib.PSG.Terminal;

namespace MechJebLib.PSG
{
    public class VariableProxy
    {
        private readonly List<PhaseProxy> _proxies = new List<PhaseProxy>();

        public readonly int TotalVariables;
        public readonly int TotalConstraints;

        public VariableProxy(PhaseCollection phases, ITerminal terminal, int n)
        {
            int idx  = 0;
            int cons = 0;

            for (int p = 0; p < phases.Count; p++)
            {
                var proxy = new PhaseProxy(n, idx, p, phases[p]);
                _proxies.Add(proxy);
                idx  += proxy.NumVars;
                cons += proxy.NumConstraints;
            }

            cons += terminal.NumConstraints; // terminal constraints

            cons += 0;

            TotalVariables   = idx;
            TotalConstraints = cons;
        }

        public PhaseProxy this[int index]
        {
            get
            {
                if (index < 0)
                    return _proxies[_proxies.Count + index];
                return _proxies[index];
            }
        }

        public void WrapVars(double[] vars)
        {
            foreach (PhaseProxy proxy in _proxies)
                proxy.WrapVars(vars);
        }
    }
}
