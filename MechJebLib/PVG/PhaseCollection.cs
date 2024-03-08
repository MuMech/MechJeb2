using System;
using System.Collections.Generic;
using System.Text;

namespace MechJebLib.PVG
{
    public class PhaseCollection : List<Phase>, IDisposable
    {
        public PhaseCollection()
        {
        }

        public PhaseCollection(IEnumerable<Phase> phases)
        {
            foreach (Phase phase in phases)
                Add(phase);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (Phase phase in this)
                sb.AppendLine(phase.ToString());

            return sb.ToString();
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
