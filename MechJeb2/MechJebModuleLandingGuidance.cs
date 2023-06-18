using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleLandingGuidance : DisplayModule
    {
        private       MechJebModuleLandingPredictions _predictor;
        public static List<LandingSite>               LandingSites;

        [Persistent(pass = (int)(Pass.Global | Pass.Local))]
        private int _landingSiteIdx;

        public struct LandingSite
        {
            public string        Name;
            public CelestialBody Body;
            public double        Latitude;
            public double        Longitude;
        }

        public override void OnStart(PartModule.StartState state)
        {
            _predictor = core.GetComputerModule<MechJebModuleLandingPredictions>();

            if (LandingSites == null && HighLogic.LoadedSceneIsFlight)
                InitLandingSitesList();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new[] { GUILayout.Width(200), GUILayout.Height(150) };
        }

        private void MoveByMeter(ref EditableAngle angle, double distance, double alt)
        {
            double angularDelta = distance * UtilMath.Rad2Deg / (alt + mainBody.Radius);
            angle += angularDelta;
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (core.target.PositionTargetExists)
            {
                double asl = core.vessel.mainBody.TerrainAltitude(core.target.targetLatitude, core.target.targetLongitude);
                GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label1")); //Target coordinates:

                GUILayout.BeginHorizontal();
                core.target.targetLatitude.DrawEditGUI(EditableAngle.Direction.NS);
                if (GUILayout.Button("▲"))
                {
                    MoveByMeter(ref core.target.targetLatitude, 10, asl);
                }

                GUILayout.Label("10m");
                if (GUILayout.Button("▼"))
                {
                    MoveByMeter(ref core.target.targetLatitude, -10, asl);
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                core.target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);
                if (GUILayout.Button("◄"))
                {
                    MoveByMeter(ref core.target.targetLongitude, -10, asl);
                }

                GUILayout.Label("10m");
                if (GUILayout.Button("►"))
                {
                    MoveByMeter(ref core.target.targetLongitude, 10, asl);
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("ASL: " + asl.ToSI() + "m");
                GUILayout.Label(core.target.targetBody.GetExperimentBiomeSafe(core.target.targetLatitude, core.target.targetLongitude));
                GUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button1"))) //Enter target coordinates
                {
                    core.target.SetPositionTarget(mainBody, core.target.targetLatitude, core.target.targetLongitude);
                }
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button2"))) core.target.PickPositionTargetOnMap(); //Pick target on map

            var availableLandingSites = LandingSites.Where(p => p.Body == mainBody).ToList();
            if (availableLandingSites.Any())
            {
                GUILayout.BeginHorizontal();
                _landingSiteIdx = GuiUtils.ComboBox.Box(_landingSiteIdx, availableLandingSites.Select(p => p.Name).ToArray(), this);
                if (GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
                {
                    core.target.SetPositionTarget(mainBody, availableLandingSites[_landingSiteIdx].Latitude,
                        availableLandingSites[_landingSiteIdx].Longitude);
                }

                GUILayout.EndHorizontal();
            }

            DrawGUITogglePredictions();

            if (core.landing != null)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label2")); //Autopilot:

                _predictor.maxOrbits        = core.landing.enabled ? 0.5 : 4;
                _predictor.noSkipToFreefall = !core.landing.enabled;

                if (core.landing.enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button3"))) core.landing.StopLanding(); //Abort autoland
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (!core.target.PositionTargetExists || vessel.LandedOrSplashed) GUI.enabled = false;
                    if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button4")))
                        core.landing.LandAtPositionTarget(this); //Land at target
                    GUI.enabled = !vessel.LandedOrSplashed;
                    if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button5"))) core.landing.LandUntargeted(this); //Land somewhere
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_LandingGuidance_label3"), core.landing.TouchdownSpeed, "m/s",
                    35); //Touchdown speed:

                if (core.landing != null)
                    core.node.autowarp = GUILayout.Toggle(core.node.autowarp, Localizer.Format("#MechJeb_LandingGuidance_checkbox1")); //Auto-warp

                core.landing.DeployGears =
                    GUILayout.Toggle(core.landing.DeployGears, Localizer.Format("#MechJeb_LandingGuidance_checkbox2")); //Deploy Landing Gear
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_LandingGuidance_label4"), core.landing.LimitGearsStage, "", 35); //"Stage Limit:"
                core.landing.DeployChutes =
                    GUILayout.Toggle(core.landing.DeployChutes, Localizer.Format("#MechJeb_LandingGuidance_checkbox3")); //Deploy Parachutes
                _predictor.deployChutes = core.landing.DeployChutes;
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_LandingGuidance_label5"), core.landing.LimitChutesStage, "", 35); //Stage Limit:
                _predictor.limitChutesStage = core.landing.LimitChutesStage;
                core.landing.RCSAdjustment =
                    GUILayout.Toggle(core.landing.RCSAdjustment,
                        Localizer.Format("#MechJeb_LandingGuidance_checkbox4")); //Use RCS for small adjustment

                if (core.landing.enabled)
                {
                    GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label6") + core.landing.Status); //Status:
                    GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label7") +
                                    (core.landing.CurrentStep != null ? core.landing.CurrentStep.GetType().Name : "N/A")); //Step:
                    GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label8") +
                                    (core.landing.DescentSpeedPolicy != null ? core.landing.DescentSpeedPolicy.GetType().Name : "N/A") + " (" +
                                    core.landing.UseAtmosphereToBrake() + ")"); //Mode
                    //GUILayout.Label("DecEndAlt: " + core.landing.DecelerationEndAltitude().ToString("F2"));
                    //var dragLength = mainBody.DragLength(core.landing.LandingAltitude, core.landing.vesselAverageDrag, vesselState.mass);
                    //GUILayout.Label("Drag Length: " + ( dragLength < double.MaxValue ? dragLength.ToString("F2") : "infinite"));
                    //
                    //string parachuteInfo = core.landing.ParachuteControlInfo;
                    //if (null != parachuteInfo)
                    //{
                    //    GUILayout.Label(parachuteInfo);
                    //}
                }
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public void SetAndLandTargetKSC()
        {
            LandingSite ksc = LandingSites.First(x => x.Name == "KSC Pad");
            core.target.SetPositionTarget(mainBody, ksc.Latitude, ksc.Longitude);
            core.landing.LandAtPositionTarget(this);
        }

        public void LandSomewhere()
        {
            core.landing.StopLanding();
            core.landing.LandUntargeted(this);
        }

        [GeneralInfoItem("#MechJeb_LandingPredictions", InfoItem.Category.Misc)] //Landing predictions
        private void DrawGUITogglePredictions()
        {
            GUILayout.BeginVertical();

            bool active = GUILayout.Toggle(_predictor.enabled, Localizer.Format("#MechJeb_LandingGuidance_checkbox5")); //Show landing predictions
            if (_predictor.enabled != active)
            {
                if (active)
                {
                    _predictor.users.Add(this);
                }
                else
                {
                    _predictor.users.Remove(this);
                }
            }

            if (_predictor.enabled)
            {
                _predictor.makeAerobrakeNodes =
                    GUILayout.Toggle(_predictor.makeAerobrakeNodes, Localizer.Format("#MechJeb_LandingGuidance_checkbox6")); //"Show aerobrake nodes"
                _predictor.showTrajectory =
                    GUILayout.Toggle(_predictor.showTrajectory, Localizer.Format("#MechJeb_LandingGuidance_checkbox7")); //Show trajectory
                _predictor.worldTrajectory =
                    GUILayout.Toggle(_predictor.worldTrajectory, Localizer.Format("#MechJeb_LandingGuidance_checkbox8")); //World trajectory
                _predictor.camTrajectory =
                    GUILayout.Toggle(_predictor.camTrajectory, Localizer.Format("#MechJeb_LandingGuidance_checkbox9")); //Camera trajectory (WIP)
                DrawGUIPrediction();
            }

            GUILayout.EndVertical();
        }

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
                            mainBody.GetWorldSurfacePosition(result.EndPosition.Latitude, result.EndPosition.Longitude, 0) - mainBody.position,
                            mainBody.GetWorldSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0) - mainBody.position);
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label10") + error.ToSI() + "m"
                                        + Localizer.Format("#MechJeb_LandingGuidance_Label11") + result.MaxDragGees.ToString("F1") + "g"
                                        + Localizer.Format("#MechJeb_LandingGuidance_Label12") + result.DeltaVExpended.ToString("F1") + "m/s"
                                        + Localizer.Format("#MechJeb_LandingGuidance_Label13") + (vessel.Landed
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
                                        + orbit.PeA.ToSI(3) + "m Pe > " + mainBody.RealMaxAtmosphereAltitude().ToSI(3) + (mainBody.atmosphere
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
                    print("site " + site);
                    string launchSiteName = site.GetValue("name");
                    string lat = site.GetValue("latitude");
                    string lon = site.GetValue("longitude");

                    if (launchSiteName == null || lat == null || lon == null)
                    {
                        print("Ignore landing site with null value");
                        continue;
                    }

                    double.TryParse(lat, out double latitude);
                    double.TryParse(lon, out double longitude);

                    string bodyName = site.GetValue("body");
                    CelestialBody body = bodyName != null ? FlightGlobals.Bodies.Find(b => b.bodyName == bodyName) : Planetarium.fetch.Home;

                    if (LandingSites.All(p => p.Name != launchSiteName))
                    {
                        print("Adding " + launchSiteName);
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
                    LandingSites.Add(new LandingSite
                    {
                        Name = point.name.Replace("_", " "), Latitude = point.latitude, Longitude = point.longitude, Body = site.Body
                    });
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
                        LandingSites.Add(new LandingSite
                        {
                            Name      = launchSiteName,
                            Latitude  = !double.IsNaN(latitude) ? latitude : 0,
                            Longitude = !double.IsNaN(longitude) ? longitude : 0,
                            Body      = body
                        });
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
                            LandingSites.Add(new LandingSite
                            {
                                Name = launchSiteName, Latitude = latitude, Longitude = longitude, Body = Planetarium.fetch.Home
                            });
                        }
                    }
                }
            }

            if (_landingSiteIdx > LandingSites.Count)
            {
                _landingSiteIdx = 0;
            }
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_LandingGuidance_title"); //Landing Guidance
        }

        public override string IconName()
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
