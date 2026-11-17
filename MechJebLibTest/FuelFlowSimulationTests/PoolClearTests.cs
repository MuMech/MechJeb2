/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.FuelFlowSimulation.PartModules;
using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    /// <summary>
    ///     Every field of a pooled part or module, whatever it is, goes back to the value a new one has when the pool clears it.
    ///     The test dirties each instance field by reflection (collections and curves get an entry), runs the type's own Clear,
    ///     and compares every field with a fresh object's: a field added later and forgotten in Clear fails here.
    /// </summary>
    public class PoolClearTests
    {
        private const BindingFlags INSTANCE = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const BindingFlags STATIC   = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static IEnumerable<object[]> PooledTypes() => new[]
        {
            new object[] { typeof(SimPart) },
            new object[] { typeof(SimModuleEngines) },
            new object[] { typeof(SimModuleRCS) },
            new object[] { typeof(SimModuleDecouple) },
            new object[] { typeof(SimModuleDockingNode) },
            new object[] { typeof(SimProceduralFairingDecoupler) },
            new object[] { typeof(SimModuleAvionics) },
            new object[] { typeof(SimLaunchClamp) }
        };

        [Theory]
        [MemberData(nameof(PooledTypes))]
        public void ClearResetsEveryField(Type type)
        {
            Dictionary<string, string> fresh = Snapshot(New(type));

            object dirty = New(type);
            List<string> notDirtied = Dirty(dirty);
            Dictionary<string, string> before = Snapshot(dirty);

            Clear(type, dirty);
            Dictionary<string, string> after = Snapshot(dirty);

            // the test only proves something for the fields it could change
            Assert.Empty(notDirtied);
            Assert.DoesNotContain(fresh.Keys, k => before[k] == fresh[k]);

            Assert.Empty(fresh.Keys.Where(k => after[k] != fresh[k]).Select(k => $"{k}: {after[k]}, new {fresh[k]}"));
        }

        private static object New(Type type) => type.GetMethod("New", STATIC)!.Invoke(null, null);

        // SimPart's pool clears through a static Clear(SimPart), the modules' through their instance Clear()
        private static void Clear(Type type, object o)
        {
            MethodInfo? staticClear = type.GetMethod("Clear", STATIC, null, new[] { type }, null);
            if (staticClear != null)
                staticClear.Invoke(null, new[] { o });
            else
                type.GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!
                    .Invoke(o, null);
        }

        private static IEnumerable<FieldInfo> Fields(Type type)
        {
            for (Type? t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (FieldInfo f in t.GetFields(INSTANCE))
                    yield return f;
        }

        private static string Key(FieldInfo f) => f.DeclaringType!.Name + "." + f.Name;

        // value fields by value; a collection or a curve by its size; any other reference by whether it is set
        private static Dictionary<string, string> Snapshot(object o)
        {
            var values = new Dictionary<string, string>();
            foreach (FieldInfo f in Fields(o.GetType()))
            {
                object? v = f.GetValue(o);
                values[Key(f)] = v switch
                {
                    null                                         => "null",
                    string s                                     => "\"" + s + "\"",
                    _ when f.FieldType.IsValueType               => v.ToString(),
                    _ when CountOf(v) is int n                   => "count " + n,
                    _ when v.GetType().GetProperty("IsEmpty") is { } p => "empty " + p.GetValue(v),
                    _                                            => "set"
                };
            }

            return values;
        }

        private static int? CountOf(object v) => v.GetType().GetProperty("Count", typeof(int))?.GetValue(v) as int?;

        // gives each field a value other than its current one; returns the fields it could not change
        private static List<string> Dirty(object o)
        {
            var failed = new List<string>();
            SimPart other = New(typeof(SimPart)) as SimPart ?? throw new InvalidOperationException();
            SimVessel vessel = SimVessel.Borrow();

            foreach (FieldInfo f in Fields(o.GetType()))
            {
                object? v = f.GetValue(o);
                Type t = f.FieldType;
                object? dirty = t switch
                {
                    _ when t == typeof(bool)   => !(bool)v!,
                    _ when t == typeof(int)    => (int)v! + 7,
                    _ when t == typeof(uint)   => (uint)v! + 7,
                    _ when t == typeof(float)  => (float)v! + 1.5f,
                    _ when t == typeof(double) => (double)v! + 1.5,
                    _ when t == typeof(V3)     => (V3)v! + new V3(1, 2, 3),
                    _ when t == typeof(string) => v + "x",
                    _ when t.IsEnum            => NextEnum(t, v!),
                    _ when t.IsAssignableFrom(typeof(SimPart)) && !f.IsInitOnly => other,
                    _ when t.IsAssignableFrom(typeof(SimVessel)) && !f.IsInitOnly => vessel,
                    _                          => null
                };

                if (dirty != null && !f.IsInitOnly)
                {
                    f.SetValue(o, dirty);
                    continue;
                }

                if (v is DoubleInterpolant curve)
                {
                    PartFixtures.AppendConstant(curve, 0, 1, 1);
                    continue;
                }

                if (v != null && AddEntry(v))
                    continue;

                failed.Add(Key(f) + " (" + t.Name + ")");
            }

            return failed;
        }

        private static object NextEnum(Type t, object v)
        {
            Array values = Enum.GetValues(t);
            foreach (object e in values)
                if (!e.Equals(v))
                    return e;
            return v;
        }

        // one entry with default arguments, through the collection's or the curve's own Add
        private static bool AddEntry(object collection)
        {
            string before = CountOf(collection)?.ToString() ?? collection.GetType().GetProperty("IsEmpty")?.GetValue(collection)?.ToString() ?? "";
            if (before == "") return false;

            foreach (MethodInfo m in collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.Name == "Add"))
            {
                ParameterInfo[] ps = m.GetParameters();
                object?[] args = ps.Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
                try
                {
                    m.Invoke(collection, args);
                }
                catch (TargetInvocationException)
                {
                    continue;
                }

                string after = CountOf(collection)?.ToString() ?? collection.GetType().GetProperty("IsEmpty")?.GetValue(collection)?.ToString() ?? "";
                if (after != before)
                    return true;
            }

            return false;
        }
    }
}
