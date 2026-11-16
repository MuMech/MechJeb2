using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using UnityEngine;
using static System.Math;

#nullable enable

namespace MuMech
{
    /// <summary>
    ///     The drag area PSG plans the ascent with (Cd × Aref, the only drag figure its model reads), worked out from the vessel.
    ///     <para />
    ///     A table of Cd·A against Mach for the vessel flying a little off its nose, as an ascent does (FLOW_AOA): stock aerodynamics
    ///     from the parts' drag cubes and drag models as FlightIntegrator applies them, FAR from FARAPI.CalculateVesselAeroForces.
    ///     PSG holds one constant, so the table is averaged along the ascent PSG planned, each point weighted by the dynamic pressure
    ///     over the mass (its share of the drag losses); with no plan yet, over Mach 0.5 to 2, where an ascent meets its maximum
    ///     dynamic pressure. In stock aerodynamics each stage of the plan has its own table, without the parts dropped by then.
    ///     <para />
    ///     Aref is the vessel's frontal area (the drag cube faces towards the airflow) or FAR's reference area, and Cd the drag area
    ///     over it.
    /// </summary>
    public class PSGAeroEstimate
    {
        public enum Model { NONE, STOCK, FAR }

        // Mach of the table, closer around Mach 1 where stock's drag curves turn
        private static readonly double[] _machs =
        {
            0.1, 0.3, 0.5, 0.7, 0.8, 0.85, 0.9, 0.95, 1.0, 1.05, 1.1, 1.2, 1.35, 1.5, 1.75, 2, 2.5, 3, 4, 5, 6, 8, 10, 15, 20, 25
        };

        // A rocket flies a degree or two off its nose through the dense air (1.6 to 2.3° on average, weighted by the dynamic
        // pressure, in stock ascents of Kerbal X), and a long body picks up drag fast with it: the table is for the airflow this
        // far off the nose, in the pitch plane.
        private const float FLOW_AOA = 2;

        // the vessel as it is
        private readonly double[] _cdA = new double[_machs.Length];

        // stock aerodynamics: the vessel in a later stage of the plan, without the parts dropped by then, read on first use
        private readonly Dictionary<int, double[]> _stageTables = new Dictionary<int, double[]>();

        private int _currentStage;

        private double _frontalArea;

        /// <summary>Which aerodynamics the table holds: NONE until the vessel has been read.</summary>
        public Model Source { get; private set; } = Model.NONE;

        /// <summary>The drag area PSG plans with, m².</summary>
        public double CdA { get; private set; }

        public double Cd { get; private set; }

        public double Aref { get; private set; }

        /// <summary>True when CdA is averaged along PSG's plan, false when over the default Mach range.</summary>
        public bool FromPlan { get; private set; }

        /// <summary>The Mach of the plan's maximum dynamic pressure (NaN without a plan).</summary>
        public double MachAtMaxQ { get; private set; } = double.NaN;

        private Vessel? _vessel;
        private CelestialBody? _body;
        private int _partCount = -1;
        private float _nextTable;

        // the table is read again this often, as parts deploy and fairings open without the part count changing
        private const float TABLE_PERIOD = 10;

        /// <summary>The drag area of the vessel as it is at <paramref name="mach" />, read off the table (0 before the first update).</summary>
        public double CdAAt(double mach) => CdAAt(_cdA, mach);

        private static double CdAAt(double[] table, double mach)
        {
            if (mach <= _machs[0])
                return table[0];
            for (int i = 1; i < _machs.Length; i++)
            {
                if (mach > _machs[i])
                    continue;
                double f = (mach - _machs[i - 1]) / (_machs[i] - _machs[i - 1]);
                return table[i - 1] + f * (table[i] - table[i - 1]);
            }

            return table[_machs.Length - 1];
        }

