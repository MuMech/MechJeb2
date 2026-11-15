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
    /// MechJebModuleLandingGuidance ("Landing Guidance") in KodeKit, including the
    /// landing prediction block of DrawGUITogglePredictions (LandingPredictionsBlock, shared with the custom windows).
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleLandingGuidance))]
    public class LandingGuidanceView : KodeView
    {
        readonly MechJebModuleLandingGuidance guidance;

        public LandingGuidanceView(MechJebModuleLandingGuidance guidance) : base(guidance) => this.guidance = guidance;

        protected override float MinWidth => 200;

        MechJebCore Core => guidance.Core;
        CelestialBody Body => Core.vessel != null ? Core.vessel.mainBody : null;
        bool LandedOrSplashed => Core.vessel != null && Core.vessel.LandedOrSplashed;
        // a point on this body to land at: a vessel in flight or a target on another body has none
        bool HasPositionTarget => Core.Target != null && Core.Target.SurfaceTargetExists(Body);
        // names of the landing sites of the current body, cached until the body or the list changes, and where they are in
        // the list: the KSC sites move once the game knows its buildings, so the position is read when a site is set
        string[] siteNames = new string[0];
        CelestialBody siteBody;
        int siteCount = -1;
        readonly List<int> siteIndexes = new List<int>();

        string[] SiteNames()
        {
            CelestialBody body = Body;
            List<MechJebModuleLandingGuidance.LandingSite> all = MechJebModuleLandingGuidance.LandingSites;
            int count = all?.Count ?? 0;
            if (body == siteBody && count == siteCount) return siteNames;

            siteBody = body;
            siteCount = count;
            siteIndexes.Clear();
            for (int i = 0; i < count; i++)
                if (all[i].Body == body)
                    siteIndexes.Add(i);
            siteNames = new string[siteIndexes.Count];
            for (int i = 0; i < siteIndexes.Count; i++) siteNames[i] = all[siteIndexes[i]].Name;
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

        // the terrain height runs the body's PQS: asked again only when the target or the body changes
        CelestialBody aslBody;
        double aslLat = double.NaN, aslLon = double.NaN, asl;

        double TargetAsl()
        {
            CelestialBody body = Body;
            double lat = Core.Target.targetLatitude, lon = Core.Target.targetLongitude;
            if (body == aslBody && lat.Equals(aslLat) && lon.Equals(aslLon)) return asl;
            aslBody = body;
            aslLat = lat;
            aslLon = lon;
            asl = body.TerrainAltitude(lat, lon);
            return asl;
        }

        void Move(bool latitude, double meters)
        {
            double alt = TargetAsl();
            if (latitude) guidance.MoveByMeter(ref Core.Target.targetLatitude, meters, alt, false);
            else guidance.MoveByMeter(ref Core.Target.targetLongitude, meters, alt, true);
        }

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
            // Clear target, except while the landing autopilot flies to it
            UIButton clear = KodeWindow.Button(r, "#MechJeb_LandingGuidance_buttonClear", () => Core.Target.Unset(), flexible); //Clear target
            w.Visible(clear.gameObject, () => HasPositionTarget);
            GuidanceUi.Enable(w, clear, () => Core.Landing == null || !Core.Landing.Enabled);
        }

        /// <summary>The landing sites list and its "Set" button (shown when there are sites).</summary>
        void SiteRow(KodeWindow w, UIObject r)
        {
            Layout siteRow = KodeWindow.Row(r);
            new ComboBox(w, siteRow, () => guidance._landingSiteIdx, i => guidance._landingSiteIdx = i, SiteNames);
            KodeWindow.Button(siteRow, "Set", () =>
            {
                SiteNames();
                if (siteIndexes.Count == 0) return;
                MechJebModuleLandingGuidance.UseRealKscSites();
                MechJebModuleLandingGuidance.LandingSite site =
                    MechJebModuleLandingGuidance.LandingSites[siteIndexes[Mathf.Clamp(guidance._landingSiteIdx, 0, siteIndexes.Count - 1)]];
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
            // UseAtmosphereToBrake walks every chute's drag cubes: only while landing, when this panel shows
            w.BoundLabel(running, Memo.Of(() => Core.Landing != null && Core.Landing.Enabled
                    ? (Core.Landing.DescentSpeedPolicy?.GetType(), Core.Landing.UseAtmosphereToBrake())
                    : (null, false),
                k => mode + (k.Item1 != null ? k.Item1.Name : "N/A") + " (" + k.Item2 + ")"), true); //Mode
        }

        // D-M-S fields with the hemisphere as a choice, one nudge pad, the options in a property grid
        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout target = KodeWindow.Panel(r);
            w.Visible(target.gameObject, () => HasPositionTarget);
            KodeWindow.Header(target, "#MechJeb_LandingGuidance_label1"); //Target coordinates:
            Layout coords = KodeWindow.Row(target);
            Layout fields = KodeWindow.Panel(coords);
            AngleFields(w, fields, "Lat", () => Core.Target.targetLatitude, EditableAngle.Direction.NS);
            AngleFields(w, fields, "Lon", () => Core.Target.targetLongitude, EditableAngle.Direction.EW);

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
            Autopilot(w, autopilot);
        }

        /// <summary>Lat [d] ° [m] ' [s] " [N|S]: the hemisphere is a two-way choice, not a button that flips.</summary>
        static void AngleFields(KodeWindow w, UIObject parent, string label, Func<EditableAngle> angle, EditableAngle.Direction direction)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, label);
            OpUi.AngleRow(w, row, angle, direction, false);
        }

        void Autopilot(KodeWindow w, UIObject p)
        {
            KodeWindow.Header(p, "#MechJeb_LandingGuidance_label2"); //Autopilot:

            LandButtons(w, p, true);

            KodeWindow.PropertyGrid g = w.Properties(p);
            EditUi.Grid(g, "#MechJeb_LandingGuidance_label3", () => Core.Landing?.TouchdownSpeed, "m/s", 50); //Touchdown speed:
            g.Switch("#MechJeb_LandingGuidance_checkbox1", () => Core.Landing != null && Core.Landing.Autowarp, v => //Auto-warp
            {
                if (Core.Landing != null) Core.Landing.Autowarp = v;
            });
            g.Switch("#MechJeb_LandingGuidance_checkbox2", () => Core.Landing != null && Core.Landing.DeployGears, v => //Deploy Landing Gear
            {
                if (Core.Landing != null) Core.Landing.DeployGears = v;
            });
            Layout gears = StageField(g, "#MechJeb_LandingGuidance_label4", () => Core.Landing?.LimitGearsStage); //Stage Limit:
            g.Dim(gears.gameObject, null, () => Core.Landing == null || !Core.Landing.DeployGears);
            g.Switch("#MechJeb_LandingGuidance_checkbox3", () => Core.Landing != null && Core.Landing.DeployChutes, v => //Deploy Parachutes
            {
                if (Core.Landing != null) Core.Landing.DeployChutes = v;
            });
            Layout chutes = StageField(g, "#MechJeb_LandingGuidance_label5", () => Core.Landing?.LimitChutesStage); //Stage Limit:
            g.Dim(chutes.gameObject, null, () => Core.Landing == null || !Core.Landing.DeployChutes);
            g.Switch("#MechJeb_LandingGuidance_jettison", () => Core.Landing != null && Core.Landing.JettisonBeforeEntry, v => //Jettison stage before entry
            {
                if (Core.Landing != null) Core.Landing.JettisonBeforeEntry = v;
            });
            g.Switch("#MechJeb_LandingGuidance_checkbox4", () => Core.Landing != null && Core.Landing.RCSAdjustment, v => //Use RCS for small adjustment
            {
                if (Core.Landing != null) Core.Landing.RCSAdjustment = v;
            });
            MechJebModuleThrustController t = Core.Thrust;
            // the landing's own minimum throttle, live while the landing is engaged
            if (Core.Landing != null)
                OptionRows.MinThrottle(g, t, Core.Landing.ThrustLimits, () => ReferenceEquals(t.LimitsOverrideUser, Core.Landing), 50); //"Keep limited throttle over"

            // while landing: the autopilot's state in a status bar
            Layout running = w.StatusBar(p, () => KodeWindow.Tone.Normal);
            w.Visible(running.gameObject, () => Core.Landing != null && Core.Landing.Enabled);
            RunningLabels(w, running);
        }

        static Layout StageField(KodeWindow.PropertyGrid g, string key, Func<EditableInt> ed) => EditUi.Grid(g, key, ed, null, 50);

    }
}
