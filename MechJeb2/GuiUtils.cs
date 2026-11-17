extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JetBrainsAnnotations::JetBrains.Annotations;
using MechJebLib.Utils;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;
using static MechJebLib.Utils.Statics;

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
    //
    //ValConfig (the multiplied value) is the source of truth. TextConfig is persisted as well, but after
    //loading it is regenerated from ValConfig if it doesn't parse back to the same value, so changing the
    //multiplier (or display format) of an existing field is safe without migrating old saves. Changing the
    //units of the stored value itself is not, and needs a new config key.
    public class EditableDoubleMult : IEditable, IPersistenceLoad
    {
        [UsedImplicitly, Persistent]
        public double ValConfig;

        public virtual double Val
        {
            get => ValConfig;
            set
            {
                ValConfig = value;
                TextConfig = (ValConfig / _multiplier).ToString();
                Parsed = true; // the text is now the value's
            }
        }

        private readonly double _multiplier;

        protected bool Parsed = true; // text generated from the value is valid until the user types something that isn't

        /// <summary>False while the text does not parse (the value keeps its last good value).</summary>
        public bool IsParsed => Parsed;

        [UsedImplicitly, Persistent]
        public string TextConfig;

        public virtual string Text
        {
            get => TextConfig;
            set
            {
                TextConfig = value;
                // throw away junk characters, keeping e/E so scientific notation like "1e6" parses (it used to become "16")
                TextConfig = Regex.Replace(TextConfig, @"[^\d+,\-.eE]", "");
                Parsed = TryParseText(TextConfig, out double parsedValue);
                if (Parsed) ValConfig = parsedValue;
            }
        }

        // parses display text into the stored (multiplied) value
        protected virtual bool TryParseText(string text, out double val)
        {
            bool parsed = NumberText.TryParseDouble(text, out val);
            val *= _multiplier;
            return parsed;
        }

        public EditableDoubleMult() : this(0) { }

        public EditableDoubleMult(double val, double multiplier = 1)
        {
            Val = val;
            _multiplier = multiplier;
            TextConfig = (val / multiplier).ToString();
        }

        // called by ConfigNode.LoadObjectFromConfig after loading, the persisted text may be stale if the multiplier changed
        public void PersistenceLoad()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (!TryParseText(TextConfig, out double val) || val != ValConfig)
                Val = ValConfig;
        }

        public static implicit operator double(EditableDoubleMult x) => x.Val;

        public override string ToString() => Val.ToString();
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
                ValConfig = value;
                TextConfig = GuiUtils.TimeToDHMS(ValConfig);
                Parsed = true; // the text is now the value's
            }
        }

        public override string Text
        {
            get => TextConfig;
            set
            {
                TextConfig = value;
                TextConfig = Regex.Replace(TextConfig, @"[^\d+-.ydhms ,]", ""); //throw away junk characters
                Parsed = TryParseText(TextConfig, out double parsedValue);
                if (Parsed) ValConfig = parsedValue;
            }
        }

        protected override bool TryParseText(string text, out double val) =>
            NumberText.TryParseDouble(text, out val) || GuiUtils.TryParseDHMS(text, out val);

        public static implicit operator EditableTime(double x) => new EditableTime(x);

        public override string ToString() => GuiUtils.TimeToDHMS(Val);
    }

    public class EditableAngle : IPersistenceLoad
    {
        [UsedImplicitly, Persistent]
        public readonly EditableDouble Degrees;

        [UsedImplicitly, Persistent]
        public readonly EditableDouble Minutes;

        [UsedImplicitly, Persistent]
        public readonly EditableDouble Seconds;

        [UsedImplicitly, Persistent]
        public bool Negative;

        // The fields show the angle to the nearest arc second, which is 9 m on a 3.2x Kerbin and 31 m on Earth. The angle keeps
        // its exact value while the fields still show what it was rounded to; once the player edits them, the fields are the angle.
        [UsedImplicitly, Persistent]
        public double Exact;

        private double _rounded;

        public EditableAngle(double angle)
        {
            Exact = Clamp180(angle);
            Split(Exact, out Negative, out double degrees, out double minutes, out double seconds);
            Degrees = new EditableDouble(degrees);
            Minutes = new EditableDouble(minutes);
            Seconds = new EditableDouble(seconds);
            _rounded = FieldValue;
        }

        private static void Split(double angle, out bool negative, out double degrees, out double minutes, out double seconds)
        {
            negative = angle < 0;
            // rounded to the arc second before it is split, so the seconds never read 60
            double totalSeconds = Math.Round(3600 * Math.Abs(angle));
            degrees = Math.Floor(totalSeconds / 3600);
            minutes = Math.Floor(totalSeconds % 3600 / 60);
            seconds = totalSeconds % 60;
        }

        // A minus typed in a field flips the hemisphere, as it does in a decimal angle: -5° 30' N is 5° 30' S.
        private static double Join(bool negative, double degrees, double minutes, double seconds)
        {
            negative ^= degrees < 0 || minutes < 0 || seconds < 0;
            return (negative ? -1 : 1) * (Math.Abs(degrees) + Math.Abs(minutes) / 60.0 + Math.Abs(seconds) / 3600.0);
        }

        private double FieldValue => Join(Negative, Degrees, Minutes, Seconds);

        // the loaded fields keep the saved exact angle only if they are its rounding; a config saved without it gets the fields
        public void PersistenceLoad()
        {
            Split(Exact, out bool negative, out double degrees, out double minutes, out double seconds);
            _rounded = Join(negative, degrees, minutes, seconds);
        }

        public static implicit operator double(EditableAngle x)
        {
            double fields = x.FieldValue;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return fields == x._rounded ? x.Exact : fields;
        }

        public static implicit operator EditableAngle(double x) => new EditableAngle(x);

        public enum Direction { NS, EW }
    }

    public class EditableInt : IEditable
    {
        [UsedImplicitly, Persistent]
        public int ValConfig;

        public int Val
        {
            get => ValConfig;
            set
            {
                ValConfig = value;
                TextConfig = value.ToString();
            }
        }

        private bool _parsed;

        [UsedImplicitly, Persistent]
        public string TextConfig;

        public virtual string Text
        {
            get => TextConfig;
            set
            {
                TextConfig = value;
                TextConfig = Regex.Replace(TextConfig, @"[^\d+-]", ""); //throw away junk characters
                _parsed = int.TryParse(TextConfig, out int parsedValue);
                if (_parsed) Val = parsedValue;
            }
        }

        public EditableInt(int val)
        {
            Val = val;
            TextConfig = val.ToString();
        }

        public static implicit operator int(EditableInt x) => x.Val;

        public static implicit operator EditableInt(int x) => new EditableInt(x);
    }

    public class EditableIntList : IEditable
    {
        [Persistent]
        public readonly List<int> Val = new List<int>();

        [UsedImplicitly, Persistent]
        public string TextConfig = "";

        public string Text
        {
            get => TextConfig;
            set
            {
                TextConfig = value;
                TextConfig = Regex.Replace(TextConfig, @"[^\d-,]", ""); //throw away junk characters
                // supports "1,2,3" and "1-3", a range is limited to NumberText.MAX_RANGE_LENGTH entries
                NumberText.ParseIntList(TextConfig, Val);
            }
        }
    }

    public static class GuiUtils
    {
        public static float Scale = 1;
        public static int ScaledScreenWidth = 1;
        public static int ScaledScreenHeight = 1;
        public static bool DontUseDropDownMenu = false;
        public static bool ShowAdvancedWindowSettings = false;

        // the scaled size follows the screen: a resolution change in flight (Alt+Enter, a resized window) recomputes it
        static GuiUtils() => GameEvents.onScreenResolutionModified.Add((width, height) => SetGUIScale(Scale));

        public static void SetGUIScale(double s)
        {
            Scale = Mathf.Clamp((float)s, 0.2f, 5f);
            ScaledScreenHeight = Mathf.RoundToInt(Screen.height / Scale);
            ScaledScreenWidth = Mathf.RoundToInt(Screen.width / Scale);
        }

        public static string TimeToDHMS(double seconds, int decimalPlaces = 0)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds)) return "Inf";

            // a calendar without days or years (a length of 0): hours are the largest unit
            long year = KSPUtil.dateTimeFormatter.Year > 0 ? KSPUtil.dateTimeFormatter.Year : long.MaxValue;
            long day = KSPUtil.dateTimeFormatter.Day > 0 ? KSPUtil.dateTimeFormatter.Day : long.MaxValue;

            // from a thousand years, years with an SI prefix (ISO 80000-3's "a"): "398.6 Ma", not "398597826y 072d 00h 06m 54s"
            double years = seconds / year;
            if (Math.Abs(years) >= 1000) return years.ToSI() + "a";

            StringBuilder ret = _dhmsBuilder ??= new StringBuilder();
            ret.Clear();
            bool showSecondsDecimals = decimalPlaces > 0;

            try
            {
                bool negative = seconds < 0;
                seconds = Math.Abs(seconds);
                // round to the decimals shown before splitting into units: 239.97 s is "4m 00.0s", not "3m 60.0s"
                if (showSecondsDecimals)
                    seconds = (double)Math.Round((decimal)seconds, Math.Min(decimalPlaces, 9), MidpointRounding.AwayFromZero);
                // no "-00.0s" for a value that rounds to zero
                if (negative && (showSecondsDecimals ? seconds > 0 : seconds >= 1)) ret.Append('-');

                for (int i = 0; i < DHMS_UNITS.Length; i++)
                {
                    long interval = i == 0 ? year : i == 1 ? day : DHMS_SECONDS[i];
                    long n = (long)(seconds / interval);
                    bool first = ret.Length < 2;
                    // the seconds always show when nothing else did, after a "-" too ("-00.5s", not "-")
                    if (!first || n != 0 || i == DHMS_UNITS.Length - 1)
                    {
                        if (!first) ret.Append(' ');

                        if (showSecondsDecimals && seconds < 60 && i == DHMS_UNITS.Length - 1)
                            ret.Append(seconds.ToString(SecondsFormat(decimalPlaces)));
                        else if (first) ret.Append(n);
                        else
                        {
                            // zero padded to two digits, three for the days
                            if (i == 1 && n < 100) ret.Append('0');
                            if (n < 10) ret.Append('0');
                            ret.Append(n);
                        }

                        ret.Append(DHMS_UNITS[i]);
                    }

                    seconds -= n * interval;
                }
            }
            catch (Exception)
            {
                return "NaN";
            }

            return ret.ToString();
        }

        // TimeToDHMS's units; the year and day lengths come from KSP's calendar at each call
        private static readonly char[] DHMS_UNITS = { 'y', 'd', 'h', 'm', 's' };
        private static readonly long[] DHMS_SECONDS = { 0, 0, 3600, 60, 1 };

        [ThreadStatic]
        private static StringBuilder _dhmsBuilder;

        private static readonly List<string> _secondsFormats = new List<string>();

        // "00.0", "00.00"...: one string per number of decimals, made once
        private static string SecondsFormat(int decimalPlaces)
        {
            lock (_secondsFormats)
            {
                while (_secondsFormats.Count <= decimalPlaces)
                    _secondsFormats.Add("00." + new string('0', _secondsFormats.Count));
                return _secondsFormats[decimalPlaces];
            }
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

                // as a plain number in the same field reads it: "1,5h" is an hour and a half
                if (!NumberText.TryParseDouble(s.Substring(0, unitPos), out double value)) return false;
                seconds += value * intervals[i];
                s = s.Substring(unitPos + 1);
                parsedSomething = true;
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
            double denominator = 2 * a * b;
            if (denominator <= 0)
                return 0;
            double ang = SafeAcos((a * a + b * b - c * c) / denominator);
            return ang * FlightGlobals.ActiveVessel.mainBody.Radius;
        }

        public static double FromToETA(Vector3 from, Vector3 to, double speed = 0) =>
            ArcDistance(from, to) / (speed > 0 ? speed : FlightGlobals.ActiveVessel.horizontalSrfSpeed);

        // MechJeb's windows are all drawn by the UI host, which reports its own hover
        public static bool MouseIsOverWindow(MechJebCore core) => MechJebUiHost.MouseOverHostWindow();

        public static Coordinates GetMouseCoordinates(CelestialBody body)
        {
            Ray mouseRay = PlanetariumCamera.Camera.ScreenPointToRay(Input.mousePosition);
            mouseRay.origin = ScaledSpace.ScaledToLocalSpace(mouseRay.origin);
            Vector3d relOrigin = mouseRay.origin - body.position;

            // a body without terrain (Jool, the Sun): the sphere of its radius, where stock's TerrainAltitude is 0
            if (body.pqsController == null)
            {
                if (!PQS.LineSphereIntersection(relOrigin, mouseRay.direction, body.Radius, out Vector3d relSpherePosition))
                    return null;
                Vector3d spherePoint = body.position + relSpherePosition;
                return new Coordinates(body.GetLatitude(spherePoint), Clamp180(body.GetLongitude(spherePoint)));
            }

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
                        return new Coordinates(body.GetLatitude(surfacePoint), Clamp180(body.GetLongitude(surfacePoint)));
                    }

                    lastRadius = curRadius;
                    curRadius = alt;
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
    }

    public class Coordinates
    {
        public readonly double Latitude;
        public readonly double Longitude;

        public Coordinates(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        [UsedImplicitly]
        public static string ToStringDecimal(double latitude, double longitude, bool newline = false, int precision = 3)
        {
            double clampedLongitude = Clamp180(longitude);
            double latitudeAbs = Math.Abs(latitude);
            double longitudeAbs = Math.Abs(clampedLongitude);
            return latitudeAbs.ToString("F" + precision) + "° " + (latitude >= 0 ? "N" : "S") + (newline ? "\n" : ", ")
                + longitudeAbs.ToString("F" + precision) + "° " + (clampedLongitude >= 0 ? "E" : "W");
        }

        public string ToStringDecimal(bool newline = false, int precision = 3) => ToStringDecimal(Latitude, Longitude, newline, precision);

        public static string ToStringDMS(double latitude, double longitude, bool newline = false)
        {
            double clampedLongitude = Clamp180(longitude);
            return AngleToDMS(latitude) + (latitude >= 0 ? " N" : " S") + (newline ? "\n" : ", ")
                + AngleToDMS(clampedLongitude) + (clampedLongitude >= 0 ? " E" : " W");
        }

        public string ToStringDMS(bool newline = false) => ToStringDMS(Latitude, Longitude, newline);

        public static string AngleToDMS(double angle)
        {
            // rounded to the arc second first, then split in whole numbers: flooring each floating remainder read one second low
            long totalSeconds = (long)Math.Round(3600 * Math.Abs(angle));
            long degrees = totalSeconds / 3600;
            long minutes = totalSeconds % 3600 / 60;
            long seconds = totalSeconds % 60;

            return $"{degrees:0}° {minutes:00}' {seconds:00}\"";
        }
    }
}
