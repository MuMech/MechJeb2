using System;

namespace MechJebLib.Utils
{
    public class MechJebLibException : Exception
    {
        public MechJebLibException() { }
        public MechJebLibException(string message) : base(message) { }
        public MechJebLibException(string message, Exception inner) : base(message, inner) { }
    }
}
