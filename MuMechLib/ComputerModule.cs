using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace MuMech
{
    public class ComputerModule : IComparable<ComputerModule>
    {
        public Part part = null;
        public MechJebCore core = null;
        public VesselState vesselState = null;

        //conveniences:
        public Vessel vessel { get { return part.vessel; } }
        public CelestialBody mainBody { get { return part.vessel.mainBody; } }
        public Orbit orbit { get { return part.vessel.orbit; } }

        public int priority = 0;

        public int CompareTo(ComputerModule other)
        {
            if (other == null) return 1;
            return priority.CompareTo(other.priority);
        }

        protected bool _enabled = false;
        public bool enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (value != _enabled)
                {
                    //core.settingsChanged = true;
                    _enabled = value;
                    if (_enabled)
                    {
                        OnModuleEnabled();
                    }
                    else
                    {
                        OnModuleDisabled();
                    }
                }
            }
        }

        public ComputerModule(MechJebCore core)
        {
            this.core = core;
            part = core.part;
            vesselState = core.vesselState;
        }

        public virtual void OnModuleEnabled()
        {
        }

        public virtual void OnModuleDisabled()
        {
        }

        public virtual void OnControlLost()
        {
        }

        public virtual void Drive(FlightCtrlState s)
        {
        }
        
        public virtual void OnStart(PartModule.StartState state)
        {
        }

        public virtual void OnActive()
        {
        }

        public virtual void OnInactive()
        {
        }

        public virtual void OnAwake()
        {
        }

        public virtual void OnFixedUpdate()
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
        }

        public virtual void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            Type thisType = this.GetType();
            foreach (FieldInfo f in thisType.GetFields())
            {
                foreach(MechJebField attr in f.GetCustomAttributes(typeof(MechJebField), true))
                {
                    string settingsName = thisType.Name + "." + f.Name;
                    if ((attr.type & PersistenceType.GLOBAL) != 0) global.AddValue(settingsName, f.GetValue(this));
                    if ((attr.type & PersistenceType.LOCAL) != 0) local.AddValue(settingsName, f.GetValue(this));
                    if ((attr.type & PersistenceType.TYPE) != 0) type.AddValue(settingsName, f.GetValue(this));
                }
            }
        }

        public virtual void OnDestroy()
        {
        }

        protected void print(String s)
        {
            MonoBehaviour.print(s);
        }
    }

    public enum PersistenceType { LOCAL = 1, TYPE = 2, GLOBAL = 4 }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class MechJebField : Attribute
    {
        public PersistenceType type;

        public MechJebField(PersistenceType type) { this.type = type; }
    }
}
