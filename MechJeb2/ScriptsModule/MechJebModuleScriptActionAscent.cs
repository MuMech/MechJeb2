using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionAscent : MechJebModuleScriptAction
    {
        public static string NAME = "Ascent";
        private MechJebModuleAscentAutopilot _autopilot { get { return core.GetComputerModule<MechJebModuleAscentAutopilot>(); } }
        private MechJebModuleAscentBase _ascentPath { get { return _autopilot.ascentPath; } }

        private ConfigNode autopilotConfig;
        private ConfigNode ascentPathConfig;
        private ConfigNode stagingConfig;
        private ConfigNode thrustConfig;

        /*
        [Persistent(pass = (int)Pass.Type)]
            private int actionType;
        private List<String> actionTypes = new List<String>();
        //Module Parameters
        */

        public MechJebModuleScriptActionAscent (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) : base(scriptModule, core, actionsList, NAME)
        {
            /*
            mainBody = core.target.mainBody;
            targetOrbit = core.target.TargetOrbit;
            actionTypes.Add("Ascent Guidance");
            actionTypes.Add("Launching to Rendezvous");
            */
            readModuleConfiguration();
        }

        public override void readModuleConfiguration()
        {
            autopilotConfig = ConfigNode.CreateConfigFromObject(_autopilot);
            ascentPathConfig = ConfigNode.CreateConfigFromObject(_ascentPath);
            stagingConfig = ConfigNode.CreateConfigFromObject(core.staging);
            thrustConfig = ConfigNode.CreateConfigFromObject(core.thrust);
            // FIXME: missing autowarp
        }

        public override void writeModuleConfiguration()
        {
            ConfigNode.LoadObjectFromConfig(_autopilot, autopilotConfig);
            ConfigNode.LoadObjectFromConfig(_ascentPath, ascentPathConfig);
            ConfigNode.LoadObjectFromConfig(core.staging, stagingConfig);
            ConfigNode.LoadObjectFromConfig(core.thrust, thrustConfig);
        }

        public override void WindowGUI(int windowID)
        {
            base.preWindowGUI(windowID);
            base.WindowGUI(windowID);

            switch (_autopilot.ascentPathIdxPublic)
            {
                case ascentType.CLASSIC:
                    GUILayout.Label("CLASSIC ASCENT to " + (_autopilot.desiredOrbitAltitude / 1000.0) + "km");
                    break;
                case ascentType.GRAVITYTURN:
                    GUILayout.Label("GravityTurn™ ASCENT to " + (_autopilot.desiredOrbitAltitude / 1000.0) + "km");
                    break;
                case ascentType.PVG:
                    GUILayout.Label("PVG ASCENT to " + (_autopilot.desiredOrbitAltitude / 1000.0) + "x" + (_autopilot.DesiredApoapsis / 1000.0) + "km");
                    break;
            }

            /*
            actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
            if (actionType == 1)
            {
                launchingToRendezvous = true;
            }
            else {
                launchingToRendezvous = false;
            }
            if (!launchingToRendezvous) {
                GUILayout.Label ("ASCENT to " + (desiredOrbitAltitude / 1000.0) + "km");
            } else {
                GUILayout.Label ("Launching to rendezvous");
            }

            */

            if (_autopilot != null)
            {
                if (isExecuted() && _autopilot.status == "Off")
                {
                    GUILayout.Label ("Finished Ascent");
                }
                else
                {
                    GUILayout.Label (_autopilot.status);
                }
            }
            else
            {
                GUILayout.Label ("-- ERROR --",GuiUtils.yellowLabel);
            }
            base.postWindowGUI(windowID);
        }

        public override void afterOnFixedUpdate()
        {
            if (_autopilot != null)
            {
                if (isStarted() && !isExecuted() && _autopilot.status == "Off")
                {
                    endAction();
                }
            }
        }

        public override void activateAction()
        {
            base.activateAction();
            writeModuleConfiguration();

            /*
            if (this.launchingToRendezvous)
            {
                this.autopilot.StartCountdown(this.scriptModule.vesselState.time +
                        LaunchTiming.TimeToPhaseAngle(this.autopilot.launchPhaseAngle,
                            mainBody, this.scriptModule.vesselState.longitude, targetOrbit));
            }
            */
            _autopilot.users.Add(this);
            Debug.Log("Autopilot should be engaged!");
        }

        public override void endAction()
        {
            base.endAction();
            _autopilot.users.Remove(this);
        }

        public override void postLoad(ConfigNode node)
        {
            autopilotConfig = node.GetNode("autopilotConfig");
            ascentPathConfig = node.GetNode("ascentPathConfig");
            stagingConfig = node.GetNode("stagingConfig");
            thrustConfig = node.GetNode("thrustConfig");
        }

        public override void postSave(ConfigNode node)
        {
            autopilotConfig.CopyTo(node.AddNode("autopilotConfig"));
            ascentPathConfig.CopyTo(node.AddNode("ascentPathConfig"));
            stagingConfig.CopyTo(node.AddNode("stagingConfig"));
            thrustConfig.CopyTo(node.AddNode("thrustConfig"));
        }

        public override void onAbord()
        {
            // ascentModule.launchingToInterplanetary = ascentModule.launchingToPlane = ascentModule.launchingToRendezvous = autopilot.timedLaunch = false;
            base.onAbord();
        }
    }
}
