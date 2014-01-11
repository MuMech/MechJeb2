using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

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
        public double val;
        public readonly double multiplier;

        public bool parsed;
        [Persistent]
        public string _text;
        public virtual string text
        {
            get { return _text; }
            set
            {
                _text = value;
                _text = Regex.Replace(_text, @"[^\d+-.]", ""); //throw away junk characters
                double parsedValue;
                parsed = double.TryParse(_text, out parsedValue);
                if (parsed) val = parsedValue * multiplier;
            }
        }

        public EditableDoubleMult() : this(0) { }

        public EditableDoubleMult(double val, double multiplier = 1)
        {
            this.val = val;
            this.multiplier = multiplier;
            _text = (val / multiplier).ToString();
        }

        public static implicit operator double(EditableDoubleMult x)
        {
            return x.val;
        }
    }

    public class EditableDouble : EditableDoubleMult
    {
        public EditableDouble(double val)
            : base(val)
        {
        }

        public static implicit operator EditableDouble(double x)
        {
            return new EditableDouble(x);
        }
    }

    public class EditableTime : EditableDouble
    {
        public EditableTime() : this(0) { }

        public EditableTime(double seconds)
            : base(seconds)
        {
            _text = GuiUtils.TimeToDHMS(seconds);
        }

        public override string text
        {
            get { return _text; }
            set
            {
                _text = value;
                _text = Regex.Replace(_text, @"[^\d+-.dhms ,]", ""); //throw away junk characters

                double parsedValue;
                parsed = double.TryParse(_text, out parsedValue);
                if (parsed) val = parsedValue;

                if (!parsed)
                {
                    parsed = GuiUtils.TryParseDHMS(_text, out parsedValue);
                    if (parsed) val = parsedValue;
                }
            }
        }

        public static implicit operator EditableTime(double x)
        {
            return new EditableTime(x);
        }
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

            negative = (angle < 0);
            angle = Math.Abs(angle);
            degrees = (int)angle;
            angle -= degrees;
            minutes = (int)(60 * angle);
            angle -= minutes / 60;
            seconds = Math.Round(3600 * angle);
        }

        public static implicit operator double(EditableAngle x)
        {
            return (x.negative ? -1 : 1) * (x.degrees + x.minutes / 60.0 + x.seconds / 3600.0);
        }

        public static implicit operator EditableAngle(double x)
        {
            return new EditableAngle(x);
        }

        public enum Direction { NS, EW }

        public void DrawEditGUI(Direction direction)
        {
            GUILayout.BeginHorizontal();
            degrees.text = GUILayout.TextField(degrees.text, GUILayout.Width(35));
            GUILayout.Label("°", GUILayout.ExpandWidth(false));
            minutes.text = GUILayout.TextField(minutes.text, GUILayout.Width(35));
            GUILayout.Label("'", GUILayout.ExpandWidth(false));
            seconds.text = GUILayout.TextField(seconds.text, GUILayout.Width(35));
            GUILayout.Label("\"", GUILayout.ExpandWidth(false));
            String dirString = (direction == Direction.NS ? (negative ? "S" : "N") : (negative ? "W" : "E"));
            if (GUILayout.Button(dirString, GUILayout.Width(25))) negative = !negative;
            GUILayout.EndHorizontal();
        }
    }

    public class EditableInt : IEditable
    {
        [Persistent]
        public int val;

        public bool parsed;
        [Persistent]
        public string _text;
        public virtual string text
        {
            get { return _text; }
            set
            {
                _text = value;
                _text = Regex.Replace(_text, @"[^\d+-]", ""); //throw away junk characters
                int parsedValue;
                parsed = int.TryParse(_text, out parsedValue);
                if (parsed) val = parsedValue;
            }
        }

        public EditableInt() : this(0) { }

        public EditableInt(int val)
        {
            this.val = val;
            _text = val.ToString();
        }

        public static implicit operator int(EditableInt x)
        {
            return x.val;
        }

        public static implicit operator EditableInt(int x)
        {
            return new EditableInt(x);
        }
    }

    public class ZombieGUILoader : MonoBehaviour
    {
        void OnGUI()
        {
            GuiUtils.CopyDefaultSkin();
            if (GuiUtils.skin == null) GuiUtils.skin = GuiUtils.defaultSkin;
            GameObject.Destroy(gameObject);
        }
    }

    public static class GuiUtils
    {
        static GUIStyle _yellowOnHover;
        public static GUIStyle yellowOnHover
        {
            get
            {
                if (_yellowOnHover == null)
                {
                    _yellowOnHover = new GUIStyle(GUI.skin.label);
                    _yellowOnHover.hover.textColor = Color.yellow;
                    Texture2D t = new Texture2D(1, 1);
                    t.SetPixel(0, 0, new Color(0, 0, 0, 0));
                    t.Apply();
                    _yellowOnHover.hover.background = t;
                }
                return _yellowOnHover;
            }
        }

        public enum SkinType { Default, MechJeb1, Compact }
        public static GUISkin skin;
        public static GUISkin defaultSkin;
        public static GUISkin compactSkin;

        public static void CopyDefaultSkin()
        {
            GUI.skin = null;
            defaultSkin = (GUISkin)GameObject.Instantiate(GUI.skin);
        }

        public static void CopyCompactSkin()
        {
            GUI.skin = null;
            compactSkin = (GUISkin)GameObject.Instantiate(GUI.skin);

            GuiUtils.skin.name = "KSP Compact";

            compactSkin.label.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.label.padding = new RectOffset(0, 0, 2, 2);

            compactSkin.button.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.button.padding = new RectOffset(4, 4, 2, 2);

            compactSkin.toggle.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.toggle.padding = new RectOffset(15, 0, 2, 0);

            compactSkin.textField.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.textField.padding = new RectOffset(2, 2, 2, 2);

            compactSkin.textArea.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.textArea.padding = new RectOffset(2, 2, 2, 2);

            compactSkin.window.margin = new RectOffset(0, 0, 0, 0);
            compactSkin.window.padding = new RectOffset(5, 5, 20, 5);
        }


        public static void LoadSkin(SkinType skinType)
        {
            switch (skinType)
            {
                case SkinType.Default:
                    if (defaultSkin == null) CopyDefaultSkin();
                    skin = defaultSkin;
                    break;

                case SkinType.MechJeb1:
                    skin = AssetBase.GetGUISkin("KSP window 2");
                    break;

                case SkinType.Compact:
                    if (compactSkin == null) CopyCompactSkin();
                    skin = compactSkin;
                    break;
            }
        }

        public static void CheckSkin()
        {
            GUI.skin = null;
            if (GUI.skin.name == "LazorSkin")
            {
                Texture2D tex = new Texture2D(64, 31, TextureFormat.ARGB32, false);
                tex.LoadImage(Properties.Resources.default_gui_window);
                GUI.skin.window.normal.background = GUI.skin.window.onNormal.background = tex;
                GUI.skin.window.normal.textColor = GUI.skin.window.onNormal.textColor = XKCDColors.Pink;

                tex = new Texture2D(20, 20, TextureFormat.ARGB32, false);
                tex.LoadImage(Properties.Resources.default_toggle_on);
                GUI.skin.toggle.onNormal.background = GUI.skin.toggle.onHover.background = GUI.skin.toggle.onActive.background = tex;
                tex = new Texture2D(20, 20, TextureFormat.ARGB32, false);
                tex.LoadImage(Properties.Resources.default_toggle_off);
                GUI.skin.toggle.normal.background = GUI.skin.toggle.hover.background = GUI.skin.toggle.active.background = tex;

                GUI.skin.name = "LazorSkinBow";
            }
        }

        public static void SimpleTextBox(string leftLabel, IEditable ed, string rightLabel = "", float width = 100)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(leftLabel, GUILayout.ExpandWidth(true));
            ed.text = GUILayout.TextField(ed.text, GUILayout.ExpandWidth(true), GUILayout.Width(width));
            GUILayout.Label(rightLabel, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        public static void SimpleLabel(string leftLabel, string rightLabel = "")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(leftLabel, GUILayout.ExpandWidth(true));
            GUILayout.Label(rightLabel, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        public static void SimpleLabelInt(string leftLabel, int rightValue)
        {
            SimpleLabel(leftLabel, rightValue.ToString());
        }

        public static int ArrowSelector(int index, int numIndices, Action centerGuiAction)
        {
            if (numIndices == 0) return index;

            GUILayout.BeginHorizontal();
            if (numIndices > 1 && GUILayout.Button("◀", GUILayout.ExpandWidth(false))) index = (index - 1 + numIndices) % numIndices;
            centerGuiAction();
            if (numIndices > 1 && GUILayout.Button("▶", GUILayout.ExpandWidth(false))) index = (index + 1) % numIndices;
            GUILayout.EndHorizontal();

            return index;
        }

        public static int ArrowSelector(int index, int modulo, string label)
        {
            Action drawLabel = () => GUILayout.Label(label, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, stretchWidth = true });
            return ArrowSelector(index, modulo, drawLabel);
        }


        //from http://wiki.unity3d.com/index.php?title=PopupList
        public static bool List(Rect position, ref bool showList, ref int listEntry, 
            GUIContent buttonContent, string[] list, GUIStyle listStyle)
        {
            return List(position, ref showList, ref listEntry, buttonContent, list, "button", "box", listStyle);
        }

        public static bool List(Rect position, ref bool showList, ref int listEntry, GUIContent buttonContent, string[] list,
                                 GUIStyle buttonStyle, GUIStyle boxStyle, GUIStyle listStyle)
        {
            int controlID = GUIUtility.GetControlID(865645, FocusType.Passive);
            bool done = false;
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.mouseDown:
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = controlID;
                        showList = true;
                    }
                    break;
                case EventType.mouseUp:
                    if (showList)
                    {
                        done = true;
                    }
                    break;
            }

            GUI.Label(position, buttonContent, buttonStyle);
            if (showList)
            {
                Rect listRect = new Rect(position.x, position.y, position.width, list.Length * 20);
                GUI.Box(listRect, "", boxStyle);
                listEntry = GUI.SelectionGrid(listRect, listEntry, list, 1, listStyle);
            }
            if (done)
            {
                showList = false;
            }
            return done;
        }



        public static string TimeToDHMS(double seconds, int decimalPlaces = 0)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds)) return "Inf";

            string ret = "";
            bool showSecondsDecimals = decimalPlaces > 0;

            try
            {
                string[] units = { "y", "d", "h", "m", "s" };
                long[] intervals = { 365 * 24 * 3600, 24 * 3600, 3600, 60, 1 };

                if (seconds < 0)
                {
                    ret += "-";
                    seconds *= -1;
                }

                for (int i = 0; i < units.Length; i++)
                {
                    long n = (long)(seconds / intervals[i]);
                    bool first = ret.Length < 2;
                    if (!first || (n != 0) || (i == units.Length - 1 && ret == ""))
                    {
                        if (!first) ret += " ";

                        if (showSecondsDecimals && seconds < 60 && i == units.Length -1) ret += seconds.ToString("0." + new string('0', decimalPlaces));
                        else if (first) ret += n.ToString();
                        else ret += n.ToString("00");

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
            int[] intervals = { 365 * 24 * 3600, 24 * 3600, 3600, 60, 1 };

            s = s.Trim(' ');
            bool minus = (s.StartsWith("-"));

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
                    seconds += value * intervals[i];
                    s = s.Substring(unitPos + 1);
                    parsedSomething = true;
                }
            }

            if (minus) seconds = -seconds;

            return parsedSomething;
        }
        
        public static double FromToETA(Vector3 From, Vector3 To, double Speed = 0) {
        	double a = (FlightGlobals.ActiveVessel.mainBody.transform.position - From).magnitude;
        	double b = (FlightGlobals.ActiveVessel.mainBody.transform.position - To).magnitude;
        	double c = Vector3d.Distance(From, To);
        	double ang = Math.Acos(((a * a + b * b) - c * c) / (double)(2f * a * b));
        	return ang * FlightGlobals.ActiveVessel.mainBody.Radius / (Speed > 0 ? Speed : FlightGlobals.ActiveVessel.horizontalSrfSpeed);
        }

        public static bool MouseIsOverWindow(MechJebCore core)
        {
            //try to check if the mouse is over any active DisplayModule
            foreach (DisplayModule m in core.GetComputerModules<DisplayModule>())
            {
                if (m.enabled && m.showInCurrentScene
                    && m.windowPos.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
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
                    double alt = body.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(body.GetLongitude(surfacePoint), Vector3d.down) * QuaternionD.AngleAxis(body.GetLatitude(surfacePoint), Vector3d.forward) * Vector3d.right);
                    error = Math.Abs(curRadius - alt);
                    if (error < (body.pqsController.radiusMax - body.pqsController.radiusMin) / 100)
                    {
                        return new Coordinates(body.GetLatitude(surfacePoint), MuUtils.ClampDegrees180(body.GetLongitude(surfacePoint)));
                    }
                    else
                    {
                        lastRadius = curRadius;
                        curRadius = alt;
                        loops++;
                    }
                }
                else
                {
                    if (loops == 0)
                    {
                        break;
                    }
                    else
                    { // Went too low, needs to try higher
                        curRadius = (lastRadius * 9 + curRadius) / 10;
                        loops++;
                    }
                }
            }

            return null;
        }

    }

    public class Coordinates
    {
        public double latitude;
        public double longitude;

        public Coordinates(double latitude, double longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }

        public static string ToStringDecimal(double latitude, double longitude, bool newline = false, int precision = 3)
        {
            double clampedLongitude = MuUtils.ClampDegrees180(longitude);
            double latitudeAbs  = Math.Abs(latitude);
            double longitudeAbs = Math.Abs(clampedLongitude);
            return latitudeAbs.ToString("F" + precision) + "° " + (latitude > 0 ? "N" : "S") + (newline ? "\n" : ", ")
                + longitudeAbs.ToString("F" + precision) + "° " + (clampedLongitude > 0 ? "E" : "W");
        }

        public string ToStringDecimal(bool newline = false, int precision = 3)
        {
            return ToStringDecimal(latitude, longitude, newline, precision);
        }

        public static string ToStringDMS(double latitude, double longitude, bool newline = false)
        {
            double clampedLongitude = MuUtils.ClampDegrees180(longitude);
            return AngleToDMS(latitude) + (latitude > 0 ? " N" : " S") + (newline ? "\n" : ", ")
                 + AngleToDMS(clampedLongitude) + (clampedLongitude > 0 ? " E" : " W");
        }

        public string ToStringDMS(bool newline = false)
        {
            return ToStringDMS(latitude, longitude, newline);
        }

        public static string AngleToDMS(double angle)
        {
            int degrees = (int)Math.Floor(Math.Abs(angle));
            int minutes = (int)Math.Floor(60 * (Math.Abs(angle) - degrees));
            int seconds = (int)Math.Floor(3600 * (Math.Abs(angle) - degrees - minutes / 60.0));

            return String.Format("{0:0}° {1:00}' {2:00}\"", degrees, minutes, seconds);
        }
    }
}
