/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

namespace MechJebLib.FuelFlowSimulation
{
    public struct SimResource
    {
        public  bool   Free;
        public  double MaxAmount;
        private double _amount;

        public double Amount
        {
            get => _amount + _rcsAmount;
            set => _amount = value;
        }

        private double _rcsAmount;
        public  int    Id;
        public  double Density;
        public  double Residual;

        public double ResidualThreshold => Residual * MaxAmount;

        public SimResource Drain(double resourceDrain)
        {
            _amount -= resourceDrain;
            if (_amount < 0)
                _amount = 0;

            return this;
        }

        public SimResource RCSDrain(double rcsDrain)
        {
            _rcsAmount -= rcsDrain;
            if (Amount < 0)
                _rcsAmount = -_amount;

            return this;
        }

        public SimResource ResetRCS()
        {
            _rcsAmount = 0;
            return this;
        }
    }
}
