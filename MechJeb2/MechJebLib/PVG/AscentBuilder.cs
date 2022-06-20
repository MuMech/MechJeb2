using MechJebLib.Primitives;
using UnityEngine;

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
            
            public AscentBuilder AddStageUsingBurnTime(double m0, double thrust, double isp, double bt, bool optimizeTime = false)
            {
               //Debug.Log($"AddStageUsingBurnTime: {m0} {thrust} {isp} {bt}");
                _ascent._phases.Add(Phase.NewStageUsingBurnTime(m0, thrust, isp, bt, optimizeTime));
                return this;
            }
            
            public AscentBuilder Initial(V3 r0, V3 v0, double t0, double mu)
            {
               //Debug.Log($"Initial: {r0} {v0} {t0} {mu}");
                _ascent._r0  = r0;
                _ascent._v0  = v0;
                _ascent._t0  = t0;
                _ascent._mu  = mu;
                return this;
            }

            public Ascent Build()
            {
                return _ascent;
            }

            public AscentBuilder SetTarget(double peR, double apR, double attR, double inclination, double lan, bool attachAltFlag, bool lanflag, double coastLen)
            {
                //Debug.Log($"SetTarget: {peR} {apR} {attR} {inclination} {lan} {attachAltFlag} {lanflag} {coastLen}");
                _ascent._peR           = peR;
                _ascent._apR           = apR;
                _ascent._attR          = attR;
                _ascent._inclination   = inclination;
                _ascent._lan           = lan;
                _ascent._attachAltFlag = attachAltFlag;
                _ascent._lanflag       = lanflag;
                _ascent._coastLen      = coastLen;

                return this;
            }

            public void OldSolution(Solution solution)
            {
                _ascent._solution = solution;
            }
        }
    }
}
