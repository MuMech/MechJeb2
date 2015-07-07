using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleLandingGuidance : DisplayModule
    {
        public MechJebModuleLandingPredictions predictor;
        public static List<LandingSite> landingSites;

        [Persistent(pass = (int)(Pass.Global|Pass.Local))]
        public int landingSiteIdx = 0;

        public struct LandingSite
        {
            public string name;
            public CelestialBody body;
            public double latitude;
            public double longitude;
        }

        public override void OnStart(PartModule.StartState state)
        {
            predictor = core.GetComputerModule<MechJebModuleLandingPredictions>();

            if (landingSites == null && HighLogic.LoadedSceneIsFlight)
                InitLandingSitesList();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(150) };
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (core.target.PositionTargetExists)
            {
                GUILayout.Label("Target coordinates:");

                core.target.targetLatitude.DrawEditGUI(EditableAngle.Direction.NS);
                core.target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);

                GUILayout.Label(ScienceUtil.GetExperimentBiome(core.target.targetBody, core.target.targetLatitude, core.target.targetLongitude));
                // Display the ASL of the taget location
                GUILayout.Label("Target ASL: " + MuUtils.ToSI(core.vessel.mainBody.TerrainAltitude(core.target.targetLatitude, core.target.targetLongitude), -1, 4) + "m");
            }
            else
            {
                if (GUILayout.Button("Enter target coordinates"))
                {
                    core.target.SetPositionTarget(mainBody, core.target.targetLatitude, core.target.targetLongitude);
                }
            }

            if (GUILayout.Button("Pick target on map")) core.target.PickPositionTargetOnMap();

            List<LandingSite> availableLandingSites = landingSites.Where(p => p.body == mainBody).ToList();
            if (availableLandingSites.Any())
            {
                GUILayout.BeginHorizontal();
                landingSiteIdx = GuiUtils.ComboBox.Box(landingSiteIdx, availableLandingSites.Select(p => p.name).ToArray(), this);
                if (GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
                {
                    core.target.SetPositionTarget(mainBody, availableLandingSites[landingSiteIdx].latitude, availableLandingSites[landingSiteIdx].longitude);
                }
                GUILayout.EndHorizontal();
            }

            DrawGUITogglePredictions();

            if (core.landing != null)
            {
                GUILayout.Label("Autopilot:");

                if (core.landing.enabled)
                {
                    if (GUILayout.Button("Abort autoland")) core.landing.StopLanding();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (!core.target.PositionTargetExists) GUI.enabled = false;
                    if (GUILayout.Button("Land at target")) core.landing.LandAtPositionTarget(this);
                    GUI.enabled = true;
                    if (GUILayout.Button("Land somewhere")) core.landing.LandUntargeted(this);
                    GUILayout.EndHorizontal();
                }

                GuiUtils.SimpleTextBox("Touchdown speed:", core.landing.touchdownSpeed, "m/s", 35);

                if (core.landing != null) core.node.autowarp = GUILayout.Toggle(core.node.autowarp, "Auto-warp");

                core.landing.deployGears = GUILayout.Toggle(core.landing.deployGears, "Deploy Landing Gear");
                GuiUtils.SimpleTextBox("Stage Limit:", core.landing.limitGearsStage, "", 35);
                core.landing.deployChutes = GUILayout.Toggle(core.landing.deployChutes, "Deploy Parachutes");
                predictor.deployChutes = core.landing.deployChutes;
                GuiUtils.SimpleTextBox("Stage Limit:", core.landing.limitChutesStage, "", 35);
                predictor.limitChutesStage = core.landing.limitChutesStage;
                core.landing.rcsAdjustment = GUILayout.Toggle(core.landing.rcsAdjustment, "Use RCS for small adjustment");

                if (core.landing.enabled)
                {
                    GUILayout.Label("Status: " + core.landing.status);
                    GUILayout.Label("Mode " + core.landing.descentSpeedPolicy.GetType().Name + "(" + core.landing.UseAtmosphereToBrake().ToString() + ")");
                    GUILayout.Label("DecEndAlt: " + core.landing.DecelerationEndAltitude().ToString("F2"));
                    var dragLength = mainBody.DragLength(core.landing.LandingAltitude, core.landing.vesselAverageDrag, vesselState.mass);
                    GUILayout.Label("Drag Leng: " + ( dragLength != Double.MaxValue ? dragLength.ToString("F2") : "infinite"));

                    string parachuteInfo = core.landing.ParachuteControlInfo;
                    if (null != parachuteInfo)
                    {
                        GUILayout.Label(parachuteInfo);
                    }
                }
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        [GeneralInfoItem("Landing predictions", InfoItem.Category.Misc)]
        void DrawGUITogglePredictions()
        {
            GUILayout.BeginVertical();

            bool active = GUILayout.Toggle(predictor.enabled, "Show landing predictions");
            if (predictor.enabled != active)
            {
                if (active)
                {
                    predictor.users.Add(this);
                }
                else
                {
                    predictor.users.Remove(this);
                }
            }

            if (predictor.enabled)
            {
                predictor.makeAerobrakeNodes = GUILayout.Toggle(predictor.makeAerobrakeNodes, "Show aerobrake nodes");
                predictor.showTrajectory = GUILayout.Toggle(predictor.showTrajectory, "Show trajectory");
                predictor.worldTrajectory = GUILayout.Toggle(predictor.worldTrajectory, "World trajectory");
                DrawGUIPrediction();
            }

            GUILayout.EndVertical();
        }
        
        void DrawGUIPrediction()
        {
            ReentrySimulation.Result result = predictor.GetResult();
            if (result != null)
            {
                switch (result.outcome)
                {
                    case ReentrySimulation.Outcome.LANDED:
                        GUILayout.Label("Landing Predictions:");
                        GUILayout.Label(Coordinates.ToStringDMS(result.endPosition.latitude, result.endPosition.longitude) + "\nASL:" + MuUtils.ToSI(result.endASL,-1, 4) + "m");
                        GUILayout.Label(ScienceUtil.GetExperimentBiome(result.body, result.endPosition.latitude, result.endPosition.longitude));
                        double error = Vector3d.Distance(mainBody.GetRelSurfacePosition(result.endPosition.latitude, result.endPosition.longitude, 0),
                                                         mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0));
                        GUILayout.Label("Target difference = " + MuUtils.ToSI(error, 0) + "m"
                                       +"\nMax drag: " + result.maxDragGees.ToString("F1") +"g"
                                       +"\nDelta-v needed: " + result.deltaVExpended.ToString("F1") + "m/s"
                                       +"\nTime to land: " + GuiUtils.TimeToDHMS(result.endUT - Planetarium.GetUniversalTime(), 1));                        
                        break;

                    case ReentrySimulation.Outcome.AEROBRAKED:
                        GUILayout.Label("Predicted orbit after aerobraking:");
                        Orbit o = result.EndOrbit();
                        if (o.eccentricity > 1) GUILayout.Label("Hyperbolic, eccentricity = " + o.eccentricity.ToString("F2"));
                        else GUILayout.Label(MuUtils.ToSI(o.PeA, 3) + "m x " + MuUtils.ToSI(o.ApA, 3) + "m");
                        GUILayout.Label("Max drag: " + result.maxDragGees.ToString("F1") + "g"
                                       +"\nExit atmosphere in: " + GuiUtils.TimeToDHMS(result.endUT - Planetarium.GetUniversalTime(), 1));                        
                        break;

                    case ReentrySimulation.Outcome.NO_REENTRY:
                        GUILayout.Label("Orbit does not reenter:\n"
                                      + MuUtils.ToSI(orbit.PeA, 3) + "m Pe > " + MuUtils.ToSI(mainBody.RealMaxAtmosphereAltitude(), 3) + "m atmosphere height");
                        break;

                    case ReentrySimulation.Outcome.TIMED_OUT:
                        GUILayout.Label("Reentry simulation timed out.");
                        break;
                }
            }
        }

        private void InitLandingSitesList()
        {
            landingSites = new List<LandingSite>();

            // Import landing sites from users createded .cfg
            foreach (var mjConf in GameDatabase.Instance.GetConfigs("MechJeb2Landing"))
            {
                foreach (ConfigNode site in mjConf.config.GetNode("LandingSites").GetNodes("Site"))
                {
                    print("site " + site);
                    string launchSiteName = site.GetValue("name");
                    string lat = site.GetValue("latitude");
                    string lon = site.GetValue("longitude");

                    if (launchSiteName == null || lat == null || lon == null)
                    {
                        print("un null");
                        continue;
                    }

                    double latitude, longitude;
                    double.TryParse(lat, out latitude);
                    double.TryParse(lon, out longitude);

                    string bodyName = site.GetValue("body");
                    CelestialBody body = bodyName != null ? FlightGlobals.Bodies.Find(b => b.bodyName == bodyName) : Planetarium.fetch.Home;

                    if (!landingSites.Any(p => p.name == launchSiteName))
                    {
                        print("Adding " + launchSiteName);
                        landingSites.Add(new LandingSite()
                        {
                            name = launchSiteName,
                            latitude = latitude,
                            longitude = longitude,
                            body = body
                        });
                    }
                }
            }

            // Create a default config file in MJ dir for those ?
            if (!landingSites.Any(p => p.name == "KSC Pad"))
                landingSites.Add(new LandingSite()
                {
                    name = "KSC Pad",
                    latitude = -0.09694444,
                    longitude = -74.5575,
                    body = Planetarium.fetch.Home
                });

            if (!landingSites.Any(p => p.name == "VAB"))
                landingSites.Add(new LandingSite()
                {
                    name = "VAB",
                    latitude = -0.09694444,
                    longitude = -74.617,
                    body = Planetarium.fetch.Home
                });

            // Import KerbTown/Kerbal-Konstructs launch site
            foreach (var config in GameDatabase.Instance.GetConfigs("STATIC"))
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

                    double latitude = Math.Asin(pos.y) * 180 / Math.PI;
                    double longitude = Math.Atan2(pos.z, pos.x) * 180 / Math.PI;

                    if (body != null && !landingSites.Any(p => p.name == launchSiteName))
                    {
                        landingSites.Add(new LandingSite()
                        {
                            name = launchSiteName,
                            latitude = !double.IsNaN(latitude) ? latitude : 0,
                            longitude = !double.IsNaN(longitude) ? longitude : 0,
                            body = body
                        });
                    }
                }
            }

            // Import RSS Launch sites
            UrlDir.UrlConfig rssSites = GameDatabase.Instance.GetConfigs("REALSOLARSYSTEM").FirstOrDefault();
            if (rssSites != null)
            {
                foreach (ConfigNode site in rssSites.config.GetNode("LaunchSites").GetNodes("Site"))
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

                    double latitude, longitude;
                    double.TryParse(lat, out latitude);
                    double.TryParse(lon, out longitude);

                    if (!landingSites.Any(p => p.name == launchSiteName))
                    {
                        landingSites.Add(new LandingSite()
                        {
                            name = launchSiteName,
                            latitude = latitude,
                            longitude = longitude,
                            body = Planetarium.fetch.Home
                        });
                    }
                }
            }
            if (landingSiteIdx > landingSites.Count)
            {
                landingSiteIdx = 0;
            }
        }

        public override string GetName()
        {
            return "Landing Guidance";
        }

        public override bool IsSpaceCenterUpgradeUnlocked()
        {
            return vessel.patchedConicsUnlocked();
        }

        public MechJebModuleLandingGuidance(MechJebCore core) : base(core) { }
    }
}
