using System;
using UnityEngine;
using Object = UnityEngine.Object;
using recordType = MuMech.MechJebModuleFlightRecorder.recordType;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleFlightRecorderGraph : DisplayModule
    {
        private const int ScaleTicks = 11;

        public struct graphState
        {
            public double minimum;
            public double maximum;
            public string[] labels;
            public double[] labelsPos;
            public int labelsActive;
            public bool display;

            public void Reset()
            {
                minimum = 0;
                maximum = 0;
                labels = new string[ScaleTicks];
                labelsPos = new double[ScaleTicks];

            }
        }

        [Persistent(pass = (int)Pass.Global)]
        public bool downrange = true;

        [Persistent(pass = (int)Pass.Global)]
        public bool realAtmo = false;

        [Persistent(pass = (int)Pass.Global)]
        public bool stages = false;

        [Persistent(pass = (int)Pass.Global)]
        public int hSize = 4;

        [Persistent(pass = (int)Pass.Global)]
        public int vSize = 2;

        [Persistent(pass = (int)Pass.Global)]
        public bool autoScale = true;

        [Persistent(pass = (int)Pass.Global)]
        public int timeScale = 0;

        [Persistent(pass = (int)Pass.Global)]
        public int downrangeScale = 0;

        [Persistent(pass = (int)Pass.Global)]
        public int scaleIdx = 0;

        public bool ascentPath = false;

        static Texture2D backgroundTexture;
        private CelestialBody oldMainBody;
        private static readonly int typeCount = Enum.GetValues(typeof(recordType)).Length;

        private readonly graphState[] graphStates;
        private double lastMaximumAltitude;
        private readonly double precision = 0.2;

        private int width = 512;
        private int height = 256;

        private bool paused = false;

        private float hPos = 0;

        private bool follow = true;

        private MechJebModuleFlightRecorder recorder;

        public MechJebModuleFlightRecorderGraph(MechJebCore core)
            : base(core)
        {
            priority = 2000;
            graphStates = new graphState[typeCount];
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                return;
            }

            width = 128 * hSize;
            height = 128 * vSize;
            recorder = core.GetComputerModule<MechJebModuleFlightRecorder>();
            ResetScale();
        }

        protected override void WindowGUI(int windowID)
        {
            //DefaultAscentPath path = (DefaultAscentPath)core.GetComputerModule<MechJebModuleAscentAutopilot>().ascentPath;

            if (oldMainBody != mainBody || lastMaximumAltitude != graphStates[(int)recordType.AltitudeASL].maximum || height != backgroundTexture.height)
            {
                UpdateScale();
                lastMaximumAltitude = graphStates[(int)recordType.AltitudeASL].maximum;

                if (backgroundTexture == null || height != backgroundTexture.height)
                {
                    Object.Destroy(backgroundTexture);
                    backgroundTexture = new Texture2D(1, height);
                }

                MechJebModuleAscentClassicMenu.UpdateAtmoTexture(backgroundTexture, vessel.mainBody, lastMaximumAltitude, realAtmo);
                oldMainBody = mainBody;
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(paused ? Localizer.Format("#MechJeb_Flightrecord_Button1_1") : Localizer.Format("#MechJeb_Flightrecord_Button1_2"), GUILayout.ExpandWidth(false)))//"Resume""Pause"
            {
                paused = !paused;
            }

            if (GUILayout.Button(downrange ? Localizer.Format("#MechJeb_Flightrecord_Button2_1") : Localizer.Format("#MechJeb_Flightrecord_Button2_2"), GUILayout.ExpandWidth(false)))//"Downrange""Time"
            {
                downrange = !downrange;
            }

            //GUILayout.Label("Size " + (8 * typeCount * recorder.history.Length >> 10).ToString() + "kB", GUILayout.ExpandWidth(false));

            GUILayout.Label(Localizer.Format("#MechJeb_Flightrecord_Label1", GuiUtils.TimeToDHMS(recorder.timeSinceMark)), GUILayout.ExpandWidth(false));//Time <<1>>

            GUILayout.Label(Localizer.Format("#MechJeb_Flightrecord_Label2", MuUtils.ToSI(recorder.history[recorder.historyIdx].downRange, -2)) + "m", GUILayout.ExpandWidth(false));//Downrange <<1>>

            //GUILayout.Label("", GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(Localizer.Format("#MechJeb_Flightrecord_Button3"), GUILayout.ExpandWidth(false)))//"Mark"
            {
                ResetScale(); // TODO : should check something else to catch Mark calls from other code
                recorder.Mark();
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_Flightrecord_Button4"), GUILayout.ExpandWidth(false)))//"Reset Scale"
            {
                ResetScale();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            autoScale = GUILayout.Toggle(autoScale, Localizer.Format("#MechJeb_Flightrecord_checkbox1"), GUILayout.ExpandWidth(false));//Auto Scale

            if (!autoScale && GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                if (downrange)
                {
                    downrangeScale--;
                }
                else
                {
                    timeScale--;
                }
            }

            var maxX = (float)(downrange ? recorder.maximums[(int)recordType.DownRange] : recorder.maximums[(int)recordType.TimeSinceMark]);

            var maxXScaled = (downrange ? maxX : maxX / precision) / width;
            var autoScaleX = Math.Max(Math.Ceiling(Math.Log(maxXScaled, 2)), 0);
            double manualScaleX = downrange ? downrangeScale : timeScale;
            var activeScaleX = autoScale ? autoScaleX : manualScaleX;

            var scaleX = downrange ? Math.Pow(2, activeScaleX) : precision * Math.Pow(2, activeScaleX);

            GUILayout.Label((downrange ? MuUtils.ToSI(scaleX, -1, 2) + "m/px" : GuiUtils.TimeToDHMS(scaleX, 1) + "/px"), GUILayout.ExpandWidth(false));

            if (!autoScale && GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                if (downrange)
                {
                    downrangeScale++;
                }
                else
                {
                    timeScale++;
                }
            }

            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                hSize--;
            }

            GUILayout.Label(width.ToString(), GUILayout.ExpandWidth(false));

            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                hSize++;
            }

            GUILayout.Label("x", GUILayout.ExpandWidth(false));

            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                vSize--;
            }

            GUILayout.Label(height.ToString(), GUILayout.ExpandWidth(false));

            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                vSize++;
            }

            timeScale = Mathf.Clamp(timeScale, 0, 20);
            downrangeScale = Mathf.Clamp(downrangeScale, 0, 20);
            hSize = Mathf.Clamp(hSize, 1, 20);
            vSize = Mathf.Clamp(vSize, 1, 10);

            var oldRealAtmo = realAtmo;

            realAtmo = GUILayout.Toggle(realAtmo, Localizer.Format("#MechJeb_Flightrecord_checkbox2"), GUILayout.ExpandWidth(false));//Real Atmo

            if (oldRealAtmo != realAtmo)
            {
                MechJebModuleAscentClassicMenu.UpdateAtmoTexture(backgroundTexture, vessel.mainBody, lastMaximumAltitude, realAtmo);
            }

            //GUILayout.Label("", GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(Localizer.Format("#MechJeb_Flightrecord_Button5"), GUILayout.ExpandWidth(false)))//CSV
            {
                recorder.DumpCSV();
            }

            GUILayout.Label(Localizer.Format("#MechJeb_Flightrecord_Label3", (100 * (recorder.historyIdx) / (float)recorder.history.Length).ToString("F1")), GUILayout.ExpandWidth(false));//Storage: <<1>> %

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            var color = GUI.color;

            // Blue, Navy, Teal, Magenta, Purple all have poor contrast on black or against other colors here
            // Maybe some of them could be lightened up, but many of the lighter variants are already in this list.

            //ascentPath = GUILayout.Toggle(ascentPath, "Ascent path", GUILayout.ExpandWidth(false));
            stages = GUILayout.Toggle(stages,Localizer.Format("#MechJeb_Flightrecord_checkbox3") , GUILayout.ExpandWidth(false));//"Stages"

            GUI.color = XKCDColors.White;
            graphStates[(int)recordType.AltitudeASL].display = GUILayout.Toggle(graphStates[(int)recordType.AltitudeASL].display, Localizer.Format("#MechJeb_Flightrecord_checkbox4"), GUILayout.ExpandWidth(false));//"Altitude"

            GUI.color = XKCDColors.Grey;
            graphStates[(int)recordType.AltitudeTrue].display = GUILayout.Toggle(graphStates[(int)recordType.AltitudeTrue].display, Localizer.Format("#MechJeb_Flightrecord_checkbox5"), GUILayout.ExpandWidth(false));//"True Altitude"

            GUI.color = XKCDColors.LightRed;
            graphStates[(int)recordType.Acceleration].display = GUILayout.Toggle(graphStates[(int)recordType.Acceleration].display, Localizer.Format("#MechJeb_Flightrecord_checkbox6"), GUILayout.ExpandWidth(false));//"Acceleration"

            GUI.color = XKCDColors.Yellow;
            graphStates[(int)recordType.SpeedSurface].display = GUILayout.Toggle(graphStates[(int)recordType.SpeedSurface].display, Localizer.Format("#MechJeb_Flightrecord_checkbox7"), GUILayout.ExpandWidth(false));//"Surface speed"

            GUI.color = XKCDColors.Apricot;
            graphStates[(int)recordType.SpeedOrbital].display = GUILayout.Toggle(graphStates[(int)recordType.SpeedOrbital].display, Localizer.Format("#MechJeb_Flightrecord_checkbox8"), GUILayout.ExpandWidth(false));//"Orbital speed"

            GUI.color = XKCDColors.Pink;
            graphStates[(int)recordType.Mass].display = GUILayout.Toggle(graphStates[(int)recordType.Mass].display, Localizer.Format("#MechJeb_Flightrecord_checkbox9"), GUILayout.ExpandWidth(false));//"Mass"

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            GUI.color = XKCDColors.Cyan;
            graphStates[(int)recordType.Q].display = GUILayout.Toggle(graphStates[(int)recordType.Q].display, Localizer.Format("#MechJeb_Flightrecord_checkbox10"), GUILayout.ExpandWidth(false));//"Q"

            GUI.color = XKCDColors.Lavender;
            graphStates[(int)recordType.AoA].display = GUILayout.Toggle(graphStates[(int)recordType.AoA].display, Localizer.Format("#MechJeb_Flightrecord_checkbox11"), GUILayout.ExpandWidth(false));//"AoA"

            GUI.color = XKCDColors.Lime;
            graphStates[(int)recordType.AoS].display = GUILayout.Toggle(graphStates[(int)recordType.AoS].display, Localizer.Format("#MechJeb_Flightrecord_checkbox12"), GUILayout.ExpandWidth(false));//"AoS"

            GUI.color = XKCDColors.Orange;
            graphStates[(int)recordType.AoD].display = GUILayout.Toggle(graphStates[(int)recordType.AoD].display, Localizer.Format("#MechJeb_Flightrecord_checkbox13"), GUILayout.ExpandWidth(false));//"AoD"

            GUI.color = XKCDColors.Mint;
            graphStates[(int)recordType.Pitch].display = GUILayout.Toggle(graphStates[(int)recordType.Pitch].display, Localizer.Format("#MechJeb_Flightrecord_checkbox14"), GUILayout.ExpandWidth(false));//"Pitch"

            GUI.color = XKCDColors.Beige;
            graphStates[(int)recordType.DeltaVExpended].display = GUILayout.Toggle(graphStates[(int)recordType.DeltaVExpended].display, "∆V", GUILayout.ExpandWidth(false));

            GUI.color = XKCDColors.Green;
            graphStates[(int)recordType.GravityLosses].display = GUILayout.Toggle(graphStates[(int)recordType.GravityLosses].display, Localizer.Format("#MechJeb_Flightrecord_checkbox15"), GUILayout.ExpandWidth(false));//"Gravity Loss"

            GUI.color = XKCDColors.LightBrown;
            graphStates[(int)recordType.DragLosses].display = GUILayout.Toggle(graphStates[(int)recordType.DragLosses].display,  Localizer.Format("#MechJeb_Flightrecord_checkbox16"), GUILayout.ExpandWidth(false));//"Drag Loss"

            GUI.color = XKCDColors.Cerise;
            graphStates[(int)recordType.SteeringLosses].display = GUILayout.Toggle(graphStates[(int)recordType.SteeringLosses].display, Localizer.Format("#MechJeb_Flightrecord_checkbox17"), GUILayout.ExpandWidth(false));//"Steering Loss"

            GUI.color = color;

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            GUILayout.Space(50);

            GUILayout.Box(Texture2D.blackTexture, GUILayout.Width(width), GUILayout.Height(height));
            var r = GUILayoutUtility.GetLastRect();

            DrawScaleLabels(r);

            GUILayout.BeginVertical();

            //GUILayout.Label("X " + MuUtils.ToSI(hPos, -1, 2) + " " + MuUtils.ToSI(hPos + scaleX * width, -1, 2), GUILayout.Width(110));

            color = GUI.color;

            if (!graphStates[scaleIdx].display)
            {
                var newIdx = 0;
                while (newIdx < typeCount && !graphStates[newIdx].display)
                {
                    newIdx++;
                }
                if (newIdx == typeCount)
                {
                    newIdx = 0;
                }

                scaleIdx = newIdx;
            }

            if (graphStates[(int)recordType.AltitudeASL].display)
            {
                GUI.color = XKCDColors.White;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.AltitudeASL, "ASL " + MuUtils.ToSI(graphStates[(int)recordType.AltitudeASL].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.AltitudeASL].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.AltitudeASL, Localizer.Format("#MechJeb_Flightrecord_checkbox18"), GUILayout.ExpandWidth(true)))//"ASL"
                {
                    scaleIdx = (int)recordType.AltitudeASL;
                }
            }

            if (graphStates[(int)recordType.AltitudeTrue].display)
            {
                GUI.color = XKCDColors.Grey;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.AltitudeTrue, "AGL " + MuUtils.ToSI(graphStates[(int)recordType.AltitudeTrue].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.AltitudeTrue].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.AltitudeTrue, Localizer.Format("#MechJeb_Flightrecord_checkbox19"), GUILayout.ExpandWidth(true)))//"AGL"
                {
                    scaleIdx = (int)recordType.AltitudeTrue;
                }
            }
            if (graphStates[(int)recordType.Acceleration].display)
            {
                GUI.color = XKCDColors.LightRed;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.Acceleration, "Acc " + MuUtils.ToSI(graphStates[(int)recordType.Acceleration].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.Acceleration].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.Acceleration, Localizer.Format("#MechJeb_Flightrecord_checkbox20"), GUILayout.ExpandWidth(true)))// "Acc"
                {
                    scaleIdx = (int)recordType.Acceleration;
                }
            }
            if (graphStates[(int)recordType.SpeedSurface].display)
            {
                GUI.color = XKCDColors.Yellow;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.SpeedSurface, "SrfVel " + MuUtils.ToSI(graphStates[(int)recordType.SpeedSurface].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.SpeedSurface].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.SpeedSurface, Localizer.Format("#MechJeb_Flightrecord_checkbox21"), GUILayout.ExpandWidth(true)))//"SrfVel"
                {
                    scaleIdx = (int)recordType.SpeedSurface;
                }
            }
            if (graphStates[(int)recordType.SpeedOrbital].display)
            {
                GUI.color = XKCDColors.Apricot;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.SpeedOrbital, "ObtVel " + MuUtils.ToSI(graphStates[(int)recordType.SpeedOrbital].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.SpeedOrbital].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.SpeedOrbital, Localizer.Format("#MechJeb_Flightrecord_checkbox22"), GUILayout.ExpandWidth(true)))//"ObtVel"
                {
                    scaleIdx = (int)recordType.SpeedOrbital;
                }
            }
            if (graphStates[(int)recordType.Mass].display)
            {
                GUI.color = XKCDColors.Pink;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.Mass, "Mass " + MuUtils.ToSI(graphStates[(int)recordType.Mass].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.Mass].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.Mass, Localizer.Format("#MechJeb_Flightrecord_checkbox23"), GUILayout.ExpandWidth(true)))//"Mass"
                {
                    scaleIdx = (int)recordType.Mass;
                }
            }
            if (graphStates[(int)recordType.Q].display)
            {
                GUI.color = XKCDColors.Cyan;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.Q, "Q " + MuUtils.ToSI(graphStates[(int)recordType.Q].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.Q].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.Q, Localizer.Format("#MechJeb_Flightrecord_checkbox24"), GUILayout.ExpandWidth(true)))//"Q"
                {
                    scaleIdx = (int)recordType.Q;
                }
            }
            if (graphStates[(int)recordType.AoA].display)
            {
                GUI.color = XKCDColors.Lavender;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.AoA, "AoA " + MuUtils.ToSI(graphStates[(int)recordType.AoA].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.AoA].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.AoA, Localizer.Format("#MechJeb_Flightrecord_checkbox25"), GUILayout.ExpandWidth(true)))//"AoA"
                {
                    scaleIdx = (int)recordType.AoA;
                }
            }
            if (graphStates[(int)recordType.AoS].display)
            {
                GUI.color = XKCDColors.Lime;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.AoS, "AoS " + MuUtils.ToSI(graphStates[(int)recordType.AoS].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.AoS].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.AoS, Localizer.Format("#MechJeb_Flightrecord_checkbox26"), GUILayout.ExpandWidth(true)))//"AoS"
                {
                    scaleIdx = (int)recordType.AoS;
                }
            }
            if (graphStates[(int)recordType.AoD].display)
            {
                GUI.color = XKCDColors.Orange;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.AoD, "AoD " + MuUtils.ToSI(graphStates[(int)recordType.AoD].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.AoD].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.AoD, Localizer.Format("#MechJeb_Flightrecord_checkbox27"), GUILayout.ExpandWidth(true)))//"AoD"
                {
                    scaleIdx = (int)recordType.AoD;
                }
            }
            if (graphStates[(int)recordType.Pitch].display)
            {
                GUI.color = XKCDColors.Mint;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.Pitch, "Pitch " + MuUtils.ToSI(graphStates[(int)recordType.Pitch].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.Pitch].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.Pitch, Localizer.Format("#MechJeb_Flightrecord_checkbox28"), GUILayout.ExpandWidth(true)))//"Pitch"
                {
                    scaleIdx = (int)recordType.Pitch;
                }
            }
            if (graphStates[(int)recordType.DeltaVExpended].display)
            {
                GUI.color = XKCDColors.Beige;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.DeltaVExpended, "DeltaVExpended " + MuUtils.ToSI(graphStates[(int)recordType.DeltaVExpended].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.DeltaVExpended].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.DeltaVExpended, "∆V", GUILayout.ExpandWidth(true)))
                {
                    scaleIdx = (int)recordType.DeltaVExpended;
                }
            }
            if (graphStates[(int)recordType.GravityLosses].display)
            {
                GUI.color = XKCDColors.Green;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.GravityLosses, "GravityLosses " + MuUtils.ToSI(graphStates[(int)recordType.GravityLosses].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.GravityLosses].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.GravityLosses, Localizer.Format("#MechJeb_Flightrecord_checkbox29"), GUILayout.ExpandWidth(true)))//"Gravity Loss"
                {
                    scaleIdx = (int)recordType.GravityLosses;
                }
            }
            if (graphStates[(int)recordType.DragLosses].display)
            {
                GUI.color = XKCDColors.LightBrown;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.DragLosses, "DragLosses " + MuUtils.ToSI(graphStates[(int)recordType.DragLosses].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.DragLosses].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.DragLosses, Localizer.Format("#MechJeb_Flightrecord_checkbox30"), GUILayout.ExpandWidth(true)))//"Drag Loss"
                {
                    scaleIdx = (int)recordType.DragLosses;
                }
            }
            if (graphStates[(int)recordType.SteeringLosses].display)
            {
                GUI.color = XKCDColors.Cerise;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.SteeringLosses, "SteeringLosses " + MuUtils.ToSI(graphStates[(int)recordType.SteeringLosses].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.SteeringLosses].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.SteeringLosses, Localizer.Format("#MechJeb_Flightrecord_checkbox31"), GUILayout.ExpandWidth(true)))//"Steering Loss"
                {
                    scaleIdx = (int)recordType.SteeringLosses;
                }
            }

            GUI.color = color;

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            var visibleX = (float)(width * scaleX);
            var rightValue = Mathf.Max(visibleX, maxX);

            if (follow)
            {
                hPos = rightValue - visibleX;
            }

            hPos = GUILayout.HorizontalScrollbar(hPos, visibleX, 0, rightValue);
            follow = GUILayout.Toggle(follow, "", GUILayout.ExpandWidth(false));

            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint)
            {
                UpdateScale();

                if (graphStates[(int)recordType.AltitudeASL].display || graphStates[(int)recordType.AltitudeTrue].display)
                {
                    GUI.DrawTexture(r, backgroundTexture, ScaleMode.StretchToFill);
                }

                if (stages)
                {
                    DrawnStages(r, scaleX, downrange);
                }

                if (graphStates[(int)recordType.AltitudeASL].display)
                {
                    DrawnPath(r, recordType.AltitudeASL, hPos, scaleX, downrange, XKCDColors.White);
                }

                if (graphStates[(int)recordType.AltitudeTrue].display)
                {
                    DrawnPath(r, recordType.AltitudeTrue, hPos, scaleX, downrange, XKCDColors.Grey);
                }

                if (graphStates[(int)recordType.Acceleration].display)
                {
                    DrawnPath(r, recordType.Acceleration, hPos, scaleX, downrange, XKCDColors.LightRed);
                }

                if (graphStates[(int)recordType.SpeedSurface].display)
                {
                    DrawnPath(r, recordType.SpeedSurface, hPos, scaleX, downrange, XKCDColors.Yellow);
                }

                if (graphStates[(int)recordType.SpeedOrbital].display)
                {
                    DrawnPath(r, recordType.SpeedOrbital, hPos, scaleX, downrange, XKCDColors.Apricot);
                }

                if (graphStates[(int)recordType.Mass].display)
                {
                    DrawnPath(r, recordType.Mass, hPos, scaleX, downrange, XKCDColors.Pink);
                }

                if (graphStates[(int)recordType.Q].display)
                {
                    DrawnPath(r, recordType.Q, hPos, scaleX, downrange, XKCDColors.Cyan);
                }

                if (graphStates[(int)recordType.AoA].display)
                {
                    DrawnPath(r, recordType.AoA, hPos, scaleX, downrange, XKCDColors.Lavender);
                }

                if (graphStates[(int)recordType.AoS].display)
                {
                    DrawnPath(r, recordType.AoS, hPos, scaleX, downrange, XKCDColors.Lime);
                }

                if (graphStates[(int)recordType.AoD].display)
                {
                    DrawnPath(r, recordType.AoD, hPos, scaleX, downrange, XKCDColors.Orange);
                }

                if (graphStates[(int)recordType.Pitch].display)
                {
                    DrawnPath(r, recordType.Pitch, hPos, scaleX, downrange, XKCDColors.Mint);
                }

                if (graphStates[(int)recordType.DeltaVExpended].display)
                {
                    DrawnPath(r, recordType.DeltaVExpended, hPos, scaleX, downrange, XKCDColors.Beige);
                }

                if (graphStates[(int)recordType.GravityLosses].display)
                {
                    DrawnPath(r, recordType.GravityLosses, hPos, scaleX, downrange, XKCDColors.Green);
                }

                if (graphStates[(int)recordType.DragLosses].display)
                {
                    DrawnPath(r, recordType.DragLosses, hPos, scaleX, downrange, XKCDColors.LightBrown);
                }

                if (graphStates[(int)recordType.SteeringLosses].display)
                {
                    DrawnPath(r, recordType.SteeringLosses, hPos, scaleX, downrange, XKCDColors.Cerise);
                }

                // Fix : the scales are different so the result is not useful
                //if (ascentPath)
                //    MechJebModuleAscentPathEditor.DrawnPath(r, (float)hScale, (float)graphStates[(int)recordType.AltitudeASL].scale, path, Color.gray);

                width = 128 * hSize;
                height = 128 * vSize;
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        private void DrawScaleLabels(Rect r)
        {
            if (scaleIdx == 0)
            {
                return;
            }

            const int w = 80;
            const int h = 20;
            var centeredStyle = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleRight};

            var state = graphStates[scaleIdx];
            if (state.labels == null)
            {
                return;
            }

            var count = state.labelsActive;
            var invScaleY = height / (state.maximum - state.minimum);
            var yBase = r.yMax + (float) (state.minimum * invScaleY);
            for (var i = 0; i < count; i++)
            {
                GUI.Label(new Rect(r.xMin - w, yBase - (float)(invScaleY * state.labelsPos[i]) - (h * 0.5f), w, h), state.labels[i], centeredStyle);
            }
        }

        private void DrawnPath(Rect r, recordType type, float minimum, double scaleX, bool downRange, Color color)
        {
            if (recorder.history.Length <= 2 || recorder.historyIdx == 0)
            {
                return;
            }

            var graphState = graphStates[(int)type];

            var scaleY = (graphState.maximum - graphState.minimum) / height;

            var invScaleX = 1 / scaleX;
            var invScaleY = 1 / scaleY;

            var xBase = (float) (r.xMin - (minimum * invScaleX));
            var yBase = r.yMax + (float)(graphState.minimum * invScaleY);

            var t = 0;
            while (t < recorder.historyIdx && t < recorder.history.Length &&
                (xBase + (float)((downRange ? recorder.history[t].downRange : recorder.history[t].timeSinceMark) * invScaleX)) <= r.xMin)
            {
                t++;
            }

            var p1 = new Vector2(xBase + (float)((downRange ? recorder.history[t].downRange : recorder.history[t].timeSinceMark) * invScaleX), yBase - (float)(recorder.history[t][type] * invScaleY));
            var p2 = new Vector2();

            while (t <= recorder.historyIdx && t < recorder.history.Length)
            {
                var rec = recorder.history[t];
                p2.x = xBase + (float)((downRange ? rec.downRange : rec.timeSinceMark) * invScaleX);
                p2.y = yBase - (float)(rec[type] * invScaleY);

                // skip 0 length line but always drawn the first 2 points
                if (r.Contains(p2) && ((p1 - p2).sqrMagnitude >= 1.0 || t < 2))
                {
                    Drawing.DrawLine(p1, p2, color, 2, true);
                    p1.x = p2.x;
                    p1.y = p2.y;
                }
                t++;
            }
        }

        private void DrawnStages(Rect r, double scaleX, bool downRange)
        {
            if (recorder.history.Length <= 2 || recorder.historyIdx == 0)
            {
                return;
            }

            var lastStage = recorder.history[0].currentStage;

            var p1 = new Vector2(0, r.yMin);
            var p2 = new Vector2(0, r.yMax);

            for (var t = 1; t <= recorder.historyIdx && t < recorder.history.Length; t++)
            {
                var rec = recorder.history[t];
                if (rec.currentStage != lastStage)
                {
                    lastStage = rec.currentStage;
                    p1.x = r.xMin + (float)((downRange ? rec.downRange : rec.timeSinceMark) / scaleX);
                    p2.x = p1.x;

                    if (r.Contains(p1))
                    {
                        Drawing.DrawLine(p1, p2, new Color(0.5f, 0.5f, 0.5f), 1, false);
                    }
                }
            }
        }

        private void UpdateScale()
        {
            if (recorder.historyIdx == 0)
            {
                ResetScale();
            }

            for (var t = 0; t < typeCount; t++)
            {
                var change = false;

                if (graphStates[t].maximum < recorder.maximums[t])
                {
                    change = true;
                    graphStates[t].maximum = recorder.maximums[t] + Math.Abs(recorder.maximums[t] * 0.2);
                }

                if (graphStates[t].minimum > recorder.minimums[t])
                {
                    change = true;
                    graphStates[t].minimum = recorder.minimums[t] - Math.Abs(recorder.minimums[t] * 0.2);
                }

                if (graphStates[t].labels == null)
                {
                    change = true;
                    graphStates[t].labels = new string[ScaleTicks];
                    graphStates[t].labelsPos = new double[ScaleTicks];
                }

                if (change)
                {
                    var maximum = graphStates[t].maximum;
                    var minimum = graphStates[t].minimum;
                    var range = heckbertNiceNum(maximum - minimum, false);
                    var step = heckbertNiceNum(range / (ScaleTicks - 1), true);

                    minimum = Math.Floor(minimum / step) * step;
                    maximum = Math.Ceiling(maximum / step) * step;
                    var digit = (int)Math.Max(-Math.Floor(Math.Log10(step)), 0);

                    var currX = minimum;
                    var i = 0;
                    while (currX <= maximum + (0.5 * step))
                    {
                        graphStates[t].labels[i] = currX.ToString("F" + digit);
                        graphStates[t].labelsPos[i] = currX;
                        currX += step;
                        i++;
                    }

                    graphStates[t].labelsActive = i;
                    graphStates[t].minimum = minimum;
                    graphStates[t].maximum = maximum;
                }
            }
        }

        private void ResetScale()
        {
            // Avoid min = max and set sane minimums
            graphStates[(int)recordType.AltitudeASL].minimum = 0;
            graphStates[(int)recordType.DownRange].minimum = 0;
            graphStates[(int)recordType.Acceleration].minimum = 0;
            graphStates[(int)recordType.SpeedSurface].minimum = 0;
            graphStates[(int)recordType.SpeedOrbital].minimum = 0;
            graphStates[(int)recordType.Mass].minimum = 0;
            graphStates[(int)recordType.Q].minimum = 0;
            graphStates[(int)recordType.AoA].minimum = -5;
            graphStates[(int)recordType.AoS].minimum = -5;
            graphStates[(int)recordType.AoD].minimum = 0; // is never negative
            graphStates[(int)recordType.AltitudeTrue].minimum = 0;
            graphStates[(int)recordType.Pitch].minimum = 0;
            graphStates[(int)recordType.DeltaVExpended].minimum = 0;
            graphStates[(int)recordType.GravityLosses].minimum = 0;
            graphStates[(int)recordType.DragLosses].minimum = 0;
            graphStates[(int)recordType.SteeringLosses].minimum = 0;

            graphStates[(int)recordType.AltitudeASL].maximum = mainBody?.atmosphere == true ? mainBody.RealMaxAtmosphereAltitude() : 10000.0;
            graphStates[(int)recordType.DownRange].maximum = 500;
            graphStates[(int)recordType.Acceleration].maximum = 2;
            graphStates[(int)recordType.SpeedSurface].maximum = 300;
            graphStates[(int)recordType.SpeedOrbital].maximum = 300;
            graphStates[(int)recordType.Mass].maximum = 5;
            graphStates[(int)recordType.Q].maximum = 1000;
            graphStates[(int)recordType.AoA].maximum = 5;
            graphStates[(int)recordType.AoS].maximum = 5;
            graphStates[(int)recordType.AoD].maximum = 5;
            graphStates[(int)recordType.AltitudeTrue].maximum = 100;
            graphStates[(int)recordType.Pitch].maximum = 90;
            graphStates[(int)recordType.DeltaVExpended].maximum = 100;
            graphStates[(int)recordType.GravityLosses].maximum = 100;
            graphStates[(int)recordType.DragLosses].maximum = 100;
            graphStates[(int)recordType.SteeringLosses].maximum = 100;
        }

        private double heckbertNiceNum(double x, bool round)
        {
            var exp = (int)Math.Log10(x);
            var f = x / (Math.Pow(10.0, exp));
            double nf = 1;

            if (round)
            {
                if (f < 1.5)
                {
                    nf = 1;
                }
                else if (f < 3)
                {
                    nf = 2;
                }
                else if (f < 7)
                {
                    nf = 5;
                }
                else
                {
                    nf = 10;
                }
            }
            else
            {
                if (f <= 1)
                {
                    nf = 1;
                }
                else if (f <= 2)
                {
                    nf = 2;
                }
                else if (f <= 5)
                {
                    nf = 5;
                }
                else
                {
                    nf = 10;
                }
            }
            return nf * Math.Pow(10.0, exp);

        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(400), GUILayout.Height(300) };
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_Flightrecord_title");//"Flight Recorder"
        }

        public override string IconName()
        {
            return "Flight Recorder";
        }
    }
}
