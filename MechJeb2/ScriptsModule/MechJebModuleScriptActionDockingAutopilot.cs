using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionDockingAutopilot : MechJebModuleScriptAction
    {
        public static    string                        NAME = "DockingAutopilot";
        private readonly MechJebModuleDockingAutopilot autopilot;
        private readonly MechJebModuleDockingGuidance  moduleGuidance;

        [Persistent(pass = (int)Pass.Type)]
        private readonly double speedLimit;

        [Persistent(pass = (int)Pass.Type)]
        private readonly double rol;

        [Persistent(pass = (int)Pass.Type)]
        private readonly bool forceRol;

        [Persistent(pass = (int)Pass.Type)]
        private readonly double overridenSafeDistance;

        [Persistent(pass = (int)Pass.Type)]
        private readonly bool overrideSafeDistance;

        [Persistent(pass = (int)Pass.Type)]
        private readonly bool overrideTargetSize;

        [Persistent(pass = (int)Pass.Type)]
        private readonly double overridenTargetSize;

        [Persistent(pass = (int)Pass.Type)]
        private readonly float safeDistance;

        [Persistent(pass = (int)Pass.Type)]
        private readonly float targetSize;

        [Persistent(pass = (int)Pass.Type)]
        private readonly bool drawBoundingBox;

        public MechJebModuleScriptActionDockingAutopilot(MechJebModuleScript scriptModule, MechJebCore core,
            MechJebModuleScriptActionsList actionsList) : base(scriptModule, core, actionsList, NAME)
        {
            autopilot = core.GetComputerModule<MechJebModuleDockingAutopilot>();
            ;
            moduleGuidance = core.GetComputerModule<MechJebModuleDockingGuidance>();
            ;
            speedLimit            = autopilot.speedLimit;
            rol                   = autopilot.rol;
            forceRol              = autopilot.forceRol;
            overridenSafeDistance = autopilot.overridenSafeDistance;
            overrideSafeDistance  = autopilot.overrideSafeDistance;
            overrideTargetSize    = autopilot.overrideTargetSize;
            overridenTargetSize   = autopilot.overridenTargetSize;
            safeDistance          = autopilot.safeDistance;
            targetSize            = autopilot.targetSize;
            drawBoundingBox       = autopilot.drawBoundingBox;
        }

        public override void activateAction()
        {
            autopilot.users.Add(moduleGuidance);
            autopilot.speedLimit            = speedLimit;
            autopilot.rol                   = rol;
            autopilot.forceRol              = forceRol;
            autopilot.overridenSafeDistance = overridenSafeDistance;
            autopilot.overrideSafeDistance  = overrideSafeDistance;
            autopilot.overrideTargetSize    = overrideTargetSize;
            autopilot.overridenTargetSize   = overridenTargetSize;
            autopilot.safeDistance          = safeDistance;
            autopilot.targetSize            = targetSize;
            autopilot.drawBoundingBox       = drawBoundingBox;
            autopilot.enabled               = true;
            autopilot.users.Add(moduleGuidance);
            autopilot.dockingStep = MechJebModuleDockingAutopilot.DockingStep.INIT;
            base.activateAction();
        }

        public override void endAction()
        {
            autopilot.users.Remove(moduleGuidance);
            base.endAction();
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label("Docking Autopilot");
            if (autopilot.status.CompareTo("") != 0)
            {
                GUILayout.Label(autopilot.status);
            }

            postWindowGUI(windowID);
        }

        public override void afterOnFixedUpdate()
        {
            if (isStarted() && !isExecuted() && autopilot.dockingStep == MechJebModuleDockingAutopilot.DockingStep.OFF)
            {
                endAction();
            }
        }

        public override void onAbord()
        {
            autopilot.enabled = false;
            base.onAbord();
        }
    }
}
