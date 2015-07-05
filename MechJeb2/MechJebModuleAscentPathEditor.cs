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
        private Boolean pathTextureDrawnBefore = false;
        private MechJebModuleFlightRecorder recorder;
        private int lastHistoryIdx = 0;

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
            }

            GUILayout.BeginVertical();

            double oldTurnStartAltitude = path.turnStartAltitude;
            double oldTurnEndAltitude = path.turnEndAltitude;
            double oldTurnShapeExponent = path.turnShapeExponent;
            double oldTurnEndAngle = path.turnEndAngle;
            double oldAutoTurnPerc = path.autoTurnPerc;
            double oldAutoTurnSpdPerc = path.autoTurnSpdFactor;

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

            GUILayout.EndVertical();

            if (path.turnStartAltitude != oldTurnStartAltitude ||
                path.turnEndAltitude != oldTurnEndAltitude ||
                path.turnShapeExponent != oldTurnShapeExponent ||
                path.turnEndAngle != oldTurnEndAngle ||
                path.autoTurnPerc != oldAutoTurnPerc ||
                path.autoTurnSpdFactor != oldAutoTurnSpdPerc ||
                recorder.historyIdx != lastHistoryIdx ||
                !pathTextureDrawnBefore)
            {
                UpdatePathTexture();
            }

            base.WindowGUI(windowID);
        }

        //redraw the picture of the planned flight path
        private void UpdatePathTexture()
        {
            double scale = (path.autoPath ? path.autoTurnEndAltitude : path.turnEndAltitude) / pathTexture.height; //meters per pixel

            double maxAtmosphereAltitude = part.vessel.mainBody.RealMaxAtmosphereAltitude();
            for (int y = 0; y < pathTexture.height; y++)
            {
                Color c = new Color(0.0F, 0.0F, (float)Math.Max(0.0, 1.0 - y * scale / maxAtmosphereAltitude));

                for (int x = 0; x < pathTexture.width; x++)
                {
                    pathTexture.SetPixel(x, y, c);
                }
            }

            double alt = 0;
            double downrange = 0;
            while (alt < (path.autoPath ? path.autoTurnEndAltitude : path.turnEndAltitude) && downrange < pathTexture.width * scale)
            {
                double desiredAngle = (alt < path.VerticalAscentEnd() ? 90 : path.FlightPathAngle(alt, 0));
                alt += scale * Math.Sin(desiredAngle * Math.PI / 180);
                downrange += scale * Math.Cos(desiredAngle * Math.PI / 180);
                for (int x = (int)(downrange / scale); x <= (downrange / scale) + 2 && x < pathTexture.width; x++)
                {
                    for (int y = (int)(alt / scale) - 1; y <= (int)(alt / scale) + 1 && y < pathTexture.height; y++)
                    {
                        pathTexture.SetPixel(x, y, Color.red);
                    }
                }
            }

            int t = 0;
            while (recorder.historyIdx > 0 && t < recorder.historyIdx - 1 && t < recorder.history.Length)
            {
                var r1 = recorder.history[t];
                int x1 = (int)(r1.downRange / scale);
                int y1 = (int)(r1.altitudeASL / scale);

                var r2 = recorder.history[t + 1];
                int x2 = (int)(r2.downRange / scale);
                int y2 = (int)(r2.altitudeASL / scale);

                t++;

                if (x1 >= 0 && y1 >= 0 && x2 >= 0 && y2 >= 0 && x1 < pathTexture.width && y1 < pathTexture.height && x2 < pathTexture.width && y2 < pathTexture.height)
                    MuUtils.DrawLine(pathTexture, x1, y1, x2, y2, Color.white);
            }

            pathTexture.Apply();
            lastHistoryIdx = recorder.historyIdx;
            pathTextureDrawnBefore = true;
        }

        public override string GetName()
        {
            return "Ascent Path Editor";
        }
    }
}
