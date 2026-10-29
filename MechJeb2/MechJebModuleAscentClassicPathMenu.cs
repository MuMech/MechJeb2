extern alias JetBrainsAnnotations;
using System;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleAscentClassicPathMenu : DisplayModule
    {
        public MechJebModuleAscentClassicPathMenu(MechJebCore core)
            : base(core)
        {
        }

        public override bool ShowInMenu => false;

        private MechJebModuleAscentSettings _ascentSettings;
        private MechJebModuleAscentClassicAutopilot _path;
        private static readonly Texture2D _pathTexture = new Texture2D(400, 100);
        private MechJebModuleFlightRecorder _recorder;
        private double _lastMaxAtmosphereAltitude = -1;

        public override void OnStart(PartModule.StartState state)
        {
            _recorder = Core.GetComputerModule<MechJebModuleFlightRecorder>();
            _ascentSettings = Core.GetComputerModule<MechJebModuleAscentSettings>();
            _path = Core.GetComputerModule<MechJebModuleAscentClassicAutopilot>();
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GuiUtils.LayoutWidth(300), GUILayout.Height(100) };

        // redraw the atmosphere picture when the body's atmosphere changed (shared with the new UI)
        internal void UpdateAtmoTextureIfNeeded()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_lastMaxAtmosphereAltitude != MainBody.RealMaxAtmosphereAltitude())
            {
                _lastMaxAtmosphereAltitude = MainBody.RealMaxAtmosphereAltitude();
                UpdateAtmoTexture(_pathTexture, MainBody,
                    _ascentSettings.AutoPath ? _ascentSettings.AutoTurnEndAltitude : _ascentSettings.TurnEndAltitude);
            }
        }

        internal MechJebModuleAscentSettings AscentSettings => _ascentSettings;

        // Round the slider's value (0..1) to sliderPrecision decimal places.
        private const int SLIDER_PRECISION = 3;

        internal void ApplyTurnShapeSlider(double sliderTurnShapeExponent, double oldTurnShapeExponent)
        {
            if (Math.Round(Math.Abs(sliderTurnShapeExponent - oldTurnShapeExponent), SLIDER_PRECISION) > 0)
            {
                _ascentSettings.TurnShapeExponent.Val = Math.Round(sliderTurnShapeExponent, SLIDER_PRECISION);
            }
        }

        // the path picture: the atmosphere texture with the planned path and the flown trajectory drawn over it
        internal void DrawPathImage()
        {
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
        }

        protected override void WindowGUI(int windowID)
        {
            UpdateAtmoTextureIfNeeded();

            GUILayout.BeginVertical();

            double oldTurnShapeExponent = _ascentSettings.TurnShapeExponent;

            _ascentSettings.AutoPath = GUILayout.Toggle(_ascentSettings.AutoPath, Localizer.Format("#MechJeb_AscentPathEd_auto"),
                GuiUtils.LayoutNoExpandWidth); //"Automatic Altitude Turn"
            if (_ascentSettings.AutoPath)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AscentPathEd_label1"), GuiUtils.LayoutWidth(60)); //"Altitude: "
                // 1 to 200 / 200 = 0.5% to 105%, without this mess would the slider cause lots of garbage floats like 0.9999864
                _ascentSettings.AutoTurnPerc = Mathf.Floor(GUILayout.HorizontalSlider(_ascentSettings.AutoTurnPerc * 200f, 1f, 210.5f)) / 200f;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AscentPathEd_label2"), GuiUtils.LayoutWidth(60)); //"Velocity: "
                _ascentSettings.AutoTurnSpdFactor = Mathf.Floor(GUILayout.HorizontalSlider(_ascentSettings.AutoTurnSpdFactor * 2f, 8f, 160f)) / 2f;
                GUILayout.EndHorizontal();
            }

            if (_ascentSettings.AutoPath)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AscentPathEd_label3"), GuiUtils.LayoutNoExpandWidth); //"Turn start when Altitude is "
                GUILayout.Label(_ascentSettings.AutoTurnStartAltitude.ToSI(2) + "m ", GuiUtils.LayoutNoExpandWidth);
                GUILayout.Label(Localizer.Format("#MechJeb_AscentPathEd_label4"), GuiUtils.LayoutNoExpandWidth); //"or Velocity reach "
                GUILayout.Label(_ascentSettings.AutoTurnStartVelocity.ToSI(3) + "m/s", GuiUtils.LayoutNoExpandWidth); //
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_AscentPathEd_label5")); //"Turn end altitude: "
                GUILayout.Label(_ascentSettings.AutoTurnEndAltitude.ToSI(2) + "m", GuiUtils.MiddleRightLabel,
                    GuiUtils.LayoutExpandWidth);
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

            double sliderTurnShapeExponent = GUILayout.HorizontalSlider((float)_ascentSettings.TurnShapeExponent, 0.0F, 1.0F);
            ApplyTurnShapeSlider(sliderTurnShapeExponent, oldTurnShapeExponent);

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

        // The planned path as points in picture pixels (x downrange, y altitude, origin bottom left, one pixel = scale meters),
        // shared by the IMGUI picture and the new UI. The first point is the origin.
        internal void ComputePathPoints(float width, float height, System.Collections.Generic.List<Vector2> points)
        {
            points.Clear();
            points.Add(Vector2.zero);

            double endAltitude = PathEndAltitude;
            float scale = (float)(endAltitude / height);
            float alt = 0;
            float downrange = 0;
            var p1 = Vector2.zero;
            var p2 = new Vector2();

            int guard = 0;
            while (alt < endAltitude && downrange < width * scale && guard++ < 100000)
            {
                float desiredAngle = (float)(alt < _path.VerticalAscentEnd() ? 90 : _path.FlightPathAngle(alt, 0));

                alt += scale * Mathf.Sin(desiredAngle * Mathf.Deg2Rad);
                downrange += scale * Mathf.Cos(desiredAngle * Mathf.Deg2Rad);

                p2.x = downrange / scale;
                p2.y = alt / scale;

                if ((p1 - p2).sqrMagnitude >= 1.0)
                {
                    points.Add(p2);
                    p1 = p2;
                }
            }
        }

        internal double PathEndAltitude => _ascentSettings.AutoPath ? _ascentSettings.AutoTurnEndAltitude : _ascentSettings.TurnEndAltitude;

        internal static Texture2D PathTexture => _pathTexture;
        internal MechJebModuleFlightRecorder Recorder => _recorder;

        // the inputs of the planned path, to redraw the new UI's picture only when one changed
        internal struct PathSignature : IEquatable<PathSignature>
        {
            public bool Auto;
            public double EndAltitude, VerticalEnd, Probe, EndAngle, Exponent, StartVelocity;

            public bool Equals(PathSignature o) =>
                Auto == o.Auto && EndAltitude == o.EndAltitude && VerticalEnd == o.VerticalEnd && Probe == o.Probe && EndAngle == o.EndAngle &&
                Exponent == o.Exponent && StartVelocity == o.StartVelocity;

            public override bool Equals(object obj) => obj is PathSignature o && Equals(o);
            public override int GetHashCode() => EndAltitude.GetHashCode();
        }

        internal PathSignature CurrentPathSignature()
        {
            double end = PathEndAltitude;
            return new PathSignature
            {
                Auto = _ascentSettings.AutoPath,
                EndAltitude = end,
                VerticalEnd = _path.VerticalAscentEnd(),
                // the turn also depends on where the autopilot started the turn: sample the angle half way up
                Probe = _path.FlightPathAngle(end * 0.5, 0),
                EndAngle = _ascentSettings.TurnEndAngle,
                Exponent = _ascentSettings.TurnShapeExponent,
                StartVelocity = _ascentSettings.AutoPath ? _ascentSettings.AutoTurnStartVelocity : _ascentSettings.TurnStartVelocity,
            };
        }

        private static readonly System.Collections.Generic.List<Vector2> _imguiPoints = new System.Collections.Generic.List<Vector2>();

        private void DrawnPath(Rect r, float scaleX, float scaleY, MechJebModuleAscentClassicAutopilot path, Color color)
        {
            ComputePathPoints(r.width, r.height, _imguiPoints);
            for (int i = 1; i < _imguiPoints.Count; i++)
            {
                Vector2 a = _imguiPoints[i - 1], b = _imguiPoints[i];
                Drawing.DrawLine(new Vector2(r.xMin + a.x, r.yMax - a.y), new Vector2(r.xMin + b.x, r.yMax - b.y), color, 2, true);
            }
        }

        private void DrawnTrajectory(Rect r, MechJebModuleFlightRecorder recorder)
        {
            if (recorder.History.Length <= 2 || recorder.HistoryIdx == 0)
                return;

            float scale = (float)((_ascentSettings.AutoPath ? _ascentSettings.AutoTurnEndAltitude : _ascentSettings.TurnEndAltitude) /
                r.height); //meters per pixel

            int t = 1;

            var p1 = new Vector2(r.xMin + (float)(recorder.History[0].DownRange / scale), r.yMax - (float)(recorder.History[0].AltitudeASL / scale));
            var p2 = new Vector2();

            while (t <= recorder.HistoryIdx && t < recorder.History.Length)
            {
                MechJebModuleFlightRecorder.RecordStruct rec = recorder.History[t];
                p2.x = r.xMin + (float)(rec.DownRange / scale);
                p2.y = r.yMax - (float)(rec.AltitudeASL / scale);

                if ((r.Contains(p2) && (p1 - p2).sqrMagnitude >= 1.0) || t < 2)
                {
                    Drawing.DrawLine(p1, p2, Color.white, 2, true);
                    p1.x = p2.x;
                    p1.y = p2.y;
                }

                t++;
            }
        }

        public override string GetName() => Localizer.Format("#MechJeb_AscentPathEd_title"); //"Ascent Path Editor"

        public override string IconName() => "Ascent Path Editor";
    }
}
