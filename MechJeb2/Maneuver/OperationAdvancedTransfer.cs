extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;
using Object = UnityEngine.Object;

#nullable enable

namespace MuMech
{
    [UsedImplicitly]
    public class OperationAdvancedTransfer : Operation
    {
        private enum Mode
        {
            LIMITED_TIME,
            PORKCHOP
        }

        private static readonly string _name = Localizer.Format("#MechJeb_AdvancedTransfer_title");
        public override string GetName() => _name;

        private static readonly string[]
            _modeNames = { Localizer.Format("#MechJeb_adv_modeName1"), Localizer.Format("#MechJeb_adv_modeName2") }; //"Limited time","Porkchop selection"

        private double _minDepartureTime;
        private double _minTransferTime;
        private double _maxDepartureTime;
        private double _maxTransferTime;

        public readonly EditableTime MaxArrivalTime = new EditableTime();

        private bool _includeCaptureBurn;

        private EditableDouble _periapsisHeight = new EditableDouble(0);

        private const double MIN_SAMPLING_STEP = 12 * 3600;

        private Mode _selectionMode = Mode.PORKCHOP;
        private int _windowWidth;

        private CelestialBody? _lastTargetCelestial;
        private TransferCalculator? _worker;
        private PlotArea? _plot;

        private static Texture2D? _texture;

        private bool _draggable = true;
        public override bool Draggable => _draggable;

        private const int PORKCHOP_HEIGHT = 200;

        private static GUIStyle? _progressStyle;

        private string? CheckPreconditions(Orbit o, MechJebModuleTargetController target)
        {
            if (o.eccentricity >= 1)
                return Localizer.Format("#MechJeb_adv_Preconditions1"); //initial orbit must not be hyperbolic

            if (o.ApR >= o.referenceBody.sphereOfInfluence)
                return Localizer.Format("#MechJeb_adv_Preconditions2",
                    o.referenceBody.displayName.LocalizeRemoveGender()); //"initial orbit must not escape " " sphere of influence."

            if (!target.NormalTargetExists)
                return Localizer.Format("#MechJeb_adv_Preconditions3"); //"must select a target for the interplanetary transfer."

            if (o.referenceBody.referenceBody == null)
                return Localizer.Format("#MechJeb_adv_Preconditions4",
                    o.referenceBody.displayName
                       .LocalizeRemoveGender()); //"doesn't make sense to plot an interplanetary transfer from an orbit around <<1>> ."

            if (o.referenceBody.referenceBody != target.TargetOrbit.referenceBody)
            {
                if (o.referenceBody == target.TargetOrbit.referenceBody)
                    return Localizer.Format("#MechJeb_adv_Preconditions5",
                        o.referenceBody.displayName
                           .LocalizeRemoveGender()); //"use regular Hohmann transfer function to intercept another body orbiting <<1>>."
                return Localizer.Format("#MechJeb_adv_Preconditions6", o.referenceBody.displayName.LocalizeRemoveGender(),
                    o.referenceBody.displayName.LocalizeRemoveGender(),
                    o.referenceBody.referenceBody.displayName
                       .LocalizeRemoveGender()); //"an interplanetary transfer from within <<1>>'s sphere of influence must target a body that orbits <<2>>'s parent,<<3>> "
            }

            if (o.referenceBody == Planetarium.fetch.Sun)
            {
                return
                    Localizer.Format(
                        "#MechJeb_adv_Preconditions7"); //"use regular Hohmann transfer function to intercept another body orbiting the Sun."
            }

            if (target.Target is CelestialBody && o.referenceBody == target.targetBody)
            {
                return Localizer.Format("#MechJeb_adv_Preconditions8",
                    o.referenceBody.displayName.LocalizeRemoveGender()); //you are already orbiting <<1>>.
            }

            return null;
        }

        private void ComputeStuff(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            ErrorMessage = CheckPreconditions(o, target);
            if (ErrorMessage == null)
                ErrorMessage = "";
            else
                return;

            if (_worker != null)
                _worker.Stop = true;
            _plot = null;

            switch (_selectionMode)
            {
                case Mode.LIMITED_TIME:
                    _worker = new TransferCalculator(o, target.TargetOrbit, universalTime, MaxArrivalTime, MIN_SAMPLING_STEP, _includeCaptureBurn);
                    break;
                case Mode.PORKCHOP:
                    _worker = new AllGraphTransferCalculator(o, target.TargetOrbit, _minDepartureTime, _maxDepartureTime, _minTransferTime,
                        _maxTransferTime, _windowWidth, PORKCHOP_HEIGHT, _includeCaptureBurn);
                    break;
            }
        }

