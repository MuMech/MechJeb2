using System;
using System.Collections.Generic;
using KodeKit;
using KodeUI;
using MechJebLib.Utils;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleAscentClassicPathMenu ("Ascent Path Editor") in KodeKit.
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
        MechJebModuleAscentClassicAutopilot Autopilot => menu.Autopilot;

        bool HasVessel => Core.vessel != null && Core.vessel.mainBody != null;

        // the atmosphere texture, refreshed before the bindings
        protected override void BeforeSync()
        {
            if (HasVessel) menu.UpdateAtmoTextureIfNeeded();
        }

        /// <summary>A "value.ToSI(digits) + suffix" text, formatted only when the value changes at the shown precision.</summary>
        sealed class SiText
        {
            readonly Func<double> value;
            readonly int digits;
            readonly string suffix;
            double lastKey = double.NaN;
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
                // an airless body's turn start follows the terrain every frame
                double key = v.RoundToSI(digits);
                if (text == null || !key.Equals(lastKey))
                {
                    lastKey = key;
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

            // Automatic | Manual: the turn from the two sliders, or from the three fields
            Layout mode = KodeWindow.Row(r);
            KodeWindow.Label(mode, "#MechJeb_NewUI_PathTurn"); //Turn
            string[] modes = { KodeWindow.Loc("#MechJeb_NewUI_PathAutomatic"), KodeWindow.Loc("#MechJeb_NewUI_PathManual") }; //Automatic, Manual
            w.Segmented(mode, modes, () => Autopilot.AutoPath ? 0 : 1, i => Autopilot.AutoPath = i == 0);

            // automatic: each slider with the value it gives at the end of its row, then the end altitude it gives
            KodeWindow.PropertyGrid auto = w.Properties(r);
            w.Visible(auto.Table.gameObject, () => Autopilot.AutoPath);
            var startAlt = new SiText(() => Autopilot.AutoTurnStartAltitude, 2, "m") { Available = () => HasVessel };
            var startVel = new SiText(() => Autopilot.AutoTurnStartVelocity, 3, "m/s") { Available = () => HasVessel };
            var endAlt = new SiText(() => Autopilot.AutoTurnEndAltitude, 2, "m") { Available = () => HasVessel };
            SliderRow(w, auto, "#MechJeb_NewUI_PathStartAltitude", startAlt.Get, //Start altitude
                // 1 to 200 / 200 = 0.5% to 105%, without this mess would the slider cause lots of garbage floats like 0.9999864
                () => Autopilot.AutoTurnPerc * 200f, v => Autopilot.AutoTurnPerc = Mathf.Floor(v) / 200f, 1f, 210.5f);
            SliderRow(w, auto, "#MechJeb_NewUI_PathStartSpeed", startVel.Get, //Start speed
                () => Autopilot.AutoTurnSpdFactor * 2f, v => Autopilot.AutoTurnSpdFactor = Mathf.Floor(v) / 2f, 8f, 160f);
            auto.Value(KodeWindow.Loc("#MechJeb_NewUI_PathEndAltitude"), endAlt.Get); //End altitude

            // manual path: the three fields
            Layout manual = KodeWindow.Panel(r);
            w.Visible(manual.gameObject, () => !Autopilot.AutoPath);
            AscentUi.SimpleBox(w, manual, "#MechJeb_AscentPathEd_label6", Autopilot.TurnStartAltitude, "km"); //"Turn start altitude:"
            AscentUi.SimpleBox(w, manual, "#MechJeb_AscentPathEd_label7", Autopilot.TurnStartVelocity, "m/s"); //"Turn start velocity:"
            AscentUi.SimpleBox(w, manual, "#MechJeb_AscentPathEd_label8", Autopilot.TurnEndAltitude, "km"); //"Turn end altitude:"

            AscentUi.SimpleBox(w, r, "#MechJeb_AscentPathEd_label9", Autopilot.TurnEndAngle, "°"); //"Final flight path angle:"

            // the turn shape: its slider and its field on one row
            Layout shape = KodeWindow.Row(r);
            KodeWindow.Label(shape, "#MechJeb_AscentPathEd_label10"); //"Turn shape:"
            w.Slider(shape, () => (float)Autopilot.TurnShapeExponent, v => menu.ApplyTurnShapeSlider(v, Autopilot.TurnShapeExponent), 0.1f, 1f);
            EditUi.Field(w, shape, Autopilot.TurnShapeExponent, 50);
            KodeWindow.Label(shape, "%");

            BuildPathPicture(w, r);
        }

        // label, slider, and the value the slider gives
        static void SliderRow(KodeWindow w, KodeWindow.PropertyGrid g, string key, Func<string> value, Func<float> get, Action<float> set,
            float min, float max)
        {
            Layout row = g.Value(KodeWindow.Loc(key), value);
            UnityEngine.UI.Slider slider = w.Slider(row, get, set, min, max);
            // the row is: the switch column (empty), the label, the value; the slider goes between the label and the value
            Transform cell = slider.transform;
            while (cell.parent != null && cell.parent != row.transform) cell = cell.parent;
            cell.SetSiblingIndex(2);
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
                // no catch: KodeWindow.Sync logs a failure once, and the picture keeps its last state
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
            });
        }
    }
}
