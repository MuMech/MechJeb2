using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleAscentPathEditor : DisplayModule
    {
        public MechJebModuleAscentPathEditor(MechJebCore core)
            : base(core)
        {
            hidden = true;
        }

        public DefaultAscentPath path;
        Texture2D pathTexture = new Texture2D(400, 100);

        public override void OnStart(PartModule.StartState state)
        {
            path = (DefaultAscentPath)core.GetComputerModule<MechJebModuleAscentGuidance>().ascentPath;
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(100) };
        }

        protected override void FlightWindowGUI(int windowID)
        {
            if (path == null)
            {
                GUILayout.Label("Path is null!!!1!!1!1!1111!11eleven");
                GUI.DragWindow();
            }

            GUILayout.BeginVertical();

            double oldTurnStartAltitude = path.turnStartAltitude;
            double oldTurnEndAltitude = path.turnEndAltitude;
            double oldTurnShapeExponent = path.turnShapeExponent;
            double oldTurnEndAngle = path.turnEndAngle;

            GuiUtils.SimpleTextBox("Turn start altitude:", path.turnStartAltitude, "km");
            GuiUtils.SimpleTextBox("Turn end altitude:", path.turnEndAltitude, "km");
            GuiUtils.SimpleTextBox("Final flight path angle:", path.turnEndAngle, "°");

            GUILayout.BeginHorizontal();
            GUILayout.Label(String.Format("Turn shape: {0:0}%", (int)(100 * path.turnShapeExponent)));
            path.turnShapeExponent = GUILayout.HorizontalSlider((float)path.turnShapeExponent, 0.0F, 1.0F);

            GUILayout.EndHorizontal();

            GUILayout.Box(pathTexture);

            GUILayout.EndVertical();

            if (path.turnStartAltitude != oldTurnStartAltitude ||
                path.turnEndAltitude != oldTurnEndAltitude ||
                path.turnShapeExponent != oldTurnShapeExponent ||
                path.turnEndAngle != oldTurnEndAngle)
            {
                UpdatePathTexture();
            }

            GUI.DragWindow();
        }

        //redraw the picture of the planned flight path
        private void UpdatePathTexture()
        {
            double scale = path.turnEndAltitude / pathTexture.height; //meters per pixel

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
            while (alt < path.turnEndAltitude && downrange < pathTexture.width * scale)
            {
                double desiredAngle = (alt < path.VerticalAscentEnd() ? 90 : path.FlightPathAngle(alt));
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
        }

    }
}
