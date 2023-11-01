using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace MuMech
{
    public interface IEditable
    {
        string Text { get; set; }
    }

    //An EditableDouble stores a double value and a text string. The user can edit the string.
    //Whenever the text is edited, it is parsed and the parsed value is stored in val. As a
    //convenience, a multiplier can be specified so that the stored value is actually
    //(multiplier * parsed value). If the parsing fails, the parsed flag is set to false and
    //the stored value is unchanged. There are implicit conversions between EditableDouble and
    //double so that if you are not doing text input you can treat an EditableDouble like a double.
    public class EditableDoubleMult : IEditable
    {
        [UsedImplicitly]
        [Persistent]
        public double ValConfig;

        public virtual double Val
        {
            get => ValConfig;
            set
            {
                ValConfig  = value;
                TextConfig = (ValConfig / _multiplier).ToString();
            }
        }

        private readonly double _multiplier;

        protected bool Parsed;

        [UsedImplicitly]
        [Persistent]
        public string TextConfig;

        public virtual string Text
        {
            get => TextConfig;
            set
            {
                TextConfig = value;
                TextConfig = Regex.Replace(TextConfig, @"[^\d+-.]", ""); //throw away junk characters
                Parsed     = double.TryParse(TextConfig, out double parsedValue);
                if (Parsed) ValConfig = parsedValue * _multiplier;
            }
        }

        public EditableDoubleMult() : this(0) { }

        public EditableDoubleMult(double val, double multiplier = 1)
        {
            Val         = val;
            _multiplier = multiplier;
            TextConfig  = (val / multiplier).ToString();
        }

        public static implicit operator double(EditableDoubleMult x) => x.Val;
    }

    public class EditableDouble : EditableDoubleMult
    {
        public EditableDouble(double val)
            : base(val)
        {
        }

        public static implicit operator EditableDouble(double x) => new EditableDouble(x);
    }

    public class EditableTime : EditableDouble
    {
        public EditableTime() : this(0) { }

        [UsedImplicitly]
        public EditableTime(double seconds)
            : base(seconds)
        {
            TextConfig = GuiUtils.TimeToDHMS(seconds);
        }

        public override double Val
        {
            get => ValConfig;
            set
            {
                ValConfig  = value;
                TextConfig = GuiUtils.TimeToDHMS(ValConfig);
            }
        }

        public override string Text
        {
            get => TextConfig;
            set
            {
                TextConfig = value;
                TextConfig = Regex.Replace(TextConfig, @"[^\d+-.ydhms ,]", ""); //throw away junk characters

                Parsed = double.TryParse(TextConfig, out double parsedValue);
                switch (Parsed)
                {
                    case true:
                        ValConfig = parsedValue;
                        break;
                    case false:
                        Parsed = GuiUtils.TryParseDHMS(TextConfig, out parsedValue);
                        if (Parsed) ValConfig = parsedValue;
                        break;
                }
            }
        }

        public static implicit operator EditableTime(double x) => new EditableTime(x);
    }

    public class EditableAngle
    {
        [UsedImplicitly]
        [Persistent]
        public readonly EditableDouble Degrees;

        [UsedImplicitly]
        [Persistent]
        public readonly EditableDouble Minutes;

        [UsedImplicitly]
        [Persistent]
        public readonly EditableDouble Seconds;

        [UsedImplicitly]
        [Persistent]
        public bool Negative;

        public EditableAngle(double angle)
        {
            angle = MuUtils.ClampDegrees180(angle);

            Negative =  angle < 0;
            angle    =  Math.Abs(angle);
            Degrees  =  new EditableDouble((int)angle);
            angle    -= Degrees.Val;
            Minutes  =  new EditableDouble((int)(60 * angle));
            angle    -= Minutes.Val / 60;
            Seconds  =  new EditableDouble(Math.Round(3600 * angle));
        }

        public static implicit operator double(EditableAngle x) => (x.Negative ? -1 : 1) * (x.Degrees + x.Minutes / 60.0 + x.Seconds / 3600.0);

        public static implicit operator EditableAngle(double x) => new EditableAngle(x);

        public enum Direction { NS, EW }

        public void DrawEditGUI(Direction direction)
        {
            GUILayout.BeginHorizontal();
            Degrees.Text = GUILayout.TextField(Degrees.Text, GUILayout.Width(30));
            GUILayout.Label("°", GUILayout.ExpandWidth(false));
            Minutes.Text = GUILayout.TextField(Minutes.Text, GUILayout.Width(30));
            GUILayout.Label("'", GUILayout.ExpandWidth(false));
            Seconds.Text = GUILayout.TextField(Seconds.Text, GUILayout.Width(30));
            GUILayout.Label("\"", GUILayout.ExpandWidth(false));
            string dirString = direction == Direction.NS ? Negative ? "S" : "N" : Negative ? "W" : "E";
            if (GUILayout.Button(dirString, GUILayout.Width(25))) Negative = !Negative;
            GUILayout.EndHorizontal();
        }
    }

    public class EditableInt : IEditable
    {
        [UsedImplicitly]
        [Persistent]
        public int ValConfig;

        public int Val
        {
            get => ValConfig;
            set
            {
                ValConfig  = value;
                TextConfig = value.ToString();
            }
        }

        private bool _parsed;

        [UsedImplicitly]
        [Persistent]
        public string TextConfig;

        public virtual string Text
        {
            get => TextConfig;
            set
            {
                TextConfig = value;
                TextConfig = Regex.Replace(TextConfig, @"[^\d+-]", ""); //throw away junk characters
                _parsed    = int.TryParse(TextConfig, out int parsedValue);
                if (_parsed) Val = parsedValue;
            }
        }

        public EditableInt(int val)
        {
            Val        = val;
            TextConfig = val.ToString();
        }

        public static implicit operator int(EditableInt x) => x.Val;

        public static implicit operator EditableInt(int x) => new EditableInt(x);
    }

    public class EditableIntList : IEditable
    {
        [Persistent]
        public readonly List<int> Val = new List<int>();

        [UsedImplicitly]
        [Persistent]
        public string TextConfig = "";

        public string Text
        {
            get => TextConfig;
            set
            {
                TextConfig = value;
                TextConfig = Regex.Replace(TextConfig, @"[^\d-,]", ""); //throw away junk characters
                Val.Clear();

                // supports "1,2,3" and "1-3"
                foreach (string x in TextConfig.Split(','))
                {
                    string[] y = x.Split('-');

                    if (!int.TryParse(y[0].Trim(), out int start)) continue;
                    if (!int.TryParse(y[y.Length - 1].Trim(), out int end)) continue;

                    for (int n = start; n <= end; n++)
                        Val.Add(n);
                }
            }
        }
    }

    public class ZombieGUILoader : MonoBehaviour
    {
        private void OnGUI()
        {
            GuiUtils.CopyDefaultSkin();
            if (GuiUtils.Skin == null) GuiUtils.Skin = GuiUtils.DefaultSkin;
            Destroy(gameObject);
        }
    }

    public static class GuiUtils
    {
        private static GUIStyle _yellowOnHover;

        public static GUIStyle YellowOnHover
        {
            get
            {
                if (_yellowOnHover != null) return _yellowOnHover;

                _yellowOnHover = new GUIStyle(GUI.skin.label) { hover = { textColor = Color.yellow } };
                var t = new Texture2D(1, 1);
                t.SetPixel(0, 0, new Color(0, 0, 0, 0));
                t.Apply();
                _yellowOnHover.hover.background = t;

                return _yellowOnHover;
            }
        }

        private static GUIStyle _yellowLabel;

        public static GUIStyle YellowLabel
        {
            get
            {
                if (_yellowLabel != null) return _yellowLabel;

                _yellowLabel = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow }, hover = { textColor = Color.yellow } };

                return _yellowLabel;
            }
        }

        private static GUIStyle _redLabel;

        public static GUIStyle RedLabel
        {
            get
            {
                if (_redLabel != null) return _redLabel;

                _redLabel = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red }, hover = { textColor = Color.red } };

                return _redLabel;
            }
        }

        private static GUIStyle _greenLabel;

        public static GUIStyle GreenLabel
        {
            get
            {
                if (_greenLabel != null) return _greenLabel;

                _greenLabel = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green }, hover = { textColor = Color.green } };

                return _greenLabel;
            }
        }

        private static GUIStyle _orangeLabel;

        public static GUIStyle OrangeLabel
        {
            get
            {
                if (_orangeLabel != null) return _orangeLabel;

                _orangeLabel = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green }, hover = { textColor = Color.green } };

                return _orangeLabel;
            }
        }

        private static GUIStyle _middleCenterLabel;

        public static GUIStyle MiddleCenterLabel
        {
            get
            {
                if (_middleCenterLabel != null) return _middleCenterLabel;

                _middleCenterLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

                return _middleCenterLabel;
            }
        }

        private static GUIStyle _middleRightLabel;

        public static GUIStyle MiddleRightLabel
        {
            get
            {
                if (_middleRightLabel != null) return _middleRightLabel;

                _middleRightLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };

                return _middleRightLabel;
            }
        }

        private static GUIStyle _upperCenterLabel;

        public static GUIStyle UpperCenterLabel
        {
            get
            {
                if (_upperCenterLabel != null) return _upperCenterLabel;

                _upperCenterLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter };

                return _upperCenterLabel;
            }
        }

        private static GUIStyle _labelNoWrap;

        public static GUIStyle LabelNoWrap => _labelNoWrap ??= new GUIStyle(GUI.skin.label) { wordWrap = false };

        private static GUIStyle _greenToggle;

        public static GUIStyle GreenToggle
        {
            get
            {
                if (_greenToggle != null) return _greenToggle;

                _greenToggle = new GUIStyle(GUI.skin.toggle) { onHover = { textColor = Color.green }, onNormal = { textColor = Color.green } };

                return _greenToggle;
            }
        }

        private static GUIStyle _redToggle;

        public static GUIStyle RedToggle
        {
            get
            {
                if (_redToggle != null) return _redToggle;

                _redToggle = new GUIStyle(GUI.skin.toggle) { onHover = { textColor = Color.red }, onNormal = { textColor = Color.red } };

                return _redToggle;
            }
        }

        private static GUIStyle _yellowToggle;

        public static GUIStyle YellowToggle => _yellowToggle ??=
            new GUIStyle(GUI.skin.toggle) { onHover = { textColor = Color.yellow }, onNormal = { textColor = Color.yellow } };

        public enum SkinType { DEFAULT, MECH_JEB1, COMPACT }

        public static GUISkin Skin;
        public static float   Scale                      = 1;
        public static int     ScaledScreenWidth          = 1;
        public static int     ScaledScreenHeight         = 1;
        public static bool    DontUseDropDownMenu        = false;
        public static bool    ShowAdvancedWindowSettings = false;
        public static GUISkin DefaultSkin;
        public static GUISkin CompactSkin;
        public static GUISkin TransparentSkin;

        public static void SetGUIScale(double s)
        {
            Scale              = Mathf.Clamp((float)s, 0.2f, 5f);
            ScaledScreenHeight = Mathf.RoundToInt(Screen.height / Scale);
            ScaledScreenWidth  = Mathf.RoundToInt(Screen.width / Scale);
        }

        public static void CopyDefaultSkin()
        {
            GUI.skin    = null;
            DefaultSkin = Object.Instantiate(GUI.skin);
        }

        [UsedImplicitly]
        public static void CopyCompactSkin()
        {
            GUI.skin    = null;
            CompactSkin = Object.Instantiate(GUI.skin);

            Skin.name = "KSP Compact";

            CompactSkin.label.margin  = new RectOffset(1, 1, 1, 1);
            CompactSkin.label.padding = new RectOffset(0, 0, 2, 2);

            CompactSkin.button.margin  = new RectOffset(1, 1, 1, 1);
            CompactSkin.button.padding = new RectOffset(4, 4, 2, 2);

            CompactSkin.toggle.margin  = new RectOffset(1, 1, 1, 1);
            CompactSkin.toggle.padding = new RectOffset(15, 0, 2, 0);

            CompactSkin.textField.margin  = new RectOffset(1, 1, 1, 1);
            CompactSkin.textField.padding = new RectOffset(2, 2, 2, 2);

            CompactSkin.textArea.margin  = new RectOffset(1, 1, 1, 1);
            CompactSkin.textArea.padding = new RectOffset(2, 2, 2, 2);

            CompactSkin.window.margin  = new RectOffset(0, 0, 0, 0);
            CompactSkin.window.padding = new RectOffset(5, 5, 20, 5);
        }

        private static void CopyTransparentSkin()
        {
            GUI.skin        = null;
            TransparentSkin = Object.Instantiate(GUI.skin);

            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, new Color(0, 0, 0, 0));
            t.Apply();

            TransparentSkin.window.normal.background   = t;
            TransparentSkin.window.onNormal.background = t;
            TransparentSkin.window.padding             = new RectOffset(5, 5, 5, 5);
        }

        public static void LoadSkin(SkinType skinType)
        {
            if (DefaultSkin == null) CopyDefaultSkin();
            if (CompactSkin == null) CopyCompactSkin();
            if (TransparentSkin == null) CopyTransparentSkin();

            switch (skinType)
            {
                case SkinType.DEFAULT:
                    Skin = DefaultSkin;
                    break;

                case SkinType.MECH_JEB1:
                    Skin = AssetBase.GetGUISkin("KSP window 2");
                    break;

                case SkinType.COMPACT:
                    Skin = CompactSkin;
                    break;
            }
        }

        private static GUILayoutOption _layoutExpandWidth, _layoutNoExpandWidth;

        [UsedImplicitly]
        public static GUILayoutOption LayoutExpandWidth => _layoutExpandWidth ??= GUILayout.ExpandWidth(true);

        [UsedImplicitly]
        public static GUILayoutOption LayoutNoExpandWidth => _layoutNoExpandWidth ??= GUILayout.ExpandWidth(false);

        public static GUILayoutOption ExpandWidth(bool b) => b ? LayoutExpandWidth : LayoutNoExpandWidth;

        private static readonly Dictionary<float, GUILayoutOption> _layoutWidthDict = new Dictionary<float, GUILayoutOption>(16);

        public static GUILayoutOption LayoutWidth(float width)
        {
            if (_layoutWidthDict.TryGetValue(width, out GUILayoutOption option)) return option;
            option = GUILayout.Width(width);
            _layoutWidthDict.Add(width, option);
            return option;
        }

        public static void SimpleTextField(IEditable ed, float width = 100, bool expandWidth = false)
        {
            Profiler.BeginSample("SimpleTextField");
            string res = !expandWidth
                ? GUILayout.TextField(ed.Text, LayoutWidth(width), ExpandWidth(false))
                : GUILayout.TextField(ed.Text, LayoutWidth(width));
            if (res != null && !res.Equals(ed.Text))
                ed.Text = res;
            Profiler.EndSample();
        }

