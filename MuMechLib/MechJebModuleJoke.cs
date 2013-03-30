using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleJoke : DisplayModule
    {
        public MechJebModuleJoke(MechJebCore core)
            : base(core)
        {
            hidden = true;
            enabled = true;
        }


        static Texture2D _lightning;
        static Texture2D lightning
        {
            get
            {
                if (_lightning == null)
                {
                    _lightning = new Texture2D(1, 1);
                    _lightning.SetPixel(0, 0, Color.white);
                    _lightning.Apply();
                }
                return _lightning;
            }
        }

        System.Random rand = new System.Random();

        bool newFlight = false;

        MissionControlTutorial tutorial;
        bool startedTutorial = false;

        bool lockedControls = false;
        bool disabledSelf = false;

        public override void OnStart(PartModule.StartState state)
        {
            if ((state & PartModule.StartState.PreLaunch) != 0)
            {
                newFlight = true;
                enabled = true;

                Debug.Log("adding tutorial...");
                tutorial = (MissionControlTutorial)ScenarioRunner.fetch.AddModule("MissionControlTutorial");
                Debug.Log("tutorial = " + tutorial);
            }
        }


        public override void DrawGUI(bool inEditor)
        {
            Debug.Log("hello world");

            if (!inEditor)
            {
                if (newFlight)
                {
                    if (vessel.missionTime > 10 && vessel.missionTime < 13)
                    {
                        DrawLightning();


                        if (vessel.missionTime > 11 && !lockedControls)
                        {
                            LockControls();
                            lockedControls = true;
                        }
                    }
                    else
                    {
                        if (vessel.missionTime > 14 && !startedTutorial)
                        {
                            tutorial.StartTutorial();
                            startedTutorial = true;
                        }

                        if (vessel.missionTime > 18 && !disabledSelf)
                        {
                            hidden = false;
                            enabled = false;
                            disabledSelf = true;
                        }

                        if (disabledSelf && enabled)
                        {
                            windowPos = GUILayout.Window(GetType().FullName.GetHashCode(), windowPos, WindowGUI, GetName(), WindowOptions());
                        }
                    }
                }
            }
        }

        bool t1 = true, t2 = false, t3 = false, t4 = false, t5 = true;        
        protected override void WindowGUI(int windowID)
        {
            bool prevT4 = t4;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            t1 = !GUILayout.Toggle(!t1, "LOW", GUI.skin.button);
            t2 = !GUILayout.Toggle(!t2, "PWR", GUI.skin.button);
            t3 = !GUILayout.Toggle(!t3, "RCV", GUI.skin.button);
            t4 = !GUILayout.Toggle(!t4, "MAN", GUI.skin.button);
            t5 = !GUILayout.Toggle(!t5, "MAG", GUI.skin.button);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("VOL");
            GUILayout.Label("APU");
            GUILayout.Label("TLM");
            GUILayout.Label("SCE");
            GUILayout.Label("???");
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            t1 = GUILayout.Toggle(t1, "HIH", GUI.skin.button);
            t2 = GUILayout.Toggle(t2, "CHG", GUI.skin.button);
            t3 = GUILayout.Toggle(t3, "SND", GUI.skin.button);
            t4 = GUILayout.Toggle(t4, "AUX", GUI.skin.button);
            t5 = GUILayout.Toggle(t5, "MOR", GUI.skin.button);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (!prevT4 && t4)
            {
                UnlockControls();
                tutorial.FinishTutorial();
            }
            if (!t5)
            {
                if (rand.NextDouble() < TimeWarp.fixedDeltaTime / 5) vessel.parts[rand.Next(vessel.parts.Count)].explode();
            }

            GUI.DragWindow();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(150), GUILayout.Height(50) };
        }

        string lockID = "MechJebJokeLock";
        void LockControls()
        {
            InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS, lockID);
        }

        void UnlockControls()
        {
            InputLockManager.RemoveControlLock(lockID);
        }

        void DrawLightning()
        {
            if (rand.NextDouble() < 0.2)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), lightning);
            }
        }

        public override string GetName()
        {
            return "Obscure Control Panel";
        }

    }
    
    public class MissionControlTutorial : TutorialScenario
    {
        public void StartTutorial()
        {
            Tutorial.StartTutorial(Tutorial.pages.First());
        }

        public void FinishTutorial()
        {
            Tutorial.GoToNextPage();
        }

        protected override void OnAssetSetup()
        {
            instructorPrefabName = "Instructor_Gene";
        }

        protected override void OnTutorialSetup()
        {
            TutorialPage page1 = new TutorialPage("Page1");
            Tutorial.AddPage(page1);
            page1.windowTitle = "Mission Control";
            page1.OnDrawContent = () =>
            {
                GUILayout.Label("What the---? Did that lightning just score a direct hit on the " + FlightGlobals.ActiveVessel.vesselName + "? Oh no...");
            };
            page1.SetAdvanceCondition((KFSMState s) => FlightGlobals.ActiveVessel.missionTime > 16);

            TutorialPage page2 = new TutorialPage("Page2");
            Tutorial.AddPage(page2);
            page2.windowTitle = "Mission Control";
            page2.OnDrawContent = () =>
            {
                GUILayout.Label("KSC, the controls are all locked up! That lightning took them out!");
            };
            page2.SetAdvanceCondition((KFSMState s) => FlightGlobals.ActiveVessel.missionTime > 18);

            TutorialPage page3 = new TutorialPage("Page3");
            Tutorial.AddPage(page3);
            page3.windowTitle = "Mission Control";
            page3.OnDrawContent = () =>
            {
                GUILayout.Label(FlightGlobals.ActiveVessel.vesselName + ", try SCE to AUX.\nI think it's on the Obscure Control Panel");
            };

            TutorialPage page4 = new TutorialPage("Page4");
            Tutorial.AddPage(page4);
            page4.windowTitle = "Mission Control";
            page4.OnDrawContent = () =>
            {
                GUILayout.BeginVertical();
                GUILayout.Label("That did it! The controls are back!");
                if (GUILayout.Button("Close")) SetDialogRect(new Rect(Screen.width + 100, 0, 0, 0));
                GUILayout.EndVertical();
            };
        }
    }
}
