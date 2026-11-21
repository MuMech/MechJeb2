using System;
using System.Collections.Generic;
using KodeKit;
using KSP.Localization;

namespace MuMech.UI
{
    /// <summary>
    /// An info item's hover card (the custom window editor's lists): its name, its live value when something computes it anyway,
    /// its description and tooltip, its category. Showing a card never computes a value nothing else computes, and never adds a
    /// module user: an item that no shown window and no running module computes says "not computed".
    /// </summary>
    public static class InfoItemCard
    {
        /// <summary>Fills <paramref name="card"/> for <paramref name="item"/> (nothing for null: a tree's branch row).</summary>
        public static void Build(HoverCard card, InfoItem item, MechJebCore core)
        {
            if (item == null) return;
            string name = string.IsNullOrEmpty(item.localizedName) ? item.localizedDescription : item.localizedName;
            card.Title(name);
            if (item is ValueInfoItem v)
            {
                if (Computed(item, core)) card.Value("#MechJeb_NewUI_Card_Value", InfoItemWidgets.ValueWriter(v));
                else
                {
                    string none = Localizer.Format("#MechJeb_NewUI_Card_NotComputed"); //not computed: ...
                    card.Value("#MechJeb_NewUI_Card_Value", ct => ct.Append(none));
                }
            }
            if (!string.IsNullOrEmpty(item.localizedDescription) && item.localizedDescription != name) card.Text(item.localizedDescription, true);
            card.Text(item.localizedTooltip);
            card.Text(Localizer.Format("#MechJeb_NewUI_Card_Category", MechJebModuleCustomWindowEditor.categories[(int)item.category]), true);
        }

        /// <summary>
        /// Whether <paramref name="item"/>'s value is computed now by something else than the card, so the card may read it: a shown
        /// custom window holds it (the window runs its getter), or its source is VesselState in flight, or a module that is enabled.
        /// The info items module's own getters compute on demand (some start simulations), so only a window counts for them. A flag
        /// on the item, not a try: nothing is called to find out.
        /// </summary>
        public static bool Computed(InfoItem item, MechJebCore core)
        {
            if (!(item is ValueInfoItem v) || core == null) return false;
            List<ComputerModule> windows = core.GetComputerModules<MechJebModuleCustomInfoWindow>();
            for (int i = 0; i < windows.Count; i++)
                if (windows[i] is MechJebModuleCustomInfoWindow w && w.Enabled && w.ShowInCurrentScene && w.items.Contains(item))
                    return true;
            switch (v.Target)
            {
                case VesselState _:
                    return HighLogic.LoadedSceneIsFlight;
                case MechJebModuleInfoItems _:
                    return false;
                case ComputerModule m:
                    return m.Enabled;
            }
            return false;
        }
    }
}
