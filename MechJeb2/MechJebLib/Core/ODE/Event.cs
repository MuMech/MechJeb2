/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Copyright Sebastien Gaggini (sebastien.gaggini@gmail.com)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System.Collections.Generic;

namespace MechJebLib.Maths.ODE
{
    public abstract class Event
    {
        public virtual  bool   Stop    { get; set; } = true;
        public          bool   Enabled { get; set; } = true;
        public abstract double Evaluate(IList<double> y, double x);
    }
}