#nullable enable
        public static void SimpleTextBox(string? leftLabel, IEditable ed, string? rightLabel = null, float width = 100,
            GUIStyle? leftLabelStyle = null, bool horizontalFraming = true)
        {
            Profiler.BeginSample("SimpleTextBox");
            if (horizontalFraming) GUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(leftLabel))
            {
                leftLabelStyle ??= GUI.skin.label;
                GUILayout.Label(leftLabel, leftLabelStyle);
            }

            SimpleTextField(ed, width, true);
            if (!string.IsNullOrEmpty(rightLabel)) GUILayout.Label(rightLabel);
            if (horizontalFraming) GUILayout.EndHorizontal();
            Profiler.EndSample();
        }

        public static void ToggledTextBox(ref bool toggle, string toggleText, IEditable ed, string? rightLabel = null, GUIStyle? toggleStyle = null,
            float width = 100)
        {
            Profiler.BeginSample("ToggledTextField");
            GUILayout.BeginHorizontal();
            toggle = toggleStyle != null ? GUILayout.Toggle(toggle, toggleText, toggleStyle) : GUILayout.Toggle(toggle, toggleText);
            SimpleTextField(ed, width);
            if (!string.IsNullOrEmpty(rightLabel)) GUILayout.Label(rightLabel);
            GUILayout.EndHorizontal();
            Profiler.EndSample();
        }

        public static bool ButtonTextBox(string buttonText, IEditable ed, string? rightLabel = null, GUIStyle? buttonStyle = null, float width = 100)
        {
            Profiler.BeginSample("ButtonTextBox");
            GUILayout.BeginHorizontal();
            bool pressed = buttonStyle != null ? GUILayout.Button(buttonText, buttonStyle) : GUILayout.Button(buttonText);
            SimpleTextField(ed, width);
            if (!string.IsNullOrEmpty(rightLabel)) GUILayout.Label(rightLabel);
            GUILayout.EndHorizontal();
            Profiler.EndSample();
            return pressed;
        }

