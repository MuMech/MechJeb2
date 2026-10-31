using System;
using System.Collections.Generic;
using KodeKit;
using KodeUI;
using KSP.Localization;
using MechJebLibBindings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MuMech.MechJebModuleThrustController;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleLandingGuidance ("Landing Guidance") in KodeKit, row for row with its WindowGUI, including the
    /// landing prediction block of DrawGUITogglePredictions (LandingPredictionsBlock, shared with the custom windows).
    /// The pieces of WindowGUI that pushed settings into the predictor every frame run in BeforeSync.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleLandingGuidance))]
    public class LandingGuidanceView : KodeView
    {
        readonly MechJebModuleLandingGuidance guidance;
        MechJebModuleLandingPredictions predictor;

        public LandingGuidanceView(MechJebModuleLandingGuidance guidance) : base(guidance) => this.guidance = guidance;

        protected override float MinWidth => 200;

        MechJebCore Core => guidance.Core;
        MechJebModuleLandingPredictions Predictor => predictor ??= Core.GetComputerModule<MechJebModuleLandingPredictions>();
        CelestialBody Body => Core.vessel != null ? Core.vessel.mainBody : null;
        bool LandedOrSplashed => Core.vessel != null && Core.vessel.LandedOrSplashed;
        bool HasPositionTarget => Core.Target != null && Core.Target.PositionTargetExists;
        // WindowGUI pushed these into the predictor each frame while the window was drawn
        protected override void BeforeSync()
        {
            MechJebModuleLandingPredictions p = Predictor;
            MechJebModuleLandingAutopilot landing = Core.Landing;
            if (landing == null || p == null) return;

            p.maxOrbits = landing.Enabled ? 0.5 : 4;
            p.noSkipToFreefall = !landing.Enabled;
            p.deployChutes = landing.DeployChutes;
            p.limitChutesStage = landing.LimitChutesStage;
        }

        // landing sites of the current body, cached until the body or the list changes
        string[] siteNames = new string[0];
        CelestialBody siteBody;
        int siteCount = -1;
        readonly List<MechJebModuleLandingGuidance.LandingSite> sites = new List<MechJebModuleLandingGuidance.LandingSite>();

        string[] SiteNames()
        {
            CelestialBody body = Body;
            List<MechJebModuleLandingGuidance.LandingSite> all = MechJebModuleLandingGuidance.LandingSites;
            int count = all?.Count ?? 0;
            if (body == siteBody && count == siteCount) return siteNames;

            siteBody = body;
            siteCount = count;
            sites.Clear();
            for (int i = 0; i < count; i++)
                if (all[i].Body == body)
                    sites.Add(all[i]);
            siteNames = new string[sites.Count];
            for (int i = 0; i < sites.Count; i++) siteNames[i] = sites[i].Name;
            return siteNames;
        }

        // biome of the target, only looked up when the coordinates change
        double biomeLat = double.NaN, biomeLon = double.NaN;
        CelestialBody biomeBody;
        string biome = "";

        string TargetBiome()
        {
            CelestialBody body = Core.Target.targetBody;
            double lat = Core.Target.targetLatitude, lon = Core.Target.targetLongitude;
            if (lat != biomeLat || lon != biomeLon || body != biomeBody)
            {
                biomeLat = lat;
                biomeLon = lon;
                biomeBody = body;
                biome = body != null ? body.GetExperimentBiomeSafe(lat, lon) ?? "" : "";
            }

            return biome;
        }

        protected override void BuildContents(KodeWindow w)
        {
            if (KodeWindow.ModernLayout)
            {
                BuildModern(w);
                return;
            }

            UIObject r = w.Root;

            // ---- target ----
            Layout target = KodeWindow.Panel(r);
            w.Visible(target.gameObject, () => HasPositionTarget);
            KodeWindow.Label(target, "#MechJeb_LandingGuidance_label1"); //Target coordinates:
            AngleRow(w, target, () => Core.Target.targetLatitude, EditableAngle.Direction.NS, Sprites.Direction.Up, 10, Sprites.Direction.Down, -10, true); // ▲ ▼ as sprites: KSP's fonts lack them
            AngleRow(w, target, () => Core.Target.targetLongitude, EditableAngle.Direction.EW, Sprites.Direction.Left, -10, Sprites.Direction.Right, 10, false);

            TargetReadouts(w, target);
            TargetButtons(w, r, false);

            // ---- landing sites ----
            SiteRow(w, r);

            // ---- predictions ----
            new LandingPredictionsBlock(w, r, guidance);

            // ---- autopilot ----
            Layout autopilot = KodeWindow.Panel(r);
            w.Visible(autopilot.gameObject, () => Core.Landing != null);
            Autopilot(w, autopilot);
        }

        double TargetAsl() => Body.TerrainAltitude(Core.Target.targetLatitude, Core.Target.targetLongitude);

        /// <summary>EditableAngle.DrawEditGUI (d ° m ' s " N/S) followed by the 10 m step buttons, on one row.</summary>
        void AngleRow(KodeWindow w, UIObject parent, Func<EditableAngle> angle, EditableAngle.Direction direction, Sprites.Direction first, double firstDelta, Sprites.Direction second, double secondDelta,
            bool latitude)
        {
            Layout row = KodeWindow.Row(parent);
            OpUi.AngleRow(w, row, angle, direction, false);
            KodeWindow.IconButton(row, Sprites.Arrow(first), () => Move(latitude, firstDelta));
            KodeWindow.Label(row, "10m");
            KodeWindow.IconButton(row, Sprites.Arrow(second), () => Move(latitude, secondDelta));
        }

        void Move(bool latitude, double meters)
        {
            double alt = TargetAsl();
            if (latitude) guidance.MoveByMeter(ref Core.Target.targetLatitude, meters, alt);
            else guidance.MoveByMeter(ref Core.Target.targetLongitude, meters, alt);
        }

        // ---- the autopilot block ----
        void Autopilot(KodeWindow w, UIObject p)
        {
            KodeWindow.Label(p, "#MechJeb_LandingGuidance_label2"); //Autopilot:

            LandButtons(w, p, false);

            Layout touchdown = KodeWindow.Row(p);
            KodeWindow.Label(touchdown, "#MechJeb_LandingGuidance_label3"); //Touchdown speed:
            EditUi.Field(w, touchdown, () => Core.Landing?.TouchdownSpeed, 35);
            KodeWindow.Label(touchdown, "m/s");

            NodeUi.AutowarpToggle(w, p, Core, "#MechJeb_LandingGuidance_checkbox1"); //Auto-warp

            w.Toggle(p, "#MechJeb_LandingGuidance_checkbox2", () => Core.Landing != null && Core.Landing.DeployGears, v => //Deploy Landing Gear
            {
                if (Core.Landing != null) Core.Landing.DeployGears = v;
            });
            StageRow(w, p, "#MechJeb_LandingGuidance_label4", () => Core.Landing?.LimitGearsStage); //Stage Limit:
            w.Toggle(p, "#MechJeb_LandingGuidance_checkbox3", () => Core.Landing != null && Core.Landing.DeployChutes, v => //Deploy Parachutes
            {
                if (Core.Landing != null) Core.Landing.DeployChutes = v;
            });
            StageRow(w, p, "#MechJeb_LandingGuidance_label5", () => Core.Landing?.LimitChutesStage); //Stage Limit:
            w.Toggle(p, "#MechJeb_LandingGuidance_checkbox4", () => Core.Landing != null && Core.Landing.RCSAdjustment, v => //Use RCS for small adjustment
            {
                if (Core.Landing != null) Core.Landing.RCSAdjustment = v;
            });

            LowerThrottleLimit(w, p);

            // while landing
            Layout running = KodeWindow.Panel(p);
            w.Visible(running.gameObject, () => Core.Landing != null && Core.Landing.Enabled);
            RunningLabels(w, running);
        }

        // ---- the parts both layouts share ----

        /// <summary>ASL and biome of the target, filled every frame without allocating; returns both labels.</summary>
        UIText[] TargetReadouts(KodeWindow w, UIObject target)
        {
            Layout asl = KodeWindow.Row(target);
            return new[]
            {
                GuidanceUi.Live(w, asl, "ASL: -1.234 km", true, ct => ct.Append("ASL: ").AppendSI(TargetAsl()).Append('m'), () => HasPositionTarget),
                GuidanceUi.Live(w, asl, "Shores of the Northern Sea", true, ct => ct.Append(TargetBiome()), () => HasPositionTarget)
            };
        }

        void TargetButtons(KodeWindow w, UIObject r, bool flexible)
        {
            UIButton enter = KodeWindow.Button(r, "#MechJeb_LandingGuidance_button1", () => //Enter target coordinates
                Core.Target.SetPositionTarget(Body, Core.Target.targetLatitude, Core.Target.targetLongitude), flexible);
            w.Visible(enter.gameObject, () => !HasPositionTarget);
            KodeWindow.Button(r, "#MechJeb_LandingGuidance_button2", () => Core.Target.PickPositionTargetOnMap(), flexible); //Pick target on map
        }

        /// <summary>The landing sites list and its "Set" button (shown when there are sites).</summary>
        void SiteRow(KodeWindow w, UIObject r)
        {
            Layout siteRow = KodeWindow.Row(r);
            new ComboBox(w, siteRow, () => guidance._landingSiteIdx, i => guidance._landingSiteIdx = i, SiteNames);
            KodeWindow.Button(siteRow, "Set", () =>
            {
                SiteNames();
                if (sites.Count == 0) return;
                MechJebModuleLandingGuidance.LandingSite site = sites[Mathf.Clamp(guidance._landingSiteIdx, 0, sites.Count - 1)];
                Core.Target.SetPositionTarget(Body, site.Latitude, site.Longitude);
            });
            w.Visible(siteRow.gameObject, () => SiteNames().Length > 0);
        }

        /// <summary>Abort while landing; Land at target / Land somewhere otherwise.</summary>
        void LandButtons(KodeWindow w, UIObject p, bool flexible)
        {
            UIButton abort = KodeWindow.Button(p, "#MechJeb_LandingGuidance_button3", () => Core.Landing.StopLanding(), ButtonRole.Danger, flexible); //Abort autoland
            w.Visible(abort.gameObject, () => Core.Landing != null && Core.Landing.Enabled);
            Layout land = KodeWindow.Row(p);
            w.Visible(land.gameObject, () => Core.Landing != null && !Core.Landing.Enabled);
            UIButton atTarget = KodeWindow.Button(land, "#MechJeb_LandingGuidance_button4", () => Core.Landing.LandAtPositionTarget(guidance),
                ButtonRole.Primary, true); //Land at target
            GuidanceUi.Enable(w, atTarget, () => HasPositionTarget && !LandedOrSplashed);
            UIButton somewhere = KodeWindow.Button(land, "#MechJeb_LandingGuidance_button5", () => Core.Landing.LandUntargeted(guidance), true); //Land somewhere
            GuidanceUi.Enable(w, somewhere, () => !LandedOrSplashed);
        }

        /// <summary>Status, step and descent mode while landing; each text is made when what it shows changes.</summary>
        void RunningLabels(KodeWindow w, UIObject running)
        {
            string status = Localizer.Format("#MechJeb_LandingGuidance_label6");
            string step = Localizer.Format("#MechJeb_LandingGuidance_label7");
            string mode = Localizer.Format("#MechJeb_LandingGuidance_label8");
            w.Wrap(w.BoundLabel(running, Memo.Of(() => Core.Landing?.Status ?? "", s => status + s), true)); //Status:
            w.BoundLabel(running, Memo.Of(() => Core.Landing?.CurrentStep?.GetType(), t => step + (t != null ? t.Name : "N/A")), true); //Step:
            w.BoundLabel(running, Memo.Of(() => (Core.Landing?.DescentSpeedPolicy?.GetType(), Core.Landing != null && Core.Landing.UseAtmosphereToBrake()),
                k => mode + (k.Item1 != null ? k.Item1.Name : "N/A") + " (" + k.Item2 + ")"), true); //Mode
        }

        static void StageRow(KodeWindow w, UIObject parent, string key, Func<EditableInt> ed) => EditUi.Row(w, parent, key, ed, null, 35);

        // ---- modern layout: D-M-S fields with the hemisphere as a choice, one nudge pad, the options in a property grid ----

        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout target = KodeWindow.Panel(r);
            w.Visible(target.gameObject, () => HasPositionTarget);
            KodeWindow.Header(target, "#MechJeb_LandingGuidance_label1"); //Target coordinates:
            Layout coords = KodeWindow.Row(target);
            Layout fields = KodeWindow.Panel(coords);
            ModernAngle(w, fields, "Lat", () => Core.Target.targetLatitude, EditableAngle.Direction.NS);
            ModernAngle(w, fields, "Lon", () => Core.Target.targetLongitude, EditableAngle.Direction.EW);

            // the four 10 m nudges as one pad, the step in its centre
            KodeWindow.CompassGrid pad = KodeWindow.Compass(coords, 20);
            pad.Grid.FlexibleLayout(0f, 0f); // 0, not -1: the grid itself asks for flexible width
            KodeWindow.IconButton(pad[Dir.N], Sprites.Arrow(Sprites.Direction.Up), () => Move(true, 10));
            KodeWindow.IconButton(pad[Dir.W], Sprites.Arrow(Sprites.Direction.Left), () => Move(false, -10));
            UIText step = KodeWindow.Label(pad[Dir.C], "10m");
            step.tmpText.alignment = TextAlignmentOptions.Center;
            KodeWindow.IconButton(pad[Dir.E], Sprites.Arrow(Sprites.Direction.Right), () => Move(false, 10));
            KodeWindow.IconButton(pad[Dir.S], Sprites.Arrow(Sprites.Direction.Down), () => Move(true, -10));

            foreach (UIText t in TargetReadouts(w, target)) KodeWindow.ToneText(t, KodeWindow.Tone.Dim);
            TargetButtons(w, r, true);
            SiteRow(w, r);

            new LandingPredictionsBlock(w, r, guidance);

            Layout autopilot = KodeWindow.Panel(r);
            w.Visible(autopilot.gameObject, () => Core.Landing != null);
            ModernAutopilot(w, autopilot);
        }

        /// <summary>Lat [d] ° [m] ' [s] " [N|S]: the hemisphere is a two-way choice, not a button that flips.</summary>
        static void ModernAngle(KodeWindow w, UIObject parent, string label, Func<EditableAngle> angle, EditableAngle.Direction direction)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, label);
            OpUi.AngleRow(w, row, angle, direction, false);
        }

        void ModernAutopilot(KodeWindow w, UIObject p)
        {
            KodeWindow.Header(p, "#MechJeb_LandingGuidance_label2"); //Autopilot:

            LandButtons(w, p, true);

            KodeWindow.PropertyGrid g = w.Properties(p);
            EditUi.Grid(g, "#MechJeb_LandingGuidance_label3", () => Core.Landing?.TouchdownSpeed, "m/s", 50); //Touchdown speed:
            g.Switch("#MechJeb_LandingGuidance_checkbox1", () => NodeUi.Autowarp(Core), v => NodeUi.Autowarp(Core, v)); //Auto-warp
            g.Switch("#MechJeb_LandingGuidance_checkbox2", () => Core.Landing != null && Core.Landing.DeployGears, v => //Deploy Landing Gear
            {
                if (Core.Landing != null) Core.Landing.DeployGears = v;
            });
            Layout gears = ModernStage(g, "#MechJeb_LandingGuidance_label4", () => Core.Landing?.LimitGearsStage); //Stage Limit:
            g.Dim(gears.gameObject, null, () => Core.Landing == null || !Core.Landing.DeployGears);
            g.Switch("#MechJeb_LandingGuidance_checkbox3", () => Core.Landing != null && Core.Landing.DeployChutes, v => //Deploy Parachutes
            {
                if (Core.Landing != null) Core.Landing.DeployChutes = v;
            });
            Layout chutes = ModernStage(g, "#MechJeb_LandingGuidance_label5", () => Core.Landing?.LimitChutesStage); //Stage Limit:
            g.Dim(chutes.gameObject, null, () => Core.Landing == null || !Core.Landing.DeployChutes);
            g.Switch("#MechJeb_LandingGuidance_checkbox4", () => Core.Landing != null && Core.Landing.RCSAdjustment, v => //Use RCS for small adjustment
            {
                if (Core.Landing != null) Core.Landing.RCSAdjustment = v;
            });
            MechJebModuleThrustController t = Core.Thrust;
            ModernRows.MinThrottle(g, t, 50); //"Keep limited throttle over"

            // while landing: the autopilot's state in a status bar
            Layout running = w.StatusBar(p, () => KodeWindow.Tone.Normal);
            w.Visible(running.gameObject, () => Core.Landing != null && Core.Landing.Enabled);
            RunningLabels(w, running);
        }

        static Layout ModernStage(KodeWindow.PropertyGrid g, string key, Func<EditableInt> ed) => EditUi.Grid(g, key, ed, null, 50);

        // MechJebModuleThrustController.LimiterMinThrottleInfoItem: [x] "Keep limited throttle over" [min] %
        void LowerThrottleLimit(KodeWindow w, UIObject parent)
        {
            GeneralItemBuilders.Build(w, parent, "General:ThrustController.LimiterMinThrottleInfoItem", Core.Thrust);
        }
    }
}
