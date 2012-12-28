using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace MuMech
{
    public class MechJebCore : PartModule, IComparable<MechJebCore>
    {
        private const int windowIDbase = 60606;

        private List<ComputerModule> computerModules = new List<ComputerModule>();
        private bool modulesUpdated = false;

        private static List<Type> moduleRegistry;

        public MechJebModuleAttitudeController attitude;
        public MechJebModuleStagingController staging;
        public MechJebModuleThrustController thrust;
        public MechJebModuleWarpController warp;

        public VesselState vesselState = new VesselState();

        private Vessel controlledVessel; //keep track of which vessel we've added our onFlyByWire callback to

        public string version = "";

        DirectionTarget testTarget;
        IAscentPath testAscentPath = new DefaultAscentPath();

        [KSPField(isPersistant = false)]
        public string blacklist = "";

        //Returns whether the vessel we've registered OnFlyByWire with is the correct one. 
        //If it isn't the correct one, fixes it before returning false
        bool CheckControlledVessel()
        {
            if (controlledVessel == part.vessel) return true;

            //else we have an onFlyByWire callback registered with the wrong vessel:
            //handle vessel changes due to docking/undocking
            if (controlledVessel != null) controlledVessel.OnFlyByWire -= onFlyByWire;
            part.vessel.OnFlyByWire += onFlyByWire;
            controlledVessel = part.vessel;
            return false;
        }

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
            return (T)computerModules.First(a => a is T);
        }

        public List<T> GetComputerModules<T>() where T : ComputerModule
        {
            return computerModules.FindAll(a => a is T).Cast<T>().ToList();
        }

        public ComputerModule GetComputerModule(string type)
        {
            return computerModules.First(a => a.GetType().Name.ToLowerInvariant() == type.ToLowerInvariant());
        }

        public void AddComputerModule(ComputerModule module)
        {
            computerModules.Add(module);
            modulesUpdated = true;
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (moduleRegistry == null)
            {
                moduleRegistry = (from ass in AppDomain.CurrentDomain.GetAssemblies() from t in ass.GetTypes() where t.IsSubclassOf(typeof(ComputerModule)) select t).ToList();
            }

            Version v = Assembly.GetAssembly(typeof(MechJebCore)).GetName().Version;
            version = v.Major.ToString() + "." + v.Minor.ToString() + "." + v.Build.ToString();

            foreach (Type t in moduleRegistry)
            {
                if ((t != typeof(ComputerModule)) && (t != typeof(DisplayModule)) && !blacklist.Contains(t.Name))
                {
                    AddComputerModule((ComputerModule)(t.GetConstructor(new Type[] { typeof(MechJebCore) }).Invoke(new object[] { this })));
                }
            }

            attitude = GetComputerModule<MechJebModuleAttitudeController>();
            thrust = GetComputerModule<MechJebModuleThrustController>();
            staging = GetComputerModule<MechJebModuleStagingController>();
            warp = GetComputerModule<MechJebModuleWarpController>();

            foreach (ComputerModule module in computerModules)
            {
                module.OnStart(state);
            }

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
            CheckControlledVessel(); //make sure our onFlyByWire callback is registered with the right vessel

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

            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
/*                print("fwd: " + testTarget.GetFwdVector());
                print("name: " + testTarget.GetName());
                //print("obtvel: " + testTarget.GetObtVelocity());
                print("obt: " + testTarget.GetOrbit());
                print("obtdriver: " + testTarget.GetOrbitDriver());
                print("srfvel: " + testTarget.GetSrfVelocity());
                print("transform: " + testTarget.GetTransform());
                print("vessel: " + testTarget.GetVessel());*/
                testTarget = new DirectionTarget("Ascent Path Guidance");
                FlightGlobals.fetch.SetVesselTarget(testTarget);//FlightGlobals.fetch.vesselTargetDelta = FlightGlobals.fetch.vesselTargetDelta = (part.vessel.transform.position + 200 * part.vessel.transform.up);
            }

            if (testTarget != null)
            {
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
                attitude.attitudeTo(Vector3.forward, AttitudeReference.ORBIT, null);
            }
            if (Input.GetKey(KeyCode.U))
            {
                print("rad+");
                attitude.attitudeTo(Vector3.up, AttitudeReference.ORBIT, null);
            }
            if (Input.GetKey(KeyCode.B)) 
            {
                print("nml+");
                attitude.attitudeTo(Vector3.left, AttitudeReference.ORBIT, null);
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

            ConfigNode type = new ConfigNode(KSP.IO.File.Exists<MechJebCore>("mechjeb_settings.cfg", vessel) ? KSP.IO.File.ReadAllText<MechJebCore>("mechjeb_settings.cfg", vessel) : "");
            ConfigNode global = new ConfigNode(KSP.IO.File.Exists<MechJebCore>("mechjeb_settings.cfg") ? KSP.IO.File.ReadAllText<MechJebCore>("mechjeb_settings.cfg") : "");

            foreach (ComputerModule module in computerModules)
            {
                module.OnLoad(node, type, global);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            print("MechJebCore.OnSave");
            base.OnSave(node); //is this necessary?

            ConfigNode type = new ConfigNode(KSP.IO.File.Exists<MechJebCore>("mechjeb_settings.cfg", vessel) ? KSP.IO.File.ReadAllText<MechJebCore>("mechjeb_settings.cfg", vessel) : "");
            ConfigNode global = new ConfigNode(KSP.IO.File.Exists<MechJebCore>("mechjeb_settings.cfg") ? KSP.IO.File.ReadAllText<MechJebCore>("mechjeb_settings.cfg") : "");

            foreach (ComputerModule module in computerModules)
            {
                module.OnSave(node, type, global);
            }

            KSP.IO.File.WriteAllText<MechJebCore>(type.ToString(), "mechjeb_settings.cfg", vessel);
            KSP.IO.File.WriteAllText<MechJebCore>(global.ToString(), "mechjeb_settings.cfg");
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
            if (!CheckControlledVessel() || this != vessel.GetMasterMechJeb())
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
            if (this == vessel.GetMasterMechJeb())
            {
                foreach (ComputerModule module in computerModules)
                {
                    if (module.enabled) module.Drive(s);
                }
            }
        }

        private void OnGUI()
        {
            if ((HighLogic.LoadedSceneIsEditor) || ((FlightGlobals.ready) && (vessel == FlightGlobals.ActiveVessel) && (part.State != PartStates.DEAD) && (this == vessel.GetMasterMechJeb())))
            {
                int wid = 0;
                foreach (DisplayModule module in GetComputerModules<DisplayModule>())
                {
                    if (module.enabled) module.DrawGUI(windowIDbase + wid, HighLogic.LoadedSceneIsEditor);
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
