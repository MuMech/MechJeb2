/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>Rules every class of the headless folder keeps (LAYOUTS.md, "What goes through the commit path").</summary>
    public class HeadlessConventionsTests
    {
        // the owner callbacks: a delegate that decides or performs what the widget does, set once by its owner
        private static readonly string[] OwnerCallbacks =
        {
            "ChoiceModel.Allow", "StepperModel.Allow", "TransferModel.Allow", "ReorderModel.IsFixed", "UndoStack.Guard",
            "DialogModel.OwnerAlive", "DialogModel.OnConfirm", "DialogModel.OnCancel", "DialogModel.OnOther", "ComboModel.Group",
            "CommitPath.Edited", "CommitPath.Describer", "CommitPath.ShowLabel", "ListEdits.Count", "ListEdits.Label", "ListEdits.Selected", "ListEdits.Select",
            "ListEdits.SourceCount", "ListEdits.SourceLabel", "KeyScope.Alive", "ValueModel.Allow", "PaletteModel.Clock"
        };

        private static bool IsDelegate(Type t) => typeof(Delegate).IsAssignableFrom(t);

        [Fact]
        public void EveryNotificationIsAnEventNotADelegateFieldASecondSubscriberWouldOverwrite()
        {
            Type[] types = typeof(ChoiceModel).Assembly.GetTypes().Where(t => t.Namespace == "KodeKit.Headless" && t.IsPublic).ToArray();
            string[] fields = types
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(f => IsDelegate(f.FieldType)).Select(f => t.Name + "." + f.Name))
                .Concat(types.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(p => IsDelegate(p.PropertyType) && p.CanWrite && p.GetSetMethod() != null).Select(p => t.Name + "." + p.Name)))
                .Select(n => n.Replace("`1", ""))
                .Where(n => !OwnerCallbacks.Contains(n))
                .ToArray();
            Assert.True(fields.Length == 0, "public delegate fields or setters that are no owner callback (make them events): " + string.Join(", ", fields));
        }

        private static string UiFolder([CallerFilePath] string here = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", "..", "MechJeb2.UI"));

        // a handler that reports a literal source ignores a host's CommitPath.As(Agent): an agent's event would be the player's
        private static readonly Regex Literal = new Regex(@"(?<![\w.])(?:Headless\.)?ChangeSource\.(Pointer|Keyboard)(?!\w)");
        private static readonly Regex Allowed = new Regex(@"Source\((?:Headless\.)?ChangeSource\.Keyboard\)|ChangeSource \w+ = (?:Headless\.)?ChangeSource\.");

        [Fact]
        public void EveryUnitySideHandlerReportsTheSourceAHostImposes()
        {
            string ui = UiFolder();
            if (!Directory.Exists(ui)) return; // the sources are not next to this build
            var found = new List<string>();
            foreach (string file in Directory.GetFiles(ui, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(ui.Length + 1).Replace('\\', '/');
                // the headless models take the source as an argument; KodeWindow.cs defines Pointer and Source
                if (rel.StartsWith("KodeKit/Headless/") || rel == "KodeKit/KodeWindow.cs" || rel.StartsWith("KodeUI/") || rel.StartsWith("obj/")) continue;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string l = Allowed.Replace(lines[i], "");
                    if (Literal.IsMatch(l)) found.Add(rel + ":" + (i + 1) + ": " + lines[i].Trim());
                }
            }
            Assert.True(found.Count == 0, "use KodeWindow.Pointer or KodeWindow.Source(ChangeSource.Keyboard):\n" + string.Join("\n", found));
        }

        [Fact]
        public void TwoSubscribersBothHearTheDropDownClose()
        {
            int v = 0, a = 0, b = 0;
            var c = new ComboModel(() => v, (i, s) => v = i);
            c.SetItems(new[] { "x", "y" });
            c.Closed += () => a++;
            c.Closed += () => b++;
            c.Open();
            c.Close();
            Assert.Equal(1, a);
            Assert.Equal(1, b);
        }
    }
}
