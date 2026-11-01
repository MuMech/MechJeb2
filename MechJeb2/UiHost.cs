namespace MuMech
{
    /// <summary>
    /// The UI in another assembly (MechJeb2.UI) that draws MechJeb's windows. IMGUI only draws another mod's windows:
    /// MechJebCore.OnGUI skips the rest, and its click-through lock also covers the host's windows, so there is a single
    /// lock owner for both.
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

        // MechJeb's own windows, with or without a host: their IMGUI drawing code is gone
        internal static bool Owns(DisplayModule module) =>
            module.GetType().Assembly == typeof(DisplayModule).Assembly || Current != null && Current.OwnsWindow(module);

        internal static bool MouseOverHostWindow() => Current != null && Current.MouseOverWindow();
    }
}
