#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.PVG
{
    public class ArrayWrapper : IDisposable
    {
        private static readonly ObjectPool<ArrayWrapper> _pool = new ObjectPool<ArrayWrapper>(New);

        private IList<double> _list;

        public const int ARRAY_WRAPPER_LEN = 15;

        private ArrayWrapper()
        {
            _list = null!;
        }

        private static ArrayWrapper New()
        {
            return new ArrayWrapper();
        }

        public double Rx
        {
            get => this[0];
            set => this[0] = value;
        }

        public double Ry
        {
            get => this[1];
            set => this[1] = value;
        }

        public double Rz
        {
            get => this[2];
            set => this[2] = value;
        }

        public V3 R
        {
            get => new V3(Rx, Ry, Rz);
            set
            {
                Rx = value[0];
                Ry = value[1];
                Rz = value[2];
            }
        }

        public double Vx
        {
            get => this[3];
            set => this[3] = value;
        }

        public double Vy
        {
            get => this[4];
            set => this[4] = value;
        }

        public double Vz
        {
            get => this[5];
            set => this[5] = value;
        }

        public V3 V
        {
            get => new V3(Vx, Vy, Vz);
            set
            {
                Vx = value[0];
                Vy = value[1];
                Vz = value[2];
            }
        }

        public double PVx
        {
            get => this[6];
            set => this[6] = value;
        }

        public double PVy
        {
            get => this[7];
            set => this[7] = value;
        }

        public double PVz
        {
            get => this[8];
            set => this[8] = value;
        }

        public V3 PV
        {
            get => new V3(PVx, PVy, PVz);
            set
            {
                PVx = value[0];
                PVy = value[1];
                PVz = value[2];
            }
        }

        public double PRx
        {
            get => this[9];
            set => this[9] = value;
        }

        public double PRy
        {
            get => this[10];
            set => this[10] = value;
        }

        public double PRz
        {
            get => this[11];
            set => this[11] = value;
        }

        public V3 PR
        {
            get => new V3(this[9], this[10], this[11]);
            set
            {
                this[9]  = value[0];
                this[10] = value[1];
                this[11] = value[2];
            }
        }

        public double CostateMagnitude =>
            Math.Sqrt(PRx * PRx + PRy * PRy + PRz * PRz + PVx * PVx + PVy * PVy + PVz * PVz);

        public double M
        {
            get => this[12];
            set => this[12] = value;
        }

        public double Pm
        {
            get => this[13];
            set => this[13] = value;
        }

        public double H0
        {
            get
            {
                double rm = R.magnitude;
                return V3.Dot(PR, V) -
                       V3.Dot(PV, R) / (rm * rm * rm); // FIXME: this changes for linear gravity model??!?!?
            }
        }

        public double Bt
        {
            get => this[14];
            set => this[14] = value;
        }

        public double DV
        {
            get => this[14];
            set => this[14] = value;
        }

        public double this[int i]
        {
            get => _list[i];
            set => _list[i] = value;
        }

        public void CopyTo(IList<double> other)
        {
            for (int i = 0; i < ARRAY_WRAPPER_LEN; i++)
                other[i] = this[i];
        }

        public static ArrayWrapper Rent(IList<double> list)
        {
            Check.True(list.Count >= ARRAY_WRAPPER_LEN);

            ArrayWrapper wrapper = _pool.Get();
            wrapper._list = list;
            return wrapper;
        }

        public void Dispose()
        {
            _pool.Return(this);
        }
    }
}
