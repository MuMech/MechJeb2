using System;
using System.Collections.Generic;
using KodeKit;
using KodeUI;
using UnityEngine;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>
    /// Native KodeKit versions of GeneralInfoItem methods (IMGUI draw methods registered as info items), by item id.
    /// Items without a builder are hosted with ImguiBlock. Builders take the refresh list for work done at the
    /// window's refresh rate.
    /// </summary>
    public static partial class GeneralItemBuilders
    {
        delegate void Builder(KodeWindow w, UIObject parent, object target, List<Action> refresh);

        static readonly Dictionary<string, Builder> builders = new Dictionary<string, Builder>
        {
            ["General:SolarPanelController.SolarPanelDeployButton"] = (w, p, t, r) =>
                DeployButton(w, p, (MechJebModuleDeployableController)t, "#MechJeb_SolarPanelDeployButton"), //"Auto-deploy solar panels"
            ["General:DeployableAntennaController.AntennaDeployButton"] = (w, p, t, r) =>
                DeployButton(w, p, (MechJebModuleDeployableController)t, "#MechJeb_Autodeployantennas"), //"Auto-deploy antennas"
            ["General:StagingController.AutostageSettingsInfoItem"] = (w, p, t, r) => AutostageSettings(w, p, (MechJebModuleStagingController)t),
            ["General:StagingController.ClampAutostageThrust"] = (w, p, t, r) => ClampAutostageThrust(w, p, (MechJebModuleStagingController)t),
            ["General:ThrustWindow.AutostageOnceItem"] = (w, p, t, r) => AutostageOnce(w, p, (MechJebModuleThrustWindow)t),
            ["General:InfoItems.HorizontalSeparator"] = (w, p, t, r) => Separator(p),
            ["General:InfoItems.AllStageStats"] = (w, p, t, r) =>
            {
                var table = new StageStatsTable(w, p, ((MechJebModuleInfoItems)t).StageStatsHelper);
                r.Add(table.Refresh);
            },

            // MechJebModuleInfoItems
            ["General:InfoItems.DockingGuidanceVelocity"] = (w, p, t, r) => DockingVelocity(w, p, (MechJebModuleInfoItems)t),
            ["General:InfoItems.DockingGuidanceAngularVelocity"] = (w, p, t, r) => DockingAngularVelocity(w, p, (MechJebModuleInfoItems)t),
            ["General:InfoItems.DockingGuidancePosition"] = (w, p, t, r) => DockingPosition(w, p, (MechJebModuleInfoItems)t),
            ["General:InfoItems.AllPlanetPhaseAngles"] = (w, p, t, r) => PhaseAngles(w, p, (MechJebModuleInfoItems)t, true),
            ["General:InfoItems.AllMoonPhaseAngles"] = (w, p, t, r) => PhaseAngles(w, p, (MechJebModuleInfoItems)t, false),
            ["General:InfoItems.LatLonClipbardCopy"] = (w, p, t, r) =>
            {
                var items = (MechJebModuleInfoItems)t;
                KodeWindow.Button(p, "#MechJeb_InfoItems_CopytoClipboard", items.CopyLatLonAltToClipboard, true); //"Copy Lat/Lon/Alt to Clipboard"
            },
            ["General:InfoItems.DebugString"] = (w, p, t, r) => PoolsStatusRows(w, p),

            // MechJebModuleLandingGuidance, MechJebModuleMenu
            ["General:LandingGuidance.DrawGUITogglePredictions"] = (w, p, t, r) => new LandingPredictionsBlock(w, p, (MechJebModuleLandingGuidance)t),
            ["General:Menu.MenuPosition"] = (w, p, t, r) => MenuView.BuildMenuPosition(w, p, (MechJebModuleMenu)t),

            // MechJebModuleRCSBalancer, MechJebModuleRCSController
            ["General:RCSBalancer.RCSBalancerInfoItem"] = (w, p, t, r) => RcsBalancerInfo(w, p, (MechJebModuleRCSBalancer)t),
            ["General:RCSBalancer.RCSThrusterStateInfoItem"] = (w, p, t, r) => RcsThrusterStates(w, p, (MechJebModuleRCSBalancer)t),
            ["General:RCSBalancer.RCSPartThrottlesInfoItem"] = (w, p, t, r) => RcsPartThrottles(w, p, (MechJebModuleRCSBalancer)t),
            ["General:RCSBalancer.ControlVectorInfoItem"] = (w, p, t, r) => ControlVector(w, p),
            ["General:RCSController.PIDGUI"] = (w, p, t, r) => RcsPid(w, p, (MechJebModuleRCSController)t),

            // MechJebModuleSmartASS, MechJebModuleSmartRcs
            ["General:SmartASS.AutoDisableSmartASS"] = (w, p, t, r) =>
            {
                var s = (MechJebModuleSmartASS)t;
                w.Toggle(p, s.Core.eduMode ? "#MechJeb_SmartASS_checkbox1" : "#MechJeb_SmartASS_checkbox2", () => s.autoDisableSmartASS,
                    v => s.autoDisableSmartASS = v); //"Disable SmartACS automatically" : "Disable SmartASS automatically"
            },
            ["General:SmartRcs.AutoDisableSmartRCS"] = (w, p, t, r) =>
            {
                var s = (MechJebModuleSmartRcs)t;
                w.Toggle(p, "#MechJeb_SmartRcs_checkbox1 ", () => s.autoDisableSmartRCS, v => s.autoDisableSmartRCS = v); //"Disable SmartRcs automatically"
            },

            // MechJebModuleThrustController, MechJebModuleThrustWindow, MechJebModuleWarpController
            ["General:ThrustController.LimitToMaxDynamicPressureInfoItem"] = (w, p, t, r) => LimitToMaxDynamicPressure(w, p, (MechJebModuleThrustController)t),
            ["General:ThrustController.LimitToPreventOverheatsInfoItem"] = (w, p, t, r) => LimitToPreventOverheats(w, p, (MechJebModuleThrustController)t),
            ["General:ThrustController.LimitToPreventFlameoutInfoItem"] = (w, p, t, r) => LimitToPreventFlameout(w, p, (MechJebModuleThrustController)t),
            ["General:ThrustController.LimitToPreventUnstableIgnitionInfoItem"] = (w, p, t, r) =>
                LimitToPreventUnstableIgnition(w, p, (MechJebModuleThrustController)t),
            ["General:ThrustController.AutoRCsUllageInfoItem"] = (w, p, t, r) => AutoRcsUllage(w, p, (MechJebModuleThrustController)t),
            ["General:ThrustController.LimitAccelerationInfoItem"] = (w, p, t, r) => LimitAcceleration(w, p, (MechJebModuleThrustController)t),
            ["General:ThrustController.LimitThrottleInfoItem"] = (w, p, t, r) => LimitThrottle(w, p, (MechJebModuleThrustController)t),
            ["General:ThrustController.LimiterMinThrottleInfoItem"] = (w, p, t, r) => LimiterMinThrottle(w, p, (MechJebModuleThrustController)t),
            ["General:ThrustController.DifferentialThrottleMenu"] = (w, p, t, r) => DifferentialThrottle(w, p, (MechJebModuleThrustController)t),
            ["General:ThrustController.LimitElectricInfoItem"] = (w, p, t, r) => LimitElectric(w, p, (MechJebModuleThrustController)t),
            ["General:ThrustWindow.Autostage"] = (w, p, t, r) => Autostage(w, p, (MechJebModuleThrustWindow)t),
            ["General:WarpController.ControlWarpButton"] = (w, p, t, r) => ControlWarpButton(w, p, (MechJebModuleWarpController)t),

            // VesselState
            ["General:VesselState.DebugString"] = (w, p, t, r) => VesselDebugString(w, p),
            ["General:VesselState.RCSTranslation"] = (w, p, t, r) => RcsVector(w, p, "#MechJeb_RCSTranslation", () => ((VesselState)t).RCSThrustAvailable), //"RCS Translation"
            ["General:VesselState.RCSTorque"] = (w, p, t, r) => RcsVector(w, p, "#MechJeb_RCSTorque", () => ((VesselState)t).RCSTorqueAvailable), //"RCS Torque"
            ["General:VesselState.TorqueCompare"] = (w, p, t, r) => TorqueCompare(w, p, (VesselState)t),
        };

        public static bool Has(string id) => builders.ContainsKey(id);

        /// <summary>Build the native version of a general item, by id; false when there is none.</summary>
        public static bool TryBuild(KodeWindow w, UIObject parent, GeneralInfoItem item, List<Action> refresh)
        {
            if (!builders.TryGetValue(item.id, out Builder b)) return false;
            b(w, parent, item.Target, refresh);
            return true;
        }

        /// <summary>Build a general item by id from its module (for windows that call the IMGUI method directly).</summary>
        public static void Build(KodeWindow w, UIObject parent, string id, object target, List<Action> refresh = null) =>
            builders[id](w, parent, target, refresh ?? new List<Action>());

        // ---- the items ----

        /// <summary>SolarPanelDeployButton / AntennaDeployButton: auto-deploy toggle + "Toggle ... (currently ...)" button.</summary>
        static void DeployButton(KodeWindow w, UIObject parent, MechJebModuleDeployableController c, string autoKey)
        {
            w.Toggle(parent, autoKey, () => c.AutoDeploy, v => c.AutoDeploy = v);
            UIButton b = KodeWindow.Button(parent, c.CurrentButtonText ?? " ", c.ToggleDeploy, true);
            string shown = c.CurrentButtonText;
            w.AddSync(() =>
            {
                string s = c.CurrentButtonText;
                if ((object)s == shown) return; // the controller swaps between cached strings
                shown = s;
                b.Text(s ?? " ");
            });
        }

        /// <summary>MechJebModuleStagingController.AutostageSettingsInfoItem, in a box (a property grid in the modern layout).</summary>
        static void AutostageSettings(KodeWindow w, UIObject parent, MechJebModuleStagingController s)
        {
            if (KodeWindow.ModernLayout)
            {
                ModernRows.AutostageSettings(w, parent, s);
                return;
            }

            LayoutPanel box = KodeWindow.Box(parent);

            Layout limit = KodeWindow.Row(box);
            KodeWindow.Label(limit, "#MechJeb_Ascent_label42", true); //"Stop at stage #"
            EditUi.Field(w, limit, () => s.AutostageLimit, 40);

            Layout delays = KodeWindow.Row(box);
            KodeWindow.Label(delays, "Delays: ");
            KodeWindow.Space(delays, 30);
            KodeWindow.Label(delays, "pre: ");
            EditUi.Field(w, delays, () => s.AutostagePreDelay, 35);
            KodeWindow.Label(delays, "s");
            KodeWindow.FlexibleSpace(delays);
            KodeWindow.Label(delays, "post: ");
            EditUi.Field(w, delays, () => s.AutostagePostDelay, 35);
            KodeWindow.Label(delays, "s");

            ClampAutostageThrust(w, box, s);

            KodeWindow.Label(box, "#MechJeb_Ascent_label38"); //"Stage fairings when:"
            Field(w, box, s._sFairingMinDynamicPressure, s.FairingMaxDynamicPressure, "kPa", 50);
            Field(w, box, s._sFairingMinAltitude, s.FairingMinAltitude, "km", 50);
            Field(w, box, s._sFairingMaxAerothermalFlux, s.FairingMaxAerothermalFlux, "W/m²", 50);

            Layout hot = KodeWindow.Row(box);
            w.Toggle(hot, "#MechJeb_Ascent_hotStaging", () => s.HotStaging, v => s.HotStaging = v); //"Support hotstaging"
            KodeWindow.FlexibleSpace(hot);
            KodeWindow.Label(hot, s._sLeadTime);
            EditUi.Field(w, hot, () => s.HotStagingLeadTime, 35);
            KodeWindow.Label(hot, "s");

            Layout solids = KodeWindow.Row(box);
            w.Toggle(solids, "#MechJeb_Ascent_dropSolids", () => s.DropSolids, v => s.DropSolids = v); //"Drop solids early"
            KodeWindow.FlexibleSpace(solids);
            EditUi.Field(w, solids, () => s.DropSolidsTwrPct, 35);
            KodeWindow.Label(solids, "% rocket accel");
        }

        static void ClampAutostageThrust(KodeWindow w, UIObject parent, MechJebModuleStagingController s)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, CachedLocalizer.Instance.MechJebAscentLabel44, true); //"Clamp AutoStage Thrust "
            EditFields.Field(w, row, () => s.ClampAutoStageThrustPct.Text, x => ModernRows.SetClamp(s, x), 50, () => s.ClampAutoStageThrustPct.IsParsed);
            KodeWindow.Label(row, "%");
        }

        static void Field(KodeWindow w, UIObject parent, string label, EditableDoubleMult ed, string unit, float width)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, label, true);
            EditUi.Field(w, row, () => ed, width);
            KodeWindow.Label(row, unit);
        }

        /// <summary>MechJebModuleThrustWindow.AutostageOnceItem: "Autostaging (active once)" or the "Autostage once" button.</summary>
        static void AutostageOnce(KodeWindow w, UIObject parent, MechJebModuleThrustWindow window)
        {
            MechJebModuleStagingController s = window.Core.Staging;
            string once = KSP.Localization.Localizer.Format("#MechJeb_Utilities_label1", KSP.Localization.Localizer.Format("#MechJeb_Utilities_label1_1"));
            string always = KSP.Localization.Localizer.Format("#MechJeb_Utilities_label1", " ");
            UIText status = w.BoundLabel(parent, () => s.AutostagingOnce ? once : always, true);
            w.Visible(status, () => s.Enabled);
            UIButton b = KodeWindow.Button(parent, "#MechJeb_Utilities_button1", () => s.AutostageOnce(window), true); //"Autostage once"
            w.Visible(b.gameObject, () => !s.Enabled);
        }

        static void Separator(UIObject parent)
        {
            if (KodeWindow.ModernLayout)
            {
                KodeWindow.Rule(parent);
                return;
            }

            UIEmpty line = parent.Add<UIEmpty>("Separator");
            line.PreferredHeight(2).FlexibleLayout(true, false).Finish();
            Image img = line.gameObject.AddComponent<Image>();
            KodeWindow.Styled(line.gameObject, th => img.color = th.Dim * new Color(1, 1, 1, 0.6f));
        }
    }
}
