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

        public Rect editorWindowPos
        {
            get { return new Rect(editorWindowVector.x, editorWindowVector.y, editorWindowVector.z, editorWindowVector.w); }
            set { editorWindowVector = new Vector4(value.x, value.y, value.width, value.height); }
        }
        [Persistent(pass = (int)Pass.Global)]
        public Vector4 editorWindowVector; //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects

        public Rect flightWindowPos 
        {
            get { return new Rect(flightWindowVector.x, flightWindowVector.y, flightWindowVector.z, flightWindowVector.w); }
            set { flightWindowVector = new Vector4(value.x, value.y, value.width, value.height); }
        }
        [Persistent(pass = (int)Pass.Global)]
        public Vector4 flightWindowVector; //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects

        [Persistent(pass = (int)Pass.Global)]
        public bool showInFlight = true;

        [Persistent(pass = (int)Pass.Global)]
        public bool showInEditor = false;

        public DisplayModule(MechJebCore core) : base(core) {}

        public virtual GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[0];
        }

        protected virtual void FlightWindowGUI(int windowID)
        {
            GUI.DragWindow();
        }

        public virtual GUILayoutOption[] EditorWindowOptions()
        {
            return new GUILayoutOption[0];
        }

        protected virtual void EditorWindowGUI(int windowID)
        {
            GUI.DragWindow();
        }

        public virtual void DrawGUI(int baseWindowID, bool inEditor)
        {
            if (inEditor)
            {
                if (showInEditor)
                {
                    editorWindowPos = GUILayout.Window(baseWindowID, editorWindowPos, EditorWindowGUI, GetName(), EditorWindowOptions());
                }
            }
            else
            {
                if (showInFlight)
                {
                    flightWindowPos = GUILayout.Window(baseWindowID, flightWindowPos, FlightWindowGUI, GetName(), FlightWindowOptions());
                }
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

