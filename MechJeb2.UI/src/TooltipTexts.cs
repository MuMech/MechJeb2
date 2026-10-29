using System.Collections.Generic;

namespace MuMech.UI
{
    /// <summary>
    /// Tooltips of the new UI, by the localization key of the widget they explain (KodeWindow.TooltipFor). A value
    /// may itself be a '#key' (localized when shown). English text for now: no MechJeb localization keys exist yet.
    /// </summary>
    public static class TooltipTexts
    {
        public static string For(string key) => texts.TryGetValue(key, out string t) ? t : null;

        static readonly Dictionary<string, string> texts = new Dictionary<string, string>
        {
            // throttle limiters (Utilities, Ascent Settings)
            ["#MechJeb_Ascent_checkbox11"] = "Lower the throttle so the dynamic pressure (Q) stays under this value. Protects fragile parts and fairings in the lower atmosphere.",
            ["#MechJeb_Ascent_checkbox12"] = "Lower the throttle when a part gets close to its maximum temperature.",
            ["#MechJeb_Ascent_checkbox16"] = "Lower the throttle so the acceleration stays under this value.",
            ["#MechJeb_Ascent_checkbox17"] = "Never throttle above this percentage.",
            ["#MechJeb_Ascent_checkbox20"] = "Throttle down when electric charge is low: full throttle above Hi, zero below Lo, scaled in between.",
            ["#MechJeb_Ascent_checkbox13"] = "Lower the throttle of jet engines when the air intakes can't supply enough air, to avoid a flameout.",
            ["#MechJeb_Ascent_checkbox14"] = "Real Fuels: only ignite engines when the propellant is settled.",
            ["#MechJeb_Ascent_checkbox15"] = "Real Fuels: fire RCS forward to settle the propellant before an ignition.",
            ["#MechJeb_Ascent_checkbox19"] = "Throttle engines individually so the thrust stays centred on the centre of mass.",
            ["#MechJeb_Utilities_checkbox3"] = "Open and close air intakes as the jet engines need them.",
            ["#MechJeb_Utilities_checkbox1"] = "Stage automatically when the current stage is empty (see the settings below).",

            // SmartASS
            ["#MechJeb_SmartASS_button11"] = "Release attitude control.",
            ["#MechJeb_SmartASS_button12"] = "Stop the vessel's rotation.",
            ["#MechJeb_SmartASS_button13"] = "Point at the next maneuver node's burn direction.",
            ["#MechJeb_SmartASS_button1"] = "Orbital directions: prograde, normal, radial.",
            ["#MechJeb_SmartASS_button2"] = "Surface directions: heading, pitch and roll.",
            ["#MechJeb_SmartASS_button3"] = "Directions relative to the target.",
            ["#MechJeb_SmartASS_button4"] = "Any direction in any reference frame.",
            ["#MechJeb_SmartASS_button15"] = "Point along the orbital velocity.",
            ["#MechJeb_SmartASS_button16"] = "Point against the orbital velocity.",
            ["#MechJeb_SmartASS_button17"] = "Point along the orbit normal (towards the north side of the orbit plane).",
            ["#MechJeb_SmartASS_button18"] = "Point against the orbit normal.",
            ["#MechJeb_SmartASS_button19"] = "Point away from the body you orbit.",
            ["#MechJeb_SmartASS_button20"] = "Point towards the body you orbit.",
            ["#MechJeb_SmartASS_button21"] = "Point along the velocity relative to the target.",
            ["#MechJeb_SmartASS_button22"] = "Point against the velocity relative to the target.",
            ["#MechJeb_SmartASS_button23"] = "Point at the target.",
            ["#MechJeb_SmartASS_button24"] = "Point away from the target.",
            ["#MechJeb_SmartASS_button25"] = "Align with the target's orientation.",
            ["#MechJeb_SmartASS_button26"] = "Align against the target's orientation.",
            ["#MechJeb_SmartASS_button58"] = "Apply the values above.",
        };
    }
}
