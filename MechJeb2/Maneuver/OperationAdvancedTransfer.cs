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
        static string[] modeNames = {Localizer.Format("#MechJeb_adv_modeName1"), Localizer.Format("#MechJeb_adv_modeName2") };//"Limited time","Porkchop selection"
        public override string getName() { return Localizer.Format("#MechJeb_AdvancedTransfer_title");}//"advanced transfer to another planet"

        double minDepartureTime;
        double minTransferTime;
        double maxDepartureTime;
        double maxTransferTime;

        public EditableTime maxArrivalTime = new EditableTime();

        bool includeCaptureBurn = false;

        const double minSamplingStep = 12 * 3600;

        private Mode selectionMode = Mode.Porkchop;
        int windowWidth;

        TransferCalculator worker;
        private PlotArea plot;

        private static Texture2D texture;

        bool _draggable = true;
        public override bool draggable { get { return _draggable;}}

        const int porkchop_Height = 200;

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
                worker.stop = true;
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
            maxTransferTime = hohmann_transfer_time * 1.5;
            maxArrivalTime.val = (synodic_period + hohmann_transfer_time) * 1.5;
        }

        private bool layoutSkipped = false;

        private void DoPorkchopGui(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
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
            if (worker.Finished && worker.computed.GetLength(1) == porkchop_Height)
            {
                if (plot == null && Event.current.type == EventType.Layout)
                {

                    int width = worker.computed.GetLength(0);
                    int height = worker.computed.GetLength(1);

                    if (texture != null && (texture.width != width || texture.height != height))
                    {
                        Object.Destroy(texture);
                        texture = null;
                    }

                    if (texture == null)
                        texture = new Texture2D(width, height, TextureFormat.RGB24, false);

                    Porkchop.RefreshTexture(worker.computed, texture);

                    plot = new PlotArea(
                        worker.minDepartureTime,
                        worker.maxDepartureTime,
                        worker.minTransferTime,
                        worker.maxTransferTime,
                        texture,
                        (xmin, xmax, ymin, ymax) => {
                            minDepartureTime = Math.Max(xmin, universalTime);
                            maxDepartureTime = xmax;
                            minTransferTime = Math.Max(ymin, 3600);
                            maxTransferTime = ymax;
                            GUI.changed = true;
                        });
                    plot.selectedPoint = new int[]{worker.bestDate, worker.bestDuration};
                }
            }
            if (plot != null)
            {
                var point = plot.selectedPoint;
                if (plot.hoveredPoint != null)
                    point = plot.hoveredPoint;

                var p = worker.computed[point[0], point[1]];
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
                GUIStyle progressStyle = new GUIStyle
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

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_adv_label2"));//"Select: "
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format("#MechJeb_adv_button1")))//Lowest ΔV
            {
                plot.selectedPoint = new int[]{ worker.bestDate, worker.bestDuration };
                GUI.changed = false;
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_adv_button2")))//ASAP
            {
                int bestDuration = 0;
                for (int i = 1; i < worker.computed.GetLength(1); i++)
                {
                    if (worker.computed[0, bestDuration] > worker.computed[0, i])
                        bestDuration = i;
                }
                plot.selectedPoint = new int[]{ 0, bestDuration };
                GUI.changed = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(Localizer.Format("#MechJeb_adv_label3") + departure);//Departure in
            GUILayout.Label(Localizer.Format("#MechJeb_adv_label4") + duration);//Transit duration
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            _draggable = true;
            if (worker != null && !target.NormalTargetExists && Event.current.type == EventType.Layout)
            {
                worker.stop = true;
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

            if (worker == null || worker.destinationOrbit != target.TargetOrbit || worker.originOrbit != o)
                ComputeTimes(o, target.TargetOrbit, universalTime);

            if (GUI.changed || worker == null || worker.destinationOrbit != target.TargetOrbit || worker.originOrbit != o)
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

            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();

            if (worker.arrivalDate < 0 )
            {
                throw new OperationException(Localizer.Format("#MechJeb_adv_Exception3"));//Computation failed
            }
            if (selectionMode == Mode.Porkchop)
            {
                if (plot == null || plot.selectedPoint == null)
                    throw new OperationException(Localizer.Format("#MechJeb_adv_Exception4"));//Invalid point selected.
                NodeList.Add( worker.OptimizeEjection(
                    worker.DateFromIndex(plot.selectedPoint[0]),
                    o, worker.destinationOrbit, target.Target as CelestialBody,
                    worker.DateFromIndex(plot.selectedPoint[0]) + worker.DurationFromIndex(plot.selectedPoint[1]),
                    UT)
                        );
                return NodeList;
            }

            NodeList.Add( worker.OptimizeEjection(
                    worker.DateFromIndex(worker.bestDate),
                    o, worker.destinationOrbit, target.Target as CelestialBody,
                    worker.DateFromIndex(worker.bestDate) + worker.DurationFromIndex(worker.bestDuration),
                    UT)
                    );
            return NodeList;
        }
    }
}
