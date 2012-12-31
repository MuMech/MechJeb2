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
                _text = Regex.Replace(_text, @"[^\d+-.dhms]", ""); //throw away junk characters

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
        public static void SimpleTextBox(string label, EditableDouble ed, double multiplier)
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

    }
}
