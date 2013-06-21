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
            priority = -10000;
            enabled = false;
            hidden = true;
        }

        [ToggleInfoItem("Enable 2013-04-01 Joke module", InfoItem.Category.Misc, showInEditor = true), Persistent(pass = (int)Pass.Global)]
        public bool enableJoke
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
            }
        }

        static Texture2D _lightningTex;
        static Texture2D lightningTex
        {
            get
            {
                if (_lightningTex == null)
                {
                    _lightningTex = new Texture2D(1, 1);
                    _lightningTex.SetPixel(0, 0, Color.white);
                    _lightningTex.Apply();
                }
                return _lightningTex;
            }
        }

        static AssetBundleCreateRequest lightningBundle = null;

        static AudioSource _lightningSound;
        AudioSource lightningSound
        {
            get
            {
                if (_lightningSound == null)
                {
                    _lightningSound = part.gameObject.AddComponent<AudioSource>();
                    _lightningSound.playOnAwake = false;
                    _lightningSound.clip = (AudioClip)lightningBundle.assetBundle.Load("lightning", typeof(AudioClip));
                }
                return _lightningSound;
            }
        }

        SpeechBubble bubble = null;

        public System.Random rand = new System.Random();

        public bool newFlight = false;

        public bool doLightningJoke = false;
        public bool doRotationJoke = false;

        public MissionControlTutorial tutorial;
        public bool startedTutorial = false;

        public bool lockedControls = false;
        public bool disabledSelf = false;

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);
            //it's no longer April 1, so force this module to always start disabled
            //it can still be enabled with the toggle.
            enabled = false; 
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (!enabled)
            {
                return;
            }

            if ((state & PartModule.StartState.PreLaunch) != 0)
            {
                newFlight = true;

                doLightningJoke = rand.Next(10) == 0;
                if (doLightningJoke) tutorial = (MissionControlTutorial)ScenarioRunner.fetch.AddModule("MissionControlTutorial");

                doRotationJoke = rand.Next(10) == 0;

                if (doRotationJoke)
                {
                    double[] rotationPeriods = { 1600, 1554, 1500, 1400, 1300, 1200, 1100, 1000, 750, 500, 250, 100, 50, 25, 10, 5, 1 };

                    vessel.mainBody.rotationPeriod = rotationPeriods[rand.Next(rotationPeriods.Length)];
                }

                if (lightningBundle == null)
                {
                    lightningBundle = AssetBundle.CreateFromMemory(Properties.Resources.lightning);
                }
            }

            if (state != PartModule.StartState.Editor)
            {
                Material munmat = GameObject.Find("scaledSpace").transform.Find("Mun").gameObject.renderer.material;

                if (munmat.mainTexture.name != "muntroll")
                {
                    Texture2D troll = new Texture2D(1024, 512, TextureFormat.ARGB32, true);
                    troll.LoadImage(Properties.Resources.troll);
                    troll.name = "muntroll";

                    munmat.mainTexture = troll;
                }
            }

            UnlockControls(); //in case the lock somehow remains in place
        }

        public override void DrawGUI(bool inEditor)
        {
            if (inEditor) return;

            if (doLightningJoke) RunLightningJoke();
        }

        void RunLightningJoke()
        {
            if (newFlight)
            {
                if (bubble == null)
                {
                    GUIStyle txt = new GUIStyle(GUI.skin.label);
                    txt.normal.textColor = Color.black;
                    txt.alignment = TextAnchor.MiddleCenter;
                    bubble = new SpeechBubble(txt);
                    bubble.bubbleHeight = 100;
                    bubble.offsetY = 75;
                    bubble.offsetX = 50;
                    bubble.bubbleWidth = 250;
                }

                if (vessel.missionTime > 10 && vessel.missionTime < 13)
                {
                    DrawLightning();

                    if (!lightningSound.isPlaying)
                    {
                        lightningSound.Play();
                    }

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

                    if ((vessel.GetCrewCount() > 0) && (vessel.missionTime > 18) && (vessel.missionTime < 22))
                    {
                        bubble.drawBubble(new Vector2(vessel.GetVesselCrew()[0].KerbalRef.screenPos.x + (vessel.GetVesselCrew()[0].KerbalRef.avatarSize / 2), vessel.GetVesselCrew()[0].KerbalRef.screenPos.y), "KSC, the controls are all locked up!\nThat lightning took them out!", Color.white);
                    }

                    if (vessel.missionTime > 18 && !disabledSelf)
                    {
                        core.GetComputerModule<MechJebModuleJokeObscurePanel>().enabled = false;
                        core.GetComputerModule<MechJebModuleJokeObscurePanel>().hidden = false;
                        disabledSelf = true;
                    }

                    if ((vessel.GetCrewCount() > 0) && (!core.GetComputerModule<MechJebModuleJokeObscurePanel>().t4) && (vessel.missionTime > 24))
                    {
                        bubble.drawBubble(new Vector2(vessel.GetVesselCrew()[0].KerbalRef.screenPos.x + (vessel.GetVesselCrew()[0].KerbalRef.avatarSize / 2), vessel.GetVesselCrew()[0].KerbalRef.screenPos.y), "Set SCE to AUX on the Obscure Control Panel!", Color.white);
                    }
                }
            }
        }

        string lockID = "MechJebJokeLock";
        public void LockControls()
        {
            InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS, lockID);
        }

        public void UnlockControls()
        {
            InputLockManager.RemoveControlLock(lockID);
        }

        public override void OnDestroy()
        {
            UnlockControls();
        }

        void DrawLightning()
        {
            if (rand.NextDouble() < 0.2)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), lightningTex);
            }
        }
    }

    public class MechJebModuleJokeObscurePanel : DisplayModule
    {
        public MechJebModuleJokeObscurePanel(MechJebCore core)
            : base(core)
        {
            hidden = true;
            enabled = false;
        }

        public bool t1 = true, t2 = false, t3 = false, t4 = false, t5 = true;
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
                core.GetComputerModule<MechJebModuleJoke>().UnlockControls();
                core.GetComputerModule<MechJebModuleJoke>().tutorial.FinishTutorial();
            }
            if (!t5)
            {
                if (core.GetComputerModule<MechJebModuleJoke>().rand.NextDouble() < TimeWarp.fixedDeltaTime / 5) vessel.parts[core.GetComputerModule<MechJebModuleJoke>().rand.Next(vessel.parts.Count)].explode();
            }

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(150), GUILayout.Height(50) };
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
                GUILayout.Label("What the---?\nDid that lightning just score a direct hit on the " + FlightGlobals.ActiveVessel.vesselName + "?\nOh no...");
            };

            if (FlightGlobals.ActiveVessel.GetCrewCount() == 0)
            {
                page1.SetAdvanceCondition((KFSMState s) => FlightGlobals.ActiveVessel.missionTime > 18);

                TutorialPage page2 = new TutorialPage("Page2");
                Tutorial.AddPage(page2);
                page2.windowTitle = "Mission Control";
                page2.OnDrawContent = () =>
                {
                    GUILayout.Label("The controls are all locked up!\nThat lightning took them out!");
                };
                page2.SetAdvanceCondition((KFSMState s) => FlightGlobals.ActiveVessel.missionTime > 22);
            }
            else
            {
                page1.SetAdvanceCondition((KFSMState s) => FlightGlobals.ActiveVessel.missionTime > 22);
            }

            TutorialPage page3 = new TutorialPage("Page3");
            Tutorial.AddPage(page3);
            page3.windowTitle = "Mission Control";
            page3.OnDrawContent = () =>
            {
                GUILayout.Label("...Hold on, " + FlightGlobals.ActiveVessel.vesselName + ", someone here thinks they have a solution.\n" +
                                "Try \"SCE to AUX\". Supposedly it's on the Obscure Control Panel. Funny, I've never heard of it.");
            };

            TutorialPage page4 = new TutorialPage("Page4");
            Tutorial.AddPage(page4);
            page4.windowTitle = "Mission Control";
            page4.OnDrawContent = () =>
            {
                GUILayout.BeginVertical();
                GUILayout.Label("That did it!\nThe controls are back!");
                if (GUILayout.Button("Close")) SetDialogRect(new Rect(Screen.width + 100, 0, 0, 0));
                GUILayout.EndVertical();
            };
        }
    }
}
