/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Control
{
    // This is where all the number-crunching occurs to output a set of throttles
    // given a set of thrusters and the desired translation and rotation vectors.
    public class RCSSolver
    {
        // One RCS module, which the game throttles as a whole.
        public class Thruster
        {
            // ModuleRCS gives no thrust from a nozzle at or under this, its EPSILON.
            // It also means a nozzle at 90 degrees to the direction doesn't count
            // as a thruster that can push, whatever the rounding says.
            private const double MIN_THROTTLE = 0.05;

            private readonly V3[] _positions;
            private readonly V3[] _thrustDirections;
            private readonly V3   _axes;

            // Thrust of one nozzle at full throttle, in kN.
            public double Power { get; }

            // Positions and directions have one entry per nozzle, in the vessel's
            // frame: the position is from the center of mass, the direction is a
            // unit vector. Axes is 1 for each axis of the translation input the
            // module responds to and 0 for the others.
            public Thruster(V3[] positions, V3[] thrustDirections, double power, V3 axes)
            {
                _positions = positions;
                _thrustDirections = thrustDirections;
                Power = power;
                _axes = axes;
            }

            public V3 GetThrust(V3 direction, V3 rotation)
            {
                V3 force = V3.zero;
                for (int i = 0; i < _thrustDirections.Length; i++)
                {
                    force += NozzleThrust(i, direction, rotation);
                }

                return force;
            }

            public V3 GetTorque(V3 direction, V3 rotation)
            {
                V3 torque = V3.zero;
                for (int i = 0; i < _thrustDirections.Length; i++)
                {
                    torque -= V3.Cross(_positions[i], NozzleThrust(i, direction, rotation));
                }

                return torque;
            }

            private V3 NozzleThrust(int nozzle, V3 direction, V3 rotation)
            {
                V3 thrustDir = _thrustDirections[nozzle];

                // The game appears to throttle a thruster based on the dot
                // product of its thrust vector (normalized) and the
                // direction vector (not normalized!).
                V3 torque = -V3.Cross(_positions[nozzle], thrustDir);
                double translateThrottle = V3.Dot(V3.Scale(direction, _axes), thrustDir);
                double rotateThrottle = V3.Dot(rotation, torque);
                double throttle = Clamp01(translateThrottle + rotateThrottle);

                return throttle > MIN_THROTTLE ? thrustDir * (throttle * Power) : V3.zero;
            }
        }

        private double[,] _a = new double[0, 0];
        private double[]  _b = Array.Empty<double>();

        private enum Params { TORQUE_X, TORQUE_Y, TORQUE_Z, TRANS_X, TRANS_Y, TRANS_Z, WASTE, FUDGE }

        private readonly int _paramLength = Enum.GetValues(typeof(Params)).Length;

        private void cost_func(double[] x, ref double func, object? obj)
        {
            func = 0;
            for (int attr = 0; attr < _b.Length; attr++)
            {
                double tmp = 0;
                for (int row = 0; row < x.Length; row++)
                {
                    tmp += x[row] * _a[attr, row];
                }

                tmp -= _b[attr];
                func += tmp * tmp;
            }
        }

        public double[] Run(IReadOnlyList<Thruster> fullThrusters, V3 direction, V3 rotation, RCSSolverTuningParams tuning)
        {
            direction = direction.normalized;

            int fullCount = fullThrusters.Count;
            var thrusters = new List<Thruster>();
            var thrustForces = new V3[fullCount];

            // Initialize the matrix based on thruster directions.
            for (int i = 0; i < fullCount; i++)
            {
                Thruster thruster = fullThrusters[i];
                thrustForces[i] = thruster.GetThrust(direction, rotation);
                if (thrustForces[i].magnitude > 0)
                {
                    thrusters.Add(thruster);
                }
            }

            int count = thrusters.Count;

            // Nothing to balance. See the comment at the end about full power.
            if (count == 0) return Enumerable.Repeat(1.0, fullCount).ToArray();

            // Every thrust below is relative to the strongest thruster, so that
            // the tuning factors mean the same thing on any vessel. Thrusters of
            // equal power then count as one unit each, whatever their real power.
            double maxPower = thrusters.Max(t => t.Power);

            // We want to minimize torque (3 values), translation error (3 values),
            // and any thrust that's wasted due to not being toward 'direction' (1
            // value). We also have a value to make sure there's always -some-
            // thrust.
            _a = new double[_paramLength, count];
            _b = new double[_paramLength];

            for (int i = 0; i < _b.Length; i++)
            {
                _b[i] = 0;
            }

            _b[_b.Length - 1] = 0.001 * count;

            double[] x = new double[count];
            double[] bndl = new double[count];
            double[] bndu = new double[count];

            // Initialize the matrix based on thruster directions.
            int tIdx = -1;
            for (int i = 0; i < fullCount; i++)
            {
                V3 thrust = thrustForces[i];
                if (thrust.magnitude == 0)
                {
                    continue;
                }

                Thruster thruster = thrusters[++tIdx];

                double relativePower = thruster.Power / maxPower;
                V3 torque = thruster.GetTorque(direction, rotation) / maxPower;
                V3 thrustNorm = thrust.normalized;

                V3 torqueErr = torque - rotation;
                V3 transErr = (thrustNorm - direction) * relativePower;

                // Waste is a value from [0..2] indicating how much thrust is being
                // wasted due to not being toward 'direction':
                //     0: perfectly aligned with direction
                //     1: perpendicular to direction
                //     2: perfectly opposite direction
                double waste = 1 - V3.Dot(thrustNorm, direction);

                if (waste < tuning.WasteThreshold) waste = 0;

                _a[(int)Params.TORQUE_X, tIdx] = torqueErr.x * tuning.FactorTorque;
                _a[(int)Params.TORQUE_Y, tIdx] = torqueErr.y * tuning.FactorTorque;
                _a[(int)Params.TORQUE_Z, tIdx] = torqueErr.z * tuning.FactorTorque;
                _a[(int)Params.TRANS_X, tIdx] = transErr.x * tuning.FactorTranslate;
                _a[(int)Params.TRANS_Y, tIdx] = transErr.y * tuning.FactorTranslate;
                _a[(int)Params.TRANS_Z, tIdx] = transErr.z * tuning.FactorTranslate;
                _a[(int)Params.WASTE, tIdx] = waste * relativePower * tuning.FactorWaste;
                _a[(int)Params.FUDGE, tIdx] = 0.001;
                x[tIdx] = 1;
                bndl[tIdx] = 0;
                bndu[tIdx] = 1;
            }

            const double EPSG = 0.01;
            const double EPSF = 0;
            const double EPSX = 0;
            const double DIFFSTEP = 1.0e-6;
            const int MAXITS = 0;

            alglib.minbleiccreatef(x, DIFFSTEP, out alglib.minbleicstate state);
            alglib.minbleicsetbc(state, bndl, bndu);
            alglib.minbleicsetcond(state, EPSG, EPSF, EPSX, MAXITS);
            alglib.minbleicoptimize(state, cost_func, null, null);
            alglib.minbleicresults(state, out double[] throttles, out alglib.minbleicreport _);

            double m = throttles.Max();

            if (m > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    throttles[i] /= m;
                }
            }

            double[] fullThrottles = new double[fullCount];

            // A thruster that can't push along 'direction' stays at full power:
            // its throttle has no effect on the translation, and it scales any
            // thrust the game asks of it for attitude control.
            int j = 0;
            for (int i = 0; i < fullCount; i++)
            {
                fullThrottles[i] = thrustForces[i].magnitude == 0 ? 1 : throttles[j++];
            }

            return fullThrottles;
        }
    }

    // Immutable so that a task on the solver thread can safely keep using the
    // parameters it was submitted with after they've been replaced.
    // Note that default(RCSSolverTuningParams) is all zeros; use Default instead.
    public readonly struct RCSSolverTuningParams
    {
        public static readonly RCSSolverTuningParams Default = new RCSSolverTuningParams(0.25, 1, 0.005, 1);

        public readonly double WasteThreshold;
        public readonly double FactorTorque;
        public readonly double FactorTranslate;
        public readonly double FactorWaste;

        public RCSSolverTuningParams(double wasteThreshold, double factorTorque, double factorTranslate, double factorWaste)
        {
            WasteThreshold = wasteThreshold;
            FactorTorque = factorTorque;
            FactorTranslate = factorTranslate;
            FactorWaste = factorWaste;
        }
    }
}
