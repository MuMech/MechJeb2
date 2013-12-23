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
            get
            {
                if (!HighLogic.LoadedSceneIsEditor)                
                    return new Rect(windowVector.x, windowVector.y, windowVector.z, windowVector.w);                
                else
                    return new Rect(windowVectorEditor.x, windowVectorEditor.y, windowVectorEditor.z, windowVectorEditor.w);
            }
            set
            {
                if (!HighLogic.LoadedSceneIsEditor)
                {
                    windowVector = new Vector4(
                        Math.Min(Math.Max(value.x, 0), Screen.width - value.width),
                        Math.Min(Math.Max(value.y, 0), Screen.height - value.height),
                        value.width, value.height
                    );
                    windowVector.x = Mathf.Clamp(windowVector.x, 10 - value.width, Screen.width - 10);
                    windowVector.y = Mathf.Clamp(windowVector.y, 10 - value.height, Screen.height - 10);
                }
                else
                {
                    windowVectorEditor = new Vector4(
                        Math.Min(Math.Max(value.x, 0), Screen.width - value.width),
                        Math.Min(Math.Max(value.y, 0), Screen.height - value.height),
                        value.width, value.height
                    );
                    windowVectorEditor.x = Mathf.Clamp(windowVectorEditor.x, 10 - value.width, Screen.width - 10);
                    windowVectorEditor.y = Mathf.Clamp(windowVectorEditor.y, 10 - value.height, Screen.height - 10);
                }
            }
        }

        [Persistent(pass = (int)Pass.Global)]
        public Vector4 windowVector = new Vector4(10, 40, 0, 0); //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects

        [Persistent(pass = (int)Pass.Global)]
        public Vector4 windowVectorEditor = new Vector4(10, 40, 0, 0); //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects


        [Persistent(pass = (int)Pass.Global)]
        public bool showInFlight = true;

        [Persistent(pass = (int)Pass.Global)]
        public bool showInEditor = false;

        // Used for the Blizzy Toolbar-Plugin
        [Persistent(pass = (int)Pass.Global)]
        public bool useIcon = false;

        public bool showInCurrentScene { get { return (HighLogic.LoadedSceneIsEditor ? showInEditor : showInFlight); } }

        public int ID;
        public static int nextID = 72190852;

        public DisplayModule(MechJebCore core)
            : base(core)
        {
            ID = nextID;
            nextID++;
        }

        public virtual GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[0];
        }

        protected virtual void WindowGUI(int windowID)
        {
            if (GUI.Button(new Rect(windowPos.width - 18, 2, 16, 16), ""))
            {
                enabled = false;
            }

            if (GUI.Button(new Rect(windowPos.width - 33, 0, 13, 20), "T", GuiUtils.yellowOnHover))
            {
                useIcon = !useIcon;
            }

            GUI.DragWindow();
        }

        public virtual void DrawGUI(bool inEditor)
        {
            if (showInCurrentScene)
            {
                windowPos = GUILayout.Window(ID, windowPos, WindowGUI, GetName(), WindowOptions());

                //                var windows = core.GetComputerModules<DisplayModule>(); // on ice until there's a way to find which window is active, unless you like dragging other windows by snapping
                //                
                //                foreach (var w in windows)
                //                {
                //                	if (w == this) { continue; }
                //                	
                //                	var diffL = (w.windowPos.x + w.windowPos.width - 2) - windowPos.x;
                //                	var diffR = w.windowPos.x - (windowPos.x + windowPos.width + 2);
                //                	var diffT = (w.windowPos.y + w.windowPos.height - 2) - windowPos.y;
                //                	var diffB = w.windowPos.y - (windowPos.y + windowPos.height + 2);
                //                	
                //                	if (Math.Abs(diffL) <= 8)
                //                	{
                //                		SetPos(w.windowPos.x + w.windowPos.width - 2, windowPos.y);
                //                	}
                //                	
                //                	if (Math.Abs(diffR) <= 8)
                //                	{
                //                		SetPos(w.windowPos.x - windowPos.width + 2, windowPos.y);
                //                	}
                //                }
            }
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);

            if (global != null) global.AddValue("enabled", enabled);
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

        public override void UnlockCheck()
        {
            if (!unlockChecked)
            {
                bool prevEn = enabled;
                enabled = true;
                base.UnlockCheck();
                if (unlockParts.Trim().Length > 0 || unlockTechs.Trim().Length > 0)
                {
                    hidden = !enabled;
                    if (hidden)
                    {
                        prevEn = false;
                    }
                }
                enabled = prevEn;
            }
        }

        public virtual string GetName()
        {
            return "Display Module";
        }
    }
}
