using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class DisplayModule : ComputerModule
    {
        public bool hidden = false;

        public Rect windowPos 
        {
            get { return new Rect(windowVector.x, windowVector.y, windowVector.z, windowVector.w); }
            set 
            { 
                windowVector = new Vector4(value.x, value.y, value.width, value.height);
                windowVector.x = Mathf.Clamp(windowVector.x, 10 - value.width, Screen.width - 10);
                windowVector.y = Mathf.Clamp(windowVector.y, 10 - value.height, Screen.height - 10);
            }
        }
        [Persistent(pass = (int)Pass.Global)]
        public Vector4 windowVector; //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects

        [Persistent(pass = (int)Pass.Global)]
        public bool showInFlight = true;

        [Persistent(pass = (int)Pass.Global)]
        public bool showInEditor = false;

        public DisplayModule(MechJebCore core) : base(core) {}

        public virtual GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[0];
        }

        protected virtual void WindowGUI(int windowID)
        {
            GUI.DragWindow();
        }


        public virtual void DrawGUI(int baseWindowID, bool inEditor)
        {
            if (HighLogic.LoadedSceneIsEditor ? showInEditor : showInFlight)
            {
                windowPos = GUILayout.Window(baseWindowID, windowPos, WindowGUI, GetName(), WindowOptions());                
            }
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);

            if(global != null) global.AddValue("enabled", enabled);
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            if (global != null && global.HasValue("enabled"))
            {
                bool loadedEnabled;
                if (bool.TryParse(global.GetValue("enabled"), out loadedEnabled)) enabled = loadedEnabled;
            }
        }

        public virtual string GetName()
        {
            return "Display Module";
        }
    }
}

