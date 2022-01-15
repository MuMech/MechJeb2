using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionWaitFor : MechJebModuleScriptAction
    {
        public static string NAME = "WaitFor";

        /*
        [Persistent(pass = (int)Pass.Type)]
        private int actionType = 0;
        */
        private readonly MechJebModuleScriptCondition condition;

        public MechJebModuleScriptActionWaitFor (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
        {
            condition = new MechJebModuleScriptCondition(scriptModule, core, this);
        }

        public override void activateAction()
        {
            base.activateAction();
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void WindowGUI(int windowID)
        {
            base.preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label ("Wait for");
            condition.WindowGUI(windowID);
            base.postWindowGUI(windowID);
        }

        public override void afterOnFixedUpdate()
        {
            if (this.isStarted() && !this.isExecuted() && condition.checkCondition())
            {
                this.endAction();
            }
        }

        public override void postLoad(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(condition, node.GetNode("Condition"));
        }

        public override void postSave(ConfigNode node)
        {
            var conditionNode = ConfigNode.CreateConfigFromObject(this.condition, (int)Pass.Type, null);
            conditionNode.CopyTo(node.AddNode("Condition"));
        }
    }
}
