/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System.Buffers;

namespace MechJebLib.Utils
{
    public static class Utils
    {
        // we need a thread-safe pool of doubles
        public static readonly ArrayPool<double> DoublePool = ArrayPool<double>.Shared;
    }
}
