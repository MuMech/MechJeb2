extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
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

        private MechJebModuleAscentClassicAutopilot _path;
        private double _lastMaxAtmosphereAltitude = -1;

        public override void OnStart(PartModule.StartState state)
        {
            Recorder = Core.GetComputerModule<MechJebModuleFlightRecorder>();
            AscentSettings = Core.GetComputerModule<MechJebModuleAscentSettings>();
            _path = Core.GetComputerModule<MechJebModuleAscentClassicAutopilot>();
        }

        // redraw the atmosphere picture when the body's atmosphere changed (shared with the new UI)
        internal void UpdateAtmoTextureIfNeeded()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_lastMaxAtmosphereAltitude != MainBody.RealMaxAtmosphereAltitude())
            {
                _lastMaxAtmosphereAltitude = MainBody.RealMaxAtmosphereAltitude();
                UpdateAtmoTexture(PathTexture, MainBody,
                    AscentSettings.AutoPath ? AscentSettings.AutoTurnEndAltitude : AscentSettings.TurnEndAltitude);
            }
        }

        internal MechJebModuleAscentSettings AscentSettings { get; private set; }

        // Round the slider's value (0..1) to sliderPrecision decimal places.
        private const int SLIDER_PRECISION = 3;

        internal void ApplyTurnShapeSlider(double sliderTurnShapeExponent, double oldTurnShapeExponent)
        {
            if (Math.Round(Math.Abs(sliderTurnShapeExponent - oldTurnShapeExponent), SLIDER_PRECISION) > 0)
            {
                AscentSettings.TurnShapeExponent.Val = Math.Round(sliderTurnShapeExponent, SLIDER_PRECISION);
            }
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
        internal void ComputePathPoints(float width, float height, List<Vector2> points)
        {
            points.Clear();
            points.Add(Vector2.zero);

            double endAltitude = PathEndAltitude;
            float scale = (float)(endAltitude / height);
            float alt = 0;
            float downrange = 0;
            Vector2 p1 = Vector2.zero;
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

        internal double PathEndAltitude => AscentSettings.AutoPath ? AscentSettings.AutoTurnEndAltitude : AscentSettings.TurnEndAltitude;

        internal static Texture2D PathTexture { get; } = new Texture2D(400, 100);

        internal MechJebModuleFlightRecorder Recorder { get; private set; }

        // the inputs of the planned path, to redraw the new UI's picture only when one changed
        internal struct PathSignature : IEquatable<PathSignature>
        {
            public bool Auto;
            public double EndAltitude, VerticalEnd, Probe, EndAngle, Exponent, StartVelocity;

            public bool Equals(PathSignature o) =>
                Auto == o.Auto && EndAltitude == o.EndAltitude && VerticalEnd == o.VerticalEnd && Probe == o.Probe && EndAngle == o.EndAngle &&
                Exponent == o.Exponent && StartVelocity == o.StartVelocity;

            public override bool Equals(object obj) => obj is PathSignature o && Equals(o);
            public override int  GetHashCode()      => EndAltitude.GetHashCode();
        }

        internal PathSignature CurrentPathSignature()
        {
            double end = PathEndAltitude;
            return new PathSignature
            {
                Auto = AscentSettings.AutoPath,
                EndAltitude = end,
                VerticalEnd = _path.VerticalAscentEnd(),
                // the turn also depends on where the autopilot started the turn: sample the angle half way up
                Probe = _path.FlightPathAngle(end * 0.5, 0),
                EndAngle = AscentSettings.TurnEndAngle,
                Exponent = AscentSettings.TurnShapeExponent,
                StartVelocity = AscentSettings.AutoPath ? AscentSettings.AutoTurnStartVelocity : AscentSettings.TurnStartVelocity
            };
        }

        private static readonly List<Vector2> _imguiPoints = new List<Vector2>();

        private void DrawnPath(Rect r, float scaleX, float scaleY, MechJebModuleAscentClassicAutopilot path, Color color)
        {
            ComputePathPoints(r.width, r.height, _imguiPoints);
            for (int i = 1; i < _imguiPoints.Count; i++)
            {
                Vector2 a = _imguiPoints[i - 1], b = _imguiPoints[i];
                Drawing.DrawLine(new Vector2(r.xMin + a.x, r.yMax - a.y), new Vector2(r.xMin + b.x, r.yMax - b.y), color, 2, true);
            }
        }

        public override string GetName() => Localizer.Format("#MechJeb_AscentPathEd_title"); //"Ascent Path Editor"

        public override string IconName() => "Ascent Path Editor";
    }
}
