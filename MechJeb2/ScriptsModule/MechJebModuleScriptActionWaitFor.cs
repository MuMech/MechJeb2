using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionWaitFor : MechJebModuleScriptAction
    {
        public static String NAME = "WaitFor";

        /*
        [Persistent(pass = (int)Pass.Type)]
        private int actionType = 0;
        */
        private MechJebModuleScriptCondition condition;

        public MechJebModuleScriptActionWaitFor (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
        {
            condition = new MechJebModuleScriptCondition(scriptModule, core, this);
        }

        override public void activateAction()
        {
            base.activateAction();
        }

        override public void endAction()
        {
            base.endAction();
        }

        override public void WindowGUI(int windowID)
        {
            base.preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label ("Wait for");
            condition.WindowGUI(windowID);
            base.postWindowGUI(windowID);
        }

        override public void afterOnFixedUpdate()
        {
            if (this.isStarted() && !this.isExecuted())
            {
                if (condition.checkCondition())
                {
                    this.endAction();
                }
            }
        }

        override public void postLoad(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(condition, node.GetNode("Condition"));
        }

        override public void postSave(ConfigNode node)
        {
            ConfigNode conditionNode = ConfigNode.CreateConfigFromObject(this.condition, (int)Pass.Type, null);
            conditionNode.CopyTo(node.AddNode("Condition"));
        }
    }
}
