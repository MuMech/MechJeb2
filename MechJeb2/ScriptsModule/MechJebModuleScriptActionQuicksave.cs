using System;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionQuicksave : MechJebModuleScriptAction
    {
        public static    string NAME = "Quicksave";
        private          int    spendTime;
        private readonly int    initTime = 5; //Add a 5s timer after the save action
        private          float  startTime;
        private          bool   saved;

        public MechJebModuleScriptActionQuicksave(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) :
            base(scriptModule, core, actionsList, NAME)
        {
        }

        public override void activateAction()
        {
            base.activateAction();
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void afterOnFixedUpdate()
        {
            if (!isExecuted() && isStarted() && startTime == 0f)
            {
                startTime = Time.time;
            }

            if (!isExecuted() && isStarted() && startTime > 0)
            {
                spendTime = initTime - (int)Math.Round(Time.time - startTime);
                if (spendTime <= 0)
                {
                    if (!saved)
                    {
                        //Wait 5s before save so FlightsGlobals are clear to save
                        scriptModule.setActiveSavepoint(actionIndex);
                        if (FlightGlobals.ClearToSave() == ClearToSaveStatus.CLEAR)
                        {
                            QuickSaveLoad.QuickSave();
                        }

                        saved     = true;
                        startTime = Time.time;
                    }
                    else
                    {
                        //Then wait 5s after save to let the time to save
                        endAction();
                    }
                }
            }
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label("Quicksave");
            if (isStarted() && !isExecuted())
            {
                GUILayout.Label(" waiting " + spendTime + "s");
            }

            postWindowGUI(windowID);
        }
    }
}
