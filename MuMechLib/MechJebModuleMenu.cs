using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleMenu : DisplayModule
    {
        public MechJebModuleMenu(MechJebCore core) : base(core)
        {
            priority = -1000;
            enabled = true;
            hidden = true;
        }

        public enum WindowStat
        {
            HIDDEN,
            MINIMIZED,
            NORMAL,
            OPENING,
            CLOSING
        }

        private WindowStat _windowStat = WindowStat.HIDDEN;
        protected WindowStat windowStat
        {
            get { return _windowStat; }
            set
            {
                if (_windowStat != value)
                {
                    _windowStat = value;
                    // settingsChanged = true;
                }
            }
        }

        protected float windowProgr = 0;

        public bool firstDraw = true;

        protected override void FlightWindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            foreach (DisplayModule module in core.GetComputerModules<DisplayModule>())
            {
                if (!module.hidden)
                {
                    bool on = module.enabled;
                    if (on != GUILayout.Toggle(on, module.GetName()))
                    {
                        module.enabled = !on;
                    }
                }
            }

            if (GUILayout.Button("Online Manual"))
            {
                Application.OpenURL("http://wiki.mechjeb.com/index.php?title=Manual");
            }
            GUILayout.EndVertical();
        }

        public override void DrawGUI(int baseWindowID, bool inEditor)
        {
            if (!inEditor)
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
                    GUILayout.Window(baseWindowID, new Rect(Screen.width - windowProgr * 200, (Screen.height - 200) / 2, 200, 200), FlightWindowGUI, "MechJeb " + core.version, GUILayout.Width(200), GUILayout.Height(200));
                }

                GUI.depth = -98;

                if (firstDraw)
                {
                    GUI.FocusControl("MechJebOpen");
                    firstDraw = false;
                }
            }
        }
    }
}