        /// <summary>
        ///     Reads the vessel again if needed and averages the table along <paramref name="plan" /> (a solution in SI units, its
        ///     clock the mission time, as the guidance flies), from <paramref name="met" /> on. False while nothing can be worked out:
        ///     no vessel, a body without atmosphere, a vessel still packed.
        /// </summary>
        public bool Update(Vessel? vessel, Solution? plan, double met)
        {
            if (vessel == null || vessel.mainBody == null || !vessel.mainBody.atmosphere || vessel.packed)
                return false;

            // FAR's read is slow (tens of milliseconds), so only a changed vessel is read again; stock's also every TABLE_PERIOD
            bool changed = vessel != _vessel || vessel.mainBody != _body || vessel.parts.Count != _partCount ||
                vessel.currentStage != _currentStage || Source == Model.NONE;
            if (changed || (Source == Model.STOCK && Time.unscaledTime >= _nextTable))
            {
                _currentStage = vessel.currentStage;
                if (changed)
                    _stageTables.Clear();
                if (!ReadTable(vessel))
                    return false;
                _vessel = vessel;
                _body = vessel.mainBody;
                _partCount = vessel.parts.Count;
                _nextTable = Time.unscaledTime + TABLE_PERIOD;
            }

            // once the rest of the plan is above the atmosphere, the last average along it stays: drag no longer matters there
            if (plan != null && AverageAlongPlan(plan, vessel, met))
                FromPlan = true;
            else if (plan == null || !FromPlan)
            {
                FromPlan = false;
                AverageDefault();
            }

            Aref = _frontalArea > 0 ? _frontalArea : CdA;
            Cd = Aref > 0 ? CdA / Aref : 0;
            return CdA > 0;
        }

        // the direction of the airflow (the velocity through the air) FLOW_AOA off the nose, in the pitch plane
        private static Vector3 Flow(Vessel vessel)
        {
            Transform reference = vessel.GetTransform();
            return Quaternion.AngleAxis(FLOW_AOA, reference.right) * reference.up;
        }

        private bool ReadTable(Vessel vessel)
        {
            if (VesselState.IsFARCorrectlyInitialized)
            {
                if (!ReadFAR(vessel, Flow(vessel)))
                    return false;
                Source = Model.FAR;
                return true;
            }

            ReadStock(vessel, Flow(vessel), _currentStage, _cdA);
            Source = Model.STOCK;
            return true;
        }

        // The table of the stage a plan's phase flies: in stock aerodynamics the vessel without the parts the stages from the
        // current one down to that one drop. FAR only has the vessel as it is.
        private double[] TableFor(Vessel vessel, int stage)
        {
            if (Source != Model.STOCK || stage >= _currentStage)
                return _cdA;
            if (!_stageTables.TryGetValue(stage, out double[] table))
            {
                table = new double[_machs.Length];
                ReadStock(vessel, Flow(vessel), stage, table);
                _stageTables[stage] = table;
            }

            return table;
        }

        // gone by the time the vessel flies that stage: decoupled (or a launch clamp released) in a stage from the next one down to it
        private bool Dropped(Part p, int stage)
        {
            for (int s = stage; s < _currentStage; s++)
                if (p.IsDecoupledInStage(s))
                    return true;
            return false;
        }

