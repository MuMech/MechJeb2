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

#region Using Directives

using System.Text;

using UnityEngine;

#endregion

namespace KerbalEngineer.VesselSimulator
{
    public class Stage
    {
        public double actualThrust = 0.0;
        public double actualThrustToWeight = 0.0;
        public double cost = 0.0;
        public double deltaV = 0.0;
        public double inverseTotalDeltaV = 0.0;
        public double isp = 0.0;
        public double mass = 0.0;
        public double rcsMass = 0.0;
        public double maxThrustToWeight = 0.0;
        public int number = 0;
        public double thrust = 0.0;
        public double thrustToWeight = 0.0;
        public double time = 0.0;
        public double totalCost = 0.0;
        public double totalDeltaV = 0.0;
        public double totalMass = 0.0;
        public double totalTime = 0.0;
        public int totalPartCount = 0;
        public int partCount = 0;
        public double resourceMass = 0.0;
        public double maxThrustTorque = 0.0;
        public double thrustOffsetAngle = 0.0;
        public float maxMach = 0.0f;

        public void Dump()
        {
            StringBuilder str = new StringBuilder("", 512);
            str.AppendFormat("number        : {0:d}\n", this.number);
            str.AppendFormat("cost          : {0:g6}\n", this.cost);
            str.AppendFormat("totalCost     : {0:g6}\n", this.totalCost);
            str.AppendFormat("time          : {0:g6}\n", this.time);
            str.AppendFormat("totalTime     : {0:g6}\n", this.totalTime);
            str.AppendFormat("mass          : {0:g6}\n", this.mass);
            str.AppendFormat("totalMass     : {0:g6}\n", this.totalMass);
            str.AppendFormat("resourceMass  : {0:g6}\n", this.resourceMass);
            str.AppendFormat("isp           : {0:g6}\n", this.isp);
            str.AppendFormat("thrust        : {0:g6}\n", this.thrust);
            str.AppendFormat("actualThrust  : {0:g6}\n", this.actualThrust);
            str.AppendFormat("thrustToWeight: {0:g6}\n", this.thrustToWeight);
            str.AppendFormat("maxTWR        : {0:g6}\n", this.maxThrustToWeight);
            str.AppendFormat("actualTWR     : {0:g6}\n", this.actualThrustToWeight);
            str.AppendFormat("ThrustTorque  : {0:g6}\n", this.maxThrustTorque);
            str.AppendFormat("ThrustOffset  : {0:g6}\n", this.thrustOffsetAngle);
            str.AppendFormat("deltaV        : {0:g6}\n", this.deltaV);
            str.AppendFormat("totalDeltaV   : {0:g6}\n", this.totalDeltaV);
            str.AppendFormat("invTotDeltaV  : {0:g6}\n", this.inverseTotalDeltaV);

            MonoBehaviour.print(str);
        }
    }
}