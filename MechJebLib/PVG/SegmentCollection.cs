using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static System.Math;

namespace MechJebLib.PVG
{
    public class SegmentCollection : List<Segment>, IDisposable
    {
        private readonly Problem _problem;

        public SegmentCollection(Problem problem, IEnumerable<Phase> phases)
        {
            _problem = problem;
            foreach (Phase phase in phases)
                Add(new Segment(problem, phase));
        }

        public new void Add(Segment item)
        {
            if (Count > 0)
                item.Offset = this[Count - 1].Offset + SegmentRecord.SEGMENT_REC_LEN;
            else
                item.Offset = 0;

            base.Add(item);
        }

        public int ObjectiveAndConstraintsLength() => SegmentRecord.SEGMENT_REC_LEN * Count + 1;

        public int EqualityConstraintLength() => SegmentRecord.SEGMENT_REC_LEN * Count;

        public int InequalityConstraintLength() => 0;

        public int VariableLength() => SegmentRecord.SEGMENT_REC_LEN * Count;

        public double CalculateEqualityConstraints(double[] zout)
        {
            int offset = 0;

            offset = this[0].CalculateInitialConstraints(zout, offset);

            for (int i = 1; i < Count; i++)
                offset = this[i].CalculateContinuityConstraints(zout, offset, this[i - 1]);

            offset = this[Count - 1].CalcluateTerminalConstraints(zout, offset);

            Check.Equal(offset, EqualityConstraintLength());

            return EqualityConstraintViolation(zout);
        }

        public double CalculateEqualityConstraints(double[] zout, double[,] jac)
        {
            int offset = 0;

            offset = this[0].CalculateInitialConstraints(zout, offset);

            for (int i = 1; i < Count; i++)
                offset = this[i].CalculateContinuityConstraints(zout, jac, offset, this[i - 1]);

            offset = this[Count - 1].CalcluateTerminalConstraints(zout, jac, offset);

            Check.Equal(offset, EqualityConstraintLength());

            return EqualityConstraintViolation(zout);
        }

        public double CalculateObjectiveAndConstraints(double[] zout)
        {
            zout[0] = 0;

            for (int i = 0; i < Count; i++)
                zout[0] += this[i].MassObjective();

            int offset = 1;

            offset = this[0].CalculateInitialConstraints(zout, offset);

            for (int i = 1; i < Count; i++)
                offset = this[i].CalculateContinuityConstraints(zout, offset, this[i - 1]);

            offset = this[Count - 1].CalcluateTerminalConstraints(zout, offset);

            Check.Equal(offset, ObjectiveAndConstraintsLength());

            return ConstraintViolationWithObjective(zout);
        }

        public double CalculateObjectiveAndConstraints(double[] zout, double[,] jac)
        {
            zout[0] = 0;

            for (int i = 0; i < Count; i++)
                zout[0] += this[i].MassObjective();

            int offset = 1;

            offset = this[0].CalculateInitialConstraints(zout, offset);

            for (int i = 1; i < Count; i++)
                offset = this[i].CalculateContinuityConstraints(zout, jac, offset, this[i - 1]);

            offset = this[Count - 1].CalcluateTerminalConstraints(zout, jac, offset);

            Check.Equal(offset, ObjectiveAndConstraintsLength());

            return ConstraintViolationWithObjective(zout);
        }

        public void UnpackVariables(double[] yin)
        {
            Guard.CanContain(yin, VariableLength());

            int offset = 0;

            for (int i = 0; i < Count; i++)
            {
                Segment segment = this[i];

                offset = segment.UnpackVariables(yin, offset, i == 0);
            }
        }

        public void UnpackBurntimesFromPhases()
        {
            for (int i = 0; i < Count; i++)
            {
                Segment segment = this[i];
                segment.UnpackBurntimesFromPhases();
            }
        }

        public void PackVariables(double[] yin)
        {
            Guard.CanContain(yin, VariableLength());

            int offset = 0;

            for (int i = 0; i < Count; i++)
            {
                Segment segment = this[i];

                offset = segment.PackVariables(yin, offset);
            }
        }

        public void BoundaryConditions(double[] bndl, double[] bndu)
        {
            Guard.CanContain(bndl, VariableLength());
            Guard.CanContain(bndu, VariableLength());

            int offset = 0;

            for (int i = 0; i < Count; i++)
            {
                Segment segment = this[i];

                offset = segment.BoundaryConditions(bndl, bndu, offset);
            }
        }

        public double EqualityConstraintViolation(double[] z)
        {
            double znorm = 0.0;

            for (int i = 0; i < EqualityConstraintLength(); i++)
                znorm += z[i] * z[i];

            return Sqrt(znorm);
        }

        public double ConstraintViolationWithObjective(double[] z)
        {
            double znorm = 0.0;

            for (int i = 0; i < EqualityConstraintLength(); i++)
                znorm += z[i + 1] * z[i + 1];

            return Sqrt(znorm);
        }

        public void MultipleShooting()
        {
            double t = 0;
            double dv = 0;
            V3 u0 = V3.zero;

            for (int i = 0; i < Count; i++)
            {
                Segment segment = this[i];
                (t, dv, u0) = segment.MultipleShooting(t, dv, u0);
            }
        }

        public void SingleShooting(IntegratorRecord x0)
        {
            double t = 0;
            V3 u0 = V3.zero;

            for (int i = 0; i < Count; i++)
            {
                Segment segment = this[i];
                (t, x0, u0) = segment.SingleShooting(t, x0, u0);
            }
        }

        public Solution GetSolution()
        {
            var solution = new Solution(_problem);

            for (int i = 0; i < Count; i++)
            {
                Segment segment = this[i];
                segment.GetSolution(solution);
            }

            return solution;
        }

        public bool NeedsObjectiveFunction()
        {
            for (int i = 0; i < Count; i++)
            {
                Segment segment = this[i];
                if (segment.NeedsObjectiveFunction())
                    return true;
            }

            return false;
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
