using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionAscent : MechJebModuleScriptAction
    {
        public static String NAME = "Ascent";
        private MechJebModuleAscentAutopilot autopilot { get { return core.GetComputerModule<MechJebModuleAscentAutopilot>(); } }
        private MechJebModuleAscentBase ascentPath { get { return autopilot.ascentPath; } }

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

        override public void readModuleConfiguration()
        {
            autopilotConfig = ConfigNode.CreateConfigFromObject(autopilot);
            ascentPathConfig = ConfigNode.CreateConfigFromObject(ascentPath);
            stagingConfig = ConfigNode.CreateConfigFromObject(core.staging);
            thrustConfig = ConfigNode.CreateConfigFromObject(core.thrust);
            // FIXME: missing autowarp
        }

        override public void writeModuleConfiguration()
        {
            ConfigNode.LoadObjectFromConfig(autopilot, autopilotConfig);
            autopilot.doWiring();
            ConfigNode.LoadObjectFromConfig(ascentPath, ascentPathConfig);
            ConfigNode.LoadObjectFromConfig(core.staging, stagingConfig);
            ConfigNode.LoadObjectFromConfig(core.thrust, thrustConfig);
        }

        override public void WindowGUI(int windowID)
        {
            base.preWindowGUI(windowID);
            base.WindowGUI(windowID);

            switch (autopilot.ascentPathIdxPublic)
            {
                case ascentType.CLASSIC:
                    GUILayout.Label("CLASSIC ASCENT to " + (autopilot.desiredOrbitAltitude / 1000.0) + "km");
                    break;
                case ascentType.GRAVITYTURN:
                    GUILayout.Label("GravityTurn™ ASCENT to " + (autopilot.desiredOrbitAltitude / 1000.0) + "km");
                    break;
                case ascentType.PVG:
                    GUILayout.Label("PVG ASCENT to " + (autopilot.desiredOrbitAltitude / 1000.0) + "x" + ((ascentPath as MechJebModuleAscentPVG).DesiredApoapsis / 1000.0) + "km");
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

            if (autopilot != null)
            {
                if (isExecuted() && autopilot.status == "Off")
                {
                    GUILayout.Label ("Finished Ascent");
                }
                else
                {
                    GUILayout.Label (autopilot.status);
                }
            }
            else
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.textColor = Color.yellow;
                GUILayout.Label ("-- ERROR --", s);
            }
            base.postWindowGUI(windowID);
        }

        override public void afterOnFixedUpdate()
        {
            if (autopilot != null)
            {
                if (isStarted() && !isExecuted() && autopilot.status == "Off")
                {
                    endAction();
                }
            }
        }

        override public void activateAction()
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
            autopilot.users.Add(this);
            Debug.Log("Autopilot should be engaged!");
        }

        override public void endAction()
        {
            base.endAction();
            autopilot.users.Remove(this);
        }

        override public void postLoad(ConfigNode node)
        {
            autopilotConfig = node.GetNode("autopilotConfig");
            ascentPathConfig = node.GetNode("ascentPathConfig");
            stagingConfig = node.GetNode("stagingConfig");
            thrustConfig = node.GetNode("thrustConfig");
        }

        override public void postSave(ConfigNode node)
        {
            autopilotConfig.CopyTo(node.AddNode("autopilotConfig"));
            ascentPathConfig.CopyTo(node.AddNode("ascentPathConfig"));
            stagingConfig.CopyTo(node.AddNode("stagingConfig"));
            thrustConfig.CopyTo(node.AddNode("thrustConfig"));
        }

        override public void onAbord()
        {
            // ascentModule.launchingToInterplanetary = ascentModule.launchingToPlane = ascentModule.launchingToRendezvous = autopilot.timedLaunch = false;
            base.onAbord();
        }
    }
}
