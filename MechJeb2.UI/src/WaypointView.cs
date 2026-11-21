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
    /// module's page. The module keeps the state and the actions (both UIs call them); the waypoints are a virtual list (only
    /// the rows in view are built and updated), the routes pooled rows in a ScrollView.
    /// The module draws the red map marker of the selected waypoint itself, through MechJebCore's post-draw queue.
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
        VirtualList<WpRow> wpList;
        Layout editPanel;
        float editPanelHeight = -1; // measured when first shown and after a restyle
        int panelFor = -2;
        int panelVersion = -1;

        // routes page
        ScrollView routeScroll;
        Layout routeContent;
        readonly List<RouteRow> routeRows = new List<RouteRow>();
        List<MechJebWaypointRoute> shownRoutes = new List<MechJebWaypointRoute>();
        int routesVersion = -1;
        CelestialBody routesBody;
        MechJebModuleWaypointWindow.WaypointMode? routesMode;
        float nextRouteRefresh;

        ScrollView settingsScroll;
        Pages lastPage = (Pages)(-1);

        public class WpRow
        {
            public Layout Item;
            public TMP_Text Label;
            public Image Background;
            public CharText Text;
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
            UIText empty = KodeWindow.Paragraph(page, "#MechJeb_NewUI_NoWaypoints"); //No waypoints: Add Waypoint, then click the map.
            KodeWindow.ToneText(empty, KodeWindow.Tone.Dim);
            w.Visible(empty.gameObject, () => Ap == null || Ap.Waypoints.Count == 0);
            wpList = new VirtualList<WpRow>(w, page, ListHeight, 0, CreateRow, row => row.Item.rectTransform, BindRow, 2);

            // the selected waypoint's edit panel: under the selected row, which the list makes taller by the panel's height
            editPanel = KodeWindow.Panel(wpList.Content);
            editPanel.Spacing(1);
            RectTransform pr = editPanel.rectTransform;
            pr.anchorMin = new Vector2(0, 1);
            pr.anchorMax = new Vector2(1, 1);
            pr.pivot = new Vector2(0.5f, 1);
            KodeWindow.Styled(editPanel.gameObject, th => editPanelHeight = -1);

            Layout limits = KodeWindow.Row(editPanel);
            KodeWindow.Label(limits, "  Radius: ");
            EditFields.Field(w, limits, () => m._tmpRadius, s =>
            {
                m._tmpRadius = s;
                Selected(wp => { if (MechJebModuleWaypointWindow.TryParseNonNegative(s, out float v)) wp.Radius = v; });
            }, 50);
            KodeWindow.Button(limits, "A", () => m.RadiusToRest(m.SelIndex));
            KodeWindow.Label(limits, "- Speed: ");
            EditFields.Field(w, limits, () => m._tmpMinSpeed, s =>
            {
                m._tmpMinSpeed = s;
                Selected(wp => { if (MechJebModuleWaypointWindow.TryParseNonNegative(s, out float v)) wp.MinSpeed = v; });
            }, 40, label: "#MechJeb_NewUI_Label_MinSpeed"); //Min speed
            KodeWindow.Button(limits, "A", () => m.MinSpeedToRest(m.SelIndex));
            KodeWindow.Label(limits, " - ");
            EditFields.Field(w, limits, () => m._tmpMaxSpeed, s =>
            {
                m._tmpMaxSpeed = s;
                Selected(wp => { if (MechJebModuleWaypointWindow.TryParseNonNegative(s, out float v)) wp.MaxSpeed = v; });
            }, 40, label: "#MechJeb_NewUI_Label_MaxSpeed"); //Max speed
            KodeWindow.Button(limits, "A", () => m.MaxSpeedToRest(m.SelIndex));
            KodeWindow.FlexibleSpace(limits);
            UIButton qs = KodeWindow.Button(limits, "QS", () => m.ToggleQuicksave(m.SelIndex, Alt));
            w.ToneText(qs.GetComponentInChildren<TMP_Text>(), () => SelectedWaypoint()?.Quicksave == true ? KodeWindow.Tone.Warn : KodeWindow.Tone.Normal, true);

            Layout coords = KodeWindow.Row(editPanel);
            KodeWindow.Label(coords, "Lat ");
            EditFields.Field(w, coords, () => m._tmpLat, s =>
            {
                m._tmpLat = s;
                Selected(wp => { if (MechJebModuleWaypointWindow.TryParseCoord(s, false, out double v)) wp.Latitude = v; });
            }, 125, label: "#MechJeb_Latitude"); //Latitude
            KodeWindow.Label(coords, " -  Lon ");
            EditFields.Field(w, coords, () => m._tmpLon, s =>
            {
                m._tmpLon = s;
                Selected(wp => { if (MechJebModuleWaypointWindow.TryParseCoord(s, true, out double v)) wp.Longitude = v; });
            }, 125, label: "#MechJeb_Longitude"); //Longitude
            editPanel.gameObject.SetActive(false);

            Layout buttons = KodeWindow.Row(page);
            RoverUi.DynButton(w, buttons, () => Alt ? "Reverse" : !m._waitingForPick ? "Add Waypoint" : "Abort Adding", () => m.AddOrReverse(Alt), 110);
            KodeWindow.Button(buttons, "Remove", () => m.RemoveOrClear(false)).PreferredWidth(65);
            KodeWindow.Button(buttons, "Clear", () => m.RemoveOrClear(true)).PreferredWidth(50);
            RoverUi.DynButton(w, buttons, () => Alt ? "Top" : "Up", () => m.MoveUp(Alt), 57);
            RoverUi.DynButton(w, buttons, () => Alt ? "Bottom" : "Down", () => m.MoveDown(Alt), 57);
            KodeWindow.Button(buttons, "Routes", m.ShowRoutesPage, true);
            KodeWindow.Button(buttons, "Settings", m.ShowSettingsPage, true);
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

        /// <summary>The waypoints' virtual list (for tests and the agent bridge).</summary>
        public VirtualList<WpRow> WaypointList => wpList;

        WpRow CreateRow(UIObject content)
        {
            KodeWindow w = Window;
            var row = new WpRow();
            row.Item = content.Add<Layout>("Waypoint");
            row.Item.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(1);
            row.Item.FlexibleLayout(true, false);
            row.Item.Finish();

            UIButton b = KodeWindow.Button(row.Item, "", () =>
            {
                int index = wpList.IndexOf(row);
                if (index >= 0) m.WaypointClicked(index, Alt);
            }, true);
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

        // each waypoint's speeds and running distance and ETA from the last per-frame pass, so a row bound between two passes
        // (a scroll, in LateUpdate) shows its waypoint at once
        readonly List<double> rowDist = new List<double>(), rowEta = new List<double>(), rowMaxSpeed = new List<double>();
        readonly List<float> rowMinSpeed = new List<float>();

        // a row shows another waypoint: forget what it showed and write it now
        void BindRow(WpRow row, int index)
        {
            row.NameWithCoords = null;
            row.Tone = -1;
            List<MechJebWaypoint> wps = Ap?.Waypoints;
            if (wps == null || index >= wps.Count || index >= rowDist.Count) return;
            WriteRow(row, index, wps[index]);
        }

        void WriteRow(WpRow row, int i, MechJebWaypoint wp)
        {
            row.Text.Clear().Append('[').AppendInt(i + 1).Append("] - ").Append(NameWithCoords(row, wp)).Append(" - R: ").Append(wp.Radius, 1)
                .Append(" m\n       S: ").Append(rowMinSpeed[i], 0).Append(" ~ ").Append(rowMaxSpeed[i], 0).Append(" - D: ").AppendSI(rowDist[i], -1)
                .Append("m - ETA: ").AppendDHMS(rowEta[i], 0);
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
            wpList.SetCount(n);
            UpdateEditPanel();

            // the totals need every waypoint; only the rows in view are written
            while (rowDist.Count < n)
            {
                rowDist.Add(0);
                rowEta.Add(0);
                rowMaxSpeed.Add(0);
                rowMinSpeed.Add(0);
            }
            int first = wpList.Model.RenderFirst, last = wpList.Model.RenderLast;
            double eta = 0, dist = 0;
            for (int i = 0; i < n; i++)
            {
                MechJebWaypoint wp = wps[i];
                m.AccumulateStats(i, wp, ref eta, ref dist, out double maxSpeed, out float minSpeed);
                rowDist[i] = dist;
                rowEta[i] = eta;
                rowMaxSpeed[i] = maxSpeed;
                rowMinSpeed[i] = minSpeed;
                if (i < first || i > last) continue;
                WpRow row = wpList.RowAt(i);
                if (row != null) WriteRow(row, i, wp);
            }
            statDist = dist;
            statEta = eta;

            // scroll to the current waypoint when it changes and nothing is selected: its top 160 px down the list
            if (m.SelIndex == -1 && Ap.WaypointIndex > -1 && m._lastIndex != Ap.WaypointIndex && Ap.WaypointIndex < n)
                wpList.SetOffset(wpList.Model.RowTop(Ap.WaypointIndex) - 160);
            m._lastIndex = Ap.WaypointIndex;
        }

        /// <summary>The edit panel sits under the selected row, 1 px below its button; the list makes that row taller by the panel.</summary>
        void UpdateEditPanel()
        {
            int sel = m.SelIndex;
            if (sel != panelFor)
            {
                panelFor = sel;
                panelVersion = -1;
                editPanel.gameObject.SetActive(sel >= 0);
            }
            if (sel < 0)
            {
                wpList.SetExpanded(-1, 0);
                return;
            }
            if (editPanelHeight < 0)
            {
                RectTransform pr = editPanel.rectTransform;
                LayoutRebuilder.ForceRebuildLayoutImmediate(pr);
                editPanelHeight = LayoutUtility.GetPreferredHeight(pr);
                pr.sizeDelta = new Vector2(0, editPanelHeight);
                panelVersion = -1;
            }
            wpList.SetExpanded(sel, editPanelHeight + 1);
            if (panelVersion == wpList.Model.GeometryVersion) return;
            panelVersion = wpList.Model.GeometryVersion;
            editPanel.rectTransform.anchoredPosition = new Vector2(0, -(wpList.Model.RowTop(sel) + wpList.Model.RowHeight + 1));
            editPanel.transform.SetAsLastSibling();
        }

        // ---------------- settings page ----------------

        void BuildSettingsPage(KodeWindow w, UIObject page)
        {
            settingsScroll = KodeWindow.ScrollList(page, ListHeight, out Layout content, 2);
            MechJebModuleCustomWindowEditor ed = Core.GetComputerModule<MechJebModuleCustomWindowEditor>();

            // rover: PID and traction
            Layout rover = KodeWindow.Row(content);
            Layout left = KodeWindow.Panel(rover);
            RoverUi.EditableRow(w, left, ed, "Editable:RoverController.hPIDp");
            RoverUi.EditableRow(w, left, ed, "Editable:RoverController.hPIDi");
            RoverUi.EditableRow(w, left, ed, "Editable:RoverController.hPIDd");
            RoverUi.EditableRow(w, left, ed, "Editable:RoverController.terrainLookAhead");
            RoverUi.EditableRow(w, left, ed, "Editable:RoverController.tractionLimit");
            Layout right = KodeWindow.Panel(rover);
            RoverUi.EditableRow(w, right, ed, "Editable:RoverController.sPIDp");
            RoverUi.EditableRow(w, right, ed, "Editable:RoverController.sPIDi");
            RoverUi.EditableRow(w, right, ed, "Editable:RoverController.sPIDd");
            RoverUi.EditableRow(w, right, ed, "Editable:RoverController.turnSpeed");
            RoverUi.ValueRow(w, right, ed, "Value:RoverController.traction", () => Ap?.traction ?? 0, "100 %");

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
            EditFields.Field(w, buttons, () => m._saveName, s => m._saveName = s, 150, label: "#MechJeb_NewUI_Label_RouteName", kind: FieldKind.Text); //Route name
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
            MechJebModuleWaypointWindow.WaypointMode mode = m.Mode;
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
                if (page == Pages.WAYPOINTS) wpList.SetOffset(0);
                else (page == Pages.SETTINGS ? settingsScroll : routeScroll).Content.rectTransform.anchoredPosition = Vector2.zero;
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

            title.Clear().Append(m.Mode == MechJebModuleWaypointWindow.WaypointMode.ROVER ? "Rover" : "Plane").Append(" waypoints");
            switch (page)
            {
                case Pages.WAYPOINTS:
                    if (Ap.Waypoints.Count > 0)
                        title.Append(" - Distance: ").AppendSI(statDist, -1).Append("m - ETA: ").AppendDHMS(statEta, 0);
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
