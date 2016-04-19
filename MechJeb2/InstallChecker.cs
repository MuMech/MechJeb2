using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

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
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Incorrect MechJeb2 Installation", "MechJeb2 has been installed incorrectly and will not function properly. All MechJeb2 files should be located in KSP like this \n<KSP>\n\tGameData\n\t\tMechJeb2\n\t\t\tParts\n\t\t\tPlugins\n\nDo not move any files from inside the MechJeb2 folder.\n\nIncorrect path(s):\n" + String.Join("\n", badPaths.ToArray()), "OK", false, HighLogic.UISkin);
            }
            assemblies = AssemblyLoader.loadedAssemblies.Where(a=> a.assembly.GetName().Name == "MechJebMenuToolbar" );
            if (assemblies.Any())
            {
                var badPaths = assemblies.Select(a => a.path).Select(p => Uri.UnescapeDataString(new Uri(Path.GetFullPath(KSPUtil.ApplicationRootPath)).MakeRelativeUri(new Uri(p)).ToString().Replace('/', Path.DirectorySeparatorChar)));
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Redundant MechJebMenuToolbar Installation", "MechJebMenuToolbar is installed but this version of MechJeb2 already includes support for Blizzy78 Toolbar Plugin.\nPlease delete this dll:\n" + String.Join("\n", badPaths.ToArray()), "OK", false, HighLogic.UISkin);
            }
        }
    }
}