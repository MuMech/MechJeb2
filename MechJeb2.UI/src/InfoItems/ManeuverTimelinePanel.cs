using System;
using System.Collections.Generic;
using KodeKit;
using KodeKit.Headless;
using KodeUI;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech.UI
{
    /// <summary>
    /// The "Maneuver timeline" general info item (MechJebModuleNodeExecutor.ManeuverTimeline): the vessel's maneuver nodes ahead on
    /// a <see cref="Timeline"/> along universal time, each node an instant on the top lane with its Δv, its burn a span on the
    /// second (from the executor's lead before the node, as long as the executor reckons it from the stages as they are now, each
    /// burn as if it were the next). "Now" moves with time. A line under the strip tells the selected node's (or burn's) countdown,
    /// Δv and burn time, or that there is no node. The strip pans and zooms; the axis runs from now (or the first burn) to the last
    /// burn's end, with a margin. The nodes are read every frame without allocation; the items and the burn times are made again
    /// when a node is added, moved or removed, and the burn times once a second (the stages change as the vessel burns).
    /// </summary>
    public sealed class ManeuverTimelinePanel
    {
        public const int KindNode = 0, KindBurn = 1;
        public const int NodeId = 0, BurnId = 1000;

        readonly MechJebModuleNodeExecutor executor;
        readonly Timeline timeline;
        readonly List<double> nodeUT = new List<double>(), nodeDv = new List<double>();
        readonly List<double> burnStart = new List<double>(), burnLength = new List<double>();
        float burnsTime = float.NegativeInfinity;
        string readout = "";
        long readoutSecond = long.MinValue;
        int readoutVersion = -1;

        static readonly Color[] colors = { new Color(0.45f, 0.75f, 1f), new Color(1f, 0.62f, 0.30f) };

        /// <summary>The panel shown in a window (for tests: by the strip's root).</summary>
        public static readonly List<ManeuverTimelinePanel> Shown = new List<ManeuverTimelinePanel>();

        public TimelineModel Model => timeline.Model;
        public Timeline Strip => timeline;

        /// <summary>The line under the strip.</summary>
        public string Readout => readout;

        ManeuverTimelinePanel(KodeWindow w, UIObject parent, MechJebModuleNodeExecutor executor)
        {
            this.executor = executor;
            var view = new ViewportModel(0, 1) { AutoFollow = false, ResetFollows = false, MinSpan = 1 };
            var model = new TimelineModel(view) { LaneCount = 2 };
            Layout row = KodeWindow.Row(parent);
            timeline = new Timeline(w, row, model, 360)
            {
                LabelText = Label,
                ItemColor = it => colors[it.Kind]
            };
            UIText line = w.BoundLabel(parent, "Node 9 in 999d 99h 99m 99s: Δv 9999.9 m/s, burn 99h 99m 99s", () => readout, true);
            KodeWindow.ToneText(line, KodeWindow.Tone.Dim);
            w.AddSync(Sync);
            w.Behaviour.AddRelocalize(() => readoutVersion = -1);
            Shown.Add(this);
            timeline.Strip.gameObject.AddComponent<Forget>().Panel = this;
        }

        public static void Build(KodeWindow w, UIObject parent, MechJebModuleNodeExecutor executor) => new ManeuverTimelinePanel(w, parent, executor);

        // the panel leaves the test list with its strip (a rebuild or the window's end)
        sealed class Forget : MonoBehaviour
        {
            public ManeuverTimelinePanel Panel;
            void OnDestroy() => Shown.Remove(Panel);
        }

        Vessel Vessel => executor.Vessel;

        string Label(TimelineItem it)
        {
            int n = (it.Kind == KindNode ? it.Id - NodeId : it.Id - BurnId) + 1;
            if (it.Kind == KindNode) return Localizer.Format("#MechJeb_NewUI_TimelineNode", n.ToString(), it.Value.ToSI() + "m/s");
            return Localizer.Format("#MechJeb_NewUI_TimelineBurn", GuiUtils.TimeToDHMS(it.Width, 1));
        }

        void Sync()
        {
            Vessel v = Vessel;
            double now = Planetarium.GetUniversalTime();
            TimelineModel model = timeline.Model;
            bool changed = ReadNodes(v);
            // the burns follow the stages: once a second
            if (changed || Time.unscaledTime - burnsTime >= 1)
            {
                burnsTime = Time.unscaledTime;
                changed |= TimeBurns();
            }

            if (changed) Rebuild(now);
            model.SetNow(now);
            ViewportModel view = model.View;
            if (now < view.DataMin || now > view.DataMax) SetData(now);
            UpdateReadout(now);
        }

        // the nodes' times and Δv: true when one was added, moved or removed
        bool ReadNodes(Vessel v)
        {
            List<ManeuverNode> nodes = v != null && v.patchedConicSolver != null ? v.patchedConicSolver.maneuverNodes : null;
            int count = nodes?.Count ?? 0;
            bool changed = count != nodeUT.Count;
            while (nodeUT.Count < count)
            {
                nodeUT.Add(double.NaN);
                nodeDv.Add(double.NaN);
            }

            if (nodeUT.Count > count)
            {
                nodeUT.RemoveRange(count, nodeUT.Count - count);
                nodeDv.RemoveRange(count, nodeDv.Count - count);
            }

            for (int i = 0; i < count; i++)
            {
                ManeuverNode node = nodes[i];
                double ut = node.UT, dv = node.DeltaV.magnitude;
                if (ut.Equals(nodeUT[i]) && Math.Abs(dv - nodeDv[i]) < 1e-3) continue;
                nodeUT[i] = ut;
                nodeDv[i] = dv;
                changed = true;
            }

            return changed;
        }

        // each burn from the stages as they are now: true when one changed by more than a tenth of a second
        bool TimeBurns()
        {
            bool changed = burnStart.Count != nodeUT.Count;
            while (burnStart.Count < nodeUT.Count)
            {
                burnStart.Add(double.NaN);
                burnLength.Add(double.NaN);
            }

            if (burnStart.Count > nodeUT.Count)
            {
                burnStart.RemoveRange(nodeUT.Count, burnStart.Count - nodeUT.Count);
                burnLength.RemoveRange(nodeUT.Count, burnLength.Count - nodeUT.Count);
            }

            for (int i = 0; i < nodeUT.Count; i++)
            {
                double length = executor.BurnDuration(nodeDv[i], out double half);
                double start = nodeUT[i] - half;
                if (Math.Abs(start - burnStart[i]) < 0.1 && Math.Abs(length - burnLength[i]) < 0.1) continue;
                burnStart[i] = start;
                burnLength[i] = length;
                changed = true;
            }

            return changed;
        }

        void Rebuild(double now)
        {
            TimelineModel model = timeline.Model;
            model.BeginUpdate();
            for (int i = 0; i < nodeUT.Count; i++)
            {
                model.Set(NodeId + i, nodeUT[i], double.NaN, 0, KindNode, "#MechJeb_NewUI_TimelineNode", nodeDv[i]);
                if (burnLength[i] > 0) model.Set(BurnId + i, burnStart[i], burnStart[i] + burnLength[i], 1, KindBurn, "#MechJeb_NewUI_TimelineBurn", nodeDv[i]);
            }

            model.EndUpdate();
            SetData(now);
        }

        // from now (or the first burn) to the last burn's end, a twentieth of that on each side; a minute ahead without a node
        void SetData(double now)
        {
            TimelineModel model = timeline.Model;
            double min = now, max = now + 60;
            if (model.Count > 0)
            {
                min = Math.Min(now, model.ItemsMin);
                max = Math.Max(now, model.ItemsMax);
            }

            double pad = Math.Max(5, (max - min) / 20);
            model.View.SetData(min - pad, max + pad);
        }

        void UpdateReadout(double now)
        {
            TimelineModel model = timeline.Model;
            TimelineItem sel = model.Selected;
            if (sel == null && model.Count > 0) sel = model.Ordered(0);
            int node = sel == null ? -1 : sel.Kind == KindNode ? sel.Id - NodeId : sel.Id - BurnId;
            if (node >= nodeUT.Count) node = -1;
            long second = node >= 0 ? (long)Math.Floor(nodeUT[node] - now) : 0;
            int version = model.Version * 31 + node;
            if (second == readoutSecond && version == readoutVersion) return;
            readoutSecond = second;
            readoutVersion = version;
            if (node < 0)
            {
                readout = KodeWindow.Loc("#MechJeb_NewUI_TimelineNoNodes");
                return;
            }

            readout = Localizer.Format("#MechJeb_NewUI_TimelineSelected", (node + 1).ToString(), GuiUtils.TimeToDHMS(nodeUT[node] - now),
                nodeDv[node].ToSI() + "m/s", GuiUtils.TimeToDHMS(burnLength[node], 1));
        }
    }
}
