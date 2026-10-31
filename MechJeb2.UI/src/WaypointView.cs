using System;
using System.Collections.Generic;
using KodeKit;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Pages = MuMech.MechJebModuleWaypointWindow.Pages;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleWaypointWindow ("Waypoints") in KodeKit: three pages (waypoints, settings, routes) built once, shown by the
    /// module's page. The module keeps the state and the actions (both UIs call them); the lists are pooled rows in a ScrollView.
    /// The red map marker of the selected waypoint is a GL draw, so it still goes through an ImguiBlock (it needs an OnGUI repaint).
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleWaypointWindow))]
    public class WaypointView : KodeView
    {
        const float ListHeight = 300;

        readonly MechJebModuleWaypointWindow m;

        public WaypointView(MechJebModuleWaypointWindow m) : base(m) => this.m = m;

        protected override float MinWidth => 500;

        MechJebModuleRoverController Ap => m._ap;
        static bool Alt => GameSettings.MODIFIER_KEY.GetKey();

        // title (the module's GetName: "<mode> Waypoints - <page info>"), updated a few times a second
        TMP_Text titleLabel;
        CharText title;
        float nextTitle;
        Pages titledPage = (Pages)(-1);
        double statDist, statEta;

        // waypoints page
        ScrollView wpScroll;
        Layout wpContent;
        Layout editPanel;
        readonly List<WpRow> rows = new List<WpRow>();
        int panelFor = -2;

        // routes page
        ScrollView routeScroll;
        Layout routeContent;
        readonly List<RouteRow> routeRows = new List<RouteRow>();
        List<MechJebWaypointRoute> shownRoutes = new List<MechJebWaypointRoute>();
        int routesVersion = -1;
        CelestialBody routesBody;
        string routesMode;
        float nextRouteRefresh;

        ScrollView settingsScroll;
        Pages lastPage = (Pages)(-1);

        class WpRow
        {
            public Layout Item;
            public TMP_Text Label;
            public Image Background;
            public CharText Text;
            public bool Shown = true;
            public int Tone = -1; // 0 normal, 1 selected, 2 quicksave
            public bool Current;
            // GetNameWithCoords is only rebuilt when something it shows changed
            public string Name, Vessel, NameWithCoords;
            public double Lat = double.NaN, Lon = double.NaN;
        }

        class RouteRow
        {
            public Layout Item;
            public UIButton Button;
            public UIButton Delete;
            public bool Shown = true;
            public bool DeleteShown = true;
        }

        static Color ToneColor(int tone) => tone == 1 ? Theme.Current.Good : tone == 2 ? Theme.Current.Warn : Theme.Current.Button.TextColor;

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            // the title text is the first child of the title bar; the "?" button goes in front of it afterwards
            titleLabel = w.TitleRow.transform.GetChild(0).GetComponent<TMP_Text>();
            title = new CharText(titleLabel);
            w.TitleButton("?", m.OpenPageHelp);

            Layout waypoints = KodeWindow.Panel(r);
            w.Visible(waypoints.gameObject, () => m._showPage == Pages.WAYPOINTS);
            BuildWaypointsPage(w, waypoints);

            Layout settings = KodeWindow.Panel(r);
            w.Visible(settings.gameObject, () => m._showPage == Pages.SETTINGS);
            BuildSettingsPage(w, settings);

            Layout routes = KodeWindow.Panel(r);
            w.Visible(routes.gameObject, () => m._showPage == Pages.ROUTES);
            BuildRoutesPage(w, routes);
        }

        // ---------------- waypoints page ----------------

        void BuildWaypointsPage(KodeWindow w, UIObject page)
        {
            wpScroll = KodeWindow.ScrollList(page, ListHeight, out wpContent, 2);

            // the selected waypoint's edit panel; it is moved under the selected row
            editPanel = KodeWindow.Panel(wpContent);
            editPanel.Spacing(1);

            Layout limits = KodeWindow.Row(editPanel);
            KodeWindow.Label(limits, "  Radius: ");
            EditFields.Field(w, limits, () => m._tmpRadius, s =>
            {
                m._tmpRadius = s;
                Selected(wp => float.TryParse(s, out wp.Radius));
            }, 50);
            KodeWindow.Button(limits, "A", () => m.RadiusToRest(m.SelIndex));
            KodeWindow.Label(limits, "- Speed: ");
            EditFields.Field(w, limits, () => m._tmpMinSpeed, s =>
            {
                m._tmpMinSpeed = s;
                Selected(wp => float.TryParse(s, out wp.MinSpeed));
            }, 40);
            KodeWindow.Button(limits, "A", () => m.MinSpeedToRest(m.SelIndex));
            KodeWindow.Label(limits, " - ");
            EditFields.Field(w, limits, () => m._tmpMaxSpeed, s =>
            {
                m._tmpMaxSpeed = s;
                Selected(wp => float.TryParse(s, out wp.MaxSpeed));
            }, 40);
            KodeWindow.Button(limits, "A", () => m.MaxSpeedToRest(m.SelIndex));
            KodeWindow.FlexibleSpace(limits);
            UIButton qs = KodeWindow.Button(limits, "QS", () => m.ToggleQuicksave(m.SelIndex, Alt));
            w.ToneText(qs.GetComponentInChildren<TMP_Text>(), () => SelectedWaypoint()?.Quicksave == true ? KodeWindow.Tone.Warn : KodeWindow.Tone.Normal, true);

            Layout coords = KodeWindow.Row(editPanel);
            KodeWindow.Label(coords, "Lat ");
            EditFields.Field(w, coords, () => m._tmpLat, s =>
            {
                m._tmpLat = s;
                Selected(wp => wp.Latitude = MechJebModuleWaypointWindow.ParseCoord(s));
            }, 125);
            KodeWindow.Label(coords, " -  Lon ");
            EditFields.Field(w, coords, () => m._tmpLon, s =>
            {
                m._tmpLon = s;
                Selected(wp => wp.Longitude = MechJebModuleWaypointWindow.ParseCoord(s, true));
            }, 125);
            editPanel.gameObject.SetActive(false);

            Layout buttons = KodeWindow.Row(page);
            RoverUi.DynButton(w, buttons, () => Alt ? "Reverse" : !m._waitingForPick ? "Add Waypoint" : "Abort Adding", () => m.AddOrReverse(Alt), 110);
            RoverUi.DynButton(w, buttons, () => Alt ? "Clear" : "Remove", () => m.RemoveOrClear(Alt), 65);
            RoverUi.DynButton(w, buttons, () => Alt ? "Top" : "Up", () => m.MoveUp(Alt), 57);
            RoverUi.DynButton(w, buttons, () => Alt ? "Bottom" : "Down", () => m.MoveDown(Alt), 57);
            KodeWindow.Button(buttons, "Routes", m.ShowRoutesPage, true);
            KodeWindow.Button(buttons, "Settings", m.ShowSettingsPage, true);

            // the map marker of the selected waypoint is drawn by GL in OnGUI: no GUILayout, no room
            ImguiBlock.DrawOnly(page, () =>
            {
                if (Event.current.type == EventType.Repaint) m.DrawSelectedMarker();
            });
        }

        /// <summary>Runs an action on the selected waypoint, if there is one.</summary>
        MechJebWaypoint SelectedWaypoint()
        {
            List<MechJebWaypoint> wps = Ap?.Waypoints;
            int i = m.SelIndex;
            return wps != null && i >= 0 && i < wps.Count ? wps[i] : null;
        }

        void Selected(Action<MechJebWaypoint> action)
        {
            MechJebWaypoint wp = SelectedWaypoint();
            if (wp != null) action(wp);
        }

        WpRow CreateRow(int index)
        {
            KodeWindow w = Window;
            var row = new WpRow();
            row.Item = wpContent.Add<Layout>("Waypoint");
            row.Item.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(1);
            row.Item.FlexibleLayout(true, false);
            row.Item.Finish();

            UIButton b = KodeWindow.Button(row.Item, "", () => m.WaypointClicked(index, Alt), true);
            row.Label = b.GetComponentInChildren<TMP_Text>();
            row.Background = b.GetComponent<Image>();
            // a fixed-size cell with no layout group, so the distance and ETA changing every frame don't relayout the list
            UnityEngine.Object.DestroyImmediate(b.Content.layoutGroup);
            UnityEngine.Object.DestroyImmediate(b.GetComponent<AnchorLayoutGroup>());
            RectTransform lr = row.Label.rectTransform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(6, 1);
            lr.offsetMax = new Vector2(-6, -1);
            row.Label.alignment = TextAlignmentOptions.TopLeft;
            row.Label.enableWordWrapping = false;
            row.Label.overflowMode = TextOverflowModes.Overflow;
            KodeWindow.Styled(b.gameObject, th => b.PreferredHeight(TextMeasure.Measure("X\nX", th).y + 4));
            row.Text = new CharText(row.Label);

            w.Behaviour.AddRestyle(() => PaintRow(row));
            return row;
        }

        void PaintRow(WpRow row)
        {
            row.Label.color = ToneColor(Math.Max(0, row.Tone));
            // GUI.backgroundColor of the current waypoint's button
            row.Background.color = row.Current ? new Color(0.5f, 1f, 0.5f) : Theme.Current.Button.Tint;
        }

        static string NameWithCoords(WpRow row, MechJebWaypoint wp)
        {
            string vessel = wp.Target is null ? null : wp.Target.vesselName;
            if (row.NameWithCoords == null || row.Lat != wp.Latitude || row.Lon != wp.Longitude || row.Name != wp.Name || row.Vessel != vessel)
            {
                row.Lat = wp.Latitude;
                row.Lon = wp.Longitude;
                row.Name = wp.Name;
                row.Vessel = vessel;
                row.NameWithCoords = wp.GetNameWithCoords();
            }
            return row.NameWithCoords;
        }

        void UpdateWaypointsPage()
        {
            List<MechJebWaypoint> wps = Ap.Waypoints;
            int n = wps.Count;
            if (m.SelIndex >= n) m.SelIndex = -1;
            while (rows.Count < n) rows.Add(CreateRow(rows.Count));

            double eta = 0, dist = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                WpRow row = rows[i];
                bool show = i < n;
                if (show != row.Shown)
                {
                    row.Shown = show;
                    row.Item.gameObject.SetActive(show);
                }
                if (!show) continue;

                MechJebWaypoint wp = wps[i];
                m.AccumulateStats(i, wp, ref eta, ref dist, out double maxSpeed, out float minSpeed);

                row.Text.Clear().Append('[').AppendInt(i + 1).Append("] - ").Append(NameWithCoords(row, wp)).Append(" - R: ").Append(wp.Radius, 1)
                    .Append(" m\n       S: ").Append(minSpeed, 0).Append(" ~ ").Append(maxSpeed, 0).Append(" - D: ").AppendSI(dist, -1)
                    .Append("m - ETA: ").AppendDHMS(eta, 0, GuiUtils.HoursPerDay, GuiUtils.DaysPerYear);
                row.Text.Commit();

                int tone = i == m.SelIndex ? 1 : wp.Quicksave ? 2 : 0;
                bool current = i == Ap.WaypointIndex;
                if (tone != row.Tone || current != row.Current)
                {
                    row.Tone = tone;
                    row.Current = current;
                    PaintRow(row);
                }
            }
            statDist = dist;
            statEta = eta;

            // the edit panel sits under the selected row
            int sel = m.SelIndex;
            if (sel != panelFor)
            {
                panelFor = sel;
                if (sel >= 0 && sel < rows.Count)
                {
                    editPanel.transform.SetParent(rows[sel].Item.transform, false);
                    editPanel.transform.SetAsLastSibling();
                    editPanel.gameObject.SetActive(true);
                }
                else
                {
                    editPanel.gameObject.SetActive(false);
                }
            }

            // scroll to the current waypoint when it changes and nothing is selected
            if (m.SelIndex == -1 && Ap.WaypointIndex > -1 && m._lastIndex != Ap.WaypointIndex && Ap.WaypointIndex < n) ScrollTo(Ap.WaypointIndex);
            m._lastIndex = Ap.WaypointIndex;
        }

        void ScrollTo(int index)
        {
            RectTransform content = wpScroll.Content.rectTransform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float top = -rows[index].Item.rectTransform.anchoredPosition.y;
            float max = Mathf.Max(0, content.rect.height - wpScroll.Viewport.rectTransform.rect.height);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(top - 160, 0, max));
            wpScroll.ScrollRect.StopMovement();
        }

        // ---------------- settings page ----------------

        void BuildSettingsPage(KodeWindow w, UIObject page)
        {
            settingsScroll = KodeWindow.ScrollList(page, ListHeight, out Layout content, 2);
            MechJebModuleCustomWindowEditor ed = Core.GetComputerModule<MechJebModuleCustomWindowEditor>();

            // the settings pages: navigation, so tabs in the modern layout
            if (KodeWindow.ModernLayout) w.Segmented(content, m._settingPages, () => m._settingPageIndex, i => m._settingPageIndex = i, ChoiceStyle.Tabs);
            else w.SelectionGrid(content, m._settingPages, () => m._settingPageIndex, i => m._settingPageIndex = i);

            // rover: PID and traction
            Layout rover = KodeWindow.Row(content);
            w.Visible(rover.gameObject, () => m._settingPageIndex == 0);
            Layout left = KodeWindow.Panel(rover);
            RoverUi.EditableRow(w, left, ed, "Editable:RoverController.hPIDp");
            RoverUi.EditableRow(w, left, ed, "Editable:RoverController.hPIDi");
            RoverUi.EditableRow(w, left, ed, "Editable:RoverController.hPIDd");
            RoverUi.EditableRow(w, left, ed, "Editable:RoverController.terrainLookAhead");
            RoverUi.EditableRow(w, left, ed, "Editable:RoverController.tractionLimit");
            RoverUi.ToggleRow(w, left, ed, "Toggle:RoverController.LimitAcceleration", () => Ap != null && Ap.LimitAcceleration);
            Layout right = KodeWindow.Panel(rover);
            RoverUi.EditableRow(w, right, ed, "Editable:RoverController.sPIDp");
            RoverUi.EditableRow(w, right, ed, "Editable:RoverController.sPIDi");
            RoverUi.EditableRow(w, right, ed, "Editable:RoverController.sPIDd");
            RoverUi.EditableRow(w, right, ed, "Editable:RoverController.turnSpeed");
            RoverUi.ValueRow(w, right, ed, "Value:RoverController.traction", () => Ap?.traction ?? 0, "100 %");

            // waypoints: map route heights per body
            Layout maps = KodeWindow.Row(content);
            w.Visible(maps.gameObject, () => m._settingPageIndex == 1);
            Layout mapsLeft = KodeWindow.Panel(maps);
            foreach (string body in new[] { "Moho", "Eve", "Gilly", "Kerbin", "Mun", "Minmus", "Duna", "Ike" })
                RoverUi.EditableRow(w, mapsLeft, ed, "Editable:WaypointWindow." + body + "Mapdist");
            Layout mapsRight = KodeWindow.Panel(maps);
            foreach (string body in new[] { "Dres", "Jool", "Laythe", "Vall", "Tylo", "Bop", "Pol", "Eeloo" })
                RoverUi.EditableRow(w, mapsRight, ed, "Editable:WaypointWindow." + body + "Mapdist");

            Layout buttons = KodeWindow.Row(page);
            KodeWindow.Button(buttons, "Waypoints", m.ShowWaypointsPage, true);
            KodeWindow.Button(buttons, "Routes", m.ShowRoutesPage, true);
            KodeWindow.Button(buttons, "Help", m.ToggleHelpWindow, true);
        }

        // ---------------- routes page ----------------

        void BuildRoutesPage(KodeWindow w, UIObject page)
        {
            routeScroll = KodeWindow.ScrollList(page, ListHeight, out routeContent, 2);

            Layout buttons = KodeWindow.Row(page);
            EditFields.Field(w, buttons, () => m._saveName, s => m._saveName = s, 150);
            KodeWindow.Button(buttons, "Save", m.SaveCurrentRoute).PreferredWidth(50);
            RoverUi.DynButton(w, buttons, () => Alt ? "Add" : "Load", () => m.LoadSelectedRoute(Alt), 50);
            KodeWindow.Button(buttons, "Waypoints", m.ShowWaypointsPage, true);
            KodeWindow.Button(buttons, "Settings", m.ShowSettingsPage, true);
        }

        RouteRow CreateRouteRow(int index)
        {
            KodeWindow w = Window;
            var row = new RouteRow();
            row.Item = KodeWindow.Row(routeContent);
            row.Button = RoverUi.StateButton(w, row.Item, "", () => m.RouteClicked(index), () => m._saveIndex == index);
            row.Delete = KodeWindow.Button(row.Item, "Delete", () =>
            {
                if (index < shownRoutes.Count) m.DeleteRoute(shownRoutes[index].Name);
            });
            row.Delete.PreferredWidth(70);
            return row;
        }

        void UpdateRoutesPage()
        {
            CelestialBody body = m.Vessel.mainBody;
            string mode = m.Mode.ToString();
            bool rebuilt = routesVersion != MechJebModuleWaypointWindow.RoutesVersion || routesBody != body || routesMode != mode;
            // the routes' stats follow their waypoints (a target's position moves): refresh the texts now and then
            if (rebuilt || Time.unscaledTime >= nextRouteRefresh)
            {
                if (rebuilt)
                {
                    routesVersion = MechJebModuleWaypointWindow.RoutesVersion;
                    routesBody = body;
                    routesMode = mode;
                    shownRoutes = m.BodyRoutes();
                }
                nextRouteRefresh = Time.unscaledTime + 1f;

                while (routeRows.Count < shownRoutes.Count) routeRows.Add(CreateRouteRow(routeRows.Count));
                for (int i = 0; i < routeRows.Count; i++)
                {
                    RouteRow row = routeRows[i];
                    bool show = i < shownRoutes.Count;
                    if (show != row.Shown)
                    {
                        row.Shown = show;
                        row.Item.gameObject.SetActive(show);
                    }
                    if (show) row.Button.Text(shownRoutes[i].Name + " - " + shownRoutes[i].Stats);
                }
            }

            // the Delete button belongs to the selected route only
            for (int i = 0; i < routeRows.Count; i++)
            {
                RouteRow row = routeRows[i];
                bool del = row.Shown && i == m._saveIndex;
                if (del == row.DeleteShown) continue;
                row.DeleteShown = del;
                row.Delete.gameObject.SetActive(del);
            }
        }

        // ---------------- per frame ----------------

        protected override void BeforeSync()
        {
            if (Ap == null) return;

            // the click on the terrain or the map that "Add Waypoint" waits for
            m.ProcessWaypointPick(Alt);

            Pages page = m._showPage;
            if (page != lastPage)
            {
                // IMGUI resets the shared scroll position when the page changes
                lastPage = page;
                ScrollView sv = page == Pages.WAYPOINTS ? wpScroll : page == Pages.SETTINGS ? settingsScroll : routeScroll;
                sv.Content.rectTransform.anchoredPosition = Vector2.zero;
            }

            switch (page)
            {
                case Pages.WAYPOINTS:
                    UpdateWaypointsPage();
                    break;
                case Pages.SETTINGS:
                    m.SettingsPageTick();
                    break;
                case Pages.ROUTES:
                    UpdateRoutesPage();
                    break;
            }

            UpdateTitle(page);
        }

        void UpdateTitle(Pages page)
        {
            if (page == titledPage && Time.unscaledTime < nextTitle) return;
            titledPage = page;
            nextTitle = Time.unscaledTime + 0.25f;

            title.Clear().Append(m.Mode == MechJebModuleWaypointWindow.WaypointMode.ROVER ? "ROVER" : "PLANE").Append(" Waypoints");
            switch (page)
            {
                case Pages.WAYPOINTS:
                    if (Ap.Waypoints.Count > 0)
                        title.Append(" - Distance: ").AppendSI(statDist, -1).Append("m - ETA: ").AppendDHMS(statEta, 0, GuiUtils.HoursPerDay, GuiUtils.DaysPerYear);
                    break;
                case Pages.SETTINGS:
                    title.Append(" - Settings");
                    break;
                case Pages.ROUTES:
                    title.Append(" - Routes for ").Append(m.Vessel.mainBody.bodyName);
                    break;
            }
            title.Commit();
        }

        MechJebCore Core => m.Core;
    }
}
