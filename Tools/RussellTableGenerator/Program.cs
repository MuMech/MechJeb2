/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.IO;

namespace RussellTableGenerator
{
    /// <summary>
    ///     Generates MechJebLib/Lambert/RussellGuessTables.cs, the interpolation tables for the initial guesses of Russell's
    ///     Lambert solver (see <see cref="TableBuilder" />).  To regenerate run:
    ///     dotnet run -c Release --project Tools/RussellTableGenerator [-- output-path]
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.Error.WriteLine("usage: RussellTableGenerator [output-path]");
                return 1;
            }

            string path = args.Length == 1 ? args[0] : Path.Combine(RepoRoot(), "MechJebLib", "Lambert", "RussellGuessTables.cs");
            File.WriteAllText(path, TableBuilder.Generate());
            Console.WriteLine($"wrote {path}");
            return 0;
        }

        private static string RepoRoot()
        {
            string? dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "MechJeb2.sln")))
                dir = Path.GetDirectoryName(dir);
            return dir ?? throw new Exception("could not find MechJeb2.sln above the generator, pass the output path instead");
        }
    }
}
