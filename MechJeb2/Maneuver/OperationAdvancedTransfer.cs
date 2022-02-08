using System;
using UnityEngine;
using Object = UnityEngine.Object;
using KSP.Localization;
using System.Collections;
using System.Collections.Generic;
namespace MuMech
{
    public class OperationAdvancedTransfer : Operation
    {
        enum Mode
        {
            LimitedTime,
            Porkchop
        }
        static readonly string[] modeNames = {Localizer.Format("#MechJeb_adv_modeName1"), Localizer.Format("#MechJeb_adv_modeName2") };//"Limited time","Porkchop selection"
        public override string getName() { return Localizer.Format("#MechJeb_AdvancedTransfer_title");}//"advanced transfer to another planet"

        double minDepartureTime;
        double minTransferTime;
        double maxDepartureTime;
        double maxTransferTime;

        public EditableTime maxArrivalTime = new EditableTime();

        bool includeCaptureBurn = false;

        EditableDouble periapsisHeight = new EditableDouble(0);

        const double minSamplingStep = 12 * 3600;

        private Mode selectionMode = Mode.Porkchop;
        int windowWidth;

        private CelestialBody lastTargetCelestial;

        TransferCalculator worker;
        private PlotArea plot;

        private static Texture2D texture;

        bool _draggable = true;
        public override bool draggable { get { return _draggable;}}

        const int porkchop_Height = 200;

        private static GUIStyle progressStyle;

        private string CheckPreconditions(Orbit o, MechJebModuleTargetController target)
        {
            if (o.eccentricity >= 1)
                return Localizer.Format("#MechJeb_adv_Preconditions1");//initial orbit must not be hyperbolic

            if (o.ApR >= o.referenceBody.sphereOfInfluence)
                return Localizer.Format("#MechJeb_adv_Preconditions2",o.referenceBody.displayName);//"initial orbit must not escape " " sphere of influence."

            if (!target.NormalTargetExists)
                return (Localizer.Format("#MechJeb_adv_Preconditions3"));//"must select a target for the interplanetary transfer."

            if (o.referenceBody.referenceBody == null)
                return Localizer.Format("#MechJeb_adv_Preconditions4",o.referenceBody.displayName);//"doesn't make sense to plot an interplanetary transfer from an orbit around <<1>> ."

            if (o.referenceBody.referenceBody != target.TargetOrbit.referenceBody)
            {
                if (o.referenceBody == target.TargetOrbit.referenceBody)
                    return Localizer.Format("#MechJeb_adv_Preconditions5", o.referenceBody.displayName) ;//"use regular Hohmann transfer function to intercept another body orbiting <<1>>."
                return Localizer.Format("#MechJeb_adv_Preconditions6",o.referenceBody.displayName,o.referenceBody.displayName,o.referenceBody.referenceBody.displayName);//"an interplanetary transfer from within <<1>>'s sphere of influence must target a body that orbits <<2>>'s parent,<<3>> "
            }

            if (o.referenceBody == Planetarium.fetch.Sun)
            {
                return Localizer.Format("#MechJeb_adv_Preconditions7");//"use regular Hohmann transfer function to intercept another body orbiting the Sun."
            }

            if (target.Target is CelestialBody && o.referenceBody == target.targetBody)
            {
                return Localizer.Format("#MechJeb_adv_Preconditions8",o.referenceBody.displayName );//you are already orbiting <<1>>.
            }

            return null;
        }

        void ComputeStuff(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            errorMessage = CheckPreconditions(o, target);
            if (errorMessage == null)
                errorMessage = "";
            else
                return;

            if (worker != null)
                worker.Stop = true;
            plot = null;

            switch (selectionMode)
            {
            case Mode.LimitedTime:
                worker = new TransferCalculator (o, target.TargetOrbit, universalTime, maxArrivalTime, minSamplingStep, includeCaptureBurn);
                break;
            case Mode.Porkchop:
                worker = new AllGraphTransferCalculator(o, target.TargetOrbit, minDepartureTime, maxDepartureTime, minTransferTime, maxTransferTime, windowWidth, porkchop_Height, includeCaptureBurn);
                break;
            }
        }

