using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAscentPathEditor : DisplayModule
    {
        public MechJebModuleAscentPathEditor(MechJebCore core)
            : base(core)
        {
            hidden = true;
        }

        public DefaultAscentPath path;
        static Texture2D pathTexture = new Texture2D(400, 100);
        private MechJebModuleFlightRecorder recorder;
        private double lastMaxAtmosphereAltitude = -1;

        public override void OnStart(PartModule.StartState state)
        {
            path = (DefaultAscentPath)core.GetComputerModule<MechJebModuleAscentAutopilot>().ascentPath;
            recorder = core.GetComputerModule<MechJebModuleFlightRecorder>();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(100) };
        }

        protected override void WindowGUI(int windowID)
        {
            if (path == null)
            {
                GUILayout.Label("Path is null!!!1!!1!1!1111!11eleven");
                base.WindowGUI(windowID);
                return;
            }

            if (lastMaxAtmosphereAltitude != mainBody.RealMaxAtmosphereAltitude())
            {
                lastMaxAtmosphereAltitude = mainBody.RealMaxAtmosphereAltitude();
                UpdateAtmoTexture(pathTexture, mainBody, path.autoPath ? path.autoTurnEndAltitude : path.turnEndAltitude);
            }

            GUILayout.BeginVertical();

            double oldTurnShapeExponent = path.turnShapeExponent;

            path.autoPath = GUILayout.Toggle(path.autoPath, "Automatic Altitude Turn", GUILayout.ExpandWidth(false));
            if (path.autoPath)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Altitude: ", GUILayout.Width(60));
                // 1 to 200 / 200 = 0.5% to 105%, without this mess would the slider cause lots of garbage floats like 0.9999864
                path.autoTurnPerc = Mathf.Floor(GUILayout.HorizontalSlider(path.autoTurnPerc * 200f, 1f, 210.5f)) / 200f;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Velocity: ", GUILayout.Width(60));
                path.autoTurnSpdFactor = Mathf.Floor(GUILayout.HorizontalSlider(path.autoTurnSpdFactor * 2f, 8f, 160f)) / 2f;
                GUILayout.EndHorizontal();
            }

            if (path.autoPath)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Turn start when Altitude is ", GUILayout.ExpandWidth(false));
                GUILayout.Label(MuUtils.ToSI(path.autoTurnStartAltitude, -1, 2) + "m", GUILayout.ExpandWidth(false));
                GUILayout.Label("or Velocity reach ", GUILayout.ExpandWidth(false));
                GUILayout.Label(MuUtils.ToSI(path.autoTurnStartVelocity, -1, 3) + "m/s", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Turn end altitude: ");
                GUILayout.Label(MuUtils.ToSI(path.autoTurnEndAltitude, -1, 2) + "m", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight }, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
            }
            else
            {
                GuiUtils.SimpleTextBox("Turn start altitude:", path.turnStartAltitude, "km");
                GuiUtils.SimpleTextBox("Turn start velocity:", path.turnStartVelocity, "m/s");
                GuiUtils.SimpleTextBox("Turn end altitude:", path.turnEndAltitude, "km");
            }

            GuiUtils.SimpleTextBox("Final flight path angle:", path.turnEndAngle, "°");
            GuiUtils.SimpleTextBox("Turn shape:", path.turnShapeExponent, "%");

            // Round the slider's value (0..1) to sliderPrecision decimal places.
            const int sliderPrecision = 3;

            double sliderTurnShapeExponent = GUILayout.HorizontalSlider((float)path.turnShapeExponent, 0.0F, 1.0F);
            if (Math.Round(Math.Abs(sliderTurnShapeExponent - oldTurnShapeExponent), sliderPrecision) > 0)
            {
                path.turnShapeExponent = new EditableDoubleMult(Math.Round(sliderTurnShapeExponent, sliderPrecision), 0.01);
            }

            GUILayout.Box(pathTexture);

            if (Event.current.type == EventType.Repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                r.xMin += GUI.skin.box.margin.left;
                r.yMin += GUI.skin.box.margin.top;

                r.xMax -= GUI.skin.box.margin.right;
                r.yMax -= GUI.skin.box.margin.bottom;

                float scale = (float)((path.autoPath ? path.autoTurnEndAltitude : path.turnEndAltitude) / r.height);

                DrawnPath(r, scale, scale, path, Color.red);
                DrawnTrajectory(r, path, recorder);
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        //redraw the picture of the planned flight path
        public static void UpdateAtmoTexture(Texture2D texture, CelestialBody mainBody, double maxAltitude, bool realAtmo = false)
        {
            double scale = maxAltitude / texture.height; //meters per pixel

            double maxAtmosphereAltitude = mainBody.RealMaxAtmosphereAltitude();
            double pressureSeaLevel = mainBody.atmospherePressureSeaLevel;

            for (int y = 0; y < texture.height; y++)
            {
                double alt = scale * y;

                if (realAtmo)
                {
                    alt = mainBody.GetPressure(alt) / pressureSeaLevel;
                }
                else
                {
                    alt = 1.0 - alt / maxAtmosphereAltitude;
                }

                float v = (float)(mainBody.atmosphere ? alt : 0.0F);
                Color c = new Color(0.0F, 0.0F, v);

                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, c);

                    if (mainBody.atmosphere && (int)(maxAtmosphereAltitude / scale) == y)
                        texture.SetPixel(x, y, XKCDColors.LightGreyBlue);
                }
            }

            texture.Apply();
        }

        public static void DrawnPath(Rect r, float scaleX, float scaleY, DefaultAscentPath path, Color color)
        {

            float alt = 0;
            float downrange = 0;
            Vector2 p1 = new Vector2(r.xMin, r.yMax);
            Vector2 p2 = new Vector2();

            while (alt < (path.autoPath ? path.autoTurnEndAltitude : path.turnEndAltitude) && downrange < r.width * scaleX)
            {
                float desiredAngle = (float)(alt < path.VerticalAscentEnd() ? 90 : path.FlightPathAngle(alt, 0));

                alt += scaleY * Mathf.Sin(desiredAngle * Mathf.Deg2Rad);
                downrange += scaleX * Mathf.Cos(desiredAngle * Mathf.Deg2Rad);

                p2.x = r.xMin + downrange / scaleX;
                p2.y = r.yMax - alt / scaleY;

                if ((p1 - p2).sqrMagnitude >= 1.0)
                {
                    Drawing.DrawLine(p1, p2, color, 2, true);
                    p1.x = p2.x;
                    p1.y = p2.y;
                }
            }
        }

        private static void DrawnTrajectory(Rect r, DefaultAscentPath path, MechJebModuleFlightRecorder recorder)
        {
            if (recorder.history.Length <= 2 || recorder.historyIdx == 0)
                return;

            float scale = (float)((path.autoPath ? path.autoTurnEndAltitude : path.turnEndAltitude) / r.height); //meters per pixel

            int t = 1;

            Vector2 p1 = new Vector2(r.xMin + (float)(recorder.history[0].downRange / scale), r.yMax - (float)(recorder.history[0].altitudeASL / scale));
            Vector2 p2 = new Vector2();

            while (t <= recorder.historyIdx && t < recorder.history.Length)
            {
                var rec = recorder.history[t];
                p2.x = r.xMin + (float)(rec.downRange / scale);
                p2.y = r.yMax - (float)(rec.altitudeASL / scale);

                if (r.Contains(p2) && (p1 - p2).sqrMagnitude >= 1.0 || t < 2)
                {
                    Drawing.DrawLine(p1, p2, Color.white, 2, true);
                    p1.x = p2.x;
                    p1.y = p2.y;
                }

                t++;
            }
        }

        public override string GetName()
        {
            return "Ascent Path Editor";
        }
    }
}
