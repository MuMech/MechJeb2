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
        double val;
        public readonly double multiplier;

        public bool parsed;
        private string _text;
        public string text
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
                
        public EditableDouble(double val, double multiplier = 1)
        {
            this.val = val;
            this.multiplier = multiplier;
            text = val.ToString();
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

    public static class GuiUtils
    {
        public static void SimpleTextBox(string label, EditableDouble ed, double multiplier)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            ed.text = GUILayout.TextField(ed.text, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }
    }
}
