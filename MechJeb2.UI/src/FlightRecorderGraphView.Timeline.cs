using System.Runtime.CompilerServices;
using KodeKit;
using KodeKit.Headless;
using KodeUI;
using KSP.Localization;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// The Flight Recorder's events as a timeline strip under the plot (a <see cref="TimelineModel"/> on the plot's own
    /// <see cref="ViewportModel"/>, so the strip and the plot pan and zoom together): the mark and each stage change on the top
    /// lane, the range and the marked points of <see cref="LineGraph.Ranges"/> on the second. "Now" is the last record. A click
    /// on the mark or a stage moves the active point there (or marks one), so the readout shows the graphs' values at that event;
    /// a click on a point makes it the active one.
    /// </summary>
    public partial class FlightRecorderGraphView
    {
        // the kinds, which give the colours
        public const int EventMark = 0, EventStage = 1, EventRange = 2, EventPoint = 3;

        // the ids: the selection follows them when the items are built again
        public const int MarkId = 1, RangeId = 2, StageId = 100, PointId = 1000;

        static readonly Color[] eventColors =
        {
            new Color(0.45f, 1f, 0.55f), new Color(0.80f, 0.80f, 0.80f), new Color(1f, 1f, 1f, 0.6f), new Color(1f, 0.85f, 0.35f),
        };

        Timeline timeline;
        // the events' model outlives the window, as the view and the range do: a rebuild keeps the selected event
        static readonly ConditionalWeakTable<MechJebModuleFlightRecorderGraph, TimelineModel> keptEvents =
            new ConditionalWeakTable<MechJebModuleFlightRecorderGraph, TimelineModel>();
        GameObject timelineRightSpace;
        int eventsStages = -1, eventsRanges = -1;
        bool eventsDownrange;
        double eventsMark = double.NaN;

        /// <summary>The events' timeline under the plot (null before the plot is built).</summary>
        public TimelineModel Events => timeline?.Model;

        /// <summary>The events' strip (null before the plot is built).</summary>
        public Timeline EventStrip => timeline;

        void BuildTimeline(KodeWindow w, UIObject plotColumn)
        {
            eventsStages = eventsRanges = -1;
            Layout row = KodeWindow.Row(plotColumn);
            row.gameObject.name = "EventsRow";
            // under the plot exactly: the Y labels' room on the left, the right labels' when they show
            KodeWindow.Space(row, 70);
            TimelineModel model = keptEvents.GetValue(g, _ => new TimelineModel(graph.View) { LaneCount = 2 });
            model.Picked += EventPicked;
            // everything recorded is before "now", the last record: nothing is drawn faded
            timeline = new Timeline(w, row, model, 0)
            {
                LabelText = EventText,
                ItemColor = it => eventColors[it.Kind],
                FadePast = false
            };
            timeline.Size.preferredWidth = 0;
            UIEmpty right = row.Add<UIEmpty>("RightSpace");
            right.PreferredWidth(70).Finish();
            timelineRightSpace = right.gameObject;
            timelineRightSpace.SetActive(RightShown() >= 0);
            w.Behaviour.AddRelocalize(() => eventsStages = -1);
        }

        string EventText(TimelineItem it)
        {
            switch (it.Kind)
            {
                case EventStage: return Localizer.Format("#MechJeb_NewUI_TimelineStage", ((int)it.Value).ToString());
                case EventMark: return KodeWindow.Loc("#MechJeb_Flightrecord_Button3"); //Mark
                case EventRange: return KodeWindow.Loc("#MechJeb_NewUI_GraphRange");
                default: return KodeWindow.Loc("#MechJeb_NewUI_GraphPoint");
            }
        }

        // the mark or a stage: the active point goes there; a point: it becomes the active one
        void EventPicked(TimelineItem it, ChangeSource source)
        {
            RangeModel ranges = Ranges;
            if (it == null || ranges == null) return;
            switch (it.Kind)
            {
                case EventMark:
                case EventStage:
                    if (ranges.ActivePoint >= 0) ranges.MovePoint(ranges.ActivePoint, it.Start, source);
                    else ranges.AddPoint(it.Start, source);
                    break;
                case EventPoint:
                    int i = it.Id - PointId;
                    if (i >= 0 && i < ranges.PointCount) ranges.MovePoint(i, ranges.Point(i), source);
                    break;
            }
        }

        // the items again when the stages, the range or the points, the units or the mark changed; "now" every frame
        void SyncTimeline(int rightIdx)
        {
            if (timeline == null) return;
            if (timelineRightSpace.activeSelf != rightIdx >= 0) timelineRightSpace.SetActive(rightIdx >= 0);
            TimelineModel events = timeline.Model;
            RangeModel ranges = Ranges;
            int stages = stageMarkers.Indices.Count, rangeVersion = ranges?.Version ?? -1;
            int n = PointCount;
            if (stages != eventsStages || rangeVersion != eventsRanges || g.downrange != eventsDownrange || !Rec.MarkUT.Equals(eventsMark))
            {
                eventsStages = stages;
                eventsRanges = rangeVersion;
                eventsDownrange = g.downrange;
                eventsMark = Rec.MarkUT;
                events.BeginUpdate();
                if (n > 0) events.Set(MarkId, XOf(0), double.NaN, 0, EventMark, "#MechJeb_Flightrecord_Button3");
                for (int i = 0; i < stages; i++)
                {
                    int idx = stageMarkers.Indices[i];
                    events.Set(StageId + i, XOf(idx), double.NaN, 0, EventStage, "#MechJeb_NewUI_TimelineStage", Rec.History[idx].CurrentStage);
                }

                if (ranges != null)
                {
                    if (ranges.HasRange) events.Set(RangeId, ranges.Start, ranges.End, 1, EventRange, "#MechJeb_NewUI_GraphRange");
                    for (int i = 0; i < ranges.PointCount; i++)
                        events.Set(PointId + i, ranges.Point(i), double.NaN, 1, EventPoint, "#MechJeb_NewUI_GraphPoint");
                }

                events.EndUpdate();
            }

            events.SetNow(n > 0 ? XOf(n - 1) : double.NaN);
        }
    }
}
