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

        public void FixLastShutdownStage()
        {
            int lastShutdownStage = -1;
            for (int i = Count - 1; i >= 0; i--)
            {
                if (!this[i].AllowShutdown || this[i].Coast)
                    continue;

                lastShutdownStage = i;
                break;
            }

            if (lastShutdownStage < 0)
                return;

            Phase phase = this[lastShutdownStage];
            phase.MaxT              = 0.999 * phase.Tau;
            this[lastShutdownStage] = phase;
        }
    }
}
