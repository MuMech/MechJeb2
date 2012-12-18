using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebCore : PartModule, IComparable<MechJebCore>
    {
        private const int windowIDbase = 60606;

        private List<ComputerModule> computerModules = new List<ComputerModule>();
        private List<DisplayModule> displayModules = new List<DisplayModule>();
        private bool modulesUpdated = false;

        public MechJebModuleAttitudeController attitude;
        public MechJebModuleStagingController staging;
        public MechJebModuleThrustController thrust;
        public MechJebModuleWarpController warp;

        public VesselState vesselState = new VesselState();

        private Vessel controlledVessel; //keep track of which vessel we've added our onFlyByWire callback to 

        public int GetImportance()
        {
            if (part.State == PartStates.DEAD)
            {
                return 0;
            }
            else
            {
                return GetInstanceID();
            }
        }

        public int CompareTo(MechJebCore other)
        {
            if (other == null) return 1;
            return GetImportance().CompareTo(other.GetImportance());
        }

        public T GetComputerModule<T>() where T : ComputerModule
        {
            return (T)computerModules.First(a => a.GetType() == typeof(T));
        }

        public T GetDisplayModule<T>() where T : DisplayModule
        {
            return (T)displayModules.First(a => a.GetType() == typeof(T));
        }

        public ComputerModule GetComputerModule(string type)
        {
            return computerModules.First(a => a.GetType().Name.ToLowerInvariant() == type.ToLowerInvariant());
        }

        public DisplayModule GetDisplayModule(string type)
        {
            return displayModules.First(a => a.GetType().Name.ToLowerInvariant() == type.ToLowerInvariant());
        }

        public void AddComputerModule(ComputerModule module)
        {
            computerModules.Add(module);
            modulesUpdated = true;
        }

        public void AddDisplayModule(DisplayModule module)
        {
            displayModules.Add(module);
            modulesUpdated = true;
        }

        public override void OnStart(PartModule.StartState state)
        {
            AddComputerModule(attitude = new MechJebModuleAttitudeController(this));
            AddComputerModule(thrust = new MechJebModuleThrustController(this));
            AddComputerModule(staging = new MechJebModuleStagingController(this));
            AddComputerModule(warp = new MechJebModuleWarpController(this));

            AddComputerModule(new MechJebModuleAscentComputer(this));
            
            foreach (ComputerModule module in computerModules)
            {
                module.OnStart(state);
            }

            attitude.enabled = true; //for testing

            part.vessel.OnFlyByWire += drive;
            controlledVessel = part.vessel;
        }

        public override void OnActive()
        {
            foreach (ComputerModule module in computerModules)
            {
                module.OnActive();
            }
        }
        
        public override void OnInactive()
        {
            foreach (ComputerModule module in computerModules)
            {
                module.OnInactive();
            }
        }

        public override void OnAwake()
        {
            foreach (ComputerModule module in computerModules)
            {
                module.OnAwake();
            }
        }

        public void FixedUpdate()
        {
            if (this != vessel.GetMasterMechJeb())
            {
                return;
            }

            vesselState.Update(part.vessel);

            if (modulesUpdated)
            {
                computerModules.Sort();
                modulesUpdated = false;
            }

            foreach (ComputerModule module in computerModules)
            {
                module.OnFixedUpdate();
            }
        }

        public void Update()
        {
            if (this != vessel.GetMasterMechJeb())
            {
                return;
            }

            if (modulesUpdated)
            {
                computerModules.Sort();
                modulesUpdated = false;
            }

            if (Input.GetKey(KeyCode.Y))
            {
                print("prograde");
                attitude.attitudeTo(Vector3.forward, MechJebModuleAttitudeController.AttitudeReference.ORBIT, null);
            }
            if (Input.GetKey(KeyCode.U))
            {
                print("rad+");
                attitude.attitudeTo(Vector3.up, MechJebModuleAttitudeController.AttitudeReference.ORBIT, null);
            }
            if (Input.GetKey(KeyCode.B)) 
            {
                print("nml+");
                attitude.attitudeTo(Vector3.left, MechJebModuleAttitudeController.AttitudeReference.ORBIT, null);
            }

            foreach (ComputerModule module in computerModules)
            {
                module.OnUpdate();
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            print("MechJebCore.OnLoad");
            base.OnLoad(node); //is this necessary?
            foreach (ComputerModule module in computerModules)
            {
                module.OnLoad(node);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            print("MechJebCore.OnSave");
            base.OnSave(node); //is this necessary?
            foreach (ComputerModule module in computerModules)
            {
                module.OnSave(node);
            }
        }

        public void OnDestroy()
        {
            print("MechJebCore.OnDestroy");
            foreach (ComputerModule module in computerModules)
            {
                module.OnDestroy();
            }

            vessel.OnFlyByWire -= onFlyByWire;
            controlledVessel = null;
        }

        private void onFlyByWire(FlightCtrlState s)
        {
            //handle vessel changes due to docking/undocking
            if (controlledVessel != part.vessel)
            {
                if (controlledVessel != null) controlledVessel.OnFlyByWire -= onFlyByWire;
                part.vessel.OnFlyByWire += onFlyByWire;
                controlledVessel = part.vessel;
                return;
            }

            if (this != vessel.GetMasterMechJeb())
            {
                return;
            }

            drive(s);

            if (vessel == FlightGlobals.ActiveVessel)
            {
                FlightInputHandler.state.mainThrottle = s.mainThrottle; //so that the on-screen throttle gauge reflects the autopilot throttle
            }
        }

        private void drive(FlightCtrlState s)
        {
            //do we need to do something to prevent conflicts here?
            foreach (ComputerModule module in computerModules)
            {
                if (module.enabled) module.drive(s);
            }
        }

        private void OnGUI()
        {
            if ((FlightGlobals.ready) && (vessel == FlightGlobals.ActiveVessel) && (part.State != PartStates.DEAD) && (this == vessel.GetMasterMechJeb()))
            {
                int wid = 0;
                foreach (DisplayModule module in displayModules)
                {
                    if (module.enabled) module.drawGUI(windowIDbase + wid);
                    wid++;
                }
            }
        }

        // VAB/SPH description
        public override string GetInfo()
        {
            return "Attitude control by MechJeb™";
        }
    }
}