#nullable restore

        public static void SimpleLabel(string leftLabel, string rightLabel = "")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(leftLabel, ExpandWidth(true));
            if (!string.IsNullOrEmpty(rightLabel))
                GUILayout.Label(rightLabel, ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        public static void SimpleLabelInt(string leftLabel, int rightValue) => SimpleLabel(leftLabel, rightValue.ToString());

        public static int ArrowSelector(int index, int numIndices, Action centerGuiAction)
        {
            if (numIndices == 0) return index;

            GUILayout.BeginHorizontal();
            //if (numIndices > 1 && GUILayout.Button("◀", GUILayout.ExpandWidth(false))) index = (index - 1 + numIndices) % numIndices; // Seems those are gone from KSP font
            if (numIndices > 1 && GUILayout.Button("<", GUILayout.ExpandWidth(false))) index = (index - 1 + numIndices) % numIndices;
            centerGuiAction();
            //if (numIndices > 1 && GUILayout.Button("▶", GUILayout.ExpandWidth(false))) index = (index + 1) % numIndices;
            if (numIndices > 1 && GUILayout.Button(">", GUILayout.ExpandWidth(false))) index = (index + 1) % numIndices;
            GUILayout.EndHorizontal();

            return index;
        }

        private static GUIStyle _arrowSelectorStyeGuiStyleExpand;

        [UsedImplicitly]
        public static GUIStyle ArrowSelectorStyeGuiStyleExpand => _arrowSelectorStyeGuiStyleExpand ??=
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, stretchWidth = true };

        private static GUIStyle _arrowSelectorStyeGuiStyleNoExpand;

        [UsedImplicitly]
        public static GUIStyle ArrowSelectorStyeGuiStyleNoExpand => _arrowSelectorStyeGuiStyleNoExpand ??=
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, stretchWidth = false };

        public static int ArrowSelector(int index, int modulo, string label, bool expandWidth = true)
        {
            return ArrowSelector(index, modulo, DrawLabel);

            void DrawLabel()
            {
                GUILayout.Label(label, expandWidth ? ArrowSelectorStyeGuiStyleExpand : ArrowSelectorStyeGuiStyleNoExpand);
            }
        }

        public static int HoursPerDay => GameSettings.KERBIN_TIME ? 6 : 24;
        public static int DaysPerYear => GameSettings.KERBIN_TIME ? 426 : 365;

        public static string TimeToDHMS(double seconds, int decimalPlaces = 0)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds)) return "Inf";

            string ret = "";
            bool showSecondsDecimals = decimalPlaces > 0;

            try
            {
                string[] units = { "y", "d", "h", "m", "s" };
                long[] intervals = { KSPUtil.dateTimeFormatter.Year, KSPUtil.dateTimeFormatter.Day, 3600, 60, 1 };

                if (seconds < 0)
                {
                    ret     += "-";
                    seconds *= -1;
                }

                for (int i = 0; i < units.Length; i++)
                {
                    long n = (long)(seconds / intervals[i]);
                    bool first = ret.Length < 2;
                    if (!first || n != 0 || (i == units.Length - 1 && ret == ""))
                    {
                        if (!first) ret += " ";

                        if (showSecondsDecimals && seconds < 60 && i == units.Length - 1)
                            ret             += seconds.ToString("00." + new string('0', decimalPlaces));
                        else if (first) ret += n.ToString();
                        else ret            += n.ToString(i == 1 ? "000" : "00");

                        ret += units[i];
                    }

                    seconds -= n * intervals[i];
                }
            }
            catch (Exception)
            {
                return "NaN";
            }

            return ret;
        }

        public static bool TryParseDHMS(string s, out double seconds)
        {
            string[] units = { "y", "d", "h", "m", "s" };
            int[] intervals = { KSPUtil.dateTimeFormatter.Year, KSPUtil.dateTimeFormatter.Day, 3600, 60, 1 };

            s = s.Trim(' ');
            bool minus = s.StartsWith("-");

            seconds = 0;
            bool parsedSomething = false;
            for (int i = 0; i < units.Length; i++)
            {
                s = s.Trim(' ', ',', '-');
                int unitPos = s.IndexOf(units[i]);
                if (unitPos == -1) continue;

                if (!double.TryParse(s.Substring(0, unitPos), out double value)) return false;
                seconds         += value * intervals[i];
                s               =  s.Substring(unitPos + 1);
                parsedSomething =  true;
            }

            if (minus) seconds = -seconds;

            return parsedSomething;
        }

        private static double ArcDistance(Vector3 from, Vector3 to)
        {
            Vector3 position = FlightGlobals.ActiveVessel.mainBody.transform.position;
            double a = (position - from).magnitude;
            double b = (position - to).magnitude;
            double c = Vector3d.Distance(from, to);
            double ang = Math.Acos((a * a + b * b - c * c) / (2f * a * b));
            return ang * FlightGlobals.ActiveVessel.mainBody.Radius;
        }

        public static double FromToETA(Vector3 from, Vector3 to, double speed = 0) =>
            ArcDistance(from, to) / (speed > 0 ? speed : FlightGlobals.ActiveVessel.horizontalSrfSpeed);

        public static bool MouseIsOverWindow(MechJebCore core)
        {
            //try to check if the mouse is over any active DisplayModule
            foreach (DisplayModule m in core.GetComputerModules<DisplayModule>())
            {
                if (m.Enabled && m.ShowInCurrentScene && !m.IsOverlay
                    && m.WindowPos.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / Scale))
                {
                    return true;
                }
            }

            return false;
        }

        public static Coordinates GetMouseCoordinates(CelestialBody body)
        {
            Ray mouseRay = PlanetariumCamera.Camera.ScreenPointToRay(Input.mousePosition);
            mouseRay.origin = ScaledSpace.ScaledToLocalSpace(mouseRay.origin);
            Vector3d relOrigin = mouseRay.origin - body.position;
            double curRadius = body.pqsController.radiusMax;
            double lastRadius = 0;
            int loops = 0;
            while (loops < 50)
            {
                if (PQS.LineSphereIntersection(relOrigin, mouseRay.direction, curRadius, out Vector3d relSurfacePosition))
                {
                    Vector3d surfacePoint = body.position + relSurfacePosition;
                    double alt = body.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(body.GetLongitude(surfacePoint), Vector3d.down) *
                                                                     QuaternionD.AngleAxis(body.GetLatitude(surfacePoint), Vector3d.forward) *
                                                                     Vector3d.right);
                    double error = Math.Abs(curRadius - alt);
                    if (error < (body.pqsController.radiusMax - body.pqsController.radiusMin) / 100)
                    {
                        return new Coordinates(body.GetLatitude(surfacePoint), MuUtils.ClampDegrees180(body.GetLongitude(surfacePoint)));
                    }

                    lastRadius = curRadius;
                    curRadius  = alt;
                    loops++;
                }
                else
                {
                    if (loops == 0)
                    {
                        break;
                    }

                    // Went too low, needs to try higher
                    curRadius = (lastRadius * 9 + curRadius) / 10;
                    loops++;
                }
            }

            return null;
        }

        [UsedImplicitly]
        public class ComboBox
        {
            // Easy to use combobox class
            // ***** For users *****
            // Call the Box method with the latest selected item, list of text entries
            // and an object identifying who is making the request.
            // The result is the newly selected item.
            // There is currently no way of knowing when a choice has been made

            // Position of the popup
            private static Rect _rect;

            // Identifier of the caller of the popup, null if nobody is waiting for a value
            private static object   _popupOwner;
            private static string[] _entries;

            private static bool _popupActive;

            // Result to be returned to the owner
            private static int _selectedItem;

            // Unity identifier of the window, just needs to be unique
            private static readonly int _id = GUIUtility.GetControlID(FocusType.Passive);

            // ComboBox GUI Style
            private static readonly GUIStyle _style;

            static ComboBox()
            {
                _style             = new GUIStyle(GUI.skin.window) { normal = { background = null }, onNormal = { background = null } };
                _style.border.top  = _style.border.bottom;
                _style.padding.top = _style.padding.bottom;
            }

            public static void DrawGUI()
            {
                if (_popupOwner == null || _rect.height == 0 || !_popupActive)
                    return;

                if (_style.normal.background == null)
                {
                    _style.normal.background   = MechJebBundlesManager.comboBoxBackground;
                    _style.onNormal.background = MechJebBundlesManager.comboBoxBackground;
                }

                // Make sure the rectangle is fully on screen
                _rect.x = Math.Max(0, Math.Min(_rect.x, ScaledScreenWidth - _rect.width));
                _rect.y = Math.Max(0, Math.Min(_rect.y, ScaledScreenHeight - _rect.height));

                _rect = GUILayout.Window(_id, _rect, identifier =>
                {
                    _selectedItem = GUILayout.SelectionGrid(-1, _entries, 1, YellowOnHover);
                    if (GUI.changed)
                        _popupActive = false;
                }, "", _style);

                //Cancel the popup if we click outside
                if (Event.current.type == EventType.MouseDown && !_rect.Contains(Event.current.mousePosition))
                    _popupOwner = null;
            }

            public static int Box(int selectedItem, string[] entries, object caller, bool expandWidth = true)
            {
                // Trivial cases (0-1 items)
                if (entries.Length == 0)
                    return 0;
                if (entries.Length == 1)
                {
                    GUILayout.Label(entries[0]);
                    return 0;
                }

                if (selectedItem >= entries.Length)
                    selectedItem = entries.Length - 1;

                if (DontUseDropDownMenu)
                    return ArrowSelector(selectedItem, entries.Length, entries[selectedItem], expandWidth);

                // A choice has been made, update the return value
                if (_popupOwner == caller && !_popupActive)
                {
                    _popupOwner  = null;
                    selectedItem = _selectedItem;
                    GUI.changed  = true;
                }

                bool guiChanged = GUI.changed;
                if (GUILayout.Button("↓ " + entries[selectedItem] + " ↓", GUILayout.ExpandWidth(expandWidth)))
                {
                    // We will set the changed status when we return from the menu instead
                    GUI.changed = guiChanged;
                    // Update the global state with the new items
                    _popupOwner  = caller;
                    _popupActive = true;
                    _entries     = entries;
                    // Magic value to force position update during repaint event
                    _rect = new Rect(0, 0, 0, 0);
                }

                // The GetLastRect method only works during repaint event, but the Button will return false during repaint
                if (Event.current.type == EventType.Repaint && _popupOwner == caller && _rect.height == 0)
                {
                    _rect = GUILayoutUtility.GetLastRect();
                    // But even worse, I can't find a clean way to convert from relative to absolute coordinates
                    Vector2 mousePos = Input.mousePosition;
                    mousePos.y = Screen.height - mousePos.y;
                    Vector2 clippedMousePos = Event.current.mousePosition;
                    _rect.x = (_rect.x + mousePos.x) / Scale - clippedMousePos.x;
                    _rect.y = (_rect.y + mousePos.y) / Scale - clippedMousePos.y;
                }

                return selectedItem;
            }
        }
    }

    public class Coordinates
    {
        public readonly double Latitude;
        public readonly double Longitude;

        public Coordinates(double latitude, double longitude)
        {
            Latitude  = latitude;
            Longitude = longitude;
        }

        [UsedImplicitly]
        public static string ToStringDecimal(double latitude, double longitude, bool newline = false, int precision = 3)
        {
            double clampedLongitude = MuUtils.ClampDegrees180(longitude);
            double latitudeAbs = Math.Abs(latitude);
            double longitudeAbs = Math.Abs(clampedLongitude);
            return latitudeAbs.ToString("F" + precision) + "° " + (latitude > 0 ? "N" : "S") + (newline ? "\n" : ", ")
                   + longitudeAbs.ToString("F" + precision) + "° " + (clampedLongitude > 0 ? "E" : "W");
        }

        public string ToStringDecimal(bool newline = false, int precision = 3) => ToStringDecimal(Latitude, Longitude, newline, precision);

        public static string ToStringDMS(double latitude, double longitude, bool newline = false)
        {
            double clampedLongitude = MuUtils.ClampDegrees180(longitude);
            return AngleToDMS(latitude) + (latitude > 0 ? " N" : " S") + (newline ? "\n" : ", ")
                   + AngleToDMS(clampedLongitude) + (clampedLongitude > 0 ? " E" : " W");
        }

        public string ToStringDMS(bool newline = false) => ToStringDMS(Latitude, Longitude, newline);

        public static string AngleToDMS(double angle)
        {
            int degrees = (int)Math.Floor(Math.Abs(angle));
            int minutes = (int)Math.Floor(60 * (Math.Abs(angle) - degrees));
            int seconds = (int)Math.Floor(3600 * (Math.Abs(angle) - degrees - minutes / 60.0));

            return $"{degrees:0}° {minutes:00}' {seconds:00}\"";
        }
    }

    public static class ColorPickerHSV
    {
        private static Texture2D _displayPicker;

        [UsedImplicitly]
        public static Color SetColor;

        private static Color _lastSetColor;

        private const int TEXTURE_WIDTH  = 240;
        private const int TEXTURE_HEIGHT = 240;

        private static float     _saturationSlider;
        private static float     _alphaSlider;
        private static Texture2D _saturationTexture;

        private static void Init()
        {
            _displayPicker = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT, TextureFormat.ARGB32, false);
            for (int i = 0; i < TEXTURE_WIDTH; i++)
            {
                for (int j = 0; j < TEXTURE_HEIGHT; j++)
                {
                    _displayPicker.SetPixel(i, j, MuUtils.HSVtoRGB(360f / TEXTURE_WIDTH * i, 1.0f / j * TEXTURE_HEIGHT, 1.0f, 1f));
                }
            }

            _displayPicker.Apply();

            float v = 0.0F;
            float diff = 1.0f / TEXTURE_HEIGHT;
            _saturationTexture = new Texture2D(20, TEXTURE_HEIGHT);
            for (int i = 0; i < _saturationTexture.width; i++)
            {
                for (int j = 0; j < _saturationTexture.height; j++)
                {
                    _saturationTexture.SetPixel(i, j, new Color(v, v, v));
                    v += diff;
                }

                v = 0.0F;
            }

            _saturationTexture.Apply();
        }

        public static void DrawGUI(int positionLeft, int positionTop)
        {
            if (!_displayPicker)
                Init();

            GUI.Box(new Rect(positionLeft - 3, positionTop - 3, TEXTURE_WIDTH + 90, TEXTURE_HEIGHT + 30), "");

            if (GUI.RepeatButton(new Rect(positionLeft, positionTop, TEXTURE_WIDTH, TEXTURE_HEIGHT), _displayPicker))
            {
                int a = (int)Input.mousePosition.x;
                int b = Screen.height - (int)Input.mousePosition.y;

                SetColor      = _displayPicker.GetPixel(a - positionLeft, -(b - positionTop));
                _lastSetColor = SetColor;
            }

            _saturationSlider = GUI.VerticalSlider(new Rect(positionLeft + TEXTURE_WIDTH + 3, positionTop, 10, TEXTURE_HEIGHT), _saturationSlider, 1,
                0);
            SetColor = _lastSetColor + new Color(_saturationSlider, _saturationSlider, _saturationSlider);
            GUI.Box(new Rect(positionLeft + TEXTURE_WIDTH + 20, positionTop, 20, TEXTURE_HEIGHT), _saturationTexture);

            _alphaSlider = GUI.VerticalSlider(new Rect(positionLeft + TEXTURE_WIDTH + 3 + 10 + 20 + 10, positionTop, 10, TEXTURE_HEIGHT),
                _alphaSlider, 1,
                0);
            SetColor.a = _alphaSlider;
            GUI.Box(new Rect(positionLeft + TEXTURE_WIDTH + 20 + 10 + 20 + 10, positionTop, 20, TEXTURE_HEIGHT), _saturationTexture);
        }
    }

    public static class ColorPickerRGB
    {
        private const int TEXTURE_WIDTH  = 240;
        private const int TEXTURE_HEIGHT = 10;

        private static Texture2D _rTexture;
        private static Texture2D _gTexture;
        private static Texture2D _bTexture;
        private static Texture2D _aTexture;

        private static void Init()
        {
            _rTexture = new Texture2D(TEXTURE_WIDTH, 1);
            _gTexture = new Texture2D(TEXTURE_WIDTH, 1);
            _bTexture = new Texture2D(TEXTURE_WIDTH, 1);
            _aTexture = new Texture2D(TEXTURE_WIDTH, 1);
            for (int i = 0; i < TEXTURE_WIDTH; i++)
            {
                float v = (float)i / (TEXTURE_WIDTH - 1);
                _rTexture.SetPixel(i, 0, new Color(v, 0, 0));
                _gTexture.SetPixel(i, 0, new Color(0, v, 0));
                _bTexture.SetPixel(i, 0, new Color(0, 0, v));
                _aTexture.SetPixel(i, 0, new Color(v, v, v));
            }

            _rTexture.Apply();
            _gTexture.Apply();
            _bTexture.Apply();
            _aTexture.Apply();

            _rTexture.wrapMode = TextureWrapMode.Repeat;
            _gTexture.wrapMode = TextureWrapMode.Repeat;
            _bTexture.wrapMode = TextureWrapMode.Repeat;
            _aTexture.wrapMode = TextureWrapMode.Repeat;
        }

        public static Color DrawGUI(int positionLeft, int positionTop, Color c)
        {
            if (!_rTexture)
                Init();

            GUI.Box(new Rect(positionLeft - 3, positionTop - 3, TEXTURE_WIDTH + 3, TEXTURE_HEIGHT + 125), "");

            float pos = positionTop + 5;
            GUI.DrawTextureWithTexCoords(new Rect(positionLeft, pos, TEXTURE_WIDTH, TEXTURE_HEIGHT), _rTexture, new Rect(0, 0, 1, TEXTURE_HEIGHT));
            c.r = GUI.HorizontalSlider(new Rect(positionLeft, pos + TEXTURE_HEIGHT + 5, TEXTURE_WIDTH, 10), c.r, 0, 1);

            pos += TEXTURE_HEIGHT + 20;

            GUI.DrawTextureWithTexCoords(new Rect(positionLeft, pos, TEXTURE_WIDTH, TEXTURE_HEIGHT), _gTexture, new Rect(0, 0, 1, TEXTURE_HEIGHT));
            c.g = GUI.HorizontalSlider(new Rect(positionLeft, pos + TEXTURE_HEIGHT + 5, TEXTURE_WIDTH, 10), c.g, 0, 1);

            pos += TEXTURE_HEIGHT + 20;

            GUI.DrawTextureWithTexCoords(new Rect(positionLeft, pos, TEXTURE_WIDTH, TEXTURE_HEIGHT), _bTexture, new Rect(0, 0, 1, TEXTURE_HEIGHT));
            c.b = GUI.HorizontalSlider(new Rect(positionLeft, pos + TEXTURE_HEIGHT + 5, TEXTURE_WIDTH, 10), c.b, 0, 1);

            pos += TEXTURE_HEIGHT + 20;

            GUI.DrawTextureWithTexCoords(new Rect(positionLeft, pos, TEXTURE_WIDTH, TEXTURE_HEIGHT), _aTexture, new Rect(0, 0, 1, TEXTURE_HEIGHT));
            c.a = GUI.HorizontalSlider(new Rect(positionLeft, pos + TEXTURE_HEIGHT + 5, TEXTURE_WIDTH, 10), c.a, 0, 1);

            return c;
        }
    }
}
