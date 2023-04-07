using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    public class ComputerModule : IComparable<ComputerModule>
    {
        public Part        part;
        public MechJebCore core;
        public VesselState vesselState;

        //conveniences:
        public Vessel        vessel   => part.vessel;
        public CelestialBody mainBody => part.vessel.mainBody;
        public Orbit         orbit    => part.vessel.orbit;

        public int priority;

        [Persistent(pass = (int)Pass.Local)]
        public string unlockParts = "";

        [Persistent(pass = (int)Pass.Local)]
        public string unlockTechs = "";

        public bool unlockChecked;

        public int CompareTo(ComputerModule other)
        {
            if (other == null) return 1;
            return priority.CompareTo(other.priority);
        }

        protected bool _enabled;

        public bool enabled
        {
            get => _enabled;
            set
            {
                if (value != _enabled)
                {
                    dirty    = true;
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

        public string profilerName;

        // Has this module config changed and should it be saved
        public bool dirty;

        //The UserPool is an alternative way to handle enabling/disabling of a ComputerModule.
        //Users can add and remove themselves from the user pool and the ComputerModule will be
        //enabled if and only if there is at least one user. For consistency, it's probably
        //best that a given ComputerModule be controlled either entirely through enabled, or
        //entirely through users, and that the two not be mixed.
        public UserPool users;

        public ComputerModule(MechJebCore core)
        {
            this.core    = core;
            part         = core.part;
            vesselState  = core.vesselState;
            profilerName = GetType().Name;

            users = new UserPool(this);
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

        public virtual void OnVesselWasModified(Vessel v)
        {
        }

        public virtual void OnVesselStandardModification(Vessel v)
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

        public virtual void OnWaitForFixedUpdate()
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            try
            {
                if (global != null) ConfigNode.LoadObjectFromConfig(this, global, (int)Pass.Global);
                if (type != null) ConfigNode.LoadObjectFromConfig(this, type, (int)Pass.Type);
                if (local != null) ConfigNode.LoadObjectFromConfig(this, local, (int)Pass.Local);
            }
            catch (Exception e)
            {
                Debug.Log("MechJeb caught exception in OnLoad for " + GetType().Name + ": " + e);
            }
        }

        public virtual void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            try
            {
                if (global != null)
                {
                    Profiler.BeginSample("ComputerModule.OnSave.global");
                    ConfigNode.CreateConfigFromObject(this, (int)Pass.Global, global);
                    Profiler.EndSample();
                }

                if (type != null)
                {
                    Profiler.BeginSample("ComputerModule.OnSave.type");
                    ConfigNode.CreateConfigFromObject(this, (int)Pass.Type, type);
                    Profiler.EndSample();
                }

                if (local != null)
                {
                    Profiler.BeginSample("ComputerModule.OnSave.local");
                    ConfigNode.CreateConfigFromObject(this, (int)Pass.Local, local);
                    Profiler.EndSample();
                }

                dirty = false;
            }
            catch (Exception e)
            {
                Debug.Log("MechJeb caught exception in OnSave for " + GetType().Name + ": " + e);
            }
        }

        public virtual void OnDestroy()
        {
        }

        public virtual bool IsSpaceCenterUpgradeUnlocked()
        {
            return true;
        }

        public virtual void UnlockCheck()
        {
            if (!unlockChecked)
            {
                bool unlock = true;

                if (ResearchAndDevelopment.Instance != null)
                {
                    string[] parts = unlockParts.Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        unlock = false;
                        foreach (string p in parts)
                        {
                            if (PartLoader.LoadedPartsList.Count(a => a.name == p) > 0 &&
                                ResearchAndDevelopment.PartModelPurchased(PartLoader.LoadedPartsList.First(a => a.name == p)))
                            {
                                unlock = true;
                                break;
                            }
                        }
                    }

                    string[] techs = unlockTechs.Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (techs.Length > 0)
                    {
                        if (parts.Length == 0)
                        {
                            unlock = false;
                        }

                        foreach (string t in techs)
                        {
                            if (ResearchAndDevelopment.GetTechnologyState(t) == RDTech.State.Available)
                            {
                                unlock = true;
                                break;
                            }
                        }
                    }
                }

                unlock = unlock && IsSpaceCenterUpgradeUnlocked();

                unlockChecked = true;
                if (!unlock)
                {
                    enabled                  = false;
                    core.someModuleAreLocked = true;
                }
            }
        }

        public static void print(object message)
        {
            MonoBehaviour.print("[MechJeb2] " + message);
        }
    }

    [Flags]
    public enum Pass
    {
        Local  = 1,
        Type   = 2,
        Global = 4
    }

    //Lets multiple users enable and disable a computer module, such that the
    //module only gets disabled when all of its users have disabled it.
    public class UserPool : List<object>
    {
        private readonly ComputerModule controlledModule;

        public UserPool(ComputerModule controlledModule)
        {
            this.controlledModule = controlledModule;
        }

        public new void Add(object user)
        {
            if (user != null && !Contains(user))
            {
                base.Add(user);
            }

            controlledModule.enabled = true;
        }

        public new void Remove(object user)
        {
            if (user != null && Contains(user))
            {
                base.Remove(user);
            }

            if (Count == 0) controlledModule.enabled = false;
        }

        public new void Clear()
        {
            base.Clear();
            controlledModule.enabled = false;
        }

        public bool RecursiveUser(object user)
        {
            if (Contains(user))
            {
                return true;
            }

            foreach (object o in this)
            {
                var c = o as ComputerModule;
                if (c != null && c != controlledModule)
                {
                    if (c.users.RecursiveUser(user)) return true;
                }
            }

            return false;
        }
    }
}