        // FlightIntegrator.UpdateAerodynamics for each part still on the vessel in that stage: CalculateDragValue (the drag cubes'
        // AreaDrag × the drag cube multiplier, or the part's own drag model) × the drag multiplier, in m² as the force is q × this.
        // The pseudo-Reynolds multiplier depends on the flight, so the average applies it.
        private void ReadStock(Vessel vessel, Vector3 forward, int stage, double[] table)
        {
            bool current = stage == _currentStage;
            Array.Clear(table, 0, table.Length);
            Array.Clear(_surfaceCdA, 0, _surfaceCdA.Length);
            if (current)
                _frontalArea = 0;

            var data = new DragCubeList.CubeData();
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                // the launch clamps stay on the pad
                if (p.ShieldedFromAirstream || (p.rb == null && !PhysicsGlobals.ApplyDragToNonPhysicsParts) || p.IsLaunchClamp() ||
                    (!current && Dropped(p, stage)))
                    continue;

                AddLiftingSurfaceDrag(p, forward);

                DragCubeList cubes = p.DragCubes;
                bool cube = p.dragModel == Part.DragModel.DEFAULT || (p.dragModel == Part.DragModel.CUBE && !cubes.None);

                if (cube)
                {
                    if (cubes.None)
                        continue;

                    // the airflow's direction in the part's frame, as DragCubeList.SetDrag turns it
                    Vector3 local = p.partTransform.InverseTransformDirection(forward);
                    if (cubes.RotateDragVector)
                        local = cubes.DragVectorRotation * local;

                    for (int m = 0; m < _machs.Length; m++)
                    {
                        cubes.AddSurfaceDragDirection(local, (float)_machs[m], ref data);
                        table[m] += data.areaDrag * PhysicsGlobals.DragCubeMultiplier;
                    }

                    if (current)
                        _frontalArea += data.crossSectionalArea;
                    continue;
                }

                double value = NoCubesDragValue(p, forward);
                for (int m = 0; m < _machs.Length; m++)
                    table[m] += value;
            }

            for (int m = 0; m < _machs.Length; m++)
                table[m] = table[m] * PhysicsGlobals.DragMultiplier + _surfaceCdA[m];
        }

        private readonly double[] _surfaceCdA = new double[_machs.Length];

        // The drag wings, fins and control surfaces add on top of their drag cubes (ModuleLiftingSurface.GetDragVector, with the
        // surface at rest): dragCurve at the airflow's angle to the lift axis × dragMachCurve × deflectionLiftCoeff × the lift
        // drag multiplier. That force is in kN for a dynamic pressure in Pa, so × 1000 for the table's kPa.
        private void AddLiftingSurfaceDrag(Part p, Vector3 forward)
        {
            for (int i = 0; i < p.Modules.Count; i++)
            {
                if (!(p.Modules[i] is ModuleLiftingSurface ls) || !ls.isEnabled || !ls.useInternalDragModel)
                    continue;
                if (ls.nodeEnabled && p.FindAttachNode(ls.attachNodeName)?.attachedPart != null)
                    continue;

                Transform t = string.IsNullOrEmpty(ls.transformName) ? null : p.FindModelTransform(ls.transformName);
                if (t == null)
                    t = ls.transform;
                Vector3 liftVector = ls.transformDir == ModuleLiftingSurface.TransformDir.Z ? t.forward :
                    ls.transformDir == ModuleLiftingSurface.TransformDir.Y ? t.up : t.right;
                float liftDot = Vector3.Dot(forward, liftVector * ls.transformSign);
                float absDot = ls.omnidirectional ? Mathf.Abs(liftDot) : Mathf.Clamp01(liftDot);

                double drag = ls.dragCurve.Evaluate(absDot) * ls.deflectionLiftCoeff * PhysicsGlobals.LiftDragMultiplier * 1000;
                for (int m = 0; m < _machs.Length; m++)
                    _surfaceCdA[m] += drag * ls.dragMachCurve.Evaluate((float)_machs[m]);
            }
        }

        // FlightIntegrator.CalculateDragValue for a part without drag cubes; forward is the direction of flight
        private static double NoCubesDragValue(Part p, Vector3 forward)
        {
            switch (p.dragModel)
            {
                case Part.DragModel.CUBE:
                case Part.DragModel.SPHERICAL:
                    return p.maximum_drag;
                case Part.DragModel.CYLINDRICAL:
                    return Mathf.Lerp(p.minimum_drag, p.maximum_drag,
                        Mathf.Abs(Vector3.Dot(p.partTransform.TransformDirection(p.dragReferenceVector), forward)));
                case Part.DragModel.CONIC:
                    return Mathf.Lerp(p.minimum_drag, p.maximum_drag,
                        Vector3.Angle(p.partTransform.TransformDirection(p.dragReferenceVector), forward) / 180f);
                default:
                    return 0;
            }
        }

