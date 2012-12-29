using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRCSController : ComputerModule
    {
        public MechJebModuleRCSController(MechJebCore core) : base(core) { }

        public void SetTargetWorldVelocity(Vector3d vel)
        {
        }

        public void SetTargetLocalVelocity(Vector3d vel)
        {
        }
    }
}
