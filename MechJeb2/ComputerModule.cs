extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
    public class ComputerModule : IComparable<ComputerModule>
    {
        public readonly MechJebCore Core;

        //conveniences:
        public Vessel        Vessel      => Part.vessel;
        public CelestialBody MainBody    => Part.vessel.mainBody;
        public VesselState   VesselState => Core.VesselState;

        [UsedImplicitly]
        public Part Part => Core.part;

        [UsedImplicitly]
        public Orbit Orbit => Part.vessel.orbit;

        [UsedImplicitly]
        public int Priority;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        public readonly string UnlockParts = "";

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        public readonly string UnlockTechs = "";

        public bool UnlockChecked;

        public int CompareTo(ComputerModule other) => other == null ? 1 : Priority.CompareTo(other.Priority);

        private bool _enabled;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (value == _enabled) return;

                Dirty    = true;
                _enabled = value;

                if (_enabled)
                    OnModuleEnabled();
                else
                {
                    OnModuleDisabled();
                    ModuleDisabledEvents.Fire(true);
                    ModuleDisabledEvents.Clear();
                }
            }
        }

        [UsedImplicitly]
        public readonly ModuleEvent ModuleDisabledEvents = new ModuleEvent();

        public readonly string ProfilerName;

        // Has this module config changed and should it be saved
        public bool Dirty;

        //The UserPool is an alternative way to handle enabling/disabling of a ComputerModule.
        //Users can add and remove themselves from the user pool and the ComputerModule will be
        //enabled if and only if there is at least one user. For consistency, it's probably
        //best that a given ComputerModule be controlled either entirely through enabled, or
        //entirely through users, and that the two not be mixed.
        public readonly UserPool Users;

        protected ComputerModule(MechJebCore core)
        {
            Core         = core;
            ProfilerName = GetType().Name;

            Users = new UserPool(this);
        }

        protected virtual void OnModuleEnabled()
        {
        }

        protected virtual void OnModuleDisabled()
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

        public virtual void OnUpdate()
        {
        }

        public virtual void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            try
            {
                if (global != null) ConfigNode.LoadObjectFromConfig(this, global, (int)Pass.GLOBAL);
                if (type != null) ConfigNode.LoadObjectFromConfig(this, type, (int)Pass.TYPE);
                if (local != null) ConfigNode.LoadObjectFromConfig(this, local, (int)Pass.LOCAL);
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
                    ConfigNode.CreateConfigFromObject(this, (int)Pass.GLOBAL, global);
                    Profiler.EndSample();
                }

                if (type != null)
                {
                    Profiler.BeginSample("ComputerModule.OnSave.type");
                    ConfigNode.CreateConfigFromObject(this, (int)Pass.TYPE, type);
                    Profiler.EndSample();
                }

                if (local != null)
                {
                    Profiler.BeginSample("ComputerModule.OnSave.local");
                    ConfigNode.CreateConfigFromObject(this, (int)Pass.LOCAL, local);
                    Profiler.EndSample();
                }

                Dirty = false;
            }
            catch (Exception e)
            {
                Debug.Log("MechJeb caught exception in OnSave for " + GetType().Name + ": " + e);
            }
        }

        public virtual void OnDestroy()
        {
        }

        protected virtual bool IsSpaceCenterUpgradeUnlocked() => true;

        public virtual void UnlockCheck()
        {
            if (UnlockChecked) return;

            bool unlock = true;

            if (ResearchAndDevelopment.Instance != null)
            {
                string[] parts = UnlockParts.Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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

                string[] techs = UnlockTechs.Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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

            UnlockChecked = true;
            if (!unlock)
            {
                Enabled                  = false;
                Core.someModuleAreLocked = true;
            }
        }

        protected static void Print(object message) => MonoBehaviour.print("[MechJeb2] " + message);

        [UsedImplicitly]
        public void Disable() => Enabled = false;

        [UsedImplicitly]
        public void Enable() => Enabled = true;

        public void CascadeDisable(ComputerModule m) => ModuleDisabledEvents.Add(m.Disable);
    }

    [Flags]
    public enum Pass
    {
        LOCAL  = 1,
        TYPE   = 2,
        GLOBAL = 4
    }

    public class ModuleEvent
    {
        public delegate void OnEvent();

        private readonly List<OnEvent>            _events     = new List<OnEvent>();
        private readonly Dictionary<OnEvent, int> _eventIndex = new Dictionary<OnEvent, int>();

        public void Add(OnEvent evt)
        {
            if (_eventIndex.ContainsKey(evt))
                return;

            _events.Add(evt);
            _eventIndex.Add(evt, _events.Count - 1);
        }

        [UsedImplicitly]
        public void Remove(OnEvent evt)
        {
            if (!_eventIndex.ContainsKey(evt))
                return;

            _events.RemoveAt(_eventIndex[evt]);
            _eventIndex.Remove(evt);
        }

        public void Clear()
        {
            _events.Clear();
            _eventIndex.Clear();
        }

        public void Fire(bool reverse)
        {
            if (reverse)
            {
                for (int i = _events.Count - 1; i >= 0; i--)
                    _events[i]();
            }
            else
            {
                for (int i = 0; i < _events.Count; i++)
                    _events[i]();
            }
        }
    }

    //Lets multiple users enable and disable a computer module, such that the
    //module only gets disabled when all of its users have disabled it.
    public class UserPool : List<object>
    {
        private readonly ComputerModule _controlledModule;

        public UserPool(ComputerModule controlledModule)
        {
            _controlledModule = controlledModule;
        }

        public new void Add(object user)
        {
            if (user != null && !Contains(user))
                base.Add(user);

            _controlledModule.Enabled = true;
        }

        public new void Remove(object user)
        {
            if (user != null && Contains(user))
                base.Remove(user);

            if (Count == 0) _controlledModule.Enabled = false;
        }

        public new void Clear()
        {
            base.Clear();
            _controlledModule.Enabled = false;
        }

        public bool RecursiveUser(object user)
        {
            if (Contains(user))
                return true;

            foreach (object o in this)
            {
                if (!(o is ComputerModule c) || c == _controlledModule)
                    continue;

                if (c.Users.RecursiveUser(user))
                    return true;
            }

            return false;
        }
    }
}
