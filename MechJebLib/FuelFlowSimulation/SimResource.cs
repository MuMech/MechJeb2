/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using static System.Math;

namespace MechJebLib.FuelFlowSimulation
{
    public struct SimResource
    {
        public bool Free;
        public double MaxAmount;
        private double _amount;

        public double Amount
        {
            get => _amount + _rcsAmount;
            set => _amount = value;
        }

        private double _rcsAmount;
        public int Id;
        public double Density;
        public double Residual;

        public double ResidualThreshold => Residual * MaxAmount;

        public SimResource Drain(double resourceDrain)
        {
            _amount -= resourceDrain;
            if (_amount < ResidualThreshold)
                _amount = ResidualThreshold;

            return this;
        }

        public SimResource RCSDrain(double rcsDrain)
        {
            // the RCS drain registration only screens on ResourceRequestRemainingThreshold, so a resource that is already
            // below its residual can reach here -- clamp to the residual without ever handing any propellant back
            double floor = Min(Amount, ResidualThreshold);

            _rcsAmount -= rcsDrain;

            if (Amount < floor)
                _rcsAmount = floor - _amount;

            return this;
        }

        public SimResource ResetRCS()
        {
            _rcsAmount = 0;
            return this;
        }
    }
}
