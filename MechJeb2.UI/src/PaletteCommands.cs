using System.Collections.Generic;
using KodeKit;
using KodeKit.Headless;
using KSP.Localization;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJeb's commands for KodeKit's command palette (<see cref="CommandPalette"/>): every window of the menu (from the shortcuts'
    /// action registry), and in flight the autopilots and the maneuver nodes. A command that changes the flight runs only on a second
    /// Enter (<see cref="PaletteCommand.Confirm"/>), the first showing what it will do. A command that can't run now is listed, greyed,
    /// with its reason; a command with no meaning now (Disengage while nothing is engaged) is not listed.
    /// <para>Career tech locks are followed as the action groups and the toolbar do: the ascent commands need Ascent Guidance, Smart
    /// A.S.S.'s need Smart A.S.S., Smart RCS's need Smart RCS (module and window), and Execute next node, Execute all nodes and
    /// Remove all nodes need the Maneuver Planner unlocked (the toolbar's Execute Node button). A locked command is listed, greyed,
    /// "not unlocked yet". Abort node execution is never locked: stopping an autopilot is always allowed.</para>
    /// <para>The palette opens from the menu's "Command palette" button, or from a keyboard shortcut bound to its action
    /// (<see cref="OpenId"/>, in Settings > Keyboard shortcuts); no key is bound by default.</para>
    /// </summary>
    internal static class PaletteCommands
    {
        /// <summary>The shortcut action that opens the palette.</summary>
        public const string OpenId = "palette:open";

        /// <summary>The core the commands act on (the active vessel's, or the editor ship's); null between scenes.</summary>
        static MechJebCore core;

        static bool registered;

        /// <summary>Once, at the host's start: the "open the palette" action in the shortcuts' registry.</summary>
        public static void Start()
        {
            if (registered) return;
            registered = true;
            Shortcuts.Actions.Add(new UiAction(OpenId, UiActionKind.Command, () => Localizer.Format("#MechJeb_NewUI_Palette"), Open, () => Shown));
        }

        /// <summary>
        /// Each frame, from the host: the core the commands act on. A palette open when the core changes (a vessel switch, the pod
        /// destroyed) closes, running nothing: its commands were made for the other core. The "open" action goes after the windows
        /// in the shortcuts' list (the windows are registered again whenever the menu's list changes).
        /// </summary>
        public static void Follow(MechJebCore c)
        {
            if (!ReferenceEquals(c, core) && CommandPalette.IsOpen) CommandPalette.Model.Close(PaletteClose.OwnerClosed);
            core = c;
            IReadOnlyList<UiAction> all = Shortcuts.Actions.All;
            if (all.Count < 2 || all[all.Count - 1].Id == OpenId) return;
            UiAction open = Shortcuts.Actions.Find(OpenId);
            if (open == null) return;
            Shortcuts.Actions.Remove(OpenId);
            Shortcuts.Actions.Add(open);
        }

        /// <summary>The host is gone: no core held across the scene change, and the palette closed.</summary>
        public static void Stop()
        {
            core = null;
            if (CommandPalette.IsOpen) CommandPalette.Close();
        }

        static bool Shown =>
            core != null && core.ShowGui && (HighLogic.LoadedSceneIsEditor || core.vessel == FlightGlobals.ActiveVessel && core.part.State != PartStates.DEAD);

        static bool InFlight => HighLogic.LoadedSceneIsFlight && core != null && core.vessel != null && core.vessel == FlightGlobals.ActiveVessel;

        /// <summary>Opens the palette on the commands of now.</summary>
        public static void Open()
        {
            if (core == null) return;
            texts.Clear();
            CommandPalette.Open(Commands());
        }

        /// <summary>
        /// Every command, in the order an empty query lists them: the flight commands first (the autopilots, then the nodes), which
        /// the windows would otherwise push past the first rows, then the windows. Outside flight only the windows are listed.
        /// </summary>
        public static List<PaletteCommand> Commands()
        {
            var list = new List<PaletteCommand>();
            Ascent(list);
            SmartAss(list);
            SmartRcs(list);
            Nodes(list);
            Windows(list);
            return list;
        }

        // the texts read once per opening (a row's tag is asked at every keystroke; a language switch shows at the next opening)
        static readonly Dictionary<string, string> texts = new Dictionary<string, string>();

        static string L(string key)
        {
            if (!texts.TryGetValue(key, out string s)) texts[key] = s = Localizer.Format(key);
            return s;
        }

        // ---- windows ----

        static DisplayModule Find(string id)
        {
            if (core == null) return null;
            List<DisplayModule> modules = core.GetDisplayModules(MechJebModuleMenu.DisplayOrder.instance);
            for (int i = 0; i < modules.Count; i++)
                if (WindowShortcuts.IdOf(modules[i]) == id) return modules[i];
            return null;
        }

        // the language-independent name a window is also found by: its module's type name without MechJebModule, or a custom window's name
        static string WindowKey(string id)
        {
            if (id.StartsWith("window:custom:")) return id.Substring("window:custom:".Length);
            string n = id.StartsWith("window:") ? id.Substring("window:".Length) : id;
            return n.StartsWith("MechJebModule") ? n.Substring("MechJebModule".Length) : n;
        }

        static void Windows(List<PaletteCommand> list)
        {
            IReadOnlyList<UiAction> actions = Shortcuts.Actions.All;
            for (int i = 0; i < actions.Count; i++)
            {
                UiAction a = actions[i];
                if (a.Kind != UiActionKind.ToggleWindow) continue;
                string id = a.Id;
                // the module found once per opening: the palette asks whether it is listed twice a second, and a search by id makes strings
                DisplayModule m = Find(id);
                if (m == null) continue;
                list.Add(new PaletteCommand(id, () => a.Label, WindowKey(id), s => a.Run())
                    .ListedWhen(() => Shown && WindowShortcuts.InMenu(m))
                    .Tagged(() => L(m.Enabled ? "#MechJeb_NewUI_PaletteTagClose" : "#MechJeb_NewUI_PaletteTagOpen")));
            }
        }

        // ---- autopilots ----

        static string AutopilotTag() => L("#MechJeb_NewUI_PaletteTagAutopilot");

        static string TechLocked() => L("#MechJeb_NewUI_PaletteTechLocked");

        static void Ascent(List<PaletteCommand> list)
        {
            MechJebModuleAscentMenu menu = core?.GetComputerModule<MechJebModuleAscentMenu>();
            MechJebModuleAscentMenu Menu() => menu;
            bool Engaged() => core != null && core.Ascent != null && core.Ascent.Enabled;
            bool Usable() => InFlight && Menu() != null;

            list.Add(new PaletteCommand("ascent:engage", () => L("#MechJeb_NewUI_PaletteAscentEngage"), "Engage ascent autopilot", s => Menu()?.Engage())
                .ListedWhen(() => Usable() && !Engaged())
                .When(() => !Menu().TechLocked && Menu().LaunchChoiceReady, () => Menu().TechLocked ? TechLocked() : L("#MechJeb_NewUI_PaletteNeedTarget"))
                .Confirm(() => EngagePreview(Menu()))
                .Tagged(AutopilotTag));
            list.Add(new PaletteCommand("ascent:disengage", () => L("#MechJeb_NewUI_PaletteAscentDisengage"), "Disengage ascent autopilot",
                    s => core?.Ascent?.Users.Clear())
                .ListedWhen(() => Usable() && Engaged())
                .Confirm(() => L("#MechJeb_NewUI_PaletteAscentDisengagePreview"))
                .Tagged(AutopilotTag));
        }

        // what Engage will do: at once, or a timed launch's countdown first (the launch choice of Ascent Guidance, while landed)
        static string EngagePreview(MechJebModuleAscentMenu menu)
        {
            string altitude = (core.AscentSettings.DesiredOrbitAltitude.Val / 1000).ToString("0.#");
            string inclination = core.AscentSettings.DesiredInclination.Val.ToString("0.##");
            string choice = null;
            switch (menu.EffectiveLaunchChoice)
            {
                case MechJebModuleAscentMenu.LaunchKind.PLANE_OF_TARGET:
                    choice = "#MechJeb_NewUI_LaunchPlaneOfTarget";
                    break;
                case MechJebModuleAscentMenu.LaunchKind.TARGET_LAN:
                    choice = "#MechJeb_NewUI_LaunchTargetLan";
                    break;
                case MechJebModuleAscentMenu.LaunchKind.LAN:
                    choice = "#MechJeb_NewUI_LaunchLan";
                    break;
            }
            return choice == null
                ? Localizer.Format("#MechJeb_NewUI_PaletteAscentEngagePreview", altitude, inclination)
                : Localizer.Format("#MechJeb_NewUI_PaletteAscentEngageTimed", altitude, inclination, Localizer.Format(choice));
        }

        // Smart A.S.S.'s targets offered, with their English names (the ones of its kOS names, #MechJeb_SmartASS_button34 on)
        static readonly MechJebModuleSmartASS.Target[] assTargets =
        {
            MechJebModuleSmartASS.Target.OFF, MechJebModuleSmartASS.Target.KILLROT, MechJebModuleSmartASS.Target.NODE,
            MechJebModuleSmartASS.Target.PROGRADE, MechJebModuleSmartASS.Target.RETROGRADE, MechJebModuleSmartASS.Target.NORMAL_PLUS,
            MechJebModuleSmartASS.Target.NORMAL_MINUS, MechJebModuleSmartASS.Target.RADIAL_PLUS, MechJebModuleSmartASS.Target.RADIAL_MINUS,
            MechJebModuleSmartASS.Target.SURFACE_PROGRADE, MechJebModuleSmartASS.Target.SURFACE_RETROGRADE, MechJebModuleSmartASS.Target.VERTICAL_PLUS
        };

        static readonly string[] assEnglish =
        {
            "Off", "Kill Rotation", "Node", "Prograde", "Retrograde", "Normal+", "Normal-", "Radial+", "Radial-", "Surface Velocity+", "Surface Velocity-", "Up"
        };

        static string TargetName(MechJebModuleSmartASS.Target t) => L("#MechJeb_SmartASS_button" + (34 + (int)t));

        static void SmartAss(List<PaletteCommand> list)
        {
            MechJebModuleSmartASS Ass() => core?.SmartASS;
            for (int i = 0; i < assTargets.Length; i++)
            {
                MechJebModuleSmartASS.Target t = assTargets[i];
                bool off = t == MechJebModuleSmartASS.Target.OFF;
                list.Add(new PaletteCommand("smartass:" + t.ToString().ToLowerInvariant(), () => Localizer.Format("#MechJeb_NewUI_PaletteSmartASS", TargetName(t)),
                        "Smart ASS " + assEnglish[i], s =>
                        {
                            MechJebModuleSmartASS ss = Ass();
                            if (ss == null) return;
                            ss.mode = MechJebModuleSmartASS.Target2Mode[(int)t];
                            ss.target = t;
                            ss.Engage();
                        })
                    .ListedWhen(() => InFlight && Ass() != null)
                    // as the Smart A.S.S. action groups: nothing while the module is tech-locked
                    .When(() => !Ass().TechLocked && !Ass().OtherUsersControlAttitude && (t != MechJebModuleSmartASS.Target.NODE || HasNode()),
                        () => Ass().TechLocked ? TechLocked() : Ass().OtherUsersControlAttitude ? L("#MechJeb_NewUI_PaletteOtherSteers") : L("#MechJeb_NewUI_PaletteNoNode"))
                    .Confirm(() => off ? L("#MechJeb_NewUI_PaletteSmartASSOffPreview") : Localizer.Format("#MechJeb_NewUI_PaletteSmartASSPreview", TargetName(t)))
                    .Tagged(() => Ass() != null && Ass().target == t ? L("#MechJeb_NewUI_PaletteTagActive") : AutopilotTag()));
            }
        }

        static void SmartRcs(List<PaletteCommand> list)
        {
            MechJebModuleSmartRcs rcs = core?.GetComputerModule<MechJebModuleSmartRcs>();
            MechJebModuleSmartRcsWindow window = core?.GetComputerModule<MechJebModuleSmartRcsWindow>();
            MechJebModuleSmartRcs Rcs() => rcs;
            bool Locked() => Rcs().TechLocked || window != null && window.TechLocked;
            bool HasTarget() => core != null && core.Target != null && core.Target.NormalTargetExists;
            void Set(MechJebModuleSmartRcs.Target t)
            {
                MechJebModuleSmartRcs r = Rcs();
                if (r == null) return;
                r.target = t;
                r.Engage();
            }

            // as the window: while another module uses the RCS controller, Smart RCS shows only AUTO
            bool Free() => !Locked() && !Rcs().RcsUsedByOtherModule;
            string OtherUses() => Locked() ? TechLocked() : L("#MechJeb_NewUI_PaletteOtherRcs");

            list.Add(new PaletteCommand("smartrcs:off", () => L("#MechJeb_NewUI_PaletteSmartRcsOff"), "Smart RCS Off", s => Set(MechJebModuleSmartRcs.Target.OFF))
                .ListedWhen(() => InFlight && Rcs() != null)
                .When(Free, OtherUses)
                .Confirm(() => L("#MechJeb_NewUI_PaletteSmartRcsOffPreview"))
                .Tagged(() => Rcs() != null && Rcs().target == MechJebModuleSmartRcs.Target.OFF ? L("#MechJeb_NewUI_PaletteTagActive") : AutopilotTag()));
            list.Add(new PaletteCommand("smartrcs:zero-rvel", () => L("#MechJeb_NewUI_PaletteSmartRcsZero"), "Smart RCS Zero relative velocity",
                    s => Set(MechJebModuleSmartRcs.Target.ZERO_RVEL))
                .ListedWhen(() => InFlight && Rcs() != null)
                .When(() => Free() && HasTarget(), () => Free() ? L("#MechJeb_NewUI_PaletteNoTarget") : OtherUses())
                .Confirm(() => L("#MechJeb_NewUI_PaletteSmartRcsZeroPreview"))
                .Tagged(() => Rcs() != null && Rcs().target == MechJebModuleSmartRcs.Target.ZERO_RVEL ? L("#MechJeb_NewUI_PaletteTagActive") : AutopilotTag()));
        }

        // ---- maneuver nodes ----

        static int NodeCount()
        {
            if (!InFlight) return 0;
            PatchedConicSolver solver = core.vessel.patchedConicSolver;
            return solver != null ? solver.maneuverNodes.Count : 0;
        }

        static bool HasNode() => NodeCount() > 0;

        static ManeuverNode NextNode() => HasNode() ? core.vessel.patchedConicSolver.maneuverNodes[0] : null;

        static bool Executing() => core != null && core.Node != null && core.Node.Enabled;

        // the Δv of the next node is set (the toolbar's Execute button refuses an empty node too)
        static bool NextNodeSet() => NextNode() != null && NextNode().DeltaV.magnitude > 0.0001;

        static string NoNodeReason() => HasNode() ? L("#MechJeb_NewUI_PaletteNodeEmpty") : L("#MechJeb_NewUI_PaletteNoNode");

        static string NodeTag() => L("#MechJeb_NewUI_PaletteTagNode");

        static void Nodes(List<PaletteCommand> list)
        {
            bool Usable() => InFlight && core.Node != null;
            MechJebModuleManeuverPlanner planner = core?.GetComputerModule<MechJebModuleManeuverPlanner>();
            object Controller() => planner;
            // as the toolbar's Execute Node button: only with the Maneuver Planner unlocked
            bool Locked() => planner == null || planner.TechLocked;
            bool CanExecute() => !Locked() && NextNodeSet();
            string CantExecute() => Locked() ? TechLocked() : NoNodeReason();

            list.Add(new PaletteCommand("node:execute-one", () => L("#MechJeb_NewUI_PaletteNodeOne"), "Execute next node", s => core.Node.ExecuteOneNode(Controller()))
                .ListedWhen(() => Usable() && !Executing())
                .When(CanExecute, CantExecute)
                .Confirm(() =>
                {
                    ManeuverNode n = NextNode();
                    double dv = n != null ? n.DeltaV.magnitude : 0;
                    double dt = n != null ? n.UT - Planetarium.GetUniversalTime() : 0;
                    return Localizer.Format("#MechJeb_NewUI_PaletteNodeOnePreview", dv.ToString("0.0"), GuiUtils.TimeToDHMS(Mathf.Max(0, (float)dt)));
                })
                .Tagged(NodeTag));
            list.Add(new PaletteCommand("node:execute-all", () => L("#MechJeb_NewUI_PaletteNodeAll"), "Execute all nodes", s => core.Node.ExecuteAllNodes(Controller()))
                .ListedWhen(() => Usable() && !Executing())
                .When(CanExecute, CantExecute)
                .Confirm(() => Localizer.Format("#MechJeb_NewUI_PaletteNodeAllPreview", NodeCount()))
                .Tagged(NodeTag));
            list.Add(new PaletteCommand("node:abort", () => L("#MechJeb_NewUI_PaletteNodeAbort"), "Abort node execution", s => core.Node.Abort())
                .ListedWhen(() => Usable() && Executing())
                .Confirm(() => L("#MechJeb_NewUI_PaletteNodeAbortPreview"))
                .Tagged(NodeTag));
            list.Add(new PaletteCommand("node:remove-all", () => L("#MechJeb_NewUI_PaletteNodeRemove"), "Remove all maneuver nodes", s => core.vessel.RemoveAllManeuverNodes())
                .ListedWhen(Usable)
                .When(() => !Locked() && HasNode() && !Executing(),
                    () => Locked() ? TechLocked() : Executing() ? L("#MechJeb_NewUI_PaletteNodeRunning") : L("#MechJeb_NewUI_PaletteNoNode"))
                .Confirm(() => Localizer.Format("#MechJeb_NewUI_PaletteNodeRemovePreview", NodeCount()))
                .Tagged(NodeTag));
        }
    }
}
