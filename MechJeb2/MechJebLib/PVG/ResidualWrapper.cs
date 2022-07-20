#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.PVG
{
    public class ResidualWrapper : IDisposable
    {
        private static readonly ObjectPool<ResidualWrapper> _pool = new ObjectPool<ResidualWrapper>(New, Clear);

        private IList<double> _list;

        public const int RESIDUAL_WRAPPER_LEN = 15;

        private ResidualWrapper()
        {
            _list = null!;
        }

        private static ResidualWrapper New()
        {
            return new ResidualWrapper();
        }

        // r0 constraint
        public V3 R
        {
            set
            {
                this[0] = value[0];
                this[1] = value[1];
                this[2] = value[2];
            }
        }

        // v0 constraint
        public V3 V
        {
            set
            {
                this[3] = value[0];
                this[4] = value[1];
                this[5] = value[2];
            }
        }

        // m0 constraint
        public double M
        {
            set => this[6] = value;
        }

        public (double a, double b, double c, double d, double e, double f) Terminal
        {
            set
            {
                this[7]  = value.a;
                this[8]  = value.b;
                this[9]  = value.c;
                this[10] = value.d;
                this[11] = value.e;
                this[12] = value.f;
            }
        }

        public double Bt
        {
            set => this[13] = value;
        }

        public double Pm_transversality
        {
            set => this[14] = value;
        }

        public V3 RContinuity
        {
            set
            {
                this[0] = value[0];
                this[1] = value[1];
                this[2] = value[2];
            }
        }

        public V3 VContinuity
        {
            set
            {
                this[3] = value[0];
                this[4] = value[1];
                this[5] = value[2];
            }
        }

        public V3 PvContinuity
        {
            set
            {
                this[6] = value[0];
                this[7] = value[1];
                this[8] = value[2];
            }
        }

        public V3 PrContinuity
        {
            set
            {
                this[9]  = value[0];
                this[10] = value[1];
                this[11] = value[2];
            }
        }

        public double M_continuity
        {
            set => this[12] = value;
        }

        public double Pm_continuity
        {
            set => this[14] = value;
        }

        public double this[int i]
        {
            get => _list[i];
            set => _list[i] = value;
        }

        public void CopyTo(IList<double> other)
        {
            for (int i = 0; i < RESIDUAL_WRAPPER_LEN; i++)
                other[i] = this[i];
        }

        public static ResidualWrapper Rent(IList<double> list)
        {
            Check.True(list.Count >= RESIDUAL_WRAPPER_LEN);

            ResidualWrapper wrapper = _pool.Get();
            wrapper._list = list;
            return wrapper;
        }

        public void Dispose()
        {
            _pool.Return(this);
        }
        
        public static void Clear(ResidualWrapper obj)
        {
            
        }
    }
}
