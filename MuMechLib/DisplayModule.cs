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

        protected Rect _editorWindowPos;
        public Rect editorWindowPos
        {
            get
            {
                return _editorWindowPos;
            }
            set
            {
                if (_editorWindowPos.x != value.x || _editorWindowPos.y != value.y)
                {
                    //core.settingsChanged = true;
                }
                _editorWindowPos = value;
            }
        }

        protected Rect _flightWindowPos;
        public Rect flightWindowPos
        {
            get
            {
                return _flightWindowPos;
            }
            set
            {
                if (_flightWindowPos.x != value.x || _flightWindowPos.y != value.y)
                {
                    //core.settingsChanged = true;
                }
                _flightWindowPos = value;
            }
        }

        public bool showInFlight = true;
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

        public virtual string GetName()
        {
            return "Display Module";
        }
    }
}
