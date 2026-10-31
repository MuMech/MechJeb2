using System;
using KodeKit;
using KodeUI;

namespace MuMech.UI
{
    /// <summary>
    /// The node executor's controls, shared by the Node Editor, Maneuver Planner, Rendezvous Planner and the guidance
    /// windows: the Execute next / Execute all / Abort buttons and the Auto-warp toggle.
    /// </summary>
    internal static class NodeUi
    {
        /// <summary>
        /// "Execute next node" while the executor is idle and <paramref name="canOne"/> holds, "Execute all nodes" while
        /// idle and <paramref name="canAll"/> holds, "Abort node execution" while it runs. Primary, normal and danger
        /// buttons in the modern layout.
        /// </summary>
        public static void ExecuteButtons(KodeWindow w, UIObject parent, MechJebCore core, object controller, string oneKey, string allKey,
            string abortKey, Func<bool> canOne, Func<bool> canAll, bool flexible)
        {
            bool Idle() => core.Node != null && !core.Node.Enabled;
            UIButton one = KodeWindow.Button(parent, oneKey, () => core.Node.ExecuteOneNode(controller), ButtonRole.Primary, flexible);
            w.Visible(one.gameObject, () => Idle() && canOne());
            UIButton all = KodeWindow.Button(parent, allKey, () => core.Node.ExecuteAllNodes(controller), ButtonRole.Normal, flexible);
            w.Visible(all.gameObject, () => Idle() && canAll());
            UIButton abort = KodeWindow.Button(parent, abortKey, () => core.Node.Abort(), ButtonRole.Danger, flexible);
            w.Visible(abort.gameObject, () => core.Node != null && core.Node.Enabled);
        }

        public static bool Autowarp(MechJebCore core) => core.Node != null && core.Node.Autowarp;

        public static void Autowarp(MechJebCore core, bool v)
        {
            if (core.Node != null) core.Node.Autowarp = v;
        }

        /// <summary>The executor's "Auto-warp" toggle (a switch in the modern layout); <paramref name="key"/> is the window's own label.</summary>
        public static UnityEngine.UI.Toggle AutowarpToggle(KodeWindow w, UIObject parent, MechJebCore core, string key) =>
            w.Toggle(parent, key, () => Autowarp(core), v => Autowarp(core, v));
    }
}
