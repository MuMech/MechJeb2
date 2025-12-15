using System.Collections.Generic;

namespace MechJebLib.PSG
{
    public class PhaseCollection : List<Phase>
    {
        public PhaseCollection DeepCopy()
        {
            var dup = new PhaseCollection();

            foreach (Phase phase in this)
                dup.Add(phase.DeepCopy());

            return dup;
        }
    }
}
