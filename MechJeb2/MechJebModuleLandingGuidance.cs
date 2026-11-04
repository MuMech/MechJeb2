extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleLandingGuidance : DisplayModule
    {
        private const string KSC_PAD_SITE = "KSC Pad";

        // the sites of LandingSites.cfg that are KSC buildings: the game's facility, and the spawn point a vessel stands on
        private static readonly (string Site, string Facility, string SpawnPoint)[] _kscSites =
        {
            (KSC_PAD_SITE, "LaunchPad", "LaunchPad"),
            ("VAB", "VAB", null)
        };

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

            if (LandingSites != null && HighLogic.LoadedSceneIsFlight)
                UseRealKscSites();
        }

        // LandingSites.cfg holds stock positions. The KSC keeps its size on a rescaled planet, so the latitude and longitude
        // of its buildings change with the scale, and KSCSwitcher moves them altogether. The game knows where they are.
        // A site is only updated from a vessel around its body, so call this again where a site is used.
        public static void UseRealKscSites()
        {
            if (LandingSites == null)
                return;

            foreach ((string site, string facilityName, string spawnPointName) in _kscSites)
            {
                PSystemSetup.SpaceCenterFacility facility = PSystemSetup.Instance.GetSpaceCenterFacility(facilityName);
                int index = LandingSites.FindIndex(x => x.Name == site);

                if (facility?.hostBody == null || index < 0 || !TryGetPosition(facility, spawnPointName, out double latitude, out double longitude))
                    continue;

                LandingSites[index] = new LandingSite { Name = site, Latitude = latitude, Longitude = longitude, Body = facility.hostBody };
            }
        }

        private static bool TryGetPosition(PSystemSetup.SpaceCenterFacility facility, string spawnPointName, out double latitude,
            out double longitude)
        {
            PSystemSetup.SpaceCenterFacility.SpawnPoint spawnPoint = spawnPointName != null ? facility.GetSpawnPoint(spawnPointName) : null;

            if (spawnPoint != null && spawnPoint.latlonaltSet)
            {
                latitude  = spawnPoint.latitude;
                longitude = spawnPoint.longitude;
                return true;
            }

            // The game sets the spawn point when a scene goes through the KSC. Loading straight into a flight doesn't, and
            // doesn't build the pad's model either, so take the facility, a few metres from the pad's spawn point. Its
            // float position is only precise near the origin, so only for a vessel around the same body.
            if (facility.facilityTransform == null || facility.hostBody != FlightGlobals.currentMainBody)
            {
                latitude = longitude = 0;
                return false;
            }

            facility.hostBody.GetLatLonAlt(facility.facilityTransform.position, out latitude, out longitude, out _);
            return true;
        }

        internal void MoveByMeter(ref EditableAngle angle, double distance, double alt)
        {
            double angularDelta = distance * UtilMath.Rad2Deg / (alt + MainBody.Radius);
            angle += angularDelta;
        }

        public void SetAndLandTargetKSC()
        {
            UseRealKscSites();
            int index = LandingSites.FindIndex(x => x.Name == KSC_PAD_SITE && x.Body == MainBody);
            if (index < 0)
            {
                ScreenMessages.PostScreenMessage("MechJeb: no KSC Pad landing site on " + MainBody.bodyName);
                return;
            }

            LandingSite ksc = LandingSites[index];
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

                    if (!double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude)) continue;
                    if (!double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude)) continue;

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

                    double latitude = SafeAsin(pos.y) * UtilMath.Rad2Deg;
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

                        if (!double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude)) continue;
                        if (!double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude)) continue;

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
