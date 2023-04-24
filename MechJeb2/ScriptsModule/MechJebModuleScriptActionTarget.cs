using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionTarget : MechJebModuleScriptAction
    {
        public static string NAME = "Target";

        [Persistent(pass = (int)Pass.Type)]
        private EditableInt targetType = 0;

        [Persistent(pass = (int)Pass.Type)]
        private EditableInt selectedCelestialBodyIndex = 0;

        /*
        [Persistent(pass = (int)Pass.Type)]
        private EditableAngle targetLatitude;
        [Persistent(pass = (int)Pass.Type)]
        private EditableAngle targetLongitude;
        */
        private readonly List<string> bodiesNamesList = new List<string>();
        private readonly List<string> targetTypes     = new List<string>();

        public MechJebModuleScriptActionTarget(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) : base(
            scriptModule, core, actionsList, NAME)
        {
            targetTypes.Add("Celestial Body");
            //targetTypes.Add ("Celestial Body Long/Lat"); //TODO: Fix the Lat/Long feature
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                bodiesNamesList.Add(body.bodyName);
            }
        }

        public CelestialBody getBodyFromIndex(int index)
        {
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.bodyName.CompareTo(bodiesNamesList[index]) == 0)
                {
                    return body;
                }
            }

            return null;
        }

        public override void activateAction()
        {
            base.activateAction();
            if (targetType == 0)
            {
                CelestialBody body = getBodyFromIndex(selectedCelestialBodyIndex);
                if (body != null)
                {
                    core.target.Set(body);
                }
            }
            else
            {
                CelestialBody body = getBodyFromIndex(selectedCelestialBodyIndex);
                if (body != null)
                {
                    // this.core.target.SetPositionTarget (body, this.targetLatitude, this.targetLongitude);
                }
            }

            endAction();
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label("Target");
            targetType                 = GuiUtils.ComboBox.Box(targetType, targetTypes.ToArray(), targetTypes);
            selectedCelestialBodyIndex = GuiUtils.ComboBox.Box(selectedCelestialBodyIndex, bodiesNamesList.ToArray(), bodiesNamesList);
            if (targetType == 1)
            {
                //TODO : Does not work for the moment with Lat/Long
                //GuiUtils.SimpleTextBox ("Lat", targetLatitude, "°", 30f);
                //GuiUtils.SimpleTextBox ("Long", targetLongitude, "°", 30);
            }

            postWindowGUI(windowID);
        }
    }
}
