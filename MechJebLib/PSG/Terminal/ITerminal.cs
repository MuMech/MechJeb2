/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;

namespace MechJebLib.PSG.Terminal
{
    public interface ITerminal
    {
        int NumConstraints { get; }

        void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci);

        ITerminal Rescale(Scale scale);

        double TargetOrbitalEnergy();

        double IncT();

        ITerminal GetFPA();

        bool IsFPA();
    }
}