        // FAR's forces on the vessel flying nose first at each Mach, at a tenth of the atmosphere's depth, where an ascent goes
        // through Mach 1 (FAR's drag depends a little on the Reynolds number, so on the altitude). Its force is in kN.
        private bool ReadFAR(Vessel vessel, Vector3 forward)
        {
            CelestialBody body = vessel.mainBody;
            double altitude = 0.1 * body.atmosphereDepth;
            double pressure = body.GetPressure(altitude);
            double density = body.GetDensity(pressure, body.GetTemperature(altitude));
            double speedOfSound = body.GetSpeedOfSound(pressure, density);
            if (!(density > 0) || !(speedOfSound > 0))
                return false;

            for (int m = 0; m < _machs.Length; m++)
            {
                double speed = _machs[m] * speedOfSound;
                VesselState.FARCalculateVesselAeroForces.Call(vessel, out Vector3 force, out _, forward * (float)speed, altitude);
                double drag = -Vector3.Dot(force, forward) * 1000;
                _cdA[m] = Max(drag / (0.5 * density * speed * speed), 0);
            }

            _frontalArea = VesselState.FARVesselRefArea.Call(vessel);

            // FAR gives no force until it has built the vessel's shape, a moment after it loads: read again next time
            for (int m = 0; m < _machs.Length; m++)
                if (_cdA[m] > 0)
                    return true;
            return false;
        }

        private void AverageDefault()
        {
            double sum = 0;
            int n = 0;
            for (double mach = 0.5; mach <= 2.0001; mach += 0.05, n++)
                sum += CdAAt(mach);
            CdA = sum / n;
            MachAtMaxQ = double.NaN;
        }

        // ∫ q/m × Cd·A(Mach) dt / ∫ q/m dt along the plan while in the atmosphere, with the body's own atmosphere curves and the
        // air turning with the body. False if the plan has no time left in the atmosphere.
        private bool AverageAlongPlan(Solution plan, Vessel vessel, double met)
        {
            CelestialBody body = vessel.mainBody;
            double t0 = Max(plan.T0, met);
            double tf = plan.Tf;
            if (!(tf > t0))
                return false;

            const int STEPS = 400;
            double dt = (tf - t0) / STEPS;
            V3 w = 2 * PI / body.rotationPeriod * V3.northpole;
            double sum = 0, weights = 0, maxQ = 0, machAtMaxQ = double.NaN;

            for (int i = 0; i <= STEPS; i++)
            {
                double t = t0 + i * dt;
                V3 r = plan.R(t);
                double altitude = r.magnitude - body.Radius;
                if (altitude >= body.atmosphereDepth)
                    continue;

                double speed = (plan.V(t) - V3.Cross(w, r)).magnitude;
                double pressure = body.GetPressure(Max(altitude, 0));
                double density = body.GetDensity(pressure, body.GetTemperature(Max(altitude, 0)));
                double speedOfSound = body.GetSpeedOfSound(pressure, density);
                if (!(density > 0) || !(speedOfSound > 0))
                    continue;

                double q = 0.5 * density * speed * speed;
                double mach = speed / speedOfSound;
                double cdA = CdAAt(TableFor(vessel, plan.KSPStageAt(t)), mach);
                if (Source == Model.STOCK)
                    cdA *= PhysicsGlobals.DragCurvePseudoReynolds.Evaluate((float)(density * speed));

                double weight = q / plan.M(t);
                sum += weight * cdA;
                weights += weight;

                if (q > maxQ)
                {
                    maxQ = q;
                    machAtMaxQ = mach;
                }
            }

            if (!(weights > 0))
                return false;

            CdA = sum / weights;
            MachAtMaxQ = machAtMaxQ;
            return true;
        }
    }
}
