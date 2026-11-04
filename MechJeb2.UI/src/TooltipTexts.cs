using System.Collections.Generic;
using KSP.Localization;

namespace MuMech.UI
{
    /// <summary>
    /// Tooltips of the new UI (KodeWindow.TooltipFor): the localization key of the widget plus "_tooltip", MechJeb's
    /// convention (e.g. #MechJeb_Ascent_checkbox18_tooltip). The texts are in Localization/*.cfg.
    /// </summary>
    public static class TooltipTexts
    {
        // looked up when a tooltip shows: cache the answer, found or not, for the current language
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        static string cacheLanguage;

        public static string For(string key)
        {
            if (string.IsNullOrEmpty(key) || key[0] != '#') return null;
            if (cacheLanguage != Localizer.CurrentLanguage)
            {
                cache.Clear();
                cacheLanguage = Localizer.CurrentLanguage;
            }
            if (cache.TryGetValue(key, out string t)) return t;
            if (!Localizer.TryGetStringByTag(key + "_tooltip", out t)) t = null;
            cache[key] = t;
            return t;
        }
    }
}
