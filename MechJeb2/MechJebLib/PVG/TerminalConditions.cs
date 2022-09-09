using MechJebLib.Primitives;
using UnityEngine;

namespace MechJebLib.PVG
{
    public class TerminalConditions
    {
        private readonly double _hT;
        private readonly bool   _positiveDirection;

        public TerminalConditions(double hT, bool positiveDirection)
        {
            this._hT                = hT;
            this._positiveDirection = positiveDirection;
        }
        
        public bool Check(V3 r, V3 v)
        {
            var hf = V3.Cross(r, v);
            Debug.Log("hf = " + hf + "hT = " + _hT);
            return _positiveDirection ? hf.magnitude > _hT : hf.magnitude < _hT;
        }
    }
}
