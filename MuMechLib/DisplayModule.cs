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
        public VesselState vesselState = null;

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
                        OnModuleEnabled();
                    }
                    else
                    {
                        OnModuleDisabled();
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
            vesselState = core.vesselState;
        }

        public virtual void OnModuleEnabled()
        {
        }

        public virtual void OnModuleDisabled()
        {
        }

        public virtual void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
        }

        public virtual void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
        }

        protected virtual void WindowGUI(int windowID)
        {
            GUI.DragWindow();
        }

        public virtual void DrawGUI(int baseWindowID)
        {
            windowPos = GUILayout.Window(baseWindowID, windowPos, WindowGUI, GetName(), WindowOptions());
        }

        public virtual GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[0];
        }


        public virtual string GetName()
        {
            return "Display Module";
        }

    }
}
