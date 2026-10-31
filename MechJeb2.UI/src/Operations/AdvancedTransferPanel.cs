using KodeKit;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// OperationAdvancedTransfer's parameters: the mode selector, the "limited time" field and the porkchop plot (KodeKit PlotView).
    /// The operation keeps the model and does what DoParametersGUI did while drawing in ViewUpdate (the planner view calls it every
    /// frame). Not ported from PlotArea: the mouse wheel zoom and the WASD / arrow keys pan.
    /// </summary>
    internal static class AdvancedTransferPanel
    {
        /// <summary>The texts under the plot, rebuilt when the selected/hovered point or the universal time (in seconds) changes.</summary>
        sealed class Readout
        {
            public string Dv = "ΔV:  - ";
            public string Departure = " - ";
            public string Duration = " - ";
            int x = -2, y = -2;
            bool ready;
            long second = -1;

            public void Refresh(OperationAdvancedTransfer op, int[] point)
            {
                int px = point?[0] ?? -1, py = point?[1] ?? -1;
                long s = Planetarium.fetch != null ? (long)Planetarium.GetUniversalTime() : 0;
                if (px == x && py == y && ready == op.PlotReady && s == second) return;
                x = px;
                y = py;
                ready = op.PlotReady;
                second = s;
                op.Readout(point, out string dv, out string departure, out string duration);
                Dv = "ΔV: " + dv;
                Departure = Localizer.Format("#MechJeb_adv_label3") + " " + departure; //Departure in
                Duration = Localizer.Format("#MechJeb_adv_label4") + " " + duration; //Transit duration
            }
        }

        public static void Build(OpContext c)
        {
            var op = (OperationAdvancedTransfer)c.Op;
            KodeWindow w = c.W;
            MechJebModuleManeuverPlanner planner = c.Planner;

            new ComboBox(w, c.Panel, () => op.ModeIndex, i => op.ModeIndex = i, () => OperationAdvancedTransfer.ModeNames);

            // Limited time
            Layout limited = KodeWindow.Panel(c.Panel);
            OpUi.LabeledField(w, limited, "#MechJeb_adv_label5", () => op.MaxArrivalTime, edited: op.MarkChanged); //Max arrival time
            UIText computing = w.BoundLabel(limited, op.ComputingText, true);
            w.Visible(computing, () => op.IsComputing);
            w.Visible(limited.gameObject, () => !op.IsPorkchopMode);

            // Porkchop selection (nothing is drawn while there is no worker, as in IMGUI)
            Layout porkchop = KodeWindow.Panel(c.Panel);
            w.Visible(porkchop.gameObject, () => op.IsPorkchopMode && op.HasWorker);

            // while the plot computes: a box of the plot's size with the progress
            LayoutPanel progress = KodeWindow.Box(porkchop);
            var progressSize = progress.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            progressSize.minWidth = progressSize.preferredWidth = OperationAdvancedTransfer.PLOT_WIDTH;
            progressSize.minHeight = progressSize.preferredHeight = OperationAdvancedTransfer.PLOT_HEIGHT;
            UIText progressText = w.BoundLabel(progress, op.ComputingText, true);
            progressText.tmpText.alignment = TextAlignmentOptions.Center;
            w.Visible(progress.gameObject, () => !op.PlotReady);

            var plot = new PlotView(porkchop, op.NativeTexture);
            plot.Clicked = (x, y) => op.NativeSelected = new[] { x, y };
            plot.ZoomBox = (x0, x1, y0, y1) =>
            {
                if (planner.Vessel == null) return;
                planner.GetOrbitAndUT(out _, out double ut);
                op.ViewZoom(x0, x1, y0, y1, ut);
            };
            w.Visible(plot.Image.transform.parent.gameObject, () => op.PlotReady);
            PickArea pick = plot.Image.GetComponent<PickArea>();

            var readout = new Readout();
            w.AddSync(() =>
            {
                if (!op.IsPorkchopMode || !op.HasWorker) return;
                // the hovered point wins over the selected one; HoveredPoint is stale once the pointer left
                readout.Refresh(op, pick.Hovered && plot.HoveredPoint != null ? plot.HoveredPoint : op.NativeSelected);
            });

            Layout dvRow = KodeWindow.Row(porkchop);
            w.BoundLabel(dvRow, () => readout.Dv, true);
            KodeWindow.Button(dvRow, "#MechJeb_adv_reset_button", () =>
            {
                if (planner.Vessel == null || c.Target == null) return;
                planner.GetOrbitAndUT(out Orbit o, out double ut);
                op.ResetTimes(o, c.Target, ut);
            });

            w.Toggle(porkchop, "#MechJeb_adv_captureburn", () => op.IncludeCaptureBurn, v => op.IncludeCaptureBurn = v); //include capture burn
            OpUi.LabeledField(w, porkchop, "#MechJeb_adv_periapsis", () => op.PeriapsisHeight, "km");
            OpUi.ToggledField(w, porkchop, "Inclination", () => op.TargetInclinationFlag, v => op.TargetInclinationFlag = v, () => op.TargetInclination, "°");

            Layout select = KodeWindow.Row(porkchop);
            KodeWindow.Label(select, "#MechJeb_adv_label2", true); //Select:
            KodeWindow.Button(select, "#MechJeb_adv_button1", op.SelectLowestDv); //Lowest ΔV
            KodeWindow.Button(select, "#MechJeb_adv_button2", op.SelectAsap); //ASAP

            w.BoundLabel(porkchop, () => readout.Departure, true);
            w.BoundLabel(porkchop, () => readout.Duration, true);
        }
    }
}
