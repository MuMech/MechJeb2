using System;
using KodeKit;
using KodeUI;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleLandingGuidance.DrawGUITogglePredictions (the "Landing predictions" general info item) in KodeKit:
    /// "Show landing predictions" with its options and one block per prediction outcome (DrawGUIPrediction).
    /// Used by the Landing Guidance window and by custom windows showing the item.
    /// </summary>
    internal sealed class LandingPredictionsBlock
    {
        readonly MechJebModuleLandingGuidance guidance;
        MechJebModuleLandingPredictions predictor;

        MechJebCore Core => guidance.Core;
        MechJebModuleLandingPredictions Predictor => predictor ??= Core.GetComputerModule<MechJebModuleLandingPredictions>();
        CelestialBody Body => Core.vessel != null ? Core.vessel.mainBody : null;
        ReentrySimulation.Result Result => Predictor?.Result;

        bool PredictionIs(ReentrySimulation.Outcome outcome)
        {
            ReentrySimulation.Result r = Result;
            return Predictor != null && Predictor.Enabled && r != null && r.Outcome == outcome;
        }

        static double Clamp180(double x)
        {
            x %= 360;
            if (x < 0) x += 360;
            return x > 180 ? x - 360 : x;
        }

        double resultBiomeLat = double.NaN, resultBiomeLon = double.NaN;
        CelestialBody resultBiomeBody;
        string resultBiome = "";

        string ResultBiome(ReentrySimulation.Result res)
        {
            double lat = res.EndPosition.Latitude, lon = res.EndPosition.Longitude;
            if (lat != resultBiomeLat || lon != resultBiomeLon || res.Body != resultBiomeBody)
            {
                resultBiomeLat = lat;
                resultBiomeLon = lon;
                resultBiomeBody = res.Body;
                resultBiome = res.Body != null ? res.Body.GetExperimentBiomeSafe(lat, lon) ?? "" : "";
            }

            return resultBiome;
        }

        public LandingPredictionsBlock(KodeWindow w, UIObject parent, MechJebModuleLandingGuidance guidance)
        {
            this.guidance = guidance;
            Build(w, parent);
        }

        void Build(KodeWindow w, UIObject parent)
        {
            Layout p = KodeWindow.Panel(parent);
            w.Toggle(p, "#MechJeb_LandingGuidance_checkbox5", () => Predictor != null && Predictor.Enabled, v => //Show landing predictions
            {
                if (v) Predictor.Users.Add(guidance);
                else Predictor.Users.Remove(guidance);
            });

            Layout on = KodeWindow.Panel(p);
            w.Visible(on.gameObject, () => Predictor != null && Predictor.Enabled);
            w.Toggle(on, "#MechJeb_LandingGuidance_checkbox6", () => Predictor.makeAerobrakeNodes, v => Predictor.makeAerobrakeNodes = v); //Show aerobrake nodes
            w.Toggle(on, "#MechJeb_LandingGuidance_checkbox7", () => Predictor.showTrajectory, v => Predictor.showTrajectory = v); //Show trajectory
            w.Toggle(on, "#MechJeb_LandingGuidance_checkbox8", () => Predictor.worldTrajectory, v => Predictor.worldTrajectory = v); //World trajectory
            w.Toggle(on, "#MechJeb_LandingGuidance_checkbox9", () => Predictor.camTrajectory, v => Predictor.camTrajectory = v); //Camera trajectory (WIP)

            // DrawGUIPrediction, one block per outcome
            Layout landed = KodeWindow.Panel(on);
            w.Visible(landed.gameObject, () => PredictionIs(ReentrySimulation.Outcome.LANDED));
            Func<bool> isLanded = () => PredictionIs(ReentrySimulation.Outcome.LANDED);
            KodeWindow.Label(landed, "#MechJeb_LandingGuidance_label9"); //Landing Predictions:
            GuidanceUi.Live(w, landed, "00° 00' 00\" N, 000° 00' 00\" W\nASL:-1.234 km", false, ct =>
            {
                ReentrySimulation.Result res = Result;
                double lat = res.EndPosition.Latitude;
                double lon = Clamp180(res.EndPosition.Longitude);
                ct.AppendDMS(lat).Append(lat > 0 ? " N" : " S").Append(", ").AppendDMS(lon).Append(lon > 0 ? " E" : " W")
                    .Append("\nASL:").AppendSI(res.EndASL).Append('m');
            }, isLanded);
            GuidanceUi.Live(w, landed, "Shores of the Northern Sea", false, ct => ct.Append(ResultBiome(Result)), isLanded);

            var p10 = new GuidanceUi.Parts("#MechJeb_LandingGuidance_Label10");
            var p11 = new GuidanceUi.Parts("#MechJeb_LandingGuidance_Label11");
            var p12 = new GuidanceUi.Parts("#MechJeb_LandingGuidance_Label12");
            var p13 = new GuidanceUi.Parts("#MechJeb_LandingGuidance_Label13");
            GuidanceUi.Live(w, landed, p10.Pre + "1234 km" + p11.Pre + "12.3g" + p12.Pre + "1234.5m/s" + p13.Pre + "123d 12h 12m 12.3s", false, ct =>
            {
                ReentrySimulation.Result res = Result;
                CelestialBody body = Body;
                double error = Vector3d.Distance(
                    body.GetWorldSurfacePosition(res.EndPosition.Latitude, res.EndPosition.Longitude, 0) - body.position,
                    body.GetWorldSurfacePosition(Core.Target.targetLatitude, Core.Target.targetLongitude, 0) - body.position);
                ct.Append(p10.Pre).AppendSI(error).Append('m')
                    .Append(p11.Pre).Append(res.MaxDragGees, 1).Append('g')
                    .Append(p12.Pre).Append(res.DeltaVExpended, 1).Append("m/s")
                    .Append(p13.Pre);
                if (Core.vessel.Landed) ct.Append("0.0s");
                else ct.AppendDHMS(res.EndUT - Planetarium.GetUniversalTime(), 1, GuidanceUi.HoursPerDay, GuidanceUi.DaysPerYear);
            }, isLanded);

            Layout aero = KodeWindow.Panel(on);
            w.Visible(aero.gameObject, () => PredictionIs(ReentrySimulation.Outcome.AEROBRAKED));
            Func<bool> isAero = () => PredictionIs(ReentrySimulation.Outcome.AEROBRAKED);
            KodeWindow.Label(aero, "#MechJeb_LandingGuidance_Label14"); //Predicted orbit after aerobraking:
            var p15 = new GuidanceUi.Parts("#MechJeb_LandingGuidance_Label15");
            GuidanceUi.Live(w, aero, p15.Pre + "1.23", false, ct =>
            {
                // the orbit is rebuilt from the result each frame, as the IMGUI window does
                Orbit o = Result.AeroBrakeOrbit();
                if (o.eccentricity > 1) ct.Append(p15.Pre).Append(o.eccentricity, 2);
                else ct.AppendSI(o.PeA, 3).Append("m x ").AppendSI(o.ApA, 3).Append('m');
            }, isAero);
            var p16 = new GuidanceUi.Parts("#MechJeb_LandingGuidance_Label16");
            var p17 = new GuidanceUi.Parts("#MechJeb_LandingGuidance_Label17");
            GuidanceUi.Live(w, aero, p16.Pre + "12.3" + p16.Post + p17.Pre + "123d 12h 12m 12.3s" + p17.Post, false, ct =>
            {
                ReentrySimulation.Result res = Result;
                ct.Append(p16.Pre).Append(res.MaxDragGees, 1).Append(p16.Post)
                    .Append(p17.Pre).AppendDHMS(res.AeroBrakeUT - Planetarium.GetUniversalTime(), 1, GuidanceUi.HoursPerDay, GuidanceUi.DaysPerYear)
                    .Append(p17.Post);
            }, isAero);

            Layout noReentry = KodeWindow.Panel(on);
            w.Visible(noReentry.gameObject, () => PredictionIs(ReentrySimulation.Outcome.NO_REENTRY));
            var p18a = new GuidanceUi.Parts("#MechJeb_LandingGuidance_Label18_1");
            var p18b = new GuidanceUi.Parts("#MechJeb_LandingGuidance_Label18_2");
            var p18c = new GuidanceUi.Parts("#MechJeb_LandingGuidance_Label18_3");
            GuidanceUi.Live(w, noReentry, p18a.Pre + "-1.23 Mm Pe > 123 km" + p18b.Pre, false, ct =>
            {
                CelestialBody body = Body;
                ct.Append(p18a.Pre).AppendSI(guidance.Orbit.PeA, 3).Append("m Pe > ").AppendSI(body.RealMaxAtmosphereAltitude(), 3)
                    .Append(body.atmosphere ? p18b.Pre : p18c.Pre);
            }, () => PredictionIs(ReentrySimulation.Outcome.NO_REENTRY));

            UIText timedOut = KodeWindow.Label(on, "#MechJeb_LandingGuidance_Label19"); //Reentry simulation timed out.
            w.Visible(timedOut.gameObject, () => PredictionIs(ReentrySimulation.Outcome.TIMED_OUT));
        }
    }
}
