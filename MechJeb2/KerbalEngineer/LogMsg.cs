using System.Text;

using UnityEngine;

namespace KerbalEngineer
{
    public class LogMsg
    {
        public StringBuilder buf;

        public LogMsg()
        {
            this.buf = new StringBuilder();
        }

        public void Flush()
        {
            MonoBehaviour.print(this.buf);
            this.buf.Length = 0;
        }
    }
}
