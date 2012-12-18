using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class ComputerModule : IComparable<ComputerModule>
    {
        public Part part = null;
        public MechJebCore core = null;
        public VesselState vesselState = null;

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
                        onModuleEnabled();
                    }
                    else
                    {
                        onModuleDisabled();
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

        public virtual void onModuleEnabled()
        {
        }

        public virtual void onModuleDisabled()
        {
        }

        public virtual void onControlLost()
        {
        }

        public virtual void drive(FlightCtrlState s)
        {
        }

        /*
                public virtual void onLoadGlobalSettings(SettingsManager settings)
                {
                    windowPos = new Rect(settings["windowPos_" + GetType().Name].value_vector.x, settings["windowPos_" + GetType().Name].value_vector.y, 10, 10);
                    enabled = settings["windowStat_" + GetType().Name].value_bool;
                }

                public virtual void onSaveGlobalSettings(SettingsManager settings)
                {
                    settings["windowPos_" + GetType().Name].value_vector = new Vector4(windowPos.x, windowPos.y);
                    settings["windowStat_" + GetType().Name].value_bool = enabled;
                }
         */

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

        public virtual void OnLoad(ConfigNode node)
        {
        }

        public virtual void OnSave(ConfigNode node)
        {
        }

        public virtual void OnDestroy()
        {
        }

        /*        public virtual void registerLuaMembers(LuaTable index)
                {
                }*/

        protected void print(String s)
        {
            MonoBehaviour.print(s);
        }
    }
}
