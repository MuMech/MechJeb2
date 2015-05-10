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
        public bool locked = false;

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
                        Math.Min(Math.Max(value.x, 0), GuiUtils.scaledScreenWidth - value.width),
                        Math.Min(Math.Max(value.y, 0), GuiUtils.scaledScreenHeight - value.height),
                        value.width, value.height
                    );
                    windowVector.x = Mathf.Clamp(windowVector.x, 10 - value.width, GuiUtils.scaledScreenWidth - 10);
                    windowVector.y = Mathf.Clamp(windowVector.y, 10 - value.height, GuiUtils.scaledScreenHeight - 10);
                }
                else
                {
                    windowVectorEditor = new Vector4(
                        Math.Min(Math.Max(value.x, 0), GuiUtils.scaledScreenWidth - value.width),
                        Math.Min(Math.Max(value.y, 0), GuiUtils.scaledScreenHeight - value.height),
                        value.width, value.height
                    );
                    windowVectorEditor.x = Mathf.Clamp(windowVectorEditor.x, 10 - value.width, GuiUtils.scaledScreenWidth - 10);
                    windowVectorEditor.y = Mathf.Clamp(windowVectorEditor.y, 10 - value.height, GuiUtils.scaledScreenHeight - 10);
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

        protected void WindowGUI(int windowID, bool draggable)
        {
            if (GUI.Button(new Rect(windowPos.width - 18, 2, 16, 16), ""))
            {
                enabled = false;
            }

//            if (GUI.Button(new Rect(windowPos.width - 40, 2, 20, 16), (locked ? "X" : "O"))) // lock button needs an icon, letters look crap
//            {
//                locked = !locked;
//            }
            
            bool allowDrag = true;
            if (core.settings.useTitlebarDragging)
            {
                float x = Mouse.screenPos.x / GuiUtils.scale;
                float y = Mouse.screenPos.y / GuiUtils.scale;
                allowDrag = x >= windowPos.xMin + 3 && x <= windowPos.xMin + windowPos.width - 3 &&
                            y >= windowPos.yMin + 3 && y <= windowPos.yMin + 17;
            }

            if (draggable && allowDrag)
                GUI.DragWindow();
        }

        protected virtual void WindowGUI(int windowID)
        {
            WindowGUI(windowID, true);
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
//            if (global != null) global.AddValue("locked", locked);
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            if (global != null && global.HasValue("enabled"))
            {
                bool loadedEnabled;
                if (bool.TryParse(global.GetValue("enabled"), out loadedEnabled)) enabled = loadedEnabled;
            }

//            if (global != null && global.HasValue("locked"))
//            {
//                bool loadedLocked;
//                if (bool.TryParse(global.GetValue("locked"), out loadedLocked)) locked = loadedLocked;
//            }
        }

        public virtual bool isActive()
        {
            ComputerModule[] makesActive = { core.attitude, core.thrust, core.rover, core.node, core.rcs, core.rcsbal };

            bool active = false;
            foreach (var m in makesActive)
            {
                if (m != null)
                {
                    if (active |= m.users.RecursiveUser(this)) break;
                }
            }
            return active;
        }

        public override void UnlockCheck()
        {
            if (!unlockChecked)
            {
                bool prevEn = enabled;
                enabled = true;
                base.UnlockCheck();
                if (unlockParts.Trim().Length > 0 || unlockTechs.Trim().Length > 0 || !IsSpaceCenterUpgradeUnlocked())
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
