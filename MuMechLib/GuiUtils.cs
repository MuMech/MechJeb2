using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MuMech
{
    //An EditableDouble stores a double value and a text string. The user can edit the string. 
    //Whenever the text is edited, it is parsed and the parsed value is stored in val. As a 
    //convenience, a multiplier can be specified so that the stored value is actually 
    //(multiplier * parsed value). If the parsing fails, the parsed flag is set to false and
    //the stored value is unchanged. There are implicit conversions between EditableDouble and
    //double so that if you are not doing text input you can treat an EditableDouble like a double.
    public class EditableDouble
    {
        protected double val;
        public readonly double multiplier;

        public bool parsed;
        protected string _text;
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

        public EditableDouble() : this(0) { }
        
        public EditableDouble(double val, double multiplier = 1)
        {
            this.val = val;
            this.multiplier = multiplier;
            _text = val.ToString();
        }

        public static implicit operator double(EditableDouble x)
        {
            return x.val;
        }

        public static implicit operator EditableDouble(double x)
        {
            return new EditableDouble(x);
        }
    }

    public class EditableTime : EditableDouble
    {
        public EditableTime() : this(0) { }

        public EditableTime(double seconds) : base(seconds)                 
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
    }

    public static class GuiUtils
    {
        public static void SimpleTextBox(string label, EditableDouble ed)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            ed.text = GUILayout.TextField(ed.text, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        public static string TimeToDHMS(double seconds)
        {
            int[] intervals = { 24 * 3600, 3600, 60 , 1};
            string[] units = { "d", "h", "m", "s" };

            string ret = "";

            if(seconds < 0) 
            {
                ret += "-";
                seconds *= -1;
            }

            for (int i = 0; i < 4; i++)
            {
                int n = (int)(seconds / intervals[i]);
                if (n != 0 || (i == 3 && ret == "")) ret += n.ToString() + units[i];
                seconds -= n * intervals[i];
            }

            return ret;
        }

        public static bool TryParseDHMS(string s, out double seconds)
        {
            string[] units = { "d", "h", "m", "s" };
            double[] intervals = { 24 * 3600, 3600, 60, 1 };

            seconds = 0;
            bool parsedSomething = false;
            for (int i = 0; i < 4; i++)
            {
                s = s.Trim(' ', ',');
                int unitPos = s.IndexOf(units[i]);
                if (unitPos != -1)
                {
                    double value;
                    if (!double.TryParse(s.Substring(0, unitPos), out value)) return false;
                    seconds += value * intervals[i];
                    s = s.Substring(unitPos+1);
                    parsedSomething = true;
                }
            }

            return parsedSomething;
        }


        public static bool MouseIsOverWindow(MechJebCore core)
        {
            //try to check if the mouse is over any active displaymodule
            foreach (DisplayModule m in core.GetComputerModules<DisplayModule>())
            {
                if (m.enabled && m.flightWindowPos.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
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
            if (PQS.LineSphereIntersection(relOrigin, mouseRay.direction, body.Radius, out relSurfacePosition))
            {
                Vector3d surfacePoint = body.position + relSurfacePosition;
                return new Coordinates(body.GetLatitude(surfacePoint), MuUtils.ClampDegrees180(body.GetLongitude(surfacePoint)));
            }
            else
            {
                return null;
            }
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
            return latitude.ToString("F" + precision) + "° " + (latitude > 0 ? "N" : "S") + (newline ? "\n" : ", ")
                + clampedLongitude.ToString("F" + precision) + "° " + (clampedLongitude > 0 ? "E" : "W");
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

            return String.Format("{0:0}° {1:0}' {2:0}\"", degrees, minutes, seconds);
        }
    }

}
