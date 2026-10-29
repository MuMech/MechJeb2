namespace MuMech
{
    /// <summary>
    /// A second UI implementation (e.g. a uGUI one in another assembly) that draws some DisplayModule windows itself.
    /// MechJebCore.OnGUI skips the windows a host owns, and its click-through lock also covers the host's windows,
    /// so there is a single lock owner for both UIs.
    /// </summary>
    public interface IMechJebUiHost
    {
        /// <summary>True when this host draws the module's window; IMGUI then does not draw it.</summary>
        bool OwnsWindow(DisplayModule module);

        /// <summary>True while the pointer is over a window (or popup) drawn by this host.</summary>
        bool MouseOverWindow();
    }

    public static class MechJebUiHost
    {
        public static IMechJebUiHost Current { get; private set; }

        public static void Register(IMechJebUiHost host) => Current = host;

        public static void Unregister(IMechJebUiHost host)
        {
            if (Current == host) Current = null;
        }

        internal static bool Owns(DisplayModule module) => Current != null && Current.OwnsWindow(module);

        internal static bool MouseOverHostWindow() => Current != null && Current.MouseOverWindow();
    }
}
