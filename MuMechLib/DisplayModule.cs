using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class DisplayModule
    {
        public Part part = null;
        public MechJebCore core = null;

        protected bool _enabled = false;
        public bool enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (value != _enabled)
                {
                    //core.settingsChanged = true;
                    _enabled = value;
                    if (_enabled)
                    {
                        onModuleEnabled();
                    }
                    else
                    {
                        onModuleDisabled();
                    }
                }
            }
        }

        protected Rect _windowPos;
        public Rect windowPos
        {
            get
            {
                return _windowPos;
            }
            set
            {
                if (_windowPos.x != value.x || _windowPos.y != value.y)
                {
                    //core.settingsChanged = true;
                }
                _windowPos = value;
            }
        }


        public DisplayModule(MechJebCore core)
        {
            this.core = core;
            part = core.part;
        }

        public virtual void onModuleEnabled()
        {
        }

        public virtual void onModuleDisabled()
        {
        }

        protected virtual void WindowGUI(int windowID)
        {
            GUI.DragWindow();
        }

        public virtual void drawGUI(int baseWindowID)
        {
            windowPos = GUILayout.Window(baseWindowID, windowPos, WindowGUI, getName(), windowOptions());
        }

        public virtual GUILayoutOption[] windowOptions()
        {
            return new GUILayoutOption[0];
        }


        public virtual string getName()
        {
            return "Display Module";
        }

    }
}