        void ComputeTimes(Orbit o, Orbit destination, double universalTime)
        {
            if (destination == null || o == null || o.referenceBody.orbit == null)
                return;

            double synodic_period = o.referenceBody.orbit.SynodicPeriod(destination);
            double hohmann_transfer_time = OrbitUtil.GetTransferTime(o.referenceBody.orbit, destination);

            // Both orbit have the same period
            if (double.IsInfinity(synodic_period))
                synodic_period = o.referenceBody.orbit.period;

            minDepartureTime = universalTime;
            minTransferTime = 3600;

            maxDepartureTime = minDepartureTime + synodic_period * 1.5;
            maxTransferTime = hohmann_transfer_time * 2.0;
            maxArrivalTime.val = synodic_period * 1.5 + hohmann_transfer_time * 2.0;
        }

        private bool layoutSkipped = false;

        private void DoPorkchopGui(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            CelestialBody targetCelestial = target.Target as CelestialBody;

            // That mess is why you should not compute anything inside a GUI call
            // TODO : rewrite all that...
            if (worker == null)
            {
                if (Event.current.type == EventType.Layout)
                    layoutSkipped = true;
                return;
            }
            if (Event.current.type == EventType.Layout)
                layoutSkipped = false;
            if (layoutSkipped)
                return;

            string dv = " - ";
            string departure = " - ";
            string duration = " - ";
            if (worker.Finished && worker.Computed.GetLength(1) == porkchop_Height)
            {
                if (plot == null && Event.current.type == EventType.Layout)
                {

                    int width = worker.Computed.GetLength(0);
                    int height = worker.Computed.GetLength(1);

                    if (texture != null && (texture.width != width || texture.height != height))
                    {
                        Object.Destroy(texture);
                        texture = null;
                    }

                    if (texture == null)
                        texture = new Texture2D(width, height, TextureFormat.RGB24, false);

                    Porkchop.RefreshTexture(worker.Computed, texture);

                    plot = new PlotArea(
                        worker.MinDepartureTime,
                        worker.MaxDepartureTime,
                        worker.MinTransferTime,
                        worker.MaxTransferTime,
                        texture,
                        (xmin, xmax, ymin, ymax) => {
                            minDepartureTime = Math.Max(xmin, universalTime);
                            maxDepartureTime = xmax;
                            minTransferTime = Math.Max(ymin, 3600);
                            maxTransferTime = ymax;
                            GUI.changed = true;
                        });
                    plot.selectedPoint = new int[]{worker.BestDate, worker.BestDuration};
                }
            }
            if (plot != null)
            {
                var point = plot.selectedPoint;
                if (plot.hoveredPoint != null)
                    point = plot.hoveredPoint;

                var p = worker.Computed[point[0], point[1]];
                if (p > 0)
                {
                    dv = MuUtils.ToSI(p) + "m/s";
                    if (worker.DateFromIndex(point[0]) < Planetarium.GetUniversalTime())
                        departure = Localizer.Format("#MechJeb_adv_label1");//any time now
                    else
                        departure = GuiUtils.TimeToDHMS(worker.DateFromIndex(point[0]) - Planetarium.GetUniversalTime());
                    duration = GuiUtils.TimeToDHMS(worker.DurationFromIndex(point[1]));
                }
                plot.DoGUI();
                if (!plot.draggable) _draggable = false;
            }
            else
            {
                if (progressStyle == null)
                    progressStyle = new GUIStyle
                    {
                        font = GuiUtils.skin.font,
                        fontSize = GuiUtils.skin.label.fontSize,
                        fontStyle = GuiUtils.skin.label.fontStyle,
                        normal = {textColor = GuiUtils.skin.label.normal.textColor}
                    
                    };
                GUILayout.Box(Localizer.Format("#MechJeb_adv_computing") + worker.Progress + "%", progressStyle, GUILayout.Width(windowWidth), GUILayout.Height(porkchop_Height));//"Computing:"
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("ΔV: " + dv);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format("#MechJeb_adv_reset_button"), GuiUtils.yellowOnHover))
                ComputeTimes(o, target.TargetOrbit, universalTime);
            GUILayout.EndHorizontal();

