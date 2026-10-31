extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleLandingGuidance : DisplayModule
    {
        private MechJebModuleLandingPredictions _predictor;
        public static List<LandingSite> LandingSites;

        [UsedImplicitly, Persistent(pass = (int)(Pass.GLOBAL | Pass.LOCAL))]
        public int _landingSiteIdx;

        public struct LandingSite
        {
            public string Name;
            public CelestialBody Body;
            public double Latitude;
            public double Longitude;
        }

        public override void OnStart(PartModule.StartState state)
        {
            _predictor = Core.GetComputerModule<MechJebModuleLandingPredictions>();

            if (LandingSites == null && HighLogic.LoadedSceneIsFlight)
                InitLandingSitesList();
        }

        internal void MoveByMeter(ref EditableAngle angle, double distance, double alt)
        {
            double angularDelta = distance * UtilMath.Rad2Deg / (alt + MainBody.Radius);
            angle += angularDelta;
        }

        public void SetAndLandTargetKSC()
        {
            LandingSite ksc = LandingSites.First(x => x.Name == "KSC Pad");
            Core.Target.SetPositionTarget(MainBody, ksc.Latitude, ksc.Longitude);
            Core.Landing.LandAtPositionTarget(this);
        }

        public void LandSomewhere()
        {
            Core.Landing.StopLanding();
            Core.Landing.LandUntargeted(this);
        }

        [GeneralInfoItem("#MechJeb_LandingPredictions", InfoItem.Category.Misc)] //Landing predictions
        private void DrawGUITogglePredictions() { }

        private void DrawGUIPrediction()
        {
            ReentrySimulation.Result result = _predictor.Result;
            if (result != null)
            {
                switch (result.Outcome)
                {
                    case ReentrySimulation.Outcome.LANDED:
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label9")); //Landing Predictions:
                        GUILayout.Label(Coordinates.ToStringDMS(result.EndPosition.Latitude, result.EndPosition.Longitude) + "\nASL:" +
                            result.EndASL.ToSI() + "m");
                        GUILayout.Label(result.Body.GetExperimentBiomeSafe(result.EndPosition.Latitude, result.EndPosition.Longitude));
                        double error = Vector3d.Distance(
                            MainBody.GetWorldSurfacePosition(result.EndPosition.Latitude, result.EndPosition.Longitude, 0) - MainBody.position,
                            MainBody.GetWorldSurfacePosition(Core.Target.targetLatitude, Core.Target.targetLongitude, 0) - MainBody.position);
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label10") + error.ToSI() + "m"
                            + Localizer.Format("#MechJeb_LandingGuidance_Label11") + result.MaxDragGees.ToString("F1") + "g"
                            + Localizer.Format("#MechJeb_LandingGuidance_Label12") + result.DeltaVExpended.ToString("F1") + "m/s"
                            + Localizer.Format("#MechJeb_LandingGuidance_Label13") + (Vessel.Landed
                                ? "0.0s"
                                : GuiUtils.TimeToDHMS(result.EndUT - Planetarium.GetUniversalTime(),
                                    1))); //Target difference = \nMax drag: \nDelta-v needed: \nTime to land:
                        break;

                    case ReentrySimulation.Outcome.AEROBRAKED:
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label14")); //Predicted orbit after aerobraking:
                        Orbit o = result.AeroBrakeOrbit();
                        if (o.eccentricity > 1)
                            GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label15") +
                                o.eccentricity.ToString("F2")); //Hyperbolic, eccentricity =
                        else GUILayout.Label(o.PeA.ToSI(3) + "m x " + o.ApA.ToSI(3) + "m");
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label16", result.MaxDragGees.ToString("F1")) +
                            Localizer.Format("#MechJeb_LandingGuidance_Label17",
                                GuiUtils.TimeToDHMS(result.AeroBrakeUT - Planetarium.GetUniversalTime(),
                                    1))); //Max drag:<<1>>g  \nExit atmosphere in:
                        break;

                    case ReentrySimulation.Outcome.NO_REENTRY:
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label18_1")
                            + Orbit.PeA.ToSI(3) + "m Pe > " + MainBody.RealMaxAtmosphereAltitude().ToSI(3) + (MainBody.atmosphere
                                ? Localizer.Format("#MechJeb_LandingGuidance_Label18_2")
                                : Localizer.Format(
                                    "#MechJeb_LandingGuidance_Label18_3"))); //"Orbit does not reenter:\n""m atmosphere height""m ground"
                        break;

                    case ReentrySimulation.Outcome.TIMED_OUT:
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label19")); //Reentry simulation timed out.
                        break;
                }
            }
        }

        private void InitLandingSitesList()
        {
            LandingSites = new List<LandingSite>();

            // Import landing sites from users createded .cfg
            foreach (UrlDir.UrlConfig mjConf in GameDatabase.Instance.GetConfigs("MechJeb2Landing"))
            {
                foreach (ConfigNode site in mjConf.config.GetNode("LandingSites").GetNodes("Site"))
                {
                    Print("site " + site);
                    string launchSiteName = site.GetValue("name");
                    string lat = site.GetValue("latitude");
                    string lon = site.GetValue("longitude");

                    if (launchSiteName == null || lat == null || lon == null)
                    {
                        Print("Ignore landing site with null value");
                        continue;
                    }

                    double.TryParse(lat, out double latitude);
                    double.TryParse(lon, out double longitude);

                    string bodyName = site.GetValue("body");
                    CelestialBody body = bodyName != null ? FlightGlobals.Bodies.Find(b => b.bodyName == bodyName) : Planetarium.fetch.Home;

                    if (LandingSites.All(p => p.Name != launchSiteName))
                    {
                        Print("Adding " + launchSiteName);
                        LandingSites.Add(new LandingSite { Name = launchSiteName, Latitude = latitude, Longitude = longitude, Body = body });
                    }
                }
            }

            // Import KSP launch sites
            foreach (LaunchSite site in PSystemSetup.Instance.LaunchSites)
            {
                if (site.spawnPoints.Length > 0)
                {
                    LaunchSite.SpawnPoint point = site.spawnPoints[0];
                    LandingSites.Add(new LandingSite { Name = point.name.Replace("_", " "), Latitude = point.latitude, Longitude = point.longitude, Body = site.Body });
                }
            }

            // Import KerbTown/Kerbal-Konstructs launch site
            foreach (UrlDir.UrlConfig config in GameDatabase.Instance.GetConfigs("STATIC"))
            {
                foreach (ConfigNode instances in config.config.GetNodes("Instances"))
                {
                    string bodyName = instances.GetValue("CelestialBody");
                    string radialPos = instances.GetValue("RadialPosition");
                    string launchSiteName = instances.GetValue("LaunchSiteName");
                    string launchSiteType = instances.GetValue("LaunchSiteType");

                    if (bodyName == null || radialPos == null || launchSiteName == null || launchSiteType == null ||
                        launchSiteType != "VAB")
                    {
                        continue;
                    }

                    Vector3d pos = ConfigNode.ParseVector3D(radialPos).normalized;
                    CelestialBody body = FlightGlobals.Bodies.Find(b => b.bodyName == bodyName);

                    double latitude = Math.Asin(pos.y) * UtilMath.Rad2Deg;
                    double longitude = Math.Atan2(pos.z, pos.x) * UtilMath.Rad2Deg;

                    if (body != null && LandingSites.All(p => p.Name != launchSiteName))
                    {
                        LandingSites.Add(new LandingSite { Name = launchSiteName, Latitude = !double.IsNaN(latitude) ? latitude : 0, Longitude = !double.IsNaN(longitude) ? longitude : 0, Body = body });
                    }
                }
            }

            // Import RSS Launch sites
            UrlDir.UrlConfig rssSites = GameDatabase.Instance.GetConfigs("KSCSWITCHER").FirstOrDefault();
            if (rssSites != null)
            {
                ConfigNode launchSites = rssSites.config.GetNode("LaunchSites");
                if (launchSites != null)
                {
                    foreach (ConfigNode site in launchSites.GetNodes("Site"))
                    {
                        string launchSiteName = site.GetValue("displayName");
                        ConfigNode pqsCity = site.GetNode("PQSCity");
                        if (pqsCity == null)
                        {
                            continue;
                        }

                        string lat = pqsCity.GetValue("latitude");
                        string lon = pqsCity.GetValue("longitude");

                        if (launchSiteName == null || lat == null || lon == null)
                        {
                            continue;
                        }

                        double.TryParse(lat, out double latitude);
                        double.TryParse(lon, out double longitude);

                        if (LandingSites.All(p => p.Name != launchSiteName))
                        {
                            LandingSites.Add(new LandingSite { Name = launchSiteName, Latitude = latitude, Longitude = longitude, Body = Planetarium.fetch.Home });
                        }
                    }
                }
            }

            if (_landingSiteIdx > LandingSites.Count)
            {
                _landingSiteIdx = 0;
            }
        }

        public override string GetName() => Localizer.Format("#MechJeb_LandingGuidance_title"); //Landing Guidance

        public override string IconName() => "Landing Guidance";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();

        public MechJebModuleLandingGuidance(MechJebCore core) : base(core) { }
    }
}
