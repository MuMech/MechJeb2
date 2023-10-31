using System;
using JetBrains.Annotations;

namespace MuMech.AttitudeControllers
{
    public class KosPIDLoop
    {
        public  double Kp { get; set; }
        private double _ki;
        private double _loopKi;

        public double Ki
        {
            get => _ki;
            set
            {
                _ki     = value;
                _loopKi = value;
            }
        }

        [UsedImplicitly]
        public double Kd { get; set; }

        [UsedImplicitly]
        public double Input { get; set; }

        [UsedImplicitly]
        public double Setpoint { get; set; }

        [UsedImplicitly]
        public double Error { get; set; }

        [UsedImplicitly]
        public double Output { get; set; }

        [UsedImplicitly]
        public double MinOutput { get; set; }

        [UsedImplicitly]
        public double MaxOutput { get; set; }

        [UsedImplicitly]
        public double ErrorSum { get; set; }

        [UsedImplicitly]
        public double PTerm { get; set; }

        [UsedImplicitly]
        public double ITerm { get; set; }

        [UsedImplicitly]
        public double DTerm { get; set; }

        [UsedImplicitly]
        public bool ExtraUnwind { get; set; }

        [UsedImplicitly]
        public double ChangeRate { get; set; }

        [UsedImplicitly]
        public bool UnWinding { get; set; }

        public KosPIDLoop(double maxoutput = double.MaxValue, double minoutput = double.MinValue, bool extraUnwind = false)
            : this(1.0, 0, 0, maxoutput, minoutput, extraUnwind)
        {
        }

        public KosPIDLoop(double kp, double ki, double kd, double maxoutput = double.MaxValue, double minoutput = double.MinValue,
            bool extraUnwind = false)
        {
            Kp          = kp;
            Ki          = ki;
            Kd          = kd;
            Input       = 0;
            Setpoint    = 0;
            Error       = 0;
            Output      = 0;
            MaxOutput   = maxoutput;
            MinOutput   = minoutput;
            ErrorSum    = 0;
            PTerm       = 0;
            ITerm       = 0;
            DTerm       = 0;
            ExtraUnwind = extraUnwind;
        }

        [UsedImplicitly]
        public double Update(double input, double setpoint, double minOutput, double maxOutput)
        {
            MaxOutput = maxOutput;
            MinOutput = minOutput;
            Setpoint  = setpoint;
            return Update(input);
        }

        public double Update(double input, double setpoint, double maxOutput) => Update(input, setpoint, -maxOutput, maxOutput);

        [UsedImplicitly]
        public double Update(double input)
        {
            double error = Setpoint - input;
            double pTerm = error * Kp;
            double iTerm = 0;
            double dTerm = 0;
            double dt = TimeWarp.fixedDeltaTime;
            if (_loopKi != 0)
            {
                if (ExtraUnwind)
                {
                    if (Math.Sign(error) != Math.Sign(ErrorSum))
                    {
                        if (!UnWinding)
                        {
                            _loopKi   *= 2;
                            UnWinding =  true;
                        }
                    }
                    else if (UnWinding)
                    {
                        _loopKi   = _ki;
                        UnWinding = false;
                    }
                }

                iTerm = ITerm + error * dt * _loopKi;
            }

            ChangeRate = (input - Input) / dt;
            if (Kd != 0)
            {
                dTerm = -ChangeRate * Kd;
            }

            Output = pTerm + iTerm + dTerm;
            if (Output > MaxOutput)
            {
                Output = MaxOutput;
                if (_loopKi != 0)
                {
                    iTerm = Output - Math.Min(pTerm + dTerm, MaxOutput);
                }
            }

            if (Output < MinOutput)
            {
                Output = MinOutput;
                if (_loopKi != 0)
                {
                    iTerm = Output - Math.Max(pTerm + dTerm, MinOutput);
                }
            }

            Input = input;
            Error = error;
            PTerm = pTerm;
            ITerm = iTerm;
            DTerm = dTerm;
            if (_loopKi != 0)
                ErrorSum = iTerm / _loopKi;
            else
                ErrorSum = 0;
            return Output;
        }

        public void ResetI()
        {
            ErrorSum = 0;
            ITerm    = 0;
        }
    }
}
