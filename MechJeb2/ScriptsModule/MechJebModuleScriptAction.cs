using UnityEngine;

namespace MuMech
{
    public abstract class MechJebModuleScriptAction
    {
        private readonly string                         name = "DEFAULT";
        protected        bool                           started;
        protected        bool                           executed;
        protected        MechJebModuleScript            scriptModule;
        protected        MechJebCore                    core;
        protected        int                            actionIndex;
        protected        MechJebModuleScriptActionsList actionsList;

        public MechJebModuleScriptAction(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList, string name)
        {
            this.scriptModule = scriptModule;
            this.core         = core;
            this.name         = name;
            this.actionsList  = actionsList;
        }

        public void setActionIndex(int actionIndex)
        {
            this.actionIndex = actionIndex;
        }

        public int getActionIndex()
        {
            return actionIndex;
        }

        public virtual void activateAction()
        {
            started = true;
        }

        public void markActionDone() //Used when we reload a previously saved script to mark past actions as resolved
        {
            started  = false;
            executed = true;
        }

        public virtual void endAction()
        {
            markActionDone();
            actionsList.notifyEndAction();
        }

        public bool isStarted()
        {
            return started;
        }

        public bool isExecuted()
        {
            return executed;
        }

        public void deleteAction()
        {
            actionsList.removeAction(this);
        }

        public virtual void readModuleConfiguration()  { }
        public virtual void writeModuleConfiguration() { }
        public virtual void afterOnFixedUpdate()       { }
        public virtual void postLoad(ConfigNode node)  { }
        public virtual void postSave(ConfigNode node)  { }

        public virtual void onAbord()
        {
            endAction();
        }

        public string getName()
        {
            return name;
        }

        public void preWindowGUI(int windowID)
        {
            GUILayout.BeginHorizontal();
        }

        public virtual void WindowGUI(int windowID)
        {
            if (isStarted())
            {
                GUILayout.Label(scriptModule.imageRed, GUILayout.Width(20), GUILayout.Height(20));
            }
            else
            {
                if (executed)
                {
                    GUILayout.Label(scriptModule.imageGreen, GUILayout.Width(20), GUILayout.Height(20));
                }
                else
                {
                    GUILayout.Label(scriptModule.imageGray, GUILayout.Width(20), GUILayout.Height(20));
                }
            }

            if (!scriptModule.isStarted())
            {
                if (GUILayout.Button("✖", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    deleteAction();
                }

                if (GUILayout.Button("↑", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    actionsList.moveActionUp(this);
                }
            }
        }

        public void postWindowGUI(int windowID)
        {
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public void resetStatus()
        {
            started  = false;
            executed = false;
        }
    }
}
