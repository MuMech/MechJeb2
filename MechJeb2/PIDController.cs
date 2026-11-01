using System;
using MechJebLib.Control;

namespace MuMech
{
    // Parallel-form gains and ConfigNode persistence for PIDLoop2s that run on the physics tick.
    public abstract class PIDControllerBase : IConfigNode
    {
        public double Kp, Ki, Kd;
        public double MinIntegral = double.MinValue;
        public double MaxIntegral = double.MaxValue;
        private readonly double _max;
        private readonly double _min;

        protected PIDControllerBase(double kp, double ki, double kd, double max, double min)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            _max = max;
            _min = min;
        }

        protected void Configure(PIDLoop2 loop)
        {
            loop.Ts = TimeWarp.fixedDeltaTime;
            // Kp first: the Ki and Kd setters scale by the current K
            loop.Kp = Kp;
            loop.Ki = Ki;
            loop.Kd = Kd;
            // with Tf = Ts/2 the Tustin derivative is the plain difference Kd * (e - e1) / Ts, and it doesn't ring
            loop.Tf = 0.5 * loop.Ts;
            loop.MinOutput = _min;
            loop.MaxOutput = _max;
            loop.MinIntegral = MinIntegral;
            loop.MaxIntegral = MaxIntegral;
            loop.ConditionalIntegration = true;
        }

        public void Load(ConfigNode node)
        {
            if (node.HasValue("Kp"))
                Kp = Convert.ToDouble(node.GetValue("Kp"));

            if (node.HasValue("Ki"))
                Ki = Convert.ToDouble(node.GetValue("Ki"));

            if (node.HasValue("Kd"))
                Kd = Convert.ToDouble(node.GetValue("Kd"));
        }

        public void Save(ConfigNode node)
        {
            node.SetValue("Kp", Kp.ToString());
            node.SetValue("Ki", Ki.ToString());
            node.SetValue("Kd", Kd.ToString());
        }
    }

    public class PIDController : PIDControllerBase
    {
        private readonly PIDLoop2 _loop = new PIDLoop2();

        public PIDController(double kp = 0, double ki = 0, double kd = 0, double max = double.MaxValue, double min = double.MinValue)
            : base(kp, ki, kd, max, min)
        {
        }

        // the integral's share of the output
        public double ITerm => _loop.ITerm;

        public double Compute(double error)
        {
            Configure(_loop);
            return _loop.Update(error, 0);
        }

        public void Reset() => _loop.Reset();
    }

    // Only P and I: this drives velocities, and for those the plant is a single integrator.
    public class PIDControllerV2 : PIDControllerBase
    {
        private readonly PIDLoop2[] _loops = { new PIDLoop2(), new PIDLoop2(), new PIDLoop2() };

        public PIDControllerV2(double kp = 0, double ki = 0, double max = double.MaxValue, double min = double.MinValue)
            : base(kp, ki, 0, max, min)
        {
        }

        public Vector3d Compute(Vector3d error)
        {
            var action = new Vector3d();

            for (int i = 0; i < 3; i++)
            {
                Configure(_loops[i]);
                action[i] = _loops[i].Update(error[i], 0);
            }

            return action;
        }

        public void Reset()
        {
            foreach (PIDLoop2 loop in _loops)
                loop.Reset();
        }
    }
}
