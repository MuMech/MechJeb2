using System;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAscentClassicPathMenu : DisplayModule
    {
        public MechJebModuleAscentClassicPathMenu(MechJebCore core)
            : base(core)
        {
            hidden = true;
        }

        private                 MechJebModuleAscentSettings _ascentSettings;
        private                 MechJebModuleAscentClassicAutopilot  _path;
        private static readonly Texture2D                   _pathTexture = new Texture2D(400, 100);
        private                 MechJebModuleFlightRecorder _recorder;
        private                 double                      _lastMaxAtmosphereAltitude = -1;

        public override void OnStart(PartModule.StartState state)
        {
            _recorder       = core.GetComputerModule<MechJebModuleFlightRecorder>();
            _ascentSettings = core.GetComputerModule<MechJebModuleAscentSettings>();
            _path           = core.GetComputerModule<MechJebModuleAscentClassicAutopilot>();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new[] { GUILayout.Width(300), GUILayout.Height(100) };
        }

        protected override void WindowGUI(int windowID)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_lastMaxAtmosphereAltitude != mainBody.RealMaxAtmosphereAltitude())
            {
                _lastMaxAtmosphereAltitude = mainBody.RealMaxAtmosphereAltitude();
                UpdateAtmoTexture(_pathTexture, mainBody,
                    _ascentSettings.AutoPath ? _ascentSettings.AutoTurnEndAltitude : _ascentSettings.TurnEndAltitude);
            }

            GUILayout.BeginVertical();

            double oldTurnShapeExponent = _ascentSettings.TurnShapeExponent;

            _ascentSettings.AutoPath = GUILayout.Toggle(_ascentSettings.AutoPath, Localizer.Format("#MechJeb_AscentPathEd_auto"),
                GUILayout.ExpandWidth(false)); //"Automatic Altitude Turn"
            if (_ascentSettings.AutoPath)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AscentPathEd_label1"), GUILayout.Width(60)); //"Altitude: "
                // 1 to 200 / 200 = 0.5% to 105%, without this mess would the slider cause lots of garbage floats like 0.9999864
                _ascentSettings.AutoTurnPerc = Mathf.Floor(GUILayout.HorizontalSlider(_ascentSettings.AutoTurnPerc * 200f, 1f, 210.5f)) / 200f;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AscentPathEd_label2"), GUILayout.Width(60)); //"Velocity: "
                _ascentSettings.AutoTurnSpdFactor = Mathf.Floor(GUILayout.HorizontalSlider(_ascentSettings.AutoTurnSpdFactor * 2f, 8f, 160f)) / 2f;
                GUILayout.EndHorizontal();
            }

            if (_ascentSettings.AutoPath)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AscentPathEd_label3"), GUILayout.ExpandWidth(false)); //"Turn start when Altitude is "
                GUILayout.Label(MuUtils.ToSI(_ascentSettings.AutoTurnStartAltitude, -1, 2) + "m ", GUILayout.ExpandWidth(false));
                GUILayout.Label(Localizer.Format("#MechJeb_AscentPathEd_label4"), GUILayout.ExpandWidth(false)); //"or Velocity reach "
                GUILayout.Label(MuUtils.ToSI(_ascentSettings.AutoTurnStartVelocity, -1, 3) + "m/s", GUILayout.ExpandWidth(false)); //
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AscentPathEd_label5")); //"Turn end altitude: "
                GUILayout.Label(MuUtils.ToSI(_ascentSettings.AutoTurnEndAltitude, -1, 2) + "m", GuiUtils.middleRightLabel,
                    GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
            }
            else
            {
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AscentPathEd_label6"), _ascentSettings.TurnStartAltitude,
                    "km"); //"Turn start altitude:"
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AscentPathEd_label7"), _ascentSettings.TurnStartVelocity,
                    "m/s"); //"Turn start velocity:"
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AscentPathEd_label8"), _ascentSettings.TurnEndAltitude,
                    "km"); //"Turn end altitude:"
            }

            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AscentPathEd_label9"), _ascentSettings.TurnEndAngle, "°"); //"Final flight path angle:"
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_AscentPathEd_label10"), _ascentSettings.TurnShapeExponent, "%"); //"Turn shape:"

            // Round the slider's value (0..1) to sliderPrecision decimal places.
            const int SLIDER_PRECISION = 3;

            double sliderTurnShapeExponent = GUILayout.HorizontalSlider((float)_ascentSettings.TurnShapeExponent, 0.0F, 1.0F);
            if (Math.Round(Math.Abs(sliderTurnShapeExponent - oldTurnShapeExponent), SLIDER_PRECISION) > 0)
            {
                _ascentSettings.TurnShapeExponent.val = Math.Round(sliderTurnShapeExponent, SLIDER_PRECISION);
            }

            GUILayout.Box(_pathTexture);

            if (Event.current.type == EventType.Repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                r.xMin += GUI.skin.box.margin.left;
                r.yMin += GUI.skin.box.margin.top;

                r.xMax -= GUI.skin.box.margin.right;
                r.yMax -= GUI.skin.box.margin.bottom;

                float scale = (float)((_ascentSettings.AutoPath ? _ascentSettings.AutoTurnEndAltitude : _ascentSettings.TurnEndAltitude) / r.height);

                DrawnPath(r, scale, scale, _path, Color.red);
                DrawnTrajectory(r, _recorder);
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
                var c = new Color(0.0F, 0.0F, v);

                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, c);

                    if (mainBody.atmosphere && (int)(maxAtmosphereAltitude / scale) == y)
                        texture.SetPixel(x, y, XKCDColors.LightGreyBlue);
                }
            }

            texture.Apply();
        }

        private void DrawnPath(Rect r, float scaleX, float scaleY, MechJebModuleAscentClassicAutopilot path, Color color)
        {
            float alt = 0;
            float downrange = 0;
            var p1 = new Vector2(r.xMin, r.yMax);
            var p2 = new Vector2();

            while (alt < (_ascentSettings.AutoPath ? _ascentSettings.AutoTurnEndAltitude : _ascentSettings.TurnEndAltitude) &&
                   downrange < r.width * scaleX)
            {
                float desiredAngle = (float)(alt < path.VerticalAscentEnd() ? 90 : path.FlightPathAngle(alt, 0));

                alt       += scaleY * Mathf.Sin(desiredAngle * Mathf.Deg2Rad);
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

        private void DrawnTrajectory(Rect r, MechJebModuleFlightRecorder recorder)
        {
            if (recorder.history.Length <= 2 || recorder.historyIdx == 0)
                return;

            float scale = (float)((_ascentSettings.AutoPath ? _ascentSettings.AutoTurnEndAltitude : _ascentSettings.TurnEndAltitude) /
                                  r.height); //meters per pixel

            int t = 1;

            var p1 = new Vector2(r.xMin + (float)(recorder.history[0].downRange / scale), r.yMax - (float)(recorder.history[0].altitudeASL / scale));
            var p2 = new Vector2();

            while (t <= recorder.historyIdx && t < recorder.history.Length)
            {
                MechJebModuleFlightRecorder.record rec = recorder.history[t];
                p2.x = r.xMin + (float)(rec.downRange / scale);
                p2.y = r.yMax - (float)(rec.altitudeASL / scale);

                if ((r.Contains(p2) && (p1 - p2).sqrMagnitude >= 1.0) || t < 2)
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
            return Localizer.Format("#MechJeb_AscentPathEd_title"); //"Ascent Path Editor"
        }

        public override string IconName()
        {
            return "Ascent Path Editor";
        }
    }
}
