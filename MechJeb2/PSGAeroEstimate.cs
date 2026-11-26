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
    ///     A table of Cd·A against Mach for the vessel flying nose first, as PSG's model has no lift: stock aerodynamics
    ///     from the parts' drag cubes and drag models as FlightIntegrator applies them, FAR from FARAPI.CalculateVesselAeroForces.
    ///     PSG holds one constant, so the table is averaged along the ascent PSG planned, each point weighted by the dynamic pressure
    ///     over the mass (its share of the drag losses); with no plan yet, over Mach 0.5 to 2, where an ascent meets its maximum
    ///     dynamic pressure. In stock aerodynamics each stage of the plan has its own table, without the parts dropped by then.
    ///     <para />
    ///     The estimate is worked out while the vessel is on the ground, along the plan from the pad, and stays once it is airborne:
    ///     a vessel first seen in flight gets one estimate, along the plan from where it is.
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

        // FAR can't leave out a launch tower: until it has dropped it, under a second after the release, the rocket's widest part at this Cd
        private const double PAD_CD = 0.5;
        private const double FAR_READ_MET = 5;

        // the vessel as it is
        private readonly double[] _cdA = new double[_machs.Length];

        // stock aerodynamics: the vessel in a later stage of the plan, without the parts dropped by then, read on first use
        private readonly Dictionary<int, double[]> _stageTables = new Dictionary<int, double[]>();

        private int _currentStage;
        private int _releaseStage; // of the launch clamps: what is dropped by then stays on the pad
        private bool _padEstimate;

        private double _frontalArea;

        /// <summary>Which aerodynamics the table holds: NONE until the vessel has been read.</summary>
        public Model Source { get; private set; } = Model.NONE;

        /// <summary>The drag area PSG plans with, m².</summary>
        public double CdA { get; private set; }

        public double Cd { get; private set; }

        public double Aref { get; private set; }

        private Vessel? _vessel;
        private CelestialBody? _body;
        private int _partCount = -1;

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
        ///     On the ground, reads the vessel again if it changed and averages the table along <paramref name="plan" /> (a solution
        ///     in SI units). Airborne, keeps the estimate it has. False while nothing can be worked out: no vessel, a body without
        ///     atmosphere, a vessel still packed.
        /// </summary>
        public bool Update(Vessel? vessel, Solution? plan)
        {
            if (vessel == null || vessel.mainBody == null || !vessel.mainBody.atmosphere || vessel.packed)
                return false;

            bool onGround = vessel.situation == Vessel.Situations.PRELAUNCH || vessel.LandedOrSplashed;
            if (!onGround && vessel == _vessel && Source != Model.NONE && !_padEstimate)
                return CdA > 0;

            // FAR's read is slow (tens of milliseconds), so only a changed vessel is read again
            if (vessel != _vessel || vessel.mainBody != _body || vessel.parts.Count != _partCount ||
                vessel.currentStage != _currentStage || Source == Model.NONE || (_padEstimate && vessel.missionTime >= FAR_READ_MET))
            {
                _currentStage = vessel.currentStage;
                _stageTables.Clear();
                if (!ReadTable(vessel))
                    return false;
                _vessel = vessel;
                _body = vessel.mainBody;
                _partCount = vessel.parts.Count;
            }

            if (plan == null || !AverageAlongPlan(plan, vessel))
                AverageDefault();

            Aref = _frontalArea > 0 ? _frontalArea : CdA;
            Cd = Aref > 0 ? CdA / Aref : 0;
            return CdA > 0;
        }

        private bool ReadTable(Vessel vessel)
        {
            _releaseStage = _currentStage;
            foreach (Part p in vessel.parts)
                if (p.IsLaunchClamp())
                    _releaseStage = Min(_releaseStage, p.inverseStage);

            if (VesselState.IsFARCorrectlyInitialized)
            {
                bool pad = vessel.parts.Exists(p => !p.IsLaunchClamp() && Dropped(p, _releaseStage)) ||
                    (_padEstimate && vessel == _vessel && vessel.missionTime < FAR_READ_MET);
                if (pad ? !ReadPadEstimate(vessel) : !ReadFAR(vessel, vessel.GetTransform().up))
                    return false;
                _padEstimate = pad;
                Source = Model.FAR;
                return true;
            }

            ReadStock(vessel, vessel.GetTransform().up, _currentStage, _cdA);
            Source = Model.STOCK;
            return true;
        }

        private bool ReadPadEstimate(Vessel vessel)
        {
            Vector3 up = vessel.GetTransform().up;
            _frontalArea = 0;
            foreach (Part p in vessel.parts)
                if (!p.DragCubes.None && !p.IsLaunchClamp() && !Dropped(p, _releaseStage))
                    _frontalArea = Max(_frontalArea,
                        p.DragCubes.GetCubeAreaDir(p.partTransform.InverseTransformDirection(up), p.DragCubes.WeightedArea));

            for (int m = 0; m < _machs.Length; m++)
                _cdA[m] = PAD_CD * _frontalArea;
            return _frontalArea > 0;
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
                ReadStock(vessel, vessel.GetTransform().up, stage, table);
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
            if (current)
                _frontalArea = 0;

            var data = new DragCubeList.CubeData();
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                // the launch clamps stay on the pad
                if (p.ShieldedFromAirstream || (p.rb == null && !PhysicsGlobals.ApplyDragToNonPhysicsParts) || p.IsLaunchClamp() ||
                    Dropped(p, Min(stage, _releaseStage)))
                    continue;

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
                table[m] *= PhysicsGlobals.DragMultiplier;
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
        }

        // ∫ q/m × Cd·A(Mach) dt / ∫ q/m dt along the plan while in the atmosphere, with the body's own atmosphere curves and the
        // air turning with the body. False if the plan has no time left in the atmosphere.
        private bool AverageAlongPlan(Solution plan, Vessel vessel)
        {
            CelestialBody body = vessel.mainBody;
            double t0 = plan.T0;
            double tf = plan.Tf;
            if (!(tf > t0))
                return false;

            const int STEPS = 400;
            double dt = (tf - t0) / STEPS;
            V3 w = 2 * PI / body.rotationPeriod * V3.northpole;
            double sum = 0, weights = 0;

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
            }

            if (!(weights > 0))
                return false;

            CdA = sum / weights;
            return true;
        }
    }
}
