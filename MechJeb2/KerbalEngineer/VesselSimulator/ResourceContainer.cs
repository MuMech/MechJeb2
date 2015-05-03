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

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#endregion

namespace KerbalEngineer.VesselSimulator
{
    public class ResourceContainer
    {
        private Hashtable resources = new Hashtable();

        public double this[int type]
        {
            get
            {
                if (this.resources.ContainsKey(type))
                {
                    return (double)this.resources[type];
                }

                return 0d;
            }
            set
            {
                if (this.resources.ContainsKey(type))
                {
                    this.resources[type] = value;
                }
                else
                {
                    this.resources.Add(type, value);
                }
            }
        }

        public List<int> Types
        {
            get
            {
                List<int> types = new List<int>();

                foreach (int key in this.resources.Keys)
                {
                    types.Add(key);
                }

                return types;
            }
        }

        public double Mass
        {
            get
            {
                double mass = 0d;

                foreach (double resource in this.resources.Values)
                {
                    mass += resource;
                }

                return mass;
            }
        }

        public bool Empty
        {
            get
            {
                foreach (int type in this.resources.Keys)
                {
                    if ((double)this.resources[type] > SimManager.RESOURCE_MIN)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool HasType(int type)
        {
            return this.resources.ContainsKey(type);
        }

        public bool EmptyOf(HashSet<int> types)
        {
            foreach (int type in types)
            {
                if (this.HasType(type) && (double)this.resources[type] > SimManager.RESOURCE_MIN)
                {
                    return false;
                }
            }

            return true;
        }

        public void Add(int type, double amount)
        {
            if (this.resources.ContainsKey(type))
            {
                this.resources[type] = (double)this.resources[type] + amount;
            }
            else
            {
                this.resources.Add(type, amount);
            }
        }

        public void Reset()
        {
            this.resources.Clear();
        }

        public void Debug()
        {
            foreach (int key in this.resources.Keys)
            {
                MonoBehaviour.print(" -> " + GetResourceName(key) + " = " + this.resources[key]);
            }
        }

        public double GetResourceMass(int type)
        {
            double density = GetResourceDensity(type);
            return density == 0d ? 0d : (double)this.resources[type] * density;
        }

        public static ResourceFlowMode GetResourceFlowMode(int type)
        {
            return PartResourceLibrary.Instance.GetDefinition(type).resourceFlowMode;
        }

        public static ResourceTransferMode GetResourceTransferMode(int type)
        {
            return PartResourceLibrary.Instance.GetDefinition(type).resourceTransferMode;
        }

        public static float GetResourceDensity(int type)
        {
            return PartResourceLibrary.Instance.GetDefinition(type).density;
        }

        public static string GetResourceName(int type)
        {
            return PartResourceLibrary.Instance.GetDefinition(type).name;
        }
    }
}