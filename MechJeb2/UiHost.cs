namespace MuMech
{
    /// <summary>
    /// The UI in another assembly (MechJeb2.UI) that draws MechJeb's windows. MechJebCore's click-through lock also covers
    /// the host's windows, so there is a single lock owner.
    /// </summary>
    public interface IMechJebUiHost
    {
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

        internal static bool MouseOverHostWindow() => Current != null && Current.MouseOverWindow();
    }
}
