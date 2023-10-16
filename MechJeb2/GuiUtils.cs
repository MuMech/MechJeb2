﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace MuMech
{
    public interface IEditable
    {
        string text { get; set; }
    }

    //An EditableDouble stores a double value and a text string. The user can edit the string.
    //Whenever the text is edited, it is parsed and the parsed value is stored in val. As a
    //convenience, a multiplier can be specified so that the stored value is actually
    //(multiplier * parsed value). If the parsing fails, the parsed flag is set to false and
    //the stored value is unchanged. There are implicit conversions between EditableDouble and
    //double so that if you are not doing text input you can treat an EditableDouble like a double.
    public class EditableDoubleMult : IEditable
    {
        [Persistent]
        protected double _val;

        public virtual double val
        {
            get => _val;
            set
            {
                _val  = value;
                _text = (_val / multiplier).ToString();
            }
        }

        public readonly double multiplier;

        public bool parsed;

        [Persistent]
        protected string _text;

        public virtual string text
        {
            get => _text;
            set
            {
                _text = value;
                _text = Regex.Replace(_text, @"[^\d+-.]", ""); //throw away junk characters
                double parsedValue;
                parsed = double.TryParse(_text, out parsedValue);
                if (parsed) _val = parsedValue * multiplier;
            }
        }

        public EditableDoubleMult() : this(0) { }

        public EditableDoubleMult(double val, double multiplier = 1)
        {
            this.val        = val;
            this.multiplier = multiplier;
            _text           = (val / multiplier).ToString();
        }

        public static implicit operator double(EditableDoubleMult x) => x.val;
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

        public EditableTime(double seconds)
            : base(seconds)
        {
            _text = GuiUtils.TimeToDHMS(seconds);
        }

        public override double val
        {
            get => _val;
            set
            {
                _val  = value;
                _text = GuiUtils.TimeToDHMS(_val);
            }
        }

        public override string text
        {
            get => _text;
            set
            {
                _text = value;
                _text = Regex.Replace(_text, @"[^\d+-.ydhms ,]", ""); //throw away junk characters

                double parsedValue;
                parsed = double.TryParse(_text, out parsedValue);
                if (parsed) _val = parsedValue;

                if (!parsed)
                {
                    parsed = GuiUtils.TryParseDHMS(_text, out parsedValue);
                    if (parsed) _val = parsedValue;
                }
            }
        }

        public static implicit operator EditableTime(double x) => new EditableTime(x);
    }

    public class EditableAngle
    {
        [Persistent]
        public EditableDouble degrees = 0;

        [Persistent]
        public EditableDouble minutes = 0;

        [Persistent]
        public EditableDouble seconds = 0;

        [Persistent]
        public bool negative;

        public EditableAngle(double angle)
        {
            angle = MuUtils.ClampDegrees180(angle);

            negative =  angle < 0;
            angle    =  Math.Abs(angle);
            degrees  =  (int)angle;
            angle    -= degrees;
            minutes  =  (int)(60 * angle);
            angle    -= minutes / 60;
            seconds  =  Math.Round(3600 * angle);
        }

        public static implicit operator double(EditableAngle x) => (x.negative ? -1 : 1) * (x.degrees + x.minutes / 60.0 + x.seconds / 3600.0);

        public static implicit operator EditableAngle(double x) => new EditableAngle(x);

        public enum Direction { NS, EW }

        public void DrawEditGUI(Direction direction)
        {
            GUILayout.BeginHorizontal();
            degrees.text = GUILayout.TextField(degrees.text, GUILayout.Width(30));
            GUILayout.Label("°", GUILayout.ExpandWidth(false));
            minutes.text = GUILayout.TextField(minutes.text, GUILayout.Width(30));
            GUILayout.Label("'", GUILayout.ExpandWidth(false));
            seconds.text = GUILayout.TextField(seconds.text, GUILayout.Width(30));
            GUILayout.Label("\"", GUILayout.ExpandWidth(false));
            string dirString = direction == Direction.NS ? negative ? "S" : "N" : negative ? "W" : "E";
            if (GUILayout.Button(dirString, GUILayout.Width(25))) negative = !negative;
            GUILayout.EndHorizontal();
        }
    }

    public class EditableInt : IEditable
    {
        [Persistent]
        private int _val;

        public int val
        {
            get => _val;
            set
            {
                _val  = value;
                _text = value.ToString();
            }
        }

        private bool _parsed;

        [Persistent]
        private string _text;

        public virtual string text
        {
            get => _text;
            set
            {
                _text   = value;
                _text   = Regex.Replace(_text, @"[^\d+-]", ""); //throw away junk characters
                _parsed = int.TryParse(_text, out int parsedValue);
                if (_parsed) val = parsedValue;
            }
        }

        public EditableInt(int val)
        {
            this.val = val;
            _text    = val.ToString();
        }

        public static implicit operator int(EditableInt x) => x.val;

        public static implicit operator EditableInt(int x) => new EditableInt(x);
    }

    public class EditableIntList : IEditable
    {
        [Persistent]
        public readonly List<int> val = new List<int>();

        [Persistent]
        private string _text = "";

        public string text
        {
            get => _text;
            set
            {
                _text = value;
                _text = Regex.Replace(_text, @"[^\d-,]", ""); //throw away junk characters
                val.Clear();

                // supports "1,2,3" and "1-3"
                foreach (string x in _text.Split(','))
                {
                    string[] y = x.Split('-');

                    if (!int.TryParse(y[0].Trim(), out int start)) continue;
                    if (!int.TryParse(y[y.Length - 1].Trim(), out int end)) continue;

                    for (int n = start; n <= end; n++)
                        val.Add(n);
                }
            }
        }
    }

    public class ZombieGUILoader : MonoBehaviour
    {
        private void OnGUI()
        {
            GuiUtils.CopyDefaultSkin();
            if (GuiUtils.skin == null) GuiUtils.skin = GuiUtils.defaultSkin;
            Destroy(gameObject);
        }
    }

    public static class GuiUtils
    {
        private static GUIStyle _yellowOnHover;

        public static GUIStyle yellowOnHover
        {
            get
            {
                if (_yellowOnHover == null)
                {
                    _yellowOnHover                 = new GUIStyle(GUI.skin.label);
                    _yellowOnHover.hover.textColor = Color.yellow;
                    var t = new Texture2D(1, 1);
                    t.SetPixel(0, 0, new Color(0, 0, 0, 0));
                    t.Apply();
                    _yellowOnHover.hover.background = t;
                }

                return _yellowOnHover;
            }
        }

        private static GUIStyle _yellowLabel;

        public static GUIStyle yellowLabel
        {
            get
            {
                if (_yellowLabel == null)
                {
                    _yellowLabel                  = new GUIStyle(GUI.skin.label);
                    _yellowLabel.normal.textColor = Color.yellow;
                    _yellowLabel.hover.textColor  = Color.yellow;
                }

                return _yellowLabel;
            }
        }

        private static GUIStyle _redLabel;

        public static GUIStyle redLabel
        {
            get
            {
                if (_redLabel == null)
                {
                    _redLabel                  = new GUIStyle(GUI.skin.label);
                    _redLabel.normal.textColor = Color.red;
                    _redLabel.hover.textColor  = Color.red;
                }

                return _redLabel;
            }
        }

        private static GUIStyle _greenLabel;

        public static GUIStyle greenLabel
        {
            get
            {
                if (_greenLabel == null)
                {
                    _greenLabel                  = new GUIStyle(GUI.skin.label);
                    _greenLabel.normal.textColor = Color.green;
                    _greenLabel.hover.textColor  = Color.green;
                }

                return _greenLabel;
            }
        }

        private static GUIStyle _orangeLabel;

        public static GUIStyle orangeLabel
        {
            get
            {
                if (_orangeLabel == null)
                {
                    _orangeLabel                  = new GUIStyle(GUI.skin.label);
                    _orangeLabel.normal.textColor = Color.green;
                    _orangeLabel.hover.textColor  = Color.green;
                }

                return _orangeLabel;
            }
        }

        private static GUIStyle _middleCenterLabel;

        public static GUIStyle middleCenterLabel
        {
            get
            {
                if (_middleCenterLabel == null)
                {
                    _middleCenterLabel           = new GUIStyle(GUI.skin.label);
                    _middleCenterLabel.alignment = TextAnchor.MiddleCenter;
                }

                return _middleCenterLabel;
            }
        }

        private static GUIStyle _middleRightLabel;

        public static GUIStyle middleRightLabel
        {
            get
            {
                if (_middleRightLabel == null)
                {
                    _middleRightLabel           = new GUIStyle(GUI.skin.label);
                    _middleRightLabel.alignment = TextAnchor.MiddleRight;
                }

                return _middleRightLabel;
            }
        }

        private static GUIStyle _UpperCenterLabel;

        public static GUIStyle UpperCenterLabel
        {
            get
            {
                if (_UpperCenterLabel == null)
                {
                    _UpperCenterLabel           = new GUIStyle(GUI.skin.label);
                    _UpperCenterLabel.alignment = TextAnchor.UpperCenter;
                }

                return _UpperCenterLabel;
            }
        }

        private static GUIStyle _labelNoWrap;

        public static GUIStyle LabelNoWrap
        {
            get
            {
                if (_labelNoWrap == null)
                {
                    _labelNoWrap = new GUIStyle(GUI.skin.label) { wordWrap = false };
                }

                return _labelNoWrap;
            }
        }

        private static GUIStyle _greenToggle;

        public static GUIStyle greenToggle
        {
            get
            {
                if (_greenToggle == null)
                {
                    _greenToggle                    = new GUIStyle(GUI.skin.toggle);
                    _greenToggle.onHover.textColor  = Color.green;
                    _greenToggle.onNormal.textColor = Color.green;
                }

                return _greenToggle;
            }
        }

        private static GUIStyle _redToggle;

        public static GUIStyle redToggle
        {
            get
            {
                if (_redToggle == null)
                {
                    _redToggle                    = new GUIStyle(GUI.skin.toggle);
                    _redToggle.onHover.textColor  = Color.red;
                    _redToggle.onNormal.textColor = Color.red;
                }

                return _redToggle;
            }
        }

        private static GUIStyle _yellowToggle;

        public static GUIStyle yellowToggle
        {
            get
            {
                if (_yellowToggle == null)
                {
                    _yellowToggle                    = new GUIStyle(GUI.skin.toggle);
                    _yellowToggle.onHover.textColor  = Color.yellow;
                    _yellowToggle.onNormal.textColor = Color.yellow;
                }

                return _yellowToggle;
            }
        }

        public enum SkinType { Default, MechJeb1, Compact }

        public static GUISkin skin;
        public static float   scale                      = 1;
        public static int     scaledScreenWidth          = 1;
        public static int     scaledScreenHeight         = 1;
        public static bool    dontUseDropDownMenu        = false;
        public static bool    showAdvancedWindowSettings = false;
        public static GUISkin defaultSkin;
        public static GUISkin compactSkin;
        public static GUISkin transparentSkin;

        public static void SetGUIScale(double s)
        {
            scale              = Mathf.Clamp((float)s, 0.2f, 5f);
            scaledScreenHeight = Mathf.RoundToInt(Screen.height / scale);
            scaledScreenWidth  = Mathf.RoundToInt(Screen.width / scale);
        }

        public static void CopyDefaultSkin()
        {
            GUI.skin    = null;
            defaultSkin = Object.Instantiate(GUI.skin);
        }

        public static void CopyCompactSkin()
        {
            GUI.skin    = null;
            compactSkin = Object.Instantiate(GUI.skin);

            skin.name = "KSP Compact";

            compactSkin.label.margin  = new RectOffset(1, 1, 1, 1);
            compactSkin.label.padding = new RectOffset(0, 0, 2, 2);

            compactSkin.button.margin  = new RectOffset(1, 1, 1, 1);
            compactSkin.button.padding = new RectOffset(4, 4, 2, 2);

            compactSkin.toggle.margin  = new RectOffset(1, 1, 1, 1);
            compactSkin.toggle.padding = new RectOffset(15, 0, 2, 0);

            compactSkin.textField.margin  = new RectOffset(1, 1, 1, 1);
            compactSkin.textField.padding = new RectOffset(2, 2, 2, 2);

            compactSkin.textArea.margin  = new RectOffset(1, 1, 1, 1);
            compactSkin.textArea.padding = new RectOffset(2, 2, 2, 2);

            compactSkin.window.margin  = new RectOffset(0, 0, 0, 0);
            compactSkin.window.padding = new RectOffset(5, 5, 20, 5);
        }

        public static void CopyTransparentSkin()
        {
            GUI.skin        = null;
            transparentSkin = Object.Instantiate(GUI.skin);

            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, new Color(0, 0, 0, 0));
            t.Apply();

            transparentSkin.window.normal.background   = t;
            transparentSkin.window.onNormal.background = t;
            transparentSkin.window.padding             = new RectOffset(5, 5, 5, 5);
        }

        public static void LoadSkin(SkinType skinType)
        {
            if (defaultSkin == null) CopyDefaultSkin();
            if (compactSkin == null) CopyCompactSkin();
            if (transparentSkin == null) CopyTransparentSkin();

            switch (skinType)
            {
                case SkinType.Default:
                    skin = defaultSkin;
                    break;

                case SkinType.MechJeb1:
                    skin = AssetBase.GetGUISkin("KSP window 2");
                    break;

                case SkinType.Compact:
                    skin = compactSkin;
                    break;
            }
        }

        private static GUILayoutOption _layoutExpandWidth, _layoutNoExpandWidth;
        public static  GUILayoutOption LayoutExpandWidth   => _layoutExpandWidth ??= GUILayout.ExpandWidth(true);
        public static  GUILayoutOption LayoutNoExpandWidth => _layoutNoExpandWidth ??= GUILayout.ExpandWidth(false);

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
                ? GUILayout.TextField(ed.text, LayoutWidth(width), ExpandWidth(expandWidth))
                : GUILayout.TextField(ed.text, LayoutWidth(width));
            if (res != null && !res.Equals(ed.text))
                ed.text = res;
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

        public static GUIStyle arrowSelectorStyeGuiStyleExpand
        {
            get
            {
                if (_arrowSelectorStyeGuiStyleExpand == null)
                {
                    _arrowSelectorStyeGuiStyleExpand = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, stretchWidth = true };
                }

                return _arrowSelectorStyeGuiStyleExpand;
            }
        }

        private static GUIStyle _arrowSelectorStyeGuiStyleNoExpand;

        public static GUIStyle arrowSelectorStyeGuiStyleNoExpand
        {
            get
            {
                if (_arrowSelectorStyeGuiStyleNoExpand == null)
                {
                    _arrowSelectorStyeGuiStyleNoExpand = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, stretchWidth = false };
                }

                return _arrowSelectorStyeGuiStyleNoExpand;
            }
        }

        public static int ArrowSelector(int index, int modulo, string label, bool expandWidth = true)
        {
            Action drawLabel = () => GUILayout.Label(label, expandWidth ? arrowSelectorStyeGuiStyleExpand : arrowSelectorStyeGuiStyleNoExpand);
            return ArrowSelector(index, modulo, drawLabel);
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
                if (unitPos != -1)
                {
                    double value;
                    if (!double.TryParse(s.Substring(0, unitPos), out value)) return false;
                    seconds         += value * intervals[i];
                    s               =  s.Substring(unitPos + 1);
                    parsedSomething =  true;
                }
            }

            if (minus) seconds = -seconds;

            return parsedSomething;
        }

        public static double ArcDistance(Vector3 From, Vector3 To)
        {
            double a = (FlightGlobals.ActiveVessel.mainBody.transform.position - From).magnitude;
            double b = (FlightGlobals.ActiveVessel.mainBody.transform.position - To).magnitude;
            double c = Vector3d.Distance(From, To);
            double ang = Math.Acos((a * a + b * b - c * c) / (2f * a * b));
            return ang * FlightGlobals.ActiveVessel.mainBody.Radius;
        }

        public static double FromToETA(Vector3 From, Vector3 To, double Speed = 0) =>
            ArcDistance(From, To) / (Speed > 0 ? Speed : FlightGlobals.ActiveVessel.horizontalSrfSpeed);

        public static bool MouseIsOverWindow(MechJebCore core)
        {
            //try to check if the mouse is over any active DisplayModule
            foreach (DisplayModule m in core.GetComputerModules<DisplayModule>())
            {
                if (m.Enabled && m.showInCurrentScene && !m.IsOverlay
                    && m.windowPos.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / scale))
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
            Vector3d relSurfacePosition;
            double curRadius = body.pqsController.radiusMax;
            double lastRadius = 0;
            double error = 0;
            int loops = 0;
            float st = Time.time;
            while (loops < 50)
            {
                if (PQS.LineSphereIntersection(relOrigin, mouseRay.direction, curRadius, out relSurfacePosition))
                {
                    Vector3d surfacePoint = body.position + relSurfacePosition;
                    double alt = body.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(body.GetLongitude(surfacePoint), Vector3d.down) *
                                                                     QuaternionD.AngleAxis(body.GetLatitude(surfacePoint), Vector3d.forward) *
                                                                     Vector3d.right);
                    error = Math.Abs(curRadius - alt);
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

        public class ComboBox
        {
            // Easy to use combobox class
            // ***** For users *****
            // Call the Box method with the latest selected item, list of text entries
            // and an object identifying who is making the request.
            // The result is the newly selected item.
            // There is currently no way of knowing when a choice has been made

            // Position of the popup
            private static Rect rect;

            // Identifier of the caller of the popup, null if nobody is waiting for a value
            private static object   popupOwner;
            private static string[] entries;

            private static bool popupActive;

            // Result to be returned to the owner
            private static int selectedItem;

            // Unity identifier of the window, just needs to be unique
            private static readonly int id = GUIUtility.GetControlID(FocusType.Passive);

            // ComboBox GUI Style
            private static readonly GUIStyle style;

            static ComboBox()
            {
                style                     = new GUIStyle(GUI.skin.window);
                style.normal.background   = null;
                style.onNormal.background = null;
                style.border.top          = style.border.bottom;
                style.padding.top         = style.padding.bottom;
            }

            public static void DrawGUI()
            {
                if (popupOwner == null || rect.height == 0 || !popupActive)
                    return;

                if (style.normal.background == null)
                {
                    style.normal.background   = MechJebBundlesManager.comboBoxBackground;
                    style.onNormal.background = MechJebBundlesManager.comboBoxBackground;
                }

                // Make sure the rectangle is fully on screen
                rect.x = Math.Max(0, Math.Min(rect.x, scaledScreenWidth - rect.width));
                rect.y = Math.Max(0, Math.Min(rect.y, scaledScreenHeight - rect.height));

                rect = GUILayout.Window(id, rect, identifier =>
                {
                    selectedItem = GUILayout.SelectionGrid(-1, entries, 1, yellowOnHover);
                    if (GUI.changed)
                        popupActive = false;
                }, "", style);

                //Cancel the popup if we click outside
                if (Event.current.type == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
                    popupOwner = null;
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

                if (dontUseDropDownMenu)
                    return ArrowSelector(selectedItem, entries.Length, entries[selectedItem], expandWidth);

                // A choice has been made, update the return value
                if (popupOwner == caller && !popupActive)
                {
                    popupOwner   = null;
                    selectedItem = ComboBox.selectedItem;
                    GUI.changed  = true;
                }

                bool guiChanged = GUI.changed;
                if (GUILayout.Button("↓ " + entries[selectedItem] + " ↓", GUILayout.ExpandWidth(expandWidth)))
                {
                    // We will set the changed status when we return from the menu instead
                    GUI.changed = guiChanged;
                    // Update the global state with the new items
                    popupOwner       = caller;
                    popupActive      = true;
                    ComboBox.entries = entries;
                    // Magic value to force position update during repaint event
                    rect = new Rect(0, 0, 0, 0);
                }

                // The GetLastRect method only works during repaint event, but the Button will return false during repaint
                if (Event.current.type == EventType.Repaint && popupOwner == caller && rect.height == 0)
                {
                    rect = GUILayoutUtility.GetLastRect();
                    // But even worse, I can't find a clean way to convert from relative to absolute coordinates
                    Vector2 mousePos = Input.mousePosition;
                    mousePos.y = Screen.height - mousePos.y;
                    Vector2 clippedMousePos = Event.current.mousePosition;
                    rect.x = (rect.x + mousePos.x) / scale - clippedMousePos.x;
                    rect.y = (rect.y + mousePos.y) / scale - clippedMousePos.y;
                }

                return selectedItem;
            }
        }
    }

    public class Coordinates
    {
        public double latitude;
        public double longitude;

        public Coordinates(double latitude, double longitude)
        {
            this.latitude  = latitude;
            this.longitude = longitude;
        }

        public static string ToStringDecimal(double latitude, double longitude, bool newline = false, int precision = 3)
        {
            double clampedLongitude = MuUtils.ClampDegrees180(longitude);
            double latitudeAbs = Math.Abs(latitude);
            double longitudeAbs = Math.Abs(clampedLongitude);
            return latitudeAbs.ToString("F" + precision) + "° " + (latitude > 0 ? "N" : "S") + (newline ? "\n" : ", ")
                   + longitudeAbs.ToString("F" + precision) + "° " + (clampedLongitude > 0 ? "E" : "W");
        }

        public string ToStringDecimal(bool newline = false, int precision = 3) => ToStringDecimal(latitude, longitude, newline, precision);

        public static string ToStringDMS(double latitude, double longitude, bool newline = false)
        {
            double clampedLongitude = MuUtils.ClampDegrees180(longitude);
            return AngleToDMS(latitude) + (latitude > 0 ? " N" : " S") + (newline ? "\n" : ", ")
                   + AngleToDMS(clampedLongitude) + (clampedLongitude > 0 ? " E" : " W");
        }

        public string ToStringDMS(bool newline = false) => ToStringDMS(latitude, longitude, newline);

        public static string AngleToDMS(double angle)
        {
            int degrees = (int)Math.Floor(Math.Abs(angle));
            int minutes = (int)Math.Floor(60 * (Math.Abs(angle) - degrees));
            int seconds = (int)Math.Floor(3600 * (Math.Abs(angle) - degrees - minutes / 60.0));

            return string.Format("{0:0}° {1:00}' {2:00}\"", degrees, minutes, seconds);
        }
    }

    public static class ColorPickerHSV
    {
        private static Texture2D displayPicker;

        public static  Color setColor;
        private static Color lastSetColor;

        private static readonly int textureWidth  = 240;
        private static readonly int textureHeight = 240;

        private static float     saturationSlider;
        private static float     alphaSlider;
        private static Texture2D saturationTexture;

        private static void Init()
        {
            displayPicker = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
            for (int i = 0; i < textureWidth; i++)
            {
                for (int j = 0; j < textureHeight; j++)
                {
                    displayPicker.SetPixel(i, j, MuUtils.HSVtoRGB(360f / textureWidth * i, 1.0f / j * textureHeight, 1.0f, 1f));
                }
            }

            displayPicker.Apply();

            float v = 0.0F;
            float diff = 1.0f / textureHeight;
            saturationTexture = new Texture2D(20, textureHeight);
            for (int i = 0; i < saturationTexture.width; i++)
            {
                for (int j = 0; j < saturationTexture.height; j++)
                {
                    saturationTexture.SetPixel(i, j, new Color(v, v, v));
                    v += diff;
                }

                v = 0.0F;
            }

            saturationTexture.Apply();
        }

        public static void DrawGUI(int positionLeft, int positionTop)
        {
            if (!displayPicker)
                Init();

            GUI.Box(new Rect(positionLeft - 3, positionTop - 3, textureWidth + 90, textureHeight + 30), "");

            if (GUI.RepeatButton(new Rect(positionLeft, positionTop, textureWidth, textureHeight), displayPicker))
            {
                int a = (int)Input.mousePosition.x;
                int b = Screen.height - (int)Input.mousePosition.y;

                setColor     = displayPicker.GetPixel(a - positionLeft, -(b - positionTop));
                lastSetColor = setColor;
            }

            saturationSlider = GUI.VerticalSlider(new Rect(positionLeft + textureWidth + 3, positionTop, 10, textureHeight), saturationSlider, 1, 0);
            setColor         = lastSetColor + new Color(saturationSlider, saturationSlider, saturationSlider);
            GUI.Box(new Rect(positionLeft + textureWidth + 20, positionTop, 20, textureHeight), saturationTexture);

            alphaSlider = GUI.VerticalSlider(new Rect(positionLeft + textureWidth + 3 + 10 + 20 + 10, positionTop, 10, textureHeight), alphaSlider, 1,
                0);
            setColor.a = alphaSlider;
            GUI.Box(new Rect(positionLeft + textureWidth + 20 + 10 + 20 + 10, positionTop, 20, textureHeight), saturationTexture);
        }
    }

    public static class ColorPickerRGB
    {
        private static readonly int textureWidth  = 240;
        private static readonly int textureHeight = 10;

        private static Texture2D rTexture;
        private static Texture2D gTexture;
        private static Texture2D bTexture;
        private static Texture2D aTexture;

        private static void Init()
        {
            rTexture = new Texture2D(textureWidth, 1);
            gTexture = new Texture2D(textureWidth, 1);
            bTexture = new Texture2D(textureWidth, 1);
            aTexture = new Texture2D(textureWidth, 1);
            for (int i = 0; i < textureWidth; i++)
            {
                float v = (float)i / (textureWidth - 1);
                rTexture.SetPixel(i, 0, new Color(v, 0, 0));
                gTexture.SetPixel(i, 0, new Color(0, v, 0));
                bTexture.SetPixel(i, 0, new Color(0, 0, v));
                aTexture.SetPixel(i, 0, new Color(v, v, v));
            }

            rTexture.Apply();
            gTexture.Apply();
            bTexture.Apply();
            aTexture.Apply();

            rTexture.wrapMode = TextureWrapMode.Repeat;
            gTexture.wrapMode = TextureWrapMode.Repeat;
            bTexture.wrapMode = TextureWrapMode.Repeat;
            aTexture.wrapMode = TextureWrapMode.Repeat;
        }

        public static Color DrawGUI(int positionLeft, int positionTop, Color c)
        {
            if (!rTexture)
                Init();

            GUI.Box(new Rect(positionLeft - 3, positionTop - 3, textureWidth + 3, textureHeight + 125), "");

            float pos = positionTop + 5;
            GUI.DrawTextureWithTexCoords(new Rect(positionLeft, pos, textureWidth, textureHeight), rTexture, new Rect(0, 0, 1, textureHeight));
            c.r = GUI.HorizontalSlider(new Rect(positionLeft, pos + textureHeight + 5, textureWidth, 10), c.r, 0, 1);

            pos += textureHeight + 20;

            GUI.DrawTextureWithTexCoords(new Rect(positionLeft, pos, textureWidth, textureHeight), gTexture, new Rect(0, 0, 1, textureHeight));
            c.g = GUI.HorizontalSlider(new Rect(positionLeft, pos + textureHeight + 5, textureWidth, 10), c.g, 0, 1);

            pos += textureHeight + 20;

            GUI.DrawTextureWithTexCoords(new Rect(positionLeft, pos, textureWidth, textureHeight), bTexture, new Rect(0, 0, 1, textureHeight));
            c.b = GUI.HorizontalSlider(new Rect(positionLeft, pos + textureHeight + 5, textureWidth, 10), c.b, 0, 1);

            pos += textureHeight + 20;

            GUI.DrawTextureWithTexCoords(new Rect(positionLeft, pos, textureWidth, textureHeight), aTexture, new Rect(0, 0, 1, textureHeight));
            c.a = GUI.HorizontalSlider(new Rect(positionLeft, pos + textureHeight + 5, textureWidth, 10), c.a, 0, 1);

            return c;
        }
    }
}