            includeCaptureBurn = GUILayout.Toggle(includeCaptureBurn, Localizer.Format("#MechJeb_adv_captureburn"));//"include capture burn"

            // fixup the default value of the periapsis if the target changes
            if (targetCelestial != null && lastTargetCelestial != targetCelestial)
            {
                if (targetCelestial.atmosphere)
                {
                    periapsisHeight = targetCelestial.atmosphereDepth/1000 + 10;
                }
                else
                {
                    periapsisHeight = 100;
                }
            }

            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_adv_periapsis"), periapsisHeight, "km");

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_adv_label2"));//"Select: "
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format("#MechJeb_adv_button1")))//Lowest ΔV
            {
                plot.selectedPoint = new int[]{ worker.BestDate, worker.BestDuration };
                GUI.changed = false;
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_adv_button2")))//ASAP
            {
                int bestDuration = 0;
                for (int i = 1; i < worker.Computed.GetLength(1); i++)
                {
                    if (worker.Computed[0, bestDuration] > worker.Computed[0, i])
                        bestDuration = i;
                }
                plot.selectedPoint = new int[]{ 0, bestDuration };
                GUI.changed = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(Localizer.Format("#MechJeb_adv_label3") + " " + departure);//Departure in
            GUILayout.Label(Localizer.Format("#MechJeb_adv_label4") + " " + duration);//Transit duration

            lastTargetCelestial = targetCelestial;
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            _draggable = true;
            if (worker != null && !target.NormalTargetExists && Event.current.type == EventType.Layout)
            {
                worker.Stop = true;
                worker = null;
                plot = null;
            }

            selectionMode = (Mode) GuiUtils.ComboBox.Box((int) selectionMode, modeNames, this);
            if (Event.current.type == EventType.Repaint)
                windowWidth = (int)GUILayoutUtility.GetLastRect().width;

            switch (selectionMode)
            {
            case Mode.LimitedTime:
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_adv_label5"), maxArrivalTime);//Max arrival time
                if (worker != null && !worker.Finished)
                    GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_adv_computing") + worker.Progress + "%");
                break;
            case Mode.Porkchop:
                DoPorkchopGui(o, universalTime, target);
                break;
            }

            if (worker == null || worker.DestinationOrbit != target.TargetOrbit || worker.OriginOrbit != o)
                ComputeTimes(o, target.TargetOrbit, universalTime);

            if (GUI.changed || worker == null || worker.DestinationOrbit != target.TargetOrbit || worker.OriginOrbit != o)
                ComputeStuff(o, universalTime, target);
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double UT, MechJebModuleTargetController target)
        {
            // Check preconditions
            string message = CheckPreconditions(o, target);
            if (message != null)
                throw new OperationException(message);

            // Check if computation is finished
            if (worker != null && !worker.Finished)
                throw new OperationException(Localizer.Format("#MechJeb_adv_Exception1"));//Computation not finished
            if (worker == null)
            {
                ComputeStuff(o, UT, target);
                throw new OperationException(Localizer.Format("#MechJeb_adv_Exception2"));//Started computation
            }

            if (worker.ArrivalDate < 0 )
            {
                throw new OperationException(Localizer.Format("#MechJeb_adv_Exception3"));//Computation failed
            }

            double target_PeR = lastTargetCelestial.Radius + periapsisHeight * 1000;

            if (selectionMode == Mode.Porkchop)
            {
                if (plot == null || plot.selectedPoint == null)
                    throw new OperationException(Localizer.Format("#MechJeb_adv_Exception4"));//Invalid point selected.
                return worker.OptimizeEjection(
                    worker.DateFromIndex(plot.selectedPoint[0]),
                    o, target.Target as CelestialBody,
                    worker.DateFromIndex(plot.selectedPoint[0]) + worker.DurationFromIndex(plot.selectedPoint[1]),
                    UT, target_PeR, includeCaptureBurn);
            }

            return worker.OptimizeEjection(
                    worker.DateFromIndex(worker.BestDate),
                    o, target.Target as CelestialBody,
                    worker.DateFromIndex(worker.BestDate) + worker.DurationFromIndex(worker.BestDuration),
                    UT, target_PeR, includeCaptureBurn);
        }
    }
}
