﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleMenu : DisplayModule
    {
        public MechJebModuleMenu(MechJebCore core)
            : base(core)
        {
            priority = -1000;
            enabled = true;
            hidden = true;
            showInFlight = true;
            showInEditor = true;
        }

        public enum WindowStat
        {
            HIDDEN,
            MINIMIZED,
            NORMAL,
            OPENING,
            CLOSING
        }

        [Persistent(pass = (int)Pass.Global)]
        public WindowStat windowStat = WindowStat.HIDDEN;

        [Persistent(pass = (int)Pass.Global)]
        public float windowProgr = 0;

        public bool firstDraw = true;

        public static GUIStyle toggleActive, toggleInactive;

        protected override void WindowGUI(int windowID)
        {
            if (toggleActive == null)
            {
                toggleInactive = new GUIStyle(GUI.skin.toggle);
                toggleInactive.normal.textColor = toggleInactive.onNormal.textColor = Color.white;

                toggleActive = new GUIStyle(toggleInactive);
                toggleActive.normal.textColor = toggleActive.onNormal.textColor = Color.green;
            }

            GUILayout.BeginVertical();

            foreach (DisplayModule module in core.GetComputerModules<DisplayModule>().OrderBy(m => m, DisplayOrder.instance))
            {
                if (!module.hidden && module.showInCurrentScene)
                {
                    bool active = core.attitude.users.RecursiveUser(module) || core.thrust.users.RecursiveUser(module) || core.rover.users.RecursiveUser(module) || core.node.users.RecursiveUser(module) || core.rcs.users.RecursiveUser(module);
                    if (module is MechJebModuleWarpHelper && ((MechJebModuleWarpHelper)module).warping) active = true;
                    module.enabled = GUILayout.Toggle(module.enabled, module.GetName(), active ? toggleActive : toggleInactive);
                }
            }

            if (GUILayout.Button("Online Manual"))
            {
                Application.OpenURL("http://wiki.mechjeb.com/index.php?title=Manual");
            }

            GUILayout.EndVertical();
        }

        public override void DrawGUI(bool inEditor)
        {
            switch (windowStat)
            {
                case WindowStat.OPENING:
                    windowProgr += Time.deltaTime;
                    if (windowProgr >= 1)
                    {
                        windowProgr = 1;
                        windowStat = WindowStat.NORMAL;
                    }
                    break;
                case WindowStat.CLOSING:
                    windowProgr -= Time.deltaTime;
                    if (windowProgr <= 0)
                    {
                        windowProgr = 0;
                        windowStat = WindowStat.HIDDEN;
                    }
                    break;
            }

            GUI.depth = -100;
            GUI.SetNextControlName("MechJebOpen");
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(new Vector3(0, 0, -90)), Vector3.one);
            if (GUI.Button(new Rect((-Screen.height - 100) / 2, Screen.width - 25 - (200 * windowProgr), 100, 25), (windowStat == WindowStat.HIDDEN) ? "/\\ MechJeb /\\" : "\\/ MechJeb \\/"))
            {
                if (windowStat == WindowStat.HIDDEN)
                {
                    windowStat = WindowStat.OPENING;
                    windowProgr = 0;
                    firstDraw = true;
                }
                else if (windowStat == WindowStat.NORMAL)
                {
                    windowStat = WindowStat.CLOSING;
                    windowProgr = 1;
                }
            }
            GUI.matrix = Matrix4x4.identity;

            GUI.depth = -99;

            if (windowStat != WindowStat.HIDDEN)
            {
                windowVector.x = Screen.width - windowProgr * 200;
                windowVector.y = (Screen.height - windowPos.height) / 2;
                windowPos = GUILayout.Window(GetType().FullName.GetHashCode(), windowPos, WindowGUI, "MechJeb " + core.version, GUILayout.Width(200), GUILayout.Height(20));
            }

            GUI.depth = -98;

            if (firstDraw)
            {
                GUI.FocusControl("MechJebOpen");
                firstDraw = false;
            }
        }

        class DisplayOrder : IComparer<DisplayModule>
        {
            private DisplayOrder() { }
            public static DisplayOrder instance = new DisplayOrder();

            int IComparer<DisplayModule>.Compare(DisplayModule a, DisplayModule b)
            {
                if (a is MechJebModuleCustomInfoWindow && b is MechJebModuleCustomInfoWindow) return a.GetName().CompareTo(b.GetName());
                if (a is MechJebModuleCustomInfoWindow) return 1;
                if (b is MechJebModuleCustomInfoWindow) return -1;
                return a.GetName().CompareTo(b.GetName());
            }
        }
    }
}
