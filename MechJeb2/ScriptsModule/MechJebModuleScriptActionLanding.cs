using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionLanding : MechJebModuleScriptAction
    {
        public static    string                        NAME = "Landing";
        private readonly MechJebModuleLandingGuidance  module;
        private readonly MechJebModuleLandingAutopilot module_autopilot;

        [Persistent(pass = (int)Pass.Type)]
        private bool deployGear = true;

        [Persistent(pass = (int)Pass.Type)]
        private bool deployChutes = true;

        [Persistent(pass = (int)Pass.Type)]
        public EditableInt limitChutesStage = 0;

        [Persistent(pass = (int)Pass.Type)]
        private EditableDouble touchdownSpeed;

        [Persistent(pass = (int)Pass.Type)]
        private bool landTarget;

        [Persistent(pass = (int)Pass.Type)]
        private EditableAngle targetLattitude = new EditableAngle(0);

        [Persistent(pass = (int)Pass.Type)]
        private EditableAngle targetLongitude = new EditableAngle(0);

        public MechJebModuleScriptActionLanding(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) :
            base(scriptModule, core, actionsList, NAME)
        {
            module           = core.GetComputerModule<MechJebModuleLandingGuidance>();
            module_autopilot = core.GetComputerModule<MechJebModuleLandingAutopilot>();
            readModuleConfiguration();
        }

        public override void activateAction()
        {
            base.activateAction();
            if (landTarget)
            {
                module_autopilot.LandAtPositionTarget(this);
            }
            else
            {
                module_autopilot.LandUntargeted(this);
            }

            writeModuleConfiguration();
            module_autopilot.users.Add(module);
        }

        public override void endAction()
        {
            base.endAction();
            module_autopilot.users.Remove(module);
        }

        public override void readModuleConfiguration()
        {
            deployGear       = module_autopilot.deployGears;
            deployChutes     = module_autopilot.deployChutes;
            limitChutesStage = module_autopilot.limitChutesStage;
            touchdownSpeed   = module_autopilot.touchdownSpeed;
            if (core.target.PositionTargetExists)
            {
                landTarget      = true;
                targetLattitude = core.target.targetLatitude;
                targetLongitude = core.target.targetLongitude;
            }
        }

        public override void writeModuleConfiguration()
        {
            module_autopilot.deployGears      = deployGear;
            module_autopilot.deployChutes     = deployChutes;
            module_autopilot.touchdownSpeed   = touchdownSpeed;
            module_autopilot.limitChutesStage = limitChutesStage;
            if (landTarget)
            {
                core.target.SetPositionTarget(core.vessel.mainBody, targetLattitude, targetLongitude);
            }
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            if (landTarget)
            {
                GUILayout.Label("Land At Target");
            }
            else
            {
                GUILayout.Label("Land Somewhere");
            }

            if (isStarted())
            {
                GUILayout.Label(core.landing.CurrentStep.ToString());
            }

            postWindowGUI(windowID);
        }

        public override void afterOnFixedUpdate()
        {
            if (isStarted() && !isExecuted() && module_autopilot.CurrentStep == null)
            {
                endAction();
            }
        }
    }
}
