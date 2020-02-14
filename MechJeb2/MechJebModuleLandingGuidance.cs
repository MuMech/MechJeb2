using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

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

        private void moveByMeter(ref EditableAngle angle, double distance, double Alt)
        {
            double angularDelta = distance * UtilMath.Rad2Deg / (Alt + mainBody.Radius);
            angle += angularDelta;
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (core.target.PositionTargetExists)
            {
                var ASL = core.vessel.mainBody.TerrainAltitude(core.target.targetLatitude, core.target.targetLongitude);
                GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label1"));//Target coordinates:

                GUILayout.BeginHorizontal();
                core.target.targetLatitude.DrawEditGUI(EditableAngle.Direction.NS);
                if (GUILayout.Button("▲"))
                {
                    moveByMeter(ref core.target.targetLatitude, 10, ASL);
                }
                GUILayout.Label("10m");
                if (GUILayout.Button("▼"))
                {
                    moveByMeter(ref core.target.targetLatitude, -10, ASL);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                core.target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);
                if (GUILayout.Button("◄"))
                {
                    moveByMeter(ref core.target.targetLongitude, -10, ASL);
                }
                GUILayout.Label("10m");
                if (GUILayout.Button("►"))
                {
                    moveByMeter(ref core.target.targetLongitude, 10, ASL);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("ASL: " + MuUtils.ToSI(ASL, -1, 4) + "m");
                GUILayout.Label(core.target.targetBody.GetExperimentBiomeSafe(core.target.targetLatitude, core.target.targetLongitude));
                GUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button1")))//Enter target coordinates
                {
                    core.target.SetPositionTarget(mainBody, core.target.targetLatitude, core.target.targetLongitude);
                }
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button2"))) core.target.PickPositionTargetOnMap();//Pick target on map

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
                GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label2"));//Autopilot:

                predictor.maxOrbits = core.landing.enabled ? 0.5 : 4;
                predictor.noSkipToFreefall = !core.landing.enabled;

                if (core.landing.enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button3"))) core.landing.StopLanding();//Abort autoland
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (!core.target.PositionTargetExists || vessel.LandedOrSplashed) GUI.enabled = false;
                    if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button4"))) core.landing.LandAtPositionTarget(this);//Land at target
                    GUI.enabled = !vessel.LandedOrSplashed;
                    if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button5"))) core.landing.LandUntargeted(this);//Land somewhere
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_LandingGuidance_label3"), core.landing.touchdownSpeed, "m/s", 35);//Touchdown speed:

                if (core.landing != null) core.node.autowarp = GUILayout.Toggle(core.node.autowarp, Localizer.Format("#MechJeb_LandingGuidance_checkbox1"));//Auto-warp

                core.landing.deployGears = GUILayout.Toggle(core.landing.deployGears, Localizer.Format("#MechJeb_LandingGuidance_checkbox2"));//Deploy Landing Gear
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_LandingGuidance_label4"), core.landing.limitGearsStage, "", 35);//"Stage Limit:"
                core.landing.deployChutes = GUILayout.Toggle(core.landing.deployChutes, Localizer.Format("#MechJeb_LandingGuidance_checkbox3"));//Deploy Parachutes
                predictor.deployChutes = core.landing.deployChutes;
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_LandingGuidance_label5"), core.landing.limitChutesStage, "", 35);//Stage Limit:
                predictor.limitChutesStage = core.landing.limitChutesStage;
                core.landing.rcsAdjustment = GUILayout.Toggle(core.landing.rcsAdjustment, Localizer.Format("#MechJeb_LandingGuidance_checkbox4"));//Use RCS for small adjustment

                if (core.landing.enabled)
                {
                    GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label6") + core.landing.status);//Status: 
                    GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label7") + (core.landing.CurrentStep != null ? core.landing.CurrentStep.GetType().Name : "N/A"));//Step: 
                    GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label8") + (core.landing.descentSpeedPolicy != null ? core.landing.descentSpeedPolicy.GetType().Name : "N/A") + " (" + core.landing.UseAtmosphereToBrake().ToString() + ")");//Mode 
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
            var ksc = landingSites.First(x => x.name == "KSC Pad");
            core.target.SetPositionTarget(mainBody, ksc.latitude, ksc.longitude);
            core.landing.LandAtPositionTarget(this);
        }

        public void LandSomewhere()
        {
            core.landing.StopLanding();
            core.landing.LandUntargeted(this);
        }


        [GeneralInfoItem("#MechJeb_LandingPredictions", InfoItem.Category.Misc)]//Landing predictions
        void DrawGUITogglePredictions()
        {
            GUILayout.BeginVertical();

            bool active = GUILayout.Toggle(predictor.enabled, Localizer.Format("#MechJeb_LandingGuidance_checkbox5"));//Show landing predictions
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
                predictor.makeAerobrakeNodes = GUILayout.Toggle(predictor.makeAerobrakeNodes, Localizer.Format("#MechJeb_LandingGuidance_checkbox6"));//"Show aerobrake nodes"
                predictor.showTrajectory = GUILayout.Toggle(predictor.showTrajectory, Localizer.Format("#MechJeb_LandingGuidance_checkbox7"));//Show trajectory
                predictor.worldTrajectory = GUILayout.Toggle(predictor.worldTrajectory, Localizer.Format("#MechJeb_LandingGuidance_checkbox8"));//World trajectory
                predictor.camTrajectory = GUILayout.Toggle(predictor.camTrajectory, Localizer.Format("#MechJeb_LandingGuidance_checkbox9"));//Camera trajectory (WIP)
                DrawGUIPrediction();
            }

            GUILayout.EndVertical();
        }
        
        void DrawGUIPrediction()
        {
            ReentrySimulation.Result result = predictor.Result;
            if (result != null)
            {
                switch (result.outcome)
                {
                    case ReentrySimulation.Outcome.LANDED:
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label9"));//Landing Predictions:
                        GUILayout.Label(Coordinates.ToStringDMS(result.endPosition.latitude, result.endPosition.longitude) + "\nASL:" + MuUtils.ToSI(result.endASL,-1, 4) + "m");
                        GUILayout.Label(result.body.GetExperimentBiomeSafe(result.endPosition.latitude, result.endPosition.longitude));
                        double error = Vector3d.Distance(mainBody.GetWorldSurfacePosition(result.endPosition.latitude, result.endPosition.longitude, 0) - mainBody.position,
                                                         mainBody.GetWorldSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0) - mainBody.position);
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label10") + MuUtils.ToSI(error, 0) + "m"
                                       + Localizer.Format("#MechJeb_LandingGuidance_Label11") + result.maxDragGees.ToString("F1") +"g"
                                       +Localizer.Format("#MechJeb_LandingGuidance_Label12") + result.deltaVExpended.ToString("F1") + "m/s"
                                       +Localizer.Format("#MechJeb_LandingGuidance_Label13") + (vessel.Landed ? "0.0s" : GuiUtils.TimeToDHMS(result.endUT - Planetarium.GetUniversalTime(), 1)));//Target difference = \nMax drag: \nDelta-v needed: \nTime to land: 
                        break;

                    case ReentrySimulation.Outcome.AEROBRAKED:
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label14"));//Predicted orbit after aerobraking:
                        Orbit o = result.AeroBrakeOrbit();
                        if (o.eccentricity > 1) GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label15") + o.eccentricity.ToString("F2"));//Hyperbolic, eccentricity = 
                        else GUILayout.Label(MuUtils.ToSI(o.PeA, 3) + "m x " + MuUtils.ToSI(o.ApA, 3) + "m");
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label16", result.maxDragGees.ToString("F1"))+Localizer.Format("#MechJeb_LandingGuidance_Label17", GuiUtils.TimeToDHMS(result.aeroBrakeUT - Planetarium.GetUniversalTime(), 1)));//Max drag:<<1>>g  \nExit atmosphere in:
                        break;

                    case ReentrySimulation.Outcome.NO_REENTRY:
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label18_1")
                                      + MuUtils.ToSI(orbit.PeA, 3) + "m Pe > " + MuUtils.ToSI(mainBody.RealMaxAtmosphereAltitude(), 3) + (mainBody.atmosphere ? Localizer.Format("#MechJeb_LandingGuidance_Label18_2") : Localizer.Format("#MechJeb_LandingGuidance_Label18_3")));//"Orbit does not reenter:\n""m atmosphere height""m ground"
                        break;

                    case ReentrySimulation.Outcome.TIMED_OUT:
                        GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_Label19"));//Reentry simulation timed out.
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
                        print("Ignore langing site with null value");
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

                    double latitude = Math.Asin(pos.y) * UtilMath.Rad2Deg;
                    double longitude = Math.Atan2(pos.z, pos.x) * UtilMath.Rad2Deg;

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
            }

            if (landingSiteIdx > landingSites.Count)
            {
                landingSiteIdx = 0;
            }
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_LandingGuidance_title");//Landing Guidance
        }

        public override bool IsSpaceCenterUpgradeUnlocked()
        {
            return vessel.patchedConicsUnlocked();
        }

        public MechJebModuleLandingGuidance(MechJebCore core) : base(core) { }
    }
}
