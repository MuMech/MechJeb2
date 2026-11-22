using UnityEngine;

namespace KodeKit
{
    /// <summary>
    /// The name a button was built with (<see cref="KodeWindow.NameButton"/>): its window's build scope and its text's '#key'. The agent
    /// bridge makes the button's id from it (<see cref="Headless.ControlMatch.ButtonIds"/>). Set once at build, no per-frame work.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ButtonName : MonoBehaviour
    {
        public string Name;
    }
}
