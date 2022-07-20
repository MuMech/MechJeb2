using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionAscent : MechJebModuleScriptAction
    {
        public static string                      NAME = "Ascent";
        private       MechJebModuleAscentSettings _ascentSettings => core.ascentSettings;
        private       MechJebModuleAscentBaseAutopilot    _autopilot      => core.ascent;

        private ConfigNode ascentConfig;
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
            ascentConfig = ConfigNode.CreateConfigFromObject(_ascentSettings);
            stagingConfig = ConfigNode.CreateConfigFromObject(core.staging);
            thrustConfig = ConfigNode.CreateConfigFromObject(core.thrust);
        }

        public override void writeModuleConfiguration()
        {
            ConfigNode.LoadObjectFromConfig(_ascentSettings, ascentConfig);
            ConfigNode.LoadObjectFromConfig(core.staging, stagingConfig);
            ConfigNode.LoadObjectFromConfig(core.thrust, thrustConfig);
        }

        public override void WindowGUI(int windowID)
        {
            base.preWindowGUI(windowID);
            base.WindowGUI(windowID);

            switch (_ascentSettings.AscentType)
            {
                case ascentType.CLASSIC:
                    GUILayout.Label("CLASSIC ASCENT to " + (_ascentSettings.DesiredOrbitAltitude / 1000.0) + "km");
                    break;
                case ascentType.GRAVITYTURN:
                    GUILayout.Label("GravityTurn™ ASCENT to " + (_ascentSettings.DesiredOrbitAltitude / 1000.0) + "km");
                    break;
                case ascentType.PVG:
                    GUILayout.Label("PVG ASCENT to " + (_ascentSettings.DesiredOrbitAltitude / 1000.0) + "x" + (_ascentSettings.DesiredApoapsis / 1000.0) + "km");
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
                if (isExecuted() && _autopilot.Status == "Off")
                {
                    GUILayout.Label ("Finished Ascent");
                }
                else
                {
                    GUILayout.Label (_autopilot.Status);
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
                if (isStarted() && !isExecuted() && _autopilot.Status == "Off")
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
            _autopilot.enabled = true;
            Debug.Log("Autopilot should be engaged!");
        }

        public override void endAction()
        {
            base.endAction();
            _autopilot.enabled = false;
        }

        public override void postLoad(ConfigNode node)
        {
            ascentConfig = node.GetNode("ascentConfig");
            stagingConfig = node.GetNode("stagingConfig");
            thrustConfig = node.GetNode("thrustConfig");
        }

        public override void postSave(ConfigNode node)
        {
            ascentConfig.CopyTo(node.AddNode("ascentConfig"));
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
