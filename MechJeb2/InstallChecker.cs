using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using KSP.Localization;

namespace MuMech
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class InstallChecker : MonoBehaviour
    {
        protected void Start()
        {
            var assemblies = AssemblyLoader.loadedAssemblies.Where(a => a.assembly.GetName().Name == Assembly.GetExecutingAssembly().GetName().Name).Where(a => a.url != "MechJeb2/Plugins");
            if (assemblies.Any())
            {
                var badPaths = assemblies.Select(a => a.path).Select(p => Uri.UnescapeDataString(new Uri(Path.GetFullPath(KSPUtil.ApplicationRootPath)).MakeRelativeUri(new Uri(p)).ToString().Replace('/', Path.DirectorySeparatorChar)));
                PopupDialog.SpawnPopupDialog(
                    new MultiOptionDialog("InstallCheckerA",
                        null, Localizer.Format("#MechJeb_InstallCheckA_title"),//"Incorrect MechJeb2 Installation"
                        HighLogic.UISkin,
                        new Rect(0.5f, 0.5f, 100f, 100f),
                        new DialogGUIContentSizer(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.MinSize),
                        new DialogGUILabel(Localizer.Format("#MechJeb_InstallCheckA_msg") + string.Join("\n", badPaths.ToArray())),//"MechJeb2 has been installed incorrectly and will not function properly.\nAll MechJeb2 files should be located in KSP like this \n<KSP>\n\tGameData\n\t\tMechJeb2\n\t\t\tParts\n\t\t\tPlugins\n\nDo not move any files from inside the MechJeb2 folder.\n\nIncorrect path(s):\n"
                        new DialogGUIButton("OK", () => { }, true)
                    ), false, HighLogic.UISkin);
            }
            assemblies = AssemblyLoader.loadedAssemblies.Where(a=> a.assembly.GetName().Name == "MechJebMenuToolbar" );
            if (assemblies.Any())
            {
                var badPaths = assemblies.Select(a => a.path).Select(p => Uri.UnescapeDataString(new Uri(Path.GetFullPath(KSPUtil.ApplicationRootPath)).MakeRelativeUri(new Uri(p)).ToString().Replace('/', Path.DirectorySeparatorChar)));
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "InstallCheckerB", Localizer.Format("#MechJeb_InstallCheckB_title"), Localizer.Format("#MechJeb_InstallCheckB_msg") + string.Join("\n", badPaths.ToArray()), "OK", false, HighLogic.UISkin);//"Redundant MechJebMenuToolbar Installation""MechJebMenuToolbar is installed but this version of MechJeb2 already includes support for Blizzy78 Toolbar Plugin.\nPlease delete this dll:\n"
            }
        }
    }
}