        private void ComputeTimes(Orbit? o, Orbit? destination, double universalTime)
        {
            if (destination == null || o?.referenceBody.orbit == null)
                return;

            double synodicPeriod = o.referenceBody.orbit.SynodicPeriod(destination);
            double hohmannTransferTime = OrbitUtil.GetTransferTime(o.referenceBody.orbit, destination);

            // Both orbit have the same period
            if (double.IsInfinity(synodicPeriod))
                synodicPeriod = o.referenceBody.orbit.period;

            _minDepartureTime = universalTime;
            _minTransferTime = 3600;

            _maxDepartureTime = _minDepartureTime + synodicPeriod * 1.5;
            _maxTransferTime = hohmannTransferTime * 2.0;
            MaxArrivalTime.Val = synodicPeriod * 1.5 + hohmannTransferTime * 2.0;
        }

        private bool _layoutSkipped;

        private void DoPorkchopGui(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            var targetCelestial = target.Target as CelestialBody;

            // That mess is why you should not compute anything inside a GUI call
            // TODO : rewrite all that...
            if (_worker == null)
            {
                if (Event.current.type == EventType.Layout)
                    _layoutSkipped = true;
                return;
            }

            if (Event.current.type == EventType.Layout)
                _layoutSkipped = false;
            if (_layoutSkipped)
                return;

            string dv = " - ";
            string departure = " - ";
            string duration = " - ";
            if (_worker.Finished && _worker.Computed.GetLength(1) == PORKCHOP_HEIGHT)
            {
                if (_plot == null && Event.current.type == EventType.Layout)
                {
                    int width = _worker.Computed.GetLength(0);
                    int height = _worker.Computed.GetLength(1);

                    if (!(_texture is null) && (_texture.width != width || _texture.height != height))
                    {
                        Object.Destroy(_texture);
                        _texture = null;
                    }

                    _texture ??= new Texture2D(width, height, TextureFormat.RGB24, false);

                    Porkchop.RefreshTexture(_worker.Computed, _texture);

                    _plot = new PlotArea(
                        _worker.MinDepartureTime,
                        _worker.MaxDepartureTime,
                        _worker.MinTransferTime,
                        _worker.MaxTransferTime,
                        _texture,
                        (xMin, xMax, yMin, yMax) =>
                        {
                            _minDepartureTime = Math.Max(xMin, universalTime);
                            _maxDepartureTime = xMax;
                            _minTransferTime = Math.Max(yMin, 3600);
                            _maxTransferTime = yMax;
                            GUI.changed = true;
                        }) { SelectedPoint = new[] { _worker.BestDate, _worker.BestDuration } };
                }
            }

            if (_plot != null)
            {
                int[] point = _plot.SelectedPoint;
                if (_plot.HoveredPoint != null)
                    point = _plot.HoveredPoint;

                double p = _worker.Computed[point[0], point[1]];
                if (p > 0)
                {
                    dv = p.ToSI() + "m/s";
                    departure = _worker.DateFromIndex(point[0]) < Planetarium.GetUniversalTime()
                        ? Localizer.Format("#MechJeb_adv_label1")
                        : //any time now
                        GuiUtils.TimeToDHMS(_worker.DateFromIndex(point[0]) - Planetarium.GetUniversalTime());
                    duration = GuiUtils.TimeToDHMS(_worker.DurationFromIndex(point[1]));
                }

                _plot.DoGUI();
                if (!_plot.Draggable) _draggable = false;
            }
            else
            {
                _progressStyle ??= new GUIStyle { font = GuiUtils.Skin.font, fontSize = GuiUtils.Skin.label.fontSize, fontStyle = GuiUtils.Skin.label.fontStyle, normal = { textColor = GuiUtils.Skin.label.normal.textColor } };
                GUILayout.Box(Localizer.Format("#MechJeb_adv_computing") + _worker.Progress + "%", _progressStyle, GuiUtils.LayoutWidth(_windowWidth),
                    GUILayout.Height(PORKCHOP_HEIGHT)); //"Computing:"
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("ΔV: " + dv);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format("#MechJeb_adv_reset_button"), GuiUtils.YellowOnHover))
                ComputeTimes(o, target.TargetOrbit, universalTime);
            GUILayout.EndHorizontal();

            _includeCaptureBurn = GUILayout.Toggle(_includeCaptureBurn, Localizer.Format("#MechJeb_adv_captureburn")); //"include capture burn"

