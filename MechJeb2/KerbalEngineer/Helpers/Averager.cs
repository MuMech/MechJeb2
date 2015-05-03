// 
//     Kerbal Engineer Redux
// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;

namespace KerbalEngineer
{ 
    public class VectorAverager
    {
        private Vector3d sum = Vector3d.zero;
        private uint count = 0;

        public void Add(Vector3d v) {
            sum += v;
            count += 1;
        }

        public Vector3d Get() {
            if (count > 0) {
                return sum / count;
            } else {
                return Vector3d.zero;
            }
        }

        public void Reset()
        {
            sum = Vector3d.zero;
            count = 0;
        }
    }

    public class WeightedVectorAverager
    {
        private Vector3d sum = Vector3d.zero;
        private double totalweight = 0;

        public void Add(Vector3d v, double weight) {
            sum += v * weight;
            totalweight += weight;
        }

        public Vector3d Get() {
            if (totalweight > 0) {
                return sum / totalweight;
            } else {
                return Vector3d.zero;
            }
        }

        public double GetTotalWeight() {
            return totalweight;
        }

        public void Reset()
        {
            sum = Vector3d.zero;
            totalweight = 0.0;
        }
    }
}

