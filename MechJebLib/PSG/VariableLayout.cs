namespace MechJebLib.PSG
{
    public readonly struct VariableLayout
    {
        private readonly int _k; // number of knot points
        private readonly int _phase;

        private int _start => (10 * _k + 2) * _phase;

        public VariableLayout(int k, int phase)
        {
            _phase = phase;
            _k     = k;
        }

        public (int, int, int) RStart       => (_start, _start + _k, _start + 2 * _k);
        public (int, int, int) R(int index) => (_start + index, _start + _k + index, _start + 2 * _k + index);
        public (int, int, int) REnd         => (_start + _k - 1, _start + 2 * _k - 1, _start + 3 * _k - 1);

        public (int, int, int) VStart       => (_start + 3 * _k, _start + 4 * _k, _start + 5 * _k);
        public (int, int, int) V(int index) => (_start + 3 * _k + index, _start + 4 * _k + index, _start + 5 * _k + index);
        public (int, int, int) VEnd         => (_start + 4 * _k - 1, _start + 5 * _k - 1, _start + 6 * _k - 1);

        public int MStart       => _start + 6 * _k;
        public int M(int index) => _start + 6 * _k + index;
        public int MEnd         => _start + 7 * _k - 1;

        public (int, int, int) UStart       => (_start + 7 * _k, _start + 8 * _k, _start + 9 * _k);
        public (int, int, int) U(int index) => (_start + 7 * _k + index, _start + 8 * _k + index, _start + 9 * _k + index);
        public (int, int, int) UEnd         => (_start + 8 * _k - 1, _start + 9 * _k - 1, _start + 10 * _k - 1);

        public int Ti => _start + 10 * _k;
        public int Tf => _start + 10 * _k + 1;

        public static int NumVariablesForPhases(int k, int numPhases) => (10 * k + 2) * numPhases;

        // - 10 initial conditions
        // - 10 continuity conditions * (numPhases - 1)
        // - 1 mass constraint * numPhases
        // - 1 burn time constraint * numPhases
        // - 1 complementarity constraint * numPhases
        // - K control constraints * numPhases
        // - 3*K unguided control constraints * numPhases
        // - N-1 dynamical constraints * numPhases * 7 variables * 2 constraints
        // - plus terminal constraints
        public static int NumConstraintsForPhases(int n, int k, int numPhases, Problem problem) => (13 + 4 * k) * numPhases + (n - 1) * numPhases * 7 * 2 + problem.Terminal.NumConstraints;
    }
}
