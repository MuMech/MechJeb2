using MechJebLib.Primitives;

namespace MechJebLib.PVG
{
    public partial class Ascent
    {
        public class AscentBuilder
        {
            private readonly Ascent       _ascent;

            public AscentBuilder(Ascent ascent)
            {
                _ascent           = ascent;
                _ascent._solution = null;
                _ascent._phases.Clear();
            }
            
            public AscentBuilder AddStageUsingBurnTime(double m0, double thrust, double isp, double bt, int kspStage, bool optimizeTime = false, bool unguided = false)
            {
                _ascent._phases.Add(Phase.NewStageUsingBurnTime(m0, thrust, isp, bt, kspStage, optimizeTime, unguided));
                return this;
            }
            
            public void AddCoast(double m0, double ct, int kspStage)
            {
                _ascent._phases.Add(Phase.NewCoast(m0, ct, kspStage));
            }
            
            public AscentBuilder Initial(V3 r0, V3 v0, V3 u0, double t0, double mu)
            {
                _ascent._r0 = r0;
                _ascent._v0 = v0;
                _ascent._u0 = u0.normalized;
                _ascent._t0 = t0;
                _ascent._mu = mu;
                return this;
            }

            public Ascent Build()
            {
                return _ascent;
            }

            public AscentBuilder SetTarget(double peR, double apR, double attR, double inclination, double lan, bool attachAltFlag, bool lanflag)
            {
                //Debug.Log($"SetTarget: {peR} {apR} {attR} {inclination} {lan} {attachAltFlag} {lanflag} {coastLen}");
                _ascent._peR           = peR;
                _ascent._apR           = apR;
                _ascent._attR          = attR;
                _ascent._incT   = inclination;
                _ascent._lanT           = lan;
                _ascent._attachAltFlag = attachAltFlag;
                _ascent._lanflag       = lanflag;

                return this;
            }

            public void OldSolution(Solution solution)
            {
                _ascent._solution = solution;
            }

            public void FixedBurnTime(bool fixedBurnTime)
            {
                _ascent._fixedBurnTime = fixedBurnTime;
            }
        }
    }
}
