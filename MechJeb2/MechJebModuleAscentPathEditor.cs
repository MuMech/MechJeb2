using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAscentPathEditor : DisplayModule
    {
        public MechJebModuleAscentPathEditor(MechJebCore core) : base(core)
        {
            hidden = true;
        }

        public DefaultAscentPath path;
        static Texture2D pathTexture = new Texture2D(400, 100);
        private Boolean pathTextureDrawnBefore = false;

        public override void OnStart(PartModule.StartState state)
        {
            path = (DefaultAscentPath)core.GetComputerModule<MechJebModuleAscentAutopilot>().ascentPath;
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

            GUILayout.BeginHorizontal();
            path.autoPath = GUILayout.Toggle(path.autoPath, "Automatic Altitude Turn", GUILayout.ExpandWidth(false));
            if (path.autoPath) path.autoTurnPerc = Mathf.Floor(GUILayout.HorizontalSlider(path.autoTurnPerc * 200f, 1f, 210.5f)) / 200f;
            // 1 to 200 / 200 = 0.5% to 105%, without this mess would the slider cause lots of garbage floats like 0.9999864
            GUILayout.EndHorizontal();

            if (path.autoPath)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Turn start altitude: ");
                GUILayout.Label(MuUtils.ToSI(path.autoTurnStartAltitude,-1,2) + "m", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight }, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Turn start velocity: ");
                GUILayout.Label(MuUtils.ToSI(path.autoTurnStartVelocity, -1, 0) + "m/s", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight }, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Turn end altitude: ");
                GUILayout.Label(MuUtils.ToSI(path.autoTurnEndAltitude,-1, 2) + "m", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight }, GUILayout.ExpandWidth(true));
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

            pathTexture.Apply();
            pathTextureDrawnBefore = true;
        }

        public override string GetName()
        {
            return "Ascent Path Editor";
        }
    }
}
