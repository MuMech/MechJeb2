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

        // "prefix" + value, rebuilt only when the value changes
        static Func<string> Cached<T>(Func<T> key, Func<T, string> build)
        {
            EqualityComparer<T> cmp = EqualityComparer<T>.Default;
            T last = default;
            string text = null;
            return () =>
            {
                T k = key();
                if (text == null || !cmp.Equals(k, last))
                {
                    last = k;
                    text = build(k);
                }

                return text;
            };
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

            Layout asl = KodeWindow.Row(target);
            GuidanceUi.Live(w, asl, "ASL: -1.234 km", true, ct => ct.Append("ASL: ").AppendSI(TargetAsl()).Append('m'), () => HasPositionTarget);
            GuidanceUi.Live(w, asl, "Shores of the Northern Sea", true, ct => ct.Append(TargetBiome()), () => HasPositionTarget);

            UIButton enter = KodeWindow.Button(r, "#MechJeb_LandingGuidance_button1", () => //Enter target coordinates
                Core.Target.SetPositionTarget(Body, Core.Target.targetLatitude, Core.Target.targetLongitude));
            w.Visible(enter.gameObject, () => !HasPositionTarget);

            KodeWindow.Button(r, "#MechJeb_LandingGuidance_button2", () => Core.Target.PickPositionTargetOnMap()); //Pick target on map

            // ---- landing sites ----
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
            AngleField(w, row, () => angle().Degrees);
            KodeWindow.Label(row, "°");
            AngleField(w, row, () => angle().Minutes);
            KodeWindow.Label(row, "'");
            AngleField(w, row, () => angle().Seconds);
            KodeWindow.Label(row, "\"");

            UIButton sign = KodeWindow.Button(row, direction == EditableAngle.Direction.NS ? "N" : "E", () =>
            {
                EditableAngle a = angle();
                a.Negative = !a.Negative;
            });
            GuidanceUi.MinWidth(sign, 25);
            string shown = null;
            w.AddSync(() =>
            {
                string s = direction == EditableAngle.Direction.NS ? angle().Negative ? "S" : "N" : angle().Negative ? "W" : "E";
                if ((object)s == shown) return;
                shown = s;
                sign.Text(s);
            });

            KodeWindow.IconButton(row, Sprites.Arrow(first), () => Move(latitude, firstDelta));
            KodeWindow.Label(row, "10m");
            KodeWindow.IconButton(row, Sprites.Arrow(second), () => Move(latitude, secondDelta));
        }

        void AngleField(KodeWindow w, UIObject row, Func<EditableDouble> ed) =>
            EditFields.Field(w, row, () => ed().Text, s => ed().Text = s, 30, () => ed().IsParsed);

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

            UIButton abort = KodeWindow.Button(p, "#MechJeb_LandingGuidance_button3", () => Core.Landing.StopLanding()); //Abort autoland
            w.Visible(abort.gameObject, () => Core.Landing != null && Core.Landing.Enabled);

            Layout land = KodeWindow.Row(p);
            w.Visible(land.gameObject, () => Core.Landing != null && !Core.Landing.Enabled);
            UIButton atTarget = KodeWindow.Button(land, "#MechJeb_LandingGuidance_button4", () => Core.Landing.LandAtPositionTarget(guidance), true); //Land at target
            GuidanceUi.Enable(w, atTarget, () => HasPositionTarget && !LandedOrSplashed);
            UIButton somewhere = KodeWindow.Button(land, "#MechJeb_LandingGuidance_button5", () => Core.Landing.LandUntargeted(guidance), true); //Land somewhere
            GuidanceUi.Enable(w, somewhere, () => !LandedOrSplashed);

            Layout touchdown = KodeWindow.Row(p);
            KodeWindow.Label(touchdown, "#MechJeb_LandingGuidance_label3"); //Touchdown speed:
            EditFields.Field(w, touchdown, () => Core.Landing?.TouchdownSpeed.Text ?? "", s =>
            {
                if (Core.Landing != null) Core.Landing.TouchdownSpeed.Text = s;
            }, 35, () => Core.Landing?.TouchdownSpeed.IsParsed ?? true);
            KodeWindow.Label(touchdown, "m/s");

            w.Toggle(p, "#MechJeb_LandingGuidance_checkbox1", () => Core.Node != null && Core.Node.Autowarp, v => //Auto-warp
            {
                if (Core.Node != null) Core.Node.Autowarp = v;
            });

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
            string status = Localizer.Format("#MechJeb_LandingGuidance_label6");
            string step = Localizer.Format("#MechJeb_LandingGuidance_label7");
            string mode = Localizer.Format("#MechJeb_LandingGuidance_label8");
            w.BoundLabel(running, Cached(() => Core.Landing?.Status ?? "", s => status + s), true); //Status:
            w.BoundLabel(running, Cached(() => Core.Landing?.CurrentStep?.GetType(), t => step + (t != null ? t.Name : "N/A")), true); //Step:
            w.BoundLabel(running, Cached(() => (Core.Landing?.DescentSpeedPolicy?.GetType(), Core.Landing != null && Core.Landing.UseAtmosphereToBrake()),
                k => mode + (k.Item1 != null ? k.Item1.Name : "N/A") + " (" + k.Item2 + ")"), true); //Mode
        }

        void StageRow(KodeWindow w, UIObject parent, string key, Func<EditableInt> ed)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, key);
            EditFields.Field(w, row, () => ed()?.Text ?? "", s =>
            {
                EditableInt e = ed();
                if (e != null) e.Text = s;
            }, 35);
        }

        // ---- modern layout: D-M-S fields with the hemisphere as a choice, one nudge pad, the options in a property grid ----

        static readonly string[] northSouth = { "N", "S" }, eastWest = { "E", "W" };

        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout target = KodeWindow.Panel(r);
            w.Visible(target.gameObject, () => HasPositionTarget);
            KodeWindow.Header(target, "#MechJeb_LandingGuidance_label1"); //Target coordinates:
            Layout coords = KodeWindow.Row(target);
            Layout fields = KodeWindow.Panel(coords);
            ModernAngle(w, fields, "Lat", () => Core.Target.targetLatitude, northSouth);
            ModernAngle(w, fields, "Lon", () => Core.Target.targetLongitude, eastWest);

            // the four 10 m nudges as one pad, the step in its centre
            KodeWindow.CompassGrid pad = KodeWindow.Compass(coords, 20);
            pad.Grid.FlexibleLayout(0f, 0f); // 0, not -1: the grid itself asks for flexible width
            KodeWindow.IconButton(pad[Dir.N], Sprites.Arrow(Sprites.Direction.Up), () => Move(true, 10));
            KodeWindow.IconButton(pad[Dir.W], Sprites.Arrow(Sprites.Direction.Left), () => Move(false, -10));
            UIText step = KodeWindow.Label(pad[Dir.C], "10m");
            step.tmpText.alignment = TextAlignmentOptions.Center;
            KodeWindow.IconButton(pad[Dir.E], Sprites.Arrow(Sprites.Direction.Right), () => Move(false, 10));
            KodeWindow.IconButton(pad[Dir.S], Sprites.Arrow(Sprites.Direction.Down), () => Move(true, -10));

            Layout asl = KodeWindow.Row(target);
            UIText aslText = GuidanceUi.Live(w, asl, "ASL: -1.234 km", true, ct => ct.Append("ASL: ").AppendSI(TargetAsl()).Append('m'), () => HasPositionTarget);
            UIText biomeText = GuidanceUi.Live(w, asl, "Shores of the Northern Sea", true, ct => ct.Append(TargetBiome()), () => HasPositionTarget);
            foreach (UIText t in new[] { aslText, biomeText })
            {
                UIText x = t;
                KodeWindow.Styled(x.gameObject, th => x.tmpText.color = th.Dim);
            }

            UIButton enter = KodeWindow.Button(r, "#MechJeb_LandingGuidance_button1", () => //Enter target coordinates
                Core.Target.SetPositionTarget(Body, Core.Target.targetLatitude, Core.Target.targetLongitude), ButtonRole.Normal, true);
            w.Visible(enter.gameObject, () => !HasPositionTarget);
            KodeWindow.Button(r, "#MechJeb_LandingGuidance_button2", () => Core.Target.PickPositionTargetOnMap(), ButtonRole.Normal, true); //Pick target on map

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

            new LandingPredictionsBlock(w, r, guidance);

            Layout autopilot = KodeWindow.Panel(r);
            w.Visible(autopilot.gameObject, () => Core.Landing != null);
            ModernAutopilot(w, autopilot);
        }

        /// <summary>Lat [d] ° [m] ' [s] " [N|S]: the hemisphere is a two-way choice, not a button that flips.</summary>
        void ModernAngle(KodeWindow w, UIObject parent, string label, Func<EditableAngle> angle, string[] hemispheres)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, label);
            AngleField(w, row, () => angle().Degrees);
            KodeWindow.Label(row, "°");
            AngleField(w, row, () => angle().Minutes);
            KodeWindow.Label(row, "'");
            AngleField(w, row, () => angle().Seconds);
            KodeWindow.Label(row, "\"");
            Layout hemi = w.Segmented(row, hemispheres, () => angle().Negative ? 1 : 0, i => angle().Negative = i == 1, ChoiceStyle.Joined);
            hemi.FlexibleLayout(0f, 0f);
        }

        void ModernAutopilot(KodeWindow w, UIObject p)
        {
            KodeWindow.Header(p, "#MechJeb_LandingGuidance_label2"); //Autopilot:

            UIButton abort = KodeWindow.Button(p, "#MechJeb_LandingGuidance_button3", () => Core.Landing.StopLanding(), ButtonRole.Danger, true); //Abort autoland
            w.Visible(abort.gameObject, () => Core.Landing != null && Core.Landing.Enabled);
            Layout land = KodeWindow.Row(p);
            w.Visible(land.gameObject, () => Core.Landing != null && !Core.Landing.Enabled);
            UIButton atTarget = KodeWindow.Button(land, "#MechJeb_LandingGuidance_button4", () => Core.Landing.LandAtPositionTarget(guidance),
                ButtonRole.Primary, true); //Land at target
            GuidanceUi.Enable(w, atTarget, () => HasPositionTarget && !LandedOrSplashed);
            UIButton somewhere = KodeWindow.Button(land, "#MechJeb_LandingGuidance_button5", () => Core.Landing.LandUntargeted(guidance),
                ButtonRole.Normal, true); //Land somewhere
            GuidanceUi.Enable(w, somewhere, () => !LandedOrSplashed);

            KodeWindow.PropertyGrid g = w.Properties(p);
            g.Field("#MechJeb_LandingGuidance_label3", () => Core.Landing?.TouchdownSpeed.Text ?? "", s => //Touchdown speed:
            {
                if (Core.Landing != null) Core.Landing.TouchdownSpeed.Text = s;
            }, 50, "m/s", () => Core.Landing?.TouchdownSpeed.IsParsed ?? true);
            g.Switch("#MechJeb_LandingGuidance_checkbox1", () => Core.Node != null && Core.Node.Autowarp, v => //Auto-warp
            {
                if (Core.Node != null) Core.Node.Autowarp = v;
            });
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
            ModernRows.Switch(g, "#MechJeb_Ascent_checkbox18", () => t.LimiterMinThrottle, v => t.LimiterMinThrottle = v, t.MinThrottle, "%", 50,
                () => t.Limiter == LimitMode.MIN_THROTTLE ? KodeWindow.Tone.Good : KodeWindow.Tone.Normal); //"Keep limited throttle over"

            // while landing: the autopilot's state in a status bar
            Layout running = w.StatusBar(p, () => KodeWindow.Tone.Normal);
            w.Visible(running.gameObject, () => Core.Landing != null && Core.Landing.Enabled);
            string status = Localizer.Format("#MechJeb_LandingGuidance_label6");
            string stepText = Localizer.Format("#MechJeb_LandingGuidance_label7");
            string mode = Localizer.Format("#MechJeb_LandingGuidance_label8");
            w.BoundLabel(running, Cached(() => Core.Landing?.Status ?? "", s => status + s), true); //Status:
            w.BoundLabel(running, Cached(() => Core.Landing?.CurrentStep?.GetType(), ty => stepText + (ty != null ? ty.Name : "N/A")), true); //Step:
            w.BoundLabel(running, Cached(() => (Core.Landing?.DescentSpeedPolicy?.GetType(), Core.Landing != null && Core.Landing.UseAtmosphereToBrake()),
                k => mode + (k.Item1 != null ? k.Item1.Name : "N/A") + " (" + k.Item2 + ")"), true); //Mode
        }

        static Layout ModernStage(KodeWindow.PropertyGrid g, string key, Func<EditableInt> ed) =>
            g.Field(key, () => ed()?.Text ?? "", s =>
            {
                EditableInt e = ed();
                if (e != null) e.Text = s;
            }, 50);

        // MechJebModuleThrustController.LimiterMinThrottleInfoItem: [x] "Keep limited throttle over" [min] %
        void LowerThrottleLimit(KodeWindow w, UIObject parent)
        {
            GeneralItemBuilders.Build(w, parent, "General:ThrustController.LimiterMinThrottleInfoItem", Core.Thrust);
        }
    }
}