            // fixup the default value of the periapsis if the target changes
            if (targetCelestial != null && _lastTargetCelestial != targetCelestial)
            {
                if (targetCelestial.atmosphere)
                {
                    _periapsisHeight = targetCelestial.atmosphereDepth / 1000 + 10;
                }
                else
                {
                    _periapsisHeight = 100;
                }
            }

            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_adv_periapsis"), _periapsisHeight, "km");

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_adv_label2")); //"Select: "
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format("#MechJeb_adv_button1"))) //Lowest ΔV
            {
                if (_plot != null)
                {
                    _plot.SelectedPoint = new[] { _worker.BestDate, _worker.BestDuration };
                    GUI.changed = false;
                }
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_adv_button2"))) //ASAP
            {
                if (_plot != null)
                {
                    int bestDuration = 0;
                    for (int i = 1; i < _worker.Computed.GetLength(1); i++)
                    {
                        if (_worker.Computed[0, bestDuration] > _worker.Computed[0, i])
                            bestDuration = i;
                    }

                    _plot.SelectedPoint = new[] { 0, bestDuration };
                    GUI.changed = false;
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(Localizer.Format("#MechJeb_adv_label3") + " " + departure); //Departure in
            GUILayout.Label(Localizer.Format("#MechJeb_adv_label4") + " " + duration);  //Transit duration

            _lastTargetCelestial = targetCelestial;
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            _draggable = true;
            if (_worker != null && !target.NormalTargetExists && Event.current.type == EventType.Layout)
            {
                _worker.Stop = true;
                _worker = null;
                _plot = null;
            }

            _selectionMode = (Mode)GuiUtils.ComboBox.Box((int)_selectionMode, _modeNames, this);
            if (Event.current.type == EventType.Repaint)
                _windowWidth = (int)GUILayoutUtility.GetLastRect().width;

            switch (_selectionMode)
            {
                case Mode.LIMITED_TIME:
                    GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_adv_label5"), MaxArrivalTime); //Max arrival time
                    if (_worker is { Finished: false })
                        GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_adv_computing") + _worker.Progress + "%");
                    break;
                case Mode.PORKCHOP:
                    DoPorkchopGui(o, universalTime, target);
                    break;
            }

            if (_worker == null || _worker.DestinationOrbit != target.TargetOrbit || _worker.OriginOrbit != o)
                ComputeTimes(o, target.TargetOrbit, universalTime);

            if (GUI.changed || _worker == null || _worker.DestinationOrbit != target.TargetOrbit || _worker.OriginOrbit != o)
                ComputeStuff(o, universalTime, target);
        }

        private (double epoch, double arrivalDt, double arrivalDtLower, double arrivalDtUpper) ResolveTimes(TransferCalculator w)
        {
            int dateIndex = _plot?.SelectedPoint?[0] ?? w.BestDate;
            int durationIndex = _plot?.SelectedPoint?[1] ?? w.BestDuration;
            double epoch = w.DateFromIndex(dateIndex);
            double arrivalDt = w.DurationFromIndex(durationIndex);

            // If the user picks a point, bound the arrival DT to the point
            double arrivalBracket = (w.MaxDepartureTime - w.MinDepartureTime) / w.DateSamples;
            double arrivalDtLower = arrivalDt - 0.5 * arrivalBracket;
            double arrivalDtUpper = arrivalDt + 0.5 * arrivalBracket;

            // XXX: this is a bit of a hack to just let the optimizer decide
            if (dateIndex == w.BestDate && durationIndex == w.BestDuration)
            {
                arrivalDtLower = 0;
                arrivalDtUpper = double.PositiveInfinity;
            }

            if (_selectionMode == Mode.LIMITED_TIME && MaxArrivalTime > 0)
                arrivalDtUpper = MaxArrivalTime;

            return (epoch, arrivalDt, arrivalDtLower, arrivalDtUpper);
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double ut, MechJebModuleTargetController target)
        {
            // Check preconditions
            string? message = CheckPreconditions(o, target);
            if (message != null)
                throw new OperationException(message);

            switch (_worker)
            {
                case { Finished: false }:
                    throw new OperationException(Localizer.Format("#MechJeb_adv_Exception1")); //Computation not finished
                case null:
                    ComputeStuff(o, ut, target);
                    throw new OperationException(Localizer.Format("#MechJeb_adv_Exception2")); //Started computation
            }

            if (_worker.ArrivalDate < 0)
                throw new OperationException(Localizer.Format("#MechJeb_adv_Exception3")); //Computation failed

            double targetPeR = (_lastTargetCelestial?.Radius ?? 0) + _periapsisHeight * 1000;

            if (_selectionMode == Mode.PORKCHOP && _plot?.SelectedPoint == null)
                throw new OperationException(Localizer.Format("#MechJeb_adv_Exception4")); //Invalid point selected.

            // FIXME: we can now better expose user-tweakable brackets around arrivalDt
            (double epoch, double arrivalDt, double arrivalDtLower, double arrivalDtUpper) = ResolveTimes(_worker);

            return OrbitalManeuverCalculator.OptimizeEjectionToTarget(o, target, targetPeR, epoch, arrivalDt, arrivalDtLower, arrivalDtUpper);
        }
    }
}
