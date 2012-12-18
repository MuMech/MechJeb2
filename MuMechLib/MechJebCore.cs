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

        public AttitudeController attitude;
        public StagingController staging;
        public ThrustController thrust;
        public WarpController warp;

        public VesselState vesselState = new VesselState();

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
            print("MechJebCore.OnStart");

            AddComputerModule(attitude = new AttitudeController(this));
            AddComputerModule(thrust = new ThrustController(this));
            AddComputerModule(staging = new StagingController(this));
            AddComputerModule(warp = new WarpController(this));

            //computer modules
            AddComputerModule(new MechJebModuleAscentComputer(this));
            
            foreach (ComputerModule module in computerModules)
            {
                module.OnStart(state);
            }

            attitude.enabled = true; //attitude controller should always be enabled

            //still need the logic that handles vessel changes and multiple MechJebs:
            part.vessel.OnFlyByWire += drive;
        }

        public override void OnActive()
        {
            print("MechJebCore.OnActive");
            foreach (ComputerModule module in computerModules)
            {
                module.OnActive();
            }
        }
        
        public override void OnInactive()
        {
            print("MechJebCore.OnInactive");
            foreach (ComputerModule module in computerModules)
            {
                module.OnInactive();
            }
        }

        public override void OnAwake()
        {
            print("MechJebCore.OnAwake");
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
                attitude.attitudeTo(Vector3.forward, AttitudeController.AttitudeReference.ORBIT, null);
            }
            if (Input.GetKey(KeyCode.U))
            {
                print("rad+");
                attitude.attitudeTo(Vector3.up, AttitudeController.AttitudeReference.ORBIT, null);
            }
            if (Input.GetKey(KeyCode.B)) 
            {
                print("nml+");
                attitude.attitudeTo(Vector3.left, AttitudeController.AttitudeReference.ORBIT, null);
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

            //still need logic to handle vessel changes and multiple MechJebs
            vessel.OnFlyByWire -= onFlyByWire;
        }

        private void onFlyByWire(FlightCtrlState s)
        {
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
