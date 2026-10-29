using KodeKit;
using KodeUI;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleWaypointHelpWindow ("Waypoint Help") in KodeKit: the topic grid, and for each topic its sections
    /// (a button, and the text under it while selected). The texts come from the module, so both UIs show the same.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleWaypointHelpWindow))]
    public class WaypointHelpView : KodeView
    {
        const float TextWidth = 400;

        readonly MechJebModuleWaypointHelpWindow help;

        public WaypointHelpView(MechJebModuleWaypointHelpWindow help) : base(help) => this.help = help;

        protected override float MinWidth => TextWidth + 16;

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            Layout grid = KodeWindow.Row(r);
            if (KodeWindow.ModernLayout) w.Segmented(grid, help.Topics, () => help.SelTopic, i => help.SelTopic = i, ChoiceStyle.Tabs); // navigation
            else w.SelectionGrid(grid, help.Topics, () => help.SelTopic, i => help.SelTopic = i);

            for (int t = 0; t < help.Topics.Length; t++)
            {
                int topic = t;
                Layout panel = KodeWindow.ListBox(r);
                w.Visible(panel.gameObject, () => help.SelTopic == topic);
                help.HelpContent(help.Topics[t], (title, text) =>
                {
                    Layout section = KodeWindow.Panel(panel);
                    RoverUi.StateButton(w, section, title, () => help.ToggleSubTopic(title), () => help._selSubTopic == title);
                    UIText body = KodeWindow.Paragraph(section, text);
                    body.PreferredWidth(TextWidth);
                    w.Visible(body.gameObject, () => help._selSubTopic == title);
                });
            }
        }
    }
}
