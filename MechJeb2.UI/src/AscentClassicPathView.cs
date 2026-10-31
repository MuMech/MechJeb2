using System;
using System.Collections.Generic;
using KodeKit;
using KodeUI;
using MechJebLib.Utils;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleAscentClassicPathMenu ("Ascent Path Editor") in KodeKit, row for row with its WindowGUI.
    /// The path picture is native: the atmosphere texture in a RawImage with the planned path and the flown trajectory as line meshes,
    /// rebuilt only when the path parameters or the recorded history change.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleAscentClassicPathMenu))]
    public class AscentClassicPathView : KodeView
    {
        readonly MechJebModuleAscentClassicPathMenu menu;

        public AscentClassicPathView(MechJebModuleAscentClassicPathMenu menu) : base(menu) => this.menu = menu;

        protected override float MinWidth => 300;

        MechJebCore Core => menu.Core;
        MechJebModuleAscentSettings Settings => menu.AscentSettings;

        bool HasVessel => Core.vessel != null && Core.vessel.mainBody != null;

        // the atmosphere texture WindowGUI refreshed before drawing
        protected override void BeforeSync()
        {
            if (HasVessel) menu.UpdateAtmoTextureIfNeeded();
        }

        /// <summary>A "value.ToSI(digits) + suffix" text, formatted only when the value changes.</summary>
        sealed class SiText
        {
            readonly Func<double> value;
            readonly int digits;
            readonly string suffix;
            double last = double.NaN;
            string text;

            public SiText(Func<double> value, int digits, string suffix)
            {
                this.value = value;
                this.digits = digits;
                this.suffix = suffix;
            }

            public string Get()
            {
                double v = HasValue ? value() : 0;
                if (text == null || !v.Equals(last))
                {
                    last = v;
                    text = v.ToSI(digits) + suffix;
                }

                return text;
            }

            public Func<bool> Available;
            bool HasValue => Available == null || Available();
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            w.Toggle(r, "#MechJeb_AscentPathEd_auto", () => Settings.AutoPath, v => Settings.AutoPath = v); //Automatic Altitude Turn

            // automatic path: sliders
            Layout sliders = KodeWindow.Panel(r);
            w.Visible(sliders.gameObject, () => Settings.AutoPath);
            Layout altitude = KodeWindow.Row(sliders);
            KodeWindow.Label(altitude, "#MechJeb_AscentPathEd_label1"); //"Altitude: "
            // 1 to 200 / 200 = 0.5% to 105%, without this mess would the slider cause lots of garbage floats like 0.9999864
            w.Slider(altitude, () => Settings.AutoTurnPerc * 200f, v => Settings.AutoTurnPerc = Mathf.Floor(v) / 200f, 1f, 210.5f);
            Layout velocity = KodeWindow.Row(sliders);
            KodeWindow.Label(velocity, "#MechJeb_AscentPathEd_label2"); //"Velocity: "
            w.Slider(velocity, () => Settings.AutoTurnSpdFactor * 2f, v => Settings.AutoTurnSpdFactor = Mathf.Floor(v) / 2f, 8f, 160f);

            // automatic path: the resulting turn
            Layout auto = KodeWindow.Panel(r);
            w.Visible(auto.gameObject, () => Settings.AutoPath);
            var startAlt = new SiText(() => Settings.AutoTurnStartAltitude, 2, "m ") { Available = () => HasVessel };
            var startVel = new SiText(() => Settings.AutoTurnStartVelocity, 3, "m/s") { Available = () => HasVessel };
            var endAlt = new SiText(() => Settings.AutoTurnEndAltitude, 2, "m") { Available = () => HasVessel };
            Layout start = KodeWindow.Row(auto);
            KodeWindow.Label(start, "#MechJeb_AscentPathEd_label3"); //"Turn start when Altitude is "
            w.BoundLabel(start, "999.99km ", startAlt.Get);
            KodeWindow.Label(start, "#MechJeb_AscentPathEd_label4"); //"or Velocity reach "
            w.BoundLabel(start, "9.999km/s", startVel.Get);
            Layout end = KodeWindow.Row(auto);
            KodeWindow.Label(end, "#MechJeb_AscentPathEd_label5"); //"Turn end altitude: "
            w.BoundLabel(end, "999.99km", endAlt.Get, true).tmpText.alignment = TMPro.TextAlignmentOptions.MidlineRight;

            // manual path: the three fields
            Layout manual = KodeWindow.Panel(r);
            w.Visible(manual.gameObject, () => !Settings.AutoPath);
            AscentUi.SimpleBox(w, manual, "#MechJeb_AscentPathEd_label6", Settings.TurnStartAltitude, "km"); //"Turn start altitude:"
            AscentUi.SimpleBox(w, manual, "#MechJeb_AscentPathEd_label7", Settings.TurnStartVelocity, "m/s"); //"Turn start velocity:"
            AscentUi.SimpleBox(w, manual, "#MechJeb_AscentPathEd_label8", Settings.TurnEndAltitude, "km"); //"Turn end altitude:"

            AscentUi.SimpleBox(w, r, "#MechJeb_AscentPathEd_label9", Settings.TurnEndAngle, "°"); //"Final flight path angle:"
            AscentUi.SimpleBox(w, r, "#MechJeb_AscentPathEd_label10", Settings.TurnShapeExponent, "%"); //"Turn shape:"

            w.Slider(r, () => (float)Settings.TurnShapeExponent, v => menu.ApplyTurnShapeSlider(v, Settings.TurnShapeExponent), 0f, 1f);

            BuildPathPicture(w, r);
        }

        // the texture's own size, as GUILayout.Box(texture) shows it; the height sets the meters-per-pixel scale
        const float PictureWidth = 400, PictureHeight = 100;

        /// <summary>A list of points as a graph line.</summary>
        sealed class PointSource : IGraphSource
        {
            public readonly List<Vector2> Points = new List<Vector2>();
            public int Count => Points.Count;
            public double X(int i) => Points[i].x;
            public double Y(int i) => Points[i].y;
        }

        /// <summary>The flight recorder's history as a line, in meters (x downrange, y altitude).</summary>
        sealed class TrajectorySource : IGraphSource
        {
            readonly MechJebModuleAscentClassicPathMenu menu;
            public TrajectorySource(MechJebModuleAscentClassicPathMenu menu) => this.menu = menu;

            public int Count
            {
                get
                {
                    MechJebModuleFlightRecorder rec = menu.Recorder;
                    if (rec == null || rec.History == null || rec.History.Length <= 2 || rec.HistoryIdx <= 0) return 0;
                    return Math.Min(rec.HistoryIdx + 1, rec.History.Length);
                }
            }

            public double X(int i) => menu.Recorder.History[i].DownRange;
            public double Y(int i) => menu.Recorder.History[i].AltitudeASL;
        }

        // the picture: atmosphere texture, planned path (red), flown trajectory (white)
        void BuildPathPicture(KodeWindow w, UIObject parent)
        {
            RectTransform surface = Graphs.Surface(parent, PictureWidth, PictureHeight, "PathPicture");
            surface.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var atmosphere = Graphs.Fill<UnityEngine.UI.RawImage>(surface, "Atmosphere");
            atmosphere.texture = MechJebModuleAscentClassicPathMenu.PathTexture;
            atmosphere.raycastTarget = false;

            var pathSource = new PointSource();
            var path = Graphs.Fill<LineSeriesGraphic>(surface, "Path");
            path.Source = pathSource;
            path.color = Color.red;
            path.Thickness = 2;
            path.raycastTarget = false;
            path.XMin = 0;
            path.XMax = PictureWidth;
            path.YMin = 0;
            path.YMax = PictureHeight;

            var trajectorySource = new TrajectorySource(menu);
            var trajectory = Graphs.Fill<LineSeriesGraphic>(surface, "Trajectory");
            trajectory.Source = trajectorySource;
            trajectory.color = Color.white;
            trajectory.Thickness = 2;
            trajectory.raycastTarget = false;
            trajectory.Unclamped = true; // like IMGUI: drawn up to its first point outside the picture, cut at the edge
            trajectory.XMin = 0;
            trajectory.YMin = 0;

            bool haveSignature = false;
            MechJebModuleAscentClassicPathMenu.PathSignature signature = default;
            int lastCount = -1;
            double lastEnd = double.NaN;
            double lastDown = double.NaN;
            MechJebModuleFlightRecorder.RecordStruct[] lastHistory = null;

            w.AddSync(() =>
            {
                if (!HasVessel) return;
                try
                {
                    MechJebModuleAscentClassicPathMenu.PathSignature now = menu.CurrentPathSignature();
                    if (!haveSignature || !now.Equals(signature))
                    {
                        haveSignature = true;
                        signature = now;
                        menu.ComputePathPoints(PictureWidth, PictureHeight, pathSource.Points);
                        path.SetVerticesDirty();
                    }

                    // the trajectory shares the path's scale (meters per pixel); redraw when it grew or the scale changed
                    double end = now.EndAltitude;
                    int count = trajectorySource.Count;
                    MechJebModuleFlightRecorder rec = menu.Recorder;
                    double down = count > 0 ? rec.History[count - 1].DownRange : 0;
                    if (count != lastCount || !end.Equals(lastEnd) || !down.Equals(lastDown) || rec.History != lastHistory)
                    {
                        lastCount = count;
                        lastEnd = end;
                        lastDown = down;
                        lastHistory = rec.History;
                        trajectory.XMax = PictureWidth * end / PictureHeight;
                        trajectory.YMax = end;
                        trajectory.SetVerticesDirty();
                    }
                }
                catch (Exception)
                {
                    // never throw from a binding: the picture keeps its last state
                }
            });
        }
    }
}
