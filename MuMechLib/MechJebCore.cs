using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebCore : PartModule
    {
        List<ComputerModule> computerModules = new List<ComputerModule>();
        List<DisplayModule> displayModules = new List<DisplayModule>();
        public AttitudeController attitude;
        public WarpController warp;
        public ThrustController thrust;
        public StagingController staging;

        public VesselState vesselState = new VesselState();

        private static int windowIDbase = 60606;


        public override void OnStart(PartModule.StartState state)
        {
            print("MechJebCore.OnStart");
            part.force_activate(); //part needs to be activated for OnFixedUpdate to get called. but maybe we should just use FixedUpdate instead?

            //computer modules
            computerModules.Add(new MechJebModuleAscentComputer(this));
            
            attitude = new AttitudeController(this);
            warp = new WarpController(this);
            thrust = new ThrustController(this);
            staging = new StagingController(this);
            computerModules.Add(warp);
            computerModules.Add(attitude);
            computerModules.Add(thrust);
            computerModules.Add(staging);

            foreach (ComputerModule module in computerModules)
            {
                module.OnStart(state);
            }

            attitude.enabled = true; //attitude controller should always be enabled

            //still need the logic that handles vessel changes and multiple MechJebs:
            part.vessel.OnFlyByWire += drive;
            RenderingManager.AddToPostDrawQueue(0, drawGUI);
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

        public override void OnFixedUpdate()
        {
            vesselState.Update(part.vessel);

            foreach (ComputerModule module in computerModules)
            {
                module.OnFixedUpdate();
            }
        }

        public override void OnUpdate()
        {
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
            RenderingManager.RemoveFromPostDrawQueue(0, drawGUI);
        }


        private void onFlyByWire(FlightCtrlState s)
        {
            //still need logic to handle vessel changes and multiple MechJebs
            if (part.vessel != FlightGlobals.ActiveVessel)
            {
                return;
            }
            drive(s);
            FlightInputHandler.state.mainThrottle = s.mainThrottle; //so that the on-screen throttle gauge reflects the autopilot throttle
        }


        private void drive(FlightCtrlState s)
        {
            //do we need to do something to prevent conflicts here?
            foreach (ComputerModule module in computerModules)
            {
                if (module.enabled) module.drive(s);
            }
        }

        private void drawGUI()
        {
            if ((part.State != PartStates.DEAD) && (part.vessel == FlightGlobals.ActiveVessel))
            {
                int wid = 0;
                foreach (DisplayModule module in displayModules)
                {
                    if (module.enabled) module.drawGUI(windowIDbase + wid);
                    wid++;
                }
            }
        }

        //what is this for?
        public override string GetInfo()
        {
            return "[" + base.GetInfo() + "] - MechJebCore.GetInfo()";
        }
    }
}
