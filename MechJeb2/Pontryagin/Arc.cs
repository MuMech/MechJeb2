using System.Collections.Generic;
using System.Text;

namespace MuMech
{
    public class ArcList : List<Arc>
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (Arc arc in this)
            {
                sb.AppendLine(arc.ToString());
            }

            return sb.ToString();
        }
    }
    
    public class Arc
    {
        private readonly PontryaginBase                          p;
        private readonly MechJebModuleLogicalStageTracking.Stage stage;
        public           double                                  dV; /* actual integrated time of the burn */

        // values copied from the stage (important to copy them since the stage values will change
        // in the main thread).

        public double Isp     { get; private set; }
        public double Thrust  { get; private set; }
        public double M0      { get; private set; }
        public double AvailDV { get; private set; }
        public double MaxBt   { get; private set; }

        public double c { get; private set; }

        public double MaxBtBar  => MaxBt / p.t_scale;
        public int    ksp_stage { get; private set; }

        public int rocket_stage { get; private set; }

        public double _synch_time;

        public bool complete_burn;
        public bool _done;

        public bool done
        {
            get => stage != null && stage.Staged || _done;
            set => _done = value;
        }

        public bool   coast;
        public bool   coast_after_jettison;
        public bool   use_fixed_time;  // confusingly, this is fixed end-time for terminal coasts to rendezvous
        public bool   use_fixed_time2; // this is a fixed time segment, but the constant appears in the y0 vector with a trivial constraint
        public double fixed_time;
        public double fixed_tbar;

        // zero mdot, infinite burntime+isp
        public bool infinite = false;

        public override string ToString()
        {
            return "ksp_stage:" + ksp_stage + " rocket_stage:" + rocket_stage + " isp:" + Isp + " thrust:" + Thrust + " c:" + c + " m0:" + M0 +
                   " maxt:" + MaxBt + " maxtbar:" + MaxBtBar + " avail ∆v:" + AvailDV + " used ∆v:" + dV + (done ? " (done)" : "") +
                   (infinite ? " (infinite) " : "");
        }

        // create a local copy of the information for the optimizer
        public void UpdateStageInfo(double t0)
        {
            if (stage != null)
            {
                Isp          = stage.Isp;
                Thrust       = stage.EffectiveThrust; // use the effective thrust to deal with ullage motors
                M0           = stage.StartMass;
                AvailDV      = stage.DeltaV;
                MaxBt        = stage.DeltaTime;
                c            = stage.Ve / p.t_scale;
                ksp_stage    = stage.KspStage;
                rocket_stage = stage.RocketStage;
            }
            else
            {
                Isp          = 0;
                Thrust       = 0;
                M0           = -1;
                AvailDV      = 0;
                MaxBt        = 0;
                c            = 0;
                ksp_stage    = -1;
                rocket_stage = -1;
            }

            _synch_time = t0;
        }

        public Arc(PontryaginBase p, double t0, MechJebModuleLogicalStageTracking.Stage stage = null, bool done = false,
            bool coast_after_jettison = false, bool use_fixed_time = false, double fixed_time = 0, bool coast = false)
        {
            this.p                    = p;
            this.stage                = stage;
            this.done                 = done;
            this.coast_after_jettison = coast_after_jettison;
            this.use_fixed_time       = use_fixed_time;
            this.fixed_time           = fixed_time;
            this.coast                = coast;
            UpdateStageInfo(t0);
        }
    }
